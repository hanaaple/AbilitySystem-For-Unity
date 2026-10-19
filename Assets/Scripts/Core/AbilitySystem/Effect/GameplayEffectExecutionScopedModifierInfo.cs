using System;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution 하나가 실행되는 동안에만 캡처 어트리뷰트에 거는 modifier(Calculation Modifier). 대상은 그 Execution이 선언한 캡처 정의(<see cref="GameplayEffectExecution.Defs"/>) 중 하나다.
    /// </summary>
    /// <remarks>UE: FGameplayEffectExecutionScopedModifierInfo</remarks>
    // TODO 런타임 적용은 아직 없다 — Execution 실행 때 캡처 aggregator 사본에 이 modifier를 더해 평가하는 경로가 필요하다.
    // TODO Transient 대상 — aggregatorType(캡처 어트리뷰트 / Transient)과 transientAggregatorIdentifier(태그) 필드를 더해,
    //  캡처 어트리뷰트 대신 Execution이 선언한 임시 변수 태그에도 걸 수 있게 한다.
    [Serializable]
    public class GameplayEffectExecutionScopedModifierInfo
    {
        [SerializeField] private GameplayEffectAttributeCaptureDefinition capturedAttribute;
        [SerializeField] private GameplayModifierOperation modifierOp;
        [SerializeField] private GameplayEffectModifierMagnitude modifierMagnitude;
        [Tooltip("이 modifier가 적용되려면 Source가 갖춰야 할 태그 조건.")]
        [SerializeField] private GameplayTagRequirements sourceTags = new();

        [Tooltip("이 modifier가 적용되려면 Target이 갖춰야 할 태그 조건.")]
        [SerializeField] private GameplayTagRequirements targetTags = new();

        /// <summary>이 modifier를 거는 캡처 어트리뷰트.</summary>
        public GameplayEffectAttributeCaptureDefinition CapturedAttribute => capturedAttribute;

        public GameplayModifierOperation ModifierOp => modifierOp;

        public GameplayEffectModifierMagnitude ModifierMagnitude => modifierMagnitude;

        /// <summary>이 modifier가 적용되려면 Source가 갖춰야 할 태그 조건.</summary>
        public GameplayTagRequirements SourceTags => sourceTags;

        /// <summary>이 modifier가 적용되려면 Target이 갖춰야 할 태그 조건.</summary>
        public GameplayTagRequirements TargetTags => targetTags;
    }
}
