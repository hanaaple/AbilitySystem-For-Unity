# AbilitySystem — Attribute

캐릭터 수치(체력·스태미나·이동속도 등)를 담고 읽고/쓰는 계층. 상위 개요는 [overview](overview.md).

> UE GAS의 `UAttributeSet`/`FGameplayAttribute`를 참고해 필요한 축만 재구현 — UE 대비는 [overview §UE GAS 대비](overview.md#ue-gas-대비--채택생략).
> **개념·방향** 중심. 필드·선언 규약 등 세부는 코드를 본다.

## 수치 저장

- **`AttributeSet`** — 빈 추상 마커. 구체 Set이 값을 필드로 보유하고, ASC는 타입당 하나만 등록한다(도메인별로 필요한 Set만 붙이고, 필드가 컴파일타임에 존재해 오타·타입 오류를 막는다). 예: 캐릭터 수치 Set, 전투 수치 Set.
- **`AttributeData`** — 한 수치의 `BaseValue`/`CurrentValue` 쌍. Base는 영구값, Current는 보정 반영값.

## 참조 2분할 — 왜 나누나

한 수치를 "어느 Set의 어느 필드"로 가리키는 참조를 **직렬화용과 런타임용 둘로 나눈다.** 근본 원인은 **C#이 `FieldInfo`를 직렬화하지 못하기 때문**(UE는 `FGameplayAttribute` 하나가 직렬화+런타임을 겸한다).

- **`GameplayAttribute`(직렬화):** 타입명(AQN) + 필드명 문자열 쌍. 에셋·인스펙터 저작용.
- **`GameplayAttributeHandle`(런타임):** 생성 시 `FieldInfo`를 캐싱하는 불변 struct. 이후 읽기/쓰기에 **string 탐색이 없다.** 값 동등성으로 Dictionary 키가 된다. `GameplayAttribute`를 해석해 만든다.

## 초기화 (ScriptableObject)

초기값을 코드가 아니라 데이터로 준다 — 밸런싱을 코드 수정 없이 조정하기 위해. `AttributeDefinitionAsset`(SO)이 "어느 Set의 어느 필드에 어떤 초기값"을 담고, ASC 초기화 시 Reflection으로 Set 인스턴스를 만들어 값을 세팅한다. 에디터 드로어가 Set·필드·값 배선을 돕는다.

## 값 접근

ASC를 통해 Base/Current를 읽고 Base를 쓴다.

```
읽기  Base    → AttributeData (진실)
      Current → Aggregator.Evaluate (mod 집계)   ← aggregator 없으면 mod 없음 = base와 동일
쓰기  Base    → SetAttributeBaseValue
                 ├ AttributeData 갱신 (진실)
                 └ aggregator 있으면 동기화 → dirty → Current 재계산
```

Base는 `AttributeData`가 진실이고, aggregator의 base는 동기화 사본이다(→ [aggregator](aggregator.md)). `AttributeSet`은 소유 ASC를 알고(등록 시 주입) 편의 세터를 ASC로 위임하며, 값 변경 전후 훅(Pre/PostAttributeBaseChange — 클램프·파생 계산 자리)을 제공한다.

> Modifier 연산과 Base/Current가 어떻게 갈리는지는 [gameplay-effect](gameplay-effect.md) 참조.
