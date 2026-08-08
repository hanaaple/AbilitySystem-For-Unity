using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Core.AbilitySystem
{
    public class AbilitySystemComponent : MonoBehaviour
    {
        [SerializeField] private AttributeDefinitionAsset attributeInitData;

        private readonly Dictionary<Type, AttributeSet> _spawnedAttributeSets = new();
        private readonly Dictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> _activeEffects = new();

        private readonly List<ActiveGameplayEffectHandle> _expiredEffects = new();
        private readonly HashSet<AttributeHandle> _tempHandles = new();

        private static int _handleIdCounter = 0;

        private void Awake()
        {
            AddSet(attributeInitData);
        }

        private void Update()
        {
            if (_activeEffects.Count == 0)
            {
                return;
            }

            TickActiveEffects(Time.deltaTime);
        }

        // ── AttributeSet ──────────────────────────────────────────────────────────

        /// <summary>같은 타입의 AttributeSet은 하나만 등록 가능.</summary>
        public bool AddSpawnedAttribute(AttributeSet set)
        {
            return _spawnedAttributeSets.TryAdd(set.GetType(), set);
        }

        public void RemoveAttributeSet(AttributeSet set)
        {
            _spawnedAttributeSets.Remove(set.GetType());
        }

        /// <summary>핸들이 가리키는 어트리뷰트의 BaseValue를 반환.</summary>
        public float GetAttributeBaseValue(AttributeHandle handle)
        {
            return TryGetAttributeData(handle, out AttributeData data) ? data.BaseValue : 0f;
        }

        /// <summary>핸들이 가리키는 어트리뷰트의 CurrentValue를 반환.</summary>
        public float GetAttributeCurrentValue(AttributeHandle handle)
        {
            return TryGetAttributeData(handle, out AttributeData data) ? data.CurrentValue : 0f;
        }

        /// <summary>핸들이 가리키는 어트리뷰트의 BaseValue를 직접 설정하고 CurrentValue를 재계산한다.</summary>
        public void SetBaseAttributeValue(AttributeHandle handle, float value)
        {
            if (!TryGetAttributeData(handle, out AttributeData data))
            {
                return;
            }

            data.BaseValue = value;
            TrySetAttributeData(handle, data);
            RecalculateAttributeCurrentValue(handle);
        }

        // ── Context / Spec 팩토리 ─────────────────────────────────────────────────

        /// <summary>
        /// 자신(this)을 Instigator(주체 ASC)로 하는 GE 컨텍스트를 만든다.
        /// SourceObject(무기·아이템 등 출처)가 필요하면 호출처에서 AddSourceObject로 따로 넣는다.
        /// (UE: MakeEffectContext)
        /// </summary>
        public GameplayEffectContextHandle MakeEffectContext()
        {
            var context = new GameplayEffectContextHandle(new GameplayEffectContext());
            context.AddInstigator(this);
            return context;
        }

        /// <summary>GameplayEffectAsset(SO)로부터 적용 대기 상태의 Spec을 만든다. (UE: MakeOutgoingSpec)</summary>
        public GameplayEffectSpec MakeOutgoingSpec(GameplayEffectAsset effect, GameplayEffectContextHandle context = default, float level = 1f)
        {
            if (effect == null)
            {
                Debug.LogWarning($"[ASC] '{name}' 에서 GameplayEffectAsset이 null이라 Spec을 만들 수 없습니다.");
                return null;
            }

            if (!context.IsValid)
            {
                context = MakeEffectContext();
            }

            return new GameplayEffectSpec(effect, context, level);
        }

        // ── Apply / Remove ────────────────────────────────────────────────────────

        /// <summary>
        /// GameplayEffectAsset SO로부터 Spec을 생성해 자신에게 적용한다.
        /// Instant는 즉시 실행 후 Invalid Handle 반환. Duration/Infinite는 핸들 반환.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToSelf(GameplayEffectAsset effect, GameplayEffectContextHandle context = default, float level = 1f)
        {
            return ApplyGameplayEffectSpecToSelf(MakeOutgoingSpec(effect, context, level));
        }

        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            // Modifier 없이 Execution만 가진 GE도 유효하다(데미지 계산을 전부 Execution에 두는 경우).
            if (spec.Modifiers.Count == 0 && spec.Executions.Count == 0)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            // 적용 경계에서 spec을 한 번 복제하고 그 복사본에 Target(this=적용 대상)을 캡처한다 —
            // Instant·Duration 공통. 복사본에 캡처해야 여러 대상에 같은 spec을 적용해도 서로의 캡처값을 덮어쓰지 않고(→D14/D19),
            // 하나의 복사본이 캡처→실행→저장을 관통한다.
            // (UE: Duration은 ApplyGameplayEffectSpec 내부 복사본에, Instant는 StackSpec 복사본에 각각 CaptureAttributeDataFromTarget —
            //  두 경로 모두 복사본에 캡처한다. 우리 구조에선 이중 복사가 없어 이 한 곳으로 통일한다.)
            GameplayEffectSpec appliedSpec = spec.Clone();
            appliedSpec.CaptureAttributeDataFromTarget(this);

            GameplayEffectAsset def = appliedSpec.Definition;

            if (def.Type == GameplayEffectType.Instant)
            {
                ExecuteGameplayEffect(appliedSpec);
                return ActiveGameplayEffectHandle.Invalid;
            }

            var handle = new ActiveGameplayEffectHandle(++_handleIdCounter);
            var active = new ActiveGameplayEffect(handle, appliedSpec, this);
            _activeEffects.Add(handle, active);

            if (def.Period > 0f && def.ExecutePeriodicEffectOnApplication)
            {
                ExecuteGameplayEffect(appliedSpec);
            }

            // period == 0인 경우만 persistent modifier로서 CurrentValue에 반영
            if (def.Period <= 0f)
            {
                RecalculateAffectedAttributes(appliedSpec);
            }

            return handle;
        }

        /// <summary>
        /// GameplayEffectAsset SO로부터 Spec을 생성해 대상 ASC에 적용한다. 자신(this)이 Instigator가 된다.
        /// context가 비어 있으면 자신을 Instigator로 하는 context를 생성한다(AttributeBased의 Source 캡처용).
        /// (UE: ApplyGameplayEffectToTarget → MakeOutgoingSpec → Target->ApplyGameplayEffectSpecToSelf)
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToTarget(GameplayEffectAsset effect, AbilitySystemComponent target, GameplayEffectContextHandle context = default, float level = 1f)
        {
            if (!context.IsValid)
            {
                context = MakeEffectContext();
            }

            return ApplyGameplayEffectSpecToTarget(MakeOutgoingSpec(effect, context, level), target);
        }

        /// <summary>
        /// 이미 만들어진 Spec을 대상 ASC에 적용한다. 실제 적용은 대상 ASC가 자신에게 수행한다.
        /// (UE: ApplyGameplayEffectSpecToTarget → Target->ApplyGameplayEffectSpecToSelf)
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToTarget(GameplayEffectSpec spec, AbilitySystemComponent target)
        {
            if (target == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            return target.ApplyGameplayEffectSpecToSelf(spec);
        }

        public bool RemoveActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            if (!handle.IsValid || !_activeEffects.Remove(handle, out ActiveGameplayEffect active))
            {
                return false;
            }

            if (active.Spec.Definition.Period == 0f)
            {
                RecalculateAffectedAttributes(active.Spec);
            }

            return true;
        }

        // ── Tick ──────────────────────────────────────────────────────────────────

        private void TickActiveEffects(float deltaTime)
        {
            _expiredEffects.Clear();

            foreach (ActiveGameplayEffect active in _activeEffects.Values)
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
        /// Instant / Periodic GE의 Execute 경로 — Modifier 적용과 Execution 실행을 모두 수행한다.
        /// (UE: FActiveGameplayEffectsContainer::ExecuteActiveEffectsFrom)
        /// Modifiers 배열 순서대로 BaseValue에 순차 적용된다. 각 Modifier는 이전 Modifier가 쓴 결과를
        /// 읽어 연산하므로, 같은 어트리뷰트를 대상으로 하는 Modifier가 여러 개일 때 순서가 결과에 영향을 준다.
        /// (Aggregator로 CurrentValue만 수정하는 persistent modifier와 달리, BaseValue를 영구 변경한다.)
        ///
        /// BaseValue를 쓸 때마다 그 자리에서 CurrentValue까지 재계산해 둘의 정합을 항상 유지한다.
        /// (UE: InternalExecuteMod → SetAttributeBaseValue가 어그리게이터를 MarkDirty해 즉시 갱신.)
        /// 마지막에 몰아서 갱신하면 뒤따르는 Modifier·Execution이 stale CurrentValue를 읽게 된다.
        /// </summary>
        private void ExecuteGameplayEffect(GameplayEffectSpec spec)
        {
            foreach (GameplayModifierSpec modSpec in spec.Modifiers)
            {
                ApplyEvaluatedModifier(new GameplayModifierEvaluatedData(modSpec.Handle, modSpec.EvaluatedMagnitude, modSpec.Operation));
            }

            RunExecutions(spec);
        }

        /// <summary>
        /// Execution을 하나씩 실행하고, **각 Execution의 출력을 다음 Execution 전에 반영**한다.
        /// 따라서 Execution[1]은 Execution[0]이 바꾼 어트리뷰트를 본다.
        /// (UE: ExecuteActiveEffectsFrom에서 ExecutionParams·ExecutionOutput이 executions 루프 내부의 지역 변수라,
        ///  출력을 execution 간에 누적하는 것 자체가 불가능하다.)
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
                // 둘 다 execution 스코프 지역 변수다(UE도 동일). output은 내부 List 때문에 호출당
                // 소량 할당이 있지만, 재사용(Clear)으로 아끼는 양보다 스코프가 명확한 쪽이 낫다고 보고 감수한다.
                GameplayEffectExecutionParameters execParams = new GameplayEffectExecutionParameters(this, spec);
                GameplayEffectExecutionOutput execOutput = GameplayEffectExecutionOutput.Create();

                execution.Execute(execParams, execOutput);

                foreach (GameplayModifierEvaluatedData evaluated in execOutput.OutputModifiers)
                {
                    ApplyEvaluatedModifier(evaluated);
                }
            }
        }

        /// <summary>
        /// 평가가 끝난 모디파이어 1건을 BaseValue에 적용하고 CurrentValue까지 즉시 재계산한다.
        /// **Execute 경로의 유일한 쓰기 지점**이다 — Modifier와 Execution 출력이 같은 경로를 타야
        /// 둘의 연산 지원 범위가 어긋나지 않는다.
        /// (UE: FActiveGameplayEffectsContainer::InternalExecuteMod → ApplyModToAttribute → SetAttributeBaseValue)
        /// </summary>
        private void ApplyEvaluatedModifier(GameplayModifierEvaluatedData evaluated)
        {
            if (!TryGetAttributeData(evaluated.Handle, out AttributeData data))
            {
                return;
            }

            data.BaseValue = ExecuteModOnBaseValue(data.BaseValue, evaluated.Operation, evaluated.Magnitude);
            TrySetAttributeData(evaluated.Handle, data);
            RecalculateAttributeCurrentValue(evaluated.Handle);
        }

        /// <summary>
        /// Execute 경로에서 연산을 BaseValue에 직접 적용한다.
        /// persistent 경로(aggregator 누산)와 달리 즉시·영구 변경이라, 가산 계열(AddBase/AddFinal)과
        /// 배율 계열(MultiplyAdditive/MultiplyCompound)은 각각 같은 연산으로 수렴한다.
        /// (UE: FAggregator::StaticExecModOnBaseValue)
        /// </summary>
        private static float ExecuteModOnBaseValue(float baseValue, GameplayModifierOperation operation, float magnitude)
        {
            switch (operation)
            {
                case GameplayModifierOperation.AddBase:
                case GameplayModifierOperation.AddFinal:
                    return baseValue + magnitude;

                case GameplayModifierOperation.MultiplyAdditive:
                case GameplayModifierOperation.MultiplyCompound:
                    return baseValue * magnitude;

                case GameplayModifierOperation.DivideAdditive:
                    return Mathf.Approximately(magnitude, 0f) ? baseValue : baseValue / magnitude;

                case GameplayModifierOperation.Override:
                    return magnitude;

                default:
                    return baseValue;
            }
        }

        // ── CurrentValue 재계산 ───────────────────────────────────────────────────

        private void RecalculateAffectedAttributes(GameplayEffectSpec spec)
        {
            _tempHandles.Clear();

            foreach (GameplayModifierSpec modSpec in spec.Modifiers)
            {
                _tempHandles.Add(modSpec.Handle);
            }

            foreach (AttributeHandle handle in _tempHandles)
            {
                RecalculateAttributeCurrentValue(handle);
            }
        }

        /// <summary>
        /// 활성 중인 모든 persistent(period == 0) GE의 모디파이어를 수집해
        /// 해당 어트리뷰트의 CurrentValue를 재계산한다.
        /// 공식: CurrentValue = ((Base + ΣAddBase) * MultiplyAdditive / DivideAdditive * ΠMultiplyCompound) + ΣAddFinal
        /// </summary>
        private void RecalculateAttributeCurrentValue(AttributeHandle handle)
        {
            if (!TryGetAttributeData(handle, out AttributeData data))
            {
                return;
            }

            float newValue = CalculateAttributeCurrentValue(handle, data.BaseValue);
            UpdateAttributeCurrentValue(handle, newValue);
        }

        /// <summary>활성 GE 모디파이어를 수집해 CurrentValue를 계산하고 반환한다. 상태 변경 없음.</summary>
        private float CalculateAttributeCurrentValue(AttributeHandle handle, float baseValue)
        {
            float addBase = 0f;
            float multiplyAdditive = 1f;
            float divideAdditive = 1f;
            float multiplyCompound = 1f;
            float addFinal = 0f;
            bool hasOverride = false;
            float overrideValue = 0f;

            foreach (ActiveGameplayEffect active in _activeEffects.Values)
            {
                if (active.Spec.Definition.Period > 0f)
                {
                    continue;
                }

                foreach (GameplayModifierSpec modSpec in active.Spec.Modifiers)
                {
                    if (!modSpec.Handle.Equals(handle))
                    {
                        continue;
                    }

                    float mag = modSpec.EvaluatedMagnitude;
                    switch (modSpec.Operation)
                    {
                        case GameplayModifierOperation.AddBase:
                        {
                            addBase += mag;
                            break;
                        }
                        case GameplayModifierOperation.MultiplyAdditive:
                        {
                            multiplyAdditive += mag - 1f;
                            break;
                        }
                        case GameplayModifierOperation.DivideAdditive:
                        {
                            divideAdditive += mag - 1f;
                            break;
                        }
                        case GameplayModifierOperation.MultiplyCompound:
                        {
                            multiplyCompound *= mag;
                            break;
                        }
                        case GameplayModifierOperation.AddFinal:
                        {
                            addFinal += mag;
                            break;
                        }
                        case GameplayModifierOperation.Override:
                        {
                            hasOverride = true;
                            overrideValue = mag;
                            break;
                        }
                    }
                }
            }

            if (Mathf.Approximately(divideAdditive, 0f))
            {
                divideAdditive = 1f;
            }

            return hasOverride
                ? overrideValue
                : ((baseValue + addBase) * multiplyAdditive / divideAdditive * multiplyCompound) + addFinal;
        }

        private void UpdateAttributeCurrentValue(AttributeHandle handle, float value)
        {
            if (!TryGetAttributeData(handle, out AttributeData data))
            {
                return;
            }

            data.CurrentValue = value;
            TrySetAttributeData(handle, data);
        }

        // ── AttributeData 접근 ────────────────────────────────────────────────────

        private bool TryGetAttributeData(AttributeHandle handle, out AttributeData data)
        {
            if (_spawnedAttributeSets.TryGetValue(handle.SetType, out AttributeSet set))
            {
                return handle.TryGetData(set, out data);
            }

            data = default;
            return false;
        }

        private bool TrySetAttributeData(AttributeHandle handle, AttributeData data)
        {
            if (_spawnedAttributeSets.TryGetValue(handle.SetType, out AttributeSet set))
            {
                return handle.TrySetData(set, data);
            }

            return false;
        }

        // ── 초기화 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 주어진 정의(SO)로 AttributeSet들을 생성해 등록한다. 이미 같은 타입이 등록돼 있으면 건너뛴다(AddSpawnedAttribute=TryAdd).
        /// Awake의 자체 초기화(attributeInitData) 외에, 외부(예: BoxRoom)가 Source 캡처용 어트리뷰트를 주입할 때도 쓴다.
        /// </summary>
        public void AddSet(AttributeDefinitionAsset data)
        {
            if (data == null)
            {
                return;
            }

            foreach (AttributeSetDefinition attributeSetData in data.AttributeSets)
            {
                Type attributeSetType = attributeSetData.GetAttributeSetType();
                if (attributeSetType == null || !typeof(AttributeSet).IsAssignableFrom(attributeSetType))
                {
                    Debug.LogWarning($"[ASC] '{attributeSetData.GetType().Name}' 에서 유효하지 않은 AttributeSet 타입.");
                    continue;
                }

                var set = (AttributeSet)Activator.CreateInstance(attributeSetType);
                foreach (AttributeFieldDefinition fieldData in attributeSetData.Attributes)
                {
                    FieldInfo field = attributeSetType.GetField(fieldData.FieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (field == null)
                    {
                        Debug.LogWarning($"[ASC] 필드 '{fieldData.FieldName}'을 {attributeSetType.Name}에서 찾을 수 없음.");
                        continue;
                    }
                    field.SetValue(set, fieldData.Data);
                }

                AddSpawnedAttribute(set);
            }
        }
    }
}
