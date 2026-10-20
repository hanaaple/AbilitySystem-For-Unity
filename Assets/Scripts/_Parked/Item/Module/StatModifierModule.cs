using System;
using Core.AbilitySystem.Effect;
using Core.ItemSystem.Module;
using UnityEngine;

namespace Item.Module
{
    // 회수 핸들 = 이 장착 개체의 per-item 상태 → 모듈이 아니라 상태가 보유한다.
    public sealed class StatModifierState : IModuleState
    {
        public ActiveGameplayEffectHandle Handle;
    }

    /// <summary>장착 시 지정한 GameplayEffectAsset(Infinite 타입)를 소유자 ASC에 적용하고, 해제 시 제거한다.</summary>
    [Serializable]
    public sealed class StatModifierModule : ItemModule<StatModifierState>
    {
        [SerializeField] private GameplayEffectAsset effect;

        protected override void OnEquip(ModuleContext context, StatModifierState state)
        {
            // 모듈은 씬을 모르고 ASC(시스템)에만 요청한다. 컨텍스트가 ASC를 주지 못하면 무시.
            if (effect == null || context is not AbilitySystemModuleContext asc)
            {
                return;
            }

            state.Handle = asc.AbilitySystem.ApplyGameplayEffectToSelf(effect);
        }

        protected override void OnUnEquip(ModuleContext context, StatModifierState state)
        {
            if (context is not AbilitySystemModuleContext asc)
            {
                return;
            }

            asc.AbilitySystem.RemoveActiveGameplayEffect(state.Handle);
            state.Handle = ActiveGameplayEffectHandle.Invalid;
        }
    }
}
