namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution에 넘기는 입력 묶음. 출력은 별도의 <see cref="GameplayEffectExecutionOutput"/>이 담는다 — 입력/출력을 한 객체에 섞지 않는다.
    ///
    /// 참조 몇 개만 들고 있는 불변 묶음이라 struct다 — Execution마다 만들어도 힙 할당이 없다.
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
        /// 캡처된 어트리뷰트의 CurrentValue를 조회한다. <paramref name="captureDefinition"/>은 이 Execution이
        /// <see cref="GameplayEffectExecution.Defs"/>로 선언한 것과 값이 같아야 매치된다. 캡처 안 됐거나 무효면 false.
        /// </summary>
        public bool AttemptCalculateCapturedAttributeMagnitude(GameplayEffectAttributeCaptureDefinition captureDefinition, out float magnitude)
        {
            return Spec.CapturedRelevantAttributes.TryGetCapturedValue(captureDefinition, AttributeCaptureValueType.CurrentValue, out magnitude);
        }

        /// <summary>값 종류(BaseValue)만 다르고 나머지는 <see cref="AttemptCalculateCapturedAttributeMagnitude"/>와 같다.</summary>
        public bool AttemptCalculateCapturedAttributeBaseValue(GameplayEffectAttributeCaptureDefinition captureDefinition, out float baseValue)
        {
            return Spec.CapturedRelevantAttributes.TryGetCapturedValue(captureDefinition, AttributeCaptureValueType.BaseValue, out baseValue);
        }
    }
}
