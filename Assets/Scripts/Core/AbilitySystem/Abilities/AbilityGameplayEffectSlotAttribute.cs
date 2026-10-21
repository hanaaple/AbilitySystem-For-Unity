using System;
using UnityEngine;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티가 GE를 어떤 용도로 쓰는 칸인지. 용도마다 GE가 갖춰야 할 설정이 다르다.</summary>
    public enum AbilityGameplayEffectSlot
    {
        Cost,
        Cooldown,
    }

    /// <summary>GE 필드에 붙여, 인스펙터에서 넣은 GE가 그 용도(<see cref="AbilityGameplayEffectSlot"/>)에 맞게 설정됐는지 초록·빨강으로 보여 준다.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AbilityGameplayEffectSlotAttribute : PropertyAttribute
    {
        public AbilityGameplayEffectSlot Slot { get; }

        public AbilityGameplayEffectSlotAttribute(AbilityGameplayEffectSlot slot)
        {
            Slot = slot;
        }
    }
}
