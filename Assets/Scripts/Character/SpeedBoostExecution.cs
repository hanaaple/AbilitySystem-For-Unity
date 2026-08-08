using System;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// 검증용 Execution — Target의 speed를 고정값만큼 올린다.
    ///
    /// 제약(구조상):
    /// - Execution 출력은 BaseValue에 반영되므로 이 증가는 **영구**다. "N초 후 원복"은
    ///   Duration GE + persistent modifier 경로(→decisions D4)의 몫이라 Execution으로는 표현할 수 없다.
    /// - Execution은 Instant 또는 Period > 0 인 GE에서만 실행된다(ASC.ExecuteGameplayEffect 경유).
    /// - 증가량은 하드코딩이다. Execution은 타입 이름(AQN)만 직렬화되므로(→D7/D8-A)
    ///   인스턴스 필드가 에디터에 노출되지 않는다.
    /// </summary>
    public sealed class SpeedBoostExecution : GameplayEffectExecution
    {
        private const float SpeedBonus = 10f;

        // A/B는 "무엇을 캡처할지" 정의이자 Execute에서 캡처값을 읽는 조회 key다(같은 정의를 key로 넘겨 params에서 조회).
        // 그래서 익명 배열로 합치지 않고 이름 필드로 둔다. defs = { A, B }가 struct 값을 복사해 담는 만큼 약간의 메모리
        // 중복은 있으나, 이 타입은 순수 data + 값-동등성 key라 복사본도 동일 key로 동작해 문제없다 — 가독성을 위해 이대로 둔다.
        private static readonly GameplayEffectAttributeCaptureDefinition A = new GameplayEffectAttributeCaptureDefinition(CharacterAttributeSet.Health, AttributeCaptureSource.Target, true);
        private static readonly GameplayEffectAttributeCaptureDefinition B = new GameplayEffectAttributeCaptureDefinition(CharacterAttributeSet.Stamina, AttributeCaptureSource.Source, false);
        public static readonly GameplayEffectAttributeCaptureDefinition[] defs = { A, B };

        public override ReadOnlySpan<GameplayEffectAttributeCaptureDefinition> Defs() => defs;

        public override void Execute(GameplayEffectExecutionParameters parameters, GameplayEffectExecutionOutput output)
        {
            float result;
            parameters.AttemptCalculateCapturedAttributeMagnitude(A, out float health);
            parameters.AttemptCalculateCapturedAttributeMagnitude(B, out float stamina);

            result = health + stamina;
            result = result * 0.01f + SpeedBonus;

            Debug.Log($"{health} + {stamina} = {result}");

            output.AddOutputModifier(CharacterAttributeSet.Speed, result, GameplayModifierOperation.AddBase);
        }
    }
}
