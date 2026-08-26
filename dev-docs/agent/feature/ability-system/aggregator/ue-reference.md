# UE `FAggregator` 참고 — 개념·구조·평가 흐름

> **성격:** UE GAS의 `FAggregator`(어트리뷰트 집계 객체) 동작을 정리한 **참고 자료**다. 우리 프로젝트의 설계 결정이 아니며, 채택/생략 판단은 여기에 적지 않는다(그건 [decisions.md](decisions.md)·[architecture/…/aggregator.md](../../../../project/architecture/ability-system/aggregator.md)). aggregator 재설계(→ progress D5 — 어트리뷰트별·채널별 Aggregator 이식)의 참조 모델로 둔다.
>
> **출처·신뢰도:** 유저가 제공한 UE 분석을 정리한 것이다. 원문에는 항목별 인용(공식 API·GAS 문서·이슈 트래커)이 달려 있었으나 링크 자체는 포함되지 않았다. 따라서 이 문서의 서술은 **2차 자료 수준**으로 다루고, 결론이 걸리는 지점은 UE 원문(ylyking 미러·Epic 문서)으로 재확인한다(HARNESS '판단은 틀릴 수 있다'·'왜의 근거는 유저 의도 → UE 원본'). 엔진 버전에 따라 세부 멤버·구현은 달라질 수 있다.
>
> 최종 갱신: 2026-08-10 (KST)

---

## 1. 개념 — `FAggregator`란

특정 `FGameplayAttribute`에 적용된 여러 GameplayEffect Modifier를 모아, **BaseValue로부터 CurrentValue를 재계산**하는 런타임 집계 객체. 공식 API 설명상 내부 BaseValue와 평가 파라미터로 값을 계산한다.

```
Attribute
 └─ FGameplayAttributeData
     ├─ BaseValue
     └─ CurrentValue

FAggregator
 ├─ BaseValue
 ├─ ModChannels
 │   ├─ Add
 │   ├─ Multiply
 │   ├─ Divide
 │   └─ Override
 └─ EvaluationMetaData
```

핵심:

```
CurrentValue = Aggregator.Evaluate(EvaluateParameters)
```

일반적인 Modifier 계산 순서:

```
CurrentValue = (BaseValue + Additive) * Multiplicative / Division
```

### BaseValue vs CurrentValue

이동 속도 예:

```
BaseValue = 600
GE_Sprint : Add 200
GE_Slow   : Multiply 0.5

CurrentValue = (600 + 200) * 0.5 = 400
```

Effect가 제거되면 다시 계산 → `CurrentValue = 600`.

중요: **Duration/Infinite GameplayEffect는 보통 BaseValue를 직접 바꾸지 않는다.**

| GameplayEffect 유형 | 주로 변경되는 값 |
|---|---|
| Instant | BaseValue에 영구 반영 |
| Duration | Aggregator를 통해 CurrentValue 계산 |
| Infinite | Aggregator를 통해 CurrentValue 계산 |
| Periodic | Instant처럼 BaseValue에 반영 |

→ 버프 만료 시 원복이 필요하면 Duration/Infinite, 영구 증가(체력·경험치)는 Instant.

---

## 2. 내부 구성

개념적 데이터(버전별 상이 가능):

```cpp
struct FAggregator
{
    float BaseValue;
    TArray<FAggregatorModChannel> ModChannels;
    const FAggregatorEvaluateMetaData* EvaluationMetaData;
};
```

- **BaseValue**: 집계 시작값
- **ModChannels**: Modifier를 Evaluation Channel별 보관
- **EvaluationMetaData**: 어떤 Modifier를 평가에 포함할지 결정
- **Dirty 상태**: Modifier 추가·삭제 시 재계산 필요 여부 관리

각 Modifier:

```cpp
struct FAggregatorMod
{
    float EvaluatedMagnitude;
    EGameplayModOp::Type Op;
    FGameplayTagRequirements Qualifier;
    bool Qualifies;
};
```

