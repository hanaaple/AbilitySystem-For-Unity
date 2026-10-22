# attribute — 결정 기록

> 무엇을·어떻게 적나 → [feature/HARNESS.md](../../HARNESS.md) '결정 기록 기준'. `D#`는 feature 내 고유·불변 코드다.
> 경위는 [worklog](worklog.md), 2026-10-18 정리 이전의 표 본문 전체는 git `ca110d2`. UE 대비 채택·생략 범위는 [architecture overview](../../../../project/architecture/ability-system/overview.md).

---

## D1 — 런타임 어트리뷰트 핸들은 `FieldInfo`를 생성 때 한 번 캐시하는 불변 struct

- 날짜: 2026-05-26
- 결정: `GameplayAttributeHandle`(readonly struct)이 만들어질 때 `FieldInfo`를 찾아 들고, `SetType + 필드 이름`으로 같음을 판정해 Dictionary 키로 쓴다.
- 왜: 수치 읽기는 매 프레임 많이 일어나 접근 경로에 문자열 탐색이 있으면 비용이 쌓인다. 리플렉션을 생성 1회로 가둔다.
- 대안: 문자열 키 Dictionary(단순 / 매 접근 문자열 조회), enum 키 + switch(빠름 / Set을 늘릴 때마다 손으로 갱신).
- 대가: UE `FGameplayAttribute`는 `FProperty`로 필드를 가리키지만 Unity엔 그 기반이 없어, 직렬화 타입(`GameplayAttribute`, gameplay-effect D15)과 런타임 핸들이 둘로 나뉜다.

## D2 — `AttributeSet`은 추상 베이스, 수치는 구체 Set의 `AttributeData` 필드

- 날짜: 2026-05-26
- 결정: 구체 Set(`CharacterAttributeSet`·`CombatAttributeSet`)이 `public AttributeData` 필드로 수치를 들고, ASC는 Set을 타입당 하나 등록한다. UE `UAttributeSet` 개념.
- 왜: 도메인별로 Set을 나누면 필요한 Set만 붙이고(상자엔 Combat만 등), 필드가 컴파일 타임에 있어 오타·타입 오류를 막는다.
- 대안: 모든 수치를 한 `Dictionary<string, float>`에(유연 / 문자열 키 블랙보드).

## D7 — Set은 소유 쪽이 만들어 등록하고, 초기값은 Instant GE(Override)로 준다

- 날짜: 2026-10-21
- 결정: 소유 컴포넌트(`PlayerCharacter`·`TrainingDummy` 등)가 `Awake`에 Set을 생성해 `AddSpawnedAttribute`, `Start`에 초기 스탯 Instant GE(Override)를 자기에게 적용. ASC 초기값 에셋(`attributeInitData`·`AddSet`·`AttributeDefinitionAsset` 계열)은 삭제.
- 왜: 유저 "임시 코드니까 빼고 정식적으로 UE에서 하는거처럼 해", "UE에서 보통은 어떻게 하는데? 그거에 맞춰서 해". UE: Owner 서브오브젝트를 ASC가 수집(`AbilitySystemComponent_Abilities.cpp:62-83`), ASC `DefaultStartingData`는 `Category="AttributeTest"`(`AbilitySystemComponent.h:196-197`).
- 대안: ASC 인스펙터에 Set 타입 목록(코드 없이 프리팹에서 고름 / UE에 없는 방식).
- 대가: Set 종류가 소유 코드에 고정. 초기값이 GE 경로라 Set 훅(클램프)을 거치고 이벤트가 난다 — 모디파이어 순서에 의존(maxHealth를 health보다 먼저).

## D6 — `AttributeSet`이 소유 ASC를 알고, Set의 쓰기 접근자는 ASC로 위임

- 날짜: 2026-08-22
- 결정: `AttributeSet`에 소유 ASC 참조를 두고(`SetOwner`, ASC 등록 때 1곳에서 주입), `AttributeSet.SetBaseValue(handle, value)`가 ASC `SetAttributeBaseValue`로 위임한다.
- 왜: UE `ATTRIBUTE_ACCESSORS`의 `SetX`도 `GetOwningAbilitySystemComponent()->SetNumericAttributeBase`로 ASC에 위임한다 — "Set에 둘까 ASC로만 할까"가 아니라 얼굴은 Set, 쓰기는 ASC.
- 대안: back-ref 없는 generic 세터(호출부가 못생김), ASC 경유만(어트리뷰트 식별을 호출부가 늘 들고 다님), 타입별 이름(`SetHealth`)을 Source Generator로 생성(UE와 같은 모양 / 제너레이터 유지비).
- 대가: Set ↔ ASC 순환 참조(소유 관계상 자연스러움).
- 재검토: 타입별 이름 접근자가 필요해지면 Source Generator.

## 치운 결정

중요하지 않거나 코드가 이미 바뀐 결정이라 본문을 치웠다(유저 2026-10-18). 코드는 재사용하지 않는다.

- **D4** (2026-08-07) — 타입 선택 UI의 "New Script..."(서브클래스 .cs 생성 후 자동 배정). 에디터 편의 도구.
- **D3** (2026-05-26) — 초기값은 SO(`AttributeDefinitionAsset`) + 리플렉션. 2026-10-21 D7로 대체(코드 삭제).
- **D5** (2026-08-21) — 런타임 핸들을 `ResolvedGameplayAttribute`로 리네임. 이름은 이후 `GameplayAttributeHandle`로 바뀌었고, 직렬화/런타임 분리 자체는 D1·gameplay-effect D15에 있다.
