using System;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Tag;
using Game.Character;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>소유자의 jumpHeight만큼 점프하고 착지할 때까지 활성으로 남는다. 그동안 Activation Owned Tags(점프 중 태그)가 붙어 있다.</summary>
    [Serializable]
    public sealed class JumpAbility : CharacterGameplayAbility
    {
        /// <remarks>UE: UGameplayAbility_CharacterJump::CanActivateAbility</remarks>
        public override bool CanActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayTagContainer optionalRelevantTags = null)
        {
            if (!base.CanActivateAbility(handle, gameObjectInfo, optionalRelevantTags))
            {
                return false;
            }

            var character = gameObjectInfo.AvatarGameObject.GetComponent<PlayerCharacter>();
            return character != null && character.CanJump();
        }

        public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            var character = gameObjectInfo.AvatarGameObject.GetComponent<PlayerCharacter>();
            if (!CommitAbility(handle, gameObjectInfo))
            {
                EndAbility(handle, gameObjectInfo, true);
                return;
            }

            // v = √(h · -2g) 가 높이 h까지 올라가는 초기 속도 — 높이는 소유자의 jumpHeight 어트리뷰트, 중력은 캐릭터 이동 쪽
            float jumpHeight = gameObjectInfo.AbilitySystemComponent.GetAttributeCurrentValue(PlayerAttributeSet.JumpHeight);
            float verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * character.Gravity);
            character.Launch(verticalVelocity, () => EndAbility(handle, gameObjectInfo, false));
        }
    }
}
