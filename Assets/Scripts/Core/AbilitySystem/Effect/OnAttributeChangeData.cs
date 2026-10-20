using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>어트리뷰트 CurrentValue가 다시 계산된 한 건 — 값 변경 이벤트(<see cref="GameplayAttributeValueChangeEvent"/>)가 구독자에게 넘긴다. 값이 그대로여도 온다.</summary>
    /// <remarks>UE: FOnAttributeChangeData</remarks>
    public readonly struct OnAttributeChangeData
    {
        public GameplayAttributeHandle Attribute { get; }
        public float NewValue { get; }
        public float OldValue { get; }

        /// <summary>GE 실행(Instant·Periodic·Execution 출력)으로 바뀐 경우의 원인. 그 밖의 변경(Duration GE의 mod 등록·해제, 직접 Base 설정)이면 null.</summary>
        public GameplayEffectModCallbackData GEModData { get; }

        public OnAttributeChangeData(GameplayAttributeHandle attribute, float newValue, float oldValue, GameplayEffectModCallbackData geModData)
        {
            Attribute = attribute;
            NewValue = newValue;
            OldValue = oldValue;
            GEModData = geModData;
        }
    }
}
