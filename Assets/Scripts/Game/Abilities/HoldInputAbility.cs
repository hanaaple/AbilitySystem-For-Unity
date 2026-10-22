using System;
using System.Collections.Generic;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>
    /// 입력을 누르는 동안 활성으로 남는다(달리기·조준). 효과는 Activation Owned Tags와, 활성 동안 걸어 두는 GE(예: 이동 속도 배율)로 나타난다.
    /// </summary>
    [Serializable]
    public sealed class HoldInputAbility : CharacterGameplayAbility
    {
        [Tooltip("활성인 동안 소유자에게 걸어 두고 끝날 때 지우는 GE(Infinite).")]
        [SerializeField] private List<GameplayEffectAsset> whileActiveEffects = new();

        private readonly List<ActiveGameplayEffectHandle> _activeHandles = new();

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (!CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            float level = GetAbilityLevel(handle, gameObjectInfo);
            foreach (GameplayEffectAsset effect in whileActiveEffects)
            {
                if (effect != null)
                {
                    _activeHandles.Add(ApplyGameplayEffectToOwner(handle, gameObjectInfo, effect, level));
                }
            }
        }

        public override void InputReleased(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            EndAbility(handle, gameObjectInfo, false);
        }

        public override void EndAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, bool wasCancelled)
        {
            if (!IsEndAbilityValid(handle, gameObjectInfo))
            {
                return;
            }

            foreach (ActiveGameplayEffectHandle activeHandle in _activeHandles)
            {
                gameObjectInfo.AbilitySystemComponent.RemoveActiveGameplayEffect(activeHandle);
            }

            _activeHandles.Clear();
            base.EndAbility(handle, gameObjectInfo, wasCancelled);
        }
    }
}
