using System;

namespace Core.AbilitySystem.Effect
{
    /// <summary>활성 GE 하나의 수명 이벤트. 적용별 상태를 필드에 둘 수 없는 쪽(GE Component)이 적용별 정리(태그 구독 해제 등)를 여기에 건다.</summary>
    /// <remarks>UE: FActiveGameplayEffectEvents</remarks>
    public sealed class ActiveGameplayEffectEvents
    {
        /// <summary>GE가 활성 목록에서 빠지고 Owner에 건 효과를 거둔 뒤 한 번.</summary>
        public event Action<ActiveGameplayEffect> OnEffectRemoved;

        internal void BroadcastEffectRemoved(ActiveGameplayEffect removedEffect)
        {
            OnEffectRemoved?.Invoke(removedEffect);
        }
    }
}
