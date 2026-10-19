using System;
using System.Collections.Generic;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>태그 노드 트리를 소유·관리하는 중심 객체. 태그 등록·조회·계층 매칭의 진입점.</summary>
    public class GameplayTagsManager
    {
        private const string InvalidTagCharacters = "\"',\r\n\t";

        private static readonly char[] _tagTrimCharacters = { '.', ' ' };

        private static GameplayTagsManager _instance;
        private static bool _isConstructing;

        // 같은 이름으로 반복 요청될 때 로그가 쌓이지 않도록 이름당 한 번만 알린다
        private static readonly HashSet<InternedName> _missingTagNames = new();

#if UNITY_EDITOR
        /// <summary>에디터에서 태그 트리를 다시 만든 직후 호출된다. 이전 트리의 노드는 비워지므로, 노드를 들고 있던 에디터 UI는 여기서 노드를 다시 조회해야 한다.</summary>
        /// <remarks>UE: UGameplayTagsManager::OnEditorRefreshGameplayTagTree</remarks>
        public static event Action OnEditorRefreshGameplayTagTree;
#endif

        private GameplayTagNode gameplayRootTag;

        private Dictionary<InternedName, GameplayTagNode> gameplayTagNodeMap;

        private GameplayTagsManager()
        {
            ConstructGameplayTagTree();
        }

        // 씬 로드 전에 트리를 미리 빌드해, GE/ASC가 태그를 건드리기 전 준비를 보장한다.
        // 도메인 리로드가 꺼져 있어도 매 재생마다 인스턴스를 새로 만들어 낡은 트리를 피한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _instance = CreateManager();
        }

        /// <summary>전역 단일 인스턴스. 최초 접근 시 생성되며 트리를 1회 빌드한다. 에디터에서도 같은 방식으로 생성된다.</summary>
        /// <remarks>UE: UGameplayTagsManager::Get</remarks>
        public static GameplayTagsManager Get()
        {
            return _instance ??= CreateManager();
        }

        /// <summary>이름으로 등록된 태그를 찾는다. 미등록이면 빈 태그(default)를 돌려준다.</summary>
        /// <param name="errorIfNotFound">true면 미등록 이름을 이름당 한 번 에러로 알린다.</param>
        /// <remarks>UE: UGameplayTagsManager::RequestGameplayTag</remarks>
        public GameplayTag RequestGameplayTag(InternedName tagName, bool errorIfNotFound = true)
        {
            // 트리에 등록된 태그만 유효로 취급한다
            if (FindTagNode(tagName) != null)
            {
                return GameplayTag.Create(tagName);
            }

            if (errorIfNotFound && _missingTagNames.Add(tagName))
            {
                Debug.LogError($"요청한 태그 {tagName}를 찾지 못했습니다. 태그는 레지스트리 에셋이나 native 태그로 등록해야 합니다.");
            }

            return default;
        }

        /// <summary>태그와 모든 부모를 명시로 담은 새 컨테이너. 미등록이면 빈 컨테이너.</summary>
        /// <remarks>UE: UGameplayTagsManager::RequestGameplayTagParents</remarks>
        public GameplayTagContainer RequestGameplayTagParents(GameplayTag gameplayTag)
        {
            GameplayTagNode tagNode = FindTagNode(gameplayTag);
            if (tagNode == null)
            {
                return new GameplayTagContainer();
            }

            GameplayTagContainer singleContainer = tagNode.GetSingleTagContainer();
            return singleContainer.GetGameplayTagParents();
        }

        /// <summary>태그의 부모들을 바로 위 부모부터 중복 없이 더한다. 미등록 태그도 나중에 등록될 수 있어 이름을 쪼개 더한다.</summary>
        /// <returns>하나라도 더했으면 true.</returns>
        /// <remarks>UE: UGameplayTagsManager::ExtractParentTags</remarks>
        public bool ExtractParentTags(GameplayTag gameplayTag, List<GameplayTag> uniqueParentTags)
        {
            if (!gameplayTag.IsValid())
            {
                return false;
            }

            int oldSize = uniqueParentTags.Count;

            GameplayTagNode tagNode = FindTagNode(gameplayTag);
            if (tagNode != null)
            {
                GameplayTagContainer singleContainer = tagNode.GetSingleTagContainer();
                foreach (GameplayTag parentTag in singleContainer.ParentTagList)
                {
                    if (!uniqueParentTags.Contains(parentTag))
                    {
                        uniqueParentTags.Add(parentTag);
                    }
                }
            }
            else
            {
                gameplayTag.ParseParentTags(uniqueParentTags);
            }

            return uniqueParentTags.Count != oldSize;
        }

        /// <summary>태그의 모든 하위(자손) 태그를 담은 새 컨테이너. 태그 자신은 넣지 않는다.</summary>
        /// <remarks>UE: UGameplayTagsManager::RequestGameplayTagChildren</remarks>
        public GameplayTagContainer RequestGameplayTagChildren(GameplayTag gameplayTag)
        {
            var tagContainer = new GameplayTagContainer();
            GameplayTagNode tagNode = FindTagNode(gameplayTag);
            if (tagNode != null)
            {
                AddChildrenTags(tagContainer, tagNode);
            }

            return tagContainer;
        }

        /// <summary>바로 위 부모 태그. 최상위·미등록이면 빈 태그.</summary>
        /// <remarks>UE: UGameplayTagsManager::RequestGameplayTagDirectParent</remarks>
        public GameplayTag RequestGameplayTagDirectParent(GameplayTag gameplayTag)
        {
            GameplayTagNode tagNode = FindTagNode(gameplayTag);
            GameplayTagNode parentNode = tagNode?.GetParentTagNode();
            return parentNode != null ? parentNode.GetCompleteTag() : default;
        }

        /// <summary>두 태그가 루트부터 몇 단계까지 같은지. <c>"A.B.C"</c>와 <c>"A.B.D"</c>는 2.</summary>
        /// <remarks>UE: UGameplayTagsManager::GameplayTagsMatchDepth</remarks>
        public int GameplayTagsMatchDepth(GameplayTag gameplayTagOne, GameplayTag gameplayTagTwo)
        {
            List<InternedName> tags1 = GetSimpleNamesFromLeaf(gameplayTagOne);
            List<InternedName> tags2 = GetSimpleNamesFromLeaf(gameplayTagTwo);

            // 리프→루트 순으로 모았으므로 뒤(루트)부터 비교한다
            int index1 = tags1.Count - 1;
            int index2 = tags2.Count - 1;
            int depth = 0;
            for (; index1 >= 0 && index2 >= 0; index1--, index2--)
            {
                if (!tags1[index1].Equals(tags2[index2]))
                {
                    break;
                }

                depth++;
            }

            return depth;
        }

        /// <summary>태그의 깊이(자신 포함 노드 수). <c>"A.B.C"</c>는 3, 미등록이면 0.</summary>
        /// <remarks>UE: UGameplayTagsManager::GetNumberOfTagNodes</remarks>
        public int GetNumberOfTagNodes(GameplayTag gameplayTag)
        {
            int count = 0;
            for (GameplayTagNode tagNode = FindTagNode(gameplayTag); tagNode != null; tagNode = tagNode.GetParentTagNode())
            {
                count++;
            }

            return count;
        }

        /// <summary>트리에 등록된 노드(태그) 수. 중간 부모로 자동 생성된 노드도 센다.</summary>
        /// <remarks>UE: UGameplayTagsManager::GetNumGameplayTagNodes</remarks>
        public int GetNumGameplayTagNodes()
        {
            return gameplayTagNodeMap.Count;
        }

        /// <remarks>UE: UGameplayTagsManager::FindTagNode</remarks>
        public GameplayTagNode FindTagNode(GameplayTag gameplayTag)
        {
            return FindTagNode(gameplayTag.GetTagName());
        }

        /// <remarks>UE: UGameplayTagsManager::FindTagNode</remarks>
        public GameplayTagNode FindTagNode(InternedName tag)
        {
            return gameplayTagNodeMap.GetValueOrDefault(tag);
        }

        /// <summary>문자열이 태그 사전에 추가될 수 있는 형식인지(문법 오류가 없는지).</summary>
        /// <remarks>UE: UGameplayTagsManager::IsValidGameplayTagString</remarks>
        public bool IsValidGameplayTagString(string tagString)
        {
            return IsValidGameplayTagString(tagString, out _, out _);
        }

        /// <summary>문자열이 태그 사전에 추가될 수 있는 형식인지(문법 오류가 없는지).</summary>
        /// <param name="outError">형식이 틀렸으면 오류 메시지.</param>
        /// <param name="outFixedString">형식이 틀렸으면 고친 문자열. 고칠 수 없으면 빈 문자열.</param>
        /// <remarks>UE: UGameplayTagsManager::IsValidGameplayTagString</remarks>
        public bool IsValidGameplayTagString(string tagString, out string outError, out string outFixedString)
        {
            var errors = new List<string>();
            bool isValid = true;
            string fixedString = tagString ?? string.Empty;

            if (fixedString.Length == 0)
            {
                errors.Add("Tag may not be empty");
                isValid = false;
            }

            string trimmedStart = fixedString.TrimStart(_tagTrimCharacters);
            if (trimmedStart.Length != fixedString.Length)
            {
                AddTrimErrors(errors, fixedString.Substring(0, fixedString.Length - trimmedStart.Length), "begin");
                isValid = false;
                fixedString = trimmedStart;
            }

            string trimmedEnd = fixedString.TrimEnd(_tagTrimCharacters);
            if (trimmedEnd.Length != fixedString.Length)
            {
                AddTrimErrors(errors, fixedString.Substring(trimmedEnd.Length), "end");
                isValid = false;
                fixedString = trimmedEnd;
            }

            // 트리 구성(AddTagToTree)이 '.'로 쪼갠 빈 구간을 이름 없는 노드로 만들므로 받지 않는다
            if (fixedString.Contains(".."))
            {
                errors.Add("Tag may not contain an empty section ('..')");
                isValid = false;
                while (fixedString.Contains(".."))
                {
                    fixedString = fixedString.Replace("..", ".");
                }
            }

            string matchedInvalidCharacters = FindInvalidTagCharacters(fixedString);
            if (matchedInvalidCharacters.Length > 0)
            {
                errors.Add($"Tag may not contain the following characters: {matchedInvalidCharacters}");
                isValid = false;
                foreach (char invalidCharacter in InvalidTagCharacters)
                {
                    fixedString = fixedString.Replace(invalidCharacter, '_');
                }
            }

            outError = string.Join(", ", errors);
            outFixedString = fixedString;
            return isValid;
        }

        /// <summary>native 태그 추가를 마감한다. 이후에는 native 태그를 더하면 안전하지 않다.</summary>
        /// <remarks>UE: UGameplayTagsManager::DoneAddingNativeTags</remarks>
        public void DoneAddingNativeTags()
        {
            throw new NotImplementedException();
        }

        /// <summary>native 태그 추가가 끝나는 시점(태그 초기화 완료)에 호출할 콜백을 등록한다. 이미 끝났으면 즉시 호출한다.</summary>
        /// <remarks>UE: UGameplayTagsManager::CallOrRegister_OnDoneAddingNativeTagsDelegate</remarks>
        public void CallOrRegister_OnDoneAddingNativeTagsDelegate(Action inDelegate)
        {
            throw new NotImplementedException();
        }

        /// <summary>태그를 구간 이름으로 나눈다. <c>"x.y.z"</c>는 <c>{x, y, z}</c>.</summary>
        /// <remarks>UE: UGameplayTagsManager::SplitGameplayTagFName</remarks>
        public void SplitGameplayTagFName(GameplayTag tag, List<InternedName> outNames)
        {
            throw new NotImplementedException();
        }

        /// <summary>등록된 모든 태그를 컨테이너에 담는다.</summary>
        /// <param name="onlyIncludeDictionaryTags">true면 중간 부모로 암시 추가된 태그를 가능한 한 뺀다.</param>
        /// <remarks>UE: UGameplayTagsManager::RequestAllGameplayTags</remarks>
        public void RequestAllGameplayTags(GameplayTagContainer tagContainer, bool onlyIncludeDictionaryTags)
        {
            throw new NotImplementedException();
        }

