using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Game.Abilities;
using UnityEngine;

namespace Game.Character
{
    /// <summary>허수아비. 어트리뷰트 세트를 만들어 ASC에 등록하고 초기 스탯 GE를 건다. 공격 어빌리티가 있으면 쿨다운이 끝날 때마다 같은 패턴을 반복하고, 실드 어빌리티가 있으면 시작할 때와, 실드가 끝난 뒤 체력이 다 차면 다시 건다. 받은 피해(실드가 막은 양 포함)를 교전 단위로 집계하고, 한동안 안 맞으면 체력을 되돌린다.</summary>
    [RequireComponent(typeof(AbilitySystemComponent))]
    public class TrainingDummy : MonoBehaviour, IProjectileCaster, IDebugOverlayTarget
    {
        // 첫 타격 직후엔 교전 시간이 0에 가까워 DPS가 튀므로 나눌 시간에 하한을 둔다
        private const float MinCombatDuration = 1f;

        private readonly Queue<(float Time, float Damage)> _recentHits = new();

        [Tooltip("시작할 때 자신에게 적용하는 초기 스탯 GE(Instant, Override).")]
        [SerializeField] private GameplayEffectAsset defaultAttributes;

        [Header("Attack")]
        [Tooltip("비워 두면 맞기만 한다.")]
        [SerializeField] private GameplayAbilityAsset attackAbility;
        [Tooltip("투사체가 나갈 위치·방향.")]
        [SerializeField] private Transform castPoint;

        [Header("Shield")]
        [Tooltip("비워 두면 실드를 쓰지 않는다.")]
        [SerializeField] private GameplayAbilityAsset shieldAbility;
        [Tooltip("실드가 끝난(깨진) 뒤, 체력이 다 찬 다음 다시 걸 때까지(초).")]
        [Min(0f)] [SerializeField] private float shieldRecastDelay = 3f;

        [Tooltip("디버그 오버레이에서 겨눴을 때 보일 썸네일.")]
        [SerializeField] private Texture2D debugThumbnail;

        [Header("Damage Meter")]
        [Tooltip("마지막 피해 후 이만큼(초) 안 맞으면 체력을 최대로 되돌리고 교전을 끝낸다.")]
        [Min(0.1f)] [SerializeField] private float resetDelay = 3f;
        [Tooltip("초당 피해량을 이 시간(초) 동안 받은 피해의 평균으로 낸다.")]
        [Min(0.1f)] [SerializeField] private float dpsWindow = 3f;

        private AbilitySystemComponent _asc;
        private GameplayAbilitySpecHandle _attackHandle = GameplayAbilitySpecHandle.Invalid;
        private GameplayAbilitySpecHandle _shieldHandle = GameplayAbilitySpecHandle.Invalid;
        private float _shieldRecastTimer;
        private GameplayEffectSpec _lastDamageSpec;
        private int _lastDamageFrame = -1;
        private bool _inCombat;
        private float _combatStartTime;
        private float _lastHitTime;

        /// <summary>피해를 받을 때마다 그 피해량·피해를 준 GE와 함께 울린다. 한 타격이 실드와 체력을 함께 깎으면 같은 GE로 각각 한 번씩 울린다.</summary>
        public event Action<float, GameplayEffectSpec> OnDamaged;

        public AbilitySystemComponent AbilitySystemComponent => _asc;

        public string DebugName => name;

        public Texture2D DebugThumbnail => debugThumbnail;

        public AbilitySystemComponent DebugAbilitySystem => _asc;

        public void AppendDebugLines(List<string> lines)
        {
            lines.Add($"Damage last / DPS / total\t{LastDamage:0} / {DamagePerSecond:0} / {TotalDamage:0}");
            lines.Add("Attack\t" + (attackAbility != null ? attackAbility.name : "-"));
            lines.Add("Shield\t" + (shieldAbility != null ? shieldAbility.name : "-"));
        }

        /// <summary>마지막 타격의 피해량.</summary>
        public float LastDamage { get; private set; }

        /// <summary>현재(또는 마지막) 교전의 누적 피해량.</summary>
        public float TotalDamage { get; private set; }

        /// <summary>최근 dpsWindow초 동안 받은 피해의 초당 평균. 안 맞으면 시간이 지나며 0으로 떨어진다.</summary>
        public float DamagePerSecond
        {
            get
            {
                float now = Time.time;
                while (_recentHits.Count > 0 && now - _recentHits.Peek().Time > dpsWindow)
                {
                    _recentHits.Dequeue();
                }

                float sum = 0f;
                foreach ((float _, float damage) in _recentHits)
                {
                    sum += damage;
                }

                // 교전 초반엔 창이 다 차지 않았으므로 지난 시간으로 나눈다
                float duration = _inCombat ? Mathf.Clamp(now - _combatStartTime, MinCombatDuration, dpsWindow) : dpsWindow;
                return sum / duration;
            }
        }

