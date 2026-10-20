namespace Core.AbilitySystem.Effect
{
    /// <summary>GE 실행이 어트리뷰트 하나를 바꾼 한 건 — 어느 Spec이 어떤 수치로 누구에게 적용했나.</summary>
    /// <remarks>UE: FGameplayEffectModCallbackData</remarks>
    public sealed class GameplayEffectModCallbackData
    {
        public GameplayEffectSpec EffectSpec { get; }
        /// <summary>적용할 수치. <see cref="Attribute.AttributeSet.PreGameplayEffectExecute"/>에서 바꾸면 바꾼 값으로 적용된다.</summary>
        public GameplayModifierEvaluatedData EvaluatedData { get; set; }
        public AbilitySystemComponent Target { get; }

        public GameplayEffectModCallbackData(GameplayEffectSpec effectSpec, GameplayModifierEvaluatedData evaluatedData, AbilitySystemComponent target)
        {
            EffectSpec = effectSpec;
            EvaluatedData = evaluatedData;
            Target = target;
        }
    }
}
