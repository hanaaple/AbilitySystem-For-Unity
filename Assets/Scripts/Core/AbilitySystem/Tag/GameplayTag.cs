using System;
using System.Collections.Generic;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>계층 문자열(예: <c>Status.Debuff.Stun</c>)로 표현되는 태그 식별자. 등록된 태그만 얻을 수 있다 — 이름으로는 <see cref="RequestGameplayTag(string)"/>, 코드 정의로는 <see cref="NativeGameplayTag"/>를 거친다.</summary>
    [Serializable]
    public struct GameplayTag : IEquatable<GameplayTag>
    {
        /// <summary>빈 태그(이름 없음). 루트 센티널처럼 태그가 없음을 나타낼 때 돌려준다.</summary>
        /// <remarks>UE: FGameplayTag::EmptyTag</remarks>
        public static readonly GameplayTag EmptyTag = default;

        // 에디터 드로어가 직렬화 경로 "name.name"(이 필드 → InternedName.name)으로 읽고 쓴다. 이름을 바꾸면 GameplayTagPropertyHandle도 바꿔야 한다.
        [SerializeField] private InternedName name;

        /// <remarks>UE: FGameplayTag::FGameplayTag</remarks>
        private GameplayTag(InternedName name)
        {
            this.name = name;
        }

        /// <remarks>UE: FGameplayTag::operator==</remarks>
        public static bool operator ==(GameplayTag left, GameplayTag right)
        {
            return left.Equals(right);
        }

        /// <remarks>UE: FGameplayTag::operator!=</remarks>
        public static bool operator !=(GameplayTag left, GameplayTag right)
        {
            return !left.Equals(right);
        }

        /// <summary>이름 순 비교. 끝의 숫자 접미사는 수 크기로 비교한다(<c>A.2</c> &lt; <c>A.10</c>).</summary>
        /// <remarks>UE: FGameplayTag::operator&lt;</remarks>
        public static bool operator <(GameplayTag left, GameplayTag right)
        {
            throw new NotImplementedException();
        }

        // C#은 <와 >를 쌍으로만 정의할 수 있어 UE에 없는 >도 둔다
        public static bool operator >(GameplayTag left, GameplayTag right)
        {
            return right < left;
        }

        /// <remarks>UE: FGameplayTag::operator==</remarks>
        public bool Equals(GameplayTag other)
        {
            return name.Equals(other.name);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTag other && Equals(other);
        }

        /// <remarks>UE: GetTypeHash(const FGameplayTag&)</remarks>
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        /// <remarks>UE: FGameplayTag::ToString</remarks>
        public override string ToString()
        {
            return name.ToString() ?? string.Empty;
        }

        /// <summary>태그 이름.</summary>
        /// <remarks>UE: FGameplayTag::GetTagName</remarks>
        public InternedName GetTagName()
        {
            return name;
        }

        /// <summary>이름으로 등록된 태그를 찾는다. 미등록이면 빈 태그(default)를 돌려준다.</summary>
        /// <param name="errorIfNotFound">true면 미등록 이름을 이름당 한 번 에러로 알린다.</param>
        /// <remarks>UE: FGameplayTag::RequestGameplayTag</remarks>
        public static GameplayTag RequestGameplayTag(InternedName tagName, bool errorIfNotFound = true)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.RequestGameplayTag(tagName, errorIfNotFound);
        }

        /// <summary>이름으로 등록된 태그를 찾는다. 미등록이면 빈 태그(default)를 돌려준다.</summary>
        /// <param name="errorIfNotFound">true면 미등록 이름을 이름당 한 번 에러로 알린다.</param>
        /// <remarks>UE: FGameplayTag::RequestGameplayTag</remarks>
        public static GameplayTag RequestGameplayTag(string tagName, bool errorIfNotFound = true)
        {
            return RequestGameplayTag(new InternedName(tagName), errorIfNotFound);
        }

        /// <summary>문자열이 태그 사전에 추가될 수 있는 형식인지(문법 오류가 없는지).</summary>
        /// <remarks>UE: FGameplayTag::IsValidGameplayTagString</remarks>
        public static bool IsValidGameplayTagString(string tagString)
        {
            return IsValidGameplayTagString(tagString, out _, out _);
        }

        /// <summary>문자열이 태그 사전에 추가될 수 있는 형식인지(문법 오류가 없는지).</summary>
        /// <param name="outError">형식이 틀렸으면 오류 메시지.</param>
        /// <param name="outFixedString">형식이 틀렸으면 고친 문자열. 고칠 수 없으면 빈 문자열.</param>
        /// <remarks>UE: FGameplayTag::IsValidGameplayTagString</remarks>
        public static bool IsValidGameplayTagString(string tagString, out string outError, out string outFixedString)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.IsValidGameplayTagString(tagString, out outError, out outFixedString);
        }

        /// <summary>빈 태그(이름 없음)가 아닌지. 이름이 있어도 트리에 등록됐다는 보장은 아니다(로드된 데이터의 미등록 이름).</summary>
        /// <remarks>UE: FGameplayTag::IsValid</remarks>
        public bool IsValid()
        {
            return !name.IsNullOrEmpty();
        }

        /// <summary>이 태그가 대상 태그이거나 그 자식인지. <c>"A.1".MatchesTag("A")</c>는 true, 반대는 false.</summary>
        /// O(1)
        /// <remarks>UE: FGameplayTag::MatchesTag</remarks>
        public bool MatchesTag(GameplayTag tagToCheck)
        {
            GameplayTagNode tagNode = FindRegisteredNode("MatchesTag");
            if (tagNode == null)
            {
                return false;
            }

            GameplayTagContainer singleContainer = tagNode.GetSingleTagContainer();
            return singleContainer.HasTag(tagToCheck);
        }

        /// <summary>이 태그가 대상 태그와 정확히 같은지(부모 매칭 없음). 대상이 유효하지 않으면 false.</summary>
        /// <remarks>UE: FGameplayTag::MatchesTagExact</remarks>
        public bool MatchesTagExact(GameplayTag tagToCheck)
        {
            if (!tagToCheck.IsValid())
            {
                return false;
            }

            return Equals(tagToCheck);
        }

        /// <summary>두 태그가 루트부터 몇 단계까지 같은지. <c>"A.B.C"</c>와 <c>"A.B.D"</c>는 2.</summary>
        /// <remarks>UE: FGameplayTag::MatchesTagDepth</remarks>
        public int MatchesTagDepth(GameplayTag tagToCheck)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.GameplayTagsMatchDepth(this, tagToCheck);
        }

        /// <summary>대상 컨테이너의 태그 중 어느 것과든 매칭되는지(이 태그가 그 태그이거나 자식). 대상이 비어 있으면 false.</summary>
        /// <remarks>UE: FGameplayTag::MatchesAny</remarks>
        public bool MatchesAny(GameplayTagContainer containerToCheck)
        {
            GameplayTagNode tagNode = FindRegisteredNode("MatchesAny");
            if (tagNode == null)
            {
                return false;
            }

            GameplayTagContainer singleContainer = tagNode.GetSingleTagContainer();
            return singleContainer.HasAny(containerToCheck);
        }

        /// <summary>대상 컨테이너에 이 태그가 명시로 들어 있는지. 대상이 비어 있으면 false.</summary>
        /// <remarks>UE: FGameplayTag::MatchesAnyExact</remarks>
        public bool MatchesAnyExact(GameplayTagContainer containerToCheck)
        {
            if (containerToCheck.IsEmpty())
            {
                return false;
            }

            return containerToCheck.HasTagExact(this);
        }

        /// <summary>이 태그와 그 부모를 담은 컨테이너의 사본. 미등록이면 빈 컨테이너.</summary>
        /// <remarks>UE: FGameplayTag::GetSingleTagContainer</remarks>
        public GameplayTagContainer GetSingleTagContainer()
        {
            GameplayTagNode tagNode = FindRegisteredNode("GetSingleTagContainer");
            if (tagNode == null)
            {
                return new GameplayTagContainer();
            }

            // 노드 캐시를 그대로 내주면 호출측 수정이 트리를 오염시키므로 사본을 준다
            return new GameplayTagContainer(tagNode.GetSingleTagContainer());
        }

        /// <summary>바로 위 부모 태그. 최상위·미등록이면 빈 태그.</summary>
        /// <remarks>UE: FGameplayTag::RequestDirectParent</remarks>
        public GameplayTag RequestDirectParent()
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.RequestGameplayTagDirectParent(this);
        }

        /// <summary>이 태그와 모든 부모를 명시로 담은 새 컨테이너.</summary>
        /// <remarks>UE: FGameplayTag::GetGameplayTagParents</remarks>
        public GameplayTagContainer GetGameplayTagParents()
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.RequestGameplayTagParents(this);
        }

        /// <summary>이름을 '.'로 쪼개 부모 태그를 바로 위 부모부터 중복 없이 더한다. 매니저(트리)를 거치지 않는다.</summary>
        /// <remarks>UE: FGameplayTag::ParseParentTags</remarks>
        public void ParseParentTags(List<GameplayTag> uniqueParentTags)
        {
            // 노드 캐시의 부모 순서(바로 위 부모 먼저)와 같아야 ExtractParentTags 결과와 일치한다
            string rawTag = name.ToString();
            if (rawTag == null)
            {
                return;
            }

            int dotIndex = rawTag.LastIndexOf('.');
            while (dotIndex >= 0)
            {
                rawTag = rawTag.Substring(0, dotIndex);
                dotIndex = rawTag.LastIndexOf('.');

                var parentTag = new GameplayTag(new InternedName(rawTag));
                if (!uniqueParentTags.Contains(parentTag))
                {
                    uniqueParentTags.Add(parentTag);
                }
            }
        }

        /// <summary>마지막 구간 이름. <c>"A.B.C"</c>는 <c>"C"</c>.</summary>
        /// <remarks>UE: FGameplayTag::GetTagLeafName</remarks>
        public InternedName GetTagLeafName()
        {
            string rawTag = name.ToString();
            if (rawTag == null)
            {
                return name;
            }

            int dotIndex = rawTag.LastIndexOf('.');
            return dotIndex < 0 ? name : new InternedName(rawTag.Substring(dotIndex + 1));
        }

        // 등록 여부를 확인하지 않는 생성 통로라 매니저(트리 구성·조회)만 쓴다.
        // 지금은 단일 어셈블리라 internal이 실제로 막지 못하며, AbilitySystem asmdef 분리 시 접근 제한이 실효를 갖는다.
        internal static GameplayTag Create(InternedName tagName)
        {
            return new GameplayTag(tagName);
        }

        // 매칭 함수는 노드 캐시를 쓰므로 미등록 태그는 답할 수 없다. 이름이 있는데 노드가 없으면 로드된 데이터의 낡은 태그라 알린다.
        private GameplayTagNode FindRegisteredNode(string callerName)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            GameplayTagNode tagNode = manager.FindTagNode(this);
            if (tagNode == null && IsValid())
            {
                Debug.LogError($"{callerName}: 미등록 태그 {this}입니다. 등록된 태그만 매칭에 쓸 수 있습니다.");
            }

            return tagNode;
        }
    }
}
