using System;
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
    /// <para>class인 이유: Unity는 struct 필드 초기화값을 새 인스턴스에 반영하지 않지만
    /// class는 반영하므로, <see cref="coefficient"/> 기본값 1을 필드 초기화로 둘 수 있다(struct면 0이 되어 식 전체가 0).</para>
    /// </summary>
    [Serializable]
    public class AttributeBasedMagnitude
    {
        // 캡처 대상(어디서·무엇을·snapshot 여부). "무엇을 캡처하나"는 여기가, "어떻게 소비하나"(Base/Current)는 captureValueType가 담는다.
        [SerializeField] private GameplayEffectAttributeCaptureDefinition backingAttribute;

        [SerializeField] private AttributeCaptureValueType captureValueType;

        // 기본값 1 — 식이 (value + Pre) * coefficient + Post라 0이면 캡처값이 통째로 사라진다.
        [SerializeField] private float coefficient = 1f;
        [SerializeField] private float preMultiplyAdditive;
        [SerializeField] private float postMultiplyAdditive;

        /// <summary>캡처 대상 정의. <see cref="GameplayEffectSpec.SetupAttributeCaptureDefinitions"/>가 컨테이너에 등록하는 키.</summary>
        public GameplayEffectAttributeCaptureDefinition BackingAttribute => backingAttribute;

        public AttributeCaptureValueType CaptureValueType => captureValueType;
        public float Coefficient => coefficient;
        public float PreMultiplyAdditive => preMultiplyAdditive;
        public float PostMultiplyAdditive => postMultiplyAdditive;

        /// <summary>
        /// 캡처값에서 최종 magnitude를 계산한다: (capturedValue + PreMultiplyAdditive) * Coefficient + PostMultiplyAdditive.
        /// 캡처값은 spec의 캡처 컨테이너에서 조회하므로 캡처가 끝난 뒤 호출해야 한다.
        /// 조회 실패 시 캡처값 0으로 계산하되 경고를 남긴다 — 0이 조용히 흘러 결과가 틀리는 것을 막기 위함이다.
        /// </summary>
        public float Evaluate(GameplayEffectSpec spec)
        {
            if (!spec.CapturedRelevantAttributes.TryGetCapturedValue(backingAttribute, captureValueType, out float capturedValue))
            {
                Debug.LogWarning($"[AttributeBased] 캡처값 조회 실패 — '{spec.Definition.name}'. 캡처값 0으로 계산한다(미등록·미캡처·무효 캡처).");
            }

            return (capturedValue + preMultiplyAdditive) * coefficient + postMultiplyAdditive;
        }
    }
}
