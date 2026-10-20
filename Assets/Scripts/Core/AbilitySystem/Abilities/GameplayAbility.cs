using System;
using UnityEngine;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티의 런타임 부분. 정의 에셋(<see cref="GameplayAbilityAsset"/>)이 하위 클래스 인스턴스를 원형으로 들고(필드 값 저장), 부여될 때마다 원형을 복제해 액터별 상태와 동작을 갖는다. 하위 클래스도 [Serializable]이어야 저장된다.</summary>
    /// <remarks>UE: UGameplayAbility</remarks>
    [Serializable]
    public abstract class GameplayAbility
    {
        /// <summary>이 인스턴스가 속한 액터 정보. 부여 전엔 null.</summary>
        /// <remarks>UE: UGameplayAbility::GetCurrentActorInfo</remarks>
        public GameplayAbilityActorInfo CurrentActorInfo { get; private set; }

        /// <summary>이 인스턴스의 Spec 핸들.</summary>
        /// <remarks>UE: UGameplayAbility::GetCurrentAbilitySpecHandle</remarks>
        public GameplayAbilitySpecHandle CurrentSpecHandle { get; private set; }

        /// <remarks>UE: UGameplayAbility::GetAbilitySystemComponentFromActorInfo</remarks>
        public AbilitySystemComponent GetAbilitySystemComponentFromActorInfo()
        {
            return CurrentActorInfo?.AbilitySystemComponent;
        }

        /// <remarks>UE: UGameplayAbility::GetOwningActorFromActorInfo</remarks>
        public GameObject GetOwningActorFromActorInfo()
        {
            return CurrentActorInfo?.OwnerActor;
        }

        /// <remarks>UE: UGameplayAbility::GetAvatarActorFromActorInfo</remarks>
        public GameObject GetAvatarActorFromActorInfo()
        {
            return CurrentActorInfo?.AvatarActor;
        }

        /// <summary>ASC에 부여된 직후 호출된다(Spec은 이미 ASC 목록에 있다). 재정의하면 base를 먼저 불러야 액터 정보가 채워진다.</summary>
        /// <remarks>UE: UGameplayAbility::OnGiveAbility</remarks>
        public virtual void OnGiveAbility(GameplayAbilityActorInfo actorInfo, GameplayAbilitySpec spec)
        {
            SetCurrentActorInfo(spec.Handle, actorInfo);

            if (actorInfo != null && actorInfo.AvatarActor != null)
            {
                OnAvatarSet(actorInfo, spec);
            }
        }

        /// <summary>ASC에서 제거되기 직전에 호출된다(Spec은 아직 ASC 목록에 있다).</summary>
        /// <remarks>UE: UGameplayAbility::OnRemoveAbility</remarks>
        public virtual void OnRemoveAbility(GameplayAbilityActorInfo actorInfo, GameplayAbilitySpec spec)
        {
        }

        /// <summary>Avatar가 정해지거나 바뀌면 호출된다. 패시브 시작 등 BeginPlay 성격의 처리를 둔다.</summary>
        /// <remarks>UE: UGameplayAbility::OnAvatarSet</remarks>
        public virtual void OnAvatarSet(GameplayAbilityActorInfo actorInfo, GameplayAbilitySpec spec)
        {
        }

        /// <remarks>UE: UGameplayAbility::SetCurrentActorInfo</remarks>
        protected void SetCurrentActorInfo(GameplayAbilitySpecHandle handle, GameplayAbilityActorInfo actorInfo)
        {
            CurrentActorInfo = actorInfo;
            CurrentSpecHandle = handle;
        }
    }
}
