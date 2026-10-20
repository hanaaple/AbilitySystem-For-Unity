using System;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Character
{
    public sealed class SpeedBoostExecution : GameplayEffectExecution
    {
        private const float SpeedBonus = 10f;

        private static readonly GameplayEffectAttributeCaptureDefinition A = new GameplayEffectAttributeCaptureDefinition(CharacterAttributeSet.Health, AttributeCaptureSource.Target, true);
        private static readonly GameplayEffectAttributeCaptureDefinition B = new GameplayEffectAttributeCaptureDefinition(CharacterAttributeSet.Stamina, AttributeCaptureSource.Source, false);
        public static readonly GameplayEffectAttributeCaptureDefinition[] defs = { A, B };

        public override ReadOnlySpan<GameplayEffectAttributeCaptureDefinition> Defs()
        {
            return defs;
        }

        public override void Execute(GameplayEffectExecutionParameters parameters, GameplayEffectExecutionOutput output)
        {
            // UE Execution 관례대로 Spec의 Source·Target 태그로 캡처 어트리뷰트의 mod 자격을 판정한다
            GameplayEffectSpec spec = parameters.Spec;
            TagContainerAggregator capturedSourceTags = spec.CapturedSourceTags;
            TagContainerAggregator capturedTargetTags = spec.CapturedTargetTags;
            var evaluationParameters = new AggregatorEvaluateParameters(capturedSourceTags.GetAggregatedTags(), capturedTargetTags.GetAggregatedTags());

            float result;
            parameters.AttemptCalculateCapturedAttributeMagnitude(A, evaluationParameters, out float health);
            parameters.AttemptCalculateCapturedAttributeMagnitude(B, evaluationParameters, out float stamina);

            result = health + stamina;
            result = result * 0.01f + SpeedBonus;

            Debug.Log($"{health} + {stamina} = {result}");

            output.AddOutputModifier(CharacterAttributeSet.Speed, result, GameplayModifierOperation.AddBase);
        }
    }
}
