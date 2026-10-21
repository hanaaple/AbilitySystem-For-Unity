using System;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>부여된 어빌리티(<see cref="GameplayAbilitySpec"/>)를 가리킨다. Spec을 만들 때 발급된다.</summary>
    /// <remarks>UE: FGameplayAbilitySpecHandle</remarks>
    public readonly struct GameplayAbilitySpecHandle : IEquatable<GameplayAbilitySpecHandle>
    {
        public static readonly GameplayAbilitySpecHandle Invalid = default;

        private static int _handleIdCounter;

        private readonly int _id;

        private GameplayAbilitySpecHandle(int id)
        {
            _id = id;
        }

        public bool IsValid => _id != 0;

        /// <summary>새 핸들을 발급한다. 발급기가 static이라 ASC 간 전역 유일하다.</summary>
        /// <remarks>UE: FGameplayAbilitySpecHandle::GenerateNewHandle</remarks>
        public static GameplayAbilitySpecHandle GenerateNewHandle()
        {
            return new GameplayAbilitySpecHandle(++_handleIdCounter);
        }

        public bool Equals(GameplayAbilitySpecHandle other)
        {
            return _id == other._id;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAbilitySpecHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id;
        }
    }
}
