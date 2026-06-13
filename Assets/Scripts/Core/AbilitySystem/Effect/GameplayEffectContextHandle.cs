using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GameplayEffectContext의 경량 래퍼. GE 적용 시 맥락 전달에 사용한다.
    /// 장비·패시브처럼 맥락이 불필요한 경우 default(Empty)로 전달한다.
    /// </summary>
    public readonly struct GameplayEffectContextHandle
    {
        private readonly GameplayEffectContext _context;

        public bool IsValid => _context != null;

        public AbilitySystemComponent Instigator => _context?.Instigator;
        public GameObject EffectCauser => _context?.EffectCauser;

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
