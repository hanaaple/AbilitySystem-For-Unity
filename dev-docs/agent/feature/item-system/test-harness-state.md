# item-system 테스트 하네스 — 현재 상태 (S1: Inventory → Equip → Item)

> **목적:** 이 슬라이스의 씬/에셋 배선과 GUID를 한 곳에. 다음 작업 때 씬·프리팹·에셋을 재스캔하지 말고 **이 문서만 읽어** 이어간다. 상태가 바뀌면 여기부터 갱신.
> 최종 갱신: 2026-07-06 (KST)

## 재현 (play 검증)
플레이어는 **초기 무기 없음(노 item)**. SampleScene Play →
1. `ItemPickup`(구체, (0,0.5,1.5))에 걸어 들어감 → 콘솔 `[Pickup] '테스트 검' 획득 · 장착`, 픽업 소멸. (Inventory Add + Equip)
2. `TestEnemy`(큐브, (0,0,3))에 붙어 **LMB** → 적 Health 100→90 (S1 평타 검증됨 2026-07-06).
3. 장착 순간 플레이어 **이동속도 −5**(Speed 10→5) — `StatModifierModule`(Infinite GE) 효과.

## 씬 배선 (SampleScene)
- 플레이어: `SequenceManager`가 `Player.prefab` 런타임 스폰 → `PlayerController.Possess`.
- `ItemPickup` (씬 상주, 프리팹 인스턴스): 트리거 진입 시 무기 지급·장착.
- `TestEnemy` (씬 상주): Cube(BoxCollider) + ASC(`Enemy_AttributeInitData`, HP 100). (0,0.5,3).

## Player.prefab 컴포넌트
`PlayerCharacter` · `NavMeshAgent` · `CapsuleCollider` · `CharacterController` · `EquipmentComponent` · `AbilitySystemComponent`(attributeInitData=`Player_AttributeInitData`) · `InventoryComponent`(**startingItems 빈 리스트 — 노 item**)
(※ EquipmentTestBootstrap은 제거됨 — 지급·장착은 ItemPickup이 담당.)

## 에셋 GUID
| 에셋 | 경로 | GUID | fileID/type |
|---|---|---|---|
| GE_MeleeDamage | Assets/Data/GE_MeleeDamage.asset | `b9eddb2c91efef14c89b9c5e09ff29f5` | 11400000 / 2 |
| GE_EquipSpeedDown | Assets/Data/GE_EquipSpeedDown.asset | `807cf69373aeb714c800b4709266042c` | 11400000 / 2 |
| Weapon_TestSword | Assets/Data/Item/Weapon_TestSword.asset | `e58ae273b92f75947b2b9a2133165a43` | 11400000 / 2 |
| Enemy_AttributeInitData | Assets/Data/Characters/Enemy_AttributeInitData.asset | `5a8f827867c82f944aba40549245dfd3` | 11400000 / 2 |
| Player.prefab | Assets/Prefabs/Player.prefab | `d6d1ae8a0d9981b438aa4902eb54c009` | — |
| ItemPickup.prefab | Assets/Prefabs/ItemPickup.prefab | (조회 시) | — |

### 스크립트 GUID (m_Script)
- GameplayEffect `fbfbc065558b85245af777d65720a126` · AttributeInitData `ca4b1177d8884bb48e6bc94b25dd4b0c`
- EquipItem `ff10094661ade914fbc458aef7f33443` · InventoryComponent `cd39f42622756104d9d78ab5fc8d5e78` · ItemPickup `5696c1f727d6830419f1b998140dc279`
- (MeleeAttackModule·StatModifierModule은 SerializeReference → script guid 불필요, `type: {class, ns, asm}`)

## 에셋 값 (현재)
- **GE_MeleeDamage**: Instant(0), Health AddBase −10.
- **GE_EquipSpeedDown**: Infinite(1), Speed AddBase −5.
- **Weapon_TestSword**: EquipItem, slot=Weapon. modules = [`MeleeAttackModule`{damageEffect→GE_MeleeDamage, range 2}, `StatModifierModule`{effect→GE_EquipSpeedDown}].
- **Enemy_AttributeInitData**: Health/MaxHealth 100.
- **ItemPickup.prefab**: Sphere + Rigidbody(kinematic) + SphereCollider(trigger) + `ItemPickup`{item→Weapon_TestSword}.

## 편집 규칙
YAML 직접 편집 일반 규칙·형식은 → [dev-tools.md](../../../project/dev-tools.md). GE 에셋 오소링은 → [gameplay-effect.md](../../../project/architecture/ability-system/gameplay-effect.md) "에셋 생성·세팅".
이 슬라이스에서 YAML로 배선한 것: 무기 modules(SerializeReference 2개), GE modifier, 적 ASC attributeInitData(씬), 프리팹 오브젝트 참조(InventoryComponent 시드→해제, ItemPickup.item).

## 정합성 메모
- 오버랩 판정: `EquipmentComponent.TriggerAttack`이 `transform.position + forward*1` 기준 `OverlapSphereNonAlloc(range)` → hit의 `GetComponentInParent<AbilitySystemComponent>()`(자신 제외)에 데미지 GE 적용.
- 모듈 lifecycle: `ItemInstance.OnEquip/OnUnEquip(ctx)`가 모듈+상태를 정렬 구동(INV-5). StatModifierModule은 회수 핸들을 `StatModifierState`(per-item)에 보유.
