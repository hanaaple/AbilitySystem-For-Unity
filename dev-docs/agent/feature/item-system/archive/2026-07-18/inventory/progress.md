# inventory-system — 인벤토리 시스템

- 상태: 🔧 IN-PROGRESS (⏸ 2026-07-06 개발 일시 중단 — item 코어 테스트 목적 달성. I1·I2(test-stage)까지 됨, 재개는 item 이후)
- 우선순위: P1
- 의존: [`item`](../item/progress.md) (아이템 코어)
- 최종 갱신: 2026-07-06 (KST)

## 목표
아이템을 보유·관리하는 인벤토리를 **캐릭터에 종속되지 않는 재사용 컨테이너**로 구현한다. 어떤 액터(플레이어·적·상자)든 각자 가질 수 있고, 인벤 자체는 캐릭터·ASC·장비를 전혀 모른다. 장비/소비 종류와 개수·스택을 관리하며, 변경을 이벤트로 통지한다.

## 수용 기준 (Definition of Done)
- [ ] 아이템 추가/제거/조회 + 개수·스택 관리가 동작하고, 변경 시 이벤트로 통지
- [ ] 인벤토리 코어가 **씬 없이 EditMode 테스트** 통과 (순수 컨테이너 → 씬 의존 0)
- [ ] 소유자에 연결돼 획득/소모가 반영되고, (씬/GameObject 파괴가 아니라) 소유자 사망 시 인벤이 소실된다
- [ ] item HARNESS §2 불변 조건 위반 0

## 범위
### 포함
- 아이템 보유 컬렉션(엔트리), 추가/제거/조회
- **개수·스택** (2026-07-05 유저 결정으로 §1 스코프 확장 — D2)
- **장비/소비 종류 구분**: 저장은 단일 컬렉션, 종류는 `ItemData` 타입·능력(`IEquippable`/`IStackable` 등)에서 파생되는 읽기 질의로 노출. 종류가 늘어도(충전형 등) 질의만 추가 (D5)
- 변경 이벤트(`OnChanged` 등), 순수 컨테이너 코어 + 얇은 컴포넌트 래핑
- **수명 귀속**: 인벤 데이터는 POCO라 씬/GameObject에 안 묶이고 소유자 수명을 따른다 — 소유자가 소실을 구동 (D6·D8)
### 제외 (명시적으로 하지 않을 것)
- **UI 연동** (2026-07-05 유저 지시로 이번 범위 제외 — D7. 이벤트는 노출하되 구독 UI는 나중)
- **세이브·네트워크** (여전히 item HARNESS §1 스코프 밖)
- **장비 배선(장착→ASC 스탯 적용)** — 별도 `equipment` feature
- **드롭·시체 루팅·아이템 이전** (사망 = 단순 소실, 재평가 트리거는 D6)
- 등급/획득 연출/드롭 테이블

## 설계 개요
- **불변 조건의 진실:** [`../HARNESS.md`](../HARNESS.md) §2 (INV-1~12)를 그대로 따른다. 특히 INV-6(공유상태는 캐릭터 소유)·INV-7(단방향)·INV-8(이벤트 구독)·INV-11(능력/데이터로 판별, 카테고리 하드코딩 금지). 인벤 전용 하네스는 만들지 않는다 — 결정은 이 progress 결정 기록에 누적.

### 담을 대상 (item 코어 재사용)
- **종류 = `ItemData` 서브클래스** (item D4): `EquipItem`(장비, `IEquippable` + `SlotType{Weapon,Armor,Accessory}`) / `ConsumeItem`(소비). 인벤의 "장비/소비 구분"은 새 축을 만들지 않고 이 타입에서 파생한다.
- **`ItemInstance`(POCO)**: 모듈별 상태(`IModuleState[]`)를 소유하는 per-item 런타임 상태. 장비의 개별성 근거.

