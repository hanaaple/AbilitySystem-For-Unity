namespace Core.AbilitySystem.Effect
{
    /// <remarks>UE: EGameplayEffectMagnitudeCalculation</remarks>
    public enum MagnitudeCalculationType
    {
        ScalableFloat,            // 고정 float 값 or Level 별 float Table
        AttributeBased,           // 어트리뷰트 값 기반: (AttrValue + PreAdd) * Coefficient + PostAdd
        CustomCalculationClass,   // TODO 구현 예정 — 여러 어트리뷰트를 캡처해 계산하는 사용자 클래스
        SetByCaller,              // TODO 구현 예정 — Spec을 만드는 쪽이 태그를 키로 값을 넣는다
    }
}