#if UNITY_EDITOR
        /// <summary>필터에 맞는 노드를 트리의 최상위처럼 모은다. 필터는 쉼표로 나눈 태그 경로이고(<c>"Status,Damage.Fire"</c>), 경로마다 그 노드를 돌려준다. 필터가 비었거나 하나도 일치하지 않으면 실제 최상위 노드 전체.</summary>
        /// <remarks>UE: UGameplayTagsManager::GetFilteredGameplayRootTags</remarks>
        public void GetFilteredGameplayRootTags(string inFilterString, List<GameplayTagNode> outTagArray)
        {
            outTagArray.Clear();
            IReadOnlyList<GameplayTagNode> gameplayRootTags = gameplayRootTag.GetChildTagNodes();

            if (!string.IsNullOrEmpty(inFilterString))
            {
                foreach (string filter in inFilterString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    RecursiveRootTagSearch(filter.Trim(), gameplayRootTags, outTagArray);
                }

                // 필터가 트리와 맞지 않을 때 태그 구조를 강요하지 않고 전체를 보인다
                if (outTagArray.Count > 0)
                {
                    return;
                }
            }

            outTagArray.AddRange(gameplayRootTags);
        }

        /// <summary>태그 정의(레지스트리·native)가 바뀐 뒤 트리를 다시 만든다. 이전 트리의 노드는 비워진다.</summary>
        /// <remarks>UE: UGameplayTagsManager::EditorRefreshGameplayTagTree</remarks>
        public void EditorRefreshGameplayTagTree()
        {
            DestroyGameplayTagTree();
            ConstructGameplayTagTree();
            OnEditorRefreshGameplayTagTree?.Invoke();
        }
