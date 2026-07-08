# equipment — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로. 과거 기록은 삭제·수정하지 않는다(오기 정정은 취소선).

### 2026-07-06 (2) — E1 검증 PASS + E2(스탯 기여) 부분 실현
- **E1 play 검증됨:** 장착 → 공격 동작 확인(item S1 PASS). `EquipmentComponent` 뼈대(슬롯·Equip/Unequip·이벤트·공격 구동) OK.
- **E2 부분 실현:** `StatModifierModule`(item 쪽)이 장착 시 Infinite GE를 소유자 ASC에 적용하고 해제 시 핸들로 회수 → "장착 시 스탯 기여, 해제 시 회수"(수용기준 2)를 **모듈 경로로** 달성. 회수 핸들은 per-item 상태(StatModifierState). 데모: 무기 장착 시 이동속도 −5.
- EquipmentComponent는 모듈 순회를 `ItemInstance.OnEquip(ctx)` 위임으로 단순화(모듈 lifecycle 상태 동반 = item D9).

### 2026-07-06 — E1 최소 착수 (item S1 슬라이스와 겸함, 코드 완료·play 검증 대기)
- **맥락:** item S1(Inventory→Equip→Item 체인 검증)의 "Equip" 스텝으로 `EquipmentComponent`를 최소 실배선. 정식 E1(스탯 기여·이벤트 완비)의 부분 착수 — 이번엔 장착 뼈대 + 공격 구동까지, 스탯 기여(E2/S7)는 미포함.
- **`EquipmentComponent` 재작성**(죽은코드 전량 대체): `[RequireComponent(ASC)]`, `Dictionary<SlotType, ItemInstance>` 슬롯(D3), `Equip(ItemInstance)`(IEquippable 능력 판별→슬롯 저장→모듈 `OnEquip(AbilitySystemModuleContext)`), `Unequip(SlotType)`, `TriggerAttack()`(무기 슬롯의 `IWeaponAttack` 능력→전방 OverlapSphere→타겟 ASC에 데미지 GE 적용), `OnEquipped/OnUnequipped` 이벤트.
- **D2 준수:** `PlayerCharacter` 무참조, 같은 GO의 ASC만 GetComponent → 적·NPC도 장착 가능. **D3 준수:** slot은 필드(딕셔너리).
- **미포함(의도적):** E2 스탯 기여·회수(핸들 per-item 상태 필요 → S7/E2), E3 `[RequireComponent]` CharacterBase 이관, E4 ItemBehaviour 스폰(표현). 공격 판정·데미지 적용 설계 근거는 item decisions D7·D8.
- **인벤→장비 홉:** `EquipmentTestBootstrap`(item 폴더, 임시 스캐폴드)이 Inventory에서 장비를 뽑아 `Equip` 호출. 정식 소유자 배선(inventory I2)은 아님.

### 2026-07-05
- **구현 순서 근거:** item 완성 전에 인벤/장비를 먼저 세우는 목적은 간단한 Item을 장착해 ① 장착 시 **Module 작동 여부**, ② **Socket → 부위(slot) 부착** 등을 디버깅·테스트하기 위함. (상세는 [`inventory` worklog](../inventory/worklog.md) 참조)
