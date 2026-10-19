using System;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 부모 GE의 태그를 물려받아 더하고 뺀 결과를 만드는 태그 묶음. 결과(<see cref="CombinedTags"/>) = 부모 결과 − <see cref="Removed"/> + <see cref="Added"/>.
    /// 빼기는 계층으로 판정하고(Removed의 `A`가 부모의 `A.B`도 뺀다), 더하기는 Removed에 정확히 같은 태그가 있을 때만 막힌다.
    /// </summary>
    /// <remarks>UE: FInheritedTagContainer</remarks>
    [Serializable]
    public class InheritedTagContainer
    {
        [Tooltip("물려받은 태그 − Removed + Added. 직접 고치지 않는다(편집하면 다시 계산된다). 부모 GE가 없으면 Added에서 Removed와 정확히 같은 태그를 뺀 것.")]
        [SerializeField] private GameplayTagContainer _combinedTags = new();

        [Tooltip("부모 태그에 더해 이 GE가 가지는 태그 (UE: Add to Inherited).")]
        [SerializeField] private GameplayTagContainer _added = new();

        [Tooltip("부모에게 물려받은 태그 중 뺄 태그. 대상이 가진 태그를 빼지는 않는다 (UE: Remove from Inherited).")]
        [SerializeField] private GameplayTagContainer _removed = new();

        /// <summary>물려받은 태그 − 뺀 태그 + 더한 태그. <see cref="UpdateInheritedTagProperties"/>가 다시 만든다.</summary>
        public GameplayTagContainer CombinedTags => _combinedTags;

        /// <summary>부모 태그에 더해 이 GE가 가지는 태그.</summary>
        public GameplayTagContainer Added => _added;

        /// <summary>부모에게 물려받은 태그 중 뺄 태그. 대상이 가진 태그를 빼는 게 아니라 <see cref="CombinedTags"/> 결과만 바꾼다.</summary>
        public GameplayTagContainer Removed => _removed;

        /// <summary>부모의 결과 태그와 이 묶음의 Added·Removed로 <see cref="CombinedTags"/>를 다시 만든다. 부모가 없으면 null.</summary>
        /// <remarks>UE: FInheritedTagContainer::UpdateInheritedTagProperties</remarks>
        public void UpdateInheritedTagProperties(InheritedTagContainer parent)
        {
            _combinedTags.Reset();

            if (parent != null)
            {
                foreach (GameplayTag parentTag in parent._combinedTags)
                {
                    if (!parentTag.MatchesAny(_removed))
                    {
                        _combinedTags.AddTag(parentTag);
                    }
                }
            }

            foreach (GameplayTag addedTag in _added)
            {
                // 정확히 같은 태그만 막는다 — `A`를 빼면서 `A.B`는 더할 수 있게
                if (!_removed.HasTagExact(addedTag))
                {
                    _combinedTags.AddTag(addedTag);
                }
            }
        }

        /// <summary>이 묶음의 Added·Removed를 주어진 컨테이너에 반영한다. 컨테이너가 처음 설정한 부모일 필요는 없다.</summary>
        /// <remarks>UE: FInheritedTagContainer::ApplyTo</remarks>
        public void ApplyTo(GameplayTagContainer applyToContainer)
        {
            if (applyToContainer.IsEmpty() && !_combinedTags.IsEmpty())
            {
                // 빈 컨테이너면 이미 계산해 둔 결과를 그대로 쓴다
                applyToContainer.AppendTags(_combinedTags);
                return;
            }

            // 계층으로 걸리는 기존 태그와, Removed에 정확히 있는 Added 태그를 뺀다
            GameplayTagContainer removesThatApply = _removed.Filter(applyToContainer);
            GameplayTagContainer removeOverridesAdd = _added.FilterExact(_removed);
            removesThatApply.AppendTags(removeOverridesAdd);

            applyToContainer.AppendTags(_added);
            applyToContainer.RemoveTags(removesThatApply);
        }

        /// <summary>물려받은 태그에 더해 가질 태그를 넣는다. 같은 태그를 빼던 설정은 지운다.</summary>
        /// <remarks>UE: FInheritedTagContainer::AddTag</remarks>
        public void AddTag(GameplayTag tagToAdd)
        {
            _removed.RemoveTag(tagToAdd);
            _added.AddTag(tagToAdd);
            _combinedTags.AddTag(tagToAdd);
        }

        /// <summary>물려받은 태그에서 뺄 태그를 넣는다. 같은 태그를 더하던 설정은 지운다.</summary>
        /// <remarks>UE: FInheritedTagContainer::RemoveTag</remarks>
        public void RemoveTag(GameplayTag tagToRemove)
        {
            _added.RemoveTag(tagToRemove);
            _removed.AddTag(tagToRemove);
            _combinedTags.RemoveTag(tagToRemove);
        }
    }
}
