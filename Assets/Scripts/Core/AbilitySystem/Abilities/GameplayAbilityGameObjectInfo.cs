using UnityEngine;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티가 동작하는 GameObject 정보. Owner는 ASC를 소유한 논리적 주인, Avatar는 월드에서 실제로 움직이는 몸이다(대개 같다).</summary>
    /// <remarks>UE: FGameplayAbilityActorInfo</remarks>
    public class GameplayAbilityGameObjectInfo
    {
        public GameObject OwnerGameObject { get; private set; }

        public GameObject AvatarGameObject { get; private set; }

        public AbilitySystemComponent AbilitySystemComponent { get; private set; }

        /// <remarks>UE: FGameplayAbilityActorInfo::InitFromActor</remarks>
        public void InitFromGameObject(GameObject ownerGameObject, GameObject avatarGameObject, AbilitySystemComponent abilitySystemComponent)
        {
            OwnerGameObject = ownerGameObject;
            AvatarGameObject = avatarGameObject;
            AbilitySystemComponent = abilitySystemComponent;
        }
    }
}