#endif

        /// <remarks>UE: UGameplayTagsManager::InitializeManager</remarks>
        private static GameplayTagsManager CreateManager()
        {
            // native 태그 정의 클래스의 static 초기화에서 태그를 조회하면 트리 구성 중 다시 여기로 들어와 무한 재귀(에디터 크래시)가 된다.
            if (_isConstructing)
            {
                throw new InvalidOperationException($"{nameof(GameplayTagsManager)} 트리 구성 중에 매니저에 접근했습니다. native 태그를 정의하는 클래스의 static 초기화에서 태그를 조회하지 마십시오.");
            }

            _isConstructing = true;
            try
            {
                return new GameplayTagsManager();
            }
            finally
            {
                _isConstructing = false;
            }
        }

        /// <remarks>UE: UGameplayTagsManager::ConstructGameplayTagTree</remarks>
        private void ConstructGameplayTagTree()
        {
            gameplayRootTag = new GameplayTagNode();
            gameplayTagNodeMap = new Dictionary<InternedName, GameplayTagNode>();

            // native 태그는 저장하지 않고 매 구성마다 코드에서 다시 모은다. 레지스트리와 경로가 겹치면 같은 노드로 합쳐진다.
            foreach (NativeGameplayTag nativeTag in NativeGameplayTag.GetRegisteredNativeTags())
            {
                AddTagToTree(nativeTag.TagName.ToString());
            }

            // 에셋 생성은 에디터 로드 시점(GameplayTagRegistryInitializer)에 끝나므로 런타임은 읽기만 한다.
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            if (registry == null)
            {
                Debug.LogWarning($"{nameof(GameplayTagRegistryAsset)}를 찾지 못해 native 태그만으로 트리를 구성합니다. (Resources/{GameplayTagRegistryAsset.ResourcesPath})");
                return;
            }

            foreach (string tagName in registry.Tags)
            {
                AddTagToTree(tagName);
            }
        }

        private void AddTagToTree(string fullTagName)
        {
            if (string.IsNullOrEmpty(fullTagName))
            {
                return;
            }

            string[] segments = fullTagName.Split('.');
            GameplayTagNode currentNode = gameplayRootTag;
            string accumulatedPath = null;

            foreach (string segment in segments)
            {
                var shortTag = new InternedName(segment);

                accumulatedPath = accumulatedPath == null ? segment : accumulatedPath + "." + segment;

                // 레벨마다 기존 자식을 재사용(dedup)하므로, 리프(A.B.C)만 등록해도 중간 부모(A, A.B)가 자동 생성된다.
                if (!currentNode.TryFindChild(shortTag, out GameplayTagNode childNode))
                {
                    var fullName = new InternedName(accumulatedPath);
                    childNode = new GameplayTagNode(shortTag, fullName, currentNode);
                    currentNode.AddChildNode(childNode);
                    gameplayTagNodeMap.Add(fullName, childNode);
                }

                currentNode = childNode;
            }
        }

        /// <remarks>UE: UGameplayTagsManager::DestroyGameplayTagTree</remarks>
        private void DestroyGameplayTagTree()
        {
            gameplayRootTag?.ResetNode();
            gameplayRootTag = null;
            gameplayTagNodeMap?.Clear();
        }

        /// <remarks>UE: UGameplayTagsManager::AddChildrenTags</remarks>
        private void AddChildrenTags(GameplayTagContainer tagContainer, GameplayTagNode gameplayTagNode, bool recurseAll = true, bool onlyIncludeDictionaryTags = false)
        {
            // 명시 등록/중간 부모 자동 생성을 노드에 기록하지 않아 사전 태그만 고를 수 없다
            if (onlyIncludeDictionaryTags)
            {
                throw new NotImplementedException($"{nameof(AddChildrenTags)}: {nameof(onlyIncludeDictionaryTags)}는 아직 지원하지 않습니다.");
            }

            if (gameplayTagNode == null)
            {
                return;
            }

            foreach (GameplayTagNode childNode in gameplayTagNode.GetChildTagNodes())
            {
                tagContainer.AddTag(childNode.GetCompleteTag());

                if (recurseAll)
                {
                    AddChildrenTags(tagContainer, childNode, true, onlyIncludeDictionaryTags);
                }
            }
        }

        // 리프에서 루트 방향으로 각 노드의 단순 이름을 모은다
        private List<InternedName> GetSimpleNamesFromLeaf(GameplayTag gameplayTag)
        {
            var simpleNames = new List<InternedName>();
            for (GameplayTagNode tagNode = FindTagNode(gameplayTag); tagNode != null; tagNode = tagNode.GetParentTagNode())
            {
                simpleNames.Add(tagNode.GetSimpleTagName());
            }

            return simpleNames;
        }

        private static void AddTrimErrors(List<string> errors, string removed, string position)
        {
            if (removed.IndexOf('.') >= 0)
            {
                errors.Add($"Tag may not {position} with a period ('.')");
            }

            if (removed.IndexOf(' ') >= 0)
            {
                errors.Add($"Tag may not {position} with a space");
            }
        }

        private static string FindInvalidTagCharacters(string tagString)
        {
            var matched = new List<string>();
            foreach (char invalidCharacter in InvalidTagCharacters)
            {
                if (tagString.IndexOf(invalidCharacter) < 0)
                {
                    continue;
                }

                switch (invalidCharacter)
                {
                    case '\r':
                    {
                        matched.Add("carriage return [\\r]");
                        break;
                    }
                    case '\n':
                    {
                        matched.Add("line feed [\\n]");
                        break;
                    }
                    case '\t':
                    {
                        matched.Add("tab");
                        break;
                    }
                    default:
                    {
                        matched.Add(invalidCharacter.ToString());
                        break;
                    }
                }
            }

            return string.Join(" ", matched);
        }

#if UNITY_EDITOR
        /// <remarks>UE: RecursiveRootTagSearch</remarks>
        private static void RecursiveRootTagSearch(string inFilterString, IReadOnlyList<GameplayTagNode> gameplayRootTags, List<GameplayTagNode> outTagArray)
        {
            int dotIndex = inFilterString.IndexOf('.');
            string currentFilter = dotIndex < 0 ? inFilterString : inFilterString.Substring(0, dotIndex);
            string restOfFilter = dotIndex < 0 ? string.Empty : inFilterString.Substring(dotIndex + 1);

            foreach (GameplayTagNode rootTag in gameplayRootTags)
            {
                InternedName rootTagName = rootTag.GetSimpleTagName();
                if (!string.Equals(rootTagName.ToString(), currentFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (restOfFilter.Length == 0)
                {
                    outTagArray.Add(rootTag);
                }
                else
                {
                    RecursiveRootTagSearch(restOfFilter, rootTag.GetChildTagNodes(), outTagArray);
                }
            }
        }
#endif
    }
}
