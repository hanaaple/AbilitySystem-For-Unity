using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    public readonly struct GameplayEffectExecutionOutput
    {
        public AttributeHandle Handle { get; }
        public float Magnitude { get; }
        public GameplayModifierOperation Operation { get; }

        public GameplayEffectExecutionOutput(AttributeHandle handle, float magnitude, GameplayModifierOperation operation = GameplayModifierOperation.AddBase)
        {
            Handle = handle;
            Magnitude = magnitude;
            Operation = operation;
        }
    }
}
