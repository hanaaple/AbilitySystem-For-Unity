using System.Collections;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using Core.Common;
using Game;
using Game.Abilities;
using Game.Character;
using UnityEngine;

namespace Test
{
    // InternedName vs IndexedName 인게임 A/B. 로딩 → Idle → 어빌리티 발동 → GE 적용 → GE 제거 → 스트레스 순으로 단계마다 TagProfiler 집계를 찍는다.
    // 같은 씬·같은 순서로 두 define(TAG_NAME_INDEXED 유무)에서 각각 돌려 비교한다.
    // 입력 장치는 거치지 않고 ASC API를 직접 부른다 — 측정 대상(태그 연산)은 전부 그 아래에 있다.
    public class NameProfilingScenario : MonoBehaviour
    {
        private const float PlayerWaitSeconds = 10f;
        private const int WarmupFrames = 120;
        private const float IdleSeconds = 5f;
        private const float ExpirySeconds = 3f;
        private const int AbilityIterations = 20;
        private const int DamagePerDummy = 50;
        private const int DamagePerFrame = 5;
        private const int StressMultiplier = 10;

        [SerializeField] private GameplayEffectAsset damageEffect;
        [SerializeField] private GameplayEffectAsset[] durationEffects;

        private AbilitySystemComponent _playerAsc;
        private readonly List<AbilitySystemComponent> _dummyAscs = new();

        private int _stageFrames;
        private double _stageStartSeconds;
        private double _stageFrameSeconds;

        private IEnumerator Start()
        {
            // 플레이어가 씬 로드 뒤에 생길 수 있어 잠시 기다린다.
            PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
            double waitUntil = Time.realtimeSinceStartupAsDouble + PlayerWaitSeconds;
            while (player == null && Time.realtimeSinceStartupAsDouble < waitUntil)
            {
                yield return null;
                player = FindFirstObjectByType<PlayerCharacter>();
            }

            _playerAsc = player != null ? player.GetComponent<AbilitySystemComponent>() : null;
            foreach (TrainingDummy dummy in FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None))
            {
                _dummyAscs.Add(dummy.AbilitySystemComponent);
            }

            if (_playerAsc == null || _dummyAscs.Count == 0 || damageEffect == null)
            {
                Debug.LogError($"[Name Profiling] 준비 실패 — player {_playerAsc != null}, dummies {_dummyAscs.Count}, damageEffect {damageEffect != null}");
                yield break;
            }

            Debug.Log($"[Name Profiling] start mode={InternedName.ImplementationName} profiler={(TagProfiler.IsEnabled ? "on" : "off")} dummies={_dummyAscs.Count} durationEffects={(durationEffects != null ? durationEffects.Length : 0)}");

            // 1. 로딩 — 도메인 시작부터 첫 Start까지 쌓인 집계(트리 구성·에셋 역직렬화·native 태그 초기화)
            EndStage("1 Loading", Time.realtimeSinceStartupAsDouble * 1000.0, 0.0);

            // Play 진입 직후 프레임은 셰이더 컴파일·스폰 초기화로 수십 ms~수 초가 걸린다 — 버리고 Idle부터 잰다.
            BeginStage();
            yield return WaitFrames(WarmupFrames);
            EndStage("0 Warmup(discard)");

            yield return IdleStage("2a Idle");

            var playerHandles = new List<(AbilitySystemComponent asc, ActiveGameplayEffectHandle handle)>();
            ApplyDurationEffects(_playerAsc, playerHandles);
            yield return IdleStage("2b Idle+GE");
            RemoveAll(playerHandles);

            yield return AbilityStage("3a Ability", AbilityIterations);

            var handles = new List<(AbilitySystemComponent asc, ActiveGameplayEffectHandle handle)>();
            yield return EffectApplyStage("3b EffectApply", DamagePerDummy, handles);
            yield return EffectRemoveStage("3c EffectRemove", handles);

            yield return AbilityStage("4a Stress Ability x10", AbilityIterations * StressMultiplier);
            yield return EffectApplyStage("4b Stress EffectApply x10", DamagePerDummy * StressMultiplier, handles);
            yield return EffectRemoveStage("4c Stress EffectRemove", handles);

            Debug.Log($"[Name Profiling] done mode={InternedName.ImplementationName}");
        }

        private IEnumerator IdleStage(string stage)
        {
            BeginStage();
            yield return WaitSeconds(IdleSeconds);
            EndStage(stage);
        }

