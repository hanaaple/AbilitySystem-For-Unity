using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 평가가 끝난 모디파이어 한 건(대상 어트리뷰트 · 연산 · 최종 수치).
    /// Execution이 계산 결과를 이 형태로 출력하면 ASC가 어트리뷰트에 반영한다.
    /// </summary>
    public readonly struct GameplayModifierEvaluatedData
    {
        public GameplayAttributeHandle Handle { get; }
        public float Magnitude { get; }
        public GameplayModifierOperation Operation { get; }

        public GameplayModifierEvaluatedData(GameplayAttributeHandle handle, float magnitude, GameplayModifierOperation operation = GameplayModifierOperation.AddBase)
        {
            Handle = handle;
            Magnitude = magnitude;
            Operation = operation;
        }
    }
}
