using Core.AbilitySystem.Effect;

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

        public override void Execute(GameplayEffectExecutionParameters parameters, GameplayEffectExecutionOutput output)
        {
            output.AddOutputModifier(CharacterAttributeSet.Speed, SpeedBonus, GameplayModifierOperation.AddBase);
        }
    }
}
