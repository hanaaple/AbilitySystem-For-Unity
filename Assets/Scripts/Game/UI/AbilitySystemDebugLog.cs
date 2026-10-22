using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// ASC 하나의 GAS 이벤트(GE 적용·주기 실행·제거, GA 발동·실패·종료)를 최근 몇 줄로 모은다.
    /// 같은 줄이 최근 몇 줄 안에 다시 오면 새 줄을 만들지 않고 ×N으로 합쳐 맨 위로 올린다 — 연사·매 프레임 쿨다운 실패가 로그를 밀어내지 않게.
    /// </summary>
    public sealed class AbilitySystemDebugLog : IDisposable
    {
        private const int MaxEntries = 10;
        private const int MergeWindow = 4;

        private readonly AbilitySystemComponent _asc;
        private readonly List<Entry> _entries = new();

        public AbilitySystemDebugLog(AbilitySystemComponent asc)
        {
            _asc = asc;
            _asc.OnGameplayEffectAppliedToSelf += OnEffectApplied;
            _asc.OnPeriodicGameplayEffectExecuteOnSelf += OnEffectPeriodic;
            _asc.OnAnyGameplayEffectRemoved += OnEffectRemoved;
            _asc.OnAbilityActivated += OnAbilityActivated;
            _asc.OnAbilityFailed += OnAbilityFailed;
            _asc.OnAbilityEndedWithData += OnAbilityEnded;
        }

        /// <summary>최신이 앞.</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        public void Dispose()
        {
            if (_asc == null)
            {
                return;
            }

            _asc.OnGameplayEffectAppliedToSelf -= OnEffectApplied;
            _asc.OnPeriodicGameplayEffectExecuteOnSelf -= OnEffectPeriodic;
            _asc.OnAnyGameplayEffectRemoved -= OnEffectRemoved;
            _asc.OnAbilityActivated -= OnAbilityActivated;
            _asc.OnAbilityFailed -= OnAbilityFailed;
            _asc.OnAbilityEndedWithData -= OnAbilityEnded;
        }

        private void Add(string text)
        {
            int searchCount = Mathf.Min(MergeWindow, _entries.Count);
            for (int i = 0; i < searchCount; i++)
            {
                Entry entry = _entries[i];
                if (entry.Text != text)
                {
                    continue;
                }

                _entries.RemoveAt(i);
                entry.Count++;
                entry.Time = Time.time;
                _entries.Insert(0, entry);
                return;
            }

            _entries.Insert(0, new Entry { Text = text, Count = 1, Time = Time.time });
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }
        }

        private void OnEffectApplied(AbilitySystemComponent source, GameplayEffectSpec spec, ActiveGameplayEffectHandle handle)
        {
            // Instant는 활성 GE로 남지 않아 핸들이 무효다
            string kind = handle.IsValid ? "<color=#7CFC9A>GE+</color>" : "<color=#7CFC9A>GE!</color>";
            Add($"{kind} {EffectName(spec)}{FromText(source)}");
        }

        private void OnEffectPeriodic(AbilitySystemComponent source, GameplayEffectSpec spec, ActiveGameplayEffectHandle handle)
        {
            Add($"<color=#B8E68A>GE~</color> {EffectName(spec)}");
        }

        private void OnEffectRemoved(ActiveGameplayEffect effect)
        {
            Add($"<color=#9AA4B0>GE-</color> {EffectName(effect.Spec)}");
        }

        private void OnAbilityActivated(GameplayAbility ability)
        {
            Add($"<color=#7CC8FF>GA></color> {AbilityName(ability)}");
        }

        private void OnAbilityFailed(GameplayAbility ability, GameplayTagContainer reasons)
        {
            Add($"<color=#FF7A6E>GA x</color> {AbilityName(ability)}  <color=#FF9F94>{TagsText(reasons)}</color>");
        }

        private void OnAbilityEnded(AbilityEndedData data)
        {
            GameplayAbilitySpec spec = _asc.FindAbilitySpecFromHandle(data.AbilitySpecHandle);
            string name = spec != null && spec.Ability != null ? spec.Ability.name : AbilityName(data.AbilityThatEnded);
            Add($"<color=#9AA4B0>GA.</color> {name}{(data.WasCancelled ? " (cancelled)" : "")}");
        }

        private string FromText(AbilitySystemComponent source)
        {
            return source != null && source != _asc ? $"  <color=#9AA4B0>from {source.name}</color>" : string.Empty;
        }

        // 실행 중인 GA는 에셋을 들고 있지 않아, 그 인스턴스를 가진 Spec에서 에셋 이름을 찾는다
        private string AbilityName(GameplayAbility ability)
        {
            foreach (GameplayAbilitySpec spec in _asc.GetActivatableAbilities())
            {
                if (spec.Ability == null)
                {
                    continue;
                }

                foreach (GameplayAbility instance in spec.GetAbilityInstances())
                {
                    if (instance == ability)
                    {
                        return spec.Ability.name;
                    }
                }
            }

            return ability != null ? ability.GetType().Name : "(null)";
        }

        public static string EffectName(GameplayEffectSpec spec)
        {
            return spec != null && spec.Definition != null ? spec.Definition.name : "(null)";
        }

        public static string TagsText(GameplayTagContainer tags)
        {
            if (tags == null)
            {
                return string.Empty;
            }

            IReadOnlyList<GameplayTag> list = tags.GetGameplayTagArray();
            var names = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                names[i] = list[i].ToString();
            }

            return string.Join(", ", names);
        }

        public sealed class Entry
        {
            public string Text;
            public int Count;
            public float Time;
        }
    }
}
