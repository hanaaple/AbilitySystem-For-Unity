using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>GE 발생 맥락 정보 — Instigator(발동 주체), EffectCauser(실제 원인 오브젝트), HitResult(피격 정보) 등을 담는다.</summary>
    public class GameplayEffectContext
    {
        /// <summary>GE를 발동한 주체의 ASC. 장비·패시브 등 자가 적용 시 Target과 동일.</summary>
        public AbilitySystemComponent Instigator { get; private set; }

        /// <summary>실제 원인 오브젝트. 총알·폭발물 등 Instigator와 다를 수 있음.</summary>
        public GameObject EffectCauser { get; private set; }

        /// <summary>이 GE의 출처 오브젝트(무기·아이템·능력 등). Instigator(주체 ASC)와 별개로 전달한다.</summary>
        public Object SourceObject { get; private set; }

        // TODO: RaycastHit? HitResult
        // TODO: Vector3? WorldOrigin

        public void AddInstigator(AbilitySystemComponent instigator, GameObject effectCauser = null)
        {
            Instigator = instigator;
            EffectCauser = effectCauser;
        }

        public void AddSourceObject(Object sourceObject)
        {
            SourceObject = sourceObject;
        }

        /// <summary>Instigator의 소유 태그를 <paramref name="actorTagContainer"/>에 채운다. Instigator가 없으면 건드리지 않는다.</summary>
        /// <param name="specTagContainer">Spec 쪽 태그 출처(능력 태그 등)가 생기면 채울 자리. 지금은 쓰지 않는다.</param>
        /// <remarks>UE: FGameplayEffectContext::GetOwnedGameplayTags</remarks>
        public void GetOwnedGameplayTags(GameplayTagContainer actorTagContainer, GameplayTagContainer specTagContainer)
        {
            if (Instigator)
            {
                Instigator.GetOwnedGameplayTags(actorTagContainer);
            }
        }
    }
}