        // 조준 → 평타 → 점프 → 달리기를 한 묶음으로. 쿨다운에 막혀 실패하는 시도도 CanActivateAbility(태그 검사)까지는 돈다.
        private IEnumerator AbilityStage(string stage, int iterations)
        {
            BeginStage();
            for (int i = 0; i < iterations; i++)
            {
                yield return PressAndRelease(AbilityInputID.Aim);
                yield return PressAndRelease(AbilityInputID.NormalAttack);
                yield return PressAndRelease(AbilityInputID.Jump);
                yield return PressAndRelease(AbilityInputID.Sprint);
                yield return WaitFrames(1);
            }
            EndStage(stage);
        }

        // 피해 GE(DamageExecution)를 허수아비마다 반복 적용 + Duration GE를 걸어 둔다. 적용·태그 갱신·이벤트·aggregator 조건 검사가 모두 포함된다.
        private IEnumerator EffectApplyStage(string stage, int damagePerDummy, List<(AbilitySystemComponent asc, ActiveGameplayEffectHandle handle)> handles)
        {
            BeginStage();
            foreach (AbilitySystemComponent dummyAsc in _dummyAscs)
            {
                ApplyDurationEffects(dummyAsc, handles);
            }

            int applied = 0;
            while (applied < damagePerDummy)
            {
                int batch = Mathf.Min(DamagePerFrame, damagePerDummy - applied);
                for (int i = 0; i < batch; i++)
                {
                    foreach (AbilitySystemComponent dummyAsc in _dummyAscs)
                    {
                        _playerAsc.ApplyGameplayEffectToTarget(damageEffect, dummyAsc);
                    }
                }
                applied += batch;
                yield return WaitFrames(1);
            }
            EndStage(stage);
        }

        // 명시 제거(프레임당 하나) 뒤 남은 Duration GE가 자연 만료되도록 기다린다 — Tick 경로의 제거도 같은 단계에 잡힌다.
        private IEnumerator EffectRemoveStage(string stage, List<(AbilitySystemComponent asc, ActiveGameplayEffectHandle handle)> handles)
        {
            BeginStage();
            foreach ((AbilitySystemComponent asc, ActiveGameplayEffectHandle handle) in handles)
            {
                asc.RemoveActiveGameplayEffect(handle);
                yield return WaitFrames(1);
            }
            handles.Clear();
            yield return WaitSeconds(ExpirySeconds);
            EndStage(stage);
        }

        private IEnumerator PressAndRelease(AbilityInputID inputID)
        {
            _playerAsc.AbilityLocalInputPressed((int)inputID);
            yield return WaitFrames(1);
            _playerAsc.AbilityLocalInputReleased((int)inputID);
            yield return WaitFrames(1);
        }

        private void ApplyDurationEffects(AbilitySystemComponent asc, List<(AbilitySystemComponent asc, ActiveGameplayEffectHandle handle)> handles)
        {
            if (durationEffects == null)
            {
                return;
            }

            foreach (GameplayEffectAsset effect in durationEffects)
            {
                if (effect == null)
                {
                    continue;
                }

                ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectToSelf(effect);
                if (handle.IsValid)
                {
                    handles.Add((asc, handle));
                }
            }
        }

        private static void RemoveAll(List<(AbilitySystemComponent asc, ActiveGameplayEffectHandle handle)> handles)
        {
            foreach ((AbilitySystemComponent asc, ActiveGameplayEffectHandle handle) in handles)
            {
                asc.RemoveActiveGameplayEffect(handle);
            }
            handles.Clear();
        }

        private IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                _stageFrames++;
                _stageFrameSeconds += Time.unscaledDeltaTime;
            }
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                yield return WaitFrames(1);
            }
        }

        private void BeginStage()
        {
            _stageFrames = 0;
            _stageFrameSeconds = 0.0;
            _stageStartSeconds = Time.realtimeSinceStartupAsDouble;
        }

        private void EndStage(string stage)
        {
            double wallMs = (Time.realtimeSinceStartupAsDouble - _stageStartSeconds) * 1000.0;
            double averageFrameMs = _stageFrames > 0 ? _stageFrameSeconds * 1000.0 / _stageFrames : 0.0;
            EndStage(stage, wallMs, averageFrameMs);
        }

        private void EndStage(string stage, double wallMs, double averageFrameMs)
        {
            Debug.Log(TagProfiler.Report(stage, InternedName.ImplementationName, _stageFrames, wallMs, averageFrameMs));
            TagProfiler.Reset();
        }
    }
}
