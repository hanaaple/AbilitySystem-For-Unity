using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GameplayModifier(정의)의 런타임 인스턴스.
    /// Spec 생성 시점에 AttributeHandle resolve와 Magnitude 계산을 완료해 캐싱한다.
    /// </summary>
    public readonly struct GameplayModifierSpec
    {
        public AttributeHandle Handle { get; }
        public GameplayModifierOperation Operation { get; }
        public float EvaluatedMagnitude { get; }

        public bool IsValid => Handle.IsValid;

        public GameplayModifierSpec(GameplayModifier modifier, float level)
        {
            Handle = modifier.ToAttributeHandle();
            Operation = modifier.Operation;
            EvaluatedMagnitude = modifier.GetMagnitude(level);
        }
    }
}
