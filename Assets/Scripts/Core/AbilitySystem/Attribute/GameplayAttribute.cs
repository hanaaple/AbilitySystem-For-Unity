using System;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    /// <summary>
    /// 직렬화 가능한 어트리뷰트 참조 — "어느 AttributeSet의 어느 필드냐" 한 쌍 (UE: <c>FGameplayAttribute</c>).
    /// <para><see cref="AttributeHandle"/>는 FieldInfo를 캐싱하는 런타임 해석 핸들이라 직렬화가 안 되므로,
    /// 저작·직렬화는 이 타입(AQN 문자열 + 필드명)이 담고 런타임에 <see cref="ToAttributeHandle"/>로 변환한다.
    /// 전용 PropertyDrawer가 붙어 이 타입의 모든 직렬화 필드에 Set/Attribute 팝업이 자동 적용된다
    /// — modifier 대상·캡처 대상 등에서 두 문자열을 매번 손으로 두지 않게 하는 것이 목적이다.</para>
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

        /// <summary>정적 <see cref="AttributeHandle"/>(예: <c>CharacterAttributeSet.Speed</c>)로부터 만든다 — 핸들의 타입·필드명을 옮긴다.</summary>
        public GameplayAttribute(AttributeHandle handle)
        {
            this.attributeSetTypeName = handle.SetType?.AssemblyQualifiedName;
            this.fieldName = handle.Name;
        }

        /// <summary>대상 AttributeSet 타입의 AssemblyQualifiedName. 미지정이면 비어 있다.</summary>
        public string AttributeSetTypeName => attributeSetTypeName;

        /// <summary>대상 필드명.</summary>
        public string FieldName => fieldName;

        /// <summary>Set·Field가 모두 지정됐는지(문자열 존재 여부 — 타입 해석 성공까지 보장하진 않는다. 해석은 <see cref="ToAttributeHandle"/>).</summary>
        public bool IsSet => !string.IsNullOrEmpty(attributeSetTypeName) && !string.IsNullOrEmpty(fieldName);

        /// <summary>AQN(<see cref="AttributeSetTypeName"/>) + <see cref="FieldName"/>으로 런타임 핸들 생성. 타입 해석 실패 시 default(무효).</summary>
        public AttributeHandle ToAttributeHandle()
        {
            Type type = Type.GetType(attributeSetTypeName);
            return type == null ? default : new AttributeHandle(type, fieldName);
        }

        public bool Equals(GameplayAttribute other) =>
            attributeSetTypeName == other.attributeSetTypeName && fieldName == other.fieldName;

        public override bool Equals(object obj) => obj is GameplayAttribute other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(attributeSetTypeName, fieldName);

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
