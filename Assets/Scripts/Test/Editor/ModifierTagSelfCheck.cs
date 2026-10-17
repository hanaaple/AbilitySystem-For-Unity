using System;
using System.Collections.Generic;
using System.Reflection;
using Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 Modifier Source/Target 태그 조건 판정을 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // 0: 테스트 태그 준비(native·레지스트리) / A: GameplayTagRequirements 판정 / B: AttributeAggregator 자격 판정
    // C: 실제 ASC에 GE 적용(Instant 건너뛰기·지속형 CurrentValue·캡처 평가).
    // ASC 소유 태그는 아직 늘 비어 있어(입구 미구현) Target 조건 통과는 B에서만, Source 조건은 Spec 태그를 직접 넣어 C에서 본다.
    public static class ModifierTagSelfCheck
    {
        private const float Eps = 0.001f;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        // 레지스트리 쪽 테스트 태그. Assets/Resources/AbilitySystem/GameplayTagRegistry.asset에 같은 이름이 있어야 한다.
        private const string RegistryStunnedName = "Test.ModifierTag.Registry.Stunned";

        [NativeTag] private static readonly NativeGameplayTag NativeParent = new("Test.ModifierTag.Native");
        [NativeTag] private static readonly NativeGameplayTag NativeEnraged = new("Test.ModifierTag.Native.Enraged");

        [MenuItem("Tools/Ability System/Run Modifier Tag Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[ModifierTag PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[ModifierTag FAIL] {label}");
                }
            }

            if (!PrepareTags(Check, out TestTags tags))
            {
                Debug.LogError($"[ModifierTag Self-Check] 테스트 태그 준비 실패로 중단 — passed {passed} / failed {failed}");
                return;
            }

            RunRequirementsChecks(tags, Check);
            RunAggregatorChecks(tags, Check);

            GameObject go = new GameObject("ModifierTagSelfCheck_Temp");
            try
            {
                AbilitySystemComponent asc = go.AddComponent<AbilitySystemComponent>();
                EnsureInitialized(asc);
                AddCharacterSet(asc, 100f);

                RunOwnedTagsCheck(asc, Check);
                RunInstantChecks(asc, tags, Check);
                RunPersistentChecks(asc, tags, Check);
                RunCaptureContextChecks(asc, tags, Check);
            }
            catch (Exception e)
            {
                Check($"ASC 검사 중 예외: {e.GetType().Name} — {e.Message}", false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            Debug.Log($"[ModifierTag Self-Check] passed {passed} / failed {failed}");
        }

        private readonly struct TestTags
        {
            public readonly GameplayTag Parent;
            public readonly GameplayTag Enraged;
            public readonly GameplayTag Stunned;

            public TestTags(GameplayTag parent, GameplayTag enraged, GameplayTag stunned)
            {
                Parent = parent;
                Enraged = enraged;
                Stunned = stunned;
            }
        }

        // ── 0. 테스트 태그 준비 ──
        private static bool PrepareTags(Action<string, bool> check, out TestTags tags)
        {
            GameplayTag parent = NativeParent.GetTag();
            GameplayTag enraged = NativeEnraged.GetTag();
            GameplayTag stunned = GameplayTag.RequestGameplayTag(RegistryStunnedName);

            check("0-1 native 태그 Test.ModifierTag.Native.Enraged 유효", enraged.IsValid());
            check("0-2 native 부모 태그 Test.ModifierTag.Native 유효", parent.IsValid());
            check($"0-3 레지스트리 태그 {RegistryStunnedName} 유효(레지스트리 에셋에 있어야 함)", stunned.IsValid());

            tags = new TestTags(parent, enraged, stunned);
            return parent.IsValid() && enraged.IsValid() && stunned.IsValid();
        }

        private static void RunOwnedTagsCheck(AbilitySystemComponent asc, Action<string, bool> check)
        {
            GameplayTagContainer ownedTags = asc.GetOwnedGameplayTags();
            check("0-4 ASC 소유 태그는 아직 늘 비어 있음(입구 미구현, D18)", ownedTags.IsEmpty());
        }

        // ── A. GameplayTagRequirements 판정 (순수) ──
        private static void RunRequirementsChecks(TestTags tags, Action<string, bool> check)
        {
            GameplayTagContainer withEnraged = MakeContainer(tags.Enraged);
            GameplayTagContainer withStunned = MakeContainer(tags.Stunned);
            GameplayTagContainer empty = new GameplayTagContainer();

            GameplayTagRequirements requireEnraged = MakeRequirements(new[] { tags.Enraged }, null);
            check("A-1 Require Enraged: Enraged 보유 → 충족", requireEnraged.RequirementsMet(withEnraged));
            check("A-2 Require Enraged: 빈 태그 → 불충족", !requireEnraged.RequirementsMet(empty));

            GameplayTagRequirements requireParent = MakeRequirements(new[] { tags.Parent }, null);
            check("A-3 Require 부모(Test.ModifierTag.Native): 자식 Enraged 보유 → 충족(계층 매칭)", requireParent.RequirementsMet(withEnraged));

            GameplayTagRequirements ignoreStunned = MakeRequirements(null, new[] { tags.Stunned });
            check("A-4 Ignore Stunned(레지스트리): Stunned 보유 → 불충족", !ignoreStunned.RequirementsMet(withStunned));
            check("A-5 Ignore Stunned: 빈 태그 → 충족", ignoreStunned.RequirementsMet(empty));

            GameplayTagRequirements queryStunned = MakeRequirements(null, null);
            SetPrivateField(queryStunned, "_tagQuery", GameplayTagQuery.MakeQuery_MatchAnyTags(withStunned));
            check("A-6 TagQuery Any(Stunned): Stunned 보유 → 충족", queryStunned.RequirementsMet(withStunned));
            check("A-7 TagQuery Any(Stunned): Enraged만 보유 → 불충족", !queryStunned.RequirementsMet(withEnraged));

            GameplayTagRequirements none = MakeRequirements(null, null);
            check("A-8 조건 없음 → IsEmpty", none.IsEmpty());
        }

        // ── B. AttributeAggregator 자격 판정 (순수) ──
        private static void RunAggregatorChecks(TestTags tags, Action<string, bool> check)
        {
            GameplayTagContainer withEnraged = MakeContainer(tags.Enraged);
            GameplayTagContainer withStunned = MakeContainer(tags.Stunned);
            GameplayTagContainer empty = new GameplayTagContainer();

            GameplayTagRequirements requireEnraged = MakeRequirements(new[] { tags.Enraged }, null);
            GameplayTagRequirements ignoreStunned = MakeRequirements(null, new[] { tags.Stunned });

            ActiveGameplayEffectHandle h1 = new ActiveGameplayEffectHandle(1, null);
            ActiveGameplayEffectHandle h2 = new ActiveGameplayEffectHandle(2, null);
            ActiveGameplayEffectHandle h3 = new ActiveGameplayEffectHandle(3, null);

            // 캡처 평가 경로(Evaluate) — 평가 문맥의 태그로 판정
            AttributeAggregator targetReq = new AttributeAggregator(100f);
            targetReq.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 50f, null, requireEnraged);
            check("B-1 Evaluate: Target Require Enraged, 문맥 Target에 Enraged → 150", Approx(targetReq.Evaluate(new AggregatorEvaluateParameters(null, withEnraged)), 150f));
            check("B-2 Evaluate: 같은 mod, 문맥 Target 빈 태그 → 100", Approx(targetReq.Evaluate(default), 100f));
            check("B-3 Evaluate: 같은 mod, Enraged가 Source에만 있음 → 100(Target 조건은 Target 태그로만)", Approx(targetReq.Evaluate(new AggregatorEvaluateParameters(withEnraged, null)), 100f));

            AttributeAggregator sourceIgnore = new AttributeAggregator(100f);
            sourceIgnore.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 50f, ignoreStunned, null);
            check("B-4 Evaluate: Source Ignore Stunned, 문맥 Source에 Stunned → 100", Approx(sourceIgnore.Evaluate(new AggregatorEvaluateParameters(withStunned, null)), 100f));
            check("B-5 Evaluate: 같은 mod, 문맥 빈 태그 → 150", Approx(sourceIgnore.Evaluate(default), 150f));

            AttributeAggregator noReq = new AttributeAggregator(100f);
            noReq.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 50f);
            check("B-6 조건 없는 mod(기존 호출) → 어느 문맥이든 150", Approx(noReq.Evaluate(new AggregatorEvaluateParameters(withStunned, withStunned)), 150f) && Approx(noReq.EvaluateCurrentValue(empty), 150f));

            // CurrentValue 경로(EvaluateCurrentValue) — Target은 소유 태그, Source는 각 mod를 건 GE의 Source 태그 (D21)
            check("B-7 CurrentValue: Target Require Enraged, 소유 태그에 Enraged → 150", Approx(targetReq.EvaluateCurrentValue(withEnraged), 150f));
            check("B-8 CurrentValue: 같은 mod, 소유 태그 빈 → 100", Approx(targetReq.EvaluateCurrentValue(empty), 100f));

            GameplayTagRequirements sourceRequireEnraged = MakeRequirements(new[] { tags.Enraged }, null);
            AttributeAggregator perModSource = new AttributeAggregator(100f);
            perModSource.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 10f, sourceRequireEnraged, null, withEnraged);
            perModSource.AddAggregatorMod(h2, GameplayModifierOperation.AddBase, 20f, sourceRequireEnraged, null, empty);
            perModSource.AddAggregatorMod(h3, GameplayModifierOperation.AddBase, 40f, sourceRequireEnraged, null, null);
            check("B-9 CurrentValue: Source Require Enraged — Source가 Enraged였던 GE의 mod(+10)만 → 110", Approx(perModSource.EvaluateCurrentValue(empty), 110f));
            check("B-10 Evaluate: 같은 mod들을 문맥 Source=Enraged로 평가 → 세 mod 모두 → 170(캡처 경로는 GE별 Source를 보지 않음)", Approx(perModSource.Evaluate(new AggregatorEvaluateParameters(withEnraged, null)), 170f));

            // snapshot도 조건을 들고 간다
            AttributeAggregator snapshot = new AttributeAggregator();
            snapshot.TakeSnapshotOf(targetReq);
            check("B-11 snapshot: 조건 복사 — 문맥 Target Enraged → 150 / 빈 → 100", Approx(snapshot.Evaluate(new AggregatorEvaluateParameters(null, withEnraged)), 150f) && Approx(snapshot.Evaluate(default), 100f));

            // Override도 조건을 못 맞추면 건너뛴다
            AttributeAggregator overrideReq = new AttributeAggregator(100f);
            overrideReq.AddAggregatorMod(h1, GameplayModifierOperation.Override, 7f, null, requireEnraged);
            check("B-12 Override + Target Require Enraged: 문맥 빈 → 100 / Enraged → 7", Approx(overrideReq.Evaluate(default), 100f) && Approx(overrideReq.Evaluate(new AggregatorEvaluateParameters(null, withEnraged)), 7f));
        }

        // ── C-1. Instant: 실행 시 조건 못 맞춘 modifier 건너뛰기 ──
        private static void RunInstantChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;

            SetBase(asc, health, 100f);
            GameplayEffectAsset targetRequire = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, null, MakeRequirements(new[] { tags.Enraged }, null)));
            asc.ApplyGameplayEffectToSelf(targetRequire);
            check("C-1 Instant +30, Target Require Enraged, 소유 태그 빈 → 건너뜀(Base 100)", Approx(asc.GetAttributeBaseValue(health), 100f));

            SetBase(asc, health, 100f);
            GameplayEffectAsset targetIgnore = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, null, MakeRequirements(null, new[] { tags.Stunned })));
            asc.ApplyGameplayEffectToSelf(targetIgnore);
            check("C-2 Instant +30, Target Ignore Stunned, 소유 태그 빈 → 적용(Base 130)", Approx(asc.GetAttributeBaseValue(health), 130f));

            GameplayEffectAsset sourceRequire = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, MakeRequirements(new[] { tags.Enraged }, null), null));

            SetBase(asc, health, 100f);
            asc.ApplyGameplayEffectToSelf(sourceRequire);
            check("C-3 Instant +30, Source Require Enraged, Source 태그 빈 → 건너뜀(Base 100)", Approx(asc.GetAttributeBaseValue(health), 100f));

            SetBase(asc, health, 100f);
            asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, sourceRequire, tags.Enraged));
            check("C-4 같은 GE, Spec Source 액터 태그에 Enraged를 넣고 적용 → 적용(Base 130)", Approx(asc.GetAttributeBaseValue(health), 130f));

            SetBase(asc, health, 100f);
            UnityEngine.Object.DestroyImmediate(targetRequire);
            UnityEngine.Object.DestroyImmediate(targetIgnore);
            UnityEngine.Object.DestroyImmediate(sourceRequire);
        }

        // ── C-2. 지속형(Infinite): CurrentValue에 조건 적용 (D21) ──
        private static void RunPersistentChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            SetBase(asc, health, 100f);

            GameplayEffectAsset targetRequire = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, null, MakeRequirements(new[] { tags.Enraged }, null)));
            ActiveGameplayEffectHandle h1 = asc.ApplyGameplayEffectToSelf(targetRequire);
            check("C-5 Infinite +50, Target Require Enraged, 소유 태그 빈 → CurrentValue 100(등록은 됨)", h1.IsValid && Approx(asc.GetAttributeCurrentValue(health), 100f));
            asc.RemoveActiveGameplayEffect(h1);

            GameplayEffectAsset targetIgnore = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, null, MakeRequirements(null, new[] { tags.Stunned })));
            ActiveGameplayEffectHandle h2 = asc.ApplyGameplayEffectToSelf(targetIgnore);
            check("C-6 Infinite +50, Target Ignore Stunned, 소유 태그 빈 → CurrentValue 150", Approx(asc.GetAttributeCurrentValue(health), 150f));
            asc.RemoveActiveGameplayEffect(h2);
            check("C-7 C-6 제거 → CurrentValue 100 복귀", Approx(asc.GetAttributeCurrentValue(health), 100f));

            GameplayEffectAsset sourceRequire = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, MakeRequirements(new[] { tags.Enraged }, null), null));
            ActiveGameplayEffectHandle h3 = asc.ApplyGameplayEffectToSelf(sourceRequire);
            check("C-8 Infinite +50, Source Require Enraged, Source 태그 빈 → CurrentValue 100", Approx(asc.GetAttributeCurrentValue(health), 100f));
            asc.RemoveActiveGameplayEffect(h3);

            ActiveGameplayEffectHandle h4 = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, sourceRequire, tags.Enraged));
            check("C-9 같은 GE, Spec Source에 Enraged → CurrentValue 150(UE 5.6이면 100, D21)", Approx(asc.GetAttributeCurrentValue(health), 150f));
            asc.RemoveActiveGameplayEffect(h4);
            check("C-10 C-9 제거 → CurrentValue 100 복귀", Approx(asc.GetAttributeCurrentValue(health), 100f));

            UnityEngine.Object.DestroyImmediate(targetRequire);
            UnityEngine.Object.DestroyImmediate(targetIgnore);
            UnityEngine.Object.DestroyImmediate(sourceRequire);
        }

        // ── C-3. 캡처 평가 문맥: CurrentValue와 다를 수 있음 (D21 트레이드오프 — 현재 설계 기록) ──
        // Health에 "Source가 Enraged일 때 +100" 버프를 건 뒤, Speed = Health.current(Target, non-snapshot) * 1 GE를 적용한다.
        // CurrentValue(Health)는 버프 GE 자신의 Source 태그로 판정해 200이지만, Speed GE의 캡처 평가는 Speed GE의 Source 태그로 판정한다.
        private static void RunCaptureContextChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);

            GameplayEffectAsset healthBuff = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 100f, MakeRequirements(new[] { tags.Enraged }, null), null));
            ActiveGameplayEffectHandle buffHandle = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, healthBuff, tags.Enraged));
            check("C-11 Health 버프(Source Require Enraged, Source에 Enraged) → Health CurrentValue 200", Approx(asc.GetAttributeCurrentValue(health), 200f));

            GameplayEffectAttributeCaptureDefinition captureHealth = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Target, false);
            GameplayEffectAsset speedFromHealth = MakeGameplayEffect(GameplayEffectType.Infinite, MakeAttributeBasedModifier(speed, captureHealth, 1f));

            ActiveGameplayEffectHandle plainSpeed = asc.ApplyGameplayEffectToSelf(speedFromHealth);
            check("C-12 [현재 설계] Speed GE(Source 태그 빈)가 Health를 캡처 → 버프 제외 → Speed 100 (Health CurrentValue는 200)", Approx(asc.GetAttributeCurrentValue(speed), 100f));
            asc.RemoveActiveGameplayEffect(plainSpeed);

            ActiveGameplayEffectHandle taggedSpeed = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, speedFromHealth, tags.Enraged));
            check("C-13 [현재 설계] Speed GE(Source에 Enraged)가 Health를 캡처 → 버프 포함 → Speed 200", Approx(asc.GetAttributeCurrentValue(speed), 200f));
            asc.RemoveActiveGameplayEffect(taggedSpeed);

            asc.RemoveActiveGameplayEffect(buffHandle);
            check("C-14 정리 → Health 100 / Speed 0", Approx(asc.GetAttributeCurrentValue(health), 100f) && Approx(asc.GetAttributeCurrentValue(speed), 0f));

            UnityEngine.Object.DestroyImmediate(healthBuff);
            UnityEngine.Object.DestroyImmediate(speedFromHealth);
        }

        // ── 헬퍼 ──

        // 소유 태그 입구가 없어 Source 태그를 Spec에 직접 넣는다. 적용 시 복제돼도 Source 태그는 그대로 복사된다
        private static GameplayEffectSpec MakeSpecWithSourceTag(AbilitySystemComponent asc, GameplayEffectAsset ge, GameplayTag sourceTag)
        {
            GameplayEffectSpec spec = asc.MakeOutgoingSpec(ge);
            TagContainerAggregator capturedSourceTags = spec.CapturedSourceTags;
            GameplayTagContainer sourceActorTags = capturedSourceTags.GetActorTags();
            sourceActorTags.AddTag(sourceTag);
            return spec;
        }

        private static GameplayTagContainer MakeContainer(GameplayTag tag)
        {
            GameplayTagContainer container = new GameplayTagContainer();
            container.AddTag(tag);
            return container;
        }

        private static GameplayTagRequirements MakeRequirements(GameplayTag[] requireTags, GameplayTag[] ignoreTags)
        {
            GameplayTagRequirements requirements = new GameplayTagRequirements();
            GameplayTagContainer requireContainer = requirements.RequireTags;
            GameplayTagContainer ignoreContainer = requirements.IgnoreTags;

            foreach (GameplayTag tag in requireTags ?? Array.Empty<GameplayTag>())
            {
                requireContainer.AddTag(tag);
            }

            foreach (GameplayTag tag in ignoreTags ?? Array.Empty<GameplayTag>())
            {
                ignoreContainer.AddTag(tag);
            }

            return requirements;
        }

        private static void AddCharacterSet(AbilitySystemComponent asc, float health)
        {
            CharacterAttributeSet set = new CharacterAttributeSet();
            set.health = new AttributeData(health, health);
            asc.AddSpawnedAttribute(set);
        }

        private static void SetBase(AbilitySystemComponent asc, GameplayAttributeHandle handle, float value)
        {
            asc.SetAttributeBaseValue(handle, value);
        }

        private static GameplayModifier MakeModifier(GameplayAttributeHandle handle, GameplayModifierOperation op, float magnitude, GameplayTagRequirements sourceTags, GameplayTagRequirements targetTags)
        {
            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(handle));
            SetPrivateField(boxed, "operation", op);
            SetPrivateField(boxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(boxed, "magnitude", magnitude);
            SetPrivateField(boxed, "sourceTags", sourceTags);
            SetPrivateField(boxed, "targetTags", targetTags);
            return (GameplayModifier)boxed;
        }

        private static GameplayModifier MakeAttributeBasedModifier(GameplayAttributeHandle target, GameplayEffectAttributeCaptureDefinition captureDef, float coefficient)
        {
            AttributeBasedMagnitude attributeBased = new AttributeBasedMagnitude();
            SetPrivateField(attributeBased, "backingAttribute", captureDef);
            SetPrivateField(attributeBased, "captureValueType", AttributeCaptureValueType.CurrentValue);
            SetPrivateField(attributeBased, "coefficient", coefficient);

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

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < Eps;
        }
    }
}