### 엔트리·스택 모델 (D4 — 핵심)
```
InventoryEntry { ItemData Data;  int Count;  ItemInstance Instance; }
```
- **스택은 능력이다(`IStackable`):** 스택 가능 여부/상한을 전 아이템 공통 필드로 두지 않는다 — 장비엔 스택 개념이 없기 때문. 기존 `IEquippable`과 같은 패턴으로 `IStackable { int MaxStack }` 능력 인터페이스를 두고, **스택되는 종류만(소비·미래 충전형) 구현**한다. 컨테이너는 `data is IStackable`로 판별 → 카테고리를 하드코딩하지 않는다(INV-11).
- **스택 가능(`IStackable`, 소비 등):** `(Data, Count)`로 병합 저장(`Count < MaxStack`인 같은 Data 엔트리에 합침, 넘치면 새 엔트리), `Instance=null` (상태 없으니 인스턴스 불필요).
- **스택 불가(`IStackable` 아님, 장비):** 엔트리마다 고유 `ItemInstance`, `Count=1` (per-item 모듈 상태를 공유하면 안 되므로 절대 병합 안 함).
  - ⚠ **교차 feature 영향:** `IStackable` 인터페이스 + `ConsumeItem`의 `MaxStack` 구현은 `item` feature 코드에 추가된다(장비 `EquipItem`은 미구현 → 스택 불가). I1 착수 시 item 코어를 최소 수정하며, 사유를 item worklog에도 남긴다.
  - 미래(충전형 등): 종류가 늘면 능력/타입 질의만 추가하면 되고 저장 구조는 불변. 충전형처럼 per-item 상태(충전량)를 갖는 종류는 장비와 마찬가지로 `ItemInstance`를 갖고 스택 정책을 재검토(D4 재평가 트리거).

### 커플링 원칙 (소유·참조 — D8)
- `Inventory`(순수 코어)는 **아무것도 모른다** — 캐릭터·ASC·장비·UI·씬 전부 무의존. 아는 타입은 `ItemData`·`ItemInstance`·`IStackable`(분기)뿐(INV-6/7).
- **소유는 이 컨테이너의 관심사가 아니다:** `Inventory`는 소유자를 모르는 POCO다. MonoBehaviour가 아니라 **씬 수명에 안 묶인다** — 참조를 쥔 소유자가 사는 한 산다.
  - 장기 보유자는 **지속 소유자 계층**(현재 미존재 — 블로커)이 생기면 그가 생성·보유한다. per-character(캐릭터마다 각자 인벤).
  - 씬의 `InventoryComponent`(I2)는 소유자가 아니라 그 POCO를 **바인딩/노출만 하는 뷰** → 컴포넌트가 파괴돼도(씬 전환·몸체 재생성) 데이터는 안 죽는다.
- **수명·소실:** "죽으면 소실"은 **소유자가 `Clear()`를 구동**하는 것이지 GameObject 파괴가 아니다. 세이브(디스크 영속화)는 여전히 스코프 밖. (D6·D8)
- UI·표현은 인벤 **이벤트를 구독**해 갱신(INV-8). 폴링 금지. (UI 자체 구현은 범위 밖 — D7)
- 장착은 `equipment` feature가 인벤에서 `ItemInstance`/`ItemData`를 **받아가는** 방향 — 인벤이 Equipment를 모른다.

- 관련 아키텍처 요약: [`../../../../project/architecture/item-equipment.md`](../../../../project/architecture/item-equipment.md)

