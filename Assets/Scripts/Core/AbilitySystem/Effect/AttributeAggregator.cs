using System;
using System.Collections.Generic;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// 한 어트리뷰트의 CurrentValue를 mod로 집계한다. 값은 <see cref="Evaluate"/>로만 얻는다.
    /// 컨테이너가 어트리뷰트별로 하나씩 소유하며, mod는 채널 없이 flat 리스트로 담는다.
    /// </summary>
    public sealed class AttributeAggregator
    {
        /// <summary>담긴 mod 한 건. <see cref="ActiveHandle"/>로 식별해 제거한다.</summary>
        /// <remarks>UE: FAggregatorMod</remarks>
        private struct Mod
        {
            public readonly ActiveGameplayEffectHandle ActiveHandle;
            public readonly GameplayModifierOperation Operation;
            public readonly float EvaluatedMagnitude;

            // null이면 조건 없음. modifier 정의의 인스턴스를 그대로 참조한다(UE도 정의를 포인터로 든다).
            public readonly GameplayTagRequirements SourceTagReqs;
            public readonly GameplayTagRequirements TargetTagReqs;

            // 평가마다 UpdateQualifies로 모든 mod를 먼저 갱신하고, 값을 계산할 때는 이것만 읽는다
            private bool _isQualified;

            public Mod(ActiveGameplayEffectHandle activeHandle, GameplayModifierOperation operation, float evaluatedMagnitude, GameplayTagRequirements sourceTagReqs, GameplayTagRequirements targetTagReqs)
            {
                ActiveHandle = activeHandle;
                Operation = operation;
                EvaluatedMagnitude = evaluatedMagnitude;
                SourceTagReqs = sourceTagReqs;
                TargetTagReqs = targetTagReqs;
                _isQualified = false;
            }

            /// <summary>마지막 <see cref="UpdateQualifies"/>의 판정 결과.</summary>
            /// <remarks>UE: FAggregatorMod::Qualifies</remarks>
            public bool Qualifies()
            {
                return _isQualified;
            }

            /// <summary>Source·Target 태그 조건을 모두 만족하는지 판정해 담는다. 조건이 없거나 비었으면 만족으로 본다.</summary>
            /// <remarks>UE: FAggregatorMod::UpdateQualifies</remarks>
            public void UpdateQualifies(GameplayTagContainer sourceTags, GameplayTagContainer targetTags)
            {
                _isQualified = RequirementsMet(SourceTagReqs, sourceTags) && RequirementsMet(TargetTagReqs, targetTags);
            }

            private static bool RequirementsMet(GameplayTagRequirements requirements, GameplayTagContainer tags)
            {
                if (requirements == null || requirements.IsEmpty())
                {
                    return true;
                }

                return requirements.RequirementsMet(tags ?? EmptyTags);
            }
        }

        // 평가 문맥에 태그가 없을 때(null) 판정에 쓰는 빈 컨테이너. 읽기만 한다.
        private static readonly GameplayTagContainer EmptyTags = new();

        private float _baseValue;

        // persistent(period 0) mod. apply 시 등록, remove 시 해제.
        private readonly List<Mod> _mods = new();

        // 이 값에 non-snapshot으로 의존하는 활성 GE. dirty 시 이들의 magnitude 재평가를 촉발한다.
        private readonly List<ActiveGameplayEffectHandle> _dependents = new();

        /// <summary>base/mod 변경 시 발화한다. 구독자가 <see cref="Evaluate"/>로 재계산한다.</summary>
        public event Action<AttributeAggregator> OnDirty;

        // 순환 의존(A→B→A)으로 인한 재귀 broadcast 폭주를 끊는 깊이 상한.
        private const int MaxBroadcastDirty = 10;

        private int _broadcastingDirtyCount;

        public AttributeAggregator(float baseValue)
        {
            _baseValue = baseValue;
        }

        /// <summary>빈 aggregator — <see cref="TakeSnapshotOf"/>로 채워 쓰는 용도.</summary>
        public AttributeAggregator()
        {
        }

        /// <summary><paramref name="snapshotFrom"/>의 base·mod를 통째 복사한다. OnDirty·dependents는 잇지 않아 이후 원본 변화와 무관한 고정본이 된다.</summary>
        public void TakeSnapshotOf(AttributeAggregator snapshotFrom)
        {
            _baseValue = snapshotFrom._baseValue;
            _mods.Clear();
            _mods.AddRange(snapshotFrom._mods);
        }

        public float GetBaseValue()
        {
            return _baseValue;
        }

        public void SetBaseValue(float value, bool broadcastDirtyEvent = true)
        {
            _baseValue = value;

            if (broadcastDirtyEvent)
            {
                BroadcastOnDirty();
            }
        }

        /// <param name="sourceTagReqs">Source 태그 조건. null이면 조건 없음.</param>
        /// <param name="targetTagReqs">Target 태그 조건. null이면 조건 없음.</param>
        /// <remarks>UE: FAggregator::AddAggregatorMod</remarks>
        public void AddAggregatorMod(ActiveGameplayEffectHandle source, GameplayModifierOperation operation, float magnitude, GameplayTagRequirements sourceTagReqs = null, GameplayTagRequirements targetTagReqs = null)
        {
            _mods.Add(new Mod(source, operation, magnitude, sourceTagReqs, targetTagReqs));
            BroadcastOnDirty();
        }

        public void RemoveAggregatorMod(ActiveGameplayEffectHandle source)
        {
            _mods.RemoveAll(mod => mod.ActiveHandle.Equals(source));
            BroadcastOnDirty();
        }

        public void AddDependent(ActiveGameplayEffectHandle handle)
        {
            _dependents.Add(handle);
        }

        public void RemoveDependent(ActiveGameplayEffectHandle handle)
        {
            _dependents.Remove(handle);
        }

        /// <summary>base/mod 변경을 구독자·dependents에 전파한다. 순환 의존은 <see cref="MaxBroadcastDirty"/> 깊이 상한으로 끊는다(폭주 방지지 해결이 아니다).</summary>
        private void BroadcastOnDirty()
        {
            if (_broadcastingDirtyCount > MaxBroadcastDirty)
            {
                Debug.LogError("AttributeAggregator: 순환 어트리뷰트 의존이 감지되어 재귀 dirty 호출을 건너뜁니다.");
                return;
            }

            _broadcastingDirtyCount++;

            OnDirty?.Invoke(this);

            // 복사 필수 — 콜백이 이 aggregator를 재진입해 _dependents를 건드린다. 비운 뒤 살아있는 핸들만 재등록한다.
            List<ActiveGameplayEffectHandle> dependentsLocalCopy = new List<ActiveGameplayEffectHandle>(_dependents);
            _dependents.Clear();

            foreach (ActiveGameplayEffectHandle handle in dependentsLocalCopy)
            {
                AbilitySystemComponent abilitySystemComponent = handle.GetOwningAbilitySystemComponent();
                if (abilitySystemComponent)
                {
                    abilitySystemComponent.OnMagnitudeDependencyChange(handle, this);
                    _dependents.Add(handle);
                }
            }

            _broadcastingDirtyCount--;
        }

        /// <summary>
        /// 평가 문맥의 태그로 자격을 판정해 값을 계산한다. 캡처 어트리뷰트 평가(AttributeBased·Execution)는 캡처하는 Spec의 태그를, CurrentValue 계산은 빈 문맥을 넘긴다.
        /// = ((Base + ΣAddBase) * MultiplyAdditive / DivideAdditive * ΠMultiplyCompound) + ΣAddFinal.
        /// Override가 있으면 그 값으로 덮어쓴다(조건을 만족하는 Override 중 목록에서 먼저 만난 것).
        /// </summary>
        /// <remarks>UE: FAggregator::Evaluate</remarks>
        public float Evaluate(AggregatorEvaluateParameters parameters)
        {
            EvaluateQualificationForAllMods(parameters);
            return EvaluateWithBase();
        }

        /// <summary>모든 mod의 자격을 평가 문맥의 태그로 갱신한다. <see cref="EvaluateWithBase"/> 전에 부른다.</summary>
        /// <remarks>UE: FAggregator::EvaluateQualificationForAllMods</remarks>
        private void EvaluateQualificationForAllMods(AggregatorEvaluateParameters parameters)
        {
            for (int modIndex = 0; modIndex < _mods.Count; modIndex++)
            {
                Mod mod = _mods[modIndex];
                mod.UpdateQualifies(parameters.SourceTags, parameters.TargetTags);

                // Mod는 값 타입이라 갱신한 복사본을 다시 넣어야 목록에 자격이 남는다
                _mods[modIndex] = mod;
            }
        }

        /// <summary>자격이 있는 mod만 모아 값을 계산한다. 자격은 <see cref="EvaluateQualificationForAllMods"/>가 갱신해 둔 것을 읽는다.</summary>
        /// <remarks>UE: FAggregatorModChannel::EvaluateWithBase</remarks>
        private float EvaluateWithBase()
        {
            float addBase = 0f;
            float multiplyAdditive = 1f;
            float divideAdditive = 1f;
            float multiplyCompound = 1f;
            float addFinal = 0f;

            foreach (Mod mod in _mods)
            {
                if (!mod.Qualifies())
                {
                    continue;
                }

                switch (mod.Operation)
                {
                    case GameplayModifierOperation.AddBase:
                    {
                        addBase += mod.EvaluatedMagnitude;
                        break;
                    }
                    case GameplayModifierOperation.MultiplyAdditive:
                    {
                        multiplyAdditive += mod.EvaluatedMagnitude - 1f;
                        break;
                    }
                    case GameplayModifierOperation.DivideAdditive:
                    {
                        divideAdditive += mod.EvaluatedMagnitude - 1f;
                        break;
                    }
                    case GameplayModifierOperation.MultiplyCompound:
                    {
                        multiplyCompound *= mod.EvaluatedMagnitude;
                        break;
                    }
                    case GameplayModifierOperation.AddFinal:
                    {
                        addFinal += mod.EvaluatedMagnitude;
                        break;
                    }
                    case GameplayModifierOperation.Override:
                    {
                        float overrideValue = mod.EvaluatedMagnitude;
                        return overrideValue;
                    }
                }
            }

            if (Mathf.Approximately(divideAdditive, 0f))
            {
                divideAdditive = 1f;
            }

            return ((_baseValue + addBase) * multiplyAdditive / divideAdditive * multiplyCompound) + addFinal;
        }

        /// <summary>연산을 base에 직접 적용한 값을 반환한다 — Instant/Periodic의 영구 base 변경용. <see cref="Evaluate"/>와 달리 즉시·영구다.</summary>
        public static float ExecModOnBaseValue(float baseValue, GameplayModifierOperation modifierOp, float evaluatedMagnitude)
        {
            switch (modifierOp)
            {
                case GameplayModifierOperation.AddBase:
                case GameplayModifierOperation.AddFinal:
                {
                    baseValue += evaluatedMagnitude;
                    break;
                }

                case GameplayModifierOperation.MultiplyAdditive:
                case GameplayModifierOperation.MultiplyCompound:
                {
                    baseValue *= evaluatedMagnitude;
                    break;
                }
                case GameplayModifierOperation.DivideAdditive:
                {
                    baseValue = Mathf.Approximately(evaluatedMagnitude, 0f) ? baseValue : baseValue / evaluatedMagnitude;
                    break;
                }
                case GameplayModifierOperation.Override:
                {
                    baseValue = evaluatedMagnitude;
                    break;
                }
            }

            return baseValue;
        }

        /// <summary>이 effect가 <paramref name="attributeHandle"/>에 건 mod를 모두 빼고 spec의 최신 magnitude로 다시 넣는다.</summary>
        /// <remarks>UE: FAggregator::UpdateAggregatorMod</remarks>
        public void UpdateAggregatorMod(ActiveGameplayEffect activeEffect, GameplayAttributeHandle attributeHandle)
        {
            ActiveGameplayEffectHandle activeHandle = activeEffect.Handle;

            // _mods에는 여러 GE의 mod가 섞여 있어 spec의 modifier 번호로 칸을 찾을 수 없다. 핸들로 빼고 다시 넣는다.
            // 다 넣은 뒤 dirty를 한 번만 알리려고 RemoveAggregatorMod 대신 목록에서 바로 뺀다
            _mods.RemoveAll(mod => mod.ActiveHandle.Equals(activeHandle));

            GameplayEffectSpec gameplayEffectSpec = activeEffect.Spec;

            for (int modIndex = 0; modIndex < gameplayEffectSpec.Modifiers.Length; modIndex++)
            {
                GameplayModifier modDef = gameplayEffectSpec.Definition.Modifiers[modIndex];

                if (modDef.ToResolvedAttribute() == attributeHandle)
                {
                    _mods.Add(new Mod(activeHandle, modDef.Operation, gameplayEffectSpec.Modifiers[modIndex].EvaluatedMagnitude, modDef.SourceTags, modDef.TargetTags));
                }
            }

            BroadcastOnDirty();
        }
    }
}
