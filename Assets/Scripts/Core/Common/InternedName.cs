using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Common
{
    /// <summary> 항상 인터닝을 보장하는 string 래퍼 클래스. </summary>
    [Serializable]
    public struct InternedName : IEquatable<InternedName>, ISerializationCallbackReceiver
    {
        [SerializeField] [FormerlySerializedAs("name")] private string _name;

        [NonSerialized] private int _hash;

        public InternedName(string inName)
        {
            _name = Intern(inName);
            _hash = ComputeHash(_name);
        }

        public override bool Equals(object inName)
        {
            return inName is InternedName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return _name;
        }

        public bool Equals(InternedName other)
        {
            // 생성·역직렬화 모든 경로에서 인터닝을 보장하므로 같은 내용은 같은 인스턴스 → 참조 비교로 충분하다.
            return ReferenceEquals(_name, other._name);
        }

        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(_name);
        }

        // Unity 역직렬화는 생성자를 우회하고 필드를 직접 채우므로, 로드 직후 다시 인터닝해야 참조 비교가 성립하고 해시도 채워진다.
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _name = Intern(_name);
            _hash = ComputeHash(_name);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        private static string Intern(string inName)
        {
            return string.IsNullOrEmpty(inName) ? null : string.Intern(inName);
        }

        // default(InternedName)의 해시(0)와 맞춰 null은 0으로 둔다.
        private static int ComputeHash(string internedName)
        {
            return internedName != null ? internedName.GetHashCode() : 0;
        }
    }
}
