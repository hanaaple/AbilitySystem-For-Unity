namespace Core.AbilitySystem.Effect
{
    /// <summary>modifier 하나의 평가된 magnitude 캐시. identity(대상·연산)는 정의 <see cref="GameplayModifier"/>가 들고, <see cref="GameplayEffectSpec.Modifiers"/>가 정의와 같은 인덱스로 평행해 i번 슬롯 magnitude는 정의 i번 modifier와 짝을 이룬다.</summary>
    public struct GameplayModifierSpec
    {
        public float EvaluatedMagnitude;

        /// <summary>이 슬롯 magnitude를 (재)계산해 <see cref="EvaluatedMagnitude"/>에 제자리로 채운다. AttributeBased는 <paramref name="spec"/> 캡처값을 읽으므로 캡처 이후 호출. <paramref name="modifier"/>는 같은 인덱스의 정의(계산식을 품음).</summary>
        public void CalculateMagnitude(GameplayModifier modifier, GameplayEffectSpec spec)
        {
            modifier.AttemptCalculateMagnitude(spec, out EvaluatedMagnitude);
        }
    }
}
