using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>
    /// 구간 안에 있는 동안의 상태. 발동하면 안쪽 GE를 걸어 두고, 끝나면(구간을 나가 취소되면) 안쪽 GE를 지운 뒤 나간 뒤 GE를 건다.
    /// </summary>
    /// <remarks>
    /// 들어오고 나가는 시점은 구간(<see cref="AbilityZone"/>)이 발동·취소로 알린다 — 무엇을 걸지는 이 어빌리티 에셋이 정한다.
    /// </remarks>
    [Serializable]
    public sealed class ZoneAbility : CharacterGameplayAbility
    {
        [Tooltip("안에 있는 동안 걸어 두는 GE(Infinite). 끝날 때 지운다.")]
        [SerializeField] private List<GameplayEffectAsset> insideEffects = new();
        [Tooltip("나갈 때 거는 GE. 나와도 잠시 남는 디버프(Duration)용.")]
        [SerializeField] private List<GameplayEffectAsset> exitEffects = new();

        private readonly List<ActiveGameplayEffectHandle> _insideHandles = new();

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (!CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            float level = GetAbilityLevel(handle, gameObjectInfo);
            foreach (GameplayEffectAsset effect in insideEffects)
            {
                _insideHandles.Add(ApplyGameplayEffectToOwner(handle, gameObjectInfo, effect, level));
            }
        }

        public override void EndAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, bool wasCancelled)
        {
            if (!IsEndAbilityValid(handle, gameObjectInfo))
            {
                return;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            foreach (ActiveGameplayEffectHandle insideHandle in _insideHandles)
            {
                abilitySystemComponent.RemoveActiveGameplayEffect(insideHandle);
            }

            _insideHandles.Clear();

            float level = GetAbilityLevel(handle, gameObjectInfo);
            foreach (GameplayEffectAsset effect in exitEffects)
            {
                ApplyGameplayEffectToOwner(handle, gameObjectInfo, effect, level);
            }

            base.EndAbility(handle, gameObjectInfo, wasCancelled);
        }
    }
}
