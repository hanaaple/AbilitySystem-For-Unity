using System;
using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>AttributeBased Magnitude가 어트리뷰트를 캡처할 대상.</summary>
    public enum AttributeCaptureSource
    {
        Source,   // GE를 발동한 주체(Context.Instigator)의 어트리뷰트
        Target,   // GE를 받는 대상의 어트리뷰트
    }

    /// <summary>캡처할 어트리뷰트 값 종류.</summary>
    public enum AttributeCaptureValueType
    {
        CurrentValue,
        BaseValue,
    }

    /// <summary>
    /// 캡처한 어트리뷰트 값에서 magnitude를 유도하는 AttributeBased 계산 정의.
    /// 계산식: (capturedValue + preMultiplyAdditive) * coefficient + postMultiplyAdditive
    /// </summary>
    [Serializable]
    public struct AttributeBasedMagnitude
    {
        [SerializeField] private AttributeCaptureSource captureSource;
        [SerializeField] private string attributeSetTypeName;
        [SerializeField] private string fieldName;
        [SerializeField] private AttributeCaptureValueType captureValueType;
        [SerializeField] private float coefficient;
        [SerializeField] private float preMultiplyAdditive;
        [SerializeField] private float postMultiplyAdditive;

        public AttributeCaptureSource CaptureSource => captureSource;
        public AttributeCaptureValueType CaptureValueType => captureValueType;
        public float Coefficient => coefficient;
        public float PreMultiplyAdditive => preMultiplyAdditive;
        public float PostMultiplyAdditive => postMultiplyAdditive;

        /// <summary>attributeSetTypeName(AssemblyQualifiedName) + fieldName으로 캡처 대상 핸들 생성.</summary>
        public AttributeHandle ToAttributeHandle()
        {
            Type type = Type.GetType(attributeSetTypeName);
            return type == null ? default : new AttributeHandle(type, fieldName);
        }
    }
}