`FAggregatorMod::UpdateQualifies()`가 평가 파라미터로 조건 충족 여부를 갱신하고, **조건을 만족하는 Modifier만** 최종 계산에 들어간다.

---

## 3. 평가 흐름

```
1. FAggregator.Evaluate()
2. EvaluationMetaData로 적용 가능한 Modifier 필터링
3. Modifier의 Tag Requirement 검사
4. Evaluation Channel별 계산
5. Add → 6. Multiply → 7. Divide → 8. Override 처리
9. 최종 CurrentValue 반환
```

의사 코드:

```cpp
float FAggregator::Evaluate(const FAggregatorEvaluateParameters& Parameters) const
{
    float Result = BaseValue;
    for (const FAggregatorModChannel& Channel : ModChannels)
        Result = Channel.EvaluateWithBase(Result, Parameters);
    return Result;
}
```

실제 구현은 EvaluationMetaData, Prediction Modifier, Tag Filter, Ignore Handle 등을 함께 고려한다. `FAggregatorEvaluateParameters`에는 SourceTags, TargetTags, Source/Target Tag Filter, 무시할 Effect Handle, Predictive Modifier 포함 여부 등이 들어간다.

### Modifier 연산별

- **Add**: `Result = BaseValue + 유효 Add 합` (음수 = 감소). 예: 100 +20 +30 = 150
- **Multiply**: `Result *= 유효 Multiply 곱`. 예: 100 * 1.2 * 0.5 = 60
- **Divide**: `Result /= 유효 Divide 곱`. 예: 100 / 2 = 50
- **Override**: `Result = OverrideValue` (기존 결과 대체)

> Override가 여럿 공존할 때 최종 승자에 의존하는 설계는 피한다. 우선순위가 필요하면 별도 Attribute·Tag·MMC·커스텀 평가로 명시 처리.

---

## 4. Tag와 Qualification

Modifier는 배열에 존재한다고 항상 계산되는 게 아니다. GE에 조건이 있으면(예: Required Source Tag `State.Berserk`, Ignored Target Tag `State.Dead`), 평가 시 파라미터에 태그를 넘겨야 한다.

```cpp
FAggregatorEvaluateParameters Params;
Params.SourceTags = SourceTags;
Params.TargetTags = TargetTags;
```

조건 미충족 시 해당 Modifier의 `Qualifies = false` → 계산 제외. `SourceTags`/`TargetTags`는 각각 Effect Source/Target에서 집계된 태그를 전달한다.

> **버전 이슈:** 특정 UE 버전에서 Duration/Infinite Effect 재평가 시 `OnAttributeAggregatorDirty()`가 Source/Target 태그를 제대로 전달하지 않아 Required/Ignore 태그가 무시되는 이슈가 보고된 적 있다(UE-207719 — 유저 분석 기준 UE 5.3.2 영향, 수정 목표 5.7). 태그 조건이 기대대로 안 먹으면 ① UE 버전, ② Modifier Tag Req vs GE 자체 Tag Req, ③ 재평가 경로에서 SourceTags/TargetTags 전달 여부, ④ 파라미터 직접 구성 시 태그 설정 여부를 확인. *(버전·이슈 번호는 유저 분석 원문 기준 — 재확인 필요.)*

---

## 5. EvaluationMetaData — 평가 정책

"모든 Modifier를 어떻게 평가할 것인가"를 정의하는 정책. Aggregator 생성 시점 콜백에서 설정한다.

```cpp
void UMyAttributeSet::OnAttributeAggregatorCreated(
    const FGameplayAttribute& Attribute, FAggregator* NewAggregator) const
{
    Super::OnAttributeAggregatorCreated(Attribute, NewAggregator);
    if (!NewAggregator) return;
    if (Attribute == GetMoveSpeedAttribute())
        NewAggregator->EvaluationMetaData =
            &FAggregatorEvaluateMetaDataLibrary::MostNegativeMod_AllPositiveMods;
}
```

