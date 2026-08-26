using Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// PlayMode에서 <b>실제 GameplayEffect 에셋</b>을 실제 ASC 라이프사이클(Awake 초기화 + Apply/Remove 파이프라인)로
    /// 돌려 Aggregator 반응성을 검증하는 씬 컴포넌트. 에디터 전용 셀프체크(AggregatorSelfCheck)가 리플렉션으로 조립한
    /// in-memory 객체를 Edit 모드에서 확인하는 것과 달리, 이 컴포넌트는 직렬화된 .asset을 인스펙터 참조로 받아
    /// <see cref="AbilitySystemComponent.ApplyGameplayEffectToSelf(GameplayEffectAsset, GameplayEffectContextHandle, float)"/>로
    /// 적용한다 → "실제 플레이 중 실제 에셋이 올바른 결과를 내는가"를 본다.
    ///
    /// <para>배선·에셋 생성은 <c>Tools ▸ Ability System ▸ Setup PlayMode Test</c> 메뉴가 자동으로 한다.
    /// Play를 누르면 <see cref="Start"/>가 전 케이스를 돌리고 <c>[PlayMode Self-Check] passed N / failed N</c>을 로그한다.</para>
    /// </summary>
    [RequireComponent(typeof(AbilitySystemComponent))]
    public sealed class AggregatorPlayModeTest : MonoBehaviour
    {
        [Tooltip("Health=100 / Speed=0 으로 초기화하는 정의 에셋. ASC.attributeInitData와 동일 에셋을 재주입(TryAdd라 안전).")]
        [SerializeField] private AttributeDefinitionAsset attributeInitData;

        [Tooltip("Infinite · Health AddBase +100.")]
        [SerializeField] private GameplayEffectAsset geHealthBoost;

        [Tooltip("Infinite · Speed = Health.current(Source, non-snapshot) * 0.5. Health 변경을 라이브 추종해야 한다.")]
        [SerializeField] private GameplayEffectAsset geSpeedFromHealthLive;

        [Tooltip("Infinite · Speed = Health.current(Source, snapshot) * 0.5. 캡처 시점 값으로 고정돼야 한다.")]
        [SerializeField] private GameplayEffectAsset geSpeedFromHealthSnapshot;

        [Tooltip("Instant · Health AddBase +30 (BaseValue 영구 변경).")]
        [SerializeField] private GameplayEffectAsset geInstantHeal;

        private AbilitySystemComponent _asc;
        private int _passed;
        private int _failed;

        private void Start()
        {
            _asc = GetComponent<AbilitySystemComponent>();

            // ASC.Awake가 attributeInitData로 이미 초기화했지만, 인스펙터에 안 꽂혀 있어도 돌게 재주입(TryAdd라 중복 무해).
            _asc.AddSet(attributeInitData);

            if (!Preconditions())
            {
                Debug.LogError("[PlayMode Self-Check] 선행조건 실패 — 에셋 배선 확인(Setup 메뉴 재실행).");
                return;
            }

            RunInitCheck();
            RunLiveReevalCheck();
            RunSnapshotFixedCheck();
            RunInstantBaseCheck();

            Debug.Log($"[PlayMode Self-Check] passed {_passed} / failed {_failed}");
        }

        private bool Preconditions()
        {
            bool ok = attributeInitData != null
                      && geHealthBoost != null
                      && geSpeedFromHealthLive != null
                      && geSpeedFromHealthSnapshot != null
                      && geInstantHeal != null
                      && _asc.HasAttributeSetForAttribute(CharacterAttributeSet.Health)
                      && _asc.HasAttributeSetForAttribute(CharacterAttributeSet.Speed);
            return ok;
        }

        // 케이스마다 깨끗한 출발점: Health=100 / Speed=0. (Unity의 Reset() 메시지와 충돌 피하려 이름 분리.)
        private void ResetAttributes()
        {
            _asc.SetAttributeBaseValue(CharacterAttributeSet.Health, 100f);
            _asc.SetAttributeBaseValue(CharacterAttributeSet.Speed, 0f);
        }

        // A. 초기값이 실제 에셋 초기화로 잡히는가.
        private void RunInitCheck()
        {
            ResetAttributes();
            Check("초기 Health == 100 (attributeInitData)", Current(CharacterAttributeSet.Health), 100f);
            Check("초기 Speed == 0", Current(CharacterAttributeSet.Speed), 0f);
        }

        // B. non-snapshot 라이브 재평가 (핵심): Health 변경이 Speed로 즉시 전파.
        private void RunLiveReevalCheck()
        {
            ResetAttributes();
            ActiveGameplayEffectHandle speedHandle = _asc.ApplyGameplayEffectToSelf(geSpeedFromHealthLive);
            Check("[Live] 적용 직후 Speed == Health(100)*0.5 = 50", Current(CharacterAttributeSet.Speed), 50f);

            ActiveGameplayEffectHandle boostHandle = _asc.ApplyGameplayEffectToSelf(geHealthBoost);
            Check("[Live] Health 100→200", Current(CharacterAttributeSet.Health), 200f);
            Check("[Live] Health 변경 → Speed 라이브 재평가 200*0.5 = 100", Current(CharacterAttributeSet.Speed), 100f);

            _asc.RemoveActiveGameplayEffect(boostHandle);
            Check("[Live] boost 제거 → Health 100 복귀", Current(CharacterAttributeSet.Health), 100f);
            Check("[Live] boost 제거 → Speed 50 복귀", Current(CharacterAttributeSet.Speed), 50f);

            _asc.RemoveActiveGameplayEffect(speedHandle);
            Check("[Live] speed GE 제거 → Speed 0", Current(CharacterAttributeSet.Speed), 0f);
        }

        // C. snapshot 회귀 없음: 캡처 시점 값으로 고정, Health가 바뀌어도 재평가 안 됨.
        private void RunSnapshotFixedCheck()
        {
            ResetAttributes();
            ActiveGameplayEffectHandle snapHandle = _asc.ApplyGameplayEffectToSelf(geSpeedFromHealthSnapshot);
            Check("[Snapshot] 적용 직후 Speed == 50 (캡처 100*0.5)", Current(CharacterAttributeSet.Speed), 50f);

            ActiveGameplayEffectHandle boostHandle = _asc.ApplyGameplayEffectToSelf(geHealthBoost);
            Check("[Snapshot] Health 100→200", Current(CharacterAttributeSet.Health), 200f);
            Check("[Snapshot] Health 변경에도 Speed 50 고정(재평가 없음)", Current(CharacterAttributeSet.Speed), 50f);

            _asc.RemoveActiveGameplayEffect(boostHandle);
            _asc.RemoveActiveGameplayEffect(snapHandle);
            Check("[Snapshot] 정리 → Speed 0", Current(CharacterAttributeSet.Speed), 0f);
        }

        // D. Instant: BaseValue 영구 변경.
        private void RunInstantBaseCheck()
        {
            ResetAttributes();
            _asc.ApplyGameplayEffectToSelf(geInstantHeal);
            Check("[Instant] AddBase +30 → Base 130 (영구)", Base(CharacterAttributeSet.Health), 130f);
            Check("[Instant] Instant 후 Current도 130", Current(CharacterAttributeSet.Health), 130f);
            ResetAttributes();
        }

        private float Current(GameplayAttributeHandle handle) => _asc.GetAttributeCurrentValue(handle);
        private float Base(GameplayAttributeHandle handle) => _asc.GetAttributeBaseValue(handle);

        private void Check(string label, float actual, float expected)
        {
            if (Mathf.Abs(actual - expected) < 0.001f)
            {
                _passed++;
                Debug.Log($"[PlayMode PASS] {label} (={actual})");
            }
            else
            {
                _failed++;
                Debug.LogError($"[PlayMode FAIL] {label} — expected {expected}, got {actual}");
            }
        }
    }
}
