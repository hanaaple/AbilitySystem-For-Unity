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
    // C: 실제 ASC에 GE 적용(소유 태그 빈 상태 — Instant 건너뛰기·지속형 CurrentValue·캡처 평가).
    // D: 소유 태그(Loose 태그) 개수·이벤트, 태그가 바뀌어도 지속형 modifier 조건은 CurrentValue에 쓰지 않음.
    // E: GE 종류별(Instant / period 0 Duration·Infinite / Period > 0) Base·Current 계산의 Source·Target 조건 판정을 UE 경로대로 본다.
    // Source 조건은 Source 태그를 Spec에 직접 넣어 본다.
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
                CharacterAttributeSet characterSet = AddCharacterSet(asc, 100f);

                RunOwnedTagsCheck(asc, Check);
                RunInstantChecks(asc, tags, Check);
                RunPersistentChecks(asc, tags, Check);
                RunCaptureContextChecks(asc, tags, Check);
                RunOwnedTagCountChecks(asc, tags, Check);
                RunOwnedTagRecalculationChecks(asc, characterSet, tags, Check);
                RunInstantTypeChecks(asc, tags, Check);
                RunPersistentTypeChecks(asc, tags, Check);
                RunPeriodicTypeChecks(asc, tags, Check);
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
            check("0-4 새 ASC의 소유 태그는 비어 있음(C는 이 상태를 전제로 한다)", ownedTags.IsEmpty());
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
            check("B-6 조건 없는 mod(기존 호출) → 어느 문맥이든 150", Approx(noReq.Evaluate(new AggregatorEvaluateParameters(withStunned, withStunned)), 150f) && Approx(noReq.Evaluate(default), 150f));

            // 여러 GE의 mod가 섞여 있어도 평가 문맥 하나로 모두 판정한다
            GameplayTagRequirements sourceRequireEnraged = MakeRequirements(new[] { tags.Enraged }, null);
            AttributeAggregator sourceRequire = new AttributeAggregator(100f);
            sourceRequire.AddAggregatorMod(h1, GameplayModifierOperation.AddBase, 10f, sourceRequireEnraged, null);
            sourceRequire.AddAggregatorMod(h2, GameplayModifierOperation.AddBase, 20f, sourceRequireEnraged, null);
            sourceRequire.AddAggregatorMod(h3, GameplayModifierOperation.AddBase, 40f, sourceRequireEnraged, null);
            check("B-10 Evaluate: Source Require Enraged mod 3개(GE 3개), 문맥 Source=Enraged → 170 / 빈 → 100", Approx(sourceRequire.Evaluate(new AggregatorEvaluateParameters(withEnraged, null)), 170f) && Approx(sourceRequire.Evaluate(default), 100f));

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

        // ── C-2. 지속형(Infinite): CurrentValue는 UE처럼 빈 문맥으로 계산 — Require가 있는 mod는 빠지고 Ignore만 있는 mod는 들어간다 ──
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
            check("C-9 같은 GE, Spec Source에 Enraged → CurrentValue 100(CurrentValue는 Source 태그를 보지 않음)", Approx(asc.GetAttributeCurrentValue(health), 100f));
            asc.RemoveActiveGameplayEffect(h4);
            check("C-10 C-9 제거 → CurrentValue 100 복귀", Approx(asc.GetAttributeCurrentValue(health), 100f));

            UnityEngine.Object.DestroyImmediate(targetRequire);
            UnityEngine.Object.DestroyImmediate(targetIgnore);
            UnityEngine.Object.DestroyImmediate(sourceRequire);
        }

        // ── C-3. 캡처 평가 문맥: 조건 mod는 캡처하는 GE의 태그로만 켜진다 ──
        // Health에 "Source가 Enraged일 때 +100" 버프를 건 뒤, Speed = Health.current(Target, non-snapshot) * 1 GE를 적용한다.
        // CurrentValue(Health)는 빈 문맥이라 100이고, Speed GE의 캡처 평가는 Speed GE의 Source 태그로 판정한다.
        private static void RunCaptureContextChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);

            GameplayEffectAsset healthBuff = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 100f, MakeRequirements(new[] { tags.Enraged }, null), null));
            ActiveGameplayEffectHandle buffHandle = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, healthBuff, tags.Enraged));
            check("C-11 Health 버프(Source Require Enraged, Source에 Enraged) → Health CurrentValue 100(빈 문맥)", Approx(asc.GetAttributeCurrentValue(health), 100f));

            GameplayEffectAttributeCaptureDefinition captureHealth = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Target, false);
            GameplayEffectAsset speedFromHealth = MakeGameplayEffect(GameplayEffectType.Infinite, MakeAttributeBasedModifier(speed, captureHealth, 1f));

            ActiveGameplayEffectHandle plainSpeed = asc.ApplyGameplayEffectToSelf(speedFromHealth);
            check("C-12 Speed GE(Source 태그 빈)가 Health를 캡처 → 버프 제외 → Speed 100",Approx(asc.GetAttributeCurrentValue(speed), 100f));
            asc.RemoveActiveGameplayEffect(plainSpeed);

            ActiveGameplayEffectHandle taggedSpeed = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, speedFromHealth, tags.Enraged));
            check("C-13 Speed GE(Source에 Enraged)가 Health를 캡처 → 버프 포함 → Speed 200",Approx(asc.GetAttributeCurrentValue(speed), 200f));
            asc.RemoveActiveGameplayEffect(taggedSpeed);

            asc.RemoveActiveGameplayEffect(buffHandle);
            check("C-14 정리 → Health 100 / Speed 0", Approx(asc.GetAttributeCurrentValue(health), 100f) && Approx(asc.GetAttributeCurrentValue(speed), 0f));

            UnityEngine.Object.DestroyImmediate(healthBuff);
            UnityEngine.Object.DestroyImmediate(speedFromHealth);
        }

        // ── D-1. 소유 태그 개수·이벤트 (GameplayTagCountContainer) ──
        private static void RunOwnedTagCountChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            asc.AddLooseGameplayTag(tags.Enraged);
            check("D-1 Loose Enraged 추가 → Enraged·부모 모두 매칭, 부모 개수 1", asc.HasMatchingGameplayTag(tags.Enraged) && asc.HasMatchingGameplayTag(tags.Parent) && asc.GetTagCount(tags.Parent) == 1);

            asc.AddLooseGameplayTag(tags.Enraged);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("D-2 Enraged 2회 추가·1회 제거 → 남음(개수 1)", asc.HasMatchingGameplayTag(tags.Enraged) && asc.GetTagCount(tags.Enraged) == 1);

            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("D-3 한 번 더 제거 → Enraged·부모 모두 없음, 소유 태그 빈", !asc.HasMatchingGameplayTag(tags.Enraged) && !asc.HasMatchingGameplayTag(tags.Parent) && asc.GetOwnedGameplayTags().IsEmpty());

            asc.AddLooseGameplayTag(tags.Enraged);
            asc.RemoveLooseGameplayTag(tags.Parent);
            check("D-4 부모로만 들어 있는 태그를 빼려 하면 무시(경고 1줄) → Enraged 그대로", asc.HasMatchingGameplayTag(tags.Enraged) && asc.GetTagCount(tags.Parent) == 1);
            asc.RemoveLooseGameplayTag(tags.Enraged);

            asc.SetLooseGameplayTagCount(tags.Stunned, 3);
            bool setThree = asc.GetTagCount(tags.Stunned) == 3;
            asc.SetLooseGameplayTagCount(tags.Stunned, 0);
            check("D-5 SetLooseGameplayTagCount 3 → 개수 3 / 0 → 없음", setThree && !asc.HasMatchingGameplayTag(tags.Stunned));

            // 부모 태그에 NewOrRemoved, 자식 태그에 AnyCountChange를 건다
            List<string> newOrRemovedLog = new List<string>();
            int anyChangeCalls = 0;
            OnGameplayEffectTagCountChanged onParentNewOrRemoved = (tag, newCount) => newOrRemovedLog.Add($"{tag}:{newCount}");
            OnGameplayEffectTagCountChanged onEnragedAnyChange = (tag, newCount) => anyChangeCalls++;
            asc.RegisterGameplayTagEvent(tags.Parent, onParentNewOrRemoved);
            asc.RegisterGameplayTagEvent(tags.Enraged, onEnragedAnyChange, GameplayTagEventType.AnyCountChange);

            asc.AddLooseGameplayTag(tags.Enraged);
            asc.AddLooseGameplayTag(tags.Enraged);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            string expectedLog = $"{tags.Parent}:1,{tags.Parent}:0";
            check($"D-6 Enraged 추가·추가·제거·제거 → 부모 NewOrRemoved 2회({expectedLog}) / Enraged AnyCountChange 4회 — 실제 {string.Join(",", newOrRemovedLog)} / {anyChangeCalls}회", string.Join(",", newOrRemovedLog) == expectedLog && anyChangeCalls == 4);

            bool unregistered = asc.UnregisterGameplayTagEvent(onParentNewOrRemoved, tags.Parent) && asc.UnregisterGameplayTagEvent(onEnragedAnyChange, tags.Enraged, GameplayTagEventType.AnyCountChange);
            asc.AddLooseGameplayTag(tags.Enraged);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("D-7 해제 → true, 이후 태그를 바꿔도 콜백 없음", unregistered && newOrRemovedLog.Count == 2 && anyChangeCalls == 4);
        }

        // ── D-2. 소유 태그가 바뀌어도 modifier 조건은 CurrentValue에 쓰지 않음(GE 단위 Ongoing Tag Requirements 담당) / Instant 적용 ──
        private static void RunOwnedTagRecalculationChecks(AbilitySystemComponent asc, CharacterAttributeSet characterSet, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            SetBase(asc, health, 100f);

            GameplayEffectAsset requireEnraged = MakeGameplayEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, null, MakeRequirements(new[] { tags.Enraged }, null)));
            ActiveGameplayEffectHandle h1 = asc.ApplyGameplayEffectToSelf(requireEnraged);
            asc.AddLooseGameplayTag(tags.Enraged);
            bool stillOff = Approx(asc.GetAttributeCurrentValue(health), 100f) && Approx(GetStoredHealth(characterSet), 100f);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("D-8 Infinite +50 Target Require Enraged: Enraged 추가해도 CurrentValue 100 (저장된 CurrentValue도)", stillOff && Approx(GetStoredHealth(characterSet), 100f));
            asc.RemoveActiveGameplayEffect(h1);

            asc.AddLooseGameplayTag(tags.Enraged);
            GameplayEffectAsset instantRequire = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, null, MakeRequirements(new[] { tags.Enraged }, null)));
            asc.ApplyGameplayEffectToSelf(instantRequire);
            check("D-14 Enraged 보유 중 Instant +30 Target Require Enraged → 적용(Base 130)", Approx(asc.GetAttributeBaseValue(health), 130f));
            asc.RemoveLooseGameplayTag(tags.Enraged);

            SetBase(asc, health, 100f);
            check("D-15 정리 → 소유 태그 빈, Health 100", asc.GetOwnedGameplayTags().IsEmpty() && Approx(asc.GetAttributeCurrentValue(health), 100f));

            // 태그만 주는 GE(Granted Tags만 설정)가 될 형태 — Modifier·Execution이 없어도 적용돼야 한다
            GameplayEffectAsset emptyInfinite = MakeGameplayEffect(GameplayEffectType.Infinite);
            ActiveGameplayEffectHandle emptyHandle = asc.ApplyGameplayEffectToSelf(emptyInfinite);
            bool emptyApplied = emptyHandle.IsValid;
            bool emptyRemoved = asc.RemoveActiveGameplayEffect(emptyHandle);
            check("D-16 Modifier·Execution 없는 Infinite GE → 적용(핸들 유효)·제거 true, Health 100 그대로", emptyApplied && emptyRemoved && Approx(asc.GetAttributeCurrentValue(health), 100f));
            UnityEngine.Object.DestroyImmediate(emptyInfinite);

            UnityEngine.Object.DestroyImmediate(requireEnraged);
            UnityEngine.Object.DestroyImmediate(instantRequire);
        }

        // ── E-1. Instant: 적용 순간 Base를 바꾼다. Target 조건은 그때의 소유 태그, Source 조건은 Spec의 Source 액터 태그로 판정 ──
        // UE: FActiveGameplayEffectsContainer::ExecuteActiveEffectsFrom (GameplayEffect.cpp:3081-3112)
        private static void RunInstantTypeChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayEffectAsset targetRequire = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, null, MakeRequirements(new[] { tags.Enraged }, null)));
            GameplayEffectAsset targetIgnore = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, null, MakeRequirements(null, new[] { tags.Stunned })));
            GameplayEffectAsset sourceRequire = MakeGameplayEffect(GameplayEffectType.Instant, MakeModifier(health, GameplayModifierOperation.AddBase, 30f, MakeRequirements(new[] { tags.Enraged }, null), null));

            SetBase(asc, health, 100f);
            asc.ApplyGameplayEffectToSelf(targetRequire);
            check("E-1 Instant +30 Target Require Enraged, 소유 태그 빈 → Base·Current 100", IsBaseAndCurrent(asc, health, 100f));

            SetBase(asc, health, 100f);
            asc.AddLooseGameplayTag(tags.Enraged);
            asc.ApplyGameplayEffectToSelf(targetRequire);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("E-2 같은 GE, 소유 태그 Enraged → Base·Current 130", IsBaseAndCurrent(asc, health, 130f));

            SetBase(asc, health, 100f);
            asc.AddLooseGameplayTag(tags.Stunned);
            asc.ApplyGameplayEffectToSelf(targetIgnore);
            asc.RemoveLooseGameplayTag(tags.Stunned);
            check("E-3 Instant +30 Target Ignore Stunned, 소유 태그 Stunned → Base·Current 100", IsBaseAndCurrent(asc, health, 100f));

            SetBase(asc, health, 100f);
            asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, sourceRequire, tags.Enraged));
            check("E-4 Instant +30 Source Require Enraged, Spec Source에 Enraged(소유 태그 빈) → Base·Current 130", IsBaseAndCurrent(asc, health, 130f));

            // 자신이 Instigator라 Spec을 만들 때의 소유 태그가 Source 태그가 된다 — 태그는 Spec을 만든 뒤에 넣는다
            SetBase(asc, health, 100f);
            GameplayEffectSpec specBeforeTag = asc.MakeOutgoingSpec(sourceRequire);
            asc.AddLooseGameplayTag(tags.Enraged);
            asc.ApplyGameplayEffectSpecToSelf(specBeforeTag);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("E-5 같은 GE, Spec 생성 뒤 소유 태그 Enraged(Source 태그 빈) → Base·Current 100(Source는 Spec 생성 때 태그)", IsBaseAndCurrent(asc, health, 100f));

            SetBase(asc, health, 100f);
            UnityEngine.Object.DestroyImmediate(targetRequire);
            UnityEngine.Object.DestroyImmediate(targetIgnore);
            UnityEngine.Object.DestroyImmediate(sourceRequire);
        }

        // ── E-2. period 0 Duration·Infinite: mod를 aggregator에 넣는다. Base는 그대로, CurrentValue는 빈 문맥으로 계산 ──
        // 조건 mod는 다른 GE가 이 어트리뷰트를 캡처해 평가할 때 그 Spec의 태그로만 켜진다.
        // UE: AddActiveGameplayEffectGrantedTagsAndModifiers(:4350)·OnAttributeAggregatorDirty(:3311-3360)·FAttributeBasedFloat::CalculateMagnitude(:983-984)
        private static void RunPersistentTypeChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            SetBase(asc, health, 100f);

            foreach (GameplayEffectType type in new[] { GameplayEffectType.Duration, GameplayEffectType.Infinite })
            {
                GameplayEffectAsset targetRequire = MakePersistentEffect(type, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, null, MakeRequirements(new[] { tags.Enraged }, null)));
                GameplayEffectAsset targetIgnore = MakePersistentEffect(type, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, null, MakeRequirements(null, new[] { tags.Stunned })));
                GameplayEffectAsset sourceRequire = MakePersistentEffect(type, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, MakeRequirements(new[] { tags.Enraged }, null), null));

                asc.AddLooseGameplayTag(tags.Enraged);
                ActiveGameplayEffectHandle h1 = asc.ApplyGameplayEffectToSelf(targetRequire);
                bool targetRequireOff = h1.IsValid && IsBaseAndCurrent(asc, health, 100f);
                asc.RemoveActiveGameplayEffect(h1);
                asc.RemoveLooseGameplayTag(tags.Enraged);
                check($"E-6 {type} +50 Target Require Enraged, 소유 태그 Enraged → Base 100·Current 100(빈 문맥이라 빠짐)", targetRequireOff);

                asc.AddLooseGameplayTag(tags.Stunned);
                ActiveGameplayEffectHandle h2 = asc.ApplyGameplayEffectToSelf(targetIgnore);
                bool targetIgnoreOn = Approx(asc.GetAttributeBaseValue(health), 100f) && Approx(asc.GetAttributeCurrentValue(health), 150f);
                asc.RemoveActiveGameplayEffect(h2);
                asc.RemoveLooseGameplayTag(tags.Stunned);
                check($"E-7 {type} +50 Target Ignore Stunned, 소유 태그 Stunned → Base 100·Current 150(빈 문맥이라 Ignore 통과)", targetIgnoreOn);

                ActiveGameplayEffectHandle h3 = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, sourceRequire, tags.Enraged));
                bool sourceRequireOff = IsBaseAndCurrent(asc, health, 100f);
                asc.RemoveActiveGameplayEffect(h3);
                check($"E-8 {type} +50 Source Require Enraged, Spec Source에 Enraged → Base 100·Current 100(빈 문맥이라 빠짐)", sourceRequireOff);

                UnityEngine.Object.DestroyImmediate(targetRequire);
                UnityEngine.Object.DestroyImmediate(targetIgnore);
                UnityEngine.Object.DestroyImmediate(sourceRequire);
            }

            // 캡처 평가: Speed += Health.current × 1(Target 캡처·non-snapshot). Speed GE의 Target 태그 = 적용 때 소유 태그
            SetBase(asc, speed, 0f);
            GameplayEffectAsset healthBuff = MakePersistentEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 50f, null, MakeRequirements(new[] { tags.Enraged }, null)));
            GameplayEffectAttributeCaptureDefinition captureHealth = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Target, false);
            GameplayEffectAsset speedFromHealth = MakePersistentEffect(GameplayEffectType.Infinite, MakeAttributeBasedModifier(speed, captureHealth, 1f));
            ActiveGameplayEffectHandle buffHandle = asc.ApplyGameplayEffectToSelf(healthBuff);

            ActiveGameplayEffectHandle plainSpeed = asc.ApplyGameplayEffectToSelf(speedFromHealth);
            check("E-9 Health 버프(Target Require Enraged) + Speed GE 적용 때 소유 태그 빈 → Health Current 100, Speed 100(캡처 평가도 버프 제외)", Approx(asc.GetAttributeCurrentValue(health), 100f) && Approx(asc.GetAttributeCurrentValue(speed), 100f));
            asc.RemoveActiveGameplayEffect(plainSpeed);

            asc.AddLooseGameplayTag(tags.Enraged);
            ActiveGameplayEffectHandle taggedSpeed = asc.ApplyGameplayEffectToSelf(speedFromHealth);
            check("E-10 같은 버프 + Speed GE 적용 때 소유 태그 Enraged → Health Current 100, Speed 150(캡처 평가는 Speed GE의 Target 태그로 판정)", Approx(asc.GetAttributeCurrentValue(health), 100f) && Approx(asc.GetAttributeCurrentValue(speed), 150f));
            asc.RemoveActiveGameplayEffect(taggedSpeed);
            asc.RemoveLooseGameplayTag(tags.Enraged);

            asc.RemoveActiveGameplayEffect(buffHandle);
            SetBase(asc, health, 100f);
            SetBase(asc, speed, 0f);
            UnityEngine.Object.DestroyImmediate(healthBuff);
            UnityEngine.Object.DestroyImmediate(speedFromHealth);
        }

        // ── E-3. Period > 0: 주기마다 Base를 바꾼다(aggregator mod 없음). 실행마다 소유 태그를 다시 읽어 Target 조건을 판정 ──
        // UE: ExecuteActiveEffectsFrom(:3081-3112) — 주기 실행도 Instant와 같은 경로
        private static void RunPeriodicTypeChecks(AbilitySystemComponent asc, TestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            ActiveGameplayEffectsContainer container = GetContainer(asc);

            foreach (GameplayEffectType type in new[] { GameplayEffectType.Duration, GameplayEffectType.Infinite })
            {
                SetBase(asc, health, 100f);
                GameplayEffectAsset targetRequire = MakePeriodicEffect(type, MakeModifier(health, GameplayModifierOperation.AddBase, 10f, null, MakeRequirements(new[] { tags.Enraged }, null)));
                ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectToSelf(targetRequire);
                bool skippedOnApply = IsBaseAndCurrent(asc, health, 100f);
                asc.AddLooseGameplayTag(tags.Enraged);
                container.Tick(1f);
                bool appliedWithTag = IsBaseAndCurrent(asc, health, 110f);
                asc.RemoveLooseGameplayTag(tags.Enraged);
                container.Tick(1f);
                bool skippedAfterRemove = IsBaseAndCurrent(asc, health, 110f);
                asc.RemoveActiveGameplayEffect(handle);
                check($"E-11 {type} Period 1 +10 Target Require Enraged: 적용 실행(태그 없음) 100 → Enraged 추가 후 주기 110 → 제거 후 주기 110 (Base·Current)", skippedOnApply && appliedWithTag && skippedAfterRemove);
                UnityEngine.Object.DestroyImmediate(targetRequire);
            }

            SetBase(asc, health, 100f);
            GameplayEffectAsset targetIgnore = MakePeriodicEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 10f, null, MakeRequirements(null, new[] { tags.Stunned })));
            ActiveGameplayEffectHandle ignoreHandle = asc.ApplyGameplayEffectToSelf(targetIgnore);
            bool ignoreOnApply = IsBaseAndCurrent(asc, health, 110f);
            asc.AddLooseGameplayTag(tags.Stunned);
            container.Tick(1f);
            bool ignoreBlocked = IsBaseAndCurrent(asc, health, 110f);
            asc.RemoveLooseGameplayTag(tags.Stunned);
            container.Tick(1f);
            bool ignoreAgain = IsBaseAndCurrent(asc, health, 120f);
            asc.RemoveActiveGameplayEffect(ignoreHandle);
            check("E-12 Infinite Period 1 +10 Target Ignore Stunned: 적용 실행 110 → Stunned 추가 후 주기 110 → 제거 후 주기 120", ignoreOnApply && ignoreBlocked && ignoreAgain);

            GameplayEffectAsset sourceRequire = MakePeriodicEffect(GameplayEffectType.Infinite, MakeModifier(health, GameplayModifierOperation.AddBase, 10f, MakeRequirements(new[] { tags.Enraged }, null), null));

            SetBase(asc, health, 100f);
            ActiveGameplayEffectHandle withSource = asc.ApplyGameplayEffectSpecToSelf(MakeSpecWithSourceTag(asc, sourceRequire, tags.Enraged));
            container.Tick(1f);
            bool sourceOn = IsBaseAndCurrent(asc, health, 120f);
            asc.RemoveActiveGameplayEffect(withSource);
            check("E-13 Infinite Period 1 +10 Source Require Enraged, Spec Source에 Enraged → 적용 실행 + 주기 1회 = 120", sourceOn);

            SetBase(asc, health, 100f);
            GameplayEffectSpec specBeforeTag = asc.MakeOutgoingSpec(sourceRequire);
            asc.AddLooseGameplayTag(tags.Enraged);
            ActiveGameplayEffectHandle withoutSource = asc.ApplyGameplayEffectSpecToSelf(specBeforeTag);
            container.Tick(1f);
            bool sourceOff = IsBaseAndCurrent(asc, health, 100f);
            asc.RemoveActiveGameplayEffect(withoutSource);
            asc.RemoveLooseGameplayTag(tags.Enraged);
            check("E-14 같은 GE, Spec 생성 뒤 소유 태그 Enraged(Source 태그 빈) → 적용 실행 + 주기 1회 모두 건너뜀 = 100(주기마다 Source를 다시 캡처하지 않음)", sourceOff);

            SetBase(asc, health, 100f);
            check("E-15 정리 → 소유 태그 빈, Health Base·Current 100", asc.GetOwnedGameplayTags().IsEmpty() && IsBaseAndCurrent(asc, health, 100f));
            UnityEngine.Object.DestroyImmediate(targetIgnore);
            UnityEngine.Object.DestroyImmediate(sourceRequire);
        }

        // ── 헬퍼 ──

        private static bool IsBaseAndCurrent(AbilitySystemComponent asc, GameplayAttributeHandle handle, float expected)
        {
            return Approx(asc.GetAttributeBaseValue(handle), expected) && Approx(asc.GetAttributeCurrentValue(handle), expected);
        }

        // Duration은 시간이 남아 있어야 Tick 중에 만료되지 않는다
        private static GameplayEffectAsset MakePersistentEffect(GameplayEffectType type, GameplayModifier modifier)
        {
            GameplayEffectAsset ge = MakeGameplayEffect(type, modifier);
            SetPrivateField(ge, "duration", 100f);
            return ge;
        }

        private static GameplayEffectAsset MakePeriodicEffect(GameplayEffectType type, GameplayModifier modifier)
        {
            GameplayEffectAsset ge = MakePersistentEffect(type, modifier);
            SetPrivateField(ge, "period", 1f);
            SetPrivateField(ge, "executePeriodicEffectOnApplication", true);
            return ge;
        }

        private static ActiveGameplayEffectsContainer GetContainer(AbilitySystemComponent asc)
        {
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", NonPublicInstance);
            return (ActiveGameplayEffectsContainer)containerField.GetValue(asc);
        }

        // GetAttributeCurrentValue는 조회 때마다 다시 계산하므로, 저장된 CurrentValue는 AttributeSet의 값을 직접 본다
        private static float GetStoredHealth(CharacterAttributeSet characterSet)
        {
            return characterSet.health.GetCurrentValue();
        }

        // Source 태그는 GE를 만들 때 Instigator에서 캡처하므로, 테스트에선 Spec에 직접 넣는다. 적용 시 복제돼도 Source 태그는 그대로 복사된다
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

        private static CharacterAttributeSet AddCharacterSet(AbilitySystemComponent asc, float health)
        {
            CharacterAttributeSet set = new CharacterAttributeSet();
            set.health = new AttributeData(health, health);
            asc.AddSpawnedAttribute(set);
            return set;
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
            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);
            SetPrivateField(boxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);
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

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < Eps;
        }
    }
}
