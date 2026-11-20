using Core.AbilitySystem.Attribute;

namespace Game
{
    public class BoxAttributeSet : AttributeSet
    {
        public static readonly GameplayAttributeHandle AAttribute    = GameplayAttributeHandle.Create<BoxAttributeSet>(nameof(A));
        public static readonly GameplayAttributeHandle BAttribute = GameplayAttributeHandle.Create<BoxAttributeSet>(nameof(B));
        public static readonly GameplayAttributeHandle CAttribute   = GameplayAttributeHandle.Create<BoxAttributeSet>(nameof(C));

        public AttributeData A;
        public AttributeData B;
        public AttributeData C;
    }
}
