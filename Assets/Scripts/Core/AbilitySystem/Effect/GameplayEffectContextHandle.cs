using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>GameplayEffectContext의 경량 래퍼 — GE 적용 시 맥락 전달용. 장비·패시브처럼 맥락이 불필요하면 default(Empty)로 전달한다.</summary>
    public readonly struct GameplayEffectContextHandle
    {
        private readonly GameplayEffectContext _context;

        public bool IsValid => _context != null;

        public GameObject EffectCauser => _context?.EffectCauser;

        public AbilitySystemComponent GetInstigator()
        {
            return _context?.Instigator;
        }

        public Object GetSourceObject()
        {
            return _context?.SourceObject;
        }

        public void AddInstigator(AbilitySystemComponent instigator, GameObject effectCauser = null)
        {
            _context?.AddInstigator(instigator, effectCauser);
        }

        public void AddSourceObject(GameObject sourceObject)
        {
            _context?.AddSourceObject(sourceObject);
        }

        /// <remarks>UE: FGameplayEffectContextHandle::AddHitResult</remarks>
        public void AddHitResult(RaycastHit hitResult, bool reset = false)
        {
            _context?.AddHitResult(hitResult, reset);
        }

        /// <remarks>UE: FGameplayEffectContextHandle::GetHitResult</remarks>
        public bool TryGetHitResult(out RaycastHit hitResult)
        {
            hitResult = default;
            return _context != null && _context.TryGetHitResult(out hitResult);
        }

        /// <remarks>UE: FGameplayEffectContextHandle::GetOwnedGameplayTags</remarks>
        public void GetOwnedGameplayTags(GameplayTagContainer actorTagContainer, GameplayTagContainer specTagContainer)
        {
            _context?.GetOwnedGameplayTags(actorTagContainer, specTagContainer);
        }

        /// <summary>담긴 컨텍스트를 복제한 새 핸들. 비어 있으면 빈 핸들.</summary>
        /// <remarks>UE: FGameplayEffectContextHandle::Duplicate</remarks>
        public GameplayEffectContextHandle Duplicate()
        {
            return new GameplayEffectContextHandle(_context?.Duplicate());
        }

        public bool TryGetContext<T>(out T context) where T : GameplayEffectContext
        {
            context = _context as T;
            return context != null;
        }

        public GameplayEffectContextHandle(GameplayEffectContext context)
        {
            _context = context;
        }
    }
}
