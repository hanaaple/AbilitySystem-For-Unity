using System;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE 실행 시 커스텀 계산 로직을 수행하는 추상 클래스 — 단순 Modifier로 표현하기 어려운 복합 계산(예: Source Damage - Target Armor)용.
    /// Execute() 안에서 output.AddOutputModifier()로 결과를 내면 ASC가 BaseValue에 즉시 적용한다. 입력(parameters)과 출력(output)은 분리해 받는다.
    /// </summary>
    public abstract class GameplayEffectExecution
    {
        /// <summary>이 Execution이 캡처할 어트리뷰트 정의들 — <see cref="GameplayEffectSpec"/>이 읽어 캡처 컨테이너에 등록. 하위는 보통 static 배열을 돌려주고, 캡처가 없으면 override 안 함(기본 빈 목록).</summary>
        public virtual ReadOnlySpan<GameplayEffectAttributeCaptureDefinition> Defs()
        {
            return default;
        }

        public abstract void Execute(GameplayEffectExecutionParameters parameters, GameplayEffectExecutionOutput output);
    }
}
