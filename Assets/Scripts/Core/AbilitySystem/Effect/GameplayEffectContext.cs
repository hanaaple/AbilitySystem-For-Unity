using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>GE 발생 맥락 정보 — Instigator(발동 주체), EffectCauser(실제 원인 오브젝트), HitResult(피격 정보) 등을 담는다.</summary>
    public class GameplayEffectContext
    {
        private bool _hasHitResult;
        private RaycastHit _hitResult;

        /// <summary>GE를 발동한 주체의 ASC. 장비·패시브 등 자가 적용 시 Target과 동일.</summary>
        public AbilitySystemComponent Instigator { get; private set; }

        /// <summary>실제 원인 오브젝트. 총알·폭발물 등 Instigator와 다를 수 있음.</summary>
        public GameObject EffectCauser { get; private set; }

        /// <summary>이 GE의 출처 오브젝트(무기·아이템·능력 등). Instigator(주체 ASC)와 별개로 전달한다.</summary>
        public GameObject SourceObject { get; private set; }

        public void AddInstigator(AbilitySystemComponent instigator, GameObject effectCauser = null)
        {
            Instigator = instigator;
            EffectCauser = effectCauser;
        }

        public void AddSourceObject(GameObject sourceObject)
        {
            SourceObject = sourceObject;
        }

        /// <summary>피격 정보(무엇의 어디를 맞혔나)를 담는다. 이미 있으면 <paramref name="reset"/>일 때만 덮어쓴다.</summary>
        /// <remarks>UE: FGameplayEffectContext::AddHitResult</remarks>
        public void AddHitResult(RaycastHit hitResult, bool reset = false)
        {
            if (_hasHitResult && !reset)
            {
                Debug.LogError("GameplayEffectContext.AddHitResult: 피격 정보가 이미 있습니다 — 덮어쓰려면 reset을 켭니다.");
                return;
            }

            _hitResult = hitResult;
            _hasHitResult = true;
        }

        /// <summary>담긴 피격 정보를 꺼낸다. 없으면 false.</summary>
        /// <remarks>UE: FGameplayEffectContext::GetHitResult</remarks>
        public bool TryGetHitResult(out RaycastHit hitResult)
        {
            hitResult = _hitResult;
            return _hasHitResult;
        }

        /// <summary>같은 내용의 새 컨텍스트. 한 Spec을 여러 대상에 걸 때 대상마다 피격 정보를 따로 담으려고 쓴다.</summary>
        /// <remarks>UE: FGameplayEffectContext::Duplicate</remarks>
        public virtual GameplayEffectContext Duplicate()
        {
            // 필드가 참조 몇 개와 값 타입(피격 정보)뿐이라 얕은 복사로 독립된다. 파생 클래스의 필드도 같이 복사된다.
            return (GameplayEffectContext)MemberwiseClone();
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
