using System;
using Core.AbilitySystem.Abilities;
using Game.Character;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>이동 입력 방향(입력이 없으면 정면)으로 구르며 소유자의 dashDistance만큼 짧게 돌진한다. 돌진이 끝날 때까지 어빌리티가 활성으로 남아 Activation Owned Tags가 그동안 붙어 있다.</summary>
    [Serializable]
    public sealed class DashAbility : CharacterGameplayAbility
    {
        [Min(0.01f)] [SerializeField] private float duration = 0.2f;

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            var character = gameObjectInfo.AvatarGameObject.GetComponent<PlayerCharacter>();
            if (character == null || !CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            // 거리는 소유자의 dashDistance 어트리뷰트, 시간은 어빌리티 정의값, 방향은 캐릭터가 가려는 쪽
            float distance = gameObjectInfo.AbilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.DashDistance);
            Vector3 velocity = character.GetMoveIntentDirection() * (distance / duration);
            character.Push(velocity, duration, () => EndAbility(handle, gameObjectInfo, false));
        }
    }
}
