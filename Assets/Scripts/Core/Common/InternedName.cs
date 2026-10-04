using System;
using UnityEngine;

namespace Core.Common
{
    /// <summary> 항상 인터닝을 보장하는 string 래퍼 클래스. </summary>
    [Serializable]
    public struct InternedName : IEquatable<InternedName>, ISerializationCallbackReceiver
    {
        [SerializeField] private string name;

        public InternedName(string inName)
        {
            name = Intern(inName);
        }

        public bool Equals(InternedName other)
        {
            // 생성·역직렬화 모든 경로에서 인터닝을 보장하므로 같은 내용은 같은 인스턴스 → 참조 비교로 충분하다.
            return ReferenceEquals(name, other.name);
        }

        public override bool Equals(object inName)
        {
            return inName is InternedName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return name != null ? name.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return name;
        }

        // Unity 역직렬화는 생성자를 우회하고 필드를 직접 채우므로, 로드 직후 다시 인터닝해야 참조 비교가 성립한다.
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            name = Intern(name);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        private static string Intern(string inName)
        {
            return string.IsNullOrEmpty(inName) ? null : string.Intern(inName);
        }
    }
}
