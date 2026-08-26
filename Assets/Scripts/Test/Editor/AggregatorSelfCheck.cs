using System;
using System.Collections.Generic;
using System.Reflection;
using Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Aggregator;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 Aggregator를 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // A: AttributeAggregator.Evaluate 공식·mod 제거 / B: ASC.GetAttributeCurrentValue 라우팅
    // C: capture 스냅샷 vs 라이브 / D: 실제 GameplayEffectAsset을 ASC에 적용한 다수 케이스.
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
            check("base만 → Evaluate == 100", Approx(agg.Evaluate(), 100f));

            agg.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 20f);
            check("AddBase 20 → 120", Approx(agg.Evaluate(), 120f));

            agg.AddAggregatorMod(h2, GameplayModifierOperation.MultiplyAdditive, 1.5f);
            check("MultiplyAdditive 1.5 → (100+20)*1.5 = 180", Approx(agg.Evaluate(), 180f));

            agg.RemoveAggregatorMod(h2);
            check("MultiplyAdditive 제거 → 120 복귀", Approx(agg.Evaluate(), 120f));

            agg.RemoveAggregatorMod(h1);
            check("AddBase 제거 → 100 복귀", Approx(agg.Evaluate(), 100f));

            AttributeAggregator mix = new AttributeAggregator(10f);
            mix.AddAggregatorMod(h1, GameplayModifierOperation.MultiplyCompound, 2f);
            mix.AddAggregatorMod(h2, GameplayModifierOperation.AddFinal, 5f);
            check("MultiplyCompound 2 + AddFinal 5 → 10*2 + 5 = 25", Approx(mix.Evaluate(), 25f));

            AttributeAggregator over = new AttributeAggregator(100f);
            over.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 50f);
            over.AddAggregatorMod(h2, GameplayModifierOperation.Override, 7f);
            check("Override → 다른 mod 무시하고 7", Approx(over.Evaluate(), 7f));

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
            check("snapshot이 mod까지 복사 → 120", Approx(snap.Evaluate(), 120f));
            check("snapshot base 복사 → GetBaseValue 50", Approx(snap.GetBaseValue(), 50f));

            // 원본을 크게 흔든다.
            src.SetBaseValue(999f);
            src.AddAggregatorMod(h3, GameplayModifierOperation.AddBase, 999f);
            check("원본 변경 후에도 snapshot 120 고정", Approx(snap.Evaluate(), 120f));
            check("snapshot에 mod 더해도 원본 불변", SrcUnaffectedAfterSnapshotEdit(snap, src));

            // 서로 독립: 두 스냅샷이 같은 원본에서 떠도 격리.
            AttributeAggregator snapA = new AttributeAggregator();
            AttributeAggregator snapB = new AttributeAggregator();
            AttributeAggregator baseAgg = new AttributeAggregator(100f);
            snapA.TakeSnapshotOf(baseAgg);
            baseAgg.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 40f);
            snapB.TakeSnapshotOf(baseAgg);
            check("snapshotA(변경 전) 100 / snapshotB(변경 후) 140 독립", Approx(snapA.Evaluate(), 100f) && Approx(snapB.Evaluate(), 140f));
        }

        private static bool SrcUnaffectedAfterSnapshotEdit(AttributeAggregator snap, AttributeAggregator src)
        {
            float srcBefore = src.Evaluate();
            snap.AddAggregatorMod(new ActiveGameplayEffectHandle(777, null), GameplayModifierOperation.AddBase, 1f);
            return Approx(src.Evaluate(), srcBefore);
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

            live.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, out float liveBefore);
            snap.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, out float snapBefore);
            check("capture 초기값 live/snap 모두 100", Approx(liveBefore, 100f) && Approx(snapBefore, 100f));

            // 소스 어트리뷰트를 +50 흔든다.
            AttributeAggregator aggregator = FindOrCreateAggregator(asc, handle);
            aggregator.AddAggregatorMod(new ActiveGameplayEffectHandle(99, asc), GameplayModifierOperation.AddBase, 50f);

            live.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, out float liveAfter);
            snap.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, out float snapAfter);
            check("non-snapshot capture 추종 → 150", Approx(liveAfter, 150f));
            check("snapshot capture 고정 → 100", Approx(snapAfter, 100f));

            snap.TryGetCapturedValue(AttributeCaptureValueType.BaseValue, out float snapBase);
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
            SetPrivateField(boxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(boxed, "magnitude", magnitude);
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
            SetPrivateField(boxed, "magnitudeCalculationType", MagnitudeCalculationType.AttributeBased);
            SetPrivateField(boxed, "attributeBased", attributeBased);
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
