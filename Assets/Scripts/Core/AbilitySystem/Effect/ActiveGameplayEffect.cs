namespace Core.AbilitySystem.Effect
{
    public sealed class ActiveGameplayEffect
    {
        public ActiveGameplayEffectHandle Handle { get; }
        public GameplayEffectSpec Spec { get; }

        /// <summary>이 effect가 적용된 대상 ASC. 컨테이너를 아직 두지 않아 effect가 직접 보유한다.</summary>
        public AbilitySystemComponent Owner { get; }

        /// <summary>Duration 타입일 때만 유효. 남은 지속 시간(초).</summary>
        public float RemainingDuration { get; set; }

        /// <summary>마지막 주기 실행 이후 경과 시간(초).</summary>
        public float PeriodTimer { get; set; }

        /// <summary>꺼져 있는지. 꺼진 GE는 활성 목록에 남지만 modifier·부여 태그를 Owner에 걸지 않고 주기 실행도 하지 않는다.</summary>
        public bool IsInhibited { get; internal set; }

        /// <summary>이 활성 GE의 수명 이벤트.</summary>
        /// <remarks>UE: FActiveGameplayEffect::EventSet</remarks>
        public ActiveGameplayEffectEvents EventSet { get; } = new();

        public ActiveGameplayEffect(ActiveGameplayEffectHandle handle, GameplayEffectSpec spec, AbilitySystemComponent owner)
        {
            Handle = handle;
            Spec = spec;
            Owner = owner;
            RemainingDuration = spec.Definition.Duration;
            PeriodTimer = 0f;
        }
    }
}
