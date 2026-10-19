using System;
using System.Collections.Generic;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem
{
    /// <param name="tag">개수가 바뀐 태그. 명시로 더한 태그의 부모일 수 있다.</param>
    /// <param name="newCount">바뀐 뒤 개수(부모 포함).</param>
    /// <remarks>UE: FOnGameplayEffectTagCountChanged</remarks>
    public delegate void OnGameplayEffectTagCountChanged(GameplayTag tag, int newCount);

    /// <summary>
    /// 태그가 몇 번 더해졌는지 센다. 명시로 더한 태그와 함께 그 부모 태그의 개수도 세어, 자식 태그만 있어도 부모 태그로 매칭된다.
    /// 어떤 태그(부모 포함)의 개수가 바뀌면 그 태그에 등록된 이벤트를 부른다.
    /// </summary>
    /// <remarks>UE: FGameplayTagCountContainer</remarks>
    public class GameplayTagCountContainer
    {
        // 등록된 태그만 들어 있다
        private readonly Dictionary<GameplayTag, DelegateInfo> _gameplayTagEventMap = new();

        // 부모 포함 개수. A.B와 A.C를 더하면 A는 2다.
        private readonly Dictionary<GameplayTag, int> _gameplayTagCountMap = new();

        // 위 맵과 합치면 자식이 올린 부모 개수와 섞여, 명시 태그를 언제 목록에서 빼야 할지 알 수 없다
        private readonly Dictionary<GameplayTag, int> _explicitTagCountMap = new();

        private readonly GameplayTagContainer _explicitTags = new();

        /// <summary>태그를 하나라도 가지면 true. 자식 태그만 있어도 부모 태그로 매칭된다.</summary>
        /// <remarks>UE: FGameplayTagCountContainer::HasMatchingGameplayTag</remarks>
        public bool HasMatchingGameplayTag(GameplayTag tagToCheck)
        {
            return _gameplayTagCountMap.GetValueOrDefault(tagToCheck) > 0;
        }

        /// <summary>컨테이너의 태그마다 개수를 <paramref name="countDelta"/>만큼 바꾼다. 부모 태그 재계산과 이벤트는 모두 바꾼 뒤 한 번에 한다.</summary>
        /// <remarks>UE: FGameplayTagCountContainer::UpdateTagCount</remarks>
        public void UpdateTagCount(GameplayTagContainer container, int countDelta)
        {
            if (countDelta == 0)
            {
                return;
            }

            bool updatedAny = false;
            List<Action> deferredTagChangeDelegates = new List<Action>();
            foreach (GameplayTag tag in container)
            {
                updatedAny |= UpdateTagMapDeferredParentRemovalInternal(tag, countDelta, deferredTagChangeDelegates);
            }

            if (updatedAny && countDelta < 0)
            {
                _explicitTags.FillParentTags();
            }

            foreach (Action tagChangeDelegate in deferredTagChangeDelegates)
            {
                tagChangeDelegate();
            }
        }

        /// <summary>태그 개수를 <paramref name="countDelta"/>만큼 바꾼다.</summary>
        /// <returns>이 태그나 부모 태그가 새로 생기거나 완전히 없어졌으면 true. 개수만 바뀌었으면(1→2) false.</returns>
        /// <remarks>UE: FGameplayTagCountContainer::UpdateTagCount</remarks>
        public bool UpdateTagCount(GameplayTag tag, int countDelta)
        {
            if (tag.IsValid() && countDelta != 0)
            {
                return UpdateTagMapInternal(tag, countDelta);
            }

            return false;
        }

        /// <summary>명시 개수를 <paramref name="newCount"/>로 맞춘다.</summary>
        /// <returns><see cref="UpdateTagCount(GameplayTag, int)"/>와 같다.</returns>
        /// <remarks>UE: FGameplayTagCountContainer::SetTagCount</remarks>
        public bool SetTagCount(GameplayTag tag, int newCount)
        {
            if (!tag.IsValid())
            {
                return false;
            }

            int existingCount = _explicitTagCountMap.GetValueOrDefault(tag);
            int countDelta = newCount - existingCount;
            if (countDelta != 0)
            {
                return UpdateTagMapInternal(tag, countDelta);
            }

            return false;
        }

        /// <summary>자식 태그 개수를 포함한 개수. A.B와 A.C를 더했으면 A는 2.</summary>
        /// <remarks>UE: FGameplayTagCountContainer::GetTagCount</remarks>
        public int GetTagCount(GameplayTag tag)
        {
            return _gameplayTagCountMap.GetValueOrDefault(tag);
        }

        /// <summary>태그 개수가 바뀔 때 부를 콜백을 등록한다. 자식 태그가 바뀌어 부모 태그 개수가 바뀌어도 불린다.</summary>
        /// <remarks>UE: FGameplayTagCountContainer::RegisterGameplayTagEvent</remarks>
        public void RegisterGameplayTagEvent(GameplayTag tag, OnGameplayEffectTagCountChanged callback, GameplayTagEventType eventType = GameplayTagEventType.NewOrRemoved)
        {
            if (!_gameplayTagEventMap.TryGetValue(tag, out DelegateInfo info))
            {
                info = new DelegateInfo();
                _gameplayTagEventMap.Add(tag, info);
            }

            if (eventType == GameplayTagEventType.NewOrRemoved)
            {
                info.OnNewOrRemove += callback;
            }
            else
            {
                info.OnAnyChange += callback;
            }
        }

        /// <summary>등록한 콜백을 뺀다. 같은 태그·같은 종류로 등록했던 것이어야 한다.</summary>
        /// <returns>등록돼 있어서 뺐으면 true.</returns>
        /// <remarks>UE: UAbilitySystemComponent::UnregisterGameplayTagEvent</remarks>
        public bool UnregisterGameplayTagEvent(OnGameplayEffectTagCountChanged callback, GameplayTag tag, GameplayTagEventType eventType = GameplayTagEventType.NewOrRemoved)
        {
            if (!_gameplayTagEventMap.TryGetValue(tag, out DelegateInfo info))
            {
                return false;
            }

            if (eventType == GameplayTagEventType.NewOrRemoved)
            {
                OnGameplayEffectTagCountChanged before = info.OnNewOrRemove;
                info.OnNewOrRemove -= callback;
                return before != info.OnNewOrRemove;
            }

            OnGameplayEffectTagCountChanged beforeAny = info.OnAnyChange;
            info.OnAnyChange -= callback;
            return beforeAny != info.OnAnyChange;
        }

        /// <summary>명시로 더한 태그. 읽기 전용으로 쓴다.</summary>
        /// <remarks>UE: FGameplayTagCountContainer::GetExplicitGameplayTags</remarks>
        public GameplayTagContainer GetExplicitGameplayTags()
        {
            return _explicitTags;
        }

        /// <remarks>UE: FGameplayTagCountContainer::UpdateTagMap_Internal</remarks>
        private bool UpdateTagMapInternal(GameplayTag tag, int countDelta)
        {
            if (!UpdateExplicitTags(tag, countDelta, false))
            {
                return false;
            }

            List<Action> deferredTagChangeDelegates = new List<Action>();
            bool significantChange = GatherTagChangeDelegates(tag, countDelta, deferredTagChangeDelegates);
            foreach (Action tagChangeDelegate in deferredTagChangeDelegates)
            {
                tagChangeDelegate();
            }

            return significantChange;
        }

        /// <summary>부모 태그 재계산과 이벤트를 호출자에게 미룬다. 호출자가 FillParentTags 뒤 <paramref name="deferredTagChangeDelegates"/>를 불러야 한다.</summary>
        /// <remarks>UE: FGameplayTagCountContainer::UpdateTagMapDeferredParentRemoval_Internal</remarks>
        private bool UpdateTagMapDeferredParentRemovalInternal(GameplayTag tag, int countDelta, List<Action> deferredTagChangeDelegates)
        {
            if (!UpdateExplicitTags(tag, countDelta, true))
            {
                return false;
            }

            return GatherTagChangeDelegates(tag, countDelta, deferredTagChangeDelegates);
        }

        /// <returns>개수를 바꿨으면 true. 명시로 더한 적 없는 태그를 빼려 하면 false.</returns>
        /// <remarks>UE: FGameplayTagCountContainer::UpdateExplicitTags</remarks>
        private bool UpdateExplicitTags(GameplayTag tag, int countDelta, bool deferParentTagsOnRemove)
        {
            bool tagAlreadyExplicitlyExists = _explicitTags.HasTagExact(tag);

            if (!tagAlreadyExplicitlyExists)
            {
                if (countDelta > 0)
                {
                    _explicitTags.AddTag(tag);
                }
                else
                {
                    // 부모로서만 들어 있는 태그를 빼면 자식 개수와 어긋나므로 막는다
                    if (_explicitTags.HasTag(tag))
                    {
                        Debug.LogWarning($"[GameplayTagCountContainer] 태그 {tag}를 빼려 했지만 명시로 더한 태그가 아닙니다(자식 태그의 부모로만 들어 있음).");
                    }

                    return false;
                }
            }

            int existingCount = Math.Max(_explicitTagCountMap.GetValueOrDefault(tag) + countDelta, 0);
            _explicitTagCountMap[tag] = existingCount;

            if (existingCount <= 0)
            {
                _explicitTags.RemoveTag(tag, deferParentTagsOnRemove);
            }

            return true;
        }

        /// <returns>이 태그나 부모 태그가 새로 생기거나 완전히 없어졌으면 true.</returns>
        /// <remarks>UE: FGameplayTagCountContainer::GatherTagChangeDelegates</remarks>
        private bool GatherTagChangeDelegates(GameplayTag tag, int countDelta, List<Action> tagChangeDelegates)
        {
            GameplayTagContainer tagAndParentsContainer = tag.GetGameplayTagParents();
            bool createdSignificantChange = false;

            foreach (GameplayTag curTag in tagAndParentsContainer)
            {
                int oldCount = _gameplayTagCountMap.GetValueOrDefault(curTag);
                int newTagCount = Math.Max(oldCount + countDelta, 0);
                _gameplayTagCountMap[curTag] = newTagCount;

                bool significantChange = oldCount == 0 || newTagCount == 0;
                createdSignificantChange |= significantChange;

                if (!_gameplayTagEventMap.TryGetValue(curTag, out DelegateInfo delegateInfo))
                {
                    continue;
                }

                // 지금 등록된 콜백을 붙잡아 둔다. 콜백 안에서 등록·해제해도 이번 알림 대상은 바뀌지 않는다
                OnGameplayEffectTagCountChanged onAnyChange = delegateInfo.OnAnyChange;
                if (onAnyChange != null)
                {
                    tagChangeDelegates.Add(() => onAnyChange(curTag, newTagCount));
                }

                OnGameplayEffectTagCountChanged onNewOrRemove = delegateInfo.OnNewOrRemove;
                if (significantChange && onNewOrRemove != null)
                {
                    tagChangeDelegates.Add(() => onNewOrRemove(curTag, newTagCount));
                }
            }

            return createdSignificantChange;
        }

        private sealed class DelegateInfo
        {
            public OnGameplayEffectTagCountChanged OnNewOrRemove;
            public OnGameplayEffectTagCountChanged OnAnyChange;
        }
    }
}
