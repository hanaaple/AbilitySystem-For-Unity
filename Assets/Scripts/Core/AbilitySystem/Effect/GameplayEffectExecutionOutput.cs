using System;
using System.Collections.Generic;
using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution의 계산 결과를 담는 출력 컨테이너. Execution은 여기에만 쓰고 어트리뷰트를 직접 건드리지 않는다.
    /// 값으로 넘겨도 같은 List를 가리키므로 Execution이 추가한 내용은 호출측에 반영된다.
    /// <b>반드시 <see cref="Create"/>로 만든다</b> — struct라 default/new()로 만들면 내부 버퍼가 null이다.
    /// </summary>
    public readonly struct GameplayEffectExecutionOutput
    {
        private const int DefaultCapacity = 4;

        private readonly List<GameplayModifierEvaluatedData> _outputModifiers;

        public GameplayEffectExecutionOutput(int capacity)
        {
            _outputModifiers = new List<GameplayModifierEvaluatedData>(capacity);
        }

        public static GameplayEffectExecutionOutput Create()
        {
            return new GameplayEffectExecutionOutput(DefaultCapacity);
        }

        public IReadOnlyList<GameplayModifierEvaluatedData> OutputModifiers => _outputModifiers ?? (IReadOnlyList<GameplayModifierEvaluatedData>)Array.Empty<GameplayModifierEvaluatedData>();

        public void AddOutputModifier(GameplayAttributeHandle handle, float magnitude, GameplayModifierOperation operation)
        {
            AddOutputModifier(new GameplayModifierEvaluatedData(handle, magnitude, operation));
        }

        public void AddOutputModifier(GameplayModifierEvaluatedData evaluatedData)
        {
            if (_outputModifiers == null)
            {
                Debug.LogWarning("[GEExecutionOutput] Create() 없이 만든 인스턴스라 출력이 무시됩니다.");
                return;
            }

            _outputModifiers.Add(evaluatedData);
        }
    }
}
