using System;
using System.Collections.Generic;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.GameplayEffectComponents
{
    /// <summary>GE를 받는 대상의 소유 태그로 GE의 적용·유지·제거를 정한다.</summary>
    /// <remarks>UE: UTargetTagRequirementsGameplayEffectComponent</remarks>
    [Serializable]
    public sealed class TargetTagRequirementsGameplayEffectComponent : GameplayEffectComponent
    {
        [Tooltip("적용하는 순간에만 본다. 못 맞추면 적용하지 않는다.")]
        [SerializeField] private GameplayTagRequirements applicationTagRequirements = new();

        [Tooltip("적용된 뒤 GE를 켜고 끄는 조건. 꺼져도 제거되지 않고 남는다.")]
        [SerializeField] private GameplayTagRequirements ongoingTagRequirements = new();

        [Tooltip("맞추면 GE를 제거한다. 적용도 막는다.")]
        [SerializeField] private GameplayTagRequirements removalTagRequirements = new();

        public GameplayTagRequirements ApplicationTagRequirements => applicationTagRequirements;
        public GameplayTagRequirements OngoingTagRequirements => ongoingTagRequirements;
        public GameplayTagRequirements RemovalTagRequirements => removalTagRequirements;

        /// <summary>대상 소유 태그가 Application 조건을 맞추고 Removal 조건은 맞추지 않아야 적용한다.</summary>
        /// <remarks>UE: UTargetTagRequirementsGameplayEffectComponent::CanGameplayEffectApply</remarks>
        public override bool CanGameplayEffectApply(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
            GameplayTagContainer ownedTags = GetOwnedTags(activeGameplayEffectsContainer.Owner);

            if (!applicationTagRequirements.RequirementsMet(ownedTags))
            {
                return false;
            }

            return !HaveRemovalRequirementsBeenMet(ownedTags);
        }

        /// <summary>Ongoing·Removal 조건에 나오는 태그마다 변화를 구독하고, 지금 Ongoing 조건을 맞추는지로 켜진 채 시작할지 정한다. 구독은 GE가 제거될 때 푼다.</summary>
        /// <remarks>UE: UTargetTagRequirementsGameplayEffectComponent::OnActiveGameplayEffectAdded</remarks>
        public override bool OnActiveGameplayEffectAdded(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, ActiveGameplayEffect activeGameplayEffect)
        {
            AbilitySystemComponent asc = activeGameplayEffectsContainer.Owner;
            ActiveGameplayEffectHandle handle = activeGameplayEffect.Handle;
            ActiveGameplayEffectEvents eventSet = asc.GetActiveEffectEventSet(handle);

            if (eventSet != null)
            {
                // 아무 태그 변화마다 불리지 않도록 조건에 나오는 태그에만 건다
                var tagsToBind = new List<GameplayTag>();
                AppendTagsUnique(tagsToBind, ongoingTagRequirements);
                AppendTagsUnique(tagsToBind, removalTagRequirements);

                // Component는 에셋에 하나뿐이고 모든 적용이 같이 쓴다 — 적용별 구독 목록은 필드가 아니라 제거 이벤트에 건 콜백이 든다
                var boundEvents = new List<(GameplayTag Tag, OnGameplayEffectTagCountChanged Callback)>();
                foreach (GameplayTag tag in tagsToBind)
                {
                    OnGameplayEffectTagCountChanged callback = (changedTag, newCount) => OnTagChanged(handle);
                    asc.RegisterGameplayTagEvent(tag, callback, GameplayTagEventType.NewOrRemoved);
                    boundEvents.Add((tag, callback));
                }

                eventSet.OnEffectRemoved += removedEffect => OnGameplayEffectRemoved(asc, boundEvents);
            }
            else
            {
                Debug.LogError($"[TargetTagRequirements] 활성 GE 핸들이 잘못돼 태그 구독을 걸지 못했습니다 — '{GetOwner()?.name}'.");
            }

            GameplayTagContainer ownedTags = GetOwnedTags(asc);
            return ongoingTagRequirements.RequirementsMet(ownedTags);
        }

        /// <remarks>UE: UTargetTagRequirementsGameplayEffectComponent::OnGameplayEffectRemoved</remarks>
        private void OnGameplayEffectRemoved(AbilitySystemComponent asc, List<(GameplayTag Tag, OnGameplayEffectTagCountChanged Callback)> boundEvents)
        {
            foreach ((GameplayTag tag, OnGameplayEffectTagCountChanged callback) in boundEvents)
            {
                if (!asc.UnregisterGameplayTagEvent(callback, tag, GameplayTagEventType.NewOrRemoved))
                {
                    Debug.LogError($"[TargetTagRequirements] '{GetOwner()?.name}'의 태그 이벤트 '{tag}' 구독을 풀지 못했습니다.");
                }
            }
        }

        /// <summary>구독한 태그가 생기거나 없어지면 Removal 조건을 맞출 때 제거하고, 아니면 Ongoing 조건으로 켜고 끈다.</summary>
        /// <remarks>UE: UTargetTagRequirementsGameplayEffectComponent::OnTagChanged</remarks>
        private void OnTagChanged(ActiveGameplayEffectHandle handle)
        {
            AbilitySystemComponent owner = handle.GetOwningAbilitySystemComponent();
            if (owner == null)
            {
                return;
            }

            // 태그 변화 콜백이 이어지는 중에 이 GE가 이미 제거됐을 수 있다
            if (owner.GetActiveGameplayEffect(handle) == null)
            {
                return;
            }

            GameplayTagContainer ownedTags = GetOwnedTags(owner);
            if (HaveRemovalRequirementsBeenMet(ownedTags))
            {
                owner.RemoveActiveGameplayEffect(handle);
                return;
            }

            bool ongoingRequirementsMet = ongoingTagRequirements.IsEmpty() || ongoingTagRequirements.RequirementsMet(ownedTags);
            owner.SetActiveGameplayEffectInhibit(handle, !ongoingRequirementsMet);
        }

        /// <remarks>UE: UTargetTagRequirementsGameplayEffectComponent::HaveRemovalRequirementsBeenMet</remarks>
        private bool HaveRemovalRequirementsBeenMet(GameplayTagContainer targetOwnedTags)
        {
            return !removalTagRequirements.IsEmpty() && removalTagRequirements.RequirementsMet(targetOwnedTags);
        }

        private static GameplayTagContainer GetOwnedTags(AbilitySystemComponent asc)
        {
            var ownedTags = new GameplayTagContainer();
            asc.GetOwnedGameplayTags(ownedTags);
            return ownedTags;
        }

        private static void AppendTagsUnique(List<GameplayTag> destination, GameplayTagRequirements requirements)
        {
            GameplayTagContainer ignoreTags = requirements.IgnoreTags;
            GameplayTagContainer requireTags = requirements.RequireTags;
            GameplayTagQuery tagQuery = requirements.TagQuery;
            AppendUnique(destination, ignoreTags.GetGameplayTagArray());
            AppendUnique(destination, requireTags.GetGameplayTagArray());
            AppendUnique(destination, tagQuery.GetGameplayTagArray());
        }

        private static void AppendUnique(List<GameplayTag> destination, IReadOnlyList<GameplayTag> source)
        {
            foreach (GameplayTag tag in source)
            {
                if (!destination.Contains(tag))
                {
                    destination.Add(tag);
                }
            }
        }
    }
}
