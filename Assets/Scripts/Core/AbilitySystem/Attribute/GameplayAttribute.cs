using System;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    /// <summary>
    /// "어느 AttributeSet의 어느 필드냐"를 문자열 쌍(AQN + 필드명)으로 담는 직렬화용 참조 데이터.
    /// 런타임 해석·값 접근은 <see cref="GameplayAttributeHandle"/>이 맡는다(<see cref="ToAttributeHandle"/>로 변환).
    /// </summary>
    [Serializable]
    public struct GameplayAttribute : IEquatable<GameplayAttribute>
    {
        [SerializeField] private string attributeSetTypeName;
        [SerializeField] private string fieldName;

        public GameplayAttribute(string attributeSetTypeName, string fieldName)
        {
            this.attributeSetTypeName = attributeSetTypeName;
            this.fieldName = fieldName;
        }

        public GameplayAttribute(GameplayAttributeHandle handle)
        {
            this.attributeSetTypeName = handle.SetType?.AssemblyQualifiedName;
            this.fieldName = handle.Name;
        }

        public string AttributeSetTypeName => attributeSetTypeName;

        public string FieldName => fieldName;

        /// <summary>Set·Field 문자열이 모두 있는지. 타입 해석 성공까진 보장하지 않는다(그건 <see cref="ToAttributeHandle"/>).</summary>
        public bool IsSet => !string.IsNullOrEmpty(attributeSetTypeName) && !string.IsNullOrEmpty(fieldName);

        /// <summary>런타임 핸들로 해석한다. 타입 해석 실패 시 default(무효).</summary>
        public GameplayAttributeHandle ToAttributeHandle()
        {
            Type type = Type.GetType(attributeSetTypeName);
            return type == null ? default : new GameplayAttributeHandle(type, fieldName);
        }

        public bool Equals(GameplayAttribute other)
        {
            return attributeSetTypeName == other.attributeSetTypeName && fieldName == other.fieldName;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAttribute other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(attributeSetTypeName, fieldName);
        }

        public override string ToString()
        {
            if (!IsSet)
            {
                return "(none)";
            }

            Type type = Type.GetType(attributeSetTypeName);
            return $"{(type != null ? type.Name : attributeSetTypeName)}.{fieldName}";
        }
    }
}
