using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.AbilitySystem.Attribute
{
    /// <summary>
    /// "어느 AttributeSet의 어느 필드냐"를 문자열 쌍(AQN + 필드명)으로 담는 직렬화용 참조 데이터.
    /// </summary>
    [Serializable]
    public struct GameplayAttribute : IEquatable<GameplayAttribute>
    {
        [SerializeField] [FormerlySerializedAs("attributeSetTypeName")] private string _attributeSetTypeName;
        [SerializeField] [FormerlySerializedAs("fieldName")] private string _fieldName;

        public string AttributeSetTypeName => _attributeSetTypeName;
        public string FieldName => _fieldName;
        ///<summary>attributeSet·Field 문자열 중 하나라도 비었는지. 비지 않아도 타입 해석 성공까진 보장하지 않는다(그건 <see cref="ToAttributeHandle"/>).</summary>
        public bool IsEmpty => string.IsNullOrEmpty(_attributeSetTypeName) || string.IsNullOrEmpty(_fieldName);

        public GameplayAttribute(string attributeSetTypeName, string fieldName)
        {
            _attributeSetTypeName = attributeSetTypeName;
            _fieldName = fieldName;
        }

        public GameplayAttribute(GameplayAttributeHandle handle)
        {
            _attributeSetTypeName = handle.AttributeSetType?.AssemblyQualifiedName;
            _fieldName = handle.FieldName;
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
            if (IsEmpty)
            {
                return "(none)";
            }

            var type = Type.GetType(_attributeSetTypeName);
            return $"{(type != null ? type.Name : _attributeSetTypeName)}.{_fieldName}";
        }

        public GameplayAttributeHandle ToAttributeHandle()
        {
            if (IsEmpty)
            {
                return default;
            }

            var type = Type.GetType(_attributeSetTypeName);
            GameplayAttributeHandle.TryCreate(type, _fieldName, out GameplayAttributeHandle handle);
            return handle;
        }

        public bool Equals(GameplayAttribute other)
        {
            return _attributeSetTypeName == other._attributeSetTypeName && _fieldName == other._fieldName;
        }
    }
}
