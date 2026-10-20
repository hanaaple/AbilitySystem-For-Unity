using System;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Test
{
    /// <summary>하위 클래스 필드가 <see cref="GameplayAbilityAsset"/> 인스펙터에 그려지고 저장되는지, 부여 때 복제되는지 확인하기 위한 테스트 어빌리티.</summary>
    [Serializable]
    public sealed class TestGameplayAbility : GameplayAbility
    {
        [SerializeField] private float _testValue;
        [SerializeField] private GameplayEffectAsset _testEffect;
    }
}
