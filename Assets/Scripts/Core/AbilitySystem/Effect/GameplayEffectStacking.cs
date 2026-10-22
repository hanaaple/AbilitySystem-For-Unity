namespace Core.AbilitySystem.Effect
{
    /// <summary>같은 GE가 다시 걸릴 때 기존 활성 GE에 합칠지, 무엇을 기준으로 같은 GE로 볼지.</summary>
    /// <remarks>UE: EGameplayEffectStackingType</remarks>
    public enum GameplayEffectStackingType
    {
        /// <summary>합치지 않는다 — 걸 때마다 새 활성 GE.</summary>
        None,

        /// <summary>시전자(Instigator ASC)마다 하나.</summary>
        AggregateBySource,

        /// <summary>대상마다 하나 — 누가 걸었든 합친다.</summary>
        AggregateByTarget,
    }

    /// <summary>합쳐질 때 지속 시간을 다시 시작할지.</summary>
    /// <remarks>UE: EGameplayEffectStackingDurationPolicy</remarks>
    public enum GameplayEffectStackingDurationPolicy
    {
        RefreshOnSuccessfulApplication,
        NeverRefresh,
    }

    /// <summary>합쳐질 때 주기 타이머를 처음부터 다시 셀지.</summary>
    /// <remarks>UE: EGameplayEffectStackingPeriodPolicy</remarks>
    public enum GameplayEffectStackingPeriodPolicy
    {
        ResetOnSuccessfulApplication,
        NeverReset,
    }
}
