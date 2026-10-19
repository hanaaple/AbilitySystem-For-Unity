using System;
using System.Text;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>태그 컨테이너에 거는 조건. 필수 태그를 모두 갖고, 금지 태그를 하나도 갖지 않고, 질의에 일치해야 충족된다.</summary>
    /// <remarks>UE: FGameplayTagRequirements</remarks>
    [Serializable]
    public class GameplayTagRequirements : IEquatable<GameplayTagRequirements>
    {
        [Tooltip("모두 있어야 하는 태그.")]
        [SerializeField] private GameplayTagContainer _requireTags = new();

        [Tooltip("하나라도 있으면 안 되는 태그.")]
        [SerializeField] private GameplayTagContainer _ignoreTags = new();

        [Tooltip("Require·Ignore만으로 표현할 수 없는 조건. 비어 있으면 검사하지 않는다.")]
        [SerializeField] private GameplayTagQuery _tagQuery;

        /// <summary>모두 있어야 하는 태그.</summary>
        public GameplayTagContainer RequireTags => _requireTags;

        /// <summary>하나라도 있으면 안 되는 태그.</summary>
        public GameplayTagContainer IgnoreTags => _ignoreTags;

        /// <summary>Require·Ignore만으로 표현할 수 없는 조건. 비어 있으면 검사하지 않는다.</summary>
        public GameplayTagQuery TagQuery => _tagQuery;

        /// <remarks>UE: FGameplayTagRequirements::ToString</remarks>
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (_requireTags.Num() > 0)
            {
                builder.Append($"require: {_requireTags.ToStringSimple()} ");
            }

            if (_ignoreTags.Num() > 0)
            {
                builder.Append($"ignore: {_ignoreTags.ToStringSimple()} ");
            }

            if (!_tagQuery.IsEmpty())
            {
                builder.Append(_tagQuery.GetDescription());
            }

            return builder.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTagRequirements other && Equals(other);
        }

        // 내용이 바뀌면 해시도 바뀌므로 해시 키로 쓰지 않는다
        public override int GetHashCode()
        {
            return HashCode.Combine(_requireTags.GetHashCode(), _ignoreTags.GetHashCode(), _tagQuery.GetHashCode());
        }

        /// <summary>세 조건이 모두 같으면 같다.</summary>
        /// <remarks>UE: FGameplayTagRequirements::operator==</remarks>
        public bool Equals(GameplayTagRequirements other)
        {
            if (other == null)
            {
                return false;
            }

            return _requireTags.Equals(other._requireTags) && _ignoreTags.Equals(other._ignoreTags) && _tagQuery.Equals(other._tagQuery);
        }

        /// <summary>필수 태그를 모두 갖고, 금지 태그가 없고, 질의가 비었거나 일치하면 true.</summary>
        /// <remarks>UE: FGameplayTagRequirements::RequirementsMet</remarks>
        public bool RequirementsMet(GameplayTagContainer container)
        {
            bool hasRequired = container.HasAll(_requireTags);
            bool hasIgnored = container.HasAny(_ignoreTags);
            bool matchQuery = _tagQuery.IsEmpty() || _tagQuery.Matches(container);

            return hasRequired && !hasIgnored && matchQuery;
        }

        /// <summary>세 조건이 모두 비었는지.</summary>
        /// <remarks>UE: FGameplayTagRequirements::IsEmpty</remarks>
        public bool IsEmpty()
        {
            return _requireTags.Num() == 0 && _ignoreTags.Num() == 0 && _tagQuery.IsEmpty();
        }

        /// <summary>Require·Ignore를 같은 뜻의 질의로 바꾼다. <see cref="TagQuery"/>는 합치지 않는다. 둘 다 비었으면 빈 질의.</summary>
        /// <remarks>UE: FGameplayTagRequirements::ConvertTagFieldsToTagQuery</remarks>
        public GameplayTagQuery ConvertTagFieldsToTagQuery()
        {
            bool hasRequireTags = !_requireTags.IsEmpty();
            bool hasIgnoreTags = !_ignoreTags.IsEmpty();

            if (!hasIgnoreTags && !hasRequireTags)
            {
                return new GameplayTagQuery();
            }

            GameplayTagQueryExpression requiredTagsQueryExpression = new GameplayTagQueryExpression().AllTagsMatch().AddTags(_requireTags);
            GameplayTagQueryExpression ignoreTagsQueryExpression = new GameplayTagQueryExpression().NoTagsMatch().AddTags(_ignoreTags);

            GameplayTagQueryExpression rootQueryExpression;
            if (hasRequireTags && hasIgnoreTags)
            {
                rootQueryExpression = new GameplayTagQueryExpression().AllExprMatch().AddExpr(requiredTagsQueryExpression).AddExpr(ignoreTagsQueryExpression);
            }
            else if (hasRequireTags)
            {
                rootQueryExpression = requiredTagsQueryExpression;
            }
            else
            {
                rootQueryExpression = ignoreTagsQueryExpression;
            }

            return GameplayTagQuery.BuildQuery(rootQueryExpression);
        }
    }
}
