using Core.AbilitySystem.Attribute;

public class BoxAttributeSet : AttributeSet
{
    public static readonly GameplayAttributeHandle A    = new(typeof(BoxAttributeSet), nameof(a));
    public static readonly GameplayAttributeHandle B = new(typeof(BoxAttributeSet), nameof(b));
    public static readonly GameplayAttributeHandle C   = new(typeof(BoxAttributeSet), nameof(c));

    public AttributeData a;
    public AttributeData b;
    public AttributeData c;
}
