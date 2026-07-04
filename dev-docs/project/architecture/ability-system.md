# AbilitySystem (GAS-like)

`Assets/Scripts/Core/AbilitySystem/`

캐릭터 수치 관리를 위한 경량 Ability System. UE5 GAS 개념을 Unity에 맞게 축소 적용.

## 핵심 구조

```
AbilitySystemComponent (MonoBehaviour)
├── Dictionary<Type, AttributeSet>       ← Type당 하나만 등록 가능
└── Dictionary<AGEHandle, ActiveGE>      ← 활성 이펙트 목록

AttributeSet (abstract)                  ← 수치 컨테이너
├── CharacterAttributeSet               — health, maxHealth, stamina, maxStamina, speed
└── CombatAttributeSet                  — Damage

AttributeHandle (struct)                 ← 경량 식별자 (SetType + fieldName + FieldInfo 캐싱)
AttributeData (struct)                   ← BaseValue / CurrentValue 쌍

GameplayEffect (ScriptableObject)        ← 불변 정의
GameplayEffectSpec                       ← 런타임 인스턴스 (Level·Context 스냅샷)
ActiveGameplayEffect                     ← 활성 상태 (Handle, RemainingDuration, PeriodTimer)
```

## AttributeHandle 규칙

- `CharacterAttributeSet`에 `static readonly AttributeHandle Health = new(typeof(...), nameof(...))` 형태로 선언
- Reflection 비용은 생성 시점 1회뿐 — 런타임 읽기/쓰기는 캐싱된 `FieldInfo` 사용
- 필드명은 camelCase(`health`), 핸들명은 PascalCase(`Health`)

## AbilitySystemComponent 주요 API

| 메서드 | 용도 |
|---|---|
| `AddAttributeSet(set)` | AttributeSet 등록 (같은 타입 중복 불가) |
| `GetAttributeBaseValue(handle)` | BaseValue 읽기 |
| `GetAttributeCurrentValue(handle)` | CurrentValue 읽기 (모디파이어 누산 후, 캐싱값) |
| `SetBaseAttributeValue(handle, float)` | BaseValue 직접 설정 → CurrentValue 재계산 |
| `ApplyGameplayEffectToSelf(GE, ctx, level)` | GE 적용. Instant는 즉시 실행 후 Invalid Handle 반환 |
| `RemoveActiveGameplayEffect(handle)` | 핸들로 Duration/Infinite GE 해제 |

## GameplayEffect 타입

- `Instant` — BaseValue 즉시 변경 후 종료. Active Effect 등록 없음.
- `Duration` — 지정 시간 후 자동 해제. Period 설정 시 주기마다 BaseValue 직접 변경.
- `Infinite` — 명시적 Remove 전까지 CurrentValue에 지속 반영.

## GameplayEffectExecution (TODO: concrete 서브클래스)

추상 클래스·ASC 호출 프레임워크는 구현됨. 단순 Modifier로 표현 불가한 복합 계산(방어력 관통, 크리티컬 배율 등)을 서브클래스 `Execute()` 오버라이드로 처리. 아직 실제 서브클래스 없음.

## ScriptableObject 초기화

`AttributeInitData` (SO) → `AttributeSetInitData[]` → `AttributeFieldInitData` (fieldName + BaseValue)  
`AbilitySystemComponent.Awake()`에서 Reflection으로 AttributeSet 인스턴스 생성 및 BaseValue 세팅.