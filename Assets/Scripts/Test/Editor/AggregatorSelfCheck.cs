using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 Aggregator를 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // A: AttributeAggregator.Evaluate 공식·mod 제거 / B: ASC.GetAttributeCurrentValue 라우팅
    // C: capture 스냅샷 vs 라이브 / D: 실제 GameplayEffectAsset을 ASC에 적용한 다수 케이스.
    // E: 런타임 dirty 라이브 재평가 / F: aggregator가 없는 어트리뷰트의 Base 변경.
    // G: 라이브 재평가가 같은 aggregator의 다른 GE mod를 건드리지 않음 / H: 주기 실행마다 magnitude 재계산.
    // I: 어트리뷰트 값 변경 이벤트(구독·해제, Old/New, 값이 같아도 울림, 원인 GEModData 유무).
    // J: PreAttributeBaseChange에서 고친 Base 값이 Base·Current·Post·값 변경 이벤트에 그대로 쓰임.
    // K: AttributeSet 훅(TestAtrributeSet) — Pre/PostGameplayEffectExecute(수치 변경·버림·meta Damage), Pre/PostAttributeChange(Current 클램프).
    public static class AggregatorSelfCheck
    {
        private const float Eps = 0.001f;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Tools/Ability System/Run Aggregator Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[Aggregator PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[Aggregator FAIL] {label}");
                }
            }

            RunAggregatorUnitChecks(Check);

            GameObject go = new GameObject("AggregatorSelfCheck_Temp");
            try
            {
                AbilitySystemComponent asc = go.AddComponent<AbilitySystemComponent>();
                EnsureInitialized(asc);
                AddHealthSet(asc, 100f);

                RunAscRoutingChecks(asc, Check);
                RunCaptureSnapshotChecks(asc, Check);
                RunAssetDrivenChecks(asc, Check);
                RunRuntimeDirtyReevalChecks(asc, Check);
                RunNoAggregatorBaseChecks(asc, Check);
                RunUpdateAggregatorModChecks(asc, Check);
                RunPeriodicRecalculationChecks(asc, Check);
                RunAttributeValueChangeEventChecks(asc, Check);
                RunPreAttributeBaseChangeClampChecks(asc, Check);
                RunAttributeSetHookChecks(asc, Check);
            }
            catch (Exception e)
            {
                Check($"ASC 검사 중 예외: {e.GetType().Name} — {e.Message}", false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            Debug.Log($"[Aggregator Self-Check] passed {passed} / failed {failed}");
        }

        // ── A. AttributeAggregator 단위 (순수) ──
        private static void RunAggregatorUnitChecks(Action<string, bool> check)
        {
            ActiveGameplayEffectHandle h1 = new ActiveGameplayEffectHandle(1, null);
            ActiveGameplayEffectHandle h2 = new ActiveGameplayEffectHandle(2, null);

            AttributeAggregator agg = new AttributeAggregator(100f);
            check("base만 → Evaluate == 100", Approx(agg.Evaluate(default), 100f));

            agg.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 20f);
            check("AddBase 20 → 120", Approx(agg.Evaluate(default), 120f));

            agg.AddAggregatorMod(h2, GameplayModifierOperation.MultiplyAdditive, 1.5f);
            check("MultiplyAdditive 1.5 → (100+20)*1.5 = 180", Approx(agg.Evaluate(default), 180f));

            agg.RemoveAggregatorMod(h2);
            check("MultiplyAdditive 제거 → 120 복귀", Approx(agg.Evaluate(default), 120f));

            agg.RemoveAggregatorMod(h1);
            check("AddBase 제거 → 100 복귀", Approx(agg.Evaluate(default), 100f));

            AttributeAggregator mix = new AttributeAggregator(10f);
            mix.AddAggregatorMod(h1, GameplayModifierOperation.MultiplyCompound, 2f);
            mix.AddAggregatorMod(h2, GameplayModifierOperation.AddFinal, 5f);
            check("MultiplyCompound 2 + AddFinal 5 → 10*2 + 5 = 25", Approx(mix.Evaluate(default), 25f));

            AttributeAggregator over = new AttributeAggregator(100f);
            over.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 50f);
            over.AddAggregatorMod(h2, GameplayModifierOperation.Override, 7f);
            check("Override → 다른 mod 무시하고 7", Approx(over.Evaluate(default), 7f));

            RunSnapshotUnitChecks(check);
        }

        // TakeSnapshotOf: base·mod를 캡처 시점으로 통째 고정, 원본 이후 변경과 무관.
        private static void RunSnapshotUnitChecks(Action<string, bool> check)
        {
            ActiveGameplayEffectHandle h1 = new ActiveGameplayEffectHandle(1, null);
            ActiveGameplayEffectHandle h2 = new ActiveGameplayEffectHandle(2, null);
            ActiveGameplayEffectHandle h3 = new ActiveGameplayEffectHandle(3, null);

            AttributeAggregator src = new AttributeAggregator(50f);
            src.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 10f);          // 60
            src.AddAggregatorMod(h2, GameplayModifierOperation.MultiplyAdditive, 2f);  // (50+10)*2 = 120

            AttributeAggregator snap = new AttributeAggregator();
            snap.TakeSnapshotOf(src);
            check("snapshot이 mod까지 복사 → 120", Approx(snap.Evaluate(default), 120f));
            check("snapshot base 복사 → GetBaseValue 50", Approx(snap.GetBaseValue(), 50f));

            // 원본을 크게 흔든다.
            src.SetBaseValue(999f);
            src.AddAggregatorMod(h3, GameplayModifierOperation.AddBase, 999f);
            check("원본 변경 후에도 snapshot 120 고정", Approx(snap.Evaluate(default), 120f));
            check("snapshot에 mod 더해도 원본 불변", SrcUnaffectedAfterSnapshotEdit(snap, src));

            // 서로 독립: 두 스냅샷이 같은 원본에서 떠도 격리.
            AttributeAggregator snapA = new AttributeAggregator();
            AttributeAggregator snapB = new AttributeAggregator();
            AttributeAggregator baseAgg = new AttributeAggregator(100f);
            snapA.TakeSnapshotOf(baseAgg);
            baseAgg.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 40f);
            snapB.TakeSnapshotOf(baseAgg);
            check("snapshotA(변경 전) 100 / snapshotB(변경 후) 140 독립", Approx(snapA.Evaluate(default), 100f) && Approx(snapB.Evaluate(default), 140f));
        }

        private static bool SrcUnaffectedAfterSnapshotEdit(AttributeAggregator snap, AttributeAggregator src)
        {
            float srcBefore = src.Evaluate(default);
            snap.AddAggregatorMod(new ActiveGameplayEffectHandle(777, null), GameplayModifierOperation.AddBase, 1f);
            return Approx(src.Evaluate(default), srcBefore);
        }

        // ── B. ASC 라우팅 (aggregator 직접) ──
        private static void RunAscRoutingChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle handle = CharacterAttributeSet.Health;
            SetBase(asc, handle, 100f);

            check("초기 Current == 100", Approx(asc.GetAttributeCurrentValue(handle), 100f));

            AttributeAggregator aggregator = FindOrCreateAggregator(asc, handle);
            if (aggregator == null)
            {
                check("ASC.FindOrCreateAttributeAggregator 접근", false);
                return;
            }

            ActiveGameplayEffectHandle modSource = new ActiveGameplayEffectHandle(10, asc);
            aggregator.AddAggregatorMod(modSource, GameplayModifierOperation.AddBase, 25f);
            check("mod 후 Current == 125 (aggregator Evaluate 경유)", Approx(asc.GetAttributeCurrentValue(handle), 125f));
            check("Base는 여전히 100", Approx(asc.GetAttributeBaseValue(handle), 100f));

            aggregator.RemoveAggregatorMod(modSource);
            check("mod 제거 후 Current 100 복귀", Approx(asc.GetAttributeCurrentValue(handle), 100f));

            SetBase(asc, handle, 200f);
            check("SetBase 200 → Base 200", Approx(asc.GetAttributeBaseValue(handle), 200f));
            check("SetBase 200 → Current 200 (mod 없음)", Approx(asc.GetAttributeCurrentValue(handle), 200f));
        }

        // ── C. capture 스냅샷 vs 라이브 ──
        private static void RunCaptureSnapshotChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle handle = CharacterAttributeSet.Health;
            SetBase(asc, handle, 100f);

            GameplayEffectAttributeCaptureDefinition liveDef = new GameplayEffectAttributeCaptureDefinition(handle, AttributeCaptureSource.Source, false);
            GameplayEffectAttributeCaptureDefinition snapDef = new GameplayEffectAttributeCaptureDefinition(handle, AttributeCaptureSource.Source, true);

            GameplayEffectAttributeCaptureSpec live = new GameplayEffectAttributeCaptureSpec(liveDef, asc);
            GameplayEffectAttributeCaptureSpec snap = new GameplayEffectAttributeCaptureSpec(snapDef, asc);

            live.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, default, out float liveBefore);
            snap.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, default, out float snapBefore);
            check("capture 초기값 live/snap 모두 100", Approx(liveBefore, 100f) && Approx(snapBefore, 100f));

            // 소스 어트리뷰트를 +50 흔든다.
            AttributeAggregator aggregator = FindOrCreateAggregator(asc, handle);
            aggregator.AddAggregatorMod(new ActiveGameplayEffectHandle(99, asc), GameplayModifierOperation.AddBase, 50f);

            live.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, default, out float liveAfter);
            snap.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, default, out float snapAfter);
            check("non-snapshot capture 추종 → 150", Approx(liveAfter, 150f));
            check("snapshot capture 고정 → 100", Approx(snapAfter, 100f));

            snap.TryGetCapturedValue(AttributeCaptureValueType.BaseValue, default, out float snapBase);
            check("snapshot base 캡처 → 100", Approx(snapBase, 100f));

            // 직접 얹은 +50 mod를 걷어 다음 레이어로 새지 않게 한다.
            aggregator.RemoveAggregatorMod(new ActiveGameplayEffectHandle(99, asc));
        }

        // ── D. GameplayEffectAsset 적용 (다수 케이스) ──
        private static void RunAssetDrivenChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;

            (GameplayModifierOperation op, float mag, float expected)[] cases =
            {
                (GameplayModifierOperation.AddBase, 20f, 120f),          // 100 + 20
                (GameplayModifierOperation.MultiplyAdditive, 1.5f, 150f), // 100 * (1 + 0.5)
                (GameplayModifierOperation.DivideAdditive, 2f, 50f),      // 100 / (1 + 1)
                (GameplayModifierOperation.MultiplyCompound, 3f, 300f),   // 100 * 3
                (GameplayModifierOperation.AddFinal, 15f, 115f),          // 100 + 15
                (GameplayModifierOperation.Override, 42f, 42f),           // override
            };

            foreach ((GameplayModifierOperation op, float mag, float expected) in cases)
            {
                SetBase(asc, health, 100f);
                GameplayEffectAsset ge = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, op, mag));
                ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectToSelf(ge);

                check($"Asset [{op} {mag}] 적용 → Current {expected}", Approx(asc.GetAttributeCurrentValue(health), expected));

                asc.RemoveActiveGameplayEffect(handle);
                check($"Asset [{op}] 제거 → Current 100 복귀", Approx(asc.GetAttributeCurrentValue(health), 100f));

                UnityEngine.Object.DestroyImmediate(ge);
            }

            // 한 GE에 여러 modifier 조합: (100+10)*(1+1) + 5 = 225
            SetBase(asc, health, 100f);
            GameplayEffectAsset combo = MakeGameplayEffect(GameplayEffectType.Infinite,
                MakeModifier(health, GameplayModifierOperation.AddBase, 10f),
                MakeModifier(health, GameplayModifierOperation.MultiplyAdditive, 2f),
                MakeModifier(health, GameplayModifierOperation.AddFinal, 5f));
            ActiveGameplayEffectHandle comboHandle = asc.ApplyGameplayEffectToSelf(combo);
            check("Asset 복합 mod → (100+10)*2 + 5 = 225", Approx(asc.GetAttributeCurrentValue(health), 225f));
            asc.RemoveActiveGameplayEffect(comboHandle);
            check("Asset 복합 제거 → 100", Approx(asc.GetAttributeCurrentValue(health), 100f));
            UnityEngine.Object.DestroyImmediate(combo);

            // 두 GE 스택: +10, +20 → 130, 부분 제거 → 120 → 100
            SetBase(asc, health, 100f);
            GameplayEffectAsset ge1 = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 10f));
            GameplayEffectAsset ge2 = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 20f));
            ActiveGameplayEffectHandle s1 = asc.ApplyGameplayEffectToSelf(ge1);
            ActiveGameplayEffectHandle s2 = asc.ApplyGameplayEffectToSelf(ge2);
            check("두 GE 스택 → 100+10+20 = 130", Approx(asc.GetAttributeCurrentValue(health), 130f));
            asc.RemoveActiveGameplayEffect(s1);
            check("한 개 제거 → 120", Approx(asc.GetAttributeCurrentValue(health), 120f));
            asc.RemoveActiveGameplayEffect(s2);
            check("둘 다 제거 → 100", Approx(asc.GetAttributeCurrentValue(health), 100f));
            UnityEngine.Object.DestroyImmediate(ge1);
            UnityEngine.Object.DestroyImmediate(ge2);

            // Instant: BaseValue 영구 변경
            SetBase(asc, health, 100f);
            GameplayEffectAsset instant = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f));
            asc.ApplyGameplayEffectToSelf(instant);
            check("Instant AddBase 30 → Base 130 (영구)", Approx(asc.GetAttributeBaseValue(health), 130f));
            check("Instant 후 Current도 130", Approx(asc.GetAttributeCurrentValue(health), 130f));
            UnityEngine.Object.DestroyImmediate(instant);
        }

        // ── E. 런타임 dirty → 라이브 재평가 (non-snapshot cross-attribute) ──
        // Speed += Health.current * 0.5 (Source 비-snapshot 캡처). Health가 바뀌면 dependents 전파로
        // Speed magnitude가 즉시 재평가돼야 한다. 실패하면 dirty/OnMagnitudeDependencyChange 체인이 끊긴 것.
        private static void RunRuntimeDirtyReevalChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);

            GameplayEffectAttributeCaptureDefinition captureDef = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Source, false);
            GameplayModifier speedMod = MakeAttributeBasedModifier(speed, captureDef, 0.5f, 0f, 0f);
            GameplayEffectAsset speedGe = MakeGameplayEffect(GameplayEffectType.Infinite, speedMod);
            ActiveGameplayEffectHandle speedHandle = asc.ApplyGameplayEffectToSelf(speedGe);
            check("초기: Speed == Health(100)*0.5 = 50", Approx(asc.GetAttributeCurrentValue(speed), 50f));

            GameplayEffectAsset healthBoost = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 100f));
            ActiveGameplayEffectHandle boostHandle = asc.ApplyGameplayEffectToSelf(healthBoost);
            check("Health 100→200 후 Speed 라이브 재평가 → 100", Approx(asc.GetAttributeCurrentValue(speed), 100f));

            asc.RemoveActiveGameplayEffect(boostHandle);
            check("Health 200→100 원복 후 Speed → 50", Approx(asc.GetAttributeCurrentValue(speed), 50f));

            asc.RemoveActiveGameplayEffect(speedHandle);
            UnityEngine.Object.DestroyImmediate(speedGe);
            UnityEngine.Object.DestroyImmediate(healthBoost);
        }

        // ── F. aggregator가 없는 어트리뷰트의 Base 변경 ──
        // Stamina는 위 단계에서 mod·캡처가 한 번도 걸리지 않아 aggregator가 없다. Base가 바뀌면 저장된 Current도 같이 바뀌어야 한다.
        private static void RunNoAggregatorBaseChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle stamina = CharacterAttributeSet.Stamina;

            SetBase(asc, stamina, 40f);
            check("aggregator 없음: SetBase 40 → Current 40", Approx(asc.GetAttributeCurrentValue(stamina), 40f));

            GameplayEffectAsset instant = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(stamina, GameplayModifierOperation.AddBase, 10f));
            asc.ApplyGameplayEffectToSelf(instant);
            check("aggregator 없음: Instant AddBase 10 → Base 50", Approx(asc.GetAttributeBaseValue(stamina), 50f));
            check("aggregator 없음: Instant 후 Current도 50", Approx(asc.GetAttributeCurrentValue(stamina), 50f));
            UnityEngine.Object.DestroyImmediate(instant);
        }

        // ── G. 라이브 재평가가 같은 aggregator의 다른 GE mod를 건드리지 않음 ──
        // 한 aggregator에는 여러 GE의 mod가 섞여 있다. 라이브 GE의 magnitude만 바뀌어야 하고, 먼저 건 고정 GE의 mod는 그대로 남아야 한다.
        private static void RunUpdateAggregatorModChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            GameplayAttributeHandle maxHealth = CharacterAttributeSet.MaxHealth;
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);
            SetBase(asc, maxHealth, 0f);

            GameplayEffectAttributeCaptureDefinition healthLive = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Source, false);

            GameplayEffectAsset fixedGe = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(speed, GameplayModifierOperation.AddBase, 10f));
            GameplayEffectAsset liveGe = MakeGameplayEffect(GameplayEffectType.Infinite, MakeAttributeBasedModifier(speed, healthLive, 0.5f, 0f, 0f));
            ActiveGameplayEffectHandle fixedHandle = asc.ApplyGameplayEffectToSelf(fixedGe);
            ActiveGameplayEffectHandle liveHandle = asc.ApplyGameplayEffectToSelf(liveGe);
            check("고정 +10 먼저, 라이브 Health*0.5 다음 → Speed 60", Approx(asc.GetAttributeCurrentValue(speed), 60f));

            GameplayEffectAsset healthBoost = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 100f));
            ActiveGameplayEffectHandle boostHandle = asc.ApplyGameplayEffectToSelf(healthBoost);
            check("Health 200 → 라이브만 100, 고정 +10 유지 → Speed 110", Approx(asc.GetAttributeCurrentValue(speed), 110f));

            asc.RemoveActiveGameplayEffect(fixedHandle);
            check("고정 GE 제거 → Speed 100", Approx(asc.GetAttributeCurrentValue(speed), 100f));
            asc.RemoveActiveGameplayEffect(liveHandle);
            check("라이브 GE 제거 → Speed 0", Approx(asc.GetAttributeCurrentValue(speed), 0f));

            // 라이브 modifier가 GE 안 2번인데 MaxHealth aggregator에는 이 mod 하나뿐인 경우
            GameplayEffectAsset mixedGe = MakeGameplayEffect(GameplayEffectType.Infinite,
                MakeModifier(speed, GameplayModifierOperation.AddBase, 1f),
                MakeModifier(speed, GameplayModifierOperation.AddBase, 2f),
                MakeAttributeBasedModifier(maxHealth, healthLive, 1f, 0f, 0f));
            ActiveGameplayEffectHandle mixedHandle = asc.ApplyGameplayEffectToSelf(mixedGe);
            check("2번 modifier가 라이브인 GE → MaxHealth 200, Speed 3", Approx(asc.GetAttributeCurrentValue(maxHealth), 200f) && Approx(asc.GetAttributeCurrentValue(speed), 3f));

            asc.RemoveActiveGameplayEffect(boostHandle);
            check("Health 100 → MaxHealth 100, Speed 3 유지", Approx(asc.GetAttributeCurrentValue(maxHealth), 100f) && Approx(asc.GetAttributeCurrentValue(speed), 3f));

            asc.RemoveActiveGameplayEffect(mixedHandle);
            UnityEngine.Object.DestroyImmediate(fixedGe);
            UnityEngine.Object.DestroyImmediate(liveGe);
            UnityEngine.Object.DestroyImmediate(healthBoost);
            UnityEngine.Object.DestroyImmediate(mixedGe);
        }

        // ── H. 주기 실행마다 magnitude 재계산 ──
        // Speed AddBase = Health.current(non-snapshot) × 1을 1초마다 실행. 적용 뒤 Health가 바뀌면 다음 실행은 바뀐 Health만큼 더해야 한다.
        private static void RunPeriodicRecalculationChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);

            ActiveGameplayEffectsContainer container = GetContainer(asc);
            if (container == null)
            {
                check("ActiveGameplayEffectsContainer 접근", false);
                return;
            }

            GameplayEffectAttributeCaptureDefinition healthLive = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Source, false);
            GameplayEffectAsset periodicGe = MakeGameplayEffect(GameplayEffectType.Duration, MakeAttributeBasedModifier(speed, healthLive, 1f, 0f, 0f));
            SetPrivateField(periodicGe, "duration", 100f);
            SetPrivateField(periodicGe, "period", 1f);
            SetPrivateField(periodicGe, "executePeriodicEffectOnApplication", true);

            ActiveGameplayEffectHandle periodicHandle = asc.ApplyGameplayEffectToSelf(periodicGe);
            check("주기 GE 적용 즉시 1회 실행 → Speed Base 100", Approx(asc.GetAttributeBaseValue(speed), 100f));

            GameplayEffectAsset healthBoost = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 100f));
            ActiveGameplayEffectHandle boostHandle = asc.ApplyGameplayEffectToSelf(healthBoost);

            container.Tick(1f);
            check("Health 200 뒤 다음 주기 실행 → +200, Speed Base 300", Approx(asc.GetAttributeBaseValue(speed), 300f));

            asc.RemoveActiveGameplayEffect(periodicHandle);
            asc.RemoveActiveGameplayEffect(boostHandle);
            UnityEngine.Object.DestroyImmediate(periodicGe);
            UnityEngine.Object.DestroyImmediate(healthBoost);
        }

        // ── I. 어트리뷰트 값 변경 이벤트 ──
        // 원인(GEModData)은 GE 실행으로 Base가 바뀐 경우에만 실리고, Duration/Infinite mod·직접 Base 설정이면 null이다.
        private static void RunAttributeValueChangeEventChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            GameplayAttributeHandle stamina = CharacterAttributeSet.Stamina;
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);
            SetBase(asc, stamina, 40f);

            List<OnAttributeChangeData> healthEvents = new List<OnAttributeChangeData>();
            void OnHealthChanged(OnAttributeChangeData data)
            {
                healthEvents.Add(data);
            }

            GameplayAttributeValueChangeEvent healthEvent = asc.GetGameplayAttributeValueChangeDelegate(health);
            check("같은 어트리뷰트는 같은 이벤트 객체", ReferenceEquals(healthEvent, asc.GetGameplayAttributeValueChangeDelegate(health)));
            healthEvent.OnValueChange += OnHealthChanged;

            // Infinite mod 등록·해제 → 원인 없음
            GameplayEffectAsset infiniteGe = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 20f));
            ActiveGameplayEffectHandle infiniteHandle = asc.ApplyGameplayEffectToSelf(infiniteGe);
            check("Infinite +20 → 이벤트 1회, 100 → 120", healthEvents.Count == 1 && Approx(healthEvents[0].OldValue, 100f) && Approx(healthEvents[0].NewValue, 120f));
            check("Infinite mod 등록 → 핸들이 Health, GEModData null", healthEvents.Count == 1 && healthEvents[0].Attribute.Equals(health) && healthEvents[0].GEModData == null);

            healthEvents.Clear();
            asc.RemoveActiveGameplayEffect(infiniteHandle);
            check("Infinite 제거 → 이벤트 1회, 120 → 100, GEModData null", healthEvents.Count == 1 && Approx(healthEvents[0].NewValue, 100f) && healthEvents[0].GEModData == null);

            // Instant 실행 → 원인 있음
            healthEvents.Clear();
            GameplayEffectAsset damageGe = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, -30f));
            asc.ApplyGameplayEffectToSelf(damageGe);
            check("Instant -30 → 이벤트 1회, 100 → 70", healthEvents.Count == 1 && Approx(healthEvents[0].OldValue, 100f) && Approx(healthEvents[0].NewValue, 70f));
            GameplayEffectModCallbackData damageModData = healthEvents.Count == 1 ? healthEvents[0].GEModData : null;
            check("Instant → GEModData 있음", damageModData != null);
            check("GEModData: Spec 정의 = 적용한 GE, 수치 -30, Target = 이 ASC", damageModData != null && damageModData.EffectSpec.Definition == damageGe && Approx(damageModData.EvaluatedData.Magnitude, -30f) && damageModData.Target == asc);

            // 값이 그대로여도 울림
            healthEvents.Clear();
            GameplayEffectAsset zeroGe = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 0f));
            asc.ApplyGameplayEffectToSelf(zeroGe);
            check("Instant +0 → 값이 같아도 이벤트 1회(70 → 70), GEModData 있음", healthEvents.Count == 1 && Approx(healthEvents[0].OldValue, 70f) && Approx(healthEvents[0].NewValue, 70f) && healthEvents[0].GEModData != null);

            // 직접 Base 설정 → 앞 실행의 원인이 남지 않음
            healthEvents.Clear();
            SetBase(asc, health, 50f);
            check("SetBase 50 → 이벤트 1회, GEModData null", healthEvents.Count == 1 && Approx(healthEvents[0].NewValue, 50f) && healthEvents[0].GEModData == null);

            // aggregator가 없는 어트리뷰트도 같은 이벤트·원인
            List<OnAttributeChangeData> staminaEvents = new List<OnAttributeChangeData>();
            void OnStaminaChanged(OnAttributeChangeData data)
            {
                staminaEvents.Add(data);
            }

            asc.GetGameplayAttributeValueChangeDelegate(stamina).OnValueChange += OnStaminaChanged;
            GameplayEffectAsset staminaGe = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(stamina, GameplayModifierOperation.AddBase, 5f));
            asc.ApplyGameplayEffectToSelf(staminaGe);
            check("aggregator 없음: Instant +5 → 이벤트 1회, 40 → 45, GEModData 있음", staminaEvents.Count == 1 && Approx(staminaEvents[0].OldValue, 40f) && Approx(staminaEvents[0].NewValue, 45f) && staminaEvents[0].GEModData != null);
            asc.GetGameplayAttributeValueChangeDelegate(stamina).OnValueChange -= OnStaminaChanged;

            // 구독자 안에서 다른 GE를 적용해도 각 이벤트의 원인이 섞이지 않음
            List<OnAttributeChangeData> speedEvents = new List<OnAttributeChangeData>();
            void OnSpeedChanged(OnAttributeChangeData data)
            {
                speedEvents.Add(data);
            }

            GameplayEffectAsset speedGe = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(speed, GameplayModifierOperation.AddBase, 3f));
            bool appliedInHandler = false;
            void ApplySpeedOnHealthChanged(OnAttributeChangeData data)
            {
                if (!appliedInHandler)
                {
                    appliedInHandler = true;
                    asc.ApplyGameplayEffectToSelf(speedGe);
                }
            }

            asc.GetGameplayAttributeValueChangeDelegate(speed).OnValueChange += OnSpeedChanged;
            healthEvent.OnValueChange += ApplySpeedOnHealthChanged;
            healthEvents.Clear();
            asc.ApplyGameplayEffectToSelf(damageGe);
            check("중첩: Health 이벤트의 원인 = Health GE", healthEvents.Count == 1 && healthEvents[0].GEModData != null && healthEvents[0].GEModData.EffectSpec.Definition == damageGe);
            check("중첩: 구독자 안에서 적용한 Speed 이벤트의 원인 = Speed GE", speedEvents.Count == 1 && speedEvents[0].GEModData != null && speedEvents[0].GEModData.EffectSpec.Definition == speedGe);
            healthEvent.OnValueChange -= ApplySpeedOnHealthChanged;
            asc.GetGameplayAttributeValueChangeDelegate(speed).OnValueChange -= OnSpeedChanged;

            healthEvents.Clear();
            SetBase(asc, health, 60f);
            check("중첩 뒤 SetBase → GEModData null(원인이 남지 않음)", healthEvents.Count == 1 && healthEvents[0].GEModData == null);

            // 해제 뒤엔 오지 않음
            healthEvent.OnValueChange -= OnHealthChanged;
            healthEvents.Clear();
            SetBase(asc, health, 100f);
            check("해제 뒤 → 이벤트 없음", healthEvents.Count == 0);

            UnityEngine.Object.DestroyImmediate(infiniteGe);
            UnityEngine.Object.DestroyImmediate(damageGe);
            UnityEngine.Object.DestroyImmediate(zeroGe);
            UnityEngine.Object.DestroyImmediate(staminaGe);
            UnityEngine.Object.DestroyImmediate(speedGe);
        }

        // ── J. PreAttributeBaseChange 클램프 ──
        // Base를 [0, 100]으로 막는 Set. aggregator 없는 경로(직접 Current)와 있는 경로(aggregator base) 둘 다 본다.
        private static void RunPreAttributeBaseChangeClampChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            ClampedAttributeSet set = new ClampedAttributeSet();
            set.health = new AttributeData(50f, 50f);
            asc.AddSpawnedAttribute(set);
            GameplayAttributeHandle health = ClampedAttributeSet.Health;

            SetBase(asc, health, -50f);
            check("aggregator 없음: SetBase -50 → Pre 클램프로 Base 0, Current 0", Approx(asc.GetAttributeBaseValue(health), 0f) && Approx(asc.GetAttributeCurrentValue(health), 0f));
            check("Post가 클램프된 값 0을 받음", Approx(set.LastPostNewValue, 0f));

            List<OnAttributeChangeData> events = new List<OnAttributeChangeData>();
            void OnHealthChanged(OnAttributeChangeData data)
            {
                events.Add(data);
            }

            asc.GetGameplayAttributeValueChangeDelegate(health).OnValueChange += OnHealthChanged;

            SetBase(asc, health, 10f);
            events.Clear();
            GameplayEffectAsset damageGe = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, -30f));
            asc.ApplyGameplayEffectToSelf(damageGe);
            check("Instant -30 (10에서) → Base 0, 값 변경 이벤트 NewValue 0", Approx(asc.GetAttributeBaseValue(health), 0f) && events.Count == 1 && Approx(events[0].NewValue, 0f));

            GameplayEffectAsset bonusGe = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 5f));
            ActiveGameplayEffectHandle bonusHandle = asc.ApplyGameplayEffectToSelf(bonusGe);
            SetBase(asc, health, 200f);
            check("aggregator 있음: SetBase 200 → Base 100, Current 105(mod +5는 Base 클램프와 별개)", Approx(asc.GetAttributeBaseValue(health), 100f) && Approx(asc.GetAttributeCurrentValue(health), 105f));
            check("aggregator 있음: Post가 클램프된 값 100을 받음", Approx(set.LastPostNewValue, 100f));

            asc.GetGameplayAttributeValueChangeDelegate(health).OnValueChange -= OnHealthChanged;
            asc.RemoveActiveGameplayEffect(bonusHandle);
            asc.RemoveAttributeSet(set);
            UnityEngine.Object.DestroyImmediate(damageGe);
            UnityEngine.Object.DestroyImmediate(bonusGe);
        }

        // ── K. AttributeSet 훅 (TestAtrributeSet) ──
        private static void RunAttributeSetHookChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            TestAtrributeSet set = new TestAtrributeSet();
            set.health = new AttributeData(100f, 100f);
            set.damage = new AttributeData(0f, 0f);
            asc.AddSpawnedAttribute(set);
            GameplayAttributeHandle health = TestAtrributeSet.Health;
            GameplayAttributeHandle damage = TestAtrributeSet.Damage;

            List<OnAttributeChangeData> healthEvents = new List<OnAttributeChangeData>();
            void OnHealthChanged(OnAttributeChangeData data)
            {
                healthEvents.Add(data);
            }

            asc.GetGameplayAttributeValueChangeDelegate(health).OnValueChange += OnHealthChanged;

            // meta Damage → Post가 Health에 반영
            GameplayEffectAsset damage30 = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(damage, GameplayModifierOperation.AddBase, 30f));
            asc.ApplyGameplayEffectToSelf(damage30);
            check("meta Damage 30 → Post가 Health 100 → 70, Damage는 0으로", Approx(asc.GetAttributeBaseValue(health), 70f) && Approx(asc.GetAttributeBaseValue(damage), 0f));
            check("Pre가 1번 불림, Post가 받은 데미지 30", set.PreExecuteCount == 1 && Approx(set.LastExecutedDamage, 30f));
            check("Health 값 변경 이벤트 100 → 70", healthEvents.Count == 1 && Approx(healthEvents[0].OldValue, 100f) && Approx(healthEvents[0].NewValue, 70f));

            // Pre가 수치를 고침
            set.DamageScale = 0.5f;
            GameplayEffectAsset damage40 = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(damage, GameplayModifierOperation.AddBase, 40f));
            asc.ApplyGameplayEffectToSelf(damage40);
            check("Pre가 배율 0.5 → Damage 20만 적용, Health 70 → 50", Approx(set.LastExecutedDamage, 20f) && Approx(asc.GetAttributeBaseValue(health), 50f));
            set.DamageScale = 1f;

            // Pre가 버림
            set.RejectExecute = true;
            healthEvents.Clear();
            int preCountBeforeReject = set.PreExecuteCount;
            asc.ApplyGameplayEffectToSelf(damage40);
            check("Pre가 false → Pre는 불렸지만 Health 50·Damage 0 그대로, 이벤트 없음", set.PreExecuteCount == preCountBeforeReject + 1 && Approx(asc.GetAttributeBaseValue(health), 50f) && Approx(asc.GetAttributeBaseValue(damage), 0f) && healthEvents.Count == 0);
            set.RejectExecute = false;

            // Current 클램프 (mod 등록 — 실행이 아니라 Pre/PostGameplayEffectExecute는 안 불림)
            int preCountBeforeBuff = set.PreExecuteCount;
            healthEvents.Clear();
            GameplayEffectAsset healthBuff = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 100f));
            ActiveGameplayEffectHandle buffHandle = asc.ApplyGameplayEffectToSelf(healthBuff);
            check("Infinite +100 (Base 50) → PreAttributeChange가 Current 150을 100으로, Base는 50 그대로", Approx(asc.GetAttributeCurrentValue(health), 100f) && Approx(asc.GetAttributeBaseValue(health), 50f));
            check("PostAttributeChange가 50 → 100을 받음, 값 변경 이벤트도 100", Approx(set.LastChangeOldValue, 50f) && Approx(set.LastChangeNewValue, 100f) && healthEvents.Count == 1 && Approx(healthEvents[0].NewValue, 100f));
            check("mod 등록엔 PreGameplayEffectExecute가 안 불림", set.PreExecuteCount == preCountBeforeBuff);

            asc.RemoveActiveGameplayEffect(buffHandle);
            check("버프 제거 → Current 50, PostAttributeChange 100 → 50", Approx(asc.GetAttributeCurrentValue(health), 50f) && Approx(set.LastChangeOldValue, 100f) && Approx(set.LastChangeNewValue, 50f));

            asc.GetGameplayAttributeValueChangeDelegate(health).OnValueChange -= OnHealthChanged;
            asc.RemoveAttributeSet(set);
            UnityEngine.Object.DestroyImmediate(damage30);
            UnityEngine.Object.DestroyImmediate(damage40);
            UnityEngine.Object.DestroyImmediate(healthBuff);
        }

        private class ClampedAttributeSet : AttributeSet
        {
            public static readonly GameplayAttributeHandle Health = new(typeof(ClampedAttributeSet), nameof(health));

            public AttributeData health;

            public float LastPostNewValue { get; private set; } = float.NaN;

            public override void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, ref float newValue)
            {
                newValue = Mathf.Clamp(newValue, 0f, 100f);
            }

            public override void PostAttributeBaseChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue)
            {
                LastPostNewValue = newValue;
            }
        }

        // ── 헬퍼 ──

        private static void AddHealthSet(AbilitySystemComponent asc, float value)
        {
            CharacterAttributeSet set = new CharacterAttributeSet();
            set.health = new AttributeData(value, value);
            asc.AddSpawnedAttribute(set);
        }

        private static void SetBase(AbilitySystemComponent asc, GameplayAttributeHandle handle, float value)
        {
            asc.SetAttributeBaseValue(handle, value);
        }

        private static GameplayModifier MakeModifier(GameplayAttributeHandle handle, GameplayModifierOperation op, float magnitude)
        {
            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(handle));
            SetPrivateField(boxed, "operation", op);
            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);
            SetPrivateField(boxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);
            return (GameplayModifier)boxed;
        }

        private static GameplayModifier MakeAttributeBasedModifier(GameplayAttributeHandle target, GameplayEffectAttributeCaptureDefinition captureDef, float coefficient, float pre, float post)
        {
            AttributeBasedMagnitude attributeBased = new AttributeBasedMagnitude();
            SetPrivateField(attributeBased, "backingAttribute", captureDef);
            SetPrivateField(attributeBased, "captureValueType", AttributeCaptureValueType.CurrentValue);
            SetPrivateField(attributeBased, "coefficient", coefficient);
            SetPrivateField(attributeBased, "preMultiplyAdditive", pre);
            SetPrivateField(attributeBased, "postMultiplyAdditive", post);

            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(target));
            SetPrivateField(boxed, "operation", GameplayModifierOperation.AddBase);
            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.AttributeBased);
            SetPrivateField(magnitudeBoxed, "attributeBasedMagnitude", attributeBased);
            SetPrivateField(boxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);
            return (GameplayModifier)boxed;
        }

        private static GameplayEffectAsset MakeGameplayEffect(GameplayEffectType type, params GameplayModifier[] modifiers)
        {
            GameplayEffectAsset ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", type);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier>(modifiers));
            return ge;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, NonPublicInstance);
            if (field == null)
            {
                throw new InvalidOperationException($"필드 '{name}'을 {target.GetType().Name}에서 찾지 못함.");
            }

            field.SetValue(target, value);
        }

        private static void EnsureInitialized(AbilitySystemComponent asc)
        {
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", NonPublicInstance);
            if (containerField != null && containerField.GetValue(asc) == null)
            {
                typeof(AbilitySystemComponent).GetMethod("Awake", NonPublicInstance)?.Invoke(asc, null);
            }
        }

        private static ActiveGameplayEffectsContainer GetContainer(AbilitySystemComponent asc)
        {
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", NonPublicInstance);
            return containerField?.GetValue(asc) as ActiveGameplayEffectsContainer;
        }

        private static AttributeAggregator FindOrCreateAggregator(AbilitySystemComponent asc, GameplayAttributeHandle handle)
        {
            MethodInfo method = typeof(AbilitySystemComponent).GetMethod("FindOrCreateAttributeAggregator", NonPublicInstance);
            return method?.Invoke(asc, new object[] { handle }) as AttributeAggregator;
        }

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < Eps;
        }
    }
}
