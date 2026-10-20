using System;

namespace Core.AbilitySystem.Effect
{
    /// <summary>어트리뷰트 하나의 CurrentValue 변경 이벤트. 어트리뷰트별로 하나씩 컨테이너가 들고, 구독자는 <see cref="AbilitySystemComponent.GetGameplayAttributeValueChangeDelegate"/>로 받아 건다.</summary>
    /// <remarks>UE: FOnGameplayAttributeValueChange</remarks>
    public sealed class GameplayAttributeValueChangeEvent
    {
        public event Action<OnAttributeChangeData> OnValueChange;

        internal void Broadcast(OnAttributeChangeData changeData)
        {
            OnValueChange?.Invoke(changeData);
        }
    }
}
