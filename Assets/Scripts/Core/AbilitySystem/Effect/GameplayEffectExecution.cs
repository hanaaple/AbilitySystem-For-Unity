using System;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE 실행 시 커스텀 계산 로직을 수행하는 추상 클래스.
    /// 단순 Modifier로 표현하기 어려운 복합 계산(예: Source의 Damage - Target의 Armor)에 사용한다.
    /// Execute() 안에서 output.AddOutputModifier()로 결과를 출력하면 ASC가 BaseValue에 즉시 적용한다.
    ///
    /// 입력(parameters)과 출력(output)을 분리해 받는다.
    /// </summary>
    public abstract class GameplayEffectExecution
    {
        /// <summary>
        /// 이 Execution이 캡처할 어트리뷰트 정의들. <see cref="GameplayEffectSpec"/>이 읽어 캡처 컨테이너에 등록한다.
        /// 하위는 보통 <c>static</c> 배열을 돌려준다. 캡처가 필요 없으면 override하지 않는다(기본 빈 목록).
        /// </summary>
        public virtual ReadOnlySpan<GameplayEffectAttributeCaptureDefinition> Defs()
        {
            return default;
        }

        public abstract void Execute(GameplayEffectExecutionParameters parameters, GameplayEffectExecutionOutput output);
    }
}
