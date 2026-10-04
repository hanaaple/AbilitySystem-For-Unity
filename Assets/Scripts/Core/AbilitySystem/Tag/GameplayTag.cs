using System;
using Core.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.AbilitySystem.Tag
{
    /// <summary>계층 문자열(예: <c>Status.Debuff.Stun</c>)로 표현되는 태그 식별자.</summary>
    [Serializable]
    public struct GameplayTag : IEquatable<GameplayTag>
    {
        [SerializeField] private InternedName name;

        public GameplayTag(InternedName name)
        {
            this.name = name;
        }

        public GameplayTag(string name)
        {
            this.name = new InternedName(name);
        }

        public bool Equals(GameplayTag other)
        {
            return name.Equals(other.name);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}
