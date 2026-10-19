namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution에 넘기는 입력 묶음. 출력은 별도 <see cref="GameplayEffectExecutionOutput"/>이 담는다(입력/출력을 한 객체에 안 섞는다).
    /// 참조 몇 개만 든 불변 묶음이라 struct — Execution마다 만들어도 힙 할당이 없다.
    /// </summary>
    public readonly struct GameplayEffectExecutionParameters
    {
        /// <summary>GE를 받는 대상 ASC.</summary>
        public AbilitySystemComponent TargetAsc { get; }

        /// <summary>GE를 발동한 주체 ASC. Context에 Instigator가 없으면 null.</summary>
        public AbilitySystemComponent SourceAsc { get; }

        public GameplayEffectSpec Spec { get; }

        // TODO: Scoped Modifier — Execution이 도는 동안에만 캡처 값을 보정하는 GE별 데이터(실제 어트리뷰트는 불변). 미도입.

        public GameplayEffectExecutionParameters(AbilitySystemComponent target, GameplayEffectSpec spec)
        {
            TargetAsc = target;
            SourceAsc = spec.Context.GetInstigator();
            Spec = spec;
        }

        /// <summary>
        /// 캡처된 어트리뷰트의 CurrentValue를 조회한다. <paramref name="captureDefinition"/>은 이 Execution이 <see cref="GameplayEffectExecution.Defs"/>로 선언한 것과 값이 같아야 매치. 미캡처·무효면 false.
        /// 캡처한 어트리뷰트의 mod 자격은 <paramref name="evaluationParameters"/>의 태그로 판정한다 — 보통 <see cref="Spec"/>의 CapturedSourceTags·CapturedTargetTags 합친 태그를 넘긴다.
        /// </summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeMagnitude</remarks>
        public bool AttemptCalculateCapturedAttributeMagnitude(GameplayEffectAttributeCaptureDefinition captureDefinition, AggregatorEvaluateParameters evaluationParameters, out float magnitude)
        {
            GameplayEffectAttributeCaptureSpecContainer capturedRelevantAttributes = Spec.CapturedRelevantAttributes;
            return capturedRelevantAttributes.TryGetCapturedValue(captureDefinition, AttributeCaptureValueType.CurrentValue, evaluationParameters, out magnitude);
        }

        /// <summary>캡처된 어트리뷰트의 BaseValue를 조회한다. mod와 무관해 태그 문맥을 받지 않는다. 나머지는 <see cref="AttemptCalculateCapturedAttributeMagnitude"/>와 같다.</summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeBaseValue</remarks>
        public bool AttemptCalculateCapturedAttributeBaseValue(GameplayEffectAttributeCaptureDefinition captureDefinition, out float baseValue)
        {
            GameplayEffectAttributeCaptureSpecContainer capturedRelevantAttributes = Spec.CapturedRelevantAttributes;
            return capturedRelevantAttributes.TryGetCapturedValue(captureDefinition, AttributeCaptureValueType.BaseValue, default, out baseValue);
        }

        // TODO Calculation Modifier 적용 — 만들 때 Execution 정의의 Calculation Modifier로 집계기 두 묶음을 채운다.
        //  캡처 어트리뷰트: 캡처 정의 → 그 어트리뷰트 aggregator의 사본 + modifier(실제 캐릭터 값은 그대로). 위 두 조회도 사본이 있으면 사본을 읽는다.
        //  Transient: 태그 → 기본값 0인 새 aggregator + modifier. AttemptCalculateTransientAggregatorMagnitude(태그, evaluationParameters, out 값)로 읽고,
        //  그 태그에 건 modifier가 없으면 false. (AttributeAggregator를 캐릭터 없이 단독으로 만들 수 있는지 먼저 확인)
    }
}
