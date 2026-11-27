using Core.AbilitySystem.Attribute;

namespace Game.Character
{
    public class CombatAttributeSet : AttributeSet
    {
        public static readonly GameplayAttributeHandle DamageAttribute = GameplayAttributeHandle.Create<CombatAttributeSet>(nameof(Damage));

        public AttributeData Damage;
    }
}
