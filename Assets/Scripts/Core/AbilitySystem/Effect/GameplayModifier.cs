using System;
using UnityEngine;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE가 어트리뷰트 하나를 어떻게 바꿀지 정의하는 단위 (UE: <c>FGameplayModifierInfo</c>).
    /// 대상 어트리뷰트 + 연산(<see cref="GameplayModifierOperation"/>) + magnitude 계산 방식(<see cref="MagnitudeCalculationType"/>)으로 구성된다.
    /// magnitude는 UE의 <c>FGameplayEffectModifierMagnitude</c>처럼 ScalableFloat·AttributeBased 등으로 나뉜다.
    /// </summary>
    // TODO(editor): GameplayModifier 전용 [CustomPropertyDrawer]로 분리 검토.
    // 지금은 GameplayEffectAssetDrawer(ReorderableList) 안에서만 수기로 그린다 — 유일 소비처라 아직 불필요.
    // GameplayEffectAsset 밖(다른 SO/컴포넌트의 List<GameplayModifier> 등)에서 편집할 일이 생기면
    // 그때 분리해 어디서 그리든 렌더링을 통일한다. (그런 소비처가 생길지는 미정)
    [Serializable]
    public struct GameplayModifier
    {
        // 대상 어트리뷰트(어느 Set의 어느 필드) — UE FGameplayModifierInfo::Attribute.
        [SerializeField] private GameplayAttribute attribute;

        [SerializeField] private GameplayModifierOperation operation;

        [SerializeField] private MagnitudeCalculationType magnitudeCalculationType;

        // ScalableFloat 전용. TODO Level 기반 커브 테이블로 교체 예정. 현재는 고정 float.
        [SerializeField] private float magnitude;

        // AttributeBased 전용. 캡처 어트리뷰트 기반 magnitude 계산 정의.
        [SerializeField] private AttributeBasedMagnitude attributeBased;

        public GameplayModifierOperation Operation => operation;
        public MagnitudeCalculationType MagnitudeCalculationType => magnitudeCalculationType;

        /// <summary>이 modifier가 바꾸는 대상 어트리뷰트 참조.</summary>
        public GameplayAttribute Attribute => attribute;

        /// <summary>AttributeBased 계산 정의. <see cref="MagnitudeCalculationType"/>가 AttributeBased일 때만 의미가 있다.</summary>
        public AttributeBasedMagnitude AttributeBased => attributeBased;

        /// <summary>
        /// 이 modifier의 최종 magnitude를 계산한다 (UE: FGameplayEffectModifierMagnitude::CalculateMagnitude).
        /// ScalableFloat이면 고정값(추후 Level 기반 커브 조회로 교체 예정), AttributeBased면 spec의 캡처값으로 계산한다
        /// (<see cref="AttributeBasedMagnitude.Evaluate"/>).
        /// <para><paramref name="spec"/>를 받는 이유: AttributeBased는 spec에 캡처된 어트리뷰트 값을 읽어야 하므로,
        /// 캡처(Source는 생성 시·Target은 적용 시)가 끝난 뒤에 호출해야 한다.</para>
        /// </summary>
        public float EvaluateMagnitude(GameplayEffectSpec spec, float level)
        {
            return magnitudeCalculationType == MagnitudeCalculationType.AttributeBased
                ? attributeBased.Evaluate(spec)
                : magnitude;
        }

        /// <summary>대상 어트리뷰트의 런타임 핸들. <see cref="GameplayAttribute.ToAttributeHandle"/>에 위임한다.</summary>
        public AttributeHandle ToAttributeHandle() => attribute.ToAttributeHandle();
    }
}
