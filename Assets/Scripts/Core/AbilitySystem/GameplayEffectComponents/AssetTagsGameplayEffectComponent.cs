using System;
using Core.AbilitySystem.Effect;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.AbilitySystem.GameplayEffectComponents
{
    /// <summary>GE 에셋 자체가 가지는 태그(Asset Tags). 대상에게 부여되지 않는다.</summary>
    /// <remarks>UE: UAssetTagsGameplayEffectComponent</remarks>
    [Serializable]
    public sealed class AssetTagsGameplayEffectComponent : GameplayEffectComponent
    {
        [Tooltip("이 GE가 가지는 태그. 대상에게 주지 않는다.")]
        [SerializeField] [FormerlySerializedAs("inheritableAssetTags")] private InheritedTagContainer _inheritableAssetTags = new();

        /// <remarks>UE: UAssetTagsGameplayEffectComponent::OnGameplayEffectChanged</remarks>
        public override void OnGameplayEffectChanged()
        {
            base.OnGameplayEffectChanged();
            SetAndApplyAssetTagChanges(_inheritableAssetTags);
        }

        /// <summary>설정된 Asset Tags 묶음.</summary>
        /// <remarks>UE: UAssetTagsGameplayEffectComponent::GetConfiguredAssetTagChanges</remarks>
        public InheritedTagContainer GetConfiguredAssetTagChanges()
        {
            return _inheritableAssetTags;
        }

        /// <summary>Asset Tags 묶음을 바꾸고 GE의 Asset Tags에 반영한다.</summary>
        /// <remarks>UE: UAssetTagsGameplayEffectComponent::SetAndApplyAssetTagChanges</remarks>
        public void SetAndApplyAssetTagChanges(InheritedTagContainer tagContainerMods)
        {
            _inheritableAssetTags = tagContainerMods;

            // GE 에셋에는 부모 GE가 없어 물려받는 태그가 없다
            _inheritableAssetTags.UpdateInheritedTagProperties(null);

            ApplyAssetTagChanges();
        }

        /// <remarks>UE: UAssetTagsGameplayEffectComponent::ApplyAssetTagChanges</remarks>
        private void ApplyAssetTagChanges()
        {
            GameplayEffectAsset owner = GetOwner();
            _inheritableAssetTags.ApplyTo(owner.CachedAssetTags);
        }
    }
}
