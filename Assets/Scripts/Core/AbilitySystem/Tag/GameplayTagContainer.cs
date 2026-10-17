using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>여러 <see cref="GameplayTag"/>를 담는 컨테이너. 소유 태그 집합·다중 매칭 질의의 단위. 명시 태그와 그 부모 태그를 함께 들고 있어 계층 매칭을 트리 탐색 없이 한다.</summary>
    [Serializable]
    public class GameplayTagContainer : IEquatable<GameplayTagContainer>, ISerializationCallbackReceiver
    {
        // 에디터 드로어가 이 직렬화 필드 이름으로 읽고 쓴다. 이름을 바꾸면 GameplayTagPropertyHandle도 바꿔야 한다.
        [SerializeField] private List<GameplayTag> _gameplayTags = new();

        // 명시 태그에서 언제든 다시 만들 수 있으므로 저장하지 않는다. 부모 매칭(HasTag 등) 가속용 캐시.
        [NonSerialized] private List<GameplayTag> _parentTags = new();

        internal IReadOnlyList<GameplayTag> ParentTagList => _parentTags;

        /// <remarks>UE: FGameplayTagContainer::FGameplayTagContainer</remarks>
        public GameplayTagContainer()
        {
        }

        /// <summary>태그 하나를 담은 컨테이너를 만든다.</summary>
        /// <remarks>UE: FGameplayTagContainer::FGameplayTagContainer</remarks>
        public GameplayTagContainer(GameplayTag tag)
        {
            AddTag(tag);
        }

        /// <summary>다른 컨테이너의 사본을 만든다.</summary>
        /// <remarks>UE: FGameplayTagContainer::FGameplayTagContainer</remarks>
        public GameplayTagContainer(GameplayTagContainer other)
        {
            _gameplayTags.AddRange(other._gameplayTags);
            _parentTags.AddRange(other._parentTags);
        }

        // 노드 트리 구성용. 트리 구성 중에는 매니저 조회(ExtractParentTags)를 할 수 없어 부모 체인을 직접 받는다.
        internal GameplayTagContainer(GameplayTag completeTag, IEnumerable<GameplayTag> parentTags)
        {
            _gameplayTags.Add(completeTag);
            _parentTags.AddRange(parentTags);
        }

        /// <summary>순서와 무관하게 같은 명시 태그를 가지면 같다.</summary>
        /// <remarks>UE: FGameplayTagContainer::operator==</remarks>
        public bool Equals(GameplayTagContainer other)
        {
            if (other == null)
            {
                return false;
            }

            return _gameplayTags.Count == other._gameplayTags.Count && HasAllExact(other);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTagContainer other && Equals(other);
        }

        // 순서와 무관한 Equals와 맞추려고 순서와 무관한 XOR로 합친다. 내용이 바뀌면 해시도 바뀌므로 해시 키로 쓰지 않는다.
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (GameplayTag tag in _gameplayTags)
            {
                hash ^= tag.GetHashCode();
            }

            return hash;
        }

        /// <remarks>UE: FGameplayTagContainer::ToString</remarks>
        public override string ToString()
        {
            return ToStringSimple();
        }

        /// <summary>태그 배열로 컨테이너를 만든다.</summary>
        /// <remarks>UE: FGameplayTagContainer::CreateFromArray</remarks>
        public static GameplayTagContainer CreateFromArray(IEnumerable<GameplayTag> sourceTags)
        {
            var container = new GameplayTagContainer();
            container._gameplayTags.AddRange(sourceTags);
            container.FillParentTags();
            return container;
        }

        /// <summary>태그가 명시 또는 부모로 들어 있는지. <c>{"A.1"}.HasTag("A")</c>는 true. 유효하지 않은 태그는 항상 false.</summary>
        /// <remarks>UE: FGameplayTagContainer::HasTag</remarks>
        public bool HasTag(GameplayTag tagToCheck)
        {
            if (!tagToCheck.IsValid())
            {
                return false;
            }

            return _gameplayTags.Contains(tagToCheck) || _parentTags.Contains(tagToCheck);
        }

        /// <summary>태그가 명시로 들어 있는지(부모 매칭 없음). <c>{"A.1"}.HasTagExact("A")</c>는 false. 유효하지 않은 태그는 항상 false.</summary>
        /// <remarks>UE: FGameplayTagContainer::HasTagExact</remarks>
        public bool HasTagExact(GameplayTag tagToCheck)
        {
            if (!tagToCheck.IsValid())
            {
                return false;
            }

            return _gameplayTags.Contains(tagToCheck);
        }

        /// <summary>대상 컨테이너의 태그 중 하나라도 명시 또는 부모로 들어 있는지. 대상이 비어 있으면 false.</summary>
        /// <remarks>UE: FGameplayTagContainer::HasAny</remarks>
        public bool HasAny(GameplayTagContainer containerToCheck)
        {
            if (containerToCheck.IsEmpty())
            {
                return false;
            }

            foreach (GameplayTag otherTag in containerToCheck._gameplayTags)
            {
                if (_gameplayTags.Contains(otherTag) || _parentTags.Contains(otherTag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>대상 컨테이너의 태그 중 하나라도 명시로 들어 있는지. 대상이 비어 있으면 false.</summary>
        /// <remarks>UE: FGameplayTagContainer::HasAnyExact</remarks>
        public bool HasAnyExact(GameplayTagContainer containerToCheck)
        {
            if (containerToCheck.IsEmpty())
            {
                return false;
            }

            foreach (GameplayTag otherTag in containerToCheck._gameplayTags)
            {
                if (_gameplayTags.Contains(otherTag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>대상 컨테이너의 태그가 모두 명시 또는 부모로 들어 있는지. 대상이 비어 있으면 실패한 검사가 없으므로 true.</summary>
        /// <remarks>UE: FGameplayTagContainer::HasAll</remarks>
        public bool HasAll(GameplayTagContainer containerToCheck)
        {
            if (containerToCheck.IsEmpty())
            {
                return true;
            }

            foreach (GameplayTag otherTag in containerToCheck._gameplayTags)
            {
                if (!_gameplayTags.Contains(otherTag) && !_parentTags.Contains(otherTag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>대상 컨테이너의 태그가 모두 명시로 들어 있는지. 대상이 비어 있으면 true.</summary>
        /// <remarks>UE: FGameplayTagContainer::HasAllExact</remarks>
        public bool HasAllExact(GameplayTagContainer containerToCheck)
        {
            if (containerToCheck.IsEmpty())
            {
                return true;
            }

            foreach (GameplayTag otherTag in containerToCheck._gameplayTags)
            {
                if (!_gameplayTags.Contains(otherTag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>명시 태그 수.</summary>
        /// <remarks>UE: FGameplayTagContainer::Num</remarks>
        public int Num()
        {
            return _gameplayTags.Count;
        }

        /// <summary>명시 태그가 하나라도 있는지.</summary>
        /// <remarks>UE: FGameplayTagContainer::IsValid</remarks>
        public bool IsValid()
        {
            return _gameplayTags.Count > 0;
        }

        /// <summary>명시 태그가 없는지.</summary>
        /// <remarks>UE: FGameplayTagContainer::IsEmpty</remarks>
        public bool IsEmpty()
        {
            return _gameplayTags.Count == 0;
        }

        /// <summary>명시 태그와 그 부모 태그를 모두 명시로 담은 새 컨테이너.</summary>
        /// <remarks>UE: FGameplayTagContainer::GetGameplayTagParents</remarks>
        public GameplayTagContainer GetGameplayTagParents()
        {
            var resultContainer = new GameplayTagContainer();
            resultContainer._gameplayTags.AddRange(_gameplayTags);

            foreach (GameplayTag tag in _parentTags)
            {
                AddUnique(resultContainer._gameplayTags, tag);
            }

            return resultContainer;
        }

        /// <summary>대상 컨테이너의 태그 중 어느 것과든 매칭되는(부모 포함) 명시 태그만 담은 새 컨테이너.</summary>
        /// <remarks>UE: FGameplayTagContainer::Filter</remarks>
        public GameplayTagContainer Filter(GameplayTagContainer otherContainer)
        {
            var resultContainer = new GameplayTagContainer();
            foreach (GameplayTag tag in _gameplayTags)
            {
                if (tag.MatchesAny(otherContainer))
                {
                    resultContainer.AddTagFast(tag);
                }
            }

            return resultContainer;
        }

        /// <summary>대상 컨테이너에 정확히 들어 있는 명시 태그만 담은 새 컨테이너.</summary>
        /// <remarks>UE: FGameplayTagContainer::FilterExact</remarks>
        public GameplayTagContainer FilterExact(GameplayTagContainer otherContainer)
        {
            var resultContainer = new GameplayTagContainer();
            foreach (GameplayTag tag in _gameplayTags)
            {
                if (tag.MatchesAnyExact(otherContainer))
                {
                    resultContainer.AddTagFast(tag);
                }
            }

            return resultContainer;
        }

        /// <summary>이 컨테이너가 질의를 만족하는지.</summary>
        /// <remarks>UE: FGameplayTagContainer::MatchesQuery</remarks>
        public bool MatchesQuery(GameplayTagQuery query)
        {
            return query.Matches(this);
        }

        /// <summary>다른 컨테이너의 명시·부모 태그를 중복 없이 더한다.</summary>
        /// <remarks>UE: FGameplayTagContainer::AppendTags</remarks>
        public void AppendTags(GameplayTagContainer other)
        {
            if (other.IsEmpty())
            {
                return;
            }

            foreach (GameplayTag otherTag in other._gameplayTags)
            {
                AddUnique(_gameplayTags, otherTag);
            }

            foreach (GameplayTag otherTag in other._parentTags)
            {
                AddUnique(_parentTags, otherTag);
            }
        }

        /// <summary>A의 명시 태그 중 B의 어느 태그와든 매칭되는(부모 포함) 것만 더한다.</summary>
        /// <remarks>UE: FGameplayTagContainer::AppendMatchingTags</remarks>
        public void AppendMatchingTags(GameplayTagContainer otherA, GameplayTagContainer otherB)
        {
            foreach (GameplayTag otherATag in otherA._gameplayTags)
            {
                if (otherATag.MatchesAny(otherB))
                {
                    AddTag(otherATag);
                }
            }
        }

        /// <summary>태그를 중복 없이 더하고 부모 태그를 갱신한다. 유효하지 않은 태그는 무시한다.</summary>
        /// <remarks>UE: FGameplayTagContainer::AddTag</remarks>
        public void AddTag(GameplayTag tagToAdd)
        {
            if (!tagToAdd.IsValid())
            {
                return;
            }

            AddUnique(_gameplayTags, tagToAdd);
            GameplayTagsManager manager = GameplayTagsManager.Get();
            manager.ExtractParentTags(tagToAdd, _parentTags);
        }

        /// <summary>중복·유효성 검사 없이 태그를 더한다. 이미 없다는 것이 보장될 때만 쓴다.</summary>
        /// <remarks>UE: FGameplayTagContainer::AddTagFast</remarks>
        public void AddTagFast(GameplayTag tagToAdd)
        {
            _gameplayTags.Add(tagToAdd);
            GameplayTagsManager manager = GameplayTagsManager.Get();
            manager.ExtractParentTags(tagToAdd, _parentTags);
        }

        /// <summary>가장 구체적인 태그만 남도록 더한다. 더하는 태그의 부모가 명시돼 있으면 그 부모를 빼고, 이미 더 구체적인 자식이 있으면 실패한다.</summary>
        /// <returns>태그가 컨테이너에 (이미) 있으면 true, 더 구체적인 자식 때문에 더하지 못하면 false.</returns>
        /// <remarks>UE: FGameplayTagContainer::AddLeafTag</remarks>
        public bool AddLeafTag(GameplayTag tagToAdd)
        {
            if (HasTagExact(tagToAdd))
            {
                return true;
            }

            // 이미 부모로 들어 있다 = 더 구체적인 자식이 명시돼 있다
            if (HasTag(tagToAdd))
            {
                return false;
            }

            GameplayTagsManager manager = GameplayTagsManager.Get();
            GameplayTagNode tagNode = manager.FindTagNode(tagToAdd);
            if (tagNode != null)
            {
                GameplayTagContainer singleContainer = tagNode.GetSingleTagContainer();
                foreach (GameplayTag parentTag in singleContainer._parentTags)
                {
                    if (HasTagExact(parentTag))
                    {
                        RemoveTag(parentTag);
                    }
                }
            }
            else
            {
                Debug.LogError($"AddLeafTag: 미등록 태그 {tagToAdd}입니다. 등록된 태그만 질의할 수 있습니다.");
            }

            AddTag(tagToAdd);
            return true;
        }

        /// <summary>명시 태그를 하나 뺀다. 같은 부모를 다른 태그가 줄 수 있어 부모 태그는 처음부터 다시 만든다.</summary>
        /// <param name="deferParentTags">true면 부모 태그 재계산을 미룬다. 호출자가 나중에 <see cref="FillParentTags"/>를 불러야 한다.</param>
        /// <remarks>UE: FGameplayTagContainer::RemoveTag</remarks>
        public bool RemoveTag(GameplayTag tagToRemove, bool deferParentTags = false)
        {
            if (!_gameplayTags.Remove(tagToRemove))
            {
                return false;
            }

            if (!deferParentTags)
            {
                FillParentTags();
            }

            return true;
        }

        /// <summary>대상 컨테이너의 명시 태그를 모두 빼고 부모 태그를 한 번 다시 만든다.</summary>
        /// <remarks>UE: FGameplayTagContainer::RemoveTags</remarks>
        public void RemoveTags(GameplayTagContainer tagsToRemove)
        {
            int numChanged = 0;
            foreach (GameplayTag tag in tagsToRemove._gameplayTags)
            {
                if (_gameplayTags.Remove(tag))
                {
                    numChanged++;
                }
            }

            if (numChanged > 0)
            {
                FillParentTags();
            }
        }

        /// <summary>모든 태그를 비운다. 할당된 용량은 유지한다.</summary>
        /// <param name="slack">비운 뒤 최소한 확보해 둘 용량.</param>
        /// <remarks>UE: FGameplayTagContainer::Reset</remarks>
        public void Reset(int slack = 0)
        {
            _gameplayTags.Clear();
            _parentTags.Clear();

            // 부모 태그 수는 보통 명시 태그 수와 비슷하므로 같은 용량을 확보한다
            if (_gameplayTags.Capacity < slack)
            {
                _gameplayTags.Capacity = slack;
            }

            if (_parentTags.Capacity < slack)
            {
                _parentTags.Capacity = slack;
            }
        }

        /// <summary>명시 태그를 쉼표로 이은 문자열.</summary>
        /// <remarks>UE: FGameplayTagContainer::ToStringSimple</remarks>
        public string ToStringSimple(bool quoted = false)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < _gameplayTags.Count; i++)
            {
                if (quoted)
                {
                    builder.Append('"');
                }

                builder.Append(_gameplayTags[i].ToString());

                if (quoted)
                {
                    builder.Append('"');
                }

                if (i < _gameplayTags.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            return builder.ToString();
        }

        /// <summary>명시 태그 목록(읽기 전용).</summary>
        /// <remarks>UE: FGameplayTagContainer::GetGameplayTagArray</remarks>
        public IReadOnlyList<GameplayTag> GetGameplayTagArray()
        {
            return _gameplayTags;
        }

        /// <summary>foreach용. List 열거자를 그대로 돌려줘 박싱·할당이 없다.</summary>
        /// <remarks>UE: FGameplayTagContainer::CreateConstIterator</remarks>
        public List<GameplayTag>.Enumerator GetEnumerator()
        {
            return _gameplayTags.GetEnumerator();
        }

        /// <remarks>UE: FGameplayTagContainer::IsValidIndex</remarks>
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < _gameplayTags.Count;
        }

        /// <summary>인덱스의 명시 태그. 범위를 벗어나면 빈 태그.</summary>
        /// <remarks>UE: FGameplayTagContainer::GetByIndex</remarks>
        public GameplayTag GetByIndex(int index)
        {
            return IsValidIndex(index) ? _gameplayTags[index] : default;
        }

        /// <summary>첫 명시 태그. 비어 있으면 빈 태그.</summary>
        /// <remarks>UE: FGameplayTagContainer::First</remarks>
        public GameplayTag First()
        {
            return _gameplayTags.Count > 0 ? _gameplayTags[0] : default;
        }

        /// <summary>마지막 명시 태그. 비어 있으면 빈 태그.</summary>
        /// <remarks>UE: FGameplayTagContainer::Last</remarks>
        public GameplayTag Last()
        {
            return _gameplayTags.Count > 0 ? _gameplayTags[_gameplayTags.Count - 1] : default;
        }

        /// <summary>명시 태그로부터 부모 태그 캐시를 다시 만든다.</summary>
        /// <remarks>UE: FGameplayTagContainer::FillParentTags</remarks>
        public void FillParentTags()
        {
            _parentTags.Clear();
            if (_gameplayTags.Count == 0)
            {
                return;
            }

            GameplayTagsManager manager = GameplayTagsManager.Get();
            foreach (GameplayTag tag in _gameplayTags)
            {
                manager.ExtractParentTags(tag, _parentTags);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        // 역직렬화 콜백 안에선 매니저(내부에서 Resources.Load)를 만들 수 없어, 매니저 없이 이름만 쪼개 부모를 복원한다.
        // 등록 태그의 부모는 항상 트리에 있으므로(중간 노드 자동 생성) 노드 캐시와 결과가 같다.
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _gameplayTags ??= new List<GameplayTag>();
            _parentTags ??= new List<GameplayTag>();
            _parentTags.Clear();

            foreach (GameplayTag tag in _gameplayTags)
            {
                tag.ParseParentTags(_parentTags);
            }
        }

        private static void AddUnique(List<GameplayTag> tags, GameplayTag tag)
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }
    }
}
