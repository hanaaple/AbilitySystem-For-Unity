using System.Collections.Generic;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>태그 노드 트리를 소유·관리하는 중심 객체. 태그 등록·조회·계층 매칭의 진입점.</summary>
    public class GameplayTagsManager
    {
        private static GameplayTagsManager _instance;

        private GameplayTagNode gameplayRootTag;

        private Dictionary<GameplayTag, GameplayTagNode> gameplayTagNodeMap;

        /// <summary>전역 단일 인스턴스. 최초 접근 시 트리를 1회 빌드한다.</summary>
        public static GameplayTagsManager Instance => _instance;

        private GameplayTagsManager()
        {
            ConstructGameplayTagTree();
        }

        // 씬 로드 전에 트리를 미리 빌드해, GE/ASC가 태그를 건드리기 전 준비를 보장한다.
        // 도메인 리로드가 꺼져 있어도 매 재생마다 인스턴스를 새로 만들어 낡은 트리를 피한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _instance = new GameplayTagsManager();
        }

        public GameplayTag RequestGameplayTag(InternedName inTagName)
        {
            var tag = new GameplayTag(inTagName);

            // 트리에 등록된 태그만 유효로 취급한다. 미등록이면 빈 태그(default)를 돌려준다.
            return FindTagNode(tag) != null ? tag : default;
        }

        // public GameplayTag AddNativeGameplayTag(InternedName TagName)
        // {
        // }

        // public void AddNativeGameplayTag()
        // public void RemoveNativeGameplayTag()

        // public GameplayTagContainer RequestGameplayTagParents(GameplayTag tag)
        // {
        //
        // }

        // ExtractParentTags?

        // public GameplayTagContainer RequestGameplayTagChildren(GameplayTag tag){}

        // public GameplayTag RequestGameplayTagDirectParent(){}

        public GameplayTagNode FindTagNode(GameplayTag gameplayTag)
        {
            return gameplayTagNodeMap.GetValueOrDefault(gameplayTag);
        }

        public GameplayTagNode FindTagNode(InternedName tag)
        {
            return FindTagNode(new GameplayTag(tag));
        }

        private void ConstructGameplayTagTree()
        {
            gameplayRootTag = new GameplayTagNode();
            gameplayTagNodeMap = new Dictionary<GameplayTag, GameplayTagNode>();

            // 에셋 생성은 에디터 로드 시점(GameplayTagRegistryInitializer)에 끝나므로 런타임은 읽기만 한다.
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            if (registry == null)
            {
                Debug.LogWarning($"{nameof(GameplayTagRegistryAsset)}를 찾지 못해 태그 트리가 비어 있습니다. (Resources/{GameplayTagRegistryAsset.ResourcesPath})");
                return;
            }

            foreach (string tempTag in registry.Tags)
            {
                if (string.IsNullOrEmpty(tempTag))
                {
                    continue;
                }

                string[] segments = tempTag.Split('.');
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
                        gameplayTagNodeMap.Add(new GameplayTag(fullName), childNode);
                    }

                    currentNode = childNode;
                }
            }
        }

        private void DestroyGameplayTagTree()
        {
            gameplayRootTag = null;
            gameplayTagNodeMap?.Clear();
        }


        // GameplayTagsMatchDepth
        // GetNumberOfTagNodes

        // GetNumGameplayTagNodes
    }
}
