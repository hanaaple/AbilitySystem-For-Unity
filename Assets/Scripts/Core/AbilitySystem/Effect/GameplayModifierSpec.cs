using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GameplayModifier(정의) 하나의 런타임 인스턴스 (UE: <c>FModifierSpec</c>).
    /// <see cref="GameplayEffectSpec.Modifiers"/>에 정의(<see cref="GameplayEffectAsset.Modifiers"/>)와
    /// <b>같은 인덱스로 평행하게</b> 놓인다 — i번 슬롯이 정의의 i번 modifier에 대응한다. 그래서 한 어트리뷰트를
    /// 여러 modifier가 건드려도(예: Speed에 +10과 ×1.5) 슬롯이 각자 존재한다(어트리뷰트가 key가 아니라 위치가 key).
    ///
    /// <para>담는 값은 <see cref="EvaluatedMagnitude"/>(계산된 수치)이며, 캡처가 갱신될 때마다
    /// <see cref="CalculateMagnitude"/>로 <b>제자리 갱신</b>된다(UE도 <c>FModifierSpec</c>은 EvaluatedMagnitude만 보유).
    /// UE와의 차이: UE <c>FGameplayModifierInfo.Attribute</c>는 런타임에 포인터로 해석돼 정의에서 바로 읽어도 싸지만,
    /// 우리 <see cref="GameplayAttribute"/>는 문자열 기반이라 <see cref="AttributeHandle"/> 해석에 리플렉션이 든다.
    /// 그래서 해석된 <see cref="Handle"/>과 <see cref="Operation"/>을 슬롯에 <b>캐시</b>해, 적용·재계산 핫패스가
    /// 매번 정의를 문자열 해석하지 않게 한다.</para>
    /// </summary>
    public struct GameplayModifierSpec
    {
        /// <summary>대상 어트리뷰트의 런타임 핸들(빌드 시 1회 해석·캐시). 무효면 <see cref="IsValid"/>=false.</summary>
        public AttributeHandle Handle { get; }

        /// <summary>연산 종류(정의에서 옮겨 캐시).</summary>
        public GameplayModifierOperation Operation { get; }

        /// <summary>계산된 최종 수치. 캡처 갱신마다 <see cref="CalculateMagnitude"/>로 다시 채워진다.</summary>
        public float EvaluatedMagnitude { get; private set; }

        public bool IsValid => Handle.IsValid;

        /// <summary>정의로부터 identity(핸들·연산)만 해석해 슬롯을 만든다. magnitude는 아직 0 — 캡처 후 <see cref="CalculateMagnitude"/>가 채운다.</summary>
        public GameplayModifierSpec(GameplayModifier modifier)
        {
            Handle = modifier.ToAttributeHandle();
            Operation = modifier.Operation;
            EvaluatedMagnitude = 0f;
        }

        /// <summary>
        /// 이 슬롯의 magnitude를 (재)계산해 <see cref="EvaluatedMagnitude"/>에 제자리로 채운다
        /// (UE: <c>FGameplayEffectModifierMagnitude::AttemptCalculateMagnitude</c>가 채우는 값).
        /// AttributeBased는 <paramref name="spec"/>의 캡처값을 읽으므로 캡처가 끝난 뒤 호출해야 한다.
        /// <paramref name="modifier"/>는 이 슬롯이 대응하는 정의(같은 인덱스) — magnitude 계산식을 품고 있다.
        /// </summary>
        public void CalculateMagnitude(GameplayModifier modifier, GameplayEffectSpec spec, float level)
        {
            EvaluatedMagnitude = modifier.EvaluateMagnitude(spec, level);
        }
    }
}
