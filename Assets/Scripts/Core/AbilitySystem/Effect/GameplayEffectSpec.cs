using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GameplayEffect(에셋)의 런타임 인스턴스.
    /// 생성 시점에 각 Modifier를 GameplayModifierSpec으로 변환해 캐싱한다.
    /// </summary>
    public sealed class GameplayEffectSpec
    {
        public GameplayEffect Definition { get; }
        public GameplayEffectContextHandle Context { get; }
        public float Level { get; }

        public IReadOnlyList<GameplayModifierSpec> Modifiers { get; }

        public GameplayEffectSpec(GameplayEffect definition, GameplayEffectContextHandle context = default, float level = 1f)
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
        }
    }
}
