using System;
using System.Collections.Generic;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>발동하면 GE들을 소유자에게 걸고 바로 끝난다. 회복·강화처럼 "먹으면 효과가 걸리는" 아이템을 어빌리티로 다루기 위한 것.</summary>
    [Serializable]
    public sealed class ApplyEffectsAbility : CharacterGameplayAbility
    {
        [SerializeField] private List<GameplayEffectAsset> effects = new();

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (!CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            float level = GetAbilityLevel(handle, gameObjectInfo);
            foreach (GameplayEffectAsset effect in effects)
            {
                if (effect != null)
                {
                    ApplyGameplayEffectToOwner(handle, gameObjectInfo, effect, level);
                }
            }

            EndAbility(handle, gameObjectInfo, false);
        }
    }
}
