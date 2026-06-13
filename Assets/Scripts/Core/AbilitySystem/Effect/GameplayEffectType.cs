namespace Core.AbilitySystem.Effect
{
    public enum GameplayEffectType
    {
        Instant,   // BaseValue 일회성 변경 (미구현)
        Infinite,  // CurrentValue 지속 변경 — 장비 장착 효과
        Duration,  // CurrentValue 시간 제한 변경 (미구현)
    }
}
