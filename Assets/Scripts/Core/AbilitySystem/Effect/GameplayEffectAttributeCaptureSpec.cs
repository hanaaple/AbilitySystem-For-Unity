using Core.AbilitySystem.Aggregator;
using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 캡처 정의(<see cref="GameplayEffectAttributeCaptureDefinition"/>) 하나에 대해 "실제로 캡처한 결과"를 담는다.
    /// 정의는 "무엇을 캡처할지", 이 Spec은 "그 캡처값"이다.
    ///
    /// <para>snapshot 동작을 정의의 <see cref="GameplayEffectAttributeCaptureDefinition.Snapshot"/>에 따라 나눈다 — 어느 쪽이든 값은 aggregator에서 낸다:
    /// <list type="bullet">
    /// <item>snapshot=true — 캡처 시점의 aggregator를 <see cref="AttributeAggregator.TakeSnapshotOf"/>로 복사해 고정한다. 이후 원본이 변해도 불변.</item>
    /// <item>snapshot=false — 컨테이너가 소유한 aggregator 인스턴스를 참조로 들고 있다가, 조회할 때마다 라이브로 <see cref="AttributeAggregator.Evaluate"/>한다.</item>
    /// </list></para>
    ///
    /// <para>Base/Current 중 어느 값을 쓸지는 정의가 고정하지 않으므로(캡처 정의는 대상만 명세) 조회 시
    /// <see cref="AttributeCaptureValueType"/>로 지정한다.</para>
    /// </summary>
    public readonly struct GameplayEffectAttributeCaptureSpec
    {
        /// <summary>이 Spec이 캡처한 정의(무엇을·어디서).</summary>
        public GameplayEffectAttributeCaptureDefinition BackingDefinition { get; }

        // 캡처한 aggregator(값의 출처). snapshot이면 캡처 시점을 고정한 복사본, non-snapshot이면 컨테이너 소유 인스턴스 참조.
        private readonly AttributeAggregator _capturedAttributeAggregator;

        // ASC·핸들 해석에 성공해 캡처가 유효한지. false면 조회는 항상 실패한다.
        private readonly bool _isValid;

        public bool IsValid => _isValid;

        /// <summary>
        /// 캡처 전 "선언만 된" 상태로 만든다 — 정의만 보관하고 아직 캡처하지 않았다. <see cref="IsValid"/>=false이고
        /// 조회는 항상 false다. 컨테이너의 CaptureAttributes가 아래 캡처 생성자로 만든 spec으로 제자리 교체한다.
        /// </summary>
        public GameplayEffectAttributeCaptureSpec(GameplayEffectAttributeCaptureDefinition definition)
        {
            BackingDefinition = definition;
            _isValid = false;
            _capturedAttributeAggregator = null;
        }

        /// <summary>
        /// <paramref name="capturedAsc"/>에서 <paramref name="definition"/>이 가리키는 어트리뷰트의 aggregator를 캡처한다.
        /// snapshot이면 그 시점의 aggregator를 복사해 고정하고, 아니면 컨테이너 소유 aggregator를 참조로 보관해 조회 시 재평가한다.
        /// </summary>
        public GameplayEffectAttributeCaptureSpec(GameplayEffectAttributeCaptureDefinition definition, AbilitySystemComponent capturedAsc)
        {
            BackingDefinition = definition;

            GameplayAttributeHandle handle = definition.ToResolvedAttribute();

            _isValid = capturedAsc != null && handle.IsValid;

            if (_isValid)
            {
                AttributeAggregator attributeAggregator = capturedAsc.FindOrCreateAttributeAggregator(handle);
                if (definition.Snapshot)
                {
                    // 캡처 시점 상태(base·mod)를 통째 고정해 이후 원본 변화와 분리한다.
                    AttributeAggregator snapshot = new AttributeAggregator();
                    snapshot.TakeSnapshotOf(attributeAggregator);
                    _capturedAttributeAggregator = snapshot;
                }
                else
                {
                    // non-snapshot: 컨테이너 소유 인스턴스를 그대로 참조 — 조회 시 라이브로 Evaluate.
                    _capturedAttributeAggregator = attributeAggregator;
                }
            }
            else
            {
                _capturedAttributeAggregator = null;
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

            value = valueType == AttributeCaptureValueType.BaseValue
                ? _capturedAttributeAggregator.GetBaseValue()
                : _capturedAttributeAggregator.Evaluate();
            return true;
        }

        public bool ShouldRefreshLinkedAggregator(AttributeAggregator changedAggregator)
        {
            return ((BackingDefinition.Snapshot == false) && (changedAggregator == null || _capturedAttributeAggregator == changedAggregator));
        }

        /// <summary>
        /// non-snapshot 캡처면 이 캡처가 참조하는 aggregator에 <paramref name="handle"/>를 의존자로 등록한다.
        /// 소스 값이 바뀌면 그 aggregator가 이 GE의 magnitude 재평가를 촉발한다. snapshot은 고정값이라 등록하지 않는다.
        /// </summary>
        public void RegisterLinkedAggregatorCallback(ActiveGameplayEffectHandle handle)
        {
            if (!BackingDefinition.Snapshot && _capturedAttributeAggregator != null)
            {
                _capturedAttributeAggregator.AddDependent(handle);
            }
        }

        /// <summary>등록했던 의존자를 해제한다.</summary>
        public void UnregisterLinkedAggregatorCallback(ActiveGameplayEffectHandle handle)
        {
            _capturedAttributeAggregator?.RemoveDependent(handle);
        }

        public bool HasValidCapture()
        {
            return _capturedAttributeAggregator != null;
        }
    }
}
