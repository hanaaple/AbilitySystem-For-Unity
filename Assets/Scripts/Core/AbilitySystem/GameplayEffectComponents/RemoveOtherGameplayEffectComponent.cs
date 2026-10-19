using System;
using System.Collections.Generic;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Core.AbilitySystem.GameplayEffectComponents
{
    /// <summary>이 GE가 적용될 때마다 질의에 맞는 다른 활성 GE를 제거한다.</summary>
    /// <remarks>UE: URemoveOtherGameplayEffectComponent</remarks>
    [Serializable]
    public sealed class RemoveOtherGameplayEffectComponent : GameplayEffectComponent
    {
        [Tooltip("적용될 때 이 질의 중 하나라도 맞는 활성 GE를 제거한다. 이 GE 자신은 제거하지 않는다.")]
        [SerializeField] private List<GameplayEffectQuery> _removeGameplayEffectQueries = new();

        public IReadOnlyList<GameplayEffectQuery> RemoveGameplayEffectQueries => _removeGameplayEffectQueries;

        /// <remarks>UE: URemoveOtherGameplayEffectComponent::OnGameplayEffectApplied</remarks>
        public override void OnGameplayEffectApplied(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
            var findOwnerQuery = new GameplayEffectQuery { EffectDefinition = GetOwner() };

            // 이미 걸린 같은 GE(방금 적용된 것 포함)는 지우지 않는다
            List<ActiveGameplayEffectHandle> ownerHandles = activeGameplayEffectsContainer.GetActiveEffects(findOwnerQuery);

            foreach (GameplayEffectQuery removeQuery in _removeGameplayEffectQueries)
            {
                if (removeQuery.IsEmpty())
                {
                    continue;
                }

                // UE는 이 목록을 첫 질의에 MoveTemp로 넘겨 둘째 질의부터 자기 제외가 빠진다 — 주석의 의도대로 모든 질의에 건다
                var mutableRemoveQuery = new GameplayEffectQuery(removeQuery);
                mutableRemoveQuery.IgnoreHandles.AddRange(ownerHandles);

                activeGameplayEffectsContainer.RemoveActiveEffects(mutableRemoveQuery);
            }
        }
    }
}
