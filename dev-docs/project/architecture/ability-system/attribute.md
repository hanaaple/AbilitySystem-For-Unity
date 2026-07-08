# AbilitySystem — Attribute

캐릭터 수치(체력·스태미나·데미지 등)를 담고 읽고/쓰는 계층. 상위 개요는 [overview](overview.md).

> UE GAS의 `UAttributeSet`/`FGameplayAttribute`를 참고해 **필요한 축만 직접 구현**한 것 — UE 대응·축소 지점은 [overview §UE GAS 대비](overview.md#ue-gas-대비--채택생략-범위) 표 참조.
> **반영 기준:** feature `ability-system/attribute` @ 2026-07-05 20:18 (KST) — HARNESS §3.4.

## Attribute 계층

- `AttributeSet`은 빈 추상 클래스(마커). 구체 Set이 `public AttributeData` 필드로 수치를 보유한다.
- `AttributeHandle` — 특정 Set의 특정 필드를 가리키는 불변 struct. 생성 시 `FieldInfo`를 캐싱하므로 **런타임 읽기/쓰기에 string 탐색이 없다**. `SetType + Name` 기반 `IEquatable`(Dictionary 키 사용).
- `AttributeData`(struct) — `BaseValue` / `CurrentValue` 쌍.
- 선언 규약: 구체 Set에 `public static readonly AttributeHandle Health = new(typeof(...), nameof(health))`. 필드명은 camelCase(`health`), 핸들명은 PascalCase(`Health`).
- 현재 어트리뷰트: `CharacterAttributeSet`(health/maxHealth/stamina/maxStamina/speed), `CombatAttributeSet`(damage).

## ScriptableObject 초기화

`AttributeInitData`(SO) → `AttributeSetInitData[]` → `AttributeFieldInitData`(fieldName + BaseValue).
`ASC.Awake()`가 Reflection(`Activator.CreateInstance` + `FieldInfo.SetValue`)으로 AttributeSet 인스턴스를 생성하고 BaseValue를 세팅한다. 에디터 배선은 `AttributeInitDataDrawer` / `AttributeSetInitDataDrawer` / `AttributeReflectionUtility`(Editor)가 담당.

## ASC — Attribute API

| 메서드 | 용도 |
|---|---|
| `AddAttributeSet(set)` / `RemoveAttributeSet(set)` | AttributeSet 등록/해제 (같은 타입 중복 불가) |
| `GetAttributeBaseValue(handle)` | BaseValue 읽기 |
| `GetAttributeCurrentValue(handle)` | CurrentValue 읽기 (캐시값, O(1)) |
| `SetBaseAttributeValue(handle, value)` | BaseValue 직접 설정 → CurrentValue 재계산 |

> BaseValue와 CurrentValue가 어떻게 갈라지는지(Modifier 연산·읽기 캐시)는 [gameplay-effect](gameplay-effect.md) 참조.
