using UnityEditor;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>에디터 로드·재컴파일마다 레지스트리 에셋을 보장한다. 생성은 에디터에서 끝내 두고, 런타임(플레이·빌드)은 Load로 읽기만 하게 한다.</summary>
    [InitializeOnLoad]
    public static class GameplayTagRegistryInitializer
    {
        static GameplayTagRegistryInitializer()
        {
            GameplayTagRegistryAsset.LoadOrCreate();
        }
    }
}
