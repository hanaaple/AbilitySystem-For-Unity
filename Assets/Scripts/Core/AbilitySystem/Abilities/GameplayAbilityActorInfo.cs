using UnityEngine;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티가 동작하는 액터 정보. Owner는 ASC를 소유한 논리적 주인, Avatar는 월드에서 실제로 움직이는 몸이다(대개 같다).</summary>
    /// <remarks>UE: FGameplayAbilityActorInfo</remarks>
    public class GameplayAbilityActorInfo
    {
        public GameObject OwnerActor { get; private set; }

        public GameObject AvatarActor { get; private set; }

        public AbilitySystemComponent AbilitySystemComponent { get; private set; }

        /// <remarks>UE: FGameplayAbilityActorInfo::InitFromActor</remarks>
        public void InitFromActor(GameObject ownerActor, GameObject avatarActor, AbilitySystemComponent abilitySystemComponent)
        {
            OwnerActor = ownerActor;
            AvatarActor = avatarActor;
            AbilitySystemComponent = abilitySystemComponent;
        }
    }
}
