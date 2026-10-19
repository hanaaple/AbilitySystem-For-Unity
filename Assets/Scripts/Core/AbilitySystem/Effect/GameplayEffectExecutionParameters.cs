using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution에 넘기는 입력 묶음. 출력은 별도 <see cref="GameplayEffectExecutionOutput"/>이 담는다(입력/출력을 한 객체에 안 섞는다).
    /// 참조 몇 개만 든 불변 묶음이라 struct — Calculation Modifier가 없으면 Execution마다 만들어도 힙 할당이 없다.
    /// </summary>
    /// <remarks>UE: FGameplayEffectCustomExecutionParameters</remarks>
    public readonly struct GameplayEffectExecutionParameters
    {
        /// <summary>GE를 받는 대상 ASC.</summary>
        public AbilitySystemComponent TargetAsc { get; }

        /// <summary>GE를 발동한 주체 ASC. Context에 Instigator가 없으면 null.</summary>
        public AbilitySystemComponent SourceAsc { get; }

        public GameplayEffectSpec Spec { get; }

        // Calculation Modifier가 걸린 캡처 어트리뷰트별 aggregator 사본. 캡처 aggregator를 복사한 뒤 modifier를 더해, 실제 캐릭터 값은 그대로다.
        // Calculation Modifier가 없으면 null.
        private readonly Dictionary<GameplayEffectAttributeCaptureDefinition, AttributeAggregator> _scopedModifierAggregators;

        /// <param name="scopedModifiers">이 Execution 정의의 Calculation Modifier들.</param>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::FGameplayEffectCustomExecutionParameters</remarks>
        public GameplayEffectExecutionParameters(AbilitySystemComponent target, GameplayEffectSpec spec, IReadOnlyList<GameplayEffectExecutionScopedModifierInfo> scopedModifiers)
        {
            TargetAsc = target;
            SourceAsc = spec.Context.GetInstigator();
            Spec = spec;
            _scopedModifierAggregators = BuildScopedModifierAggregators(spec, scopedModifiers);
        }

        private static Dictionary<GameplayEffectAttributeCaptureDefinition, AttributeAggregator> BuildScopedModifierAggregators(GameplayEffectSpec spec, IReadOnlyList<GameplayEffectExecutionScopedModifierInfo> scopedModifiers)
        {
            if (scopedModifiers == null || scopedModifiers.Count == 0)
            {
                return null;
            }

            var scopedModifierAggregators = new Dictionary<GameplayEffectAttributeCaptureDefinition, AttributeAggregator>();
            GameplayEffectAttributeCaptureSpecContainer capturedRelevantAttributes = spec.CapturedRelevantAttributes;
            foreach (GameplayEffectExecutionScopedModifierInfo scopedModifier in scopedModifiers)
            {
                GameplayEffectAttributeCaptureDefinition capturedAttribute = scopedModifier.CapturedAttribute;
                if (!scopedModifierAggregators.TryGetValue(capturedAttribute, out AttributeAggregator scopedAggregator))
                {
                    GameplayEffectAttributeCaptureSpec captureSpec = capturedRelevantAttributes.FindCaptureSpecByDefinition(capturedAttribute, true);
                    var snapshotAggregator = new AttributeAggregator();
                    if (captureSpec.AttemptGetAttributeAggregatorSnapshot(snapshotAggregator))
                    {
                        scopedAggregator = snapshotAggregator;
                        scopedModifierAggregators.Add(capturedAttribute, scopedAggregator);
                    }
                }

                GameplayEffectModifierMagnitude modifierMagnitude = scopedModifier.ModifierMagnitude;
                if (scopedAggregator != null && modifierMagnitude.AttemptCalculateMagnitude(spec, out float modifierValue))
                {
                    scopedAggregator.AddAggregatorMod(default, scopedModifier.ModifierOp, modifierValue, scopedModifier.SourceTags, scopedModifier.TargetTags);
                }
                else
                {
                    Debug.LogWarning($"[GEExecution] '{spec.Definition.name}'의 Calculation Modifier({capturedAttribute})를 계산하지 못해 건너뜀 — 대상이 캡처되지 않았거나 magnitude 계산에 필요한 값이 없다.");
                }
            }

            return scopedModifierAggregators;
        }

        /// <summary>
        /// 캡처된 어트리뷰트의 CurrentValue를 조회한다. <paramref name="captureDefinition"/>은 이 Execution이 <see cref="GameplayEffectExecution.Defs"/>로 선언한 것과 값이 같아야 매치. 미캡처·무효면 false.
        /// 캡처한 어트리뷰트의 mod 자격은 <paramref name="evaluationParameters"/>의 태그로 판정한다 — 보통 <see cref="Spec"/>의 CapturedSourceTags·CapturedTargetTags 합친 태그를 넘긴다.
        /// </summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeMagnitude</remarks>
        public bool AttemptCalculateCapturedAttributeMagnitude(GameplayEffectAttributeCaptureDefinition captureDefinition, AggregatorEvaluateParameters evaluationParameters, out float magnitude)
        {
            if (_scopedModifierAggregators != null && _scopedModifierAggregators.TryGetValue(captureDefinition, out AttributeAggregator scopedAggregator))
            {
                magnitude = scopedAggregator.Evaluate(evaluationParameters);
                return true;
            }

            GameplayEffectAttributeCaptureSpecContainer capturedRelevantAttributes = Spec.CapturedRelevantAttributes;
            return capturedRelevantAttributes.TryGetCapturedValue(captureDefinition, AttributeCaptureValueType.CurrentValue, evaluationParameters, out magnitude);
        }

        /// <summary>캡처된 어트리뷰트의 BaseValue를 조회한다. mod와 무관해 태그 문맥을 받지 않는다. 나머지는 <see cref="AttemptCalculateCapturedAttributeMagnitude"/>와 같다.</summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeBaseValue</remarks>
        public bool AttemptCalculateCapturedAttributeBaseValue(GameplayEffectAttributeCaptureDefinition captureDefinition, out float baseValue)
        {
            if (_scopedModifierAggregators != null && _scopedModifierAggregators.TryGetValue(captureDefinition, out AttributeAggregator scopedAggregator))
            {
                baseValue = scopedAggregator.GetBaseValue();
                return true;
            }

            GameplayEffectAttributeCaptureSpecContainer capturedRelevantAttributes = Spec.CapturedRelevantAttributes;
            return capturedRelevantAttributes.TryGetCapturedValue(captureDefinition, AttributeCaptureValueType.BaseValue, default, out baseValue);
        }

        /// <summary>캡처된 어트리뷰트의 CurrentValue를 <paramref name="baseValue"/>를 base로 삼아 계산한다.</summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeMagnitudeWithBase</remarks>
        // TODO 미구현 — 사본이 있으면 사본, 없으면 캡처 Spec에서 base 대신 baseValue로 mod를 적용해 계산한다. AttributeAggregator에 EvaluateWithBase(base, 평가 파라미터)가 먼저 필요하다.
        public bool AttemptCalculateCapturedAttributeMagnitudeWithBase(GameplayEffectAttributeCaptureDefinition captureDefinition, AggregatorEvaluateParameters evaluationParameters, float baseValue, out float magnitude)
        {
            throw new NotImplementedException();
        }

        /// <summary>캡처된 어트리뷰트에서 mod가 더한 몫(CurrentValue − BaseValue)을 계산한다.</summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeBonusMagnitude</remarks>
        // TODO 미구현 — 사본이 있으면 사본, 없으면 캡처 Spec에서 계산한다. AttributeAggregator·캡처 Spec에 bonus 계산(UE FAggregator::EvaluateBonus)이 먼저 필요하다.
        public bool AttemptCalculateCapturedAttributeBonusMagnitude(GameplayEffectAttributeCaptureDefinition captureDefinition, AggregatorEvaluateParameters evaluationParameters, out float bonusMagnitude)
        {
            throw new NotImplementedException();
        }

        /// <summary>캡처된 어트리뷰트의 aggregator를 <paramref name="outSnapshottedAggregator"/>에 복사한다.</summary>
        /// <remarks>UE: FGameplayEffectCustomExecutionParameters::AttemptGetCapturedAttributeAggregatorSnapshot</remarks>
        // TODO 미구현 — 사본이 있으면 사본을, 없으면 캡처 Spec의 AttemptGetAttributeAggregatorSnapshot으로 복사한다.
        public bool AttemptGetCapturedAttributeAggregatorSnapshot(GameplayEffectAttributeCaptureDefinition captureDefinition, AttributeAggregator outSnapshottedAggregator)
        {
            throw new NotImplementedException();
        }

        // TODO 미구현 — UE AttemptGatherAttributeMods(캡처 정의의 mod 목록을 채널별로 돌려줌)·ForEachQualifiedAttributeMod(자격 있는 mod마다 콜백).
        //  둘 다 aggregator mod(UE FAggregatorMod)를 밖으로 넘기는데 AttributeAggregator.Mod가 private이라 시그니처를 둘 수 없다.
        //  넣을 때 Mod를 공개할지(또는 연산·magnitude만 넘길지) 정하고, 우리 aggregator엔 채널이 없으니 채널 인자는 뺀다.

        // TODO Transient 집계기 — 태그 → 기본값 0인 새 aggregator + modifier. AttemptCalculateTransientAggregatorMagnitude(태그, evaluationParameters, out 값)로 읽고,
        //  그 태그에 건 modifier가 없으면 false.
    }
}
