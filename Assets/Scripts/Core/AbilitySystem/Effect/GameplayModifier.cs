using System;
using System.Collections.Generic;
using Core.AbilitySystem.Aggregator;
using UnityEngine;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE가 어트리뷰트 하나를 어떻게 바꿀지 정의하는 단위.
    /// 대상 어트리뷰트 + 연산(<see cref="GameplayModifierOperation"/>) + magnitude 계산 방식(<see cref="MagnitudeCalculationType"/>)으로 구성된다.
    /// </summary>
    // TODO(editor): 전용 PropertyDrawer로 분리 검토 — 지금은 GameplayEffectAssetDrawer 안에서만 그려 유일 소비처면 불필요.
    [Serializable]
    public struct GameplayModifier
    {
        // 대상 어트리뷰트(어느 Set의 어느 필드).
        [SerializeField] private GameplayAttribute attribute;

        [SerializeField] private GameplayModifierOperation operation;

        [SerializeField] private MagnitudeCalculationType magnitudeCalculationType;

        // ScalableFloat 전용. TODO Level 기반 커브 테이블로 교체 예정. 현재는 고정 float.
        [SerializeField] private float magnitude;

        // AttributeBased 전용. 캡처 어트리뷰트 기반 magnitude 계산 정의.
        [SerializeField] private AttributeBasedMagnitude attributeBased;

        public GameplayModifierOperation Operation => operation;
        public MagnitudeCalculationType MagnitudeCalculationType => magnitudeCalculationType;

        public GameplayAttribute Attribute => attribute;

        /// <summary>AttributeBased 계산 정의. <see cref="MagnitudeCalculationType"/>가 AttributeBased일 때만 의미가 있다.</summary>
        public AttributeBasedMagnitude AttributeBased => attributeBased;

        /// <summary>
        /// 이 modifier의 최종 magnitude를 계산한다. ScalableFloat이면 고정값, AttributeBased면 spec의 캡처값으로 계산한다.
        /// AttributeBased는 캡처값을 읽으므로 캡처(Source는 생성 시·Target은 적용 시)가 끝난 뒤에 호출해야 한다.
        /// </summary>
        public bool AttemptCalculateMagnitude(GameplayEffectSpec spec, out float outCalculatedMagnitude)
        {
            bool bCanCalc = CanCalculateMagnitude(spec);

            if (bCanCalc)
            {
                float level = spec.Level;

                switch (magnitudeCalculationType)
                {
                    case MagnitudeCalculationType.ScalableFloat:
                    {
                        outCalculatedMagnitude = magnitude;
                        break;
                    }

                    case MagnitudeCalculationType.AttributeBased:
                    {
                        outCalculatedMagnitude = attributeBased.Evaluate(spec);
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

        private bool CanCalculateMagnitude(GameplayEffectSpec spec)
        {
            List<GameplayEffectAttributeCaptureDefinition> reqCaptureDefs = GetAttributeCaptureDefinitions();

            return spec.HasValidCapturedAttributes(reqCaptureDefs);
        }

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

        private List<GameplayEffectAttributeCaptureDefinition> GetAttributeCaptureDefinitions()
        {
            List<GameplayEffectAttributeCaptureDefinition> outCaptureDefs = new();
            switch (MagnitudeCalculationType)
            {
                case MagnitudeCalculationType.AttributeBased:
                {
                    outCaptureDefs.Add(AttributeBased.BackingAttribute);
                    break;
                }

                // case MagnitudeCalculationType.CustomCalculationClass:
                // {
                //     break;
                // }
            }

            return outCaptureDefs;
        }

        public GameplayAttributeHandle ToResolvedAttribute()
        {
            return attribute.ToAttributeHandle();
        }
    }
}