`MostNegativeMod_AllPositiveMods` 정책: **양의 Modifier는 모두 허용, 음의 Modifier는 가장 큰 음수 하나만 허용.**

```
BaseValue = 600, Slow A = -100, Slow B = -200, Slow C = -50, Buff = +100

모두 허용:          600 -100 -200 -50 +100 = 350
MostNegative 정책:  600 -200 +100         = 500
```

→ 감속 중첩으로 이동 속도가 과도하게 낮아지는 것을 방지.

---

## 6. Dirty 처리 & Attribute 변경

```
GE 추가 → Aggregator에 Modifier 추가 → Dirty → 재평가 → CurrentValue 갱신 → Change Delegate
GE 제거 → Aggregator에서 Modifier 제거 → Dirty → 남은 Modifier로 재평가 → CurrentValue 갱신
```

Duration Effect 만료는 **BaseValue 복원이 아니라** 해당 Modifier 제거 후 남은 Modifier로 재계산하는 과정이다. Instant Effect가 BaseValue를 바꾸면, Aggregator가 있으면 새 BaseValue로 CurrentValue를 재계산한다.

### Dirty 콜백의 방향 (Aggregator → ASC → AttributeSet)

```
FAggregator.OnDirty Delegate
   → UAbilitySystemComponent::OnAttributeAggregatorDirty()
   → FActiveGameplayEffectsContainer::InternalUpdateNumericalAttribute()
   → FGameplayAttribute::SetNumericValueChecked()
   → FGameplayAttributeData::CurrentValue 변경
```

Aggregator가 직접 AttributeSet을 찾아 갱신하는 게 아니라(X), **Dirty delegate로 ASC가 갱신을 처리한다(O).** 그래서 Aggregator는 ASC 수명에 묶이고, AttributeSet은 변경 결과를 전달받는 쪽에 가깝다.

---

## 7. `PreAttributeChange` vs `PreAttributeBaseChange`

| 함수 | 시점 | 용도 |
|---|---|---|
| `PreAttributeChange` | CurrentValue 변경 직전 | Aggregator가 계산한 **CurrentValue 클램프** |
| `PreAttributeBaseChange` | BaseValue 변경 직전 | 영구 **BaseValue 클램프** |
| `PostGameplayEffectExecute` | Instant GE 실행 후 | 실제 피해·회복 결과 처리 |

---

## 8. Derived Attribute

`MaxHealth = BaseHealth + Vigor * 10` 같은 파생값도 Aggregator로 구현. Infinite GE의 Attribute-Based Modifier(또는 MMC)로 구성하면 Vigor가 바뀔 때 MaxHealth가 재평가된다.

```
MaxHealth Aggregator
    BaseValue = BaseHealth
    Add Modifier = Vigor * 10
```

코드에서 직접 갱신하는 대신 다른 Attribute를 참조하는 Infinite GE를 유지하고, GAS가 Aggregator를 Dirty 처리하게 한다. 복잡한 순서 제어가 필요하면 MMC 내부에서 계산.

---

## 9. 소유 관계 — 누가 Aggregator를 관리하나

`FAggregator`는 ASC가 직접 TMap으로 들고 있는 게 아니라, ASC가 소유한 `FActiveGameplayEffectsContainer`가 관리한다.

```
UAbilitySystemComponent
└─ ActiveGameplayEffects : FActiveGameplayEffectsContainer
   ├─ TArray<FActiveGameplayEffect>
   └─ AttributeAggregatorMap : TMap<FGameplayAttribute, FAggregatorRef>
      ├─ Health      → FAggregator
      ├─ MoveSpeed   → FAggregator
      └─ Armor       → FAggregator
```

