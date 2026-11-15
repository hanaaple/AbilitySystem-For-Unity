using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Abilities
{
    /// <summary>발동하면 시전 지점에서 시전자가 겨누는 지점으로 투사체를 생성하고 바로 끝난다(여러 개면 수평 부채꼴로). 날아가는 동안의 수명은 투사체가 가진다.</summary>
    [Serializable]
    public sealed class FireProjectileAbility : CharacterGameplayAbility
    {
        [SerializeField] [FormerlySerializedAs("projectilePrefab")] private Projectile _projectilePrefab;

        [Tooltip("투사체가 맞힌 대상에게 거는 GE. 발사 시점에 Spec을 만들어 시전자 어트리뷰트를 캡처한다.")]
        [SerializeField] [FormerlySerializedAs("hitEffect")] private GameplayEffectAsset _hitEffect;

        [Tooltip("hitEffect와 함께 맞힌 대상에게 거는 GE들(예: 시간이 지나면 터지는 낙인).")]
        [SerializeField] [FormerlySerializedAs("additionalHitEffects")] private List<GameplayEffectAsset> _additionalHitEffects = new();

        [Tooltip("쏠 때마다 시전자에게 거는 GE(예: 잠깐 카메라 방향을 보게 하는 전투 자세). 비워도 된다.")]
        [SerializeField] [FormerlySerializedAs("launchEffect")] private GameplayEffectAsset _launchEffect;

        [Tooltip("켜면 겨눈 방향의 위아래를 버리고 수평으로 쏜다(바닥 위를 쓸고 가는 파동 등). 끄면 겨눈 지점으로 곧장 — 앞 대상의 몸을 겨누면 그 뒤로는 바닥으로 파고든다.")]
        [SerializeField] [FormerlySerializedAs("flattenAim")] private bool _flattenAim;

        [Header("Spread")]
        [Tooltip("겨눈 방향에서 벗어날 수 있는 최대 각도(원뿔 반각, 도). 0이면 겨눈 곳으로 정확히 간다.")]
        [Min(0f)] [SerializeField] [FormerlySerializedAs("spreadHalfAngle")] private float _spreadHalfAngle;

        [Tooltip("클수록 탄이 가운데로 몰린다. 1이면 벗어나는 각도가 고르게 나온다.")]
        [Min(0.1f)] [SerializeField] [FormerlySerializedAs("spreadExponent")] private float _spreadExponent = 1f;

        [Tooltip("쏘는 순간 시전자가 이 중 하나라도 가지면 퍼짐 없이 쏜다(예: 조준).")]
        [SerializeField] [FormerlySerializedAs("preciseAimTags")] private GameplayTagContainer _preciseAimTags;

        [Header("Fan")]
        [Tooltip("한 번에 쏘는 투사체 수. 2 이상이면 겨눈 방향을 가운데로 수평 부채꼴로 고르게 벌린다.")]
        [Min(1)] [SerializeField] [FormerlySerializedAs("projectileCount")] private int _projectileCount = 1;

        [Tooltip("부채꼴 전체 각도(도) — 맨 왼쪽과 맨 오른쪽 투사체 사이.")]
        [Min(0f)] [SerializeField] [FormerlySerializedAs("fanAngle")] private float _fanAngle;

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            var caster = gameObjectInfo.AvatarGameObject.GetComponent<IProjectileCaster>();
            if (_projectilePrefab == null || caster == null)
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            if (!CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            var hitSpecs = new List<GameplayEffectSpec>();
            float level = GetAbilityLevel(handle, gameObjectInfo);
            if (_hitEffect != null)
            {
                hitSpecs.Add(MakeOutgoingGameplayEffectSpec(handle, gameObjectInfo, _hitEffect, level));
            }

            foreach (GameplayEffectAsset additionalHitEffect in _additionalHitEffects)
            {
                if (additionalHitEffect != null)
                {
                    hitSpecs.Add(MakeOutgoingGameplayEffectSpec(handle, gameObjectInfo, additionalHitEffect, level));
                }
            }

            caster.RequestLaunch((origin, aimPoint) =>
            {
                // 겨눌 때까지 기다리는 사이 취소됐으면 쏘지 않는다
                if (!IsActive)
                {
                    return;
                }

                Vector3 toAim = aimPoint - origin;
                if (_flattenAim)
                {
                    toAim.y = 0f;
                }

                Quaternion rotation = toAim.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toAim) : Quaternion.identity;
                rotation = ApplySpread(rotation, gameObjectInfo.AbilitySystemComponent);
                for (int i = 0; i < _projectileCount; i++)
                {
                    float yawOffset = _projectileCount > 1 ? Mathf.Lerp(-_fanAngle * 0.5f, _fanAngle * 0.5f, i / (float)(_projectileCount - 1)) : 0f;
                    Quaternion fanRotation = Quaternion.Euler(0f, yawOffset, 0f) * rotation;
                    Projectile projectile = UnityEngine.Object.Instantiate(_projectilePrefab, origin, fanRotation);

                    // Spec은 투사체끼리 같이 써도 된다 — 맞힌 대상마다 복제해서 건다
                    projectile.Launch(gameObjectInfo.AvatarGameObject, hitSpecs);
                }

                if (_launchEffect != null)
                {
                    ApplyGameplayEffectToOwner(handle, gameObjectInfo, _launchEffect, GetAbilityLevel(handle, gameObjectInfo));
                }

                EndAbility(handle, gameObjectInfo, false);
            });
        }

        /// <remarks>UE: LyraGameplayAbility_RangedWeapon.cpp VRandConeNormalDistribution</remarks>
        private Quaternion ApplySpread(Quaternion aimRotation, AbilitySystemComponent abilitySystemComponent)
        {
            if (_spreadHalfAngle <= 0f || abilitySystemComponent.HasAnyMatchingGameplayTags(_preciseAimTags))
            {
                return aimRotation;
            }

            // 중심에서 벗어나는 각도와 둘레 방향을 따로 뽑아, 지수는 벗어나는 각도에만 건다

            float angleFromCenter = Mathf.Pow(UnityEngine.Random.value, _spreadExponent) * _spreadHalfAngle;
            float angleAround = UnityEngine.Random.value * 360f;
            return aimRotation * Quaternion.Euler(0f, 0f, angleAround) * Quaternion.Euler(0f, angleFromCenter, 0f);
        }
    }
}
