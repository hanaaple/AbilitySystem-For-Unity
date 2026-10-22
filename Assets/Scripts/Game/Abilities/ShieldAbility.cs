using System;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Game.Character;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>
    /// 실드를 건다. 유지 GE가 걸려 있는 동안 활성으로 남아 Activation Owned Tags(실드 태그)가 붙어 있다.
    /// 실드가 피해로 0이 되면 유지 GE를 지워 깨지고, 유지 GE가 끝나면 남은 실드를 비운다.
    /// </summary>
    /// <remarks>
    /// 실드량은 어트리뷰트(<see cref="CharacterAttributeSet.Shield"/>)라 피해 계산이 깎고, 유지 시간은 GE가 잰다 — 이 어빌리티는 둘을 묶어 함께 끝낸다.
    /// </remarks>
    [Serializable]
    public sealed class ShieldAbility : CharacterGameplayAbility
    {
        [Tooltip("실드량을 채우는 GE. 한 번에 채우면 Instant(Override), 천천히 채우면 주기형 Duration — 차는 도중 실드가 끝나면 함께 지운다.")]
        [SerializeField] private GameplayEffectAsset shieldAmountEffect;
        [Tooltip("실드 유지 시간을 재는 GE(Duration). 이 GE가 끝나면 실드도 끝난다. Infinite면 깨질 때까지 유지한다.")]
        [SerializeField] private GameplayEffectAsset durationEffect;
        [Tooltip("시간이 다 돼 끝날 때 남은 실드를 비우는 GE(Instant, Override 0).")]
        [SerializeField] private GameplayEffectAsset clearEffect;

        private GameplayAbilitySpecHandle _handle;
        private GameplayAbilityGameObjectInfo _gameObjectInfo;
        private ActiveGameplayEffectHandle _amountHandle;
        private ActiveGameplayEffectHandle _durationHandle;
        private ActiveGameplayEffectEvents _durationEvents;
        private bool _isBroken;

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (durationEffect == null || !CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            _handle = handle;
            _gameObjectInfo = gameObjectInfo;
            _isBroken = false;

            float level = GetAbilityLevel(handle, gameObjectInfo);
            _amountHandle = shieldAmountEffect != null ? ApplyGameplayEffectToOwner(handle, gameObjectInfo, shieldAmountEffect, level) : ActiveGameplayEffectHandle.Invalid;

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            _durationHandle = ApplyGameplayEffectToOwner(handle, gameObjectInfo, durationEffect, level);
            _durationEvents = abilitySystemComponent.GetActiveEffectEventSet(_durationHandle);
            if (_durationEvents == null)
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            _durationEvents.OnEffectRemoved += OnDurationRemoved;
            abilitySystemComponent.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Shield).OnValueChange += OnShieldChanged;
        }

        public override void EndAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, bool wasCancelled)
        {
            if (!IsEndAbilityValid(handle, gameObjectInfo))
            {
                return;
            }

            // 유지 GE를 지우면 OnDurationRemoved가 다시 이리로 오므로 구독부터 끊는다
            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            abilitySystemComponent.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Shield).OnValueChange -= OnShieldChanged;
            if (_durationEvents != null)
            {
                _durationEvents.OnEffectRemoved -= OnDurationRemoved;
                _durationEvents = null;
                abilitySystemComponent.RemoveActiveGameplayEffect(_durationHandle);
            }

            if (_amountHandle.IsValid)
            {
                abilitySystemComponent.RemoveActiveGameplayEffect(_amountHandle);
                _amountHandle = ActiveGameplayEffectHandle.Invalid;
            }

            base.EndAbility(handle, gameObjectInfo, wasCancelled);

            // 태그가 빠진 뒤에 비운다 — 연출이 태그가 빠지는 순간 남은 실드로 깨짐(0)과 시간 만료(>0)를 가른다
            if (!_isBroken && clearEffect != null)
            {
                ApplyGameplayEffectToOwner(handle, gameObjectInfo, clearEffect, GetAbilityLevel(handle, gameObjectInfo));
            }
        }

        private void OnShieldChanged(OnAttributeChangeData data)
        {
            if (data.OldValue > 0f && data.NewValue <= 0f)
            {
                _isBroken = true;
                EndAbility(_handle, _gameObjectInfo, false);
            }
        }

        private void OnDurationRemoved(ActiveGameplayEffect effect)
        {
            EndAbility(_handle, _gameObjectInfo, false);
        }
    }
}