| 객체 | 역할 |
|---|---|
| `UAbilitySystemComponent` | Attribute·GameplayEffect·Ability 전체 진입점 |
| `FActiveGameplayEffectsContainer` | 활성 GameplayEffect + Attribute Aggregator 관리 |
| `FActiveGameplayEffect` | 적용된 하나의 GE 인스턴스 |
| `FAggregator` | 하나의 Attribute에 적용된 Modifier들 집계 |
| `FAggregatorRef` | `FAggregator`의 공유·수명 관리 참조 |
| `UAttributeSet` | Attribute 데이터·변경 콜백·Aggregator 설정 제공 |

> **오해 주의:** `AttributeSet → FAggregator`가 아니라 `ASC → ActiveGameplayEffectsContainer → FAggregator`. `UAttributeSet::OnAttributeAggregatorCreated()`는 **소유 함수가 아니라 생성 시 정책 설정 콜백**이다. (Container가 별도 struct인 이유는 `FFastArraySerializer` 구현을 위한 분리.)

### 관리 Key = `FGameplayAttribute`

관리 기준은 GE 개수/클래스/핸들/AttributeSet 주소/Modifier 개수가 아니라 **`FGameplayAttribute`**다.

```
하나의 ASC
 └─ 하나의 특정 FGameplayAttribute
     └─ 일반적으로 하나의 FAggregator
         └─ 여러 GameplayEffect Modifier (누적)
```

동일 Attribute에 여러 GE가 적용되면 Aggregator가 여러 개 생기는 게 아니라 **하나의 Aggregator에 Modifier가 누적**된다.

### GE ↔ Aggregator = M:1 (단, GE 하나가 여러 Attribute 수정 가능)

하나의 GE가 여러 Attribute를 수정하면 각 Attribute Aggregator로 Modifier가 **분산**된다.

```
FActiveGameplayEffect A (Health/Armor/MoveSpeed Modifier)
  → Health Aggregator   ← A의 Health Modifier
  → Armor Aggregator    ← A의 Armor Modifier
  → MoveSpeed Aggregator← A의 MoveSpeed Modifier
```

---

## 10. 생성 시점 — 지연 생성

Aggregator는 처음부터 모든 Attribute에 생기지 않고 **필요할 때 지연 생성**된다.

```
1. ASC에 GameplayEffect 적용
2. Container에 FActiveGameplayEffect 추가
3. Effect의 Modifier 목록 순회 → 대상 Attribute 확인
4. 해당 Attribute Aggregator 검색 → 없으면 생성 (FindOrCreateAttributeAggregator)
5. Modifier 추가 → Dirty → CurrentValue 재계산
```

관련 API: `FActiveGameplayEffectsContainer::FindOrCreateAttributeAggregator(FGameplayAttribute)`, `UpdateAggregatorModMagnitudes()`(활성 Effect의 Modifier Magnitude를 Aggregator와 갱신).

**Aggregator가 없는 Attribute도 존재한다** — 선언만 되고 아직 아무 Duration/Infinite Modifier·Capture·평가 요구가 없으면 Aggregator는 미생성. 그래서 `FGameplayAttributeData`와 `FAggregator`는 별개로 이해해야 한다.

---

## 11. 두 BaseValue의 구분 (가장 헷갈리는 지점)

```
FGameplayAttributeData::BaseValue  → Attribute의 실제 영구 BaseValue
FAggregator::BaseValue             → Aggregator가 CurrentValue 계산 시 쓰는 평가 기준값
```

일반 흐름(단방향으로 이해):

```
AttributeData.BaseValue → Aggregator.BaseValue → Aggregator.Evaluate() → AttributeData.CurrentValue
```

Duration GE Add +50 (BaseValue 100):

```
AttributeData.BaseValue = 100, Aggregator 결과 = 150, AttributeData.CurrentValue = 150
제거 시 → CurrentValue = 100 (BaseValue 불변)
```

Instant Damage -30 (BaseValue 변경):

```
AttributeData.BaseValue = 70, Aggregator.BaseValue = 70, CurrentValue = 70
```

