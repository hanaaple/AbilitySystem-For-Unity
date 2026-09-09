namespace Core.AbilitySystem.Effect
{
    public enum MagnitudeCalculationType
    {
        ScalableFloat,    // 고정 float 값 or Level 별 float Table
        AttributeBased,   // 어트리뷰트 값 기반: (AttrValue + PreAdd) * Coefficient + PostAdd
    }
}
