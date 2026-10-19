using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>modifier 크기(magnitude)를 어떻게 정할지 — 계산 방식(<see cref="MagnitudeCalculationType"/>)과 방식별 값. GE Modifier와 Execution의 Calculation Modifier가 함께 쓴다.</summary>
    /// <remarks>UE: FGameplayEffectModifierMagnitude</remarks>
    [Serializable]
    public struct GameplayEffectModifierMagnitude
    {
        [SerializeField] private MagnitudeCalculationType magnitudeCalculationType;

        // ScalableFloat 전용. TODO Level 기반 커브 테이블로 교체 예정. 현재는 고정 float.
        [SerializeField] private float scalableFloatMagnitude;

        // AttributeBased 전용. 캡처 어트리뷰트 기반 magnitude 계산 정의.
        [SerializeField] private AttributeBasedMagnitude attributeBasedMagnitude;

        public MagnitudeCalculationType MagnitudeCalculationType => magnitudeCalculationType;

        /// <summary>AttributeBased 계산 정의. <see cref="MagnitudeCalculationType"/>가 AttributeBased일 때만 의미가 있다.</summary>
        public AttributeBasedMagnitude AttributeBasedMagnitude => attributeBasedMagnitude;

        /// <summary>최종 magnitude를 계산한다 — ScalableFloat이면 고정값, AttributeBased면 spec 캡처값으로. 캡처(Source 생성 시·Target 적용 시) 이후에 호출해야 한다.</summary>
        /// <remarks>UE: FGameplayEffectModifierMagnitude::AttemptCalculateMagnitude</remarks>
        public bool AttemptCalculateMagnitude(GameplayEffectSpec spec, out float outCalculatedMagnitude)
        {
            bool bCanCalc = CanCalculateMagnitude(spec);

            if (bCanCalc)
            {
                switch (magnitudeCalculationType)
                {
                    case MagnitudeCalculationType.ScalableFloat:
                    {
                        outCalculatedMagnitude = scalableFloatMagnitude;
                        break;
                    }

                    case MagnitudeCalculationType.AttributeBased:
                    {
                        outCalculatedMagnitude = attributeBasedMagnitude.Evaluate(spec);
                        break;
                    }

                    // 구현 전이라 계산 실패로 돌려, 조용히 0이 되지 않고 Spec이 경고를 남기게 한다
                    case MagnitudeCalculationType.CustomCalculationClass:
                    case MagnitudeCalculationType.SetByCaller:
                    {
                        outCalculatedMagnitude = 0;
                        bCanCalc = false;
                        break;
                    }

                    default:
                    {
                        outCalculatedMagnitude = 0;
                        break;
                    }
                }
            }
            else
            {
                outCalculatedMagnitude = 0f;
            }

            return bCanCalc;
        }

        /// <remarks>UE: FGameplayEffectModifierMagnitude::CanCalculateMagnitude</remarks>
        public bool CanCalculateMagnitude(GameplayEffectSpec spec)
        {
            List<GameplayEffectAttributeCaptureDefinition> reqCaptureDefs = GetAttributeCaptureDefinitions();

            return spec.HasValidCapturedAttributes(reqCaptureDefs);
        }

        /// <summary>바뀐 aggregator가 이 magnitude의 non-snapshot 캡처 대상이면 다시 계산한다.</summary>
        /// <remarks>UE: FGameplayEffectModifierMagnitude::AttemptRecalculateMagnitudeFromDependentAggregatorChange</remarks>
        public bool AttemptRecalculateMagnitudeFromDependentAggregatorChange(GameplayEffectSpec gameplayEffectSpec, AttributeAggregator changedAggregator, out float outCalculatedMagnitude)
        {
            List<GameplayEffectAttributeCaptureDefinition> reqCaptureDefs = GetAttributeCaptureDefinitions();
            outCalculatedMagnitude = 0;

            foreach (GameplayEffectAttributeCaptureDefinition captureDef in reqCaptureDefs)
            {
                if (!captureDef.Snapshot)
                {
                    GameplayEffectAttributeCaptureSpec capturedSpec = gameplayEffectSpec.CapturedRelevantAttributes.FindCaptureSpecByDefinition(captureDef, true);
                    if (capturedSpec.IsValid && capturedSpec.ShouldRefreshLinkedAggregator(changedAggregator))
                    {
                        return AttemptCalculateMagnitude(gameplayEffectSpec, out outCalculatedMagnitude);
                    }
                }
            }

            return false;
        }

        /// <summary>이 magnitude를 계산하는 데 필요한 캡처 정의들.</summary>
        /// <remarks>UE: FGameplayEffectModifierMagnitude::GetAttributeCaptureDefinitions</remarks>
        public List<GameplayEffectAttributeCaptureDefinition> GetAttributeCaptureDefinitions()
        {
            List<GameplayEffectAttributeCaptureDefinition> outCaptureDefs = new();
            switch (magnitudeCalculationType)
            {
                case MagnitudeCalculationType.AttributeBased:
                {
                    outCaptureDefs.Add(attributeBasedMagnitude.BackingAttribute);
                    break;
                }
            }

            return outCaptureDefs;
        }
    }
}