→ 일반 Duration/Infinite Modifier는 Aggregator 계산만으로 Attribute BaseValue를 바꾸지 않는다.

---

## 12. ASC 간 독립성 & Source/Target

각 ASC는 독립적인 Aggregator 집합을 갖는다. 이름이 같은 `MoveSpeed`라도 Player ASC와 Enemy ASC의 Aggregator는 **공유되지 않는다.** 사실상 식별 기준은 `ASC + FGameplayAttribute`.

GE 적용 시 Source/Target이 구분되며, **수정받는 Target ASC**가 대상 Attribute의 Aggregator를 소유한다. 단 Modifier Magnitude가 Source Attribute를 참조하면(`Damage = Source AttackPower * 1.5`) Source ASC 쪽 값을 Capture한다. `FAggregatorEvaluateParameters`가 SourceTags/TargetTags를 따로 받는 것도 Source·Target 상태를 모두 고려할 수 있음을 보여준다.

---

## 13. 디버깅 체크리스트

값이 예상과 다를 때:

1. **BaseValue 확인** — `GetBaseValue()`/`GetCurrentValue()`. Base가 이상하면 Instant Effect·초기화 코드·`SetNumericAttributeBase()` 직접 호출 확인.
2. **적용 중 GE 확인** — `ASC.ActiveGameplayEffects` 순회. 각 Effect의 Modifier Op·Magnitude·Duration·Stack·Source/Target Tag·Eval Channel·Prediction.
3. **Modifier Operation 혼동** — Add 0.2 vs Multiply 0.2 / Multiply 20% vs Add 20 / Divide 2 vs Multiply 0.5.
4. **EvaluationMetaData** — `MostNegativeMod_AllPositiveMods`면 일부 감속 Modifier가 계산에서 빠지는 게 정상.
5. **Dirty/Recalculation** — 올바른 ASC/AttributeSet인지, Effect가 무효화·제거되지 않았는지, Tag Requirement가 현재 상태에서 충족되는지, Client 예측 Modifier와 Server Modifier 중복 여부.

---

## 14. 핵심 정리

```
FGameplayAttributeData    → BaseValue·CurrentValue 보유
FAggregator               → BaseValue + GE Modifier들 집계
FAggregatorMod            → 개별 Add/Multiply/Divide/Override
FAggregatorEvaluateParameters → SourceTags·TargetTags·Filter·Prediction
EvaluationMetaData        → 어떤 Modifier를 최종 계산에 포함할지
Aggregator Dirty          → Modifier 추가·제거·Tag 변경 후 CurrentValue 재계산
```

**가장 중요한 관점:** Aggregator는 Attribute의 영구 데이터를 저장하는 객체가 아니라, **현재 적용된 GE들을 기반으로 CurrentValue를 계산하는 객체**다. BaseValue/CurrentValue를 혼동하지 말고, EvaluationMetaData와 `FAggregatorEvaluateParameters`를 함께 보면 대부분의 GAS Attribute 이상 동작을 추적할 수 있다.

관리 규칙 요약:
- Aggregator는 `UAttributeSet`이 아니라 ASC의 `FActiveGameplayEffectsContainer`가 관리한다.
- 관리 Key는 `FGameplayAttribute`. 동일 Attribute엔 보통 하나의 Aggregator, 여러 GE Modifier가 누적.
- 하나의 GE가 여러 Attribute를 수정하면 각 Attribute Aggregator로 Modifier가 나뉜다.
- Aggregator는 주로 Duration/Infinite를 평가해 CurrentValue를 갱신, Instant는 보통 BaseValue를 직접 변경.
- Aggregator 변경은 `OnAttributeAggregatorDirty()`를 통해 ASC가 처리, 서로 다른 ASC의 Aggregator는 공유되지 않는다.
- `OnAttributeAggregatorCreated()`는 소유가 아니라 생성 시 정책 설정 콜백.