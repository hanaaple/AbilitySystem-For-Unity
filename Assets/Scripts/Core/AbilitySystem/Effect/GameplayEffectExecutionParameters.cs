using System.Collections.Generic;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    public sealed class GameplayEffectExecutionParameters
    {
        private readonly List<GameplayEffectExecutionOutput> _outputs = new();

        /// <summary>GE를 받는 대상 ASC.</summary>
        public AbilitySystemComponent TargetAsc { get; }

        /// <summary>GE를 발동한 주체 ASC. Context에 Instigator가 없으면 null.</summary>
        public AbilitySystemComponent SourceAsc { get; }

        public GameplayEffectSpec Spec { get; }

        public IReadOnlyList<GameplayEffectExecutionOutput> Outputs => _outputs;

        public GameplayEffectExecutionParameters(AbilitySystemComponent target, GameplayEffectSpec spec)
        {
            TargetAsc = target;
            SourceAsc = spec.Context.Instigator;
            Spec = spec;
        }

        /// <summary>Execution 결과로 적용할 모디파이어를 출력 목록에 추가한다.</summary>
        public void AddOutput(AttributeHandle handle, float magnitude, GameplayModifierOperation operation = GameplayModifierOperation.AddBase)
        {
            _outputs.Add(new GameplayEffectExecutionOutput(handle, magnitude, operation));
        }
    }
}
