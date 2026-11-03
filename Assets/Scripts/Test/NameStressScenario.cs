using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.GameplayEffectComponents;
using Core.AbilitySystem.Tag;
using Core.Common;
using Game.Character;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Test
{
    /// <summary>
    /// InternedName vs IndexedName 인게임 A/B 2차 — 빈 씬에서 ASC만 세워 GA 발동·GE 적용/제거·태그 질의를 ASC 수 사다리(10 → 100 → 1000)로 키우며
    /// <c>Tag.*</c> ProfilerMarker 합계를 ProfilerRecorder로 프레임마다 모은다. 프로파일러가 켜져 있어야 마커가 기록된다(에디터는 ProfilerDriver.enabled).
    /// GE·GA 정의는 메모리에 만든다(에셋 파일 없음). Play 중인 빈 씬에 이 컴포넌트만 붙이면 단계마다 <c>[Name Stress]</c> 줄이 찍힌다.
    /// </summary>
    public sealed class NameStressScenario : MonoBehaviour
    {
        private const int WarmupFrames = 30;
        private const int MeasuredFrames = 60;
        private const int SettleFrames = 10;
        // 다음 프레임에는 만료돼 매 프레임 발동이 성공하도록 한 프레임보다 짧게 — 만료 제거(Tick 경로)도 매 프레임 같이 잰다.
        private const float CooldownDuration = 0.02f;
        // 버프는 명시 제거 경로만 타도록 자연 만료보다 훨씬 길게.
        private const float BuffDuration = 100f;
        private const string CooldownTagName = "Cooldown.NormalAttack";
        private const string OwnedTagName = "State.Combat";
        private const string BlockedTagName = "State.Dashing";
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] AscCounts = { 1000, 3000 };
        // 레지스트리 태그 중 쿨다운·발동 소유·발동 차단에 쓰는 셋을 뺀 전부 — 컨테이너당 태그 수를 키워 이름 비교 횟수를 늘린다.
        private static readonly string[] BuffTagNames =
        {
            "A.B.C", "A.C", "ASD",
            "Ability.ActivateFail.CanActivateAbility", "Ability.ActivateFail.Cooldown", "Ability.ActivateFail.Cost", "Ability.ActivateFail.TagsBlocked", "Ability.ActivateFail.TagsMissing",
            "Ability.Aim", "Ability.Sprint",
            "Cooldown.Dummy.Attack", "Cooldown.Pickup.Respawn", "Cooldown.Skill.Dash", "Cooldown.Skill.Wave",
            "Gameplay.Zone.Head", "Movement.Mode.Falling", "Movement.Mode.Walking",
            "State.Aiming", "State.Healing", "State.Jumping", "State.Sprinting",
            "Status.Buff.Empowered", "Status.Buff.Shielded", "Status.Debuff.Burning", "Status.Debuff.Marked", "Status.Debuff.Poisoned",
            "Test.ASD.A", "Test.ASD.s", "Test.ModifierTag.Registry.Stunned",
        };

        private readonly List<ScriptableObject> _created = new();
        private readonly List<GameObject> _actors = new();
        private readonly List<AbilitySystemComponent> _ascs = new();
        private readonly List<GameplayAbilitySpecHandle> _abilityHandles = new();
        private readonly List<ActiveGameplayEffectHandle> _buffHandles = new();
        private GameplayEffectAsset _buffEffect;
        private GameplayAbilityAsset _abilityAsset;
        private GameplayTag[] _queryTags;
        private ProfilerRecorder[] _markerRecorders;
        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _gcAllocRecorder;
        private int _queryHits;
        private int _activations;

        private IEnumerator Start()
        {
            // 에디터가 포커스를 잃어도 프레임이 흐르게 — 측정 중 유저가 다른 창으로 가도 결과가 늘어지지 않는다.
            Application.runInBackground = true;

            GameplayTag cooldownTag = GameplayTag.RequestGameplayTag(CooldownTagName);
            GameplayTag ownedTag = GameplayTag.RequestGameplayTag(OwnedTagName);
            GameplayTag blockedTag = GameplayTag.RequestGameplayTag(BlockedTagName);
            _queryTags = new GameplayTag[BuffTagNames.Length];
            bool tagsValid = cooldownTag.IsValid() && ownedTag.IsValid() && blockedTag.IsValid();
            for (int i = 0; i < BuffTagNames.Length; i++)
            {
                _queryTags[i] = GameplayTag.RequestGameplayTag(BuffTagNames[i]);
                tagsValid &= _queryTags[i].IsValid();
            }

            if (!tagsValid)
            {
                Debug.LogError("[Name Stress] 레지스트리 태그를 찾지 못해 중단.");
                yield break;
            }

            _buffEffect = MakeBuffEffect(_queryTags);
            GameplayEffectAsset costEffect = MakeInstantEffect(CharacterAttributeSet.Stamina, -1f);
            GameplayEffectAsset cooldownEffect = MakeCooldownEffect(cooldownTag);
            GameplayEffectAsset activationEffect = MakeInstantEffect(CharacterAttributeSet.Speed, 1f);
            _abilityAsset = MakeAbilityAsset(costEffect, cooldownEffect, activationEffect, ownedTag, blockedTag);

            StartRecorders();
            Debug.Log($"[Name Stress] start mode={InternedName.ImplementationName} profilerEnabled={Profiler.enabled} markers={_markerRecorders.Length} ladder={string.Join("/", AscCounts)} warmup={WarmupFrames} measured={MeasuredFrames}");

            foreach (int ascCount in AscCounts)
            {
                yield return BuildStage(ascCount);
                yield return RunStage($"warmup(discard) asc={ascCount}", ascCount, WarmupFrames, false);
                yield return RunStage($"x{ascCount}", ascCount, MeasuredFrames, true);
                Teardown();
                yield return WaitFrames(SettleFrames);
            }

            StopRecorders();
            foreach (ScriptableObject created in _created)
            {
                Destroy(created);
            }

            Debug.Log($"[Name Stress] done mode={InternedName.ImplementationName} activations={_activations} queryHits={_queryHits}");
        }

        // ASC·어트리뷰트·GiveAbility를 한 프레임에 전부 만든다 — 로딩 경로(스펙 복제·태그 컨테이너 생성)를 한 프레임의 마커 합계로 본다.
        private IEnumerator BuildStage(int ascCount)
        {
            int firstFrame = Time.frameCount;
            double startMs = Time.realtimeSinceStartupAsDouble * 1000.0;
            for (int i = 0; i < ascCount; i++)
            {
                var actor = new GameObject($"StressActor_{i}");
                var asc = actor.AddComponent<AbilitySystemComponent>();
                asc.AddSpawnedAttribute(new CharacterAttributeSet { stamina = new AttributeData(1e9f, 1e9f), speed = new AttributeData(0f, 0f) });
                GameplayAbilitySpecHandle handle = asc.GiveAbility(new GameplayAbilitySpec(_abilityAsset));
                _actors.Add(actor);
                _ascs.Add(asc);
                _abilityHandles.Add(handle);
                _buffHandles.Add(default);
            }
            double wallMs = Time.realtimeSinceStartupAsDouble * 1000.0 - startMs;

            yield return null;
            Debug.Log($"[Name Stress] build asc={ascCount} mode={InternedName.ImplementationName} frame={firstFrame} wall={wallMs:F2} ms | {ReadFrameLine()}");
        }

        private IEnumerator RunStage(string stage, int ascCount, int frames, bool report)
        {
            int markerCount = _markerRecorders.Length;
            var markerNs = new List<double>[markerCount];
            var markerCalls = new List<long>[markerCount];
            for (int m = 0; m < markerCount; m++)
            {
                markerNs[m] = new List<double>(frames);
                markerCalls[m] = new List<long>(frames);
            }
            var mainThreadNs = new List<double>(frames);
            var gcBytes = new List<double>(frames);
            var deltaMs = new List<double>(frames);
            int firstFrame = Time.frameCount;

            for (int f = 0; f < frames; f++)
            {
                DoFrameWork();
                // 코루틴은 Update 뒤에 재개되므로 다음 재개 때 읽는 LastValue는 방금 일을 넣은 그 프레임의 합계다.
                yield return null;
                for (int m = 0; m < markerCount; m++)
                {
                    ProfilerRecorderSample sample = ReadLast(_markerRecorders[m]);
                    markerNs[m].Add(sample.Value);
                    markerCalls[m].Add(sample.Count);
                }
                mainThreadNs.Add(ReadLast(_mainThreadRecorder).Value);
                gcBytes.Add(ReadLast(_gcAllocRecorder).Value);
                deltaMs.Add(Time.unscaledDeltaTime * 1000.0);
            }
            int lastFrame = Time.frameCount - 1;

            if (!report)
            {
                yield break;
            }

            Debug.Log($"[Name Stress] stage={stage} asc={ascCount} mode={InternedName.ImplementationName} frames={frames} frameRange={firstFrame}-{lastFrame} | mainThread med={Median(mainThreadNs) / 1e6:F3} ms mean={Mean(mainThreadNs) / 1e6:F3} ms | delta med={Median(deltaMs):F3} ms | gcAlloc mean={Mean(gcBytes):F0} B/frame");
            for (int m = 0; m < markerCount; m++)
            {
                double medMs = Median(markerNs[m]) / 1e6;
                double meanMs = Mean(markerNs[m]) / 1e6;
                double callsPerFrame = MeanLong(markerCalls[m]);
                double usPerCall = callsPerFrame > 0 ? meanMs * 1000.0 / callsPerFrame : 0.0;
                Debug.Log($"[Name Stress]   stage={stage} mode={InternedName.ImplementationName} marker={TagProfiler.GetMarkerName((TagProfileSection)m)} calls/frame={callsPerFrame:F1} med={medMs:F4} ms/frame mean={meanMs:F4} ms/frame us/call={usPerCall:F2}");
            }
        }

        // 실제 플레이에서 한 캐릭터가 프레임마다 하는 일의 과장판: 버프 교체(명시 제거 + 적용) → 어빌리티 발동(코스트·쿨다운·효과 GE) → 상태 질의.
        private void DoFrameWork()
        {
            for (int i = 0; i < _ascs.Count; i++)
            {
                AbilitySystemComponent asc = _ascs[i];
                if (_buffHandles[i].IsValid)
                {
                    asc.RemoveActiveGameplayEffect(_buffHandles[i]);
                }
                _buffHandles[i] = asc.ApplyGameplayEffectToSelf(_buffEffect);

                if (asc.TryActivateAbility(_abilityHandles[i]))
                {
                    _activations++;
                }

                for (int q = 0; q < _queryTags.Length; q++)
                {
                    if (asc.HasMatchingGameplayTag(_queryTags[q]))
                    {
                        _queryHits++;
                    }
                }
            }
        }

        private void Teardown()
        {
            foreach (GameObject actor in _actors)
            {
                Destroy(actor);
            }
            _actors.Clear();
            _ascs.Clear();
            _abilityHandles.Clear();
            _buffHandles.Clear();
        }

        private void StartRecorders()
        {
            int sectionCount = (int)TagProfileSection.Count;
            _markerRecorders = new ProfilerRecorder[sectionCount];
            for (int i = 0; i < sectionCount; i++)
            {
                _markerRecorders[i] = new ProfilerRecorder(ProfilerCategory.Scripts, TagProfiler.GetMarkerName((TagProfileSection)i), 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
                _markerRecorders[i].Start();
            }
            _mainThreadRecorder = new ProfilerRecorder(ProfilerCategory.Internal, "Main Thread", 1, ProfilerRecorderOptions.Default);
            _mainThreadRecorder.Start();
            _gcAllocRecorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC Allocated In Frame", 1, ProfilerRecorderOptions.Default);
            _gcAllocRecorder.Start();
        }

        private void StopRecorders()
        {
            foreach (ProfilerRecorder recorder in _markerRecorders)
            {
                recorder.Dispose();
            }
            _mainThreadRecorder.Dispose();
            _gcAllocRecorder.Dispose();
        }

        private string ReadFrameLine()
        {
            var parts = new List<string>();
            for (int m = 0; m < _markerRecorders.Length; m++)
            {
                ProfilerRecorderSample sample = ReadLast(_markerRecorders[m]);
                parts.Add($"{TagProfiler.GetMarkerName((TagProfileSection)m)} calls={sample.Count} ms={sample.Value / 1e6:F3}");
            }
            parts.Add($"mainThread={ReadLast(_mainThreadRecorder).Value / 1e6:F3} ms");
            parts.Add($"gcAlloc={ReadLast(_gcAllocRecorder).Value} B");
            return string.Join(" | ", parts);
        }

        private static ProfilerRecorderSample ReadLast(ProfilerRecorder recorder)
        {
            return recorder.Count > 0 ? recorder.GetSample(recorder.Count - 1) : default;
        }

        private IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0.0;
            }

            var sorted = new List<double>(values);
            sorted.Sort();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) * 0.5;
        }

        private static double Mean(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0.0;
            }

            double sum = 0.0;
            foreach (double value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }

        private static double MeanLong(List<long> values)
        {
            if (values.Count == 0)
            {
                return 0.0;
            }

            double sum = 0.0;
            foreach (long value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }

        private GameplayEffectAsset MakeInstantEffect(GameplayAttributeHandle attribute, float magnitude)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", GameplayEffectType.Instant);

            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);

            object modifierBoxed = default(GameplayModifier);
            SetPrivateField(modifierBoxed, "attribute", new GameplayAttribute(attribute));
            SetPrivateField(modifierBoxed, "operation", GameplayModifierOperation.AddBase);
            SetPrivateField(modifierBoxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);

            SetPrivateField(ge, "modifiers", new List<GameplayModifier> { (GameplayModifier)modifierBoxed });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private GameplayEffectAsset MakeCooldownEffect(GameplayTag cooldownTag)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", GameplayEffectType.Duration);
            SetPrivateField(ge, "duration", CooldownDuration);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier>());

            var grant = new TargetTagsGameplayEffectComponent();
            grant.GetConfiguredTargetTagChanges().AddTag(cooldownTag);
            SetPrivateField(ge, "gameplayEffectComponents", new List<GameplayEffectComponent> { grant });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private GameplayEffectAsset MakeBuffEffect(GameplayTag[] grantedTags)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", GameplayEffectType.Duration);
            SetPrivateField(ge, "duration", BuffDuration);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier>());

            var grant = new TargetTagsGameplayEffectComponent();
            InheritedTagContainer added = grant.GetConfiguredTargetTagChanges();
            foreach (GameplayTag tag in grantedTags)
            {
                added.AddTag(tag);
            }
            SetPrivateField(ge, "gameplayEffectComponents", new List<GameplayEffectComponent> { grant });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private GameplayAbilityAsset MakeAbilityAsset(GameplayEffectAsset costEffect, GameplayEffectAsset cooldownEffect, GameplayEffectAsset activationEffect, GameplayTag ownedTag, GameplayTag blockedTag)
        {
            var ability = new TestGameplayAbility();
            SetPrivateField(ability, "costGameplayEffect", costEffect);
            SetPrivateField(ability, "cooldownGameplayEffect", cooldownEffect);
            SetPrivateField(ability, "_testEffect", activationEffect);

            var owned = new GameplayTagContainer();
            owned.AddTag(ownedTag);
            SetPrivateField(ability, "activationOwnedTags", owned);
            var blocked = new GameplayTagContainer();
            blocked.AddTag(blockedTag);
            SetPrivateField(ability, "activationBlockedTags", blocked);

            var asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            SetPrivateField(asset, "abilityTemplate", ability);

            _created.Add(asset);
            return asset;
        }

        // 기반 클래스의 private 필드(GameplayAbility의 코스트·쿨다운·태그 칸)도 찾도록 상속 계층을 올라간다.
        private static void SetPrivateField(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, NonPublicInstance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            throw new InvalidOperationException($"필드 '{name}'을 {target.GetType().Name}에서 찾지 못함.");
        }
    }
}
