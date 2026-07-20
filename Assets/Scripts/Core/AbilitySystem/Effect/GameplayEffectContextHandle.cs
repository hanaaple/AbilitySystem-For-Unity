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

        public GameObject EffectCauser => _context?.EffectCauser;

        /// <summary>발동 주체 ASC를 반환한다. (UE: GetInstigatorAbilitySystemComponent)</summary>
        public AbilitySystemComponent GetInstigator() => _context?.Instigator;

        /// <summary>출처 오브젝트를 반환한다. (UE: GetSourceObject)</summary>
        public Object GetSourceObject() => _context?.SourceObject;

        /// <summary>발동 주체(Instigator)와 원인 오브젝트(EffectCauser)를 넣는다. (UE: AddInstigator)</summary>
        public void AddInstigator(AbilitySystemComponent instigator, GameObject effectCauser = null)
        {
            _context?.AddInstigator(instigator, effectCauser);
        }

        /// <summary>출처 오브젝트를 컨텍스트에 넣는다. (UE: AddSourceObject)</summary>
        public void AddSourceObject(Object sourceObject)
        {
            _context?.AddSourceObject(sourceObject);
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
