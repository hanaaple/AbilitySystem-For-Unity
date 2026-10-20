namespace Core.AbilitySystem.Effect
{
    /// <summary>GE 실행이 어트리뷰트 하나를 바꾼 한 건 — 어느 Spec이 어떤 수치로 누구에게 적용했나.</summary>
    /// <remarks>UE: FGameplayEffectModCallbackData</remarks>
    public sealed class GameplayEffectModCallbackData
    {
        public GameplayEffectSpec EffectSpec { get; }
        public GameplayModifierEvaluatedData EvaluatedData { get; }
        public AbilitySystemComponent Target { get; }

        public GameplayEffectModCallbackData(GameplayEffectSpec effectSpec, GameplayModifierEvaluatedData evaluatedData, AbilitySystemComponent target)
        {
            EffectSpec = effectSpec;
            EvaluatedData = evaluatedData;
            Target = target;
        }
    }
}
