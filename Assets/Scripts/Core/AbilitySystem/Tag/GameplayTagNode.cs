using System.Collections.Generic;
using Core.Common;

namespace Core.AbilitySystem.Tag
{
    /// <summary>태그 계층 트리를 이루는 노드. 자식을 참조로 링크한다. 트리는 구성 시 1회 만들어지고 이후 매칭은 이 구조를 읽는다.</summary>
    public class GameplayTagNode
    {
        // raw name
        private readonly InternedName _tag;
        private readonly GameplayTagContainer _completeTagWithParents;
        private readonly List<GameplayTagNode> _childNodes = new();

        /// <summary>루트 센티널. 빈 태그를 가지며 최상위 태그들의 공통 부모가 된다.</summary>
        public GameplayTagNode()
        {
            _tag = new InternedName(null);
            _completeTagWithParents = new GameplayTagContainer
            {
                GameplayTags = new List<GameplayTag>(),
                ParentTags = new List<GameplayTag>()
            };
        }

        public GameplayTagNode(InternedName shortTag, InternedName fullTag, GameplayTagNode parentNode)
        {
            _tag = shortTag;

            _completeTagWithParents = new GameplayTagContainer
            {
                GameplayTags = new List<GameplayTag> { new GameplayTag(fullTag) },
                ParentTags = new List<GameplayTag>()
            };

            // 부모가 이미 펼쳐 둔 조상 체인을 그대로 이어받아 재귀 없이 완성한다. 빈 태그 루트는 부모로 치지 않는다.
            if (parentNode != null && !parentNode._tag.Equals(default(InternedName)))
            {
                GameplayTagContainer parentContainer = parentNode._completeTagWithParents;
                _completeTagWithParents.ParentTags.Add(parentContainer.GameplayTags[0]);
                _completeTagWithParents.ParentTags.AddRange(parentContainer.ParentTags);
            }
        }

        public bool TryFindChild(InternedName shortTag, out GameplayTagNode child)
        {
            foreach (GameplayTagNode node in _childNodes)
            {
                if (node._tag.Equals(shortTag))
                {
                    child = node;
                    return true;
                }
            }

            child = null;
            return false;
        }

        public void AddChildNode(GameplayTagNode child)
        {
            _childNodes.Add(child);
        }

        // GetSingleTagContainer

        // GetCompleteTag

        // GetCompleteTagName

        // GetCompleteTagString

        // GetSimpleTagName

        // GetChildTagNodes

        // GetParentTagNode

        // ResetNode
    }
}
