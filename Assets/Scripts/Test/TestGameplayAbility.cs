using System;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// 하위 클래스 필드가 <see cref="GameplayAbilityAsset"/> 인스펙터에 그려지고 저장되는지, 부여 때 복제되는지 확인하기 위한 테스트 어빌리티.
    /// 발동하면 즉발형으로 커밋 → <c>_testEffect</c>를 Owner에 적용 → 종료한다. 커밋에 실패하면 효과 없이 취소로 끝낸다.
    /// </summary>
    [Serializable]
    public sealed class TestGameplayAbility : GameplayAbility
    {
        [SerializeField] private float _testValue;
        [SerializeField] private GameplayEffectAsset _testEffect;

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityActorInfo actorInfo)
        {
            if (!CommitAbility(handle, actorInfo))
            {
                EndAbility(handle, actorInfo, true);
                return;
            }

            ApplyGameplayEffectToOwner(handle, actorInfo, _testEffect, GetAbilityLevel(handle, actorInfo));
            EndAbility(handle, actorInfo, false);
        }
    }
}
