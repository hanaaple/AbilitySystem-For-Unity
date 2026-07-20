namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution에 넘기는 입력 묶음. 출력은 별도의 <see cref="GameplayEffectExecutionOutput"/>이 담는다
    /// — 입력/출력을 한 객체에 섞지 않는다(UE: FGameplayEffectCustomExecutionParameters).
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

        // TODO Calculation Modifiers (UE: FGameplayEffectExecutionScopedModifierInfo / ScopedModifierAggregators)
        // "이 Execution이 도는 동안에만" 캡처 값을 보정하는 GE 에셋별 데이터. 실제 어트리뷰트는 불변.
        // 예) 같은 데미지 공식을 쓰면서 GE마다 "방어력 50% 무시" / "공격력 1.5배"를 데이터로만 지정.
        // Execution 입장에선 보정이 이미 반영된 값이 조회되어, 계산 코드는 바뀌지 않는다.
        //
        // 선행 조건: AttributeCapture 계층 (→decisions D6에서 미도입).
        //   지금은 Execution이 SourceAsc/TargetAsc를 직접 읽어 개입 지점이 없다.
        //   보정을 끼우려면 읽기를 캡처 계층 한 곳으로 모아야 한다
        //   (parameters.SourceAsc.GetAttributeCurrentValue(h) → parameters.GetCapturedValue(captureDef)).
        // 참고: UE는 계산 클래스가 InvalidScopedModifierAttributes로 "이 캡처는 보정 금지"를 선언할 수 있다.
        //
        // 도입 시 이 데이터가 Execution별로 달라지므로, ASC.RunExecutions에서 params를 루프 안에서 만들어야 한다.

        public GameplayEffectExecutionParameters(AbilitySystemComponent target, GameplayEffectSpec spec)
        {
            TargetAsc = target;
            SourceAsc = spec.Context.GetInstigator();
            Spec = spec;
        }
    }
}
