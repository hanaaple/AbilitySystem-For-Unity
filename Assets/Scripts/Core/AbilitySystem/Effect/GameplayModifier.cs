using System;
using UnityEngine;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    [Serializable]
    public struct GameplayModifier
    {
        [SerializeField] private string attributeSetTypeName;
        [SerializeField] private string fieldName;

        [SerializeField] private GameplayModifierOperation operation;

        [SerializeField] private MagnitudeCalculationType magnitudeCalculationType;

        // TODO ScalableFloat — Level 기반 커브 테이블로 교체 예정. 현재는 고정 float.
        [SerializeField] private float magnitude;

        public GameplayModifierOperation Operation => operation;

        /// <summary>현재는 ScalableFloat 고정값. 추후 Level 기반 커브 조회로 교체 예정.</summary>
        public float GetMagnitude(float level) => magnitude;

        /// <summary>attributeSetTypeName(AssemblyQualifiedName) + fieldName으로 AttributeHandle을 생성한다.</summary>
        public AttributeHandle ToAttributeHandle()
        {
            Type type = Type.GetType(attributeSetTypeName);
            if (type == null)
            {
                return default;
            }
            return new AttributeHandle(type, fieldName);
        }
    }
}
