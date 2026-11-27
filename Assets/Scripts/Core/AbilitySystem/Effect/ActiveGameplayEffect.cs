namespace Core.AbilitySystem.Effect
{
    public sealed class ActiveGameplayEffect
    {
        public ActiveGameplayEffectHandle Handle { get; }
        public GameplayEffectSpec Spec { get; }

        /// <summary>이 effect가 적용된 대상 ASC. 컨테이너를 아직 두지 않아 effect가 직접 보유한다.</summary>
        public AbilitySystemComponent Owner { get; }

        /// <summary>적용된 시각(컨테이너 시계 <see cref="ActiveGameplayEffectsContainer.GetWorldTime"/> 기준).</summary>
        /// <remarks>UE: FActiveGameplayEffect::StartWorldTime</remarks>
        public double StartWorldTime { get; internal set; }

        /// <summary>다음 주기 실행 시각(<see cref="StartWorldTime"/>과 같은 시계). 지금 시각이 이 시각을 넘으면 실행한다. 주기 GE가 아니면 쓰지 않는다.</summary>
        public double NextPeriodWorldTime { get; internal set; }

        /// <summary>꺼져 있는지. 꺼진 GE는 활성 목록에 남지만 modifier·부여 태그를 Owner에 걸지 않고 주기 실행도 하지 않는다.</summary>
        public bool IsInhibited { get; internal set; }

        /// <summary>이 활성 GE의 수명 이벤트.</summary>
        /// <remarks>UE: FActiveGameplayEffect::EventSet</remarks>
        public ActiveGameplayEffectEvents EventSet { get; } = new();

        public ActiveGameplayEffect(ActiveGameplayEffectHandle handle, GameplayEffectSpec spec, AbilitySystemComponent owner, double startWorldTime)
        {
            Handle = handle;
            Spec = spec;
            Owner = owner;
            StartWorldTime = startWorldTime;
            NextPeriodWorldTime = startWorldTime + spec.Definition.Period;
        }

        /// <summary>Spec의 지속시간. Duration 타입이 아니면 <see cref="GameplayEffectConstants.InfiniteDuration"/>.</summary>
        /// <remarks>UE: FActiveGameplayEffect::GetDuration</remarks>
        public float GetDuration()
        {
            GameplayEffectAsset def = Spec.Definition;
            return def.Type == GameplayEffectType.Duration ? Spec.Duration : GameplayEffectConstants.InfiniteDuration;
        }

        /// <summary>남은 지속 시간(초). 끝나는 시간이 없으면 <see cref="GameplayEffectConstants.InfiniteDuration"/>.</summary>
        /// <param name="worldTime">지금 시각(<see cref="StartWorldTime"/>과 같은 시계).</param>
        /// <remarks>UE: FActiveGameplayEffect::GetTimeRemaining</remarks>
        public float GetTimeRemaining(double worldTime)
        {
            float duration = GetDuration();
            return duration == GameplayEffectConstants.InfiniteDuration ? GameplayEffectConstants.InfiniteDuration : (float)(duration - (worldTime - StartWorldTime));
        }

        /// <summary>끝나는 시각(<see cref="StartWorldTime"/>과 같은 시계).</summary>
        /// <remarks>UE: FActiveGameplayEffect::GetEndTime</remarks>
        public double GetEndTime()
        {
            float duration = GetDuration();
            return duration == GameplayEffectConstants.InfiniteDuration ? GameplayEffectConstants.InfiniteDuration : StartWorldTime + duration;
        }
    }
}
