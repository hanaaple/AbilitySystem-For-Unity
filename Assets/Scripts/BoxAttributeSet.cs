using Core.AbilitySystem.Attribute;

public class BoxAttributeSet : AttributeSet
{
    public static readonly AttributeHandle A    = new(typeof(BoxAttributeSet), nameof(a));
    public static readonly AttributeHandle B = new(typeof(BoxAttributeSet), nameof(b));
    public static readonly AttributeHandle C   = new(typeof(BoxAttributeSet), nameof(c));

    public AttributeData a;
    public AttributeData b;
    public AttributeData c;
}
