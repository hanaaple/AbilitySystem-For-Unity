using System;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Character
{
    /// <summary>시전자 공격력으로 체력을 깎는다. 공격력의 일정 범위에서 무작위로 정하고, 컨텍스트 피격 정보의 부위가 머리면 배율을 곱한다. 대상에게 실드가 있으면 실드부터 깎고 남은 만큼만 체력을 깎는다.</summary>
    public sealed class DamageExecution : GameplayEffectExecution
    {
        private const float MinDamageRatio = 0.95f;
        private const float MaxDamageRatio = 1.05f;
        private const float HeadDamageMultiplier = 2f;
        private const string HeadZoneTagName = "Gameplay.Zone.Head";

        // 발사 순간의 공격력으로 친다 — 날아가는 동안 버프가 끝나도 쏠 때 값이 맞는다
        private static readonly GameplayEffectAttributeCaptureDefinition[] CaptureDefinitions =
        {
            new(CharacterAttributeSet.Attack, AttributeCaptureSource.Source, true),
            // 맞는 순간 남아 있는 실드로 막는다
            new(CharacterAttributeSet.Shield, AttributeCaptureSource.Target, false),
        };

        private static GameplayEffectAttributeCaptureDefinition AttackDefinition => CaptureDefinitions[0];
        private static GameplayEffectAttributeCaptureDefinition ShieldDefinition => CaptureDefinitions[1];

        public override ReadOnlySpan<GameplayEffectAttributeCaptureDefinition> Defs()
        {
            return CaptureDefinitions;
        }

        public override void Execute(GameplayEffectExecutionParameters parameters, GameplayEffectExecutionOutput output)
        {
            GameplayEffectSpec spec = parameters.Spec;
            TagContainerAggregator capturedSourceTags = spec.CapturedSourceTags;
            TagContainerAggregator capturedTargetTags = spec.CapturedTargetTags;
            var evaluationParameters = new AggregatorEvaluateParameters(capturedSourceTags.GetAggregatedTags(), capturedTargetTags.GetAggregatedTags());

            parameters.AttemptCalculateCapturedAttributeMagnitude(AttackDefinition, evaluationParameters, out float attack);

            float damage = attack * Random.Range(MinDamageRatio, MaxDamageRatio);
            if (IsHeadHit(spec.Context))
            {
                damage *= HeadDamageMultiplier;
            }

            if (damage <= 0f)
            {
                return;
            }

            parameters.AttemptCalculateCapturedAttributeMagnitude(ShieldDefinition, evaluationParameters, out float shield);
            float absorbed = Mathf.Clamp(shield, 0f, damage);
            if (absorbed > 0f)
            {
                output.AddOutputModifier(CharacterAttributeSet.Shield, -absorbed, GameplayModifierOperation.AddBase);
            }

            float healthDamage = damage - absorbed;
            if (healthDamage > 0f)
            {
                output.AddOutputModifier(CharacterAttributeSet.Health, -healthDamage, GameplayModifierOperation.AddBase);
            }
        }

        /// <summary>컨텍스트 피격 정보의 부위가 머리인지. 피해 표시도 같은 기준을 쓴다.</summary>
        public static bool IsHeadHit(GameplayEffectContextHandle context)
        {
            if (!context.TryGetHitResult(out RaycastHit hitResult) || hitResult.collider == null)
            {
                return false;
            }

            var hitZone = hitResult.collider.GetComponent<HitZone>();
            if (hitZone == null)
            {
                return false;
            }

            GameplayTag headTag = GameplayTag.RequestGameplayTag(HeadZoneTagName);
            GameplayTag zoneTag = hitZone.ZoneTag;
            return zoneTag.MatchesTag(headTag);
        }
    }
}
