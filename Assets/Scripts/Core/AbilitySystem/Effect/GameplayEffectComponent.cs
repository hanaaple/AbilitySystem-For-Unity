using System;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE의 기능 하나를 맡는 부품. GE 에셋이 목록으로 들고, GE가 적용·추가·실행될 때마다 각 Component를 부른다.
    /// 에셋 안에 하나만 있는 인스턴스를 모든 적용이 함께 쓰므로, 적용 대상별 상태를 필드에 두지 않는다.
    /// </summary>
    /// <remarks>UE: UGameplayEffectComponent</remarks>
    [Serializable]
    public abstract class GameplayEffectComponent
    {
        // 담고 있는 GE가 OnGameplayEffectChanged 때마다 넣는다. 직렬화하지 않아야 Component를 다른 GE로 복사해도 옛 GE를 가리키지 않는다
        [NonSerialized] private GameplayEffectAsset _owner;

        /// <summary>이 Component를 담고 있는 GE.</summary>
        /// <remarks>UE: UGameplayEffectComponent::GetOwner</remarks>
        public GameplayEffectAsset GetOwner()
        {
            return _owner;
        }

        internal void SetOwner(GameplayEffectAsset owner)
        {
            _owner = owner;
        }

        /// <summary>이 Spec을 컨테이너 주인에게 적용할 수 있는지. GE의 Component 중 하나라도 false면 적용하지 않는다.</summary>
        /// <remarks>UE: UGameplayEffectComponent::CanGameplayEffectApply</remarks>
        public virtual bool CanGameplayEffectApply(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
            return true;
        }

        /// <summary>지속형 GE가 활성 목록에 들어갈 때. false면 GE를 끈다 — 제거가 아니라 활성으로 남아 다시 켜지기를 기다린다.</summary>
        /// <remarks>UE: UGameplayEffectComponent::OnActiveGameplayEffectAdded</remarks>
        public virtual bool OnActiveGameplayEffectAdded(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, ActiveGameplayEffect activeGameplayEffect)
        {
            return true;
        }

        /// <summary>GE가 실행될 때(Instant 적용, Periodic 주기마다).</summary>
        /// <remarks>UE: UGameplayEffectComponent::OnGameplayEffectExecuted</remarks>
        public virtual void OnGameplayEffectExecuted(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
        }

        /// <summary>GE가 처음 적용될 때(Instant·지속형 모두, 주기 실행에서는 불리지 않음).</summary>
        /// <remarks>UE: UGameplayEffectComponent::OnGameplayEffectApplied</remarks>
        public virtual void OnGameplayEffectApplied(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
        }

        /// <summary>GE 에셋이 로드되거나 편집됐을 때. GE의 캐시(부여 태그 등)를 채운다.</summary>
        /// <remarks>UE: UGameplayEffectComponent::OnGameplayEffectChanged</remarks>
        public virtual void OnGameplayEffectChanged()
        {
        }
    }
}
