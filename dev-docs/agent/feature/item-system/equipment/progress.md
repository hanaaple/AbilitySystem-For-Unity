# equipment-system — 장비 장착 시스템

- 상태: 🔧 IN-PROGRESS (⏸ 2026-07-06 개발 일시 중단 — item 코어 테스트 목적 달성. E1 검증·E2 부분까지 됨, 재개는 item 이후)
- 우선순위: P1
- 의존: [`item`](../item/progress.md) (아이템 코어), [`inventory`](../inventory/progress.md) (장착 대상 공급)
- 최종 갱신: 2026-07-06 (KST)

## 목표
아이템을 **부위(slot)에 장착**하고, 장착된 아이템의 스탯 기여를 캐릭터에 반영·회수한다. **캐릭터(`PlayerCharacter`)에 종속되지 않고 ASC(스탯 시스템)에만 연결**한다.

## 수용 기준 (Definition of Done)
- [ ] `EquipItem`(`IEquippable`)을 slot에 장착/해제, `OnEquipped/Unequipped` 이벤트 통지
- [ ] 장착 시 모듈/`ItemRuntime` 스탯 기여가 `GameplayEffect`로 ASC에 적용, 해제 시 회수
- [ ] `EquipmentComponent`이 `PlayerCharacter`가 아닌 **ASC에만 의존** (적·NPC도 장착 가능)
- [ ] item HARNESS §2 불변 조건 위반 0

## 범위
### 포함
- slot→`ItemInstance` 장착 상태, Equip/Unequip
- 스탯 기여 배선 (모듈 → `GameplayEffect` → ASC 적용·회수) — item HARNESS 로드맵 S7에 해당
- 표현용 `ItemBehaviour` 스폰
### 제외
- 세이브·네트워크 (item HARNESS §1 스코프 밖)
- 인벤토리 보유·개수 관리 → `inventory-system` feature
- 아이템 데이터/모듈/런타임 정의 자체 → `item` feature

## 설계 개요
- **불변 조건의 진실:** [`../HARNESS.md`](../HARNESS.md) §2 (INV-1~12). 전용 하네스 없음.
- **커플링 원칙 (핵심):**
  - `EquipmentComponent`은 **ASC(스탯 능력)에만 의존**, `PlayerCharacter` 무참조. → 적·NPC 재사용 가능.
  - **소유**는 `CharacterBase`가 한다(HAS-A). 현재 `[RequireComponent(EquipmentComponent)]`가 `PlayerCharacter`에 있는데, 장착을 모든 액터로 열려면 `CharacterBase`로 올리는 게 맞음(이관점).
  - 스탯 반영은 GAS 경유: 장착 아이템의 기여를 `GameplayEffect`로 ASC에 적용, 해제 시 핸들로 회수 (기존 `possessEffect` 패턴과 동일).
  - 장착 대상은 `inventory-system`에서 `ItemInstance/ItemData`를 **받아온다** — 인벤 타입을 알 필요는 없음.
- 관련 아키텍처 요약: [`../../../../project/architecture/item-equipment.md`](../../../../project/architecture/item-equipment.md)

## 세부 TODO (구현 체크리스트)
- [~] E1 — `EquipmentComponent`을 ASC 참조 기반으로 재작성 (slot→`ItemInstance`, Equip/Unequip + `OnEquipped/Unequipped` 이벤트). **뼈대·공격 구동 코드 완료(2026-07-06), play 검증 대기.** 스탯 기여(E2)는 미포함.
- [ ] E2 — 스탯 기여 배선: 장착 시 모듈/`ItemRuntime` 기여 → `GameplayEffect`로 ASC 적용, 해제 시 핸들로 회수 (item HARNESS S7)
- [ ] E3 — `[RequireComponent(EquipmentComponent)]` 위치를 `PlayerCharacter` → `CharacterBase`로 이관 검토
- [ ] E4 — 표현용 `ItemBehaviour` 스폰 (Socket → 부위 부착 확인)

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-07-05 | 장비를 별도 feature로 격리, 현재 착수 제외 (ASC 배선은 인벤/코어 뒤) → 2026-07-06 item S1 검증 필요로 E1 최소 착수(D1 보류 해제) |
| D2 | 2026-07-05 | `EquipmentComponent`은 ASC에만 의존, `PlayerCharacter` 종속 금지 (INV-6/7/11) |
| — | 2026-07-06 | 공격 판정/데미지 적용 설계는 item decisions D7(`IWeaponAttack` 능력)·D8(EquipmentComponent 호스팅) 참조 |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음(E1 최소 착수됨). E2 스탯 기여는 per-item GE 핸들 상태 필요 → item 모듈 상태 인프라(S3/S4)와 함께 진행.

## 다음 작업 (착수 시)
1. **(진행) E1 play 검증:** item S1 에셋/씬 셋업 후 Play에서 장착·공격 확인 → E1 뼈대 검증 완료 처리.
2. E2 — 스탯 기여 → `GameplayEffect` 적용·회수 배선 (item HARNESS S7). per-item 회수 핸들을 `ItemInstance` 상태로(INV-5) — 모듈 상태 인프라(S3/S4) 선행.
3. E3 — `[RequireComponent(EquipmentComponent)]` 위치를 `PlayerCharacter` → `CharacterBase`로 이관 검토.
4. E4 — 표현용 `ItemBehaviour` 스폰 (Socket → 부위 부착).
