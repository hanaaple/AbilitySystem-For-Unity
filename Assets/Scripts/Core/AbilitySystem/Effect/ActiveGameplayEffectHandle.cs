using System;

namespace Core.AbilitySystem.Effect
{
    /// <summary>ApplyEffect 반환값. RemoveEffect 호출 시 사용.</summary>
    public readonly struct ActiveGameplayEffectHandle : IEquatable<ActiveGameplayEffectHandle>
    {
        public static readonly ActiveGameplayEffectHandle Invalid = default;

        private readonly int _id;

        // 이 핸들을 발급한 소유 ASC. aggregator의 dirty 전파가 dependent 핸들만으로 소유 ASC를 찾아가는 경로에 쓰인다.
        private readonly AbilitySystemComponent _owner;

        public bool IsValid => _id != 0;

        public ActiveGameplayEffectHandle(int id, AbilitySystemComponent owner)
        {
            _id = id;
            _owner = owner;
        }

        public AbilitySystemComponent GetOwningAbilitySystemComponent()
        {
            return _owner;
        }

        // 식별은 _id만으로 한다 — 발급기가 static이라 ASC 간 전역 유일하다. _owner는 조회용 payload일 뿐 identity가 아니다.
        public bool Equals(ActiveGameplayEffectHandle other)
        {
            return _id == other._id;
        }

        public override bool Equals(object obj)
        {
            return obj is ActiveGameplayEffectHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id;
        }
    }
}
