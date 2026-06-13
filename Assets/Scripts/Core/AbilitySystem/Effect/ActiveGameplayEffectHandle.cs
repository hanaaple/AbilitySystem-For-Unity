using System;

namespace Core.AbilitySystem.Effect
{
    /// <summary>ApplyEffect 반환값. RemoveEffect 호출 시 사용.</summary>
    public readonly struct ActiveGameplayEffectHandle : IEquatable<ActiveGameplayEffectHandle>
    {
        public static readonly ActiveGameplayEffectHandle Invalid = default;

        private readonly int _id;
        public bool IsValid => _id != 0;

        public ActiveGameplayEffectHandle(int id)
        {
            _id = id;
        }

        public bool Equals(ActiveGameplayEffectHandle other) => _id == other._id;
        public override bool Equals(object obj) => obj is ActiveGameplayEffectHandle other && Equals(other);
        public override int GetHashCode() => _id;
    }
}
