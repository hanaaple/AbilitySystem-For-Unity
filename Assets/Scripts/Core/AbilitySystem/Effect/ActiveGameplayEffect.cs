namespace Core.AbilitySystem.Effect
{
    public sealed class ActiveGameplayEffect
    {
        public ActiveGameplayEffectHandle Handle { get; }
        public GameplayEffectSpec Spec { get; }

        /// <summary>Duration 타입일 때만 유효. 남은 지속 시간(초).</summary>
        public float RemainingDuration { get; set; }

        /// <summary>마지막 주기 실행 이후 경과 시간(초).</summary>
        public float PeriodTimer { get; set; }

        public ActiveGameplayEffect(ActiveGameplayEffectHandle handle, GameplayEffectSpec spec)
        {
            Handle = handle;
            Spec = spec;
            RemainingDuration = spec.Definition.Duration;
            PeriodTimer = 0f;
        }
    }
}
