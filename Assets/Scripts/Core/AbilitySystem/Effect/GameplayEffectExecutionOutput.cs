using System;
using System.Collections.Generic;
using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Execution의 계산 결과를 담는 출력 컨테이너. Execution은 여기에만 쓰고 어트리뷰트를 직접 건드리지 않는다
    /// — 실제 반영·재계산 순서는 ASC가 통제한다(모든 수치 변경은 GE 단일 채널 →decisions D1).
    /// (UE: FGameplayEffectCustomExecutionOutput)
    ///
    /// <see cref="GameplayEffectExecutionParameters"/>와 짝을 이루도록 struct다. 출력 개수가 가변이라
    /// 내부 버퍼는 힙 List다(원소 <see cref="GameplayModifierEvaluatedData"/>가 관리 참조를 포함해 stackalloc 불가).
    /// 값으로 넘겨도 같은 List를 가리키므로 Execution이 추가한 내용은 호출측에 반영된다.
    ///
    /// **반드시 <see cref="Create"/>로 만든다** — struct라 파라미터 없는 생성자를 정의할 수 없어
    /// `default`/`new()`로 만들면 버퍼가 null이다.
    /// </summary>
    public readonly struct GameplayEffectExecutionOutput
    {
        private const int DefaultCapacity = 4;

        private readonly List<GameplayModifierEvaluatedData> _outputModifiers;

        public GameplayEffectExecutionOutput(int capacity)
        {
            _outputModifiers = new List<GameplayModifierEvaluatedData>(capacity);
        }

        /// <summary>기본 용량으로 출력 컨테이너를 만든다. 한 Execution의 출력은 보통 한 자릿수다.</summary>
        public static GameplayEffectExecutionOutput Create() => new(DefaultCapacity);

        public IReadOnlyList<GameplayModifierEvaluatedData> OutputModifiers
            => _outputModifiers ?? (IReadOnlyList<GameplayModifierEvaluatedData>)Array.Empty<GameplayModifierEvaluatedData>();

        /// <summary>Execution 결과로 적용할 모디파이어를 출력 목록에 추가한다. (UE: AddOutputModifier)</summary>
        public void AddOutputModifier(AttributeHandle handle, float magnitude, GameplayModifierOperation operation)
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
