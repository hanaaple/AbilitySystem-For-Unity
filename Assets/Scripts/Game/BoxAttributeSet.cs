using Core.AbilitySystem.Attribute;

namespace Game
{
    public class BoxAttributeSet : AttributeSet
    {
        public static readonly GameplayAttributeHandle AAttribute    = new(typeof(BoxAttributeSet), nameof(A));
        public static readonly GameplayAttributeHandle BAttribute = new(typeof(BoxAttributeSet), nameof(B));
        public static readonly GameplayAttributeHandle CAttribute   = new(typeof(BoxAttributeSet), nameof(C));

        public AttributeData A;
        public AttributeData B;
        public AttributeData C;
    }
}
