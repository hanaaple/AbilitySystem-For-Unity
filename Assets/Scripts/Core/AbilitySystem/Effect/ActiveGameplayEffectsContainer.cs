using System.Collections.Generic;
using Core.AbilitySystem.Aggregator;
using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 활성 GameplayEffect의 소유·수명·실행·집계를 한 경계에 모은다. ASC는 이들 공개 API의 얇은 위임 래퍼만 남긴다.
    ///
    /// <para>어트리뷰트별 <see cref="AttributeAggregator"/>를 소유하고, CurrentValue는 오직 <see cref="AttributeAggregator.Evaluate"/>로만 계산된다.
    /// persistent(period 0) GE는 대상 aggregator에 mod를 등록/해제하고, Instant/Periodic은 BaseValue를 영구 변경한다.</para>
    ///
    /// <para><c>AttributeData</c>의 Base/Current는 aggregator 결과를 담는 미러이며(Inspector·핸들 조회용), 저장은 Owner의 <see cref="AttributeSet"/> 책임이다.</para>
    ///
    /// <para>재계산은 각 aggregator의 <see cref="AttributeAggregator.OnDirty"/>를 구독해 몰린다 — base/mod가 바뀐 지점이 알리면 그 어트리뷰트만 재평가해 미러에 반영한다.</para>
    /// </summary>
    public class ActiveGameplayEffectsContainer
    {
        private readonly AbilitySystemComponent _owner;
        private readonly Dictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> _activeGameplayEffects = new();

        // 지연 생성 — 처음 mod를 받거나 base가 바뀔 때 AttributeData.BaseValue로 시드해 만든다.
        private readonly Dictionary<GameplayAttributeHandle, AttributeAggregator> _attributeAggregatorMap = new();

        private readonly Dictionary<GameplayAttributeHandle, OnGameplayAttributeValueChange> _attributeValueChangeDelegates = new();

        private delegate void OnGameplayAttributeValueChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue);

        // Tick에서 만료 핸들을 모으는 스크래치(순회 중 제거 방지).
        private readonly List<ActiveGameplayEffectHandle> _expiredEffects = new();

        // 활성 이펙트 핸들 발급기. ASC 간 전역 유일성을 위해 static.
        private static int _handleIdCounter;

        public ActiveGameplayEffectsContainer(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        /// <summary>읽기 전용 뷰 — 에디터 관측 도구가 활성 목록을 훑는 통로.</summary>
        public IReadOnlyDictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> ActiveGameplayEffects => _activeGameplayEffects;

        // ── Apply / Remove ────────────────────────────────────────────────────────

        /// <summary>
        /// Spec을 Owner에게 적용한다. Instant는 즉시 실행 후 Invalid Handle 반환, Duration/Infinite는 핸들 반환.
        /// (UE: FActiveGameplayEffectsContainer::ApplyGameplayEffectSpec)
        ///
        /// 적용 경계에서 spec을 한 번 복제하고 그 복사본에 Target(Owner)을 캡처한다 — 그래야 같은 spec을 여러 대상에
        /// 적용해도 캡처값이 서로 덮어쓰이지 않고, 하나의 복사본이 캡처→실행→저장을 관통한다.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpec(GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            // Modifier 없이 Execution만 가진 GE도 유효하다(데미지 계산을 전부 Execution에 두는 경우).
            if (spec.Modifiers.Length == 0 && spec.Executions.Count == 0)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            GameplayEffectSpec appliedSpec = spec.Clone();
            appliedSpec.CaptureAttributeDataFromTarget(_owner);

            GameplayEffectAsset def = appliedSpec.Definition;

            if (def.Type == GameplayEffectType.Instant)
            {
                ExecuteGameplayEffect(appliedSpec);
                return ActiveGameplayEffectHandle.Invalid;
            }

            var handle = new ActiveGameplayEffectHandle(++_handleIdCounter, _owner);
            var active = new ActiveGameplayEffect(handle, appliedSpec, _owner);
            _activeGameplayEffects.Add(active.Handle, active);

            if (def.Period > 0f && def.ExecutePeriodicEffectOnApplication)
            {
                ExecuteGameplayEffect(appliedSpec);
            }

            // period == 0인 경우만 persistent modifier로 aggregator에 등록한다(등록이 OnDirty를 울려 CurrentValue가 재계산됨).
            if (def.Period <= 0f)
            {
                AddSpecMods(handle, appliedSpec);

                appliedSpec.CapturedRelevantAttributes.RegisterLinkedAggregatorCallbacks(handle);
            }

            return handle;
        }

        /// <summary>핸들의 활성 이펙트를 제거하고, persistent(period 0)였으면 aggregator에서 mod를 해제 후 영향 어트리뷰트를 재계산한다.</summary>
        public bool RemoveActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            if (!handle.IsValid || !_activeGameplayEffects.Remove(handle, out ActiveGameplayEffect active))
            {
                return false;
            }

            if (active.Spec.Definition.Period == 0f)
            {
                RemoveSpecMods(handle, active.Spec);

                // 등록했던 의존자 콜백을 해제한다 — 그러지 않으면 소스 aggregator가 죽은 핸들을 계속 통지하려 한다(→D12).
                active.Spec.CapturedRelevantAttributes.UnregisterLinkedAggregatorCallbacks(handle);
            }

            return true;
        }

        // ── Tick (수명) ───────────────────────────────────────────────────────────

        /// <summary>
        /// 활성 이펙트의 주기 실행과 Duration 만료를 처리한다. ASC.Update가 매 프레임 위임한다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_activeGameplayEffects.Count == 0)
            {
                return;
            }

            _expiredEffects.Clear();

            foreach (ActiveGameplayEffect active in _activeGameplayEffects.Values)
            {
                GameplayEffectAsset def = active.Spec.Definition;

                if (def.Period > 0f)
                {
                    active.PeriodTimer += deltaTime;
                    while (active.PeriodTimer >= def.Period)
                    {
                        active.PeriodTimer -= def.Period;
                        ExecuteGameplayEffect(active.Spec);
                    }
                }

                if (def.Type == GameplayEffectType.Duration)
                {
                    active.RemainingDuration -= deltaTime;
                    if (active.RemainingDuration <= 0f)
                    {
                        _expiredEffects.Add(active.Handle);
                    }
                }
            }

            foreach (ActiveGameplayEffectHandle expired in _expiredEffects)
            {
                RemoveActiveGameplayEffect(expired);
            }
        }

        // ── GE 실행 (Instant / Periodic) ─────────────────────────────────────────

        /// <summary>
        /// Instant / Periodic GE의 Execute 경로 — Modifier 적용과 Execution 실행을 수행한다.
        /// Modifier는 배열 순서대로 BaseValue에 순차 적용되며, 각 Modifier가 이전 결과를 읽으므로 같은 어트리뷰트를
        /// 대상으로 할 때 순서가 결과에 영향을 준다(persistent modifier와 달리 BaseValue를 영구 변경).
        /// </summary>
        private void ExecuteGameplayEffect(GameplayEffectSpec spec)
        {
            // 슬롯은 magnitude만 들고 identity(대상·연산)는 정의가 든다 — 같은 인덱스로 짝지어 읽는다.
            for (int i = 0; i < spec.Modifiers.Length; i++)
            {
                ApplyModToAttribute(spec.Definition.Modifiers[i].ToResolvedAttribute(), spec.Definition.Modifiers[i].Operation, spec.Modifiers[i].EvaluatedMagnitude);
            }

            RunExecutions(spec);
        }

        /// <summary>
        /// Execution을 하나씩 실행하고 각 출력을 다음 Execution 전에 반영한다 — 따라서 Execution[1]은 Execution[0]이
        /// 바꾼 어트리뷰트를 본다. params·output이 execution 루프 내부 지역 변수라 execution 간 출력 누적은 불가능하다.
        /// </summary>
        private void RunExecutions(GameplayEffectSpec spec)
        {
            IReadOnlyList<GameplayEffectExecution> executions = spec.Executions;
            if (executions.Count == 0)
            {
                return;
            }

            foreach (GameplayEffectExecution execution in executions)
            {
                // execution 스코프 지역 변수(UE 동일). 호출당 소량 할당은 스코프 명확성을 위해 감수한다.
                GameplayEffectExecutionParameters execParams = new GameplayEffectExecutionParameters(_owner, spec);
                GameplayEffectExecutionOutput execOutput = GameplayEffectExecutionOutput.Create();

                execution.Execute(execParams, execOutput);

                foreach (GameplayModifierEvaluatedData evaluatedData in execOutput.OutputModifiers)
                {
                    ApplyModToAttribute(evaluatedData.Handle, evaluatedData.Operation, evaluatedData.Magnitude);;
                }
            }
        }

        /// <summary>
        /// 평가가 끝난 모디파이어 1건을 대상 BaseValue에 적용한다. Execute 경로의 유일한 쓰기 지점 —
        /// Modifier와 Execution 출력이 같은 경로를 타야 둘의 연산 지원 범위가 어긋나지 않는다.
        /// </summary>
        private void ApplyModToAttribute(GameplayAttributeHandle gameplayAttributeHandle, GameplayModifierOperation modifierOperation, float modifierMagnitude)
        {
            // 존재하지 않는 어트리뷰트(무효 핸들 등)면 미러가 없으므로 건너뛴다.
            if (!gameplayAttributeHandle.IsValid || !_owner.TryGetAttributeData(gameplayAttributeHandle, out _))
            {
                return;
            }

            float baseValue = GetAttributeBaseValue(gameplayAttributeHandle);
            float newBaseValue = AttributeAggregator.ExecModOnBaseValue(baseValue, modifierOperation, modifierMagnitude);

            SetAttributeBaseValue(gameplayAttributeHandle, newBaseValue);
        }

        // ── 재계산 (aggregator OnDirty 구독) ──────────────────────────────────────

        /// <summary>어트리뷰트의 CurrentValue를 재계산한다. <paramref name="gameplayAttributeHandle"/>는 구독 시 캡처해 넘긴다.</summary>
        internal void OnAttributeAggregatorDirty(AttributeAggregator aggregator, GameplayAttributeHandle gameplayAttributeHandle)
        {
            float newValue = aggregator.Evaluate();

            AttributeSet attributeSet = _owner.GetAttributeSet(gameplayAttributeHandle);
            if (attributeSet == null)
            {
                return;
            }

            float oldValue = gameplayAttributeHandle.GetCurrentValue(attributeSet);

            if (Mathf.Approximately(newValue, oldValue))
            {
                return;
            }

            gameplayAttributeHandle.SetCurrentValueRaw(attributeSet, newValue);

            if (_attributeValueChangeDelegates.TryGetValue(gameplayAttributeHandle, out OnGameplayAttributeValueChange attributeChanged))
            {
                attributeChanged?.Invoke(gameplayAttributeHandle, oldValue, newValue);
            }
        }

        /// <summary>
        /// <paramref name="changedAggregator"/>에 non-snapshot으로 의존하는 활성 GE의 magnitude를 라이브로 재평가하고,
        /// 바뀐 mod를 대상 aggregator에 제자리 갱신해 반영한다. 그 갱신이 대상을 dirty시켜 재계산·연쇄된다.
        /// </summary>
        internal void OnMagnitudeDependencyChange(ActiveGameplayEffectHandle handle, AttributeAggregator changedAggregator)
        {
            if (!handle.IsValid || !_activeGameplayEffects.TryGetValue(handle, out ActiveGameplayEffect activeEffect))
            {
                return;
            }

            GameplayEffectSpec spec = activeEffect.Spec;

            HashSet<GameplayAttributeHandle> attributesToUpdate = new HashSet<GameplayAttributeHandle>();

            for (int modIdx = 0; modIdx < spec.Modifiers.Length; modIdx++)
            {
                GameplayModifier modifierDefinition = spec.Definition.Modifiers[modIdx];

                // dependentAggregatorChange로 인해 mod 재계산이 필요한 경우
                if (modifierDefinition.AttemptRecalculateMagnitudeFromDependentAggregatorChange(spec, changedAggregator, out float recalculatedMagnitude))
                {
                    spec.Modifiers[modIdx].EvaluatedMagnitude = recalculatedMagnitude;
                    attributesToUpdate.Add(modifierDefinition.ToResolvedAttribute());
                }
            }

            UpdateAggregatorModMagnitudes(attributesToUpdate, activeEffect);
        }

        private void UpdateAggregatorModMagnitudes(HashSet<GameplayAttributeHandle> attributesToUpdate, ActiveGameplayEffect activeEffect)
        {
            foreach (GameplayAttributeHandle attributeHandle in attributesToUpdate)
            {
                if (!_owner || _owner.HasAttributeSetForAttribute(attributeHandle) == false)
                {
                    continue;
                }

                AttributeAggregator aggregator = FindOrCreateAttributeAggregator(attributeHandle);

                aggregator.UpdateAggregatorMod(activeEffect, attributeHandle);
            }
        }

        // ── aggregator 소유·mod 채널 ───────────────────────────────────────────────

        /// <summary>어트리뷰트의 aggregator를 찾아 반환한다. 없으면 null — 생성하지 않는다.</summary>
        private AttributeAggregator FindAggregator(GameplayAttributeHandle handle)
        {
            return _attributeAggregatorMap.GetValueOrDefault(handle);
        }

        /// <summary>어트리뷰트의 aggregator를 찾거나, 없으면 현재 <c>AttributeData.BaseValue</c>로 시드해 지연 생성한다.</summary>
        internal AttributeAggregator FindOrCreateAttributeAggregator(GameplayAttributeHandle handle)
        {
            AttributeAggregator aggregator = FindAggregator(handle);
            if (aggregator == null)
            {
                float seedBase = handle.GetBaseValue(_owner.GetAttributeSet(handle));

                aggregator = new AttributeAggregator(seedBase);

                aggregator.OnDirty += dirtied => _owner.OnAttributeAggregatorDirty(dirtied, handle);

                _attributeAggregatorMap.Add(handle, aggregator);
            }

            return aggregator;
        }

        /// <summary>persistent spec의 각 modifier를 대상 어트리뷰트 aggregator 채널에 등록한다(identity는 정의에서 인덱스로 읽는다).</summary>
        private void AddSpecMods(ActiveGameplayEffectHandle source, GameplayEffectSpec spec)
        {
            IReadOnlyList<GameplayModifier> defs = spec.Definition.Modifiers;
            IReadOnlyList<GameplayModifierSpec> mods = spec.Modifiers;
            for (int i = 0; i < mods.Count; i++)
            {
                GameplayAttributeHandle handle = defs[i].ToResolvedAttribute();
                if (!handle.IsValid)
                {
                    continue;
                }

                AttributeAggregator aggregator = FindOrCreateAttributeAggregator(handle);
                aggregator.AddAggregatorMod(source, defs[i].Operation, mods[i].EvaluatedMagnitude);
            }
        }

        /// <summary>해당 활성 이펙트가 등록했던 mod를 각 대상 aggregator에서 해제한다(remove/만료 시).</summary>
        private void RemoveSpecMods(ActiveGameplayEffectHandle source, GameplayEffectSpec spec)
        {
            foreach (GameplayModifier def in spec.Definition.Modifiers)
            {
                GameplayAttributeHandle handle = def.ToResolvedAttribute();
                if (handle.IsValid && _attributeAggregatorMap.TryGetValue(handle, out AttributeAggregator aggregator))
                {
                    aggregator.RemoveAggregatorMod(source);
                }
            }
        }

        // ── 어트리뷰트 값 접근 ─────────────────────────────────────────────────────

        /// <summary>어트리뷰트의 BaseValue를 반환 — aggregator가 있으면 그 진실을, 없으면 미러(AttributeData)를 읽는다.</summary>
        public float GetAttributeBaseValue(GameplayAttributeHandle handle)
        {
            if (_attributeAggregatorMap.TryGetValue(handle, out AttributeAggregator aggregator))
            {
                return aggregator.GetBaseValue();
            }

            return handle.GetBaseValue(_owner.GetAttributeSet(handle));
        }

        /// <summary>
        /// 어트리뷰트의 CurrentValue를 반환 — aggregator가 있으면 <see cref="AttributeAggregator.Evaluate"/>로만 계산해 낸다(외부는 Evaluate 외 경로 없음).
        /// aggregator가 없으면 mod가 없어 current == base이므로 미러를 읽는다.
        /// </summary>
        public float GetAttributeCurrentValue(GameplayAttributeHandle handle)
        {
            if (_attributeAggregatorMap.TryGetValue(handle, out AttributeAggregator aggregator))
            {
                return aggregator.Evaluate();
            }

            return handle.GetCurrentValue(_owner.GetAttributeSet(handle));
        }

        public void SetAttributeBaseValue(GameplayAttributeHandle attributeHandle, float newBaseValue)
        {
            AttributeSet attributeSet = _owner.GetAttributeSet(attributeHandle);

            if (attributeSet == null)
            {
                return;
            }

            float oldBaseValue = 0f;

            attributeSet.PreAttributeBaseChange(attributeHandle, newBaseValue);

            oldBaseValue = attributeHandle.GetBaseValue(attributeSet);
            attributeHandle.SetBaseValueRaw(attributeSet, newBaseValue);

            AttributeAggregator aggregator = FindAggregator(attributeHandle);
            if (aggregator != null)
            {
                oldBaseValue = aggregator.GetBaseValue();
                aggregator.SetBaseValue(newBaseValue);
            }

            attributeSet.PostAttributeBaseChange(attributeHandle, oldBaseValue, newBaseValue);
        }
    }
}
