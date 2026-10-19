using System;
using System.Collections.Generic;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>GE에 붙는 Execution 하나 — 실행할 계산 클래스와, 그 실행 동안 캡처 어트리뷰트에 걸 Calculation Modifier들.</summary>
    /// <remarks>UE: FGameplayEffectExecutionDefinition</remarks>
    [Serializable]
    public class GameplayEffectExecutionDefinition
    {
        // Execution은 SO가 아니라 클래스라 타입 이름(AQN)만 저장하고, 실제 인스턴스화는 GameplayEffectSpec 생성 시 1회 한다.
        [SerializeField] [SubclassSelector(typeof(GameplayEffectExecution))]
        private string calculationClass;

        [SerializeField] private List<GameplayEffectExecutionScopedModifierInfo> calculationModifiers = new();

        /// <summary>계산 클래스(<see cref="GameplayEffectExecution"/> 서브클래스)의 AQN. 비어 있으면 실행하지 않는다.</summary>
        public string CalculationClass => calculationClass;

        public IReadOnlyList<GameplayEffectExecutionScopedModifierInfo> CalculationModifiers => calculationModifiers;
    }
}