## 세부 TODO (구현 체크리스트)
- [ ] I1 — 인벤 코어(`Inventory` POCO): `InventoryEntry` 자료구조 + 추가/제거/조회 + `IStackable` 기반 스택 병합 + 변경 이벤트(`OnChanged`). (선행: `IStackable` 인터페이스 + `ConsumeItem.MaxStack` 최소 추가 — 교차 feature)
- [ ] I1 검증 — 씬 없이 EditMode 테스트: 소비 추가→개수 증가·스택 상한 병합, 장비 추가→개별 엔트리 유지, 제거 반영
- [~] I2 — 컴포넌트 바인딩(`InventoryComponent`): **test-stage 착수(2026-07-06)** — 컴포넌트가 POCO 직접 소유, `startingItems` 시드, 사망 시 `Clear()`. Player 프리팹에 부착·무기 시드 완료. **정식 D8(계층 소유·컴포넌트는 참조)은 지속 소유자 계층 생길 때 전환**(빙의 body-swap이 재평가 트리거).
- [ ] I3 — (보류) UI: 인벤 이벤트 구독으로 갱신되는 최소 UI (폴링 금지, INV-8) — 유저 지시로 이번 범위 제외(D7)

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-07-05 | `item-system`을 item/inventory/equipment로 3분할 (이 feature는 인벤만) |
| D2 | 2026-07-05 | 인벤 스코프에 개수·스택 포함 (세이브·네트워크는 계속 제외) |
| D3 | 2026-07-05 | 인벤 = 무의존 순수 컨테이너, `CharacterBase` 소유, UI는 이벤트 구독 (INV-6/7/8) |
| D4 | 2026-07-05 | 엔트리·스택 모델: 스택은 `IStackable` 능력(장비 미구현), 소비=`(Data,Count)` 병합 / 장비=개별 `ItemInstance` |
| D5 | 2026-07-05 | 장비/소비 저장 = 단일 컬렉션, 종류는 타입·능력에서 파생 질의 (충전형 등 확장도 질의만 추가) |
| D6 | 2026-07-05 | 인벤 수명 = 소유자 수명(사망 시 소실). 드롭·루팅·이전은 스코프 밖 |
| D7 | 2026-07-05 | UI 이번 범위 제외(유저 지시). 이벤트는 노출, 구독 UI는 나중 |
| D8 | 2026-07-05 | Inventory 소유를 GameObject/씬에 안 묶음 — 소유자 미지정 POCO, 씬 컴포넌트는 바인딩 뷰. 지속 소유자 계층 생길 때까지 I2 배선 보류. D3 대체·D6 정제 |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- **I2 배선 블로킹:** 인벤 POCO를 장기 보유할 **지속 소유자 계층이 아직 없다**(유저 확인). 그 계층 전엔 `InventoryComponent`↔소유자↔사망 배선을 확정할 수 없다 → I1(순수 코어)은 무블로킹 진행, I2는 대기. (D8)
- **EditMode 테스트 인프라 부재:** 게임 코드가 전부 `Assembly-CSharp`(무 asmdef)라 테스트 asmdef가 참조 불가(Unity 제약). → **유저 결정: 임시로 에디터 메뉴 셀프체크 채택**(`Tools/Inventory/Run Self-Check`), 정식 asmdef화(EditMode)는 보류. 수용 기준의 "씬 없이 EditMode 테스트"는 **미충족 상태 유지**(셀프체크는 잠정 검증일 뿐) — 정식 테스트 도입 시 셀프체크 제거.
- (해소) 그룹 [`../HARNESS.md`](../HARNESS.md) §1 스코프 문구 충돌 → 유저 승인으로 "인벤토리·개수/스택 스코프 안"으로 정정 완료.

## 다음 작업
1. **(완료) `IStackable` + `ConsumeItem.MaxStack`** 추가됨(item 코어). **I1 코어 작성됨:** `Assets/Scripts/Core/ItemSystem/Inventory/`에 `Inventory`(POCO)·`InventoryEntry{Data,Count,Instance}` — 추가/제거/조회 + `IStackable` 스택 병합 + `OnChanged`. 소유자 미지정(D8).
2. **(완료) I1 검증:** `Tools/Inventory/Run Self-Check` 실행 → 콘솔 `[Inventory Self-Check] passed 9 / failed 0` (스택 병합·장비 개별·원자적 제거·Query·OnChanged 모두 PASS, 2026-07-06). 정식 EditMode 테스트는 asmdef 인프라 결정 후(수용 기준은 그때 충족).
3. **I2(블로킹) — 컴포넌트 바인딩:** 지속 소유자 계층이 생기면 `InventoryComponent`가 그 POCO를 노출·바인딩, 사망 시 `Clear()`.
4. **I3(보류) — UI:** 유저 지시로 범위 제외.
