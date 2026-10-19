using System;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Core.AbilitySystem.GameplayEffectComponents
{
    /// <summary>GE가 활성인 동안 대상에게 부여하는 태그(Granted Tags). 적용·제거·켜고 끌 때 컨테이너가 대상 소유 태그 개수를 올리고 내린다.</summary>
    /// <remarks>UE: UTargetTagsGameplayEffectComponent</remarks>
    [Serializable]
    public sealed class TargetTagsGameplayEffectComponent : GameplayEffectComponent
    {
        [Tooltip("이 GE가 활성인 동안 대상에게 주는 태그 (UE: Add Tags). Instant GE는 활성 목록에 남지 않아 주지 않는다.")]
        [SerializeField] private InheritedTagContainer inheritableGrantedTagsContainer = new();

        /// <summary>설정된 부여 태그 묶음.</summary>
        /// <remarks>UE: UTargetTagsGameplayEffectComponent::GetConfiguredTargetTagChanges</remarks>
        public InheritedTagContainer GetConfiguredTargetTagChanges()
        {
            return inheritableGrantedTagsContainer;
        }

        /// <remarks>UE: UTargetTagsGameplayEffectComponent::OnGameplayEffectChanged</remarks>
        public override void OnGameplayEffectChanged()
        {
            base.OnGameplayEffectChanged();
            SetAndApplyTargetTagChanges(inheritableGrantedTagsContainer);
        }

        /// <summary>부여 태그 묶음을 바꾸고 GE의 Granted Tags에 반영한다.</summary>
        /// <remarks>UE: UTargetTagsGameplayEffectComponent::SetAndApplyTargetTagChanges</remarks>
        public void SetAndApplyTargetTagChanges(InheritedTagContainer tagContainerMods)
        {
            inheritableGrantedTagsContainer = tagContainerMods;

            // GE 에셋에는 부모 GE가 없어 물려받는 태그가 없다
            inheritableGrantedTagsContainer.UpdateInheritedTagProperties(null);

            ApplyTargetTagChanges();
        }

        /// <remarks>UE: UTargetTagsGameplayEffectComponent::ApplyTargetTagChanges</remarks>
        private void ApplyTargetTagChanges()
        {
            GameplayEffectAsset owner = GetOwner();
            inheritableGrantedTagsContainer.ApplyTo(owner.CachedGrantedTags);
        }
    }
}
