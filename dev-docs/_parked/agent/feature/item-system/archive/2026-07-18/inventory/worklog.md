# inventory — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로. 과거 기록은 삭제·수정하지 않는다(오기 정정은 취소선).
> 결정을 가리킬 땐 `(→D#)`(decisions.md), 로드맵 작업은 `(I#)` 코드로 링크한다.

### 2026-07-06 (2) — I2 부분 착수: InventoryComponent를 캐릭터에 부착 (유저 지시)
- **유저 지시:** "Inventory 먼저 만들어서 캐릭터에 부착시키고 그쪽부터." → I2(컴포넌트 바인딩)를 **테스트 단계 형태로** 착수. 원래 I2 블로커(지속 소유자 계층 부재)는 유지되나, 유저가 캐릭터 부착을 지시 → **컴포넌트가 POCO를 직접 소유하는 최소판**으로 진행.
- **`InventoryComponent`**(`Core/ItemSystem/Inventory/`): `Inventory` POCO 소유·노출, 직렬화 `startingItems`(ItemData 리스트) 시드, `CharacterBase.OnDeath` 구독 → `Clear()`(D6 소실). D8 "바인딩 뷰"의 test-stage 버전 — 주석에 **재평가 트리거(빙의 body-swap 시 참조 주입으로 전환)** 명시.
- **씬 배선:** `Weapon_TestSword`(EquipItem, Slot=Weapon) 에셋 생성 → `Player.prefab`에 `InventoryComponent`(startingItems=[무기])·`EquipmentTestBootstrap` 부착(EquipmentComponent·ASC는 기존). 부트스트랩은 이제 로컬 인벤 생성이 아니라 **이 컴포넌트의 Inventory에서** 장비를 뽑아 장착.
- **D8 유지 여부:** 컴포넌트 소유는 D8 원안(계층 소유·컴포넌트는 참조)과 어긋나므로 **test-stage 예외**로 명시하고 재평가 트리거를 걸어둠. 빙의 생존이 실제 요구가 되면 전환.

### 2026-07-06
- **I1 검증 완료:** `Tools/Inventory/Run Self-Check` 실행 → 콘솔 `[Inventory Self-Check] passed 9 / failed 0`. 스택 병합(2개→엔트리1·Count2, 누적4/max3→엔트리2)·OnChanged Add당 1회·장비 IStackable 아님·장비 개별 엔트리+ItemInstance 보유·부족분 제거 거부(무변경)·정상 제거·Query(IEquippable) 전부 PASS. I1 코어 정합성 확인됨(정식 EditMode는 asmdef 인프라 대기).

### 2026-07-05 (3)
- **소유·수명 재설계 (→D8):** 유저 우려 — "인벤이 몸체(GameObject)에 종속돼 씬 전환/종료와 함께 사라지면 안 됨" + "타 캐릭터도 각자 인벤". 지속 소유자 계층은 아직 없음(유저 확인). → `Inventory`는 **소유자 미지정 POCO**(씬 수명 무관), 씬 컴포넌트는 바인딩 뷰, 사망 소실은 소유자가 `Clear()` 구동. D3의 "CharacterBase 소유" 대체·D6 정제.
- **I1 코어 작성:** `Core/ItemSystem/Inventory/`에 `Inventory`·`InventoryEntry`(순수 POCO). Add(스택=IStackable 병합/비스택=개별 ItemInstance)·Remove(원자적·엔트리 지목)·CountOf·Query(카테고리 파생)·Clear + `OnChanged`. 새 폴더 신설(§3-2 보고).
- **블로커 2건:** ① I2(컴포넌트·사망 배선)는 지속 소유자 계층 대기. ② EditMode 테스트 인프라 부재(게임 코드가 무 asmdef `Assembly-CSharp`라 테스트 asmdef가 참조 불가) — 인프라 결정 필요, 유저 상의.
- **I1 검증(유저 결정: 임시 에디터 셀프체크):** `Core/ItemSystem/Inventory/Editor/InventorySelfCheck.cs` — 메뉴 `Tools/Inventory/Run Self-Check`가 Inventory를 구동해 스택 병합(max3: 4→3+1)·장비 개별 엔트리·ItemInstance 보유·원자적 제거·Query(IEquippable)·OnChanged 횟수를 콘솔 PASS/FAIL로 검증. 정식 asmdef화(EditMode)는 보류, 도입 시 이 파일 제거. (maxStack은 private 직렬화라 리플렉션으로 주입)

### 2026-07-05 (2)
- **구현 착수 전 정리 세션.** 그룹 HARNESS §1~§7·item/inventory progress·decisions·아키텍처(item-equipment)·item 코어 코드(ItemData/EquipItem/ConsumeItem/ItemInstance/ItemModule)·`CharacterBase`·`EquipmentComponent`를 통독.
- 유저 신규 요구를 문서에 반영: 장비/소비 구분·개수/스택·캐릭터 소유·사망 시 상실·**UI 제외**·이후 equipment.
- 신규 설계 결정 확정 (→D4·D5·D6·D7): 엔트리·스택 모델(데이터 주도 `MaxStack`, 소비=count / 장비=개별 `ItemInstance`), 단일 컬렉션+종류 파생 질의, 수명=소유자(사망 소실), UI 보류.
- 확인 사실: 종류/부위는 이미 item 코어에 존재(EquipItem/ConsumeItem·SlotType). `CharacterBase`에 `OnDeath`/`IsAlive` 있어 사망 처리 앵커 확보. `MaxStack`는 item 코어 침투(교차 feature) — I1 착수 시 최소 추가 예정.
- 유저 확인 3건 반영: ① D5 단일 컬렉션 확정(+"필터가 충전형 등으로 다양해질 것" → 술어 질의 확장성으로 근거 보강) ② **스택을 `ItemData.MaxStack` 공통 필드 → `IStackable` 능력 인터페이스로 재설계**(유저: "장비엔 스택 개념 없다"; `IEquippable`과 동형, 장비 미구현→스택 불가, INV-11) ③ 그룹 HARNESS §1 스코프 문구 "인벤토리 스코프 안"으로 정정(승인).
- 그룹 HARNESS **INV-2 문구 명확화**(유저 요청): 상속만 금지이고 합성 기반 별개 `ItemRuntime`은 허용임을 INV-10과 상호 참조로 명시(오해 방지). 위반↔허용 예시 대비 추가.

### 2026-07-05
- `item-system` → `item` rename, `inventory-system`·`equipment-system` feature 분리. feature-list·architecture 링크 갱신 (→D1)
- 인벤 스코프(개수·스택 포함)·커플링 원칙(무의존 컨테이너 + CharacterBase 소유 + 이벤트 구독) 확정 (→D2·D3)
- **구현 순서 근거(item 완성 전에 인벤/장비 먼저):** 간단한 Item을 만들어 실제로 장착해봄으로써 ① 장착 시 **Module이 실제 작동하는지**, ② **Socket을 만들어 해당 부위(slot)에 부착되는지** 등을 **디버깅과 함께 테스트**하기 위함. 즉 인벤/장비는 item 코어를 눈으로 검증하는 실험 도구 역할.