        private void Awake()
        {
            _asc = GetComponent<AbilitySystemComponent>();
            _asc.AddSpawnedAttribute(new CharacterAttributeSet());
        }

        // GE 컨테이너가 ASC Awake에서 만들어지므로 Start에서 적용한다
        private void Start()
        {
            if (defaultAttributes != null)
            {
                _asc.ApplyGameplayEffectToSelf(defaultAttributes);
            }

            if (attackAbility != null && castPoint != null)
            {
                _attackHandle = _asc.GiveAbility(new GameplayAbilitySpec(attackAbility));
            }

            if (shieldAbility != null)
            {
                _shieldHandle = _asc.GiveAbility(new GameplayAbilitySpec(shieldAbility));
                // 시작하자마자 건다
                _shieldRecastTimer = shieldRecastDelay;
            }

            _asc.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Health).OnValueChange += OnDamageAttributeChanged;
            _asc.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Shield).OnValueChange += OnDamageAttributeChanged;
        }

        private void Update()
        {
            TickReset();
            TickAttack();
            TickShield();
        }

        private void OnDestroy()
        {
            if (_asc != null)
            {
                _asc.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Health).OnValueChange -= OnDamageAttributeChanged;
                _asc.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Shield).OnValueChange -= OnDamageAttributeChanged;
            }
        }

        // 허수아비는 정면 고정으로 쏜다
        public void RequestLaunch(Action<Vector3, Vector3> launch)
        {
            launch(castPoint.position, castPoint.position + castPoint.forward);
        }

        private void TickReset()
        {
            if (!_inCombat || Time.time - _lastHitTime < resetDelay)
            {
                return;
            }

            _inCombat = false;
            _asc.SetAttributeBaseValue(CharacterAttributeSet.Health, _asc.GetAttributeCurrentValue(CharacterAttributeSet.MaxHealth));
        }

        private void TickAttack()
        {
            if (!_attackHandle.IsValid)
            {
                return;
            }

            // 간격은 어빌리티의 쿨다운 GE(공격속도로 나뉨)가 정한다 — 쿨다운 중이면 발동이 막힌다
            _asc.TryActivateAbility(_attackHandle);
        }

        private void TickShield()
        {
            if (!_shieldHandle.IsValid)
            {
                return;
            }

            GameplayAbilitySpec spec = _asc.FindAbilitySpecFromHandle(_shieldHandle);
            // 체력이 다 찬 뒤에야 실드가 다시 차기 시작한다
            bool isHealthFull = _asc.GetAttributeCurrentValue(CharacterAttributeSet.Health) >= _asc.GetAttributeCurrentValue(CharacterAttributeSet.MaxHealth);
            if (spec == null || spec.IsActive || !isHealthFull)
            {
                _shieldRecastTimer = 0f;
                return;
            }

            _shieldRecastTimer += Time.deltaTime;
            if (_shieldRecastTimer < shieldRecastDelay)
            {
                return;
            }

            _shieldRecastTimer = 0f;
            _asc.TryActivateAbility(_shieldHandle);
        }

        // 한 번의 타격이 실드와 체력을 함께 깎으면 값 변경이 두 번 오므로, 같은 GE의 같은 프레임 변경은 한 타격으로 합친다
        private void RecordDamage(float damage, GameplayEffectSpec source)
        {
            // 누적·DPS는 다음 교전의 첫 타격까지 지난 교전 값을 보여 준다
            if (!_inCombat)
            {
                _inCombat = true;
                _combatStartTime = Time.time;
                TotalDamage = 0f;
            }

            bool isSameHit = source == _lastDamageSpec && Time.frameCount == _lastDamageFrame;
            _lastDamageSpec = source;
            _lastDamageFrame = Time.frameCount;

            _lastHitTime = Time.time;
            LastDamage = isSameHit ? LastDamage + damage : damage;
            TotalDamage += damage;
            _recentHits.Enqueue((Time.time, damage));
            OnDamaged?.Invoke(damage, source);
        }

        private void OnDamageAttributeChanged(OnAttributeChangeData data)
        {
            GameplayEffectModCallbackData modData = data.GEModData;
            if (modData == null)
            {
                return;
            }

            // 체력 하한에 잘리기 전의 수치로 센다 — 바닥 근처에서도 타격의 원래 피해가 보이게
            GameplayModifierEvaluatedData evaluatedData = modData.EvaluatedData;
            if (evaluatedData.Operation != GameplayModifierOperation.AddBase || evaluatedData.Magnitude >= 0f)
            {
                return;
            }

            RecordDamage(-evaluatedData.Magnitude, modData.EffectSpec);
        }
    }
}
