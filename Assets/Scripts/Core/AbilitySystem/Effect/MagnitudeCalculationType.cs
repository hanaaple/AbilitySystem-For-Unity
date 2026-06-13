namespace Core.AbilitySystem.Effect
{
    public enum MagnitudeCalculationType
    {
        ScalableFloat,    // 고정 float 값 or Level 별 float Table
        // AttributeBased,   // 어트리뷰트 값 기반: (AttrValue + PreAdd) * Coefficient + PostAdd
        // SetByCaller,      // ApplyEffect 호출 시 키로 주입되는 런타임 값
        // CustomCalculationClass
    }
}
