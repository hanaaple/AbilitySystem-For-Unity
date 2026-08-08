using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 캡처 정의(<see cref="GameplayEffectAttributeCaptureDefinition"/>) 하나에 대해 "실제로 캡처한 결과"를 담는다
    /// (UE: FGameplayEffectAttributeCaptureSpec). 정의는 "무엇을 캡처할지", 이 Spec은 "그 캡처값"이다.
    ///
    /// <para>snapshot 동작을 정의의 <see cref="GameplayEffectAttributeCaptureDefinition.Snapshot"/>에 따라 나눈다:
    /// <list type="bullet">
    /// <item>snapshot=true — 생성(캡처) 시점의 값을 <see cref="_snapshotData"/>에 고정한다. 이후 ASC가 변해도 불변.</item>
    /// <item>snapshot=false — 값을 고정하지 않고 캡처 대상 ASC 참조만 들고 있다가, 조회할 때마다 라이브로 재조회한다.</item>
    /// </list></para>
    ///
    /// <para>Base/Current 중 어느 값을 쓸지는 정의가 고정하지 않으므로(캡처 정의는 대상만 명세) 조회 시
    /// <see cref="AttributeCaptureValueType"/>로 지정한다.</para>
    /// </summary>
    public readonly struct GameplayEffectAttributeCaptureSpec
    {
        /// <summary>이 Spec이 캡처한 정의(무엇을·어디서).</summary>
        public GameplayEffectAttributeCaptureDefinition BackingDefinition { get; }

        // non-snapshot 재조회 대상. snapshot이어도 참조는 보관하나 조회엔 쓰지 않는다.
        private readonly AbilitySystemComponent _capturedAsc;
        private readonly AttributeHandle _handle;

        // ASC·핸들 해석에 성공해 캡처가 유효한지. false면 조회는 항상 실패한다.
        private readonly bool _isValid;

        // snapshot=true일 때 고정된 값(Base·Current 모두). non-snapshot이면 default(미사용).
        private readonly AttributeData _snapshotData;

        public bool IsValid => _isValid;

        /// <summary>
        /// 캡처 전 "선언만 된" 상태로 만든다 — 정의만 보관하고 아직 캡처하지 않았다
        /// (UE: AddCaptureDefinition이 컨테이너에 넣는 미캡처 spec). <see cref="IsValid"/>=false이고
        /// 조회는 항상 false다. 컨테이너의 CaptureAttributes가 아래 캡처 생성자로 만든 spec으로 제자리 교체한다.
        /// </summary>
        public GameplayEffectAttributeCaptureSpec(GameplayEffectAttributeCaptureDefinition definition)
        {
            BackingDefinition = definition;
            _capturedAsc = null;
            _handle = default;
            _isValid = false;
            _snapshotData = default;
        }

        /// <summary>
        /// <paramref name="capturedAsc"/>에서 <paramref name="definition"/>이 가리키는 어트리뷰트를 캡처한다.
        /// 정의가 snapshot이면 이 시점의 값을 고정하고, 아니면 ASC 참조만 보관해 조회 시 재조회한다.
        /// </summary>
        public GameplayEffectAttributeCaptureSpec(GameplayEffectAttributeCaptureDefinition definition, AbilitySystemComponent capturedAsc)
        {
            BackingDefinition = definition;
            _capturedAsc = capturedAsc;
            _handle = definition.ToAttributeHandle();
            _isValid = capturedAsc != null && _handle.IsValid;

            _snapshotData = default;
            if (_isValid && definition.Snapshot)
            {
                _snapshotData = new AttributeData(
                    capturedAsc.GetAttributeBaseValue(_handle),
                    capturedAsc.GetAttributeCurrentValue(_handle));
            }
        }

        /// <summary>
        /// 캡처값을 조회한다. snapshot이면 고정된 값을, 아니면 조회 시점의 라이브 값을 반환한다.
        /// 캡처가 무효(<see cref="IsValid"/>=false)면 false.
        /// </summary>
        public bool TryGetCapturedValue(AttributeCaptureValueType valueType, out float value)
        {
            if (!_isValid)
            {
                value = 0f;
                return false;
            }

            if (BackingDefinition.Snapshot)
            {
                value = valueType == AttributeCaptureValueType.BaseValue ? _snapshotData.BaseValue : _snapshotData.CurrentValue;
                return true;
            }

            value = valueType == AttributeCaptureValueType.BaseValue
                ? _capturedAsc.GetAttributeBaseValue(_handle)
                : _capturedAsc.GetAttributeCurrentValue(_handle);
            return true;
        }
    }
}
