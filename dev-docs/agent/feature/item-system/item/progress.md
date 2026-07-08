# item — 아이템 코어 (데이터·인스턴스·모듈·런타임)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-07-07 (KST)

> **구조(2026-07-05):** `item-system` 그룹의 하위 feature다 — 이 `item`(코어) · [`inventory`](../inventory/progress.md) · [`equipment`](../equipment/progress.md)(미착수·제외). 공유 불변조건은 그룹 폴더의 [`HARNESS.md`](../HARNESS.md)이며 세 하위가 모두 이를 따른다.

## 목표
아이템(무기·방어구)이 모듈 조합으로 동작을 획득하고, 장착 시 캐릭터 전투 루프에 반영되는 수직 슬라이스를 구현한다. 최종적으로 **코드 추가 없이 에셋 조합만으로** 신규 온히트 효과를 만들 수 있는 상태가 목표.

## 수용 기준 (Definition of Done)
- [ ] feature 전용 하네스 [HARNESS.md](../HARNESS.md) §7 로드맵 S1~S10 슬라이스 전부 ☑
- [ ] 각 슬라이스가 HARNESS §4 3단 검증(V1 정적 / V2 기능 / V3 회귀) 통과
- [ ] HARNESS §2 불변 조건(INV-1~12) 위반 0

## 범위
### 포함
- ItemData(SO) / ItemInstance / ItemModule / ItemBehaviour 조합 구조
- 근접·총기 Strategy, 온히트 모듈, 콤보·패링, 기여(스탯) 배선, 저작 툴링
### 제외 (명시적으로 하지 않을 것)
- 등급 / 인벤토리 / 개수 / 네트워크 / 세이브 (HARNESS §1 스코프 밖)

## 설계 개요
- **구현 방향·불변 조건·검증·로드맵의 단일 진실:** 그룹 폴더의 [HARNESS.md](../HARNESS.md) (item-system 그룹 공유 하네스).
- 아키텍처 문서: [item-equipment.md](../../../../project/architecture/item-equipment.md)
- 코드: `Assets/Scripts/Core/ItemSystem/`

## 세부 TODO (구현 체크리스트)
이 feature의 구현 backlog는 전용 하네스의 로드맵이 진실이다 — 여기에 중복 나열하지 않는다.
→ [`../HARNESS.md`](../HARNESS.md) §7 **S1~S10 슬라이스**. 각 슬라이스 완료 시 그쪽에서 ☑.

## 결정 기록
상세(맥락·대안·근거·트레이드오프·재평가 트리거)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-07-04 | feature 전용 하네스를 progress와 분리 배치 |
| D2 | 2026-07-04 | 설계 권위 = Item HARNESS 우선 (상속 금지·POCO·모듈 조합, INV-2/3/4) |
| D3 | 2026-07-04 | 장착 부위(slot) = 필드 (Module 아님) — 뒤집는 트리거: 복수 부위 점유 |
| D4 | 2026-07-04 | 아이템 종류(category) = `ItemData` 서브클래스(EquipItem/ConsumeItem), 시스템은 인터페이스로 판별(INV-11) |
| D5 | 2026-07-04 | 능력 인터페이스는 소비 시스템 있을 때만 정의 (지금 IEquippable만) |
| D6 | 2026-07-05 | 아이템 전용 자유형 로직 = `ItemRuntime`(합성 멤버), 첫 트리거까지 보류 |
| D7 | 2026-07-06 | 공격 = 능력 인터페이스 `IWeaponAttack`(데미지 GE+사거리)로 선언, 시스템은 구체 모듈 아닌 능력으로 요구(INV-11) |
| D8 | 2026-07-06 | 공격 씬 판정·데미지 적용은 EquipmentComponent가 호스팅(모듈은 결정만), 대상 적용=대상 ASC.ApplyGameplayEffectToSelf (신규 API 없음) |
| D9 | 2026-07-06 | 모듈 lifecycle이 상태 동반(`OnEquip(ctx, state)`), 순회는 ItemInstance, 캐스트는 제네릭 베이스 1곳(INV-5). StatModifierModule 회수 핸들=StatModifierState. S3/S4 부분 선착수 |
| D10 | 2026-07-07 | 장착 표현(프리팹 소켓 소환)을 `EquipmentComponent`가 겸함(별도 컴포넌트 분리 기각·삭제 — 발견성). 소켓 맵 필드 + Equip/Unequip에서 소환/파괴. ItemBehaviour=표현 앵커(구체, 로직0, Instance+Bind). `CreateBehaviourRuntime` 제거. + D2 취지 정정(지속성) |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음. (직전 [설계 충돌]은 Item HARNESS 우선으로 해소.)
- ~~기존 상속 기반 코드/서술 INV-2/3/4 위반 소지 → 이관 대상~~ **해소(2026-07-06):** 상속 기반 구코드(`WeaponInstance`·`ItemBase` 등)는 이미 삭제됨. INV-2/3/4 자가점검 완료 — 능동 위반 0(`ItemInstance` 상속 클래스 0, Module의 씬/GameObject 접근 0, `ItemBehaviour` 로직 0). 경미 스멜: `TestItemData`(테스트 스캐폴드)·`EquipmentComponent` 죽은코드 — S1에서 실 아이템으로 대체하며 정리.

## 다음 작업
1. **(코드 완료·play 검증 대기) 장착 표현 슬라이스(D10, 2026-07-07):** `ItemBehaviour`(구체 표현 앵커) + `EquipmentComponent`가 소켓 소환 겸함(별도 컴포넌트 병합). **유저 Unity 배선 남음** → (a) 무기 표현 프리팹 생성(메시+`ItemBehaviour`) 후 `Weapon_TestSword.prefab`에 할당, (b) `Player.prefab`에 소켓 Transform(예: `PlayerCharacter.model` 하위 `WeaponSocket`) 만들어 기존 **`EquipmentComponent`의 sockets 필드**에 Weapon→그 Transform 매핑. 검증: 픽업 장착 시 손에 프리팹 뜸 / 해제 시 사라짐. 배선 후 [test-harness-state.md](../../test-harness-state.md)에 프리팹·소켓·GUID 반영.
2. **(완료) S1 play 검증 PASS(2026-07-06).** + 모듈 상태 lifecycle(D9, S3/S4 부분)·`StatModifierModule`(장착 시 Speed −5)·`ItemPickup` 프리팹까지.
3. **S2 — WeaponData + 근접 Strategy:** 무기 에셋 교체로 데미지·범위가 달라지게. (다음 초점 후보)
4. **S3/S4 마무리:** lifecycle 기반은 섰으니, "발화 온히트 모듈"(S3)·"차지샷 상태"(S4) 실제 예제로 완결.
