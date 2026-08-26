using Core.AbilitySystem.Attribute;

namespace Character
{
    public class CharacterAttributeSet : AttributeSet
    {
        // 외부에서 어트리뷰트를 식별하는 정적 핸들
        public static readonly GameplayAttributeHandle Health    = new(typeof(CharacterAttributeSet), nameof(health));
        public static readonly GameplayAttributeHandle MaxHealth = new(typeof(CharacterAttributeSet), nameof(maxHealth));
        public static readonly GameplayAttributeHandle Stamina   = new(typeof(CharacterAttributeSet), nameof(stamina));
        public static readonly GameplayAttributeHandle MaxStamina = new(typeof(CharacterAttributeSet), nameof(maxStamina));
        public static readonly GameplayAttributeHandle Speed     = new(typeof(CharacterAttributeSet), nameof(speed));

        public AttributeData health;
        public AttributeData maxHealth;
        public AttributeData stamina;
        public AttributeData maxStamina;
        public AttributeData speed;
    }
}
