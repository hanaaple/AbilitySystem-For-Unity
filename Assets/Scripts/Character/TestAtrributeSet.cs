using Core.AbilitySystem.Attribute;

namespace Character
{
    public class TestAtrributeSet : AttributeSet
    {
        public static readonly GameplayAttributeHandle Damage = new(typeof(CombatAttributeSet), nameof(damage));

        public AttributeData damage;
    }
}
