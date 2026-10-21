namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티 종료 한 건 — ASC 종료 이벤트(<see cref="AbilitySystemComponent.OnAbilityEndedWithData"/>)가 구독자에게 넘긴다.</summary>
    /// <remarks>UE: FAbilityEndedData</remarks>
    public readonly struct AbilityEndedData
    {
        /// <summary>끝난 어빌리티 인스턴스.</summary>
        public GameplayAbility AbilityThatEnded { get; }

        public GameplayAbilitySpecHandle AbilitySpecHandle { get; }

        /// <summary>취소로 끝났으면 true, 정상 종료면 false.</summary>
        public bool WasCancelled { get; }

        public AbilityEndedData(GameplayAbility abilityThatEnded, GameplayAbilitySpecHandle abilitySpecHandle, bool wasCancelled)
        {
            AbilityThatEnded = abilityThatEnded;
            AbilitySpecHandle = abilitySpecHandle;
            WasCancelled = wasCancelled;
        }
    }
}
