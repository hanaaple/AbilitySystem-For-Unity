using System.Collections.Generic;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 한 GE(Spec)가 캡처할 어트리뷰트들을 source/target별로 모으고, 캡처 결과를 보관·조회하는 컨테이너.
    ///
    /// <para><b>정의를 spec과 따로 두지 않는다</b> — 각 <see cref="GameplayEffectAttributeCaptureSpec"/>이
    /// 자기 정의(<see cref="GameplayEffectAttributeCaptureSpec.BackingDefinition"/>)를 품는다.
    /// <see cref="AddCaptureDefinition"/>은 "선언만 된(미캡처)" spec을 배열에 넣고,
    /// <see cref="CaptureAttributes"/>가 그 자리에서 캡처된 spec으로 채운다.</para>
    ///
    /// <para>캡처 소스별로 배열을 나눈다 — Source 캡처와 Target 캡처는 서로 다른 ASC에서·다른 시점
    /// (이미 정해진 source vs 적용 시점의 target)에 채워지므로 섞지 않는다.</para>
    ///
    /// <para>흐름: ① <see cref="AddCaptureDefinition"/>로 대상 선언 → ② 적용 시점에 <see cref="CaptureAttributes"/>를
    /// 소스별로 호출 → ③ 계산 중 <see cref="TryGetCapturedValue"/>로 조회. ASC를 직접 읽는 대신 이 한 곳을 거치게 해
    /// 캡처 대상 중복 제거와 (추후) Scoped Modifier 보정의 개입 지점을 만든다.</para>
    /// </summary>
    public sealed class GameplayEffectAttributeCaptureSpecContainer
    {
        private readonly List<GameplayEffectAttributeCaptureSpec> _sourceSpecs = new();
        private readonly List<GameplayEffectAttributeCaptureSpec> _targetSpecs = new();

        /// <summary>
        /// 이 컨테이너의 독립 복사본을 만든다 — 적용 시점에 spec을 대상별로 복제할 때 사용한다.
        /// 원소 <see cref="GameplayEffectAttributeCaptureSpec"/>가 readonly struct라 새 List로 값 복사하면 서로 독립이다:
        /// source 슬롯은 이미 캡처된 값을 그대로 보존하고, target 슬롯(미캡처)도 복사돼 복사본이 자기 target을 새로 캡처한다.
        /// 공유하면 대상별 target 캡처가 서로의 컨테이너를 덮어쓰므로 반드시 복사해야 한다.
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
        /// <paramref name="captureSource"/>로 선언된 spec들을 <paramref name="ascToCapture"/>에서 캡처해 제자리 채운다.
        /// 캡처 소스별로 호출한다 — Source용 ASC로 한 번, Target용 ASC로 한 번.
        /// 배열이 소스별로 이미 라우팅돼 있어 소스 검사는 불필요하며, 재호출 시 그 소스의 spec들을 다시 캡처한다.
        /// </summary>
        public void CaptureAttributes(AbilitySystemComponent ascToCapture, AttributeCaptureSource captureSource)
        {
            List<GameplayEffectAttributeCaptureSpec> specs = SpecsFor(captureSource);

            for (int i = 0; i < specs.Count; i++)
            {
                specs[i] = new GameplayEffectAttributeCaptureSpec(specs[i].BackingDefinition, ascToCapture);
            }
        }

        /// <summary>
        /// 정의로 캡처 Spec을 찾아 캡처값을 조회한다. 정의의 <see cref="AttributeCaptureSource"/>로
        /// 조회할 배열을 고른다. 선언되지 않았거나 아직 캡처 전·캡처가 무효면 false.
        /// snapshot 여부에 따른 고정값/라이브값 분기는 Spec이 처리한다.
        /// </summary>
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
