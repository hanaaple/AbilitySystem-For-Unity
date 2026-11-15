using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.AbilitySystem.Attribute
{
    /// <summary>"어느 AttributeSet의 어느 필드냐"를 문자열 쌍(AQN + 필드명)으로 담는 직렬화용 참조 데이터. 런타임 해석·값 접근은 <see cref="GameplayAttributeHandle"/>이 맡는다(<see cref="ToAttributeHandle"/>로 변환).</summary>
    [Serializable]
    public struct GameplayAttribute : IEquatable<GameplayAttribute>
    {
        [SerializeField] [FormerlySerializedAs("attributeSetTypeName")] private string _attributeSetTypeName;
        [SerializeField] [FormerlySerializedAs("fieldName")] private string _fieldName;

        public GameplayAttribute(string attributeSetTypeName, string fieldName)
        {
            _attributeSetTypeName = attributeSetTypeName;
            _fieldName = fieldName;
        }

        public GameplayAttribute(GameplayAttributeHandle handle)
        {
            _attributeSetTypeName = handle.SetType?.AssemblyQualifiedName;
            _fieldName = handle.Name;
        }

        public string AttributeSetTypeName => _attributeSetTypeName;

        public string FieldName => _fieldName;

        /// <summary>Set·Field 문자열이 모두 있는지. 타입 해석 성공까진 보장하지 않는다(그건 <see cref="ToAttributeHandle"/>).</summary>
        public bool IsSet => !string.IsNullOrEmpty(_attributeSetTypeName) && !string.IsNullOrEmpty(_fieldName);

        /// <summary>런타임 핸들로 해석한다. 타입 해석 실패 시 default(무효).</summary>
        public GameplayAttributeHandle ToAttributeHandle()
        {
            Type type = Type.GetType(_attributeSetTypeName);
            return type == null ? default : new GameplayAttributeHandle(type, _fieldName);
        }

        public bool Equals(GameplayAttribute other)
        {
            return _attributeSetTypeName == other._attributeSetTypeName && _fieldName == other._fieldName;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAttribute other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_attributeSetTypeName, _fieldName);
        }

        public override string ToString()
        {
            if (!IsSet)
            {
                return "(none)";
            }

            Type type = Type.GetType(_attributeSetTypeName);
            return $"{(type != null ? type.Name : _attributeSetTypeName)}.{_fieldName}";
        }
    }
}
