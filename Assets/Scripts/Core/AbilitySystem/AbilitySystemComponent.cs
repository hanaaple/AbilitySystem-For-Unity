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
        [SerializeField] private AttributeInitData attributeInitData;

        private readonly Dictionary<Type, AttributeSet> _spawnedAttributeSets = new();
        private readonly Dictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> _activeEffects = new();

        private readonly List<ActiveGameplayEffectHandle> _expiredEffects = new();
        private readonly HashSet<AttributeHandle> _tempHandles = new();

        private static int _handleIdCounter = 0;

        private void Awake()
        {
            InitAttributeSets();
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
        public bool AddAttributeSet(AttributeSet set)
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

        // ── Apply / Remove ────────────────────────────────────────────────────────

        /// <summary>
        /// GameplayEffect SO로부터 Spec을 생성해 자신에게 적용한다.
        /// Instant는 즉시 실행 후 Invalid Handle 반환. Duration/Infinite는 핸들 반환.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToSelf(GameplayEffect effect, GameplayEffectContextHandle context = default, float level = 1f)
        {
            var spec = new GameplayEffectSpec(effect, context, level);
            return ApplyGameplayEffectSpecToSelf(spec);
        }

        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(GameplayEffectSpec spec)
        {
            if (spec.Modifiers.Count == 0)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            GameplayEffect def = spec.Definition;

            if (def.Type == GameplayEffectType.Instant)
            {
                ExecuteModifiers(spec);
                return ActiveGameplayEffectHandle.Invalid;
            }

            var handle = new ActiveGameplayEffectHandle(++_handleIdCounter);
            var active = new ActiveGameplayEffect(handle, spec);
            _activeEffects.Add(handle, active);

            if (def.Period > 0f && def.ExecutePeriodicEffectOnApplication)
            {
                ExecuteModifiers(spec);
            }

            // period == 0인 경우만 persistent modifier로서 CurrentValue에 반영
            if (def.Period <= 0f)
            {
                RecalculateAffectedAttributes(spec);
            }

            return handle;
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
                GameplayEffect def = active.Spec.Definition;

                if (def.Period > 0f)
                {
                    active.PeriodTimer += deltaTime;
                    while (active.PeriodTimer >= def.Period)
                    {
                        active.PeriodTimer -= def.Period;
                        ExecuteModifiers(active.Spec);
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

        // ── Modifier 실행 (Instant / Periodic) ───────────────────────────────────

        /// <summary>
        /// Instant / Periodic GE의 Execute 경로. GAS의 ExecuteGameplayEffect와 동일한 방식으로 동작한다.
        /// Modifiers 배열 순서대로 BaseValue에 순차 적용된다. 각 Modifier는 이전 Modifier가 쓴 결과를
        /// 읽어 연산하므로, 같은 어트리뷰트를 대상으로 하는 Modifier가 여러 개일 때 순서가 결과에 영향을 준다.
        /// (Aggregator로 CurrentValue만 수정하는 persistent modifier와 달리, BaseValue를 영구 변경한다.)
        /// </summary>
        private void ExecuteModifiers(GameplayEffectSpec spec)
        {
            _tempHandles.Clear();

            foreach (GameplayModifierSpec modSpec in spec.Modifiers)
            {
                if (!TryGetAttributeData(modSpec.Handle, out AttributeData data))
                {
                    continue;
                }

                float mag = modSpec.EvaluatedMagnitude;
                switch (modSpec.Operation)
                {
                    case GameplayModifierOperation.AddBase:
                    case GameplayModifierOperation.AddFinal:
                    {
                        data.BaseValue += mag;
                        break;
                    }
                    case GameplayModifierOperation.MultiplyAdditive:
                    case GameplayModifierOperation.MultiplyCompound:
                    {
                        data.BaseValue *= mag;
                        break;
                    }
                    case GameplayModifierOperation.DivideAdditive:
                    {
                        data.BaseValue /= (mag != 0f ? mag : 1f);
                        break;
                    }
                    case GameplayModifierOperation.Override:
                    {
                        data.BaseValue = mag;
                        break;
                    }
                }

                TrySetAttributeData(modSpec.Handle, data);
                _tempHandles.Add(modSpec.Handle);
            }

            RunExecutions(spec);

            foreach (AttributeHandle handle in _tempHandles)
            {
                RecalculateAttributeCurrentValue(handle);
            }
        }

        private void RunExecutions(GameplayEffectSpec spec)
        {
            IReadOnlyList<GameplayEffectExecution> executions = spec.Definition.Executions;
            if (executions == null || executions.Count == 0)
            {
                return;
            }

            var execParams = new GameplayEffectExecutionParameters(this, spec);
            foreach (GameplayEffectExecution execution in executions)
            {
                if (execution == null)
                {
                    continue;
                }

                execution.Execute(execParams);
            }

            foreach (GameplayEffectExecutionOutput output in execParams.Outputs)
            {
                if (!TryGetAttributeData(output.Handle, out AttributeData data))
                {
                    continue;
                }

                switch (output.Operation)
                {
                    case GameplayModifierOperation.AddBase:
                        data.BaseValue += output.Magnitude;
                        break;
                    case GameplayModifierOperation.Override:
                        data.BaseValue = output.Magnitude;
                        break;
                    default:
                        Debug.LogWarning($"[ASC] Execution output '{output.Operation}'은 AddBase 또는 Override만 지원합니다.");
                        continue;
                }

                TrySetAttributeData(output.Handle, data);
                _tempHandles.Add(output.Handle);
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

        private void InitAttributeSets()
        {
            if (attributeInitData == null)
            {
                return;
            }

            foreach (AttributeSetInitData attributeSetData in attributeInitData.AttributeSets)
            {
                Type attributeSetType = attributeSetData.GetAttributeSetType();
                if (attributeSetType == null || !typeof(AttributeSet).IsAssignableFrom(attributeSetType))
                {
                    Debug.LogWarning($"[ASC] '{attributeSetData.GetType().Name}' 에서 유효하지 않은 AttributeSet 타입.");
                    continue;
                }

                var set = (AttributeSet)Activator.CreateInstance(attributeSetType);
                foreach (AttributeFieldInitData fieldData in attributeSetData.Attributes)
                {
                    FieldInfo field = attributeSetType.GetField(fieldData.FieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (field == null)
                    {
                        Debug.LogWarning($"[ASC] 필드 '{fieldData.FieldName}'을 {attributeSetType.Name}에서 찾을 수 없음.");
                        continue;
                    }
                    field.SetValue(set, fieldData.Data);
                }

                AddAttributeSet(set);
            }
        }
    }
}
