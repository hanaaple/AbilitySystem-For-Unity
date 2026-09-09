using System.Collections.Generic;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 한 GE(Spec)가 캡처할 어트리뷰트들을 source/target별로 모으고 캡처 결과를 보관·조회하는 컨테이너.
    /// 정의를 spec과 따로 두지 않는다 — 각 <see cref="GameplayEffectAttributeCaptureSpec"/>이 자기 정의를 품는다. <see cref="AddCaptureDefinition"/>은 "미캡처" spec을 넣고 <see cref="CaptureAttributes"/>가 제자리에서 캡처된 spec으로 채운다.
    /// source/target을 나누는 건 서로 다른 ASC·시점(정해진 source vs 적용 시점 target)에 채워지기 때문. ASC를 직접 읽지 않고 이 한 곳을 거쳐 캡처 중복 제거·(추후) Scoped Modifier 개입 지점을 만든다.
    /// </summary>
    public sealed class GameplayEffectAttributeCaptureSpecContainer
    {
        private readonly List<GameplayEffectAttributeCaptureSpec> _sourceSpecs = new();
        private readonly List<GameplayEffectAttributeCaptureSpec> _targetSpecs = new();

        /// <summary>
        /// 적용 시점에 spec을 대상별로 복제할 때 쓰는 독립 복사본. 원소가 readonly struct라 새 List로 값 복사하면 독립 —
        /// source 슬롯은 캡처값 보존, target 슬롯(미캡처)은 복사본이 자기 target을 새로 캡처. 공유하면 대상별 target 캡처가 서로를 덮어써서 반드시 복사한다.
        /// </summary>
        public GameplayEffectAttributeCaptureSpecContainer Clone()
        {
            var copy = new GameplayEffectAttributeCaptureSpecContainer();
            copy._sourceSpecs.AddRange(_sourceSpecs);
            copy._targetSpecs.AddRange(_targetSpecs);
            return copy;
        }

        /// <summary>캡처할 대상을 선언한다(아직 캡처 안 함). 값이 같은 정의가 이미 있으면 무시한다.</summary>
        public void AddCaptureDefinition(GameplayEffectAttributeCaptureDefinition definition)
        {
            List<GameplayEffectAttributeCaptureSpec> destination = SpecsFor(definition.CaptureSource);

            foreach (GameplayEffectAttributeCaptureSpec existing in destination)
            {
                if (existing.BackingDefinition.Equals(definition))
                {
                    return;
                }
            }

            destination.Add(new GameplayEffectAttributeCaptureSpec(definition));
        }

        /// <summary>
        /// <paramref name="captureSource"/>로 선언된 spec들을 <paramref name="ascToCapture"/>에서 캡처해 제자리 채운다 — 소스별로 호출한다(Source ASC로 한 번, Target ASC로 한 번).
        /// 배열이 소스별로 라우팅돼 있어 소스 검사는 불필요하다.
        /// </summary>
        public void CaptureAttributes(AbilitySystemComponent ascToCapture, AttributeCaptureSource captureSource)
        {
            List<GameplayEffectAttributeCaptureSpec> specs = SpecsFor(captureSource);

            for (int i = 0; i < specs.Count; i++)
            {
                specs[i] = new GameplayEffectAttributeCaptureSpec(specs[i].BackingDefinition, ascToCapture);
            }
        }

        /// <summary>정의로 캡처 Spec을 찾아 값을 조회한다 — 정의의 <see cref="AttributeCaptureSource"/>로 배열을 고른다. 미선언·미캡처·무효면 false. snapshot 분기는 Spec이 처리.</summary>
        public bool TryGetCapturedValue(GameplayEffectAttributeCaptureDefinition definition, AttributeCaptureValueType valueType, out float value)
        {
            foreach (GameplayEffectAttributeCaptureSpec spec in SpecsFor(definition.CaptureSource))
            {
                if (spec.BackingDefinition.Equals(definition))
                {
                    return spec.TryGetCapturedValue(valueType, out value);
                }
            }

            value = 0f;
            return false;
        }

        private List<GameplayEffectAttributeCaptureSpec> SpecsFor(AttributeCaptureSource captureSource)
        {
            return captureSource == AttributeCaptureSource.Source ? _sourceSpecs : _targetSpecs;
        }

        public GameplayEffectAttributeCaptureSpec FindCaptureSpecByDefinition(GameplayEffectAttributeCaptureDefinition captureDefinition, bool bOnlyIncludeValidCapture)
        {
            GameplayEffectAttributeCaptureSpec matchingSpec = default;
            bool bSourceAttribute = (captureDefinition.CaptureSource == AttributeCaptureSource.Source);

            List<GameplayEffectAttributeCaptureSpec> attributeArray = (bSourceAttribute ? _sourceSpecs : _targetSpecs);

            foreach (GameplayEffectAttributeCaptureSpec spec in attributeArray)
            {
                if (spec.BackingDefinition.Equals(captureDefinition))
                {
                    matchingSpec = spec;
                }
            }

            if (matchingSpec.IsValid && bOnlyIncludeValidCapture && !matchingSpec.HasValidCapture())
            {
                matchingSpec = default;
            }

            return matchingSpec;
        }

        public bool HasValidCapturedAttributes(IReadOnlyList<GameplayEffectAttributeCaptureDefinition> reqCaptureDefs)
        {
            bool bHasValid = true;

            foreach (GameplayEffectAttributeCaptureDefinition curDef in reqCaptureDefs)
            {
                GameplayEffectAttributeCaptureSpec captureSpec = FindCaptureSpecByDefinition(curDef, true);
                if (!captureSpec.IsValid)
                {
                    bHasValid = false;
                    break;
                }
            }

            return bHasValid;
        }

        /// <summary>source·target 캡처 spec 전부에 대해 <paramref name="handle"/>를 non-snapshot aggregator의 의존자로 등록한다. GE 활성화 시 호출한다.</summary>
        public void RegisterLinkedAggregatorCallbacks(ActiveGameplayEffectHandle handle)
        {
            foreach (GameplayEffectAttributeCaptureSpec spec in _sourceSpecs)
            {
                spec.RegisterLinkedAggregatorCallback(handle);
            }

            foreach (GameplayEffectAttributeCaptureSpec spec in _targetSpecs)
            {
                spec.RegisterLinkedAggregatorCallback(handle);
            }
        }

        /// <summary>등록했던 의존자 콜백을 전부 해제한다. GE 제거·재캡처 시 호출한다.</summary>
        public void UnregisterLinkedAggregatorCallbacks(ActiveGameplayEffectHandle handle)
        {
            foreach (GameplayEffectAttributeCaptureSpec spec in _sourceSpecs)
            {
                spec.UnregisterLinkedAggregatorCallback(handle);
            }

            foreach (GameplayEffectAttributeCaptureSpec spec in _targetSpecs)
            {
                spec.UnregisterLinkedAggregatorCallback(handle);
            }
        }
    }
}
