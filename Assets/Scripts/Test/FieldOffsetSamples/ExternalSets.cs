using System;
using Core.AbilitySystem.Attribute;
using UnityEngine;

// 필드 값은 정확도 체크가 리플렉션으로만 읽고 쓴다 — 미사용·미할당 경고를 끈다
#pragma warning disable CS0169, CS0414, CS0649

namespace Test.FieldOffsetSamples
{
    // FieldOffset 정확도 체크용 — 체크 코드와 다른 어셈블리(asmdef)에 선언된 AttributeSet.
    // 접근 제한자·직렬화 특성을 섞고 AttributeData 사이에 다른 타입 필드를 끼워 레이아웃을 흔든다.
    public class ExternalBaseSet : AttributeSet
    {
        public AttributeData PublicData;
        private byte _padByte;
        internal AttributeData InternalData;
        private double _padDouble;
        protected AttributeData ProtectedData;
        private string _padString;
        protected internal AttributeData ProtectedInternalData;
        private object _padObject;
        private protected AttributeData PrivateProtectedData;
        [SerializeField] private AttributeData _serializedPrivateData;
        [NonSerialized] public AttributeData NonSerializedPublicData;
        [NonSerialized] private AttributeData _nonSerializedPrivateData;
        public readonly AttributeData ReadonlyData;
        private int _padInt;
    }

    public class ExternalDerivedSet : ExternalBaseSet
    {
        public AttributeData DerivedPublicData;
        private byte _derivedPadByte;
        internal AttributeData DerivedInternalData;
        protected AttributeData DerivedProtectedData;
        [SerializeField] private AttributeData _derivedSerializedPrivateData;
        [NonSerialized] private AttributeData _derivedNonSerializedPrivateData;
    }
}
