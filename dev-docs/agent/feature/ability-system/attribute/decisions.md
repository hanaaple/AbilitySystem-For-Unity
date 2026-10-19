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

## D2 — `AttributeSet`은 빈 추상 베이스, 수치는 구체 Set의 `AttributeData` 필드

- 날짜: 2026-05-26
- 결정: 구체 Set(`CharacterAttributeSet`·`CombatAttributeSet`)이 `public AttributeData` 필드로 수치를 들고, ASC는 Set을 타입당 하나 등록한다. UE `UAttributeSet` 개념.
- 왜: 도메인별로 Set을 나누면 필요한 Set만 붙이고(상자엔 Combat만 등), 필드가 컴파일 타임에 있어 오타·타입 오류를 막는다.
- 대안: 모든 수치를 한 `Dictionary<string, float>`에(유연 / 문자열 키 블랙보드).

## D3 — 초기값은 SO(`AttributeDefinitionAsset`) + 리플렉션으로 채운다 (UE와 다름)

- 날짜: 2026-05-26
- 결정: ASC가 정의 SO를 읽어 `Activator.CreateInstance`로 Set을 만들고 `FieldInfo.SetValue`로 초기값을 넣는다.
- 왜: 밸런싱 값을 코드 수정 없이 데이터로 바꿀 수 있어야 하고, 같은 초기화 코드가 어떤 Set에도 통해야 한다.
- 대안: 코드·인스펙터에 초기값 하드코딩(단순 / 값마다 코드 수정).
- 대가: UE는 GE·CurveTable로 초기값을 주지만 여기선 전용 SO로 줄였다. 초기화 때 리플렉션 1회.

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
- **D5** (2026-08-21) — 런타임 핸들을 `ResolvedGameplayAttribute`로 리네임. 이름은 이후 `GameplayAttributeHandle`로 되돌아갔고, 직렬화/런타임 분리 자체는 D1·gameplay-effect D15에 있다.
