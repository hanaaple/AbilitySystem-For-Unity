using System.Collections.Generic;
using Core.Common;

namespace Core.AbilitySystem.Tag
{
    /// <summary>태그 계층 트리를 이루는 노드. 자식을 참조로 링크한다. 트리는 구성 시 1회 만들어지고 이후 매칭은 이 구조를 읽는다.</summary>
    public class GameplayTagNode
    {
        // 단순(짧은) 이름. "A.B.C" 노드면 "C".
        private InternedName _tag;
        // 자기 태그 + 모든 부모. "A.B.C" 노드면 명시 태그 [A.B.C], 부모 태그 [A.B, A](바로 위 부모가 먼저). 루트 센티널은 넣지 않는다.
        private readonly GameplayTagContainer _completeTagWithParents;
        private GameplayTagNode _parentNode;
        private readonly List<GameplayTagNode> _childNodes = new();

        /// <summary>루트 센티널. 빈 태그를 가지며 최상위 태그들의 공통 부모가 된다.</summary>
        /// <remarks>UE: FGameplayTagNode::FGameplayTagNode</remarks>
        public GameplayTagNode()
        {
            _tag = default;
            _completeTagWithParents = new GameplayTagContainer();
        }

        /// <param name="inTag">단순(짧은) 이름. <c>"A.B.C"</c> 노드면 <c>"C"</c>.</param>
        /// <param name="inFullTag">전체 경로 이름. 중복 계산을 피하려고 호출측이 이미 만든 것을 넘겨받는다.</param>
        /// <param name="inParentNode">부모 노드. 최상위 태그면 루트 센티널.</param>
        /// <remarks>UE: FGameplayTagNode::FGameplayTagNode</remarks>
        public GameplayTagNode(InternedName inTag, InternedName inFullTag, GameplayTagNode inParentNode)
        {
            _tag = inTag;

            var parentTags = new List<GameplayTag>();

            // 빈 태그 루트는 부모로 치지 않는다 — 최상위 태그면 부모 노드는 null, 부모 태그는 없음.
            if (inParentNode != null && !inParentNode._tag.Equals(default))
            {
                _parentNode = inParentNode;

                // 부모가 이미 펼쳐 둔 조상 체인을 그대로 이어받아 재귀 없이 완성한다(바로 위 부모가 먼저).
                parentTags.Add(inParentNode.GetCompleteTag());
                parentTags.AddRange(inParentNode._completeTagWithParents.ParentTagList);
            }

            // 노드는 트리 구성 중 매니저가 만드므로 등록 확인 없이 태그를 만든다
            GameplayTag completeTag = GameplayTag.Create(inFullTag);
            _completeTagWithParents = new GameplayTagContainer(completeTag, parentTags);
        }

        /// <summary>이 노드의 태그와 부모 태그를 담은 컨테이너. 트리가 소유한 캐시라 읽기 전용으로만 쓴다(수정이 필요하면 사본을 만든다).</summary>
        /// <remarks>UE: FGameplayTagNode::GetSingleTagContainer</remarks>
        public GameplayTagContainer GetSingleTagContainer()
        {
            return _completeTagWithParents;
        }

        /// <summary>이 노드의 전체 경로 태그. 루트 센티널이면 빈 태그.</summary>
        /// <remarks>UE: FGameplayTagNode::GetCompleteTag</remarks>
        public GameplayTag GetCompleteTag()
        {
            return _completeTagWithParents.Num() > 0 ? _completeTagWithParents.GetByIndex(0) : GameplayTag.EmptyTag;
        }

        /// <remarks>UE: FGameplayTagNode::GetCompleteTagName</remarks>
        public InternedName GetCompleteTagName()
        {
            GameplayTag completeTag = GetCompleteTag();
            return completeTag.GetTagName();
        }

        /// <remarks>UE: FGameplayTagNode::GetCompleteTagString</remarks>
        public string GetCompleteTagString()
        {
            GameplayTag completeTag = GetCompleteTag();
            return completeTag.ToString();
        }

        /// <remarks>UE: FGameplayTagNode::GetSimpleTagName</remarks>
        public InternedName GetSimpleTagName()
        {
            return _tag;
        }

        /// <remarks>UE: FGameplayTagNode::GetChildTagNodes</remarks>
        public IReadOnlyList<GameplayTagNode> GetChildTagNodes()
        {
            return _childNodes;
        }

        /// <summary>부모 노드. 최상위 태그면 null(루트 센티널은 부모로 치지 않는다).</summary>
        /// <remarks>UE: FGameplayTagNode::GetParentTagNode</remarks>
        public GameplayTagNode GetParentTagNode()
        {
            return _parentNode;
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

        /// <summary>하위 트리 전체를 끊고 비운다. 트리를 버릴 때 노드 간 참조를 정리한다.</summary>
        /// <remarks>UE: FGameplayTagNode::ResetNode</remarks>
        public void ResetNode()
        {
            _tag = default;
            _completeTagWithParents.Reset();

            foreach (GameplayTagNode childNode in _childNodes)
            {
                childNode.ResetNode();
            }

            _childNodes.Clear();
            _parentNode = null;
        }
    }
}
