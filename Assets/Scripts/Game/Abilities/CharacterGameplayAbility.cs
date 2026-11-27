using System;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Game.Character;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Abilities
{
    /// <summary>캐릭터 스킬의 공통 부모. 공격속도를 따르는 어빌리티면 쿨다운 GE의 지속 시간을 소유자의 공격속도로 나눈다.</summary>
    [Serializable]
    public abstract class CharacterGameplayAbility : GameplayAbility
    {
        [SerializeField] [FormerlySerializedAs("activationPolicy")] private AbilityActivationPolicy _activationPolicy = AbilityActivationPolicy.OnInputTriggered;
        [Tooltip("켜면 쿨다운을 소유자의 공격속도로 나눈다(기본 공격처럼 공격속도를 따르는 어빌리티).")]
        [SerializeField] [FormerlySerializedAs("scalesWithAttackSpeed")] private bool _scalesWithAttackSpeed;

        public AbilityActivationPolicy ActivationPolicy => _activationPolicy;

        // 공격속도는 걸 때 한 번만 읽는다 — 이미 돌고 있는 쿨다운은 버프가 생기거나 끝나도 길이가 그대로다.
        public override void ApplyCooldown(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            GameplayEffectAsset cooldownEffect = GetCooldownGameplayEffect();
            if (cooldownEffect == null)
            {
                return;
            }

            GameplayEffectSpec spec = MakeOutgoingGameplayEffectSpec(handle, gameObjectInfo, cooldownEffect, GetAbilityLevel(handle, gameObjectInfo));
            if (spec == null)
            {
                return;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            if (_scalesWithAttackSpeed && abilitySystemComponent.HasAttributeSetForAttribute(CharacterAttributeSet.AttackSpeedAttribute))
            {
                // 초기값이 없는(0) 캐릭터는 기본 속도로 본다
                float attackSpeed = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.AttackSpeedAttribute);
                spec.SetDuration(spec.Duration / (attackSpeed > 0f ? attackSpeed : 1f));
            }

            ApplyGameplayEffectSpecToOwner(handle, gameObjectInfo, spec);
        }
    }
}
