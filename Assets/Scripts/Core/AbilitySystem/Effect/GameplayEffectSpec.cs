using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GameplayEffectAsset(에셋)의 런타임 인스턴스.
    /// 생성 시점에 각 Modifier를 GameplayModifierSpec으로 변환해 캐싱한다.
    /// </summary>
    public sealed class GameplayEffectSpec
    {
        public GameplayEffectAsset Definition { get; }
        public GameplayEffectContextHandle Context { get; }
        public float Level { get; }

        public IReadOnlyList<GameplayModifierSpec> Modifiers { get; }

        /// <summary>정의에 적힌 AQN을 resolve해 만든 Execution 인스턴스들. 실패한 항목은 경고 후 제외.</summary>
        public IReadOnlyList<GameplayEffectExecution> Executions { get; }

        public GameplayEffectSpec(GameplayEffectAsset definition, GameplayEffectContextHandle context = default, float level = 1f)
        {
            Definition = definition;
            Context = context;
            Level = level;

            var list = new List<GameplayModifierSpec>(definition.Modifiers.Count);
            foreach (GameplayModifier modifier in definition.Modifiers)
            {
                var modSpec = new GameplayModifierSpec(modifier, level);
                if (modSpec.IsValid)
                {
                    list.Add(modSpec);
                }
                else
                {
                    Debug.LogWarning($"[GESpec] AttributeHandle 해석 실패 — '{definition.name}' 의 modifier를 건너뜀.");
                }
            }

            Modifiers = list;
            Executions = ResolveExecutions(definition);
        }

        /// <summary>AQN 목록을 Execution 인스턴스 목록으로 변환한다. resolve 실패는 경고 후 건너뛴다.</summary>
        private static IReadOnlyList<GameplayEffectExecution> ResolveExecutions(GameplayEffectAsset definition)
        {
            IReadOnlyList<string> typeNames = definition.ExecutionTypeNames;
            if (typeNames == null || typeNames.Count == 0)
            {
                return Array.Empty<GameplayEffectExecution>();
            }

            var executions = new List<GameplayEffectExecution>(typeNames.Count);
            foreach (string typeName in typeNames)
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    continue;
                }

                Type type = Type.GetType(typeName);
                if (type == null || !typeof(GameplayEffectExecution).IsAssignableFrom(type) || type.IsAbstract)
                {
                    Debug.LogWarning($"[GESpec] Execution 타입 해석 실패 — '{definition.name}' 의 '{typeName}'을 건너뜀.");
                    continue;
                }

                executions.Add((GameplayEffectExecution)Activator.CreateInstance(type));
            }

            return executions;
        }
    }
}
