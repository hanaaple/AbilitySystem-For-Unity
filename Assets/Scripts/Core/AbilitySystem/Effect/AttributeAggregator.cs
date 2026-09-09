using System;
using System.Collections.Generic;
using Core.AbilitySystem.Attribute;
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
        private readonly struct Mod
        {
            public readonly ActiveGameplayEffectHandle ActiveHandle;
            public readonly GameplayModifierOperation Operation;
            public readonly float EvaluatedMagnitude;

            public Mod(ActiveGameplayEffectHandle activeHandle, GameplayModifierOperation operation, float evaluatedMagnitude)
            {
                ActiveHandle = activeHandle;
                Operation = operation;
                EvaluatedMagnitude = evaluatedMagnitude;
            }
        }

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

        public void AddAggregatorMod(ActiveGameplayEffectHandle source, GameplayModifierOperation operation, float magnitude)
        {
            _mods.Add(new Mod(source, operation, magnitude));
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
        /// base·mod로 CurrentValue를 계산한다(순수). 값 취득의 유일 경로다.
        /// = ((Base + ΣAddBase) * MultiplyAdditive / DivideAdditive * ΠMultiplyCompound) + ΣAddFinal.
        /// Override가 있으면 그 값으로 덮어쓴다(마지막 Override 우선).
        /// </summary>
        public float Evaluate()
        {
            float addBase = 0f;
            float multiplyAdditive = 1f;
            float divideAdditive = 1f;
            float multiplyCompound = 1f;
            float addFinal = 0f;

            foreach (Mod mod in _mods)
            {
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

        /// <summary>이 effect의 <paramref name="attributeHandle"/> 대상 mod magnitude를 spec의 최신 값으로 제자리 갱신한다.</summary>
        public void UpdateAggregatorMod(ActiveGameplayEffect activeEffect, GameplayAttributeHandle attributeHandle)
        {
            GameplayEffectSpec gameplayEffectSpec = activeEffect.Spec;
            for (int modIndex = 0; modIndex < gameplayEffectSpec.Modifiers.Length; modIndex++)
            {
                GameplayModifier modDef = gameplayEffectSpec.Definition.Modifiers[modIndex];

                if (modDef.ToResolvedAttribute() == attributeHandle)
                {
                    _mods[modIndex] = new Mod(activeEffect.Handle, modDef.Operation, gameplayEffectSpec.Modifiers[modIndex].EvaluatedMagnitude);
                }
            }

            BroadcastOnDirty();
        }
    }
}
