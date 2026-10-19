using System.Collections.Generic;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Test
{
    /// <summary>GameplayTag 인스펙터 에디터를 확인하기 위한 테스트 에셋. 단일 필드·리스트 요소·컨테이너에서 각각 어떻게 그려지고 저장되는지 본다.</summary>
    [CreateAssetMenu(menuName = "Test/Gameplay Tag Test", fileName = "GameplayTagTest")]
    public sealed class GameplayTagTestAsset : ScriptableObject
    {
        [SerializeField] private GameplayTag _tag;
        [SerializeField] private List<GameplayTag> _tagList = new();
        [SerializeField] private GameplayTagContainer _tagContainer = new();
    }
}
