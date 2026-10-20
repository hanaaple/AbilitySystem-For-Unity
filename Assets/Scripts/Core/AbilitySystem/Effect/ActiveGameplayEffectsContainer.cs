using System.Collections.Generic;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 활성 GameplayEffect의 소유·수명·실행·집계를 한 경계에 모은다(ASC는 얇은 위임 래퍼만 남긴다).
    /// 어트리뷰트별 <see cref="AttributeAggregator"/>를 소유하고 CurrentValue는 <see cref="AttributeAggregator.Evaluate"/>로만 계산된다 — persistent(period 0) GE는 aggregator에 mod를 등록/해제, Instant/Periodic은 BaseValue를 영구 변경.
    /// AttributeData의 Base/Current는 aggregator 결과 미러(Inspector·핸들 조회용)이고 저장은 Owner의 <see cref="AttributeSet"/> 책임. 재계산은 <see cref="AttributeAggregator.OnDirty"/> 구독으로 바뀐 어트리뷰트만 몰아 처리한다.
    /// </summary>
    public class ActiveGameplayEffectsContainer
    {
        private readonly AbilitySystemComponent _owner;
        private readonly Dictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> _activeGameplayEffects = new();

        // 지연 생성 — 처음 mod를 받거나 base가 바뀔 때 AttributeData.BaseValue로 시드해 만든다.
        private readonly Dictionary<GameplayAttributeHandle, AttributeAggregator> _attributeAggregatorMap = new();

        private readonly Dictionary<GameplayAttributeHandle, GameplayAttributeValueChangeEvent> _attributeValueChangeDelegates = new();

        // ApplyModToAttribute가 Base를 바꾸기 직전에 넣고, 그 변경이 dirty를 거쳐 닿는 InternalUpdateNumericalAttribute가 값 변경 이벤트에 실어 보낸 뒤 비운다.
        // Base 변경 API에 원인 인자가 없어 그 사이를 필드로 건넌다(dirty 전파가 동기라 성립).
        private GameplayEffectModCallbackData _currentModCallbackData;

        // Tick에서 만료 핸들을 모으는 스크래치(순회 중 제거 방지).
        private readonly List<ActiveGameplayEffectHandle> _expiredEffects = new();

        // 활성 이펙트 핸들 발급기. ASC 간 전역 유일성을 위해 static.
        private static int _handleIdCounter;

        public ActiveGameplayEffectsContainer(AbilitySystemComponent owner)
        {
            _owner = owner;
        }

        /// <summary>이 컨테이너를 가진 ASC(적용 대상).</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::Owner</remarks>
        public AbilitySystemComponent Owner => _owner;

        /// <summary>읽기 전용 뷰 — 에디터 관측 도구가 활성 목록을 훑는 통로.</summary>
        public IReadOnlyDictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> ActiveGameplayEffects => _activeGameplayEffects;

        /// <summary>핸들의 활성 GE. 없으면(제거됐거나 Instant) null.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveGameplayEffect</remarks>
        public ActiveGameplayEffect GetActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            return _activeGameplayEffects.GetValueOrDefault(handle);
        }

        // ── 질의 (GameplayEffectQuery) ───────────────────────────────────────────

        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffects</remarks>
        public List<ActiveGameplayEffectHandle> GetActiveEffects(GameplayEffectQuery query)
        {
            var handles = new List<ActiveGameplayEffectHandle>();
            foreach (ActiveGameplayEffect effect in _activeGameplayEffects.Values)
            {
                if (query.Matches(effect))
                {
                    handles.Add(effect.Handle);
                }
            }

            return handles;
        }

        /// <summary>질의에 맞는 GE 수. 꺼진 GE는 <paramref name="enforceOnGoingCheck"/>가 true면 세지 않는다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffectCount</remarks>
        public int GetActiveEffectCount(GameplayEffectQuery query, bool enforceOnGoingCheck = true)
        {
            int count = 0;
            foreach (ActiveGameplayEffect effect in _activeGameplayEffects.Values)
            {
                if ((!effect.IsInhibited || !enforceOnGoingCheck) && query.Matches(effect))
                {
                    // TODO GE 스택이 생기면 1 대신 스택 수를 더한다(UE Spec.GetStackCount)
                    count++;
                }
            }

            return count;
        }

        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffectsTimeRemaining</remarks>
        public List<float> GetActiveEffectsTimeRemaining(GameplayEffectQuery query)
        {
            var times = new List<float>();
            foreach (ActiveGameplayEffect effect in _activeGameplayEffects.Values)
            {
                if (query.Matches(effect))
                {
                    times.Add(effect.GetTimeRemaining());
                }
            }

            return times;
        }

        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffectsDuration</remarks>
        public List<float> GetActiveEffectsDuration(GameplayEffectQuery query)
        {
            var durations = new List<float>();
            foreach (ActiveGameplayEffect effect in _activeGameplayEffects.Values)
            {
                if (query.Matches(effect))
                {
                    durations.Add(effect.GetDuration());
                }
            }

            return durations;
        }

        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffectsTimeRemainingAndDuration</remarks>
        public List<(float TimeRemaining, float Duration)> GetActiveEffectsTimeRemainingAndDuration(GameplayEffectQuery query)
        {
            var results = new List<(float TimeRemaining, float Duration)>();
            foreach (ActiveGameplayEffect effect in _activeGameplayEffects.Values)
            {
                if (query.Matches(effect))
                {
                    results.Add((effect.GetTimeRemaining(), effect.GetDuration()));
                }
            }

            return results;
        }

        /// <summary>
        /// 질의에 맞는 GE 중 가장 늦게 끝나는 시각과 그 지속시간을 낸다. 하나라도 끝나는 시간이 없으면 둘 다 <see cref="GameplayEffectConstants.InfiniteDuration"/>.
        /// <paramref name="instigators"/>에는 맞는 GE의 시전자를 겹치지 않게 더한다.
        /// </summary>
        /// <returns>맞는 GE가 하나라도 있었는지.</returns>
        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffectsEndTimeAndDuration</remarks>
        public bool GetActiveEffectsEndTimeAndDuration(GameplayEffectQuery query, out float endTime, out float duration, List<AbilitySystemComponent> instigators)
        {
            endTime = 0f;
            duration = 0f;
            bool foundSomething = false;

            foreach (ActiveGameplayEffect effect in _activeGameplayEffects.Values)
            {
                if (!query.Matches(effect))
                {
                    continue;
                }

                foundSomething = true;

                float thisEndTime = effect.GetEndTime();
                if (thisEndTime <= GameplayEffectConstants.InfiniteDuration)
                {
                    endTime = GameplayEffectConstants.InfiniteDuration;
                    duration = GameplayEffectConstants.InfiniteDuration;
                    return true;
                }

                if (thisEndTime > endTime)
                {
                    endTime = thisEndTime;
                    duration = effect.GetDuration();
                }

                GameplayEffectContextHandle context = effect.Spec.Context;
                AbilitySystemComponent instigator = context.GetInstigator();
                if (!instigators.Contains(instigator))
                {
                    instigators.Add(instigator);
                }
            }

            return foundSomething;
        }

        /// <remarks>UE: FActiveGameplayEffectsContainer::GetActiveEffectsEndTime</remarks>
        public float GetActiveEffectsEndTime(GameplayEffectQuery query, List<AbilitySystemComponent> instigators)
        {
            GetActiveEffectsEndTimeAndDuration(query, out float endTime, out _, instigators);
            return endTime;
        }

        /// <summary>질의에 맞는 GE를 제거한다.</summary>
        /// <param name="stacksToRemove">UE 시그니처 자리. GE 스택이 아직 없어 늘 통째로 제거한다.</param>
        /// <returns>실제로 제거한 GE 수.</returns>
        /// <remarks>UE: FActiveGameplayEffectsContainer::RemoveActiveEffects</remarks>
        public int RemoveActiveEffects(GameplayEffectQuery query, int stacksToRemove = -1)
        {
            // 제거가 태그 변화로 다른 GE 제거를 부를 수 있어 먼저 핸들을 모은다. 이미 빠진 핸들은 RemoveActiveGameplayEffect가 false를 낸다
            List<ActiveGameplayEffectHandle> matchedHandles = GetActiveEffects(query);

            int numRemoved = 0;
            foreach (ActiveGameplayEffectHandle handle in matchedHandles)
            {
                if (RemoveActiveGameplayEffect(handle))
                {
                    numRemoved++;
                }
            }

            return numRemoved;
        }

        /// <summary>활성 GE의 레벨을 바꾸고 magnitude를 다시 계산해 aggregator에 반영한다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::SetActiveGameplayEffectLevel</remarks>
        public void SetActiveGameplayEffectLevel(ActiveGameplayEffectHandle handle, float newLevel)
        {
            ActiveGameplayEffect effect = GetActiveGameplayEffect(handle);
            if (effect == null || Mathf.Approximately(effect.Spec.Level, newLevel))
            {
                return;
            }

            effect.Spec.SetLevel(newLevel);
            effect.Spec.CalculateModifierMagnitudes();
            UpdateAllAggregatorModMagnitudes(effect);
        }

        // ── Apply / Remove ────────────────────────────────────────────────────────

        /// <summary>
        /// Spec을 Owner에게 적용한다. Instant는 즉시 실행 후 Invalid Handle 반환, Duration/Infinite는 핸들 반환.
        /// 적용 경계에서 spec을 한 번 복제해 Target(Owner)을 캡처한다 — 같은 spec을 여러 대상에 적용해도 캡처값이 섞이지 않고, 한 복사본이 캡처→실행→저장을 관통한다.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpec(GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            GameplayEffectAsset def = spec.Definition;

            if (!def.CanApply(this, spec))
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            GameplayEffectSpec appliedSpec = spec.Clone();

            // 대상 태그를 먼저 모아야 아래 magnitude 계산(캡처 평가)이 Target 태그 조건을 판정할 수 있다
            CaptureTargetActorTags(appliedSpec);
            appliedSpec.CaptureAttributeDataFromTarget(_owner);

            if (def.Type == GameplayEffectType.Instant)
            {
                ExecuteActiveEffectsFrom(appliedSpec);
                def.OnApplied(this, appliedSpec);
                return ActiveGameplayEffectHandle.Invalid;
            }

            var handle = new ActiveGameplayEffectHandle(++_handleIdCounter, _owner);
            var active = new ActiveGameplayEffect(handle, appliedSpec, _owner);
            _activeGameplayEffects.Add(active.Handle, active);

            InternalOnActiveGameplayEffectAdded(active);

            def.OnApplied(this, appliedSpec);

            // UE는 이 실행을 다음 틱으로 미뤄 부여 태그·OnApplied가 먼저다. 바로 실행하는 우리도 그 뒤에 둬 같은 상태를 보게 한다.
            if (def.Period > 0f && def.ExecutePeriodicEffectOnApplication)
            {
                InternalExecutePeriodicGameplayEffect(active);
            }

            return handle;
        }

        /// <summary>핸들의 활성 이펙트를 제거하고 Owner에 건 효과(modifier·부여 태그)를 거둔다.</summary>
        public bool RemoveActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            if (!handle.IsValid || !_activeGameplayEffects.Remove(handle, out ActiveGameplayEffect active))
            {
                return false;
            }

            // 꺼진 GE는 끌 때 이미 효과를 거뒀다
            if (!active.IsInhibited)
            {
                RemoveActiveGameplayEffectGrantedTagsAndModifiers(active);
            }

            // UE InternalOnActiveGameplayEffectRemoved처럼 효과를 거둔 뒤 알린다
            active.EventSet.BroadcastEffectRemoved(active);

            return true;
        }

        /// <summary>새 활성 GE를 GE Component에 알리고, 켜져야 하는지에 따라 효과를 건다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::InternalOnActiveGameplayEffectAdded</remarks>
        private void InternalOnActiveGameplayEffectAdded(ActiveGameplayEffect active)
        {
            GameplayEffectAsset def = active.Spec.Definition;
            bool shouldBeActive = def.OnAddedToActiveContainer(this, active);

            // 꺼진 상태에서 시작해야 켤 때 상태 변화로 보고 효과를 건다
            active.IsInhibited = true;
            SetActiveGameplayEffectInhibit(active.Handle, !shouldBeActive);
        }

        /// <summary>활성 GE를 끄거나 켠다. 끄면 modifier·부여 태그를 거두고 켜면 다시 건다. 제거하지는 않는다.</summary>
        /// <returns>계속 쓸 수 있는 핸들. 효과를 걸거나 거두는 중에 GE가 제거됐으면 Invalid.</returns>
        /// <remarks>UE: UAbilitySystemComponent::SetActiveGameplayEffectInhibit</remarks>
        internal ActiveGameplayEffectHandle SetActiveGameplayEffectInhibit(ActiveGameplayEffectHandle handle, bool inhibit)
        {
            if (!handle.IsValid || !_activeGameplayEffects.TryGetValue(handle, out ActiveGameplayEffect active))
            {
                Debug.LogError("[ASC] SetActiveGameplayEffectInhibit에 활성 GE가 아닌 핸들이 들어왔습니다.");
                return ActiveGameplayEffectHandle.Invalid;
            }

            if (active.IsInhibited != inhibit)
            {
                active.IsInhibited = inhibit;

                if (inhibit)
                {
                    RemoveActiveGameplayEffectGrantedTagsAndModifiers(active);
                }
                else
                {
                    AddActiveGameplayEffectGrantedTagsAndModifiers(active);
                }

                // 부여 태그가 바뀌면 태그 이벤트를 받은 쪽이 이 GE를 제거할 수 있다
                if (!_activeGameplayEffects.ContainsKey(handle))
                {
                    return ActiveGameplayEffectHandle.Invalid;
                }
            }

            return handle;
        }

        /// <summary>
        /// 활성 GE의 효과를 Owner에 건다 — period 0이면 modifier를 aggregator에 등록(등록이 OnDirty를 울려 CurrentValue가 재계산됨), period와 상관없이 부여 태그를 올린다.
        /// <see cref="RemoveActiveGameplayEffectGrantedTagsAndModifiers"/>와 짝 — 한쪽에 넣은 것은 다른 쪽에서 빼야 한다.
        /// </summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::AddActiveGameplayEffectGrantedTagsAndModifiers</remarks>
        private void AddActiveGameplayEffectGrantedTagsAndModifiers(ActiveGameplayEffect active)
        {
            ActiveGameplayEffectHandle handle = active.Handle;
            GameplayEffectSpec spec = active.Spec;
            GameplayEffectAsset def = spec.Definition;

            if (def.Period <= 0f)
            {
                AddSpecMods(handle, spec);

                spec.CapturedRelevantAttributes.RegisterLinkedAggregatorCallbacks(handle);
            }

            GameplayTagContainer grantedTags = def.GetGrantedTags();
            _owner.UpdateTagMap(grantedTags, 1);
        }

        /// <summary>활성 GE가 Owner에 건 효과를 거둔다 — period 0이면 modifier 해제 후 영향 어트리뷰트 재계산, 부여 태그를 내린다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::RemoveActiveGameplayEffectGrantedTagsAndModifiers</remarks>
        private void RemoveActiveGameplayEffectGrantedTagsAndModifiers(ActiveGameplayEffect active)
        {
            ActiveGameplayEffectHandle handle = active.Handle;
            GameplayEffectSpec spec = active.Spec;
            GameplayEffectAsset def = spec.Definition;

            if (def.Period <= 0f)
            {
                RemoveSpecMods(handle, spec);

                // 등록했던 의존자 콜백을 해제한다 — 안 하면 소스 aggregator가 죽은 핸들을 계속 통지하려 한다.
                spec.CapturedRelevantAttributes.UnregisterLinkedAggregatorCallbacks(handle);
            }

            GameplayTagContainer grantedTags = def.GetGrantedTags();
            _owner.UpdateTagMap(grantedTags, -1);
        }

        // ── Tick (수명) ───────────────────────────────────────────────────────────

        /// <summary>활성 이펙트의 주기 실행과 Duration 만료를 처리한다. ASC.Update가 매 프레임 위임한다.</summary>
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
                        InternalExecutePeriodicGameplayEffect(active);
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

        /// <summary>주기 GE를 한 번 실행한다. 꺼진 GE는 실행하지 않는다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::InternalExecutePeriodicGameplayEffect</remarks>
        private void InternalExecutePeriodicGameplayEffect(ActiveGameplayEffect active)
        {
            if (!active.IsInhibited)
            {
                ExecuteActiveEffectsFrom(active.Spec);
            }
        }

        /// <summary>
        /// Instant/Periodic GE의 Execute 경로 — Modifier와 Execution을 적용한다. Modifier는 배열 순서대로 BaseValue에
        /// 순차 적용돼 같은 어트리뷰트면 순서가 결과에 영향(persistent와 달리 BaseValue를 영구 변경).
        /// Source·Target 태그 조건을 못 맞춘 modifier는 건너뛴다. Target 태그와 magnitude는 실행할 때마다 다시 읽고 계산한다.
        /// </summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::ExecuteActiveEffectsFrom</remarks>
        private void ExecuteActiveEffectsFrom(GameplayEffectSpec spec)
        {
            CaptureTargetActorTags(spec);
            spec.CalculateModifierMagnitudes();

            TagContainerAggregator capturedSourceTags = spec.CapturedSourceTags;
            TagContainerAggregator capturedTargetTags = spec.CapturedTargetTags;
            GameplayTagContainer sourceActorTags = capturedSourceTags.GetActorTags();
            GameplayTagContainer targetActorTags = capturedTargetTags.GetActorTags();

            // 슬롯은 magnitude만 들고 identity(대상·연산)는 정의가 든다 — 같은 인덱스로 짝지어 읽는다.
            for (int i = 0; i < spec.Modifiers.Length; i++)
            {
                GameplayModifier modDef = spec.Definition.Modifiers[i];

                if (!RequirementsMetOrEmpty(modDef.SourceTags, sourceActorTags) || !RequirementsMetOrEmpty(modDef.TargetTags, targetActorTags))
                {
                    continue;
                }

                GameplayModifierEvaluatedData evaluatedData = new GameplayModifierEvaluatedData(modDef.ToResolvedAttribute(), spec.Modifiers[i].EvaluatedMagnitude, modDef.Operation);
                InternalExecuteMod(spec, evaluatedData);
            }

            RunExecutions(spec);

            GameplayEffectAsset def = spec.Definition;
            def.OnExecuted(this, spec);
        }

        private static bool RequirementsMetOrEmpty(GameplayTagRequirements requirements, GameplayTagContainer tags)
        {
            return requirements == null || requirements.IsEmpty() || requirements.RequirementsMet(tags);
        }

        /// <summary>Spec의 Target 액터 태그를 Owner의 지금 소유 태그로 다시 채운다.</summary>
        // UE는 적용(ApplyGameplayEffectSpec)과 실행(ExecuteActiveEffectsFrom) 양쪽에서 같은 두 줄로 한다
        private void CaptureTargetActorTags(GameplayEffectSpec spec)
        {
            TagContainerAggregator capturedTargetTags = spec.CapturedTargetTags;
            GameplayTagContainer targetActorTags = capturedTargetTags.GetActorTags();
            _owner.GetOwnedGameplayTags(targetActorTags);
        }

        /// <summary>
        /// Execution을 하나씩 실행하고 각 출력을 다음 Execution 전에 반영한다 — Execution[1]은 Execution[0]이 바꾼 어트리뷰트를 본다.
        /// params·output이 루프 내부 지역 변수라 execution 간 출력 누적은 불가능하다.
        /// </summary>
        private void RunExecutions(GameplayEffectSpec spec)
        {
            IReadOnlyList<GameplayEffectExecution> executions = spec.Executions;
            if (executions.Count == 0)
            {
                return;
            }

            IReadOnlyList<GameplayEffectExecutionDefinition> executionDefinitions = spec.ExecutionDefinitions;
            for (int executionIndex = 0; executionIndex < executions.Count; executionIndex++)
            {
                GameplayEffectExecution execution = executions[executionIndex];
                GameplayEffectExecutionDefinition executionDefinition = executionDefinitions[executionIndex];

                // execution 스코프 지역 변수. 호출당 소량 할당은 스코프 명확성을 위해 감수한다.
                GameplayEffectExecutionParameters execParams = new GameplayEffectExecutionParameters(_owner, spec, executionDefinition.CalculationModifiers);
                GameplayEffectExecutionOutput execOutput = GameplayEffectExecutionOutput.Create();

                execution.Execute(execParams, execOutput);

                foreach (GameplayModifierEvaluatedData evaluatedData in execOutput.OutputModifiers)
                {
                    InternalExecuteMod(spec, evaluatedData);
                }
            }
        }

        /// <summary>
        /// 평가 끝난 modifier 1건을 실행한다 — 대상 Set의 <see cref="AttributeSet.PreGameplayEffectExecute"/>가 수치를 고치거나 버릴 기회를 갖고,
        /// 적용 뒤 <see cref="AttributeSet.PostGameplayEffectExecute"/>가 후처리한다. Execute 경로의 유일한 쓰기 지점 —
        /// Modifier와 Execution 출력이 같은 경로를 타야 연산 지원 범위·훅 호출이 어긋나지 않는다.
        /// </summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::InternalExecuteMod</remarks>
        private void InternalExecuteMod(GameplayEffectSpec spec, GameplayModifierEvaluatedData evaluatedData)
        {
            // 존재하지 않는 어트리뷰트(무효 핸들 등)면 미러가 없으므로 건너뛴다.
            GameplayAttributeHandle handle = evaluatedData.Handle;
            if (!handle.IsValid || !_owner.TryGetAttributeData(handle, out _))
            {
                return;
            }

            AttributeSet attributeSet = _owner.GetAttributeSet(handle);
            GameplayEffectModCallbackData executeData = new GameplayEffectModCallbackData(spec, evaluatedData, _owner);

            if (!attributeSet.PreGameplayEffectExecute(executeData))
            {
                return;
            }

            // Pre가 고쳤을 수 있으니 executeData에서 다시 읽는다
            GameplayModifierEvaluatedData finalData = executeData.EvaluatedData;
            ApplyModToAttribute(finalData.Handle, finalData.Operation, finalData.Magnitude, executeData);

            attributeSet.PostGameplayEffectExecute(executeData);
        }

        /// <summary>modifier 1건을 BaseValue에 적용한다. 원인(<paramref name="modData"/>)은 이 변경의 값 변경 이벤트에 실린다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::ApplyModToAttribute</remarks>
        private void ApplyModToAttribute(GameplayAttributeHandle gameplayAttributeHandle, GameplayModifierOperation modifierOperation, float modifierMagnitude, GameplayEffectModCallbackData modData)
        {
            // Pre가 대상을 바꿨을 수 있어 다시 거른다
            if (!gameplayAttributeHandle.IsValid || !_owner.TryGetAttributeData(gameplayAttributeHandle, out _))
            {
                return;
            }

            _currentModCallbackData = modData;

            float baseValue = GetAttributeBaseValue(gameplayAttributeHandle);
            float newBaseValue = AttributeAggregator.ExecModOnBaseValue(baseValue, modifierOperation, modifierMagnitude);

            SetAttributeBaseValue(gameplayAttributeHandle, newBaseValue);

            // 남아 있으면 이 변경이 InternalUpdateNumericalAttribute까지 닿지 않은 것 — 다음 변경에 엉뚱한 원인이 실리지 않게 비운다
            if (_currentModCallbackData != null)
            {
                Debug.LogWarning($"ActiveGameplayEffectsContainer.ApplyModToAttribute: 원인 정보가 값 변경 이벤트에 실리지 않았습니다 — {gameplayAttributeHandle}");
                _currentModCallbackData = null;
            }
        }

        // ── 재계산 (aggregator OnDirty 구독) ──────────────────────────────────────

        /// <summary>어트리뷰트의 CurrentValue를 재계산한다. <paramref name="gameplayAttributeHandle"/>는 구독 시 캡처해 넘긴다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::OnAttributeAggregatorDirty</remarks>
        internal void OnAttributeAggregatorDirty(AttributeAggregator aggregator, GameplayAttributeHandle gameplayAttributeHandle)
        {
            // UE처럼 Source/Target 태그 없이 계산한다. 소유 태그에 따라 켜고 끄는 것은 GE 단위 Ongoing Tag Requirements가 맡는다
            float newValue = aggregator.Evaluate(default);

            AttributeSet attributeSet = _owner.GetAttributeSet(gameplayAttributeHandle);
            if (attributeSet == null)
            {
                return;
            }

            InternalUpdateNumericalAttribute(gameplayAttributeHandle, attributeSet, newValue);
        }

        /// <summary>
        /// 어트리뷰트의 CurrentValue를 쓰고 값 변경 이벤트를 부른다. 값이 그대로여도 부른다 — 구독자가 "적용됐지만 안 바뀜"(0 데미지 등)도 받는다.
        /// 쓰기 앞뒤로 <see cref="AttributeSet.PreAttributeChange"/>·<see cref="AttributeSet.PostAttributeChange"/>를 부르고, Pre가 고친 값을 쓰고 알린다.
        /// </summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::InternalUpdateNumericalAttribute</remarks>
        private void InternalUpdateNumericalAttribute(GameplayAttributeHandle gameplayAttributeHandle, AttributeSet attributeSet, float newValue)
        {
            float oldValue = gameplayAttributeHandle.GetCurrentValue(attributeSet);
            attributeSet.PreAttributeChange(gameplayAttributeHandle, ref newValue);
            gameplayAttributeHandle.SetCurrentValueRaw(attributeSet, newValue);
            attributeSet.PostAttributeChange(gameplayAttributeHandle, oldValue, newValue);

            // 비우기 전에 지역 변수로 받는다 — 구독자가 GE를 적용해 원인 필드를 다시 채워도 이번 이벤트의 원인은 그대로다
            GameplayEffectModCallbackData modData = _currentModCallbackData;

            if (_attributeValueChangeDelegates.TryGetValue(gameplayAttributeHandle, out GameplayAttributeValueChangeEvent attributeChanged))
            {
                attributeChanged.Broadcast(new OnAttributeChangeData(gameplayAttributeHandle, newValue, oldValue, modData));
            }

            _currentModCallbackData = null;
        }

        /// <summary>어트리뷰트의 값 변경 이벤트를 찾거나 없으면 만들어 반환한다 — 구독자는 반환값에 핸들러를 건다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::GetGameplayAttributeValueChangeDelegate</remarks>
        public GameplayAttributeValueChangeEvent GetGameplayAttributeValueChangeDelegate(GameplayAttributeHandle attributeHandle)
        {
            if (!_attributeValueChangeDelegates.TryGetValue(attributeHandle, out GameplayAttributeValueChangeEvent attributeChanged))
            {
                attributeChanged = new GameplayAttributeValueChangeEvent();
                _attributeValueChangeDelegates.Add(attributeHandle, attributeChanged);
            }

            return attributeChanged;
        }

        /// <summary>
        /// <paramref name="changedAggregator"/>에 non-snapshot으로 의존하는 활성 GE의 magnitude를 라이브로 재평가하고,
        /// 대상 aggregator에서 그 GE의 mod를 빼고 다시 넣어 반영한다. 그 갱신이 대상을 dirty시켜 재계산·연쇄된다.
        /// </summary>
        internal void OnMagnitudeDependencyChange(ActiveGameplayEffectHandle handle, AttributeAggregator changedAggregator)
        {
            if (!handle.IsValid || !_activeGameplayEffects.TryGetValue(handle, out ActiveGameplayEffect activeEffect))
            {
                return;
            }

            GameplayEffectSpec spec = activeEffect.Spec;
            GameplayEffectAsset def = spec.Definition;

            // 꺼진 GE·주기 GE는 aggregator에 mod가 없어 magnitude만 갱신한다
            bool mustUpdateAttributeAggregators = !activeEffect.IsInhibited && def.Period <= 0f;

            HashSet<GameplayAttributeHandle> attributesToUpdate = new HashSet<GameplayAttributeHandle>();

            for (int modIdx = 0; modIdx < spec.Modifiers.Length; modIdx++)
            {
                GameplayModifier modifierDefinition = def.Modifiers[modIdx];
                GameplayEffectModifierMagnitude modifierMagnitude = modifierDefinition.ModifierMagnitude;

                if (modifierMagnitude.AttemptRecalculateMagnitudeFromDependentAggregatorChange(spec, changedAggregator, out float recalculatedMagnitude))
                {
                    spec.Modifiers[modIdx].EvaluatedMagnitude = recalculatedMagnitude;

                    if (mustUpdateAttributeAggregators)
                    {
                        attributesToUpdate.Add(modifierDefinition.ToResolvedAttribute());
                    }
                }
            }

            UpdateAggregatorModMagnitudes(attributesToUpdate, activeEffect);
        }

        /// <summary>GE의 모든 modifier 대상 aggregator에 지금 magnitude를 다시 건다. 주기 GE·꺼진 GE는 aggregator에 mod가 없어 건너뛴다.</summary>
        /// <remarks>UE: FActiveGameplayEffectsContainer::UpdateAllAggregatorModMagnitudes</remarks>
        private void UpdateAllAggregatorModMagnitudes(ActiveGameplayEffect activeEffect)
        {
            GameplayEffectAsset def = activeEffect.Spec.Definition;
            if (def.Period > 0f || activeEffect.IsInhibited)
            {
                return;
            }

            var attributesToUpdate = new HashSet<GameplayAttributeHandle>();
            foreach (GameplayModifier modifier in def.Modifiers)
            {
                attributesToUpdate.Add(modifier.ToResolvedAttribute());
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

        /// <summary>
        /// persistent spec의 각 modifier를 대상 어트리뷰트 aggregator 채널에 등록한다(identity는 정의에서 인덱스로 읽는다).
        /// 태그 조건은 여기서 거르지 않고 mod와 함께 넘겨, 평가할 때마다 판정한다.
        /// </summary>
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
                aggregator.AddAggregatorMod(source, defs[i].Operation, mods[i].EvaluatedMagnitude, defs[i].SourceTags, defs[i].TargetTags);
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
        /// 어트리뷰트의 CurrentValue를 반환 — 저장값(AttributeData)을 읽는다. aggregator 계산값이 아닌 이유: 저장 직전 <see cref="AttributeSet.PreAttributeChange"/>가
        /// 고친 값(클램프 등)이 조회에도 보여야 한다. 저장값은 aggregator가 바뀔 때마다 <see cref="InternalUpdateNumericalAttribute"/>가 동기로 갱신한다.
        /// </summary>
        /// <remarks>UE: UAbilitySystemComponent::GetNumericAttribute</remarks>
        public float GetAttributeCurrentValue(GameplayAttributeHandle handle)
        {
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

            attributeSet.PreAttributeBaseChange(attributeHandle, ref newBaseValue);

            oldBaseValue = attributeHandle.GetBaseValue(attributeSet);
            attributeHandle.SetBaseValueRaw(attributeSet, newBaseValue);

            AttributeAggregator aggregator = FindAggregator(attributeHandle);
            if (aggregator != null)
            {
                oldBaseValue = aggregator.GetBaseValue();
                aggregator.SetBaseValue(newBaseValue);
            }
            else
            {
                // aggregator가 없으면 걸린 mod도 없어 CurrentValue가 곧 BaseValue다
                InternalUpdateNumericalAttribute(attributeHandle, attributeSet, newBaseValue);
            }

            attributeSet.PostAttributeBaseChange(attributeHandle, oldBaseValue, newBaseValue);
        }
    }
}
