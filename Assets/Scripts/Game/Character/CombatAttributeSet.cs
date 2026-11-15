using Core.AbilitySystem.Attribute;

namespace Game.Character
{
    public class CombatAttributeSet : AttributeSet
    {
        public static readonly GameplayAttributeHandle DamageAttribute = new(typeof(CombatAttributeSet), nameof(Damage));

        public AttributeData Damage;
    }
}
