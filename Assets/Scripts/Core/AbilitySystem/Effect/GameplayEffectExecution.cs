using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GE 실행 시 커스텀 계산 로직을 수행하는 추상 ScriptableObject.
    /// 단순 Modifier로 표현하기 어려운 복합 계산(예: Source의 Damage - Target의 Armor)에 사용한다.
    /// Execute() 안에서 parameters.AddOutput()으로 결과를 출력하면 ASC가 BaseValue에 즉시 적용한다.
    /// </summary>
    public abstract class GameplayEffectExecution : ScriptableObject
    {
        public abstract void Execute(GameplayEffectExecutionParameters parameters);
    }
}
