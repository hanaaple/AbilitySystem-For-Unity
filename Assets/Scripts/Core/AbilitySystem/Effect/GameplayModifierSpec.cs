namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// modifier 하나의 평가된 magnitude 캐시. identity(대상 어트리뷰트·연산)는 이 슬롯이 아니라 정의 <see cref="GameplayModifier"/>가 들고,
    /// <see cref="GameplayEffectSpec.Modifiers"/>가 정의와 <b>같은 인덱스로 평행</b>하므로 i번 슬롯의 magnitude는 정의 i번 modifier와 짝을 이룬다.
    /// </summary>
    public struct GameplayModifierSpec
    {
        public float EvaluatedMagnitude;

        /// <summary>
        /// 이 슬롯의 magnitude를 (재)계산해 <see cref="EvaluatedMagnitude"/>에 제자리로 채운다.
        /// AttributeBased는 <paramref name="spec"/>의 캡처값을 읽으므로 캡처가 끝난 뒤 호출해야 한다.
        /// <paramref name="modifier"/>는 같은 인덱스의 정의 — magnitude 계산식을 품는다.
        /// </summary>
        public void CalculateMagnitude(GameplayModifier modifier, GameplayEffectSpec spec)
        {
            modifier.AttemptCalculateMagnitude(spec, out EvaluatedMagnitude);
        }
    }
}
