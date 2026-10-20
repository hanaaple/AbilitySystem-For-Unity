using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.GameplayEffectComponents;
using Core.AbilitySystem.Tag;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 GE Component 호출(이벤트 함수)과 GE 켜고 끄기(inhibit)가 UE 5.6처럼 동작하는지 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // 호출을 기록하는 테스트 전용 Component(RecordingComponent)를 GE에 넣어 본다.
    // A: 호출 순서·인자 / B: CanApply 차단 / C: OnAddedToActiveContainer 결과와 inhibit / D: 꺼진 주기 GE
    // E: Asset Tags Component — 상속 태그 계산(InheritedTagContainer), GE 캐시, Spec 태그, 판정 경로
    // F: Granted Tags / G: Target Tag Requirements / H: GameplayEffectQuery·ASC 질의·제거 API / I: Remove Other Component
    public static class GameplayEffectComponentSelfCheck
    {
        private const float Eps = 0.001f;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [NativeTag] private static readonly NativeGameplayTag GrantedTag = new("Test.GEComponent.Granted");
        [NativeTag] private static readonly NativeGameplayTag AssetParentTag = new("Test.GEComponent.Asset");
        [NativeTag] private static readonly NativeGameplayTag AssetChildTag = new("Test.GEComponent.Asset.Child");
        [NativeTag] private static readonly NativeGameplayTag OtherTag = new("Test.GEComponent.Other");

        [MenuItem("Tools/Ability System/Run GE Component Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[GEComponent PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[GEComponent FAIL] {label}");
                }
            }

            GameplayTag grantedTag = GrantedTag.GetTag();
            var assetTags = new AssetTestTags(AssetParentTag.GetTag(), AssetChildTag.GetTag(), OtherTag.GetTag());
            Check("0-1 native 태그 Test.GEComponent.Granted 유효", grantedTag.IsValid());
            Check("0-2 native 태그 Test.GEComponent.Asset·Asset.Child·Other 유효", assetTags.IsValid());
            if (!grantedTag.IsValid() || !assetTags.IsValid())
            {
                Debug.LogError($"[GEComponent Self-Check] 테스트 태그 준비 실패로 중단 — passed {passed} / failed {failed}");
                return;
            }

            GameObject go = new GameObject("GameplayEffectComponentSelfCheck_Temp");
            try
            {
                AbilitySystemComponent asc = go.AddComponent<AbilitySystemComponent>();
                EnsureInitialized(asc);
                AddCharacterSet(asc, 100f);

                RunCallOrderChecks(asc, Check);
                RunCanApplyChecks(asc, Check);
                RunInhibitChecks(asc, grantedTag, Check);
                RunPeriodicInhibitChecks(asc, Check);
                RunInheritedTagContainerChecks(assetTags, Check);
                RunAssetTagsChecks(asc, assetTags, Check);
                RunGrantedTagsChecks(asc, assetTags, Check);
                RunTargetTagRequirementsChecks(asc, assetTags, Check);
                RunEffectQueryChecks(asc, assetTags, Check);
                RunRemoveOtherChecks(asc, assetTags, Check);
            }
            catch (Exception e)
            {
                Check($"ASC 검사 중 예외: {e.GetType().Name} — {e.Message}", false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            Debug.Log($"[GEComponent Self-Check] passed {passed} / failed {failed}");
        }

        // 받은 호출을 "이름.함수" 꼴로 공유 로그에 남기고, 인자를 들고 있는 테스트 전용 Component
        [Serializable]
        private sealed class RecordingComponent : GameplayEffectComponent
        {
            public bool AllowApply = true;
            public bool ShouldBeActive = true;

            public GameplayEffectSpec CanApplySpec;
            public ActiveGameplayEffect AddedActive;
            public bool AddedWasInContainer;
            public GameplayEffectSpec ExecutedSpec;
            public GameplayEffectSpec AppliedSpec;
            public readonly List<ActiveGameplayEffectsContainer> Containers = new();

            private readonly string _name;
            private readonly List<string> _log;

            public RecordingComponent(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public override bool CanGameplayEffectApply(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
            {
                _log.Add($"{_name}.CanApply");
                Containers.Add(activeGameplayEffectsContainer);
                CanApplySpec = spec;
                return AllowApply;
            }

            public override bool OnActiveGameplayEffectAdded(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, ActiveGameplayEffect activeGameplayEffect)
            {
                _log.Add($"{_name}.Added");
                Containers.Add(activeGameplayEffectsContainer);
                AddedActive = activeGameplayEffect;
                IReadOnlyDictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> activeEffects = activeGameplayEffectsContainer.ActiveGameplayEffects;
                AddedWasInContainer = activeEffects.ContainsKey(activeGameplayEffect.Handle);
                return ShouldBeActive;
            }

            public override void OnGameplayEffectExecuted(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
            {
                _log.Add($"{_name}.Executed");
                Containers.Add(activeGameplayEffectsContainer);
                ExecutedSpec = spec;
            }

            public override void OnGameplayEffectApplied(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
            {
                _log.Add($"{_name}.Applied");
                Containers.Add(activeGameplayEffectsContainer);
                AppliedSpec = spec;
            }

            public override void OnGameplayEffectChanged()
            {
                _log.Add($"{_name}.Changed");
            }
        }

        // ── A. 호출 순서·인자 ──
        private static void RunCallOrderChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            ActiveGameplayEffectsContainer container = GetContainer(asc);
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            List<string> log = new List<string>();

            // Instant
            RecordingComponent instantRecorder = new RecordingComponent("R", log);
            GameplayEffectAsset instant = MakeGameplayEffect(GameplayEffectType.Instant, new GameplayEffectComponent[] { instantRecorder }, MakeModifier(health, 30f));
            GameplayEffectSpec instantSpec = asc.MakeOutgoingSpec(instant);
            ActiveGameplayEffectHandle instantHandle = asc.ApplyGameplayEffectSpecToSelf(instantSpec);

            check("A-1 Instant: CanApply → Executed → Applied", IsLog(log, "R.CanApply", "R.Executed", "R.Applied"));
            check("A-2 Instant: Invalid 핸들, Base 130", !instantHandle.IsValid && IsBaseAndCurrent(asc, health, 130f));
            check("A-3 CanApply는 넘긴 Spec 원본을 받고, 모든 호출이 이 ASC의 컨테이너를 받는다", ReferenceEquals(instantRecorder.CanApplySpec, instantSpec) && AllSame(instantRecorder.Containers, container));
            check("A-4 Executed·Applied는 적용 때 만든 같은 복사본을 받는다", instantRecorder.ExecutedSpec != null && ReferenceEquals(instantRecorder.ExecutedSpec, instantRecorder.AppliedSpec) && !ReferenceEquals(instantRecorder.AppliedSpec, instantSpec));
            SetBase(asc, health, 100f);

            // Infinite(period 0)
            log.Clear();
            RecordingComponent infiniteRecorder = new RecordingComponent("R", log);
            GameplayEffectAsset infinite = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { infiniteRecorder }, MakeModifier(health, 50f));
            ActiveGameplayEffectHandle infiniteHandle = asc.ApplyGameplayEffectToSelf(infinite);
            ActiveGameplayEffect addedActive = infiniteRecorder.AddedActive;

            check("A-5 Infinite(period 0): CanApply → Added → Applied, Executed 없음", IsLog(log, "R.CanApply", "R.Added", "R.Applied"));
            check("A-6 Added는 반환 핸들의 활성 GE를, 이미 활성 목록에 든 채로 받는다", addedActive != null && addedActive.Handle.Equals(infiniteHandle) && infiniteRecorder.AddedWasInContainer);
            check("A-7 Added가 true면 켜진 채 시작: IsInhibited false, Current 150", addedActive != null && !addedActive.IsInhibited && Approx(asc.GetAttributeCurrentValue(health), 150f));

            log.Clear();
            bool removed = asc.RemoveActiveGameplayEffect(infiniteHandle);
            check("A-8 제거: Component 호출 없음(UE에 제거 훅 없음), Current 100", removed && log.Count == 0 && Approx(asc.GetAttributeCurrentValue(health), 100f));

            // Periodic(적용 시 실행)
            log.Clear();
            RecordingComponent periodicRecorder = new RecordingComponent("R", log);
            GameplayEffectAsset periodic = MakePeriodicEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { periodicRecorder }, MakeModifier(health, 10f));
            ActiveGameplayEffectHandle periodicHandle = asc.ApplyGameplayEffectToSelf(periodic);

            check("A-9 Periodic(적용 시 실행): CanApply → Added → Applied → Executed, Base 110", IsLog(log, "R.CanApply", "R.Added", "R.Applied", "R.Executed") && IsBaseAndCurrent(asc, health, 110f));

            log.Clear();
            container.Tick(1f);
            check("A-10 주기 1회: Executed만, Base 120", IsLog(log, "R.Executed") && IsBaseAndCurrent(asc, health, 120f));

            asc.RemoveActiveGameplayEffect(periodicHandle);
            SetBase(asc, health, 100f);

            // Component 여러 개 + 타입을 고르지 않은 빈 칸
            log.Clear();
            RecordingComponent first = new RecordingComponent("R1", log);
            RecordingComponent second = new RecordingComponent("R2", log);
            GameplayEffectAsset multi = MakeGameplayEffect(GameplayEffectType.Instant, new GameplayEffectComponent[] { first, null, second });
            asc.ApplyGameplayEffectToSelf(multi);

            check("A-11 Component 여러 개·빈 칸: 함수마다 목록 순서대로 부르고 빈 칸은 건너뛴다", IsLog(log, "R1.CanApply", "R2.CanApply", "R1.Executed", "R2.Executed", "R1.Applied", "R2.Applied"));

            log.Clear();
            multi.OnGameplayEffectChanged();
            check("A-12 GE의 OnGameplayEffectChanged가 Component마다 전달된다", IsLog(log, "R1.Changed", "R2.Changed"));

            UnityEngine.Object.DestroyImmediate(instant);
            UnityEngine.Object.DestroyImmediate(infinite);
            UnityEngine.Object.DestroyImmediate(periodic);
            UnityEngine.Object.DestroyImmediate(multi);
        }

        // ── B. CanApply 차단 ──
        private static void RunCanApplyChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            ActiveGameplayEffectsContainer container = GetContainer(asc);
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            List<string> log = new List<string>();

            RecordingComponent blocker = new RecordingComponent("B1", log) { AllowApply = false };
            RecordingComponent after = new RecordingComponent("B2", log);
            GameplayEffectAsset instant = MakeGameplayEffect(GameplayEffectType.Instant, new GameplayEffectComponent[] { blocker, after }, MakeModifier(health, 30f));
            ActiveGameplayEffectHandle instantHandle = asc.ApplyGameplayEffectToSelf(instant);

            check("B-1 Instant CanApply false: 적용 안 됨(Base 100), 뒤 Component의 CanApply를 포함해 다른 호출 없음", !instantHandle.IsValid && IsLog(log, "B1.CanApply") && IsBaseAndCurrent(asc, health, 100f));

            log.Clear();
            int activeCountBefore = container.ActiveGameplayEffects.Count;
            GameplayEffectAsset infinite = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { blocker }, MakeModifier(health, 50f));
            ActiveGameplayEffectHandle infiniteHandle = asc.ApplyGameplayEffectToSelf(infinite);

            check("B-2 Infinite CanApply false: Invalid 핸들, 활성 목록에 안 들어감, Added·Applied 없음, Current 100", !infiniteHandle.IsValid && container.ActiveGameplayEffects.Count == activeCountBefore && IsLog(log, "B1.CanApply") && Approx(asc.GetAttributeCurrentValue(health), 100f));

            UnityEngine.Object.DestroyImmediate(instant);
            UnityEngine.Object.DestroyImmediate(infinite);
        }

        // ── C. OnAddedToActiveContainer 결과와 inhibit ──
        private static void RunInhibitChecks(AbilitySystemComponent asc, GameplayTag grantedTag, Action<string, bool> check)
        {
            ActiveGameplayEffectsContainer container = GetContainer(asc);
            IReadOnlyDictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect> activeEffects = container.ActiveGameplayEffects;
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            List<string> log = new List<string>();

            RecordingComponent off = new RecordingComponent("C1", log) { ShouldBeActive = false };
            RecordingComponent on = new RecordingComponent("C2", log);
            GameplayEffectAsset ge = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { off, on }, MakeModifier(health, 50f));
            GrantTag(ge, grantedTag);
            ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectToSelf(ge);

            check("C-1 한 Component가 Added에서 false여도 나머지 Component의 Added를 부른다", IsLog(log, "C1.CanApply", "C2.CanApply", "C1.Added", "C2.Added", "C1.Applied", "C2.Applied"));

            bool isActive = activeEffects.TryGetValue(handle, out ActiveGameplayEffect active);
            check("C-2 꺼진 채 시작: 핸들 유효, 활성 목록에 있음, IsInhibited true", handle.IsValid && isActive && active.IsInhibited);
            check("C-3 꺼진 GE는 modifier·부여 태그를 걸지 않는다: Current 100, 태그 0", IsCurrentAndTag(asc, health, 100f, grantedTag, 0));

            ActiveGameplayEffectHandle returned = asc.SetActiveGameplayEffectInhibit(handle, false);
            check("C-4 켜기: 같은 핸들 반환, IsInhibited false, Current 150, 태그 1", returned.Equals(handle) && isActive && !active.IsInhibited && IsCurrentAndTag(asc, health, 150f, grantedTag, 1));

            returned = asc.SetActiveGameplayEffectInhibit(handle, false);
            check("C-5 켜진 GE를 다시 켜기: 같은 핸들, 두 번 걸지 않음(Current 150, 태그 1)", returned.Equals(handle) && IsCurrentAndTag(asc, health, 150f, grantedTag, 1));

            returned = asc.SetActiveGameplayEffectInhibit(handle, true);
            check("C-6 끄기: 제거되지 않고 효과만 거둠(활성 목록에 있음, Current 100, 태그 0)", returned.Equals(handle) && activeEffects.ContainsKey(handle) && active.IsInhibited && IsCurrentAndTag(asc, health, 100f, grantedTag, 0));

            // 같은 태그를 Loose로 1개 넣어 두면, 꺼진 GE 제거가 태그를 한 번 더 빼는지 개수로 드러난다
            asc.AddLooseGameplayTag(grantedTag);
            log.Clear();
            bool removed = asc.RemoveActiveGameplayEffect(handle);
            check("C-7 꺼진 GE 제거: true, 거둔 효과를 또 거두지 않음(Loose 태그 1개 그대로, Current 100), Component 호출 없음", removed && !activeEffects.ContainsKey(handle) && IsCurrentAndTag(asc, health, 100f, grantedTag, 1) && log.Count == 0);
            asc.RemoveLooseGameplayTag(grantedTag);

            // 켜진 채 시작한 GE의 부여 태그
            RecordingComponent always = new RecordingComponent("C3", log);
            GameplayEffectAsset granting = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { always }, MakeModifier(health, 50f));
            GrantTag(granting, grantedTag);
            ActiveGameplayEffectHandle grantingHandle = asc.ApplyGameplayEffectToSelf(granting);
            bool grantedWhileActive = IsCurrentAndTag(asc, health, 150f, grantedTag, 1);
            asc.RemoveActiveGameplayEffect(grantingHandle);
            check("C-8 켜진 GE: 부여 태그 1·Current 150 → 제거 후 0·100", grantedWhileActive && IsCurrentAndTag(asc, health, 100f, grantedTag, 0));

            // 켜는 중 부여 태그 이벤트가 GE를 제거하는 경우(UE는 Invalid 핸들로 알린다)
            RecordingComponent offAgain = new RecordingComponent("C4", log) { ShouldBeActive = false };
            GameplayEffectAsset selfRemoving = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { offAgain }, MakeModifier(health, 50f));
            GrantTag(selfRemoving, grantedTag);
            ActiveGameplayEffectHandle selfRemovingHandle = asc.ApplyGameplayEffectToSelf(selfRemoving);

            void RemoveOnGranted(GameplayTag tag, int newCount)
            {
                if (newCount > 0)
                {
                    asc.RemoveActiveGameplayEffect(selfRemovingHandle);
                }
            }

            asc.RegisterGameplayTagEvent(grantedTag, RemoveOnGranted);
            returned = asc.SetActiveGameplayEffectInhibit(selfRemovingHandle, false);
            asc.UnregisterGameplayTagEvent(RemoveOnGranted, grantedTag);
            check("C-9 켜는 중 태그 이벤트가 GE를 제거: Invalid 반환, 활성 목록에 없음, 태그 0, Current 100", !returned.IsValid && !activeEffects.ContainsKey(selfRemovingHandle) && IsCurrentAndTag(asc, health, 100f, grantedTag, 0));

            UnityEngine.Object.DestroyImmediate(ge);
            UnityEngine.Object.DestroyImmediate(granting);
            UnityEngine.Object.DestroyImmediate(selfRemoving);
        }

        // ── D. 꺼진 주기 GE ──
        private static void RunPeriodicInhibitChecks(AbilitySystemComponent asc, Action<string, bool> check)
        {
            ActiveGameplayEffectsContainer container = GetContainer(asc);
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            List<string> log = new List<string>();

            RecordingComponent off = new RecordingComponent("D1", log) { ShouldBeActive = false };
            GameplayEffectAsset periodic = MakePeriodicEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { off }, MakeModifier(health, 10f));
            ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectToSelf(periodic);

            check("D-1 꺼진 채 시작한 주기 GE: Applied는 부르지만 적용 시 실행은 안 함(Base 100)", IsLog(log, "D1.CanApply", "D1.Added", "D1.Applied") && IsBaseAndCurrent(asc, health, 100f));

            log.Clear();
            container.Tick(1f);
            check("D-2 꺼진 동안 주기가 와도 실행 안 함(Executed 없음, Base 100)", log.Count == 0 && IsBaseAndCurrent(asc, health, 100f));

            asc.SetActiveGameplayEffectInhibit(handle, false);
            container.Tick(1f);
            check("D-3 켠 뒤 주기: Executed, Base 110", IsLog(log, "D1.Executed") && IsBaseAndCurrent(asc, health, 110f));

            asc.RemoveActiveGameplayEffect(handle);
            SetBase(asc, health, 100f);

            UnityEngine.Object.DestroyImmediate(periodic);
        }

        private readonly struct AssetTestTags
        {
            public readonly GameplayTag Parent;
            public readonly GameplayTag Child;
            public readonly GameplayTag Other;

            public AssetTestTags(GameplayTag parent, GameplayTag child, GameplayTag other)
            {
                Parent = parent;
                Child = child;
                Other = other;
            }

            public bool IsValid()
            {
                return Parent.IsValid() && Child.IsValid() && Other.IsValid();
            }
        }

        // ── E. Asset Tags: 상속 태그 계산 ──
        private static void RunInheritedTagContainerChecks(AssetTestTags tags, Action<string, bool> check)
        {
            InheritedTagContainer noParent = new InheritedTagContainer();
            noParent.Added.AddTag(tags.Child);
            noParent.Added.AddTag(tags.Other);
            noParent.UpdateInheritedTagProperties(null);
            check("E-1 부모 없음: Combined = Added(Asset.Child, Other)", IsExactly(noParent.CombinedTags, tags.Child, tags.Other));

            InheritedTagContainer parent = new InheritedTagContainer();
            parent.AddTag(tags.Child);
            parent.AddTag(tags.Other);

            InheritedTagContainer removeByParent = new InheritedTagContainer();
            removeByParent.Removed.AddTag(tags.Parent);
            removeByParent.UpdateInheritedTagProperties(parent);
            check("E-2 부모 {Asset.Child, Other}, Removed {Asset}: 부모 태그는 계층으로 빠진다 → {Other}", IsExactly(removeByParent.CombinedTags, tags.Other));

            InheritedTagContainer removeBlocksExactAdd = new InheritedTagContainer();
            removeBlocksExactAdd.Removed.AddTag(tags.Parent);
            removeBlocksExactAdd.Added.AddTag(tags.Parent);
            removeBlocksExactAdd.Added.AddTag(tags.Child);
            removeBlocksExactAdd.UpdateInheritedTagProperties(parent);
            check("E-3 같은 부모, Removed {Asset}, Added {Asset, Asset.Child}: Removed와 정확히 같은 Added만 막힌다 → {Other, Asset.Child}", IsExactly(removeBlocksExactAdd.CombinedTags, tags.Other, tags.Child));

            InheritedTagContainer addRemove = new InheritedTagContainer();
            addRemove.RemoveTag(tags.Other);
            addRemove.AddTag(tags.Other);
            check("E-4 AddTag·RemoveTag: 같은 태그를 빼다가 더하면 Removed에서 지워지고 Added·Combined에 들어간다", addRemove.Added.HasTagExact(tags.Other) && !addRemove.Removed.HasTagExact(tags.Other) && addRemove.CombinedTags.HasTagExact(tags.Other));

            InheritedTagContainer mods = new InheritedTagContainer();
            mods.Added.AddTag(tags.Parent);
            mods.Removed.AddTag(tags.Parent);
            mods.Removed.AddTag(tags.Other);
            mods.UpdateInheritedTagProperties(null);
            GameplayTagContainer target = new GameplayTagContainer();
            target.AddTag(tags.Child);
            target.AddTag(tags.Other);
            mods.ApplyTo(target);
            check("E-5 ApplyTo(기존 {Asset.Child, Other}), Added {Asset}, Removed {Asset, Other}: Other는 빠지고 Removed와 같은 Added는 안 들어간다 → {Asset.Child}", IsExactly(target, tags.Child));

            InheritedTagContainer fast = new InheritedTagContainer();
            fast.AddTag(tags.Other);
            GameplayTagContainer empty = new GameplayTagContainer();
            fast.ApplyTo(empty);
            check("E-6 ApplyTo(빈 컨테이너): Combined를 그대로 쓴다 → {Other}", IsExactly(empty, tags.Other));
        }

        // ── E. Asset Tags: GE 캐시·Spec·판정 경로 ──
        private static void RunAssetTagsChecks(AbilitySystemComponent asc, AssetTestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;

            AssetTagsGameplayEffectComponent assetTagsComponent = MakeAssetTagsComponent(tags.Child, tags.Other);
            GameplayEffectAsset ge = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { assetTagsComponent }, MakeModifier(health, 50f));
            ge.OnGameplayEffectChanged();

            check("E-7 OnGameplayEffectChanged: Component의 GetOwner()가 그 GE", ReferenceEquals(assetTagsComponent.GetOwner(), ge));
            check("E-8 GE GetAssetTags = Component 설정 {Asset.Child, Other}", IsExactly(ge.GetAssetTags(), tags.Child, tags.Other));

            ge.OnGameplayEffectChanged();
            check("E-9 OnGameplayEffectChanged 두 번: 캐시를 비우고 다시 모아 중복 없음", IsExactly(ge.GetAssetTags(), tags.Child, tags.Other));

            GameplayEffectSpec spec = asc.MakeOutgoingSpec(ge);
            TagContainerAggregator capturedSourceTags = spec.CapturedSourceTags;
            GameplayTagContainer specTags = capturedSourceTags.GetSpecTags();
            GameplayTagContainer actorTags = capturedSourceTags.GetActorTags();
            GameplayTagContainer aggregatedTags = capturedSourceTags.GetAggregatedTags();
            check("E-10 Spec 생성: Asset Tags가 Source의 Spec 태그·합친 태그에 들어가고 액터 태그엔 없다", specTags.HasTagExact(tags.Other) && specTags.HasTagExact(tags.Child) && aggregatedTags.HasTagExact(tags.Other) && !actorTags.HasTagExact(tags.Other));

            GameplayTagContainer allAssetTags = new GameplayTagContainer();
            spec.GetAllAssetTags(allAssetTags);
            check("E-11 Spec.GetAllAssetTags = {Asset.Child, Other}", IsExactly(allAssetTags, tags.Child, tags.Other));

            ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectSpecToSelf(spec);
            check("E-12 Asset Tags는 대상에게 부여되지 않는다(적용 중 소유 태그 0)", handle.IsValid && asc.GetTagCount(tags.Other) == 0 && asc.GetTagCount(tags.Child) == 0);
            asc.RemoveActiveGameplayEffect(handle);

            // Instant modifier 조건은 액터 태그만 본다 — GE 자신의 Asset Tag로는 Source Require를 못 맞춘다
            AssetTagsGameplayEffectComponent instantAssetTags = MakeAssetTagsComponent(tags.Other);
            GameplayEffectAsset instant = MakeGameplayEffect(GameplayEffectType.Instant, new GameplayEffectComponent[] { instantAssetTags }, MakeModifier(health, 30f, MakeRequire(tags.Other)));
            instant.OnGameplayEffectChanged();
            asc.ApplyGameplayEffectToSelf(instant);
            check("E-13 Instant Source Require Other + GE Asset Tag Other: 액터 태그로만 판정해 건너뛴다(Base 100)", IsBaseAndCurrent(asc, health, 100f));

            // 캡처 평가는 캡처하는 GE의 합친 태그로 판정한다 — Asset Tag가 버프 조건을 맞춘다
            GameplayEffectAsset healthBuff = MakePersistentEffect(GameplayEffectType.Infinite, Array.Empty<GameplayEffectComponent>(), MakeModifier(health, 50f, MakeRequire(tags.Other)));
            ActiveGameplayEffectHandle buffHandle = asc.ApplyGameplayEffectToSelf(healthBuff);

            GameplayEffectAttributeCaptureDefinition captureHealth = new GameplayEffectAttributeCaptureDefinition(health, AttributeCaptureSource.Target, false);
            GameplayEffectAsset speedPlain = MakePersistentEffect(GameplayEffectType.Infinite, Array.Empty<GameplayEffectComponent>(), MakeAttributeBasedModifier(speed, captureHealth, 1f));
            ActiveGameplayEffectHandle plainHandle = asc.ApplyGameplayEffectToSelf(speedPlain);
            float speedWithoutAssetTag = asc.GetAttributeCurrentValue(speed);
            asc.RemoveActiveGameplayEffect(plainHandle);

            AssetTagsGameplayEffectComponent speedAssetTags = MakeAssetTagsComponent(tags.Other);
            GameplayEffectAsset speedTagged = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { speedAssetTags }, MakeAttributeBasedModifier(speed, captureHealth, 1f));
            speedTagged.OnGameplayEffectChanged();
            ActiveGameplayEffectHandle taggedHandle = asc.ApplyGameplayEffectToSelf(speedTagged);
            float speedWithAssetTag = asc.GetAttributeCurrentValue(speed);
            asc.RemoveActiveGameplayEffect(taggedHandle);

            asc.RemoveActiveGameplayEffect(buffHandle);
            check("E-14 캡처 평가(Speed += Health, 버프 Source Require Other): Asset Tag 없음 100, Speed GE에 Asset Tag Other 150", Approx(speedWithoutAssetTag, 100f) && Approx(speedWithAssetTag, 150f));
            check("E-15 정리: Health 100, Speed 0", IsBaseAndCurrent(asc, health, 100f) && Approx(asc.GetAttributeCurrentValue(speed), 0f));

            UnityEngine.Object.DestroyImmediate(ge);
            UnityEngine.Object.DestroyImmediate(instant);
            UnityEngine.Object.DestroyImmediate(healthBuff);
            UnityEngine.Object.DestroyImmediate(speedPlain);
            UnityEngine.Object.DestroyImmediate(speedTagged);
        }

        // ── F. Granted Tags(Target Tags Component) ──
        private static void RunGrantedTagsChecks(AbilitySystemComponent asc, AssetTestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;

            TargetTagsGameplayEffectComponent grantOther = MakeTargetTagsComponent(tags.Other);
            GameplayEffectAsset first = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { grantOther }, MakeModifier(health, 0f));
            first.OnGameplayEffectChanged();
            check("F-1 OnGameplayEffectChanged: GetOwner()가 그 GE, GetGrantedTags = {Other}", ReferenceEquals(grantOther.GetOwner(), first) && IsExactly(first.GetGrantedTags(), tags.Other));

            first.OnGameplayEffectChanged();
            check("F-2 OnGameplayEffectChanged 두 번: 중복 없음", IsExactly(first.GetGrantedTags(), tags.Other));

            GameplayEffectSpec spec = asc.MakeOutgoingSpec(first);
            GameplayTagContainer allGranted = new GameplayTagContainer();
            spec.GetAllGrantedTags(allGranted);
            check("F-3 Spec.GetAllGrantedTags = {Other}", IsExactly(allGranted, tags.Other));

            ActiveGameplayEffectHandle firstHandle = asc.ApplyGameplayEffectSpecToSelf(spec);
            check("F-4 Infinite 적용: 대상 소유 태그 Other 1개", asc.GetTagCount(tags.Other) == 1 && asc.HasMatchingGameplayTag(tags.Other));

            // 같은 태그를 주는 두 번째 GE — 개수가 쌓이고, 하나만 제거해도 남는다
            GameplayEffectAsset second = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 0f));
            second.OnGameplayEffectChanged();
            ActiveGameplayEffectHandle secondHandle = asc.ApplyGameplayEffectToSelf(second);
            int countBoth = asc.GetTagCount(tags.Other);
            asc.RemoveActiveGameplayEffect(firstHandle);
            int countAfterOne = asc.GetTagCount(tags.Other);
            asc.RemoveActiveGameplayEffect(secondHandle);
            check($"F-5 같은 태그를 두 GE가 부여: 2 → 하나 제거 1 → 둘 다 제거 0 (실제 {countBoth} → {countAfterOne} → {asc.GetTagCount(tags.Other)})", countBoth == 2 && countAfterOne == 1 && asc.GetTagCount(tags.Other) == 0);

            GameplayEffectAsset childGrant = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeTargetTagsComponent(tags.Child) }, MakeModifier(health, 0f));
            childGrant.OnGameplayEffectChanged();
            ActiveGameplayEffectHandle childHandle = asc.ApplyGameplayEffectToSelf(childGrant);
            bool parentMatches = asc.HasMatchingGameplayTag(tags.Parent);
            asc.RemoveActiveGameplayEffect(childHandle);
            check("F-6 Asset.Child 부여 → 부모 Asset으로도 매칭, 제거 후 없음", parentMatches && !asc.HasMatchingGameplayTag(tags.Parent));

            GameplayEffectAsset instant = MakeGameplayEffect(GameplayEffectType.Instant, new GameplayEffectComponent[] { MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 0f));
            instant.OnGameplayEffectChanged();
            asc.ApplyGameplayEffectToSelf(instant);
            check("F-7 Instant는 활성 목록에 남지 않아 태그를 주지 않는다", asc.GetTagCount(tags.Other) == 0);

            GameplayEffectAsset periodic = MakePeriodicEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 0f));
            periodic.OnGameplayEffectChanged();
            ActiveGameplayEffectHandle periodicHandle = asc.ApplyGameplayEffectToSelf(periodic);
            int periodicCount = asc.GetTagCount(tags.Other);
            asc.RemoveActiveGameplayEffect(periodicHandle);
            check("F-8 주기 GE도 활성인 동안 부여(1) → 제거 0", periodicCount == 1 && asc.GetTagCount(tags.Other) == 0);

            List<string> log = new List<string>();
            RecordingComponent off = new RecordingComponent("F", log) { ShouldBeActive = false };
            GameplayEffectAsset inhibited = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { off, MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 0f));
            inhibited.OnGameplayEffectChanged();
            ActiveGameplayEffectHandle inhibitedHandle = asc.ApplyGameplayEffectToSelf(inhibited);
            int whileOff = asc.GetTagCount(tags.Other);
            asc.SetActiveGameplayEffectInhibit(inhibitedHandle, false);
            int whileOn = asc.GetTagCount(tags.Other);
            asc.RemoveActiveGameplayEffect(inhibitedHandle);
            check($"F-9 꺼진 채 시작한 GE: 꺼진 동안 0 → 켜면 1 → 제거 0 (실제 {whileOff} → {whileOn} → {asc.GetTagCount(tags.Other)})", whileOff == 0 && whileOn == 1 && asc.GetTagCount(tags.Other) == 0);

            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(childGrant);
            UnityEngine.Object.DestroyImmediate(instant);
            UnityEngine.Object.DestroyImmediate(periodic);
            UnityEngine.Object.DestroyImmediate(inhibited);
        }

        // ── G. Target Tag Requirements ──
        private static void RunTargetTagRequirementsChecks(AbilitySystemComponent asc, AssetTestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            var created = new List<GameplayEffectAsset>();

            GameplayEffectAsset MakeRequirementsEffect(GameplayEffectType type, Action<TargetTagRequirementsGameplayEffectComponent> configure, params GameplayEffectComponent[] extra)
            {
                var requirements = new TargetTagRequirementsGameplayEffectComponent();
                configure(requirements);
                var components = new List<GameplayEffectComponent> { requirements };
                components.AddRange(extra);
                GameplayEffectAsset ge = type == GameplayEffectType.Instant
                    ? MakeGameplayEffect(type, components.ToArray(), MakeModifier(health, 30f))
                    : MakePersistentEffect(type, components.ToArray(), MakeModifier(health, 50f));
                ge.OnGameplayEffectChanged();
                created.Add(ge);
                return ge;
            }

            // Application: 적용 순간에만
            GameplayEffectAsset needOther = MakeRequirementsEffect(GameplayEffectType.Instant, c => c.ApplicationTagRequirements.RequireTags.AddTag(tags.Other));
            asc.ApplyGameplayEffectToSelf(needOther);
            bool blockedWithoutTag = IsBaseAndCurrent(asc, health, 100f);
            asc.AddLooseGameplayTag(tags.Other);
            asc.ApplyGameplayEffectToSelf(needOther);
            bool appliedWithTag = IsBaseAndCurrent(asc, health, 130f);
            asc.RemoveLooseGameplayTag(tags.Other);
            SetBase(asc, health, 100f);
            check("G-1 Application Require Other: 태그 없으면 적용 안 됨(100), 있으면 적용(130)", blockedWithoutTag && appliedWithTag);

            GameplayEffectAsset ignoreOther = MakeRequirementsEffect(GameplayEffectType.Infinite, c => c.ApplicationTagRequirements.IgnoreTags.AddTag(tags.Other));
            asc.AddLooseGameplayTag(tags.Other);
            ActiveGameplayEffectHandle ignoredHandle = asc.ApplyGameplayEffectToSelf(ignoreOther);
            asc.RemoveLooseGameplayTag(tags.Other);
            check("G-2 Application Ignore Other: 태그가 있으면 Infinite도 적용 안 됨(Invalid 핸들)", !ignoredHandle.IsValid && Approx(asc.GetAttributeCurrentValue(health), 100f));

            GameplayEffectAsset removalOther = MakeRequirementsEffect(GameplayEffectType.Infinite, c => c.RemovalTagRequirements.RequireTags.AddTag(tags.Other));
            asc.AddLooseGameplayTag(tags.Other);
            ActiveGameplayEffectHandle removalBlocked = asc.ApplyGameplayEffectToSelf(removalOther);
            asc.RemoveLooseGameplayTag(tags.Other);
            check("G-3 Removal 조건을 이미 맞추면 적용도 막힌다", !removalBlocked.IsValid);

            // Ongoing: 적용된 뒤 태그에 따라 켜고 끄기
            GameplayEffectAsset ongoingOther = MakeRequirementsEffect(GameplayEffectType.Infinite, c => c.OngoingTagRequirements.RequireTags.AddTag(tags.Other));
            ActiveGameplayEffectHandle ongoingHandle = asc.ApplyGameplayEffectToSelf(ongoingOther);
            ActiveGameplayEffect ongoingActive = asc.GetActiveGameplayEffect(ongoingHandle);
            bool startsOff = ongoingHandle.IsValid && ongoingActive != null && ongoingActive.IsInhibited && Approx(asc.GetAttributeCurrentValue(health), 100f);
            asc.AddLooseGameplayTag(tags.Other);
            bool turnsOn = !ongoingActive.IsInhibited && Approx(asc.GetAttributeCurrentValue(health), 150f);
            asc.RemoveLooseGameplayTag(tags.Other);
            bool turnsOff = ongoingActive.IsInhibited && Approx(asc.GetAttributeCurrentValue(health), 100f) && asc.GetActiveGameplayEffect(ongoingHandle) != null;
            check($"G-4 Ongoing Require Other: 태그 없이 적용 → 꺼진 채(100) / 태그 생김 → 켜짐(150) / 태그 없어짐 → 꺼짐(100), 제거되진 않음 ({startsOff}/{turnsOn}/{turnsOff})", startsOff && turnsOn && turnsOff);

            asc.RemoveActiveGameplayEffect(ongoingHandle);
            asc.AddLooseGameplayTag(tags.Other);
            bool noStaleCallback = Approx(asc.GetAttributeCurrentValue(health), 100f) && asc.GetActiveGameplayEffect(ongoingHandle) == null;
            asc.RemoveLooseGameplayTag(tags.Other);
            check("G-5 GE 제거 뒤 구독이 풀려 태그가 바뀌어도 아무 일 없음(예외·효과 없음)", noStaleCallback);

            // Removal: 적용 중 조건을 맞추면 제거
            GameplayEffectAsset removedByChild = MakeRequirementsEffect(GameplayEffectType.Infinite, c => c.RemovalTagRequirements.RequireTags.AddTag(tags.Child));
            ActiveGameplayEffectHandle removedHandle = asc.ApplyGameplayEffectToSelf(removedByChild);
            bool activeBefore = asc.GetActiveGameplayEffect(removedHandle) != null && Approx(asc.GetAttributeCurrentValue(health), 150f);
            asc.AddLooseGameplayTag(tags.Child);
            bool removedAfter = asc.GetActiveGameplayEffect(removedHandle) == null && Approx(asc.GetAttributeCurrentValue(health), 100f);
            asc.RemoveLooseGameplayTag(tags.Child);
            check("G-6 Removal Require Asset.Child: 적용 중(150) → 태그 생김 → 제거(100)", activeBefore && removedAfter);

            // 다른 GE의 Granted Tags로 켜고 끄기(태그 연동 전체 흐름)
            GameplayEffectAsset needsGranted = MakeRequirementsEffect(GameplayEffectType.Infinite, c => c.OngoingTagRequirements.RequireTags.AddTag(tags.Other));
            GameplayEffectAsset grantsOther = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 0f));
            grantsOther.OnGameplayEffectChanged();
            created.Add(grantsOther);
            ActiveGameplayEffectHandle dependentHandle = asc.ApplyGameplayEffectToSelf(needsGranted);
            ActiveGameplayEffectHandle grantHandle = asc.ApplyGameplayEffectToSelf(grantsOther);
            bool onWithGrant = Approx(asc.GetAttributeCurrentValue(health), 150f);
            asc.RemoveActiveGameplayEffect(grantHandle);
            bool offWithoutGrant = Approx(asc.GetAttributeCurrentValue(health), 100f);
            asc.RemoveActiveGameplayEffect(dependentHandle);
            check("G-7 다른 GE의 Granted Tag로 Ongoing 충족: 부여 GE 적용 → 켜짐(150), 부여 GE 제거 → 꺼짐(100)", onWithGrant && offWithoutGrant);

            // 질의(TagQuery)에 나오는 태그도 구독한다
            GameplayEffectAsset queryOngoing = MakeRequirementsEffect(GameplayEffectType.Infinite,
                c => SetPrivateField(c.OngoingTagRequirements, "_tagQuery", GameplayTagQuery.MakeQuery_MatchAnyTags(new GameplayTagContainer(tags.Other))));
            ActiveGameplayEffectHandle queryHandle = asc.ApplyGameplayEffectToSelf(queryOngoing);
            bool queryOff = Approx(asc.GetAttributeCurrentValue(health), 100f);
            asc.AddLooseGameplayTag(tags.Other);
            bool queryOn = Approx(asc.GetAttributeCurrentValue(health), 150f);
            asc.RemoveLooseGameplayTag(tags.Other);
            asc.RemoveActiveGameplayEffect(queryHandle);
            check("G-8 Ongoing TagQuery(Other 중 하나): 꺼진 채(100) → 태그 생김 → 켜짐(150)", queryOff && queryOn);

            check("G-9 정리: Health 100, Other·Asset.Child 태그 0", IsBaseAndCurrent(asc, health, 100f) && asc.GetTagCount(tags.Other) == 0 && asc.GetTagCount(tags.Child) == 0);

            foreach (GameplayEffectAsset ge in created)
            {
                UnityEngine.Object.DestroyImmediate(ge);
            }
        }

        // ── H. GameplayEffectQuery·ASC 질의 API ──
        private static void RunEffectQueryChecks(AbilitySystemComponent asc, AssetTestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            var child = new GameplayTagContainer(tags.Child);
            var parent = new GameplayTagContainer(tags.Parent);
            var other = new GameplayTagContainer(tags.Other);

            check("H-1 빈 질의는 IsEmpty, 태그 질의는 아니다", new GameplayEffectQuery().IsEmpty() && !GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(child).IsEmpty());

            // A: Asset Tag Child, Health +10 / B: Granted Tag Other, Health +20 / C: Duration 100초, Speed +5
            GameplayEffectAsset geA = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeAssetTagsComponent(tags.Child) }, MakeModifier(health, 10f));
            GameplayEffectAsset geB = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 20f));
            GameplayEffectAsset geC = MakePersistentEffect(GameplayEffectType.Duration, Array.Empty<GameplayEffectComponent>(), MakeModifier(speed, 5f));
            geA.OnGameplayEffectChanged();
            geB.OnGameplayEffectChanged();
            geC.OnGameplayEffectChanged();

            ActiveGameplayEffectHandle handleA = asc.ApplyGameplayEffectToSelf(geA);
            ActiveGameplayEffectHandle handleB = asc.ApplyGameplayEffectToSelf(geB);
            ActiveGameplayEffectHandle handleC = asc.ApplyGameplayEffectToSelf(geC);

            check("H-2 MatchAnyEffectTags{Child} = A", IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(child)), handleA));
            check("H-3 MatchAnyEffectTags{Asset(부모)} = A(자식 태그를 가진 GE도 맞는다)", IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(parent)), handleA));
            check("H-4 Effect 태그는 Granted를 안 본다: MatchAnyEffectTags{Other} = 없음", IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(other))));
            check("H-5 Owning 태그는 Asset+Granted: {Other} = B, {Child} = A", IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(other)), handleB) && IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(child)), handleA));
            check("H-6 MatchNoEffectTags{Child} = B, C", IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchNoEffectTags(child)), handleB, handleC));
            check("H-7 GetActiveEffectsWithAllTags{Child} = A", IsHandles(asc.GetActiveEffectsWithAllTags(child), handleA));
            check("H-8 Asset Tags는 시전자 Spec 태그로도 들어가 MatchAnySourceSpecTags{Child} = A", IsHandles(asc.GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnySourceSpecTags(child)), handleA));
            check("H-9 ModifyingAttribute Speed = C", IsHandles(asc.GetActiveEffects(new GameplayEffectQuery { ModifyingAttribute = new GameplayAttribute(speed) }), handleC));
            check("H-10 EffectDefinition B = B", IsHandles(asc.GetActiveEffects(new GameplayEffectQuery { EffectDefinition = geB }), handleB));

            var ignoreQuery = new GameplayEffectQuery { EffectDefinition = geB };
            ignoreQuery.IgnoreHandles.Add(handleB);
            check("H-11 IgnoreHandles에 든 핸들은 다른 조건을 맞춰도 빠진다", IsHandles(asc.GetActiveEffects(ignoreQuery)));

            var customQuery = new GameplayEffectQuery { CustomMatchDelegate = effect => effect.Handle.Equals(handleC) };
            check("H-12 CustomMatchDelegate만 채운 질의: 비어 있지 않고 C만 고른다", !customQuery.IsEmpty() && IsHandles(asc.GetActiveEffects(customQuery), handleC));

            GameplayEffectQuery ownsOther = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(other);
            bool countBefore = asc.GetAggregatedStackCount(ownsOther) == 1;
            asc.SetActiveGameplayEffectInhibit(handleB, true);
            bool countInhibited = asc.GetAggregatedStackCount(ownsOther) == 0 && IsHandles(asc.GetActiveEffects(new GameplayEffectQuery { EffectDefinition = geB }), handleB);
            asc.SetActiveGameplayEffectInhibit(handleB, false);
            check("H-13 GetAggregatedStackCount: 켜진 GE만 센다(B 1 → 끄면 0, 조회엔 남음)", countBefore && countInhibited);

            var queryA = new GameplayEffectQuery { EffectDefinition = geA };
            var queryC = new GameplayEffectQuery { EffectDefinition = geC };
            List<float> durationsC = asc.GetActiveEffectsDuration(queryC);
            List<float> remainingC = asc.GetActiveEffectsTimeRemaining(queryC);
            List<float> durationsA = asc.GetActiveEffectsDuration(queryA);
            List<float> remainingA = asc.GetActiveEffectsTimeRemaining(queryA);
            check("H-14 지속시간·남은 시간: C = 100·100, 끝 없는 A = -1·-1", durationsC.Count == 1 && Approx(durationsC[0], 100f) && remainingC.Count == 1 && Approx(remainingC[0], 100f) && durationsA.Count == 1 && Approx(durationsA[0], GameplayEffectConstants.InfiniteDuration) && remainingA.Count == 1 && Approx(remainingA[0], GameplayEffectConstants.InfiniteDuration));

            bool foundC = asc.GetActiveEffectsEndTimeAndDuration(queryC, out float endTimeC, out float durationC);
            var queryAOrC = new GameplayEffectQuery { CustomMatchDelegate = effect => effect.Handle.Equals(handleA) || effect.Handle.Equals(handleC) };
            bool foundAOrC = asc.GetActiveEffectsEndTimeAndDuration(queryAOrC, out float endTimeAOrC, out float durationAOrC);
            bool foundNone = asc.GetActiveEffectsEndTimeAndDuration(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(other), out _, out _);
            check("H-15 끝나는 시각: C = 지금+100, 끝 없는 A가 섞이면 -1, 맞는 GE가 없으면 false", foundC && Approx(endTimeC, Time.time + 100f) && Approx(durationC, 100f) && foundAOrC && Approx(endTimeAOrC, GameplayEffectConstants.InfiniteDuration) && Approx(durationAOrC, GameplayEffectConstants.InfiniteDuration) && !foundNone);

            asc.SetActiveGameplayEffectLevelUsingQuery(queryA, 3f);
            ActiveGameplayEffect activeA = asc.GetActiveGameplayEffect(handleA);
            ActiveGameplayEffect activeB = asc.GetActiveGameplayEffect(handleB);
            check("H-16 SetActiveGameplayEffectLevelUsingQuery: A만 레벨 3, Health 130 그대로(고정 magnitude)", Approx(activeA.Spec.Level, 3f) && Approx(activeB.Spec.Level, 1f) && Approx(asc.GetAttributeCurrentValue(health), 130f));

            int removedA = asc.RemoveActiveEffectsWithTags(child);
            check("H-17 RemoveActiveEffectsWithTags{Child}: 1개, A 제거, Health 120", removedA == 1 && asc.GetActiveGameplayEffect(handleA) == null && Approx(asc.GetAttributeCurrentValue(health), 120f));

            int removedNone = asc.RemoveActiveEffectsWithTags(child);
            check("H-18 맞는 GE가 없으면 0", removedNone == 0);

            int removedB = asc.RemoveActiveEffectsWithGrantedTags(other);
            check("H-19 RemoveActiveEffectsWithGrantedTags{Other}: 1개, B 제거, Other 태그 0, Health 100", removedB == 1 && asc.GetActiveGameplayEffect(handleB) == null && asc.GetTagCount(tags.Other) == 0 && Approx(asc.GetAttributeCurrentValue(health), 100f));

            int removedC = asc.RemoveActiveEffects(queryC);
            check("H-20 RemoveActiveEffects(정의 C): 1개, Speed 0", removedC == 1 && asc.GetActiveGameplayEffect(handleC) == null && Approx(asc.GetAttributeCurrentValue(speed), 0f));

            // 제거가 태그 변화로 다른 GE 제거를 부르는 경우: X가 주는 Other가 사라지면 Y가 Removal 조건(Other 없음)을 맞춰 스스로 빠진다
            GameplayEffectAsset geX = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeAssetTagsComponent(tags.Child), MakeTargetTagsComponent(tags.Other) }, MakeModifier(health, 10f));
            var removalRequirements = new TargetTagRequirementsGameplayEffectComponent();
            GameplayTagContainer removalIgnoreTags = removalRequirements.RemovalTagRequirements.IgnoreTags;
            removalIgnoreTags.AddTag(tags.Other);
            GameplayEffectAsset geY = MakePersistentEffect(GameplayEffectType.Infinite, new GameplayEffectComponent[] { MakeAssetTagsComponent(tags.Child), removalRequirements }, MakeModifier(health, 20f));
            geX.OnGameplayEffectChanged();
            geY.OnGameplayEffectChanged();
            ActiveGameplayEffectHandle handleX = asc.ApplyGameplayEffectToSelf(geX);
            ActiveGameplayEffectHandle handleY = asc.ApplyGameplayEffectToSelf(geY);
            bool bothApplied = handleX.IsValid && handleY.IsValid && Approx(asc.GetAttributeCurrentValue(health), 130f);
            int removedXY = asc.RemoveActiveEffectsWithTags(child);
            check("H-21 연쇄 제거 중에도 예외 없이 둘 다 빠지고 Health 100", bothApplied && removedXY >= 1 && asc.GetActiveGameplayEffect(handleX) == null && asc.GetActiveGameplayEffect(handleY) == null && Approx(asc.GetAttributeCurrentValue(health), 100f));

            UnityEngine.Object.DestroyImmediate(geA);
            UnityEngine.Object.DestroyImmediate(geB);
            UnityEngine.Object.DestroyImmediate(geC);
            UnityEngine.Object.DestroyImmediate(geX);
            UnityEngine.Object.DestroyImmediate(geY);
        }

        // ── I. Remove Other Component ──
        private static void RunRemoveOtherChecks(AbilitySystemComponent asc, AssetTestTags tags, Action<string, bool> check)
        {
            GameplayAttributeHandle health = CharacterAttributeSet.Health;
            var child = new GameplayTagContainer(tags.Child);
            var created = new List<GameplayEffectAsset>();

            GameplayEffectAsset MakeEffect(GameplayEffectType type, float magnitude, params GameplayEffectComponent[] components)
            {
                GameplayEffectAsset ge = type == GameplayEffectType.Instant ? MakeGameplayEffect(type, components, MakeModifier(health, magnitude)) : MakePersistentEffect(type, components, MakeModifier(health, magnitude));
                ge.OnGameplayEffectChanged();
                created.Add(ge);
                return ge;
            }

            RemoveOtherGameplayEffectComponent MakeRemoveOther(params GameplayEffectQuery[] queries)
            {
                var component = new RemoveOtherGameplayEffectComponent();
                SetPrivateField(component, "_removeGameplayEffectQueries", new List<GameplayEffectQuery>(queries));
                return component;
            }

            // 자기도 Child를 가진 정화 GE — 자기를 지우면 안 된다
            GameplayEffectAsset debuff = MakeEffect(GameplayEffectType.Infinite, 10f, MakeAssetTagsComponent(tags.Child));
            GameplayEffectQuery removeChild = GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(child);
            GameplayEffectAsset cleanse = MakeEffect(GameplayEffectType.Infinite, 1f, MakeAssetTagsComponent(tags.Child), MakeRemoveOther(removeChild));

            ActiveGameplayEffectHandle debuffHandle = asc.ApplyGameplayEffectToSelf(debuff);
            ActiveGameplayEffectHandle cleanseHandle = asc.ApplyGameplayEffectToSelf(cleanse);
            check("I-1 적용되면 질의에 맞는 다른 GE를 지우고 자기는 남는다(Health 101)", asc.GetActiveGameplayEffect(debuffHandle) == null && asc.GetActiveGameplayEffect(cleanseHandle) != null && Approx(asc.GetAttributeCurrentValue(health), 101f));
            check("I-2 에셋의 질의는 고치지 않는다(IgnoreHandles 비어 있음)", removeChild.IgnoreHandles.Count == 0);

            ActiveGameplayEffectHandle cleanseHandle2 = asc.ApplyGameplayEffectToSelf(cleanse);
            check("I-3 같은 GE가 다시 적용돼도 먼저 걸린 같은 GE는 지우지 않는다", asc.GetActiveGameplayEffect(cleanseHandle) != null && asc.GetActiveGameplayEffect(cleanseHandle2) != null);
            asc.RemoveActiveGameplayEffect(cleanseHandle);
            asc.RemoveActiveGameplayEffect(cleanseHandle2);

            // UE는 둘째 질의부터 자기 제외가 빠진다 — 우리는 모든 질의에 건다
            GameplayEffectAsset cleanseTwoQueries = MakeEffect(GameplayEffectType.Infinite, 1f, MakeAssetTagsComponent(tags.Child), MakeRemoveOther(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(child), GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(child)));
            debuffHandle = asc.ApplyGameplayEffectToSelf(debuff);
            ActiveGameplayEffectHandle twoQueriesHandle = asc.ApplyGameplayEffectToSelf(cleanseTwoQueries);
            check("I-4 질의가 둘이어도 자기는 남고 대상은 지운다", asc.GetActiveGameplayEffect(twoQueriesHandle) != null && asc.GetActiveGameplayEffect(debuffHandle) == null);
            asc.RemoveActiveGameplayEffect(twoQueriesHandle);

            GameplayEffectAsset emptyQuery = MakeEffect(GameplayEffectType.Infinite, 1f, MakeRemoveOther(new GameplayEffectQuery()));
            debuffHandle = asc.ApplyGameplayEffectToSelf(debuff);
            ActiveGameplayEffectHandle emptyHandle = asc.ApplyGameplayEffectToSelf(emptyQuery);
            check("I-5 빈 질의는 건너뛴다(아무것도 지우지 않음)", asc.GetActiveGameplayEffect(debuffHandle) != null && asc.GetActiveGameplayEffect(emptyHandle) != null);
            asc.RemoveActiveGameplayEffect(emptyHandle);

            GameplayEffectAsset instantCleanse = MakeEffect(GameplayEffectType.Instant, 5f, MakeRemoveOther(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(child)));
            asc.ApplyGameplayEffectToSelf(instantCleanse);
            check("I-6 Instant GE도 적용되면 지운다(Debuff 제거, Base 105)", asc.GetActiveGameplayEffect(debuffHandle) == null && IsBaseAndCurrent(asc, health, 105f));

            SetBase(asc, health, 100f);
            check("I-7 정리: Health 100, 활성 GE 0", IsBaseAndCurrent(asc, health, 100f) && GetContainer(asc).ActiveGameplayEffects.Count == 0);

            foreach (GameplayEffectAsset ge in created)
            {
                UnityEngine.Object.DestroyImmediate(ge);
            }
        }

        // ── 헬퍼 ──

        // 핸들 목록이 정확히 이것들인지(순서 무관)
        private static bool IsHandles(List<ActiveGameplayEffectHandle> actual, params ActiveGameplayEffectHandle[] expected)
        {
            if (actual.Count != expected.Length)
            {
                return false;
            }

            foreach (ActiveGameplayEffectHandle handle in expected)
            {
                if (!actual.Contains(handle))
                {
                    return false;
                }
            }

            return true;
        }

        private static TargetTagsGameplayEffectComponent MakeTargetTagsComponent(params GameplayTag[] tags)
        {
            TargetTagsGameplayEffectComponent component = new TargetTagsGameplayEffectComponent();
            InheritedTagContainer grantedTags = component.GetConfiguredTargetTagChanges();
            foreach (GameplayTag tag in tags)
            {
                grantedTags.AddTag(tag);
            }

            return component;
        }

        private static AssetTagsGameplayEffectComponent MakeAssetTagsComponent(params GameplayTag[] tags)
        {
            AssetTagsGameplayEffectComponent component = new AssetTagsGameplayEffectComponent();
            InheritedTagContainer assetTags = component.GetConfiguredAssetTagChanges();
            foreach (GameplayTag tag in tags)
            {
                assetTags.AddTag(tag);
            }

            return component;
        }

        private static GameplayTagRequirements MakeRequire(GameplayTag tag)
        {
            GameplayTagRequirements requirements = new GameplayTagRequirements();
            GameplayTagContainer requireTags = requirements.RequireTags;
            requireTags.AddTag(tag);
            return requirements;
        }

        // 명시 태그가 정확히 이것들뿐인지(부모 태그는 세지 않는다)
        private static bool IsExactly(GameplayTagContainer container, params GameplayTag[] expected)
        {
            if (container.Num() != expected.Length)
            {
                return false;
            }

            foreach (GameplayTag tag in expected)
            {
                if (!container.HasTagExact(tag))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLog(List<string> log, params string[] expected)
        {
            if (log.Count != expected.Length)
            {
                return false;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (log[i] != expected[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllSame(List<ActiveGameplayEffectsContainer> containers, ActiveGameplayEffectsContainer expected)
        {
            foreach (ActiveGameplayEffectsContainer container in containers)
            {
                if (!ReferenceEquals(container, expected))
                {
                    return false;
                }
            }

            return containers.Count > 0;
        }

        private static bool IsCurrentAndTag(AbilitySystemComponent asc, GameplayAttributeHandle handle, float expectedCurrent, GameplayTag tag, int expectedTagCount)
        {
            return Approx(asc.GetAttributeCurrentValue(handle), expectedCurrent) && asc.GetTagCount(tag) == expectedTagCount;
        }

        private static bool IsBaseAndCurrent(AbilitySystemComponent asc, GameplayAttributeHandle handle, float expected)
        {
            return Approx(asc.GetAttributeBaseValue(handle), expected) && Approx(asc.GetAttributeCurrentValue(handle), expected);
        }

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < Eps;
        }

        // Granted Tags Component가 아직 없어 그 Component가 채울 GE 캐시에 직접 넣는다. 캐시는 OnGameplayEffectChanged에서 다시 만들어지므로 그 뒤에 부르면 사라진다
        private static void GrantTag(GameplayEffectAsset ge, GameplayTag tag)
        {
            GameplayTagContainer grantedTags = ge.GetGrantedTags();
            grantedTags.AddTag(tag);
        }

        private static GameplayEffectAsset MakeGameplayEffect(GameplayEffectType type, GameplayEffectComponent[] components, params GameplayModifier[] modifiers)
        {
            GameplayEffectAsset ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", type);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier>(modifiers));
            SetPrivateField(ge, "gameplayEffectComponents", new List<GameplayEffectComponent>(components));
            return ge;
        }

        // Duration은 시간이 남아 있어야 Tick 중에 만료되지 않는다
        private static GameplayEffectAsset MakePersistentEffect(GameplayEffectType type, GameplayEffectComponent[] components, GameplayModifier modifier)
        {
            GameplayEffectAsset ge = MakeGameplayEffect(type, components, modifier);
            SetPrivateField(ge, "duration", 100f);
            return ge;
        }

        private static GameplayEffectAsset MakePeriodicEffect(GameplayEffectType type, GameplayEffectComponent[] components, GameplayModifier modifier)
        {
            GameplayEffectAsset ge = MakePersistentEffect(type, components, modifier);
            SetPrivateField(ge, "period", 1f);
            SetPrivateField(ge, "executePeriodicEffectOnApplication", true);
            return ge;
        }

        private static GameplayModifier MakeModifier(GameplayAttributeHandle handle, float magnitude, GameplayTagRequirements sourceTags = null)
        {
            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(handle));
            SetPrivateField(boxed, "operation", GameplayModifierOperation.AddBase);
            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);
            SetPrivateField(boxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);
            if (sourceTags != null)
            {
                SetPrivateField(boxed, "sourceTags", sourceTags);
            }

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

        private static ActiveGameplayEffectsContainer GetContainer(AbilitySystemComponent asc)
        {
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", NonPublicInstance);
            return (ActiveGameplayEffectsContainer)containerField.GetValue(asc);
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
    }
}
