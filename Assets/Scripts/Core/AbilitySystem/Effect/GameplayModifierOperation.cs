namespace Core.AbilitySystem.Effect
{
    // CurrentValue = ((BaseValue + AddBase) * MultiplyAdditive / DivideAdditive * MultiplyCompound) + AddFinal
    public enum GameplayModifierOperation
    {
        AddBase,          // BaseValue에 더함 — 곱셈 전 적용
        MultiplyAdditive, // 배율 누산: 1 + Σ(magnitude - 1)  →  1.5x + 1.5x = 2.0x
        DivideAdditive,   // 제수 누산: 1 + Σ(magnitude - 1)  →  분모에 동일 방식 적용
        MultiplyCompound, // 배율 곱산: Π magnitude            →  1.5x * 1.5x = 2.25x
        AddFinal,         // 최종값에 더함 — 곱셈 후 적용
        Override,         // 마지막 Override가 최종값을 덮어씀
    }
}
