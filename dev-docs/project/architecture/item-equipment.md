# ItemSystem / EquipmentSystem

`Assets/Scripts/Core/ItemSystem/` · `Assets/Scripts/Item/`

> # ⚠ 설계 미정 (2026-07-13)
>
> **이 시스템의 설계는 확정된 것이 없다.** 아이템의 구조 — `ItemModule` · `ItemInstance` · `ItemRuntime`,
> "모듈 조합" · "아이템별 상속 금지" · 무상태 경계 등 — 은 **전부 재검토 중이며 권위가 없다.**
> 이전 문서가 이것들을 확정 설계로 서술했으나(모듈 조합이 단일 진실, INV-1~12 등), **그 서술은 폐기했다.**
>
> - 다른 문서·코드 주석이 위 개념을 확정된 규약처럼 인용하더라도 **구속력 없음.**
> - 논의 중인 후보와 미결 쟁점: [`agent/feature/item-system/design-draft.md`](../../agent/feature/item-system/design-draft.md)
> - 결론이 서면 그때 이 문서를 아키텍처 요약으로 다시 쓴다. **그 전까지 이 문서는 "지금 코드에 무엇이 있는가"만 기술한다.**

---

## 현재 코드에 존재하는 것 (사실 — 설계 주장 아님)

아래는 지금 리포지토리에 실제로 있는 타입들이다. **이 배치가 옳다는 뜻이 아니다.** 재검토 대상이다.

- `ItemDataAsset` (SO) — 아이템 템플릿. 모듈 리스트와 표현 프리팹을 담고 있다.
- `ItemModule` — SO에 `[SerializeReference]`로 담기는 동작/데이터 단위. 현재 `StatModifier` · `MeleeAttack` · `GunAttack` 등.
- `ItemInstance` (POCO) — 장착 시 생성되는 런타임 개체. 모듈별 상태 배열과 `ItemRuntime`을 보유.
- `ItemRuntime` — 클래스와 lifecycle 훅만 있고 **로직은 비어 있다.**
- `ItemBehaviour` (MonoBehaviour) — 장착 시 소켓에 소환되는 표현 오브젝트를 인스턴스와 연결.
- `EquipmentComponent` — 슬롯별 장착 상태 + 소켓에 프리팹 부착.
- `WeaponAttackComponent` — 장착 무기의 모듈을 능력 인터페이스로 찾아 **판정(오버랩/레이)과 데미지 GE 적용을 직접 수행**하는 전투 드라이버.

### 알려진 결함 (설계 결론 대기 중이라 방치)

- `ModuleContext`가 **빈 클래스**다. 그 결과 `EquipmentComponent`의 장착/해제 시 **모듈 lifecycle 호출이 주석 처리**돼 있고,
  `StatModifierModule`(장착 중 이속 −5)이 **동작하지 않는다.** 씬/ASC 접근 경로를 어떻게 줄지가 미결이기 때문이다.
- 적/아군 판별이 없다. 현재는 "자기 자신이 아니면 때린다"로 임시 동작한다.

## 코드 위치

- **코어:** `Assets/Scripts/Core/ItemSystem/` — `ItemDataAsset` · `ItemInstance` · `ItemRuntime` · `ItemBehaviour` · `Module/ItemModule` · `Module/ModuleContext`
- **장비:** `Assets/Scripts/Core/ItemSystem/Equipment/` — `EquipmentComponent`. 장착 계약(`IEquippable` · `SlotType`)은 카테고리 SO 옆(`EquipItemAsset.cs`).
- **모듈·게임 구현부:** `Assets/Scripts/Item/` — `Module/`(StatModifier · MeleeAttack · GunAttack), `WeaponAttackComponent`, `ItemPickup`.
  (Core는 메인 시스템만, 소비 구현부는 게임 레이어에 — CODE_CONVENTION "네임스페이스·폴더 구조".)
