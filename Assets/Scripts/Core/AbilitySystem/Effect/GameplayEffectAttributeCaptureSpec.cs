using Core.AbilitySystem.Attribute;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 캡처 정의(<see cref="GameplayEffectAttributeCaptureDefinition"/>) 하나에 대한 "실제 캡처 결과" — 정의는 무엇을 캡처할지, 이 Spec은 그 캡처값이다.
    /// snapshot=true면 캡처 시점의 aggregator를 <see cref="AttributeAggregator.TakeSnapshotOf"/>로 복사해 고정, snapshot=false면 컨테이너 소유 aggregator를 참조로 들고 조회 시 라이브 <see cref="AttributeAggregator.Evaluate"/>.
    /// Base/Current 중 무엇을 쓸지는 정의가 고정하지 않아 조회 시 <see cref="AttributeCaptureValueType"/>로 지정한다.
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

        /// <summary>캡처 전 "선언만 된" 상태 — 정의만 보관, <see cref="IsValid"/>=false, 조회는 항상 false. 컨테이너의 CaptureAttributes가 캡처된 spec으로 제자리 교체한다.</summary>
        public GameplayEffectAttributeCaptureSpec(GameplayEffectAttributeCaptureDefinition definition)
        {
            BackingDefinition = definition;
            _isValid = false;
            _capturedAttributeAggregator = null;
        }

        /// <summary><paramref name="capturedAsc"/>에서 <paramref name="definition"/> 어트리뷰트의 aggregator를 캡처한다 — snapshot이면 그 시점을 복사해 고정, 아니면 컨테이너 aggregator를 참조로 보관해 조회 시 재평가.</summary>
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

        /// <summary>캡처값을 조회한다 — snapshot이면 고정값, 아니면 조회 시점 라이브값. 무효(<see cref="IsValid"/>=false)면 false.</summary>
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

        /// <summary>non-snapshot 캡처면 참조 aggregator에 <paramref name="handle"/>를 의존자로 등록 — 소스 값이 바뀌면 이 GE magnitude 재평가를 촉발. snapshot은 고정값이라 등록 안 함.</summary>
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
