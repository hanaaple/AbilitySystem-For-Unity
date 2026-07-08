using Core.AbilitySystem.Attribute;

namespace Character
{
    public class TestAtrributeSet : AttributeSet
    {
        public static readonly AttributeHandle Damage = new(typeof(CombatAttributeSet), nameof(damage));

        public AttributeData damage;
    }
}
