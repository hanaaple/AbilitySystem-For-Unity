using System;
using UnityEngine;
using Core.AbilitySystem.Attribute;
using UnityEngine.Serialization;

namespace Core.AbilitySystem.Effect
{
    /// <summary>GE가 어트리뷰트 하나를 어떻게 바꿀지 정의하는 단위 — 대상 어트리뷰트 + 연산(<see cref="GameplayModifierOperation"/>) + magnitude(<see cref="GameplayEffectModifierMagnitude"/>).</summary>
    /// <remarks>UE: FGameplayModifierInfo</remarks>
    // TODO(editor): 전용 PropertyDrawer로 분리 검토 — 지금은 GameplayEffectAssetDrawer 안에서만 그려 유일 소비처면 불필요.
    [Serializable]
    public struct GameplayModifier
    {
        // 대상 어트리뷰트(어느 Set의 어느 필드).
        [SerializeField] [FormerlySerializedAs("attribute")] private GameplayAttribute _attribute;

        [SerializeField] [FormerlySerializedAs("operation")] private GameplayModifierOperation _operation;

        [SerializeField] [FormerlySerializedAs("modifierMagnitude")] private GameplayEffectModifierMagnitude _modifierMagnitude;

        // Instant/Periodic 실행과 aggregator 평가(ActiveGameplayEffectsContainer·AttributeAggregator)가 이 두 조건을 판정한다.
        [Tooltip("이 modifier가 적용되려면 Source(발동 주체)가 갖춰야 할 태그 조건.")]
        [SerializeField] [FormerlySerializedAs("sourceTags")] private GameplayTagRequirements _sourceTags;

        [Tooltip("이 modifier가 적용되려면 Target(받는 대상)이 갖춰야 할 태그 조건.")]
        [SerializeField] [FormerlySerializedAs("targetTags")] private GameplayTagRequirements _targetTags;

        public GameplayModifierOperation Operation => _operation;

        public GameplayAttribute Attribute => _attribute;

        /// <summary>magnitude 계산 방식과 값.</summary>
        public GameplayEffectModifierMagnitude ModifierMagnitude => _modifierMagnitude;

        /// <summary>이 modifier가 적용되려면 Source(발동 주체)가 갖춰야 할 태그 조건.</summary>
        public GameplayTagRequirements SourceTags => _sourceTags;

        /// <summary>이 modifier가 적용되려면 Target(받는 대상)이 갖춰야 할 태그 조건.</summary>
        public GameplayTagRequirements TargetTags => _targetTags;

        public GameplayAttributeHandle ToResolvedAttribute()
        {
            return _attribute.ToAttributeHandle();
        }
    }
}
