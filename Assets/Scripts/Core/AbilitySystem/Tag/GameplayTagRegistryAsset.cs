using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>태그 정의의 단일 출처. 계층 문자열 목록을 담고, 시작 시 이 목록으로 태그 트리를 빌드한다. Resources 하위 고정 경로에서 로드해 빌드·에디터가 같은 정의를 참조한다. 에셋은 에디터 로드 시점에 자동 생성되며(GameplayTagRegistryInitializer → LoadOrCreate), 수동 생성 메뉴는 두지 않아 생성 지점을 단일화한다.</summary>
    public sealed class GameplayTagRegistryAsset : ScriptableObject
    {
        /// <summary>Resources 기준 고정 경로. 이 경로의 에셋 하나만 정의 출처로 삼아 로드 지점을 단일화한다.</summary>
        public const string ResourcesPath = "AbilitySystem/GameplayTagRegistry";

        [SerializeField] private List<string> _tags = new();

        public IReadOnlyList<string> Tags => _tags;

        /// <summary>고정 경로에서 레지스트리를 로드한다. 없으면 null.</summary>
        public static GameplayTagRegistryAsset Load()
        {
            return Resources.Load<GameplayTagRegistryAsset>(ResourcesPath);
        }

#if UNITY_EDITOR
        // 생성은 에디터에서만 가능(AssetDatabase는 빌드에 없음). 에디터 로드 시점에 이 경로의 에셋을
        // 보장해 두면 빌드는 Load()로 읽기만 하면 된다.
        private const string EditorAssetPath = "Assets/Resources/" + ResourcesPath + ".asset";

        /// <summary>에디터 전용: 고정 경로의 레지스트리를 로드하고, 없으면 그 자리에 생성해 돌려준다.</summary>
        public static GameplayTagRegistryAsset LoadOrCreate()
        {
            GameplayTagRegistryAsset asset = Load();
            if (asset != null)
            {
                return asset;
            }

            asset = CreateInstance<GameplayTagRegistryAsset>();

            EnsureParentFolders();
            UnityEditor.AssetDatabase.CreateAsset(asset, EditorAssetPath);
            UnityEditor.AssetDatabase.SaveAssets();

            return asset;
        }

        private static void EnsureParentFolders()
        {
            string directory = System.IO.Path.GetDirectoryName(EditorAssetPath).Replace('\\', '/');
            if (UnityEditor.AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            // CreateAsset은 부모 폴더가 AssetDatabase에 등록돼 있어야 하므로 "Assets"부터 한 단계씩 만든다.
            string[] segments = directory.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
#endif
    }
}
