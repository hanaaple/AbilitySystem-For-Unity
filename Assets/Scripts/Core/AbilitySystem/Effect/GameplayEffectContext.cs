using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE 발생 맥락 정보. Instigator(발동 주체), EffectCauser(실제 원인 오브젝트),
    /// HitResult(피격 정보) 등을 담는다.
    /// </summary>
    public class GameplayEffectContext
    {
        /// <summary>GE를 발동한 주체의 ASC. 장비·패시브 등 자가 적용 시 Target과 동일.</summary>
        public AbilitySystemComponent Instigator { get; }

        /// <summary>실제 원인 오브젝트. 총알·폭발물 등 Instigator와 다를 수 있음.</summary>
        public GameObject EffectCauser { get; }

        // TODO: RaycastHit? HitResult
        // TODO: Vector3? WorldOrigin

        public GameplayEffectContext(AbilitySystemComponent instigator, GameObject effectCauser = null)
        {
            Instigator = instigator;
            EffectCauser = effectCauser;
        }
    }
}
