using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.GameplayEffectComponents;
using Core.AbilitySystem.Tag;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 GA 부여·해제·생명주기가 UE 5.6처럼 동작하는지 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // 호출을 기록하는 테스트 전용 어빌리티(RecordingAbility)를 원형으로 넣은 정의 에셋을 메모리에 만들어 본다.
    // A: 부여 기본 / B: 원형 깊은 복사 / C: 같은 정의 여러 번 / D: 해제 / E: 잘못된 입력 / F: 스코프 락·보류 / G: 액터 정보 / H: 정의 에셋 인스펙터·저장 / I: 생명주기(발동·종료·취소) / J: GA에서 GE 적용 / K: Per Execution 인스턴싱 / L: 코스트·쿨다운·커밋·태그 조건 / M: TestGameplayAbility 통합(시간 경과 쿨다운 포함) / N: 커밋 경계 / O: 어빌리티 이벤트·실패 사유 / P: 어빌리티 태그·차단·취소
    public static class GameplayAbilitySelfCheck
    {
        private const string TempAssetPath = "Assets/_Test/GA_SelfCheck_Temp.asset";
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [NativeTag] private static readonly NativeGameplayTag CooldownTag = new("Test.GA.Cooldown");
        [NativeTag] private static readonly NativeGameplayTag OwnedTag = new("Test.GA.Owned");
        [NativeTag] private static readonly NativeGameplayTag RequiredTag = new("Test.GA.Required");
        [NativeTag] private static readonly NativeGameplayTag BlockedTag = new("Test.GA.Blocked");
        [NativeTag] private static readonly NativeGameplayTag BlockedChildTag = new("Test.GA.Blocked.Child");
        [NativeTag] private static readonly NativeGameplayTag FailCanActivateTag = new("Test.GA.Fail.CanActivate");
        [NativeTag] private static readonly NativeGameplayTag FailCooldownTag = new("Test.GA.Fail.Cooldown");
        [NativeTag] private static readonly NativeGameplayTag FailCostTag = new("Test.GA.Fail.Cost");
        [NativeTag] private static readonly NativeGameplayTag FailBlockedTag = new("Test.GA.Fail.Blocked");
        [NativeTag] private static readonly NativeGameplayTag FailMissingTag = new("Test.GA.Fail.Missing");
        [NativeTag] private static readonly NativeGameplayTag AttackAbilityTag = new("Test.GA.Ability.Attack");
        [NativeTag] private static readonly NativeGameplayTag MeleeAbilityTag = new("Test.GA.Ability.Attack.Melee");
        [NativeTag] private static readonly NativeGameplayTag MoveAbilityTag = new("Test.GA.Ability.Move");

        [MenuItem("Tools/Ability System/Run Gameplay Ability Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[GA PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[GA FAIL] {label}");
                }
            }

            var created = new List<UnityEngine.Object>();
            GameObject go = new GameObject("GameplayAbilitySelfCheck_Temp");
            GameObject otherAvatar = new GameObject("GameplayAbilitySelfCheck_Avatar");
            created.Add(go);
            created.Add(otherAvatar);
            try
            {
                AbilitySystemComponent asc = go.AddComponent<AbilitySystemComponent>();
                asc.InitAbilityActorInfo(go, go);

                RunGiveChecks(asc, go, created, Check);
                RunDeepCopyChecks(asc, created, Check);
                RunMultipleGiveChecks(asc, created, Check);
                RunClearChecks(asc, created, Check);
                RunInvalidInputChecks(asc, created, Check);
                RunScopeLockChecks(asc, created, Check);
                RunActorInfoChecks(asc, go, otherAvatar, created, Check);
                RunInspectorChecks(created, Check);
                RunActivationChecks(asc, go, created, Check);
                RunEffectChecks(asc, go, created, Check);
                RunPerExecutionChecks(asc, go, created, Check);
                RunCostCooldownTagChecks(asc, created, Check);
                RunTestAbilityChecks(asc, created, Check);
                RunCommitEdgeChecks(asc, created, Check);
                RunEventChecks(asc, created, Check);
                RunBlockCancelChecks(asc, created, Check);
            }
            catch (Exception e)
            {
                Check($"검사 중 예외: {e.GetType().Name} — {e.Message}\n{e.StackTrace}", false);
            }
            finally
            {
                RecordingAbility.Hook = null;
                foreach (UnityEngine.Object obj in created)
                {
                    if (obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                }

                AssetDatabase.DeleteAsset(TempAssetPath);
            }

            Debug.Log($"[GA Self-Check] passed {passed} / failed {failed}");
        }

        // 받은 호출을 공유 로그에 "이름.함수"로 남기고, 깊은 복사 검사용 필드를 가진 테스트 전용 어빌리티
        [Serializable]
        private sealed class RecordingAbility : GameplayAbility
        {
            public static readonly List<string> Log = new();

            // 콜백 안에서 ASC를 다시 부르는 검사용. static이라 복제와 무관하다.
            public static Action<string, RecordingAbility, GameplayAbilitySpec> Hook;

            public string Name;
            public float Value;
            public List<int> Numbers = new();
            public Nested Inline = new();
            public GameplayEffectAsset Effect;
            public AnimationCurve Curve = new();
            [SerializeReference] public Shared SharedA;
            [SerializeReference] public Shared SharedB;

            // 직렬화되지 않는 런타임 상태 — 복제본은 원형 값이 아니라 초기값으로 시작해야 한다.
            [NonSerialized] public int RuntimeCounter = 7;

            public GameplayAbilityActorInfo AvatarSetInfo;

            // 생명주기 검사용 스위치 — 부여 뒤 인스턴스에 직접 켠다.
            [NonSerialized] public bool BlockActivation;
            [NonSerialized] public bool EndOnActivate;

            public override bool CanActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityActorInfo actorInfo, GameplayTagContainer optionalRelevantTags = null)
            {
                return !BlockActivation && base.CanActivateAbility(handle, actorInfo, optionalRelevantTags);
            }

            public override void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityActorInfo actorInfo)
            {
                Log.Add($"{Name}.Activate");
                AbilitySystemComponent asc = actorInfo.AbilitySystemComponent;
                Hook?.Invoke("Activate", this, asc.FindAbilitySpecFromHandle(handle));

                if (EndOnActivate)
                {
                    EndAbility(handle, actorInfo, false);
                }
            }

            public override void EndAbility(GameplayAbilitySpecHandle handle, GameplayAbilityActorInfo actorInfo, bool wasCancelled)
            {
                if (IsEndAbilityValid(handle, actorInfo))
                {
                    Log.Add(wasCancelled ? $"{Name}.Cancel" : $"{Name}.End");
                }

                base.EndAbility(handle, actorInfo, wasCancelled);
            }

            public override void OnGiveAbility(GameplayAbilityActorInfo actorInfo, GameplayAbilitySpec spec)
            {
                Log.Add($"{Name}.OnGive");
                base.OnGiveAbility(actorInfo, spec);
                Hook?.Invoke("OnGive", this, spec);
            }

            public override void OnRemoveAbility(GameplayAbilityActorInfo actorInfo, GameplayAbilitySpec spec)
            {
                Log.Add($"{Name}.OnRemove");
                Hook?.Invoke("OnRemove", this, spec);
            }

            public override void OnAvatarSet(GameplayAbilityActorInfo actorInfo, GameplayAbilitySpec spec)
            {
                Log.Add($"{Name}.OnAvatarSet");
                AvatarSetInfo = actorInfo;
            }
        }

        [Serializable]
        private sealed class Nested
        {
            public int Inner;
        }

        [Serializable]
        private sealed class Shared
        {
            public int Id;
        }

        // ── A: 부여 기본 ──────────────────────────────────────────────────────────

        private static void RunGiveChecks(AbilitySystemComponent asc, GameObject go, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            RecordingAbility.Log.Clear();
            GameplayAbilityAsset asset = MakeAsset("A", created);

            var spec = new GameplayAbilitySpec(asset, 3, 5, go);
            check("A-1 Spec 생성 시 핸들 발급", spec.Handle.IsValid);
            check("A-2 부여 전 인스턴스 없음", spec.GetPrimaryInstance() == null);

            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            check("A-3 GiveAbility는 Spec의 핸들을 돌려준다", handle.Equals(spec.Handle));
            check("A-4 FindAbilitySpecFromHandle로 찾음", asc.FindAbilitySpecFromHandle(handle) == spec);
            check("A-5 GetActivatableAbilities 1개", asc.GetActivatableAbilities().Count == 1);
            check("A-6 FindAbilitySpecFromAsset로 찾음", asc.FindAbilitySpecFromAsset(asset) == spec);
            check("A-7 Level·InputID·SourceObject 유지", spec.Level == 3 && spec.InputID == 5 && spec.SourceObject == go);

            var instance = spec.GetPrimaryInstance() as RecordingAbility;
            check("A-8 인스턴스가 만들어짐(RecordingAbility)", instance != null);
            check("A-9 인스턴스는 원형과 다른 객체", instance != null && !ReferenceEquals(instance, asset.AbilityTemplate));
            check("A-10 OnGiveAbility 뒤 CurrentSpecHandle·CurrentActorInfo", instance != null && instance.CurrentAbilitySpecHandle.Equals(handle) && instance.CurrentActorInfo == asc.AbilityActorInfo);
            check("A-11 GetAbilitySystemComponent/Owning/AvatarFromActorInfo", instance != null
                && instance.GetAbilitySystemComponentFromActorInfo() == asc
                && instance.GetOwningActorFromActorInfo() == go
                && instance.GetAvatarActorFromActorInfo() == go);
            check("A-12 호출 순서 OnGive → OnAvatarSet(Avatar 있음)", Sequence("A.OnGive", "A.OnAvatarSet"));
            check("A-13 원형은 실행되지 않음(액터 정보 없음)", asset.AbilityTemplate.CurrentActorInfo == null);
            check("A-14 비활성 상태로 시작", !spec.IsActive && spec.ActiveCount == 0 && !spec.PendingRemove);
        }

        // ── B: 원형 깊은 복사 ─────────────────────────────────────────────────────

        private static void RunDeepCopyChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            GameplayAbilityAsset asset = MakeAsset("B", created);
            var template = (RecordingAbility)asset.AbilityTemplate;
            GameplayEffectAsset effect = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            created.Add(effect);

            var shared = new Shared { Id = 9 };
            template.Value = 3f;
            template.Numbers = new List<int> { 1, 2 };
            template.Inline = new Nested { Inner = 5 };
            template.Effect = effect;
            template.SharedA = shared;
            template.SharedB = shared;
            template.RuntimeCounter = 100;

            var spec = new GameplayAbilitySpec(asset);
            asc.GiveAbility(spec);
            var copy = (RecordingAbility)spec.GetPrimaryInstance();

            check("B-1 값 필드 복사(Name·Value)", copy.Name == "B" && Mathf.Approximately(copy.Value, 3f));
            check("B-2 List는 새 List, 값은 같음", !ReferenceEquals(copy.Numbers, template.Numbers) && copy.Numbers.Count == 2 && copy.Numbers[0] == 1 && copy.Numbers[1] == 2);
            check("B-3 [Serializable] 중첩 객체는 새 객체, 값은 같음", !ReferenceEquals(copy.Inline, template.Inline) && copy.Inline.Inner == 5);
            check("B-4 UnityEngine.Object 참조는 그대로(GE 에셋)", copy.Effect == effect);
            check("B-5 SerializeReference 공유 참조는 복사본에서도 공유", copy.SharedA != null && ReferenceEquals(copy.SharedA, copy.SharedB));
            check("B-6 SerializeReference 대상도 새 객체", !ReferenceEquals(copy.SharedA, shared) && copy.SharedA.Id == 9);
            check("B-7 직렬화 안 되는 필드는 초기값(원형 값 100 아님)", copy.RuntimeCounter == 7);
            check("B-8 엔진 클래스(AnimationCurve)는 원형과 공유", ReferenceEquals(copy.Curve, template.Curve));

            copy.Numbers.Add(3);
            copy.Inline.Inner = 50;
            copy.SharedA.Id = 90;
            copy.Value = 30f;
            check("B-9 복사본을 바꿔도 원형은 그대로", template.Numbers.Count == 2 && template.Inline.Inner == 5 && shared.Id == 9 && Mathf.Approximately(template.Value, 3f));
        }

        // ── C: 같은 정의 여러 번 ──────────────────────────────────────────────────

        private static void RunMultipleGiveChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            GameplayAbilityAsset asset = MakeAsset("C", created);

            var first = new GameplayAbilitySpec(asset);
            var second = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle h1 = asc.GiveAbility(first);
            GameplayAbilitySpecHandle h2 = asc.GiveAbility(second);

            check("C-1 같은 정의라도 Spec마다 부여(2개)", asc.GetActivatableAbilities().Count == 2);
            check("C-2 핸들이 서로 다름", !h1.Equals(h2));
            check("C-3 인스턴스가 서로 다름", !ReferenceEquals(first.GetPrimaryInstance(), second.GetPrimaryInstance()));
            check("C-4 FindAbilitySpecFromAsset은 첫 Spec", asc.FindAbilitySpecFromAsset(asset) == first);
        }

        // ── D: 해제 ───────────────────────────────────────────────────────────────

        private static void RunClearChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            RecordingAbility.Log.Clear();
            GameplayAbilityAsset asset = MakeAsset("D", created);
            GameplayAbilityAsset keepAsset = MakeAsset("Keep", created);

            var spec = new GameplayAbilitySpec(asset);
            var keep = new GameplayAbilitySpec(keepAsset);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            asc.GiveAbility(keep);
            GameplayAbility instance = spec.GetPrimaryInstance();

            bool stillListedDuringRemove = false;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "OnRemove" && ability.Name == "D")
                {
                    stillListedDuringRemove = asc.FindAbilitySpecFromHandle(s.Handle) == s;
                }
            };
            asc.ClearAbility(handle);
            RecordingAbility.Hook = null;

            check("D-1 OnRemoveAbility 호출", RecordingAbility.Log.Contains("D.OnRemove"));
            check("D-2 OnRemoveAbility 시점엔 아직 목록에 있음(UE 순서)", stillListedDuringRemove);
            check("D-3 해제 뒤 찾을 수 없음", asc.FindAbilitySpecFromHandle(handle) == null);
            check("D-4 해제 뒤 Spec의 인스턴스 비움", spec.GetPrimaryInstance() == null && instance != null);
            check("D-5 다른 어빌리티는 남음", asc.GetActivatableAbilities().Count == 1 && asc.GetActivatableAbilities()[0] == keep);

            int countBefore = asc.GetActivatableAbilities().Count;
            asc.ClearAbility(handle);
            asc.ClearAbility(GameplayAbilitySpecHandle.Invalid);
            check("D-6 없는 핸들·Invalid 해제는 아무것도 안 함", asc.GetActivatableAbilities().Count == countBefore);

            RecordingAbility.Log.Clear();
            asc.ClearAllAbilities();
            check("D-7 ClearAllAbilities — 전부 OnRemove 후 비움", asc.GetActivatableAbilities().Count == 0 && RecordingAbility.Log.Contains("Keep.OnRemove"));
        }

        // ── E: 잘못된 입력 ────────────────────────────────────────────────────────

        private static void RunInvalidInputChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            GameplayAbilityAsset empty = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            empty.name = "GA_SelfCheck_Empty";
            created.Add(empty);

            Debug.Log("[GA Self-Check] 아래 에러 로그 3줄은 E 검사의 정상 출력이다.");
            check("E-1 null Spec → Invalid", !asc.GiveAbility(null).IsValid);
            check("E-2 정의 없는 Spec → Invalid", !asc.GiveAbility(new GameplayAbilitySpec(null)).IsValid);
            check("E-3 원형이 빈 정의 → Invalid", !asc.GiveAbility(new GameplayAbilitySpec(empty)).IsValid);
            check("E-4 실패한 부여는 목록에 없음", asc.GetActivatableAbilities().Count == 0);
        }

        // ── F: 스코프 락·보류 ─────────────────────────────────────────────────────

        private static void RunScopeLockChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            RecordingAbility.Log.Clear();
            GameplayAbilityAsset outerAsset = MakeAsset("Outer", created);
            GameplayAbilityAsset innerAsset = MakeAsset("Inner", created);

            // F-1~4: OnGiveAbility 안에서 부여하면 보류됐다가 락이 풀릴 때 부여
            var inner = new GameplayAbilitySpec(innerAsset);
            bool innerListedDuringHook = true;
            bool innerFoundAsPending = false;
            GameplayAbilitySpecHandle returned = default;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "OnGive" && ability.Name == "Outer")
                {
                    returned = asc.GiveAbility(inner);
                    innerListedDuringHook = asc.FindAbilitySpecFromHandle(inner.Handle, ConsiderPending.None) != null;
                    innerFoundAsPending = asc.FindAbilitySpecFromHandle(inner.Handle, ConsiderPending.PendingAdd) == inner;
                }
            };
            asc.GiveAbility(new GameplayAbilitySpec(outerAsset));
            RecordingAbility.Hook = null;

            check("F-1 락 중 부여는 Spec 핸들을 돌려준다", returned.Equals(inner.Handle));
            check("F-2 락 중엔 목록에 없음", !innerListedDuringHook);
            check("F-3 PendingAdd로는 찾음", innerFoundAsPending);
            check("F-4 락이 풀린 뒤 부여됨(OnGive 순서 Outer → Inner)", asc.FindAbilitySpecFromHandle(inner.Handle) == inner && Sequence("Outer.OnGive", "Inner.OnGive"));

            // F-5~7: OnRemoveAbility 안에서 다른 것을 해제하면 PendingRemove로 표시됐다가 락이 풀릴 때 해제
            GameplayAbilitySpec outer = asc.FindAbilitySpecFromAsset(outerAsset);
            bool innerPendingDuringHook = false;
            bool innerHiddenByDefaultFind = false;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "OnRemove" && ability.Name == "Outer")
                {
                    asc.ClearAbility(inner.Handle);
                    innerPendingDuringHook = inner.PendingRemove && IsListed(asc, inner);
                    innerHiddenByDefaultFind = asc.FindAbilitySpecFromHandle(inner.Handle, ConsiderPending.None) == null;
                }
            };
            asc.ClearAbility(outer.Handle);
            RecordingAbility.Hook = null;

            check("F-5 락 중 해제는 PendingRemove 표시만(목록엔 남음)", innerPendingDuringHook);
            check("F-6 PendingRemove는 ConsiderPending.None 조회에서 빠짐", innerHiddenByDefaultFind);
            check("F-7 락이 풀린 뒤 해제됨", asc.GetActivatableAbilities().Count == 0 && RecordingAbility.Log.Contains("Inner.OnRemove"));

            // F-8: 락 중 부여 후 같은 핸들을 해제하면 보류 부여가 취소된다
            RecordingAbility.Log.Clear();
            var canceled = new GameplayAbilitySpec(innerAsset);
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "OnGive" && ability.Name == "Outer")
                {
                    asc.GiveAbility(canceled);
                    asc.ClearAbility(canceled.Handle);
                }
            };
            asc.GiveAbility(new GameplayAbilitySpec(outerAsset));
            RecordingAbility.Hook = null;
            check("F-8 보류 부여를 해제하면 부여되지 않음", asc.FindAbilitySpecFromHandle(canceled.Handle, ConsiderPending.All) == null && !RecordingAbility.Log.Contains("Inner.OnGive"));

            // F-9: ClearAllAbilities 중 콜백이 해제를 불러도 예외 없이 전부 비운다
            asc.GiveAbility(new GameplayAbilitySpec(innerAsset));
            RecordingAbility.Log.Clear();
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "OnRemove")
                {
                    foreach (GameplayAbilitySpec other in asc.GetActivatableAbilities())
                    {
                        asc.ClearAbility(other.Handle);
                    }
                }
            };
            asc.ClearAllAbilities();
            RecordingAbility.Hook = null;
            check("F-9 ClearAll 중 재진입 해제 — 비워지고 OnRemove는 한 번씩", asc.GetActivatableAbilities().Count == 0
                && Count("Outer.OnRemove") == 1 && Count("Inner.OnRemove") == 1);

            // F-10: 락 중 ClearAllAbilities는 락이 풀릴 때 처리(같은 락 안의 부여보다 우선)
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "OnGive" && ability.Name == "Outer")
                {
                    asc.ClearAllAbilities();
                }
            };
            asc.GiveAbility(new GameplayAbilitySpec(outerAsset));
            RecordingAbility.Hook = null;
            check("F-10 락 중 ClearAll은 락이 풀릴 때 처리", asc.GetActivatableAbilities().Count == 0);
        }

        // ── G: 액터 정보 ──────────────────────────────────────────────────────────

        private static void RunActorInfoChecks(AbilitySystemComponent asc, GameObject go, GameObject otherAvatar, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            GameplayAbilityAsset asset = MakeAsset("G", created);
            var spec = new GameplayAbilitySpec(asset);
            asc.GiveAbility(spec);
            var instance = (RecordingAbility)spec.GetPrimaryInstance();

            check("G-1 기본 액터 정보 Owner = Avatar = ASC GameObject", asc.AbilityActorInfo.OwnerActor == go && asc.AbilityActorInfo.AvatarActor == go && asc.AbilityActorInfo.AbilitySystemComponent == asc);

            RecordingAbility.Log.Clear();
            asc.InitAbilityActorInfo(go, go);
            check("G-2 Avatar가 그대로면 OnAvatarSet 없음", !RecordingAbility.Log.Contains("G.OnAvatarSet"));

            asc.InitAbilityActorInfo(go, otherAvatar);
            check("G-3 Avatar가 바뀌면 부여된 인스턴스에 OnAvatarSet", RecordingAbility.Log.Contains("G.OnAvatarSet") && instance.AvatarSetInfo.AvatarActor == otherAvatar);
            check("G-4 인스턴스가 보는 Avatar도 바뀜(같은 액터 정보 공유)", instance.GetAvatarActorFromActorInfo() == otherAvatar);

            Debug.Log("[GA Self-Check] 아래 에러 로그 1줄은 G-5의 정상 출력이다.");
            asc.InitAbilityActorInfo(null, go);
            check("G-5 Owner null이면 거부(액터 정보 그대로)", asc.AbilityActorInfo.OwnerActor == go && asc.AbilityActorInfo.AvatarActor == otherAvatar);

            asc.InitAbilityActorInfo(go, go);
        }

        // ── H: 정의 에셋 인스펙터·저장 ─────────────────────────────────────────────

        private static void RunInspectorChecks(List<UnityEngine.Object> created, Action<string, bool> check)
        {
            GameplayAbilityAsset asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            created.Add(asset);
            GameplayEffectAsset effect = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            created.Add(effect);

            var serializedObject = new SerializedObject(asset);
            SerializedProperty template = serializedObject.FindProperty("abilityTemplate");
            float emptyHeight = EditorGUI.GetPropertyHeight(template, true);

            template.managedReferenceValue = new TestGameplayAbility();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            template = serializedObject.FindProperty("abilityTemplate");

            SerializedProperty value = template.FindPropertyRelative("_testValue");
            SerializedProperty effectField = template.FindPropertyRelative("_testEffect");
            check("H-1 하위 클래스 필드가 SerializedProperty로 보임(_testValue·_testEffect)", value != null && effectField != null);
            check("H-2 드로어 높이가 하위 필드만큼 늘어남", EditorGUI.GetPropertyHeight(template, true) > emptyHeight + EditorGUIUtility.singleLineHeight);

            value.floatValue = 4.5f;
            effectField.objectReferenceValue = effect;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            check("H-3 인스펙터 경로로 바꾼 값이 원형에 들어감", asset.AbilityTemplate is TestGameplayAbility
                && Mathf.Approximately(new SerializedObject(asset).FindProperty("abilityTemplate._testValue").floatValue, 4.5f));

            // 파일로 저장해 YAML에 하위 필드가 남는지 본다. GE 참조는 메모리 에셋이라 파일 검사에서 뺀다
            effectField.objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, TempAssetPath);
            AssetDatabase.SaveAssets();
            created.Remove(asset);
            string yaml = File.ReadAllText(TempAssetPath);
            check("H-4 저장 파일에 하위 클래스 타입과 필드 값이 남음", yaml.Contains("TestGameplayAbility") && yaml.Contains("_testValue: 4.5"));

            AssetDatabase.ImportAsset(TempAssetPath, ImportAssetOptions.ForceUpdate);
            Resources.UnloadAsset(asset);
            var reloaded = AssetDatabase.LoadAssetAtPath<GameplayAbilityAsset>(TempAssetPath);
            check("H-5 다시 읽어도 원형 타입 유지", reloaded != null && reloaded.AbilityTemplate is TestGameplayAbility);
        }

        // ── I: 생명주기(발동·종료·취소) ───────────────────────────────────────────

        private static void RunActivationChecks(AbilitySystemComponent asc, GameObject go, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            RecordingAbility.Log.Clear();
            GameplayAbilityAsset asset = MakeAsset("I", created);
            var spec = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            var instance = (RecordingAbility)spec.GetPrimaryInstance();

            check("I-1 발동 성공 — 인스턴스·Spec 활성, ActiveCount 1", asc.TryActivateAbility(handle) && instance.IsActive && spec.IsActive && spec.ActiveCount == 1 && RecordingAbility.Log.Contains("I.Activate"));
            check("I-2 활성 중 다시 발동은 실패(ActivateAbility 한 번)", !asc.TryActivateAbility(handle) && spec.ActiveCount == 1 && Count("I.Activate") == 1);

            instance.EndAbility(handle, asc.AbilityActorInfo, false);
            check("I-3 EndAbility — 비활성, ActiveCount 0", !instance.IsActive && !spec.IsActive && spec.ActiveCount == 0 && RecordingAbility.Log.Contains("I.End"));

            instance.EndAbility(handle, asc.AbilityActorInfo, false);
            check("I-4 끝난 뒤 다시 EndAbility는 무시", spec.ActiveCount == 0 && Count("I.End") == 1);
            check("I-5 끝난 뒤 다시 발동 가능", asc.TryActivateAbility(handle) && spec.ActiveCount == 1);

            asc.CancelAbilityHandle(handle);
            check("I-6 CancelAbilityHandle — 취소로 끝남", !spec.IsActive && RecordingAbility.Log.Contains("I.Cancel") && Count("I.End") == 1);
            asc.CancelAbilityHandle(handle);
            check("I-7 비활성 Spec 취소는 아무것도 안 함", Count("I.Cancel") == 1 && spec.ActiveCount == 0);

            RecordingAbility.Log.Clear();
            instance.EndOnActivate = true;
            bool instantActivated = asc.TryActivateAbility(handle);
            instance.EndOnActivate = false;
            check("I-8 즉발형(ActivateAbility 안에서 EndAbility) — true를 돌려주고 이미 비활성", instantActivated && !spec.IsActive && spec.ActiveCount == 0 && Sequence("I.Activate", "I.End"));

            RecordingAbility.Log.Clear();
            instance.BlockActivation = true;
            check("I-9 CanActivateAbility가 거부하면 실패(ActivateAbility 안 불림)", !asc.TryActivateAbility(handle) && !spec.IsActive && !RecordingAbility.Log.Contains("I.Activate"));
            instance.BlockActivation = false;

            asc.InitAbilityActorInfo(go, null);
            check("I-10 Avatar가 없으면 실패", !asc.TryActivateAbility(handle) && !spec.IsActive);
            asc.InitAbilityActorInfo(go, go);

            Debug.Log("[GA Self-Check] 아래 경고 로그 1줄은 I-11의 정상 출력이다.");
            check("I-11 없는 핸들 발동은 실패", !asc.TryActivateAbility(GameplayAbilitySpecHandle.Invalid));

            // I-12: 같은 정의로 부여된 Spec들을 정의로 한꺼번에 취소
            var second = new GameplayAbilitySpec(asset);
            asc.GiveAbility(second);
            asc.TryActivateAbility(handle);
            asc.TryActivateAbility(second.Handle);
            RecordingAbility.Log.Clear();
            asc.CancelAbility(asset);
            check("I-12 CancelAbility(정의) — 같은 정의의 활성 Spec 모두 취소", !spec.IsActive && !second.IsActive && Count("I.Cancel") == 2);

            // I-13: 활성 중 해제하면 취소가 아닌 종료로 끝낸 뒤 OnRemoveAbility
            asc.TryActivateAbility(second.Handle);
            RecordingAbility.Log.Clear();
            GameplayAbility secondInstance = second.GetPrimaryInstance();
            asc.ClearAbility(second.Handle);
            check("I-13 활성 중 ClearAbility — End → OnRemove, 인스턴스 비활성", Sequence("I.End", "I.OnRemove") && !RecordingAbility.Log.Contains("I.Cancel") && !secondInstance.IsActive && asc.FindAbilitySpecFromHandle(second.Handle) == null);

            // I-14: ActivateAbility 안에서 자기를 해제하면 락이 풀린 뒤 해제되고, 그때 활성이면 끝낸다
            bool listedDuringActivate = false;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate")
                {
                    asc.ClearAbility(s.Handle);
                    listedDuringActivate = asc.FindAbilitySpecFromHandle(s.Handle) == s && s.PendingRemove;
                }
            };
            RecordingAbility.Log.Clear();
            bool selfClearActivated = asc.TryActivateAbility(handle);
            RecordingAbility.Hook = null;
            check("I-14 발동 중 자기 해제 — 발동 동안 목록에 남고, 락이 풀린 뒤 End → OnRemove", selfClearActivated && listedDuringActivate && Sequence("I.End", "I.OnRemove") && asc.FindAbilitySpecFromHandle(handle) == null);

            // I-15: 해제 대기(PendingRemove) Spec은 발동하지 않는다
            var pending = new GameplayAbilitySpec(asset);
            asc.GiveAbility(pending);
            bool pendingActivated;
            using (new ScopedAbilityListLock(asc))
            {
                asc.ClearAbility(pending.Handle);
                pendingActivated = asc.TryActivateAbility(pending.Handle);
            }

            check("I-15 해제 대기 Spec은 발동 실패", !pendingActivated && asc.FindAbilitySpecFromHandle(pending.Handle) == null);

            // I-16: ClearAllAbilities도 활성 중인 것을 끝낸다
            var last = new GameplayAbilitySpec(asset);
            asc.GiveAbility(last);
            asc.TryActivateAbility(last.Handle);
            GameplayAbility lastInstance = last.GetPrimaryInstance();
            RecordingAbility.Log.Clear();
            asc.ClearAllAbilities();
            check("I-16 활성 중 ClearAllAbilities — End → OnRemove", Sequence("I.End", "I.OnRemove") && !lastInstance.IsActive && asc.GetActivatableAbilities().Count == 0);
        }

        // ── J: GA에서 GE 적용 ─────────────────────────────────────────────────────

        private static void RunEffectChecks(AbilitySystemComponent asc, GameObject go, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            EnsureInitialized(asc);

            GameObject targetGo = new GameObject("GameplayAbilitySelfCheck_Target");
            created.Add(targetGo);
            AbilitySystemComponent target = targetGo.AddComponent<AbilitySystemComponent>();
            EnsureInitialized(target);

            GameObject sourceGo = new GameObject("GameplayAbilitySelfCheck_Source");
            created.Add(sourceGo);

            GameplayEffectAsset infinite = MakeEffect(GameplayEffectType.Infinite, created);
            GameplayAbilityAsset asset = MakeAsset("J", created);
            var spec = new GameplayAbilitySpec(asset, 3, GameplayAbilitySpec.NoInputID, sourceGo);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            GameplayAbility instance = spec.GetPrimaryInstance();
            GameplayAbilityActorInfo actorInfo = asc.AbilityActorInfo;

            GameplayEffectContextHandle context = instance.MakeEffectContext(handle, actorInfo);
            check("J-1 MakeEffectContext — Instigator = ASC, EffectCauser = Avatar, SourceObject = Spec 출처", context.GetInstigator() == asc && context.EffectCauser == go && context.GetSourceObject() == sourceGo);

            GameplayEffectSpec outgoing = instance.MakeOutgoingGameplayEffectSpec(handle, actorInfo, infinite, 2f);
            check("J-2 MakeOutgoingGameplayEffectSpec — 레벨·GE·Instigator", outgoing != null && Mathf.Approximately(outgoing.Level, 2f) && outgoing.Context.GetInstigator() == asc);

            GameplayEffectSpec current = instance.MakeOutgoingGameplayEffectSpec(infinite);
            check("J-3 현재 Spec·액터 정보 판은 같은 컨텍스트(Instigator·출처)", current != null && current.Context.GetInstigator() == asc && current.Context.GetSourceObject() == sourceGo);

            ActiveGameplayEffectHandle ownerHandle = instance.ApplyGameplayEffectToOwner(handle, actorInfo, infinite, 1f);
            ActiveGameplayEffect ownerActive = asc.GetActiveGameplayEffect(ownerHandle);
            check("J-4 ApplyGameplayEffectToOwner — Owner ASC에 활성 GE", ownerHandle.IsValid && ownerActive != null && ownerActive.Spec.Context.GetInstigator() == asc);

            ActiveGameplayEffectHandle targetHandle = instance.ApplyGameplayEffectToTarget(handle, actorInfo, target, infinite, 1f);
            ActiveGameplayEffect targetActive = target.GetActiveGameplayEffect(targetHandle);
            check("J-5 ApplyGameplayEffectToTarget — 대상 ASC에 활성 GE, Instigator는 어빌리티 쪽 ASC", targetHandle.IsValid && targetActive != null && targetActive.Spec.Context.GetInstigator() == asc && asc.GetActiveGameplayEffect(targetHandle) == null);

            check("J-6 GE null을 Owner에 — Invalid(로그 없음)", !instance.ApplyGameplayEffectToOwner(handle, actorInfo, null, 1f).IsValid);
            Debug.Log("[GA Self-Check] 아래 에러 로그 1줄은 J-7의 정상 출력이다.");
            check("J-7 GE null을 대상에 — Invalid", !instance.ApplyGameplayEffectToTarget(handle, actorInfo, target, null, 1f).IsValid);
            check("J-8 대상 null — Invalid", !instance.ApplyGameplayEffectToTarget(handle, actorInfo, null, infinite, 1f).IsValid);

            check("J-9 GetAbilityLevel = Spec 레벨(3)", instance.GetAbilityLevel() == 3);
            Debug.Log("[GA Self-Check] 아래 경고 로그 1줄은 J-10의 정상 출력이다.");
            check("J-10 없는 핸들의 레벨은 1", instance.GetAbilityLevel(GameplayAbilitySpecHandle.Invalid, actorInfo) == 1);

            // J-11: 출처가 GameObject가 아니면(예: 에셋) 컨텍스트에 넘기지 않는다
            var assetSourced = new GameplayAbilitySpec(asset, 1, GameplayAbilitySpec.NoInputID, infinite);
            asc.GiveAbility(assetSourced);
            GameplayEffectContextHandle assetContext = assetSourced.GetPrimaryInstance().MakeEffectContext(assetSourced.Handle, actorInfo);
            check("J-11 GameObject가 아닌 출처는 컨텍스트에 안 들어감", assetContext.GetInstigator() == asc && assetContext.GetSourceObject() == null);

            // J-12: 발동 중(ActivateAbility 안)에 대상에게 GE를 거는 실제 흐름
            ActiveGameplayEffectHandle fromActivate = ActiveGameplayEffectHandle.Invalid;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && s == spec)
                {
                    fromActivate = ability.ApplyGameplayEffectToTarget(s.Handle, ability.CurrentActorInfo, target, infinite, ability.GetAbilityLevel());
                    ability.EndAbility(s.Handle, ability.CurrentActorInfo, false);
                }
            };
            bool activated = asc.TryActivateAbility(handle);
            RecordingAbility.Hook = null;
            ActiveGameplayEffect activateActive = target.GetActiveGameplayEffect(fromActivate);
            check("J-12 ActivateAbility에서 대상에 GE(레벨 = 어빌리티 레벨) 후 종료", activated && activateActive != null && Mathf.Approximately(activateActive.Spec.Level, 3f) && !spec.IsActive);
        }

        // ── K: Per Execution 인스턴싱 ─────────────────────────────────────────────

        private static void RunPerExecutionChecks(AbilitySystemComponent asc, GameObject go, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            RecordingAbility.Log.Clear();

            check("K-0 정책 기본값은 Per Execution(UE와 같음)", new TestGameplayAbility().InstancingPolicy == GameplayAbilityInstancingPolicy.InstancedPerExecution);

            GameplayAbilityAsset asset = MakeAsset("K", created, GameplayAbilityInstancingPolicy.InstancedPerExecution);
            var template = (RecordingAbility)asset.AbilityTemplate;
            var spec = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);

            check("K-1 부여 — 인스턴스 없음, 원형이 OnGive·OnAvatarSet을 받음", spec.GetPrimaryInstance() == null && spec.GetAbilityInstances().Count == 0 && Sequence("K.OnGive", "K.OnAvatarSet"));
            check("K-2 원형에는 액터 정보를 쓰지 않음", template.CurrentActorInfo == null && !template.IsInstantiated);

            GameplayAbility first = null;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && s == spec)
                {
                    first = ability;
                }
            };
            bool firstActivated = asc.TryActivateAbility(handle);
            RecordingAbility.Hook = null;
            check("K-3 발동 — 새 인스턴스 1개(원형 아님), 활성, 액터 정보 있음", firstActivated && spec.GetAbilityInstances().Count == 1 && first != null && !ReferenceEquals(first, template)
                && first.IsInstantiated && first.IsActive && first.CurrentActorInfo == asc.AbilityActorInfo && first.CurrentAbilitySpecHandle.Equals(handle));
            check("K-4 활성 중 다시 발동 — 성공, 인스턴스 2개·ActiveCount 2", asc.TryActivateAbility(handle) && spec.GetAbilityInstances().Count == 2 && spec.ActiveCount == 2 && !ReferenceEquals(spec.GetAbilityInstances()[0], spec.GetAbilityInstances()[1]));

            first.EndAbility(handle, asc.AbilityActorInfo, false);
            check("K-5 하나 종료 — 그 인스턴스만 빠짐, ActiveCount 1, Spec은 여전히 활성", spec.GetAbilityInstances().Count == 1 && !ReferenceEquals(spec.GetAbilityInstances()[0], first) && spec.ActiveCount == 1 && spec.IsActive && !first.IsActive);

            RecordingAbility.Log.Clear();
            asc.CancelAbilityHandle(handle);
            check("K-6 CancelAbilityHandle — 남은 인스턴스 취소, 인스턴스 0·ActiveCount 0", spec.GetAbilityInstances().Count == 0 && spec.ActiveCount == 0 && Count("K.Cancel") == 1);

            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && s == spec)
                {
                    ability.EndAbility(s.Handle, ability.CurrentActorInfo, false);
                }
            };
            bool instantActivated = asc.TryActivateAbility(handle);
            RecordingAbility.Hook = null;
            check("K-7 즉발형 — 발동 안에서 끝나면 인스턴스가 남지 않음", instantActivated && spec.GetAbilityInstances().Count == 0 && spec.ActiveCount == 0);

            template.BlockActivation = true;
            check("K-8 CanActivateAbility는 원형이 판단(원형이 거부하면 실패, 인스턴스 안 만듦)", !asc.TryActivateAbility(handle) && spec.GetAbilityInstances().Count == 0);
            template.BlockActivation = false;

            RecordingAbility.Log.Clear();
            GameObject otherAvatar = new GameObject("GameplayAbilitySelfCheck_KAvatar");
            created.Add(otherAvatar);
            asc.InitAbilityActorInfo(go, otherAvatar);
            check("K-9 Avatar가 바뀌면 원형이 OnAvatarSet을 받음", RecordingAbility.Log.Contains("K.OnAvatarSet") && template.CurrentActorInfo == null);
            asc.InitAbilityActorInfo(go, go);

            // K-10: 활성 인스턴스가 여럿일 때 해제 — 모두 종료(취소 아님) 뒤 원형이 OnRemove
            asc.TryActivateAbility(handle);
            asc.TryActivateAbility(handle);
            RecordingAbility.Log.Clear();
            asc.ClearAbility(handle);
            check("K-10 활성 2개 중 ClearAbility — End 2번 → 원형 OnRemove, 목록에서 빠짐", Count("K.End") == 2 && !RecordingAbility.Log.Contains("K.Cancel") && Sequence("K.End", "K.OnRemove")
                && spec.GetAbilityInstances().Count == 0 && asc.FindAbilitySpecFromHandle(handle) == null);

            // K-11: Per Actor와 섞여 있어도 각자 규칙대로
            GameplayAbilityAsset actorAsset = MakeAsset("KActor", created);
            var actorSpec = new GameplayAbilitySpec(actorAsset);
            asc.GiveAbility(actorSpec);
            check("K-11 Per Actor는 부여 때 인스턴스 1개(대표 인스턴스), 원형과 다름", actorSpec.GetAbilityInstances().Count == 1 && actorSpec.GetPrimaryInstance() != null && actorSpec.GetPrimaryInstance().IsInstantiated);
        }

        // ── L: 코스트·쿨다운·커밋·태그 조건 ───────────────────────────────────────

        private static void RunCostCooldownTagChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            EnsureInitialized(asc);

            GameplayTag cooldownTag = CooldownTag.GetTag();
            GameplayTag ownedTag = OwnedTag.GetTag();
            GameplayTag requiredTag = RequiredTag.GetTag();
            GameplayTag blockedTag = BlockedTag.GetTag();
            GameplayTag blockedChildTag = BlockedChildTag.GetTag();
            bool tagsValid = cooldownTag.IsValid() && ownedTag.IsValid() && requiredTag.IsValid() && blockedTag.IsValid() && blockedChildTag.IsValid();
            check("L-0 native 태그 Test.GA.* 유효", tagsValid);
            if (!tagsValid)
            {
                return;
            }

            GameplayAttributeHandle stamina = CharacterAttributeSet.Stamina;
            asc.AddSpawnedAttribute(new CharacterAttributeSet());

            GameplayEffectAsset costEffect = MakeInstantAddBaseEffect(stamina, -30f, created);
            GameplayEffectAsset cooldownEffect = MakeCooldownEffect(cooldownTag, 10f, created);

            var cooldownTags = new GameplayTagContainer();
            cooldownTags.AddTag(cooldownTag);

            // 코스트·쿨다운 — 발동 안에서 커밋하고 끝내는 즉발형 흐름
            GameplayAbilityAsset asset = MakeAsset("L", created);
            AssignCostAndCooldown(asset, costEffect, cooldownEffect);

            var spec = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            GameplayAbility instance = spec.GetPrimaryInstance();

            bool committed = false;
            Action<AbilitySystemComponent> beforeCommit = null;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && ability.Name == "L")
                {
                    beforeCommit?.Invoke(asc);
                    committed = ability.CommitAbility(s.Handle, ability.CurrentActorInfo);
                    ability.EndAbility(s.Handle, ability.CurrentActorInfo, false);
                }
            };

            asc.SetAttributeBaseValue(stamina, 50f);
            bool activated = asc.TryActivateAbility(handle);
            check("L-1 커밋 — 코스트 적용(Stamina 50 → 20), 쿨다운 태그 부여", activated && committed && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 20f) && asc.HasMatchingGameplayTag(cooldownTag) && !spec.IsActive);

            instance.GetCooldownTimeRemainingAndDuration(handle, asc.AbilityActorInfo, out float remaining, out float duration);
            check("L-2 남은 쿨다운 = 10, 전체 = 10", Mathf.Approximately(instance.GetCooldownTimeRemaining(asc.AbilityActorInfo), 10f) && Mathf.Approximately(remaining, 10f) && Mathf.Approximately(duration, 10f));

            RecordingAbility.Log.Clear();
            check("L-3 쿨다운 중엔 발동 실패(ActivateAbility 안 불림)", !instance.CheckCooldown(handle, asc.AbilityActorInfo) && !asc.TryActivateAbility(handle) && !RecordingAbility.Log.Contains("L.Activate"));

            asc.RemoveActiveEffectsWithGrantedTags(cooldownTags);
            check("L-4 쿨다운이 빠지면 쿨다운 0, 코스트 부족(Stamina 20 < 30)이면 발동 실패", Mathf.Approximately(instance.GetCooldownTimeRemaining(asc.AbilityActorInfo), 0f) && !instance.CheckCost(handle, asc.AbilityActorInfo) && !asc.TryActivateAbility(handle));

            // 발동 뒤 커밋 전에 자원이 줄면 커밋 실패 — 아무것도 적용하지 않는다
            asc.SetAttributeBaseValue(stamina, 40f);
            beforeCommit = a => a.SetAttributeBaseValue(stamina, 5f);
            bool lateActivated = asc.TryActivateAbility(handle);
            beforeCommit = null;
            check("L-5 커밋 직전 자원이 줄면 CommitAbility 실패 — 코스트·쿨다운 적용 안 함", lateActivated && !committed && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 5f) && !asc.HasMatchingGameplayTag(cooldownTag));

            asc.SetAttributeBaseValue(stamina, 30f);
            bool exactActivated = asc.TryActivateAbility(handle);
            check("L-6 딱 맞는 자원(30 - 30 = 0)은 통과", exactActivated && committed && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 0f));
            RecordingAbility.Hook = null;
            asc.RemoveActiveEffectsWithGrantedTags(cooldownTags);

            // 태그 조건
            GameplayAbilityAsset tagAsset = MakeAsset("LTag", created);
            var tagTemplate = (RecordingAbility)tagAsset.AbilityTemplate;
            tagTemplate.ActivationOwnedTags.AddTag(ownedTag);
            tagTemplate.ActivationRequiredTags.AddTag(requiredTag);
            tagTemplate.ActivationBlockedTags.AddTag(blockedTag);
            var tagSpec = new GameplayAbilitySpec(tagAsset);
            GameplayAbilitySpecHandle tagHandle = asc.GiveAbility(tagSpec);

            check("L-7 필요 태그가 없으면 실패", !asc.TryActivateAbility(tagHandle));

            asc.AddLooseGameplayTag(requiredTag);
            bool ownedDuringActivation = false;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && ability.Name == "LTag")
                {
                    ownedDuringActivation = asc.HasMatchingGameplayTag(ownedTag);
                }
            };
            bool tagActivated = asc.TryActivateAbility(tagHandle);
            RecordingAbility.Hook = null;
            check("L-8 필요 태그가 있으면 발동, 활성 중 Owner에 ActivationOwnedTags", tagActivated && ownedDuringActivation && asc.HasMatchingGameplayTag(ownedTag));

            tagSpec.GetPrimaryInstance().EndAbility(tagHandle, asc.AbilityActorInfo, false);
            check("L-9 종료하면 ActivationOwnedTags가 빠짐", !asc.HasMatchingGameplayTag(ownedTag));

            asc.AddLooseGameplayTag(blockedChildTag);
            check("L-10 차단 태그의 자식 태그만 가져도 실패(부모 매칭)", !asc.TryActivateAbility(tagHandle));
            asc.RemoveLooseGameplayTag(blockedChildTag);
            check("L-11 차단 태그가 빠지면 다시 발동", asc.TryActivateAbility(tagHandle));
            asc.CancelAbilityHandle(tagHandle);
            asc.RemoveLooseGameplayTag(requiredTag);

            // Per Execution 동시 활성 — 활성 수만큼 태그가 쌓이고 전부 끝나면 빠진다
            GameplayAbilityAsset execAsset = MakeAsset("LExec", created, GameplayAbilityInstancingPolicy.InstancedPerExecution);
            ((RecordingAbility)execAsset.AbilityTemplate).ActivationOwnedTags.AddTag(ownedTag);
            var execSpec = new GameplayAbilitySpec(execAsset);
            GameplayAbilitySpecHandle execHandle = asc.GiveAbility(execSpec);
            asc.TryActivateAbility(execHandle);
            asc.TryActivateAbility(execHandle);
            int stackedCount = asc.GetTagCount(ownedTag);
            asc.CancelAbilityHandle(execHandle);
            check("L-12 Per Execution 2개 활성 — ActivationOwnedTags 2개, 모두 취소하면 0", stackedCount == 2 && asc.GetTagCount(ownedTag) == 0);

            asc.ClearAllAbilities();
        }

        // ── M: TestGameplayAbility 통합 ───────────────────────────────────────────

        // 실제 하위 클래스(즉발형: 커밋 → 효과 GE → 종료)로 부여부터 쿨다운 만료까지 이어서 본다. 시간은 컨테이너 시계를 흘리고 Tick한다.
        private static void RunTestAbilityChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            EnsureInitialized(asc);

            GameplayTag cooldownTag = CooldownTag.GetTag();
            GameplayTag blockedTag = BlockedTag.GetTag();
            GameplayAttributeHandle stamina = CharacterAttributeSet.Stamina;
            GameplayAttributeHandle speed = CharacterAttributeSet.Speed;
            asc.AddSpawnedAttribute(new CharacterAttributeSet());
            asc.SetAttributeBaseValue(stamina, 50f);
            asc.SetAttributeBaseValue(speed, 0f);

            GameplayEffectAsset costEffect = MakeInstantAddBaseEffect(stamina, -30f, created);
            GameplayEffectAsset cooldownEffect = MakeCooldownEffect(cooldownTag, 10f, created);
            GameplayEffectAsset speedEffect = MakeInstantAddBaseEffect(speed, 5f, created);
            GameplayAbilityAsset asset = MakeTestAbilityAsset(costEffect, cooldownEffect, speedEffect, created);
            GameplayAbility template = asset.AbilityTemplate;
            template.ActivationBlockedTags.AddTag(blockedTag);

            var spec = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            check("M-1 TestGameplayAbility 부여 — 기본 정책 Per Execution이라 인스턴스 없음", handle.IsValid && template is TestGameplayAbility && spec.GetAbilityInstances().Count == 0);

            bool activated = asc.TryActivateAbility(handle);
            check("M-2 발동 → 커밋(Stamina 50 → 20, 쿨다운 태그) → 효과(Speed 0 → 5) → 종료(비활성, 인스턴스 0)", activated && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 20f) && Mathf.Approximately(asc.GetAttributeCurrentValue(speed), 5f) && asc.HasMatchingGameplayTag(cooldownTag) && !spec.IsActive && spec.GetAbilityInstances().Count == 0);

            check("M-3 쿨다운 중엔 발동 실패 — Stamina·Speed 그대로", !asc.TryActivateAbility(handle) && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 20f) && Mathf.Approximately(asc.GetAttributeCurrentValue(speed), 5f));

            Tick(asc, 4f);
            check("M-4 4초 흐름 — 남은 쿨다운 10 → 6, 여전히 발동 실패", Mathf.Approximately(template.GetCooldownTimeRemaining(asc.AbilityActorInfo), 6f) && asc.HasMatchingGameplayTag(cooldownTag) && !asc.TryActivateAbility(handle));

            Tick(asc, 6.5f);
            check("M-5 쿨다운 Duration(10초)이 지나면(4 + 6.5초) 쿨다운 태그가 빠지고 남은 쿨다운 0", !asc.HasMatchingGameplayTag(cooldownTag) && Mathf.Approximately(template.GetCooldownTimeRemaining(asc.AbilityActorInfo), 0f));

            check("M-6 쿨다운이 끝나도 코스트 부족(Stamina 20 < 30)이면 실패 — 쿨다운·효과 안 걸림", !asc.TryActivateAbility(handle) && !asc.HasMatchingGameplayTag(cooldownTag) && Mathf.Approximately(asc.GetAttributeCurrentValue(speed), 5f));

            asc.SetAttributeBaseValue(stamina, 50f);
            asc.AddLooseGameplayTag(blockedTag);
            bool blockedActivated = asc.TryActivateAbility(handle);
            asc.RemoveLooseGameplayTag(blockedTag);
            check("M-7 차단 태그가 있으면 실패 — Stamina 50 그대로", !blockedActivated && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 50f));

            bool reactivated = asc.TryActivateAbility(handle);
            check("M-8 차단 태그가 빠지면 다시 발동 — Stamina 20, Speed 10", reactivated && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 20f) && Mathf.Approximately(asc.GetAttributeCurrentValue(speed), 10f));

            // 프레임 도중 적용: 프레임이 시작돼 시간이 먼저 흐르고, 그 프레임의 Tick보다 먼저 쿨다운이 걸린다
            Tick(asc, 11f);
            asc.SetAttributeBaseValue(stamina, 50f);
            ActiveGameplayEffectsContainer container = AdvanceClock(asc, 0.5f);
            bool midFrameActivated = asc.TryActivateAbility(handle);
            container.Tick();
            check("M-9 시간이 흐른 뒤 같은 프레임에 걸린 쿨다운은 그 프레임 Tick에서 줄지 않음(남은 10, 적용 전 0.5초는 안 셈)", midFrameActivated && Mathf.Approximately(template.GetCooldownTimeRemaining(asc.AbilityActorInfo), 10f));

            Tick(asc, 11f);
            asc.ClearAllAbilities();
        }

        // ── N: 커밋 경계 ──────────────────────────────────────────────────────────

        private static void RunCommitEdgeChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            EnsureInitialized(asc);

            GameplayTag cooldownTag = CooldownTag.GetTag();
            GameplayAttributeHandle stamina = CharacterAttributeSet.Stamina;
            asc.AddSpawnedAttribute(new CharacterAttributeSet());

            GameplayEffectAsset costEffect = MakeInstantAddBaseEffect(stamina, -30f, created);
            GameplayEffectAsset cooldownEffect = MakeCooldownEffect(cooldownTag, 10f, created);

            // 발동 뒤 커밋 전에 쿨다운이 걸리면 커밋 실패 — 코스트도, 두 번째 쿨다운도 적용하지 않는다
            GameplayAbilityAsset asset = MakeAsset("N", created);
            AssignCostAndCooldown(asset, costEffect, cooldownEffect);
            var spec = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
            GameplayAbility instance = spec.GetPrimaryInstance();

            bool committed = true;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && ability.Name == "N")
                {
                    ability.ApplyGameplayEffectToOwner(s.Handle, ability.CurrentActorInfo, cooldownEffect, 1f);
                    committed = ability.CommitAbility(s.Handle, ability.CurrentActorInfo);
                    ability.EndAbility(s.Handle, ability.CurrentActorInfo, false);
                }
            };

            asc.SetAttributeBaseValue(stamina, 50f);
            bool activated = asc.TryActivateAbility(handle);
            RecordingAbility.Hook = null;
            check("N-1 발동 뒤 커밋 전에 쿨다운이 걸리면 CommitAbility 실패 — Stamina 50 그대로, 쿨다운 태그는 직접 건 1개뿐", activated && !committed && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 50f) && asc.GetTagCount(cooldownTag) == 1);
            Tick(asc, 11f);

            // 커밋 대상이 무효면 아무것도 적용하지 않는다
            Debug.Log("[GA Self-Check] 아래 경고 로그 3줄은 N-2의 정상 출력이다.");
            bool invalidHandle = instance.CommitAbility(GameplayAbilitySpecHandle.Invalid, asc.AbilityActorInfo);
            bool missingSpec = instance.CommitAbility(GameplayAbilitySpecHandle.GenerateNewHandle(), asc.AbilityActorInfo);
            bool missingActorInfo = instance.CommitAbility(handle, null);
            check("N-2 무효 핸들·없는 Spec·액터 정보 null이면 CommitAbility 실패 — Stamina 50 그대로, 쿨다운 없음", !invalidHandle && !missingSpec && !missingActorInfo && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 50f) && !asc.HasMatchingGameplayTag(cooldownTag));

            // Per Execution 인스턴스에서 커밋해도 코스트·쿨다운이 Owner에 적용된다
            GameplayAbilityAsset execAsset = MakeAsset("NExec", created, GameplayAbilityInstancingPolicy.InstancedPerExecution);
            AssignCostAndCooldown(execAsset, costEffect, cooldownEffect);
            var execSpec = new GameplayAbilitySpec(execAsset);
            GameplayAbilitySpecHandle execHandle = asc.GiveAbility(execSpec);

            bool execCommitted = false;
            bool committedOnInstance = false;
            RecordingAbility.Hook = (evt, ability, s) =>
            {
                if (evt == "Activate" && ability.Name == "NExec")
                {
                    committedOnInstance = ability.IsInstantiated;
                    execCommitted = ability.CommitAbility(s.Handle, ability.CurrentActorInfo);
                    ability.EndAbility(s.Handle, ability.CurrentActorInfo, false);
                }
            };

            bool execActivated = asc.TryActivateAbility(execHandle);
            RecordingAbility.Hook = null;
            check("N-3 Per Execution 인스턴스에서 커밋 — Stamina 50 → 20, 쿨다운 태그, 끝나면 인스턴스 0", execActivated && committedOnInstance && execCommitted && Mathf.Approximately(asc.GetAttributeCurrentValue(stamina), 20f) && asc.HasMatchingGameplayTag(cooldownTag) && execSpec.GetAbilityInstances().Count == 0);

            Tick(asc, 11f);
            asc.ClearAllAbilities();
        }

        // ── O: 어빌리티 이벤트·실패 사유 ──────────────────────────────────────────

        private static void RunEventChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            EnsureInitialized(asc);

            GameplayTag cooldownTag = CooldownTag.GetTag();
            GameplayTag requiredTag = RequiredTag.GetTag();
            GameplayTag blockedTag = BlockedTag.GetTag();
            GameplayTag blockedChildTag = BlockedChildTag.GetTag();
            GameplayTag failCanActivateTag = FailCanActivateTag.GetTag();
            GameplayTag failCooldownTag = FailCooldownTag.GetTag();
            GameplayTag failCostTag = FailCostTag.GetTag();
            GameplayTag failBlockedTag = FailBlockedTag.GetTag();
            GameplayTag failMissingTag = FailMissingTag.GetTag();
            bool tagsValid = failCanActivateTag.IsValid() && failCooldownTag.IsValid() && failCostTag.IsValid() && failBlockedTag.IsValid() && failMissingTag.IsValid();
            check("O-0 native 태그 Test.GA.Fail.* 유효", tagsValid);
            if (!tagsValid)
            {
                return;
            }

            // 프로젝트 설정 에셋 대신 테스트 태그를 채운 설정을 끼운다.
            FieldInfo settingsField = typeof(GameplayAbilitiesSettings).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            object previousSettings = settingsField.GetValue(null);
            GameplayAbilitiesSettings settings = ScriptableObject.CreateInstance<GameplayAbilitiesSettings>();
            created.Add(settings);
            SetPrivateField(settings, "activateFailCanActivateAbilityTag", failCanActivateTag);
            SetPrivateField(settings, "activateFailCooldownTag", failCooldownTag);
            SetPrivateField(settings, "activateFailCostTag", failCostTag);
            SetPrivateField(settings, "activateFailTagsBlockedTag", failBlockedTag);
            SetPrivateField(settings, "activateFailTagsMissingTag", failMissingTag);
            settingsField.SetValue(null, settings);

            var events = new List<string>();
            var endedData = new List<AbilityEndedData>();
            GameplayAbility failedAbility = null;
            GameplayTagContainer failureTags = null;
            Action<GameplayAbility> onActivated = _ => events.Add("Activated");
            Action<GameplayAbility> onCommitted = _ => events.Add("Committed");
            Action<GameplayAbility, GameplayTagContainer> onFailed = (ability, tags) =>
            {
                events.Add("Failed");
                failedAbility = ability;
                failureTags = new GameplayTagContainer(tags);
            };
            Action<GameplayAbility> onEnded = _ => events.Add("Ended");
            Action<AbilityEndedData> onEndedWithData = data =>
            {
                events.Add("EndedWithData");
                endedData.Add(data);
            };
            Action<AbilityEndedData> afterEnded = null;
            Action<AbilityEndedData> onEndedHook = data => afterEnded?.Invoke(data);

            asc.OnAbilityActivated += onActivated;
            asc.OnAbilityCommitted += onCommitted;
            asc.OnAbilityFailed += onFailed;
            asc.OnAbilityEnded += onEnded;
            asc.OnAbilityEndedWithData += onEndedWithData;
            asc.OnAbilityEndedWithData += onEndedHook;
            try
            {
                GameplayAttributeHandle stamina = CharacterAttributeSet.Stamina;
                asc.AddSpawnedAttribute(new CharacterAttributeSet());

                GameplayEffectAsset costEffect = MakeInstantAddBaseEffect(stamina, -30f, created);
                GameplayEffectAsset cooldownEffect = MakeCooldownEffect(cooldownTag, 10f, created);
                var cooldownTags = new GameplayTagContainer();
                cooldownTags.AddTag(cooldownTag);

                // 발동 안에서 커밋하고 끝내는 즉발형 흐름
                GameplayAbilityAsset asset = MakeAsset("O", created);
                AssignCostAndCooldown(asset, costEffect, cooldownEffect);
                var spec = new GameplayAbilitySpec(asset);
                GameplayAbilitySpecHandle handle = asc.GiveAbility(spec);
                var instance = (RecordingAbility)spec.GetPrimaryInstance();

                bool committed = false;
                var commitFailureTags = new GameplayTagContainer();
                Action<AbilitySystemComponent> beforeCommit = null;
                RecordingAbility.Hook = (evt, ability, s) =>
                {
                    if (evt == "Activate" && ability.Name == "O")
                    {
                        events.Add("Activate");
                        beforeCommit?.Invoke(asc);
                        commitFailureTags.Reset();
                        committed = ability.CommitAbility(s.Handle, ability.CurrentActorInfo, commitFailureTags);
                        ability.EndAbility(s.Handle, ability.CurrentActorInfo, false);
                    }
                };

                asc.SetAttributeBaseValue(stamina, 50f);
                bool activated = asc.TryActivateAbility(handle);
                bool activationOrder = events.Count == 5 && events[0] == "Activated" && events[1] == "Activate" && events[2] == "Committed" && events[3] == "Ended" && events[4] == "EndedWithData";
                check("O-1 발동 → ActivateAbility → 커밋 → 종료 → 종료(데이터) 순서로 알림", activated && committed && activationOrder);

                AbilityEndedData ended = endedData.Count == 1 ? endedData[0] : default;
                check("O-2 종료 데이터 — 끝난 인스턴스·Spec 핸들·정상 종료", ended.AbilityThatEnded == instance && ended.AbilitySpecHandle.Equals(handle) && !ended.WasCancelled);

                // 쿨다운이 먼저 막아 코스트 부족(20 < 30)은 사유에 없다.
                events.Clear();
                bool cooldownActivated = asc.TryActivateAbility(handle);
                bool cooldownReason = failureTags != null && failureTags.HasTagExact(failCooldownTag) && failureTags.HasTagExact(cooldownTag) && !failureTags.HasTagExact(failCostTag);
                check("O-3 쿨다운 실패 — 실패 이벤트만, 사유 = 쿨다운 실패 태그 + 가진 쿨다운 태그", !cooldownActivated && events.Count == 1 && events[0] == "Failed" && failedAbility == instance && cooldownReason);

                asc.RemoveActiveEffectsWithGrantedTags(cooldownTags);
                events.Clear();
                bool costActivated = asc.TryActivateAbility(handle);
                check("O-4 코스트 실패 — 사유 = 코스트 실패 태그 하나", !costActivated && events.Count == 1 && failureTags.HasTagExact(failCostTag) && failureTags.Num() == 1);

                asc.SetAttributeBaseValue(stamina, 40f);
                beforeCommit = a => a.SetAttributeBaseValue(stamina, 5f);
                events.Clear();
                bool lateActivated = asc.TryActivateAbility(handle);
                beforeCommit = null;
                bool noCommitEvent = !events.Contains("Committed") && !events.Contains("Failed") && events.Contains("Ended");
                check("O-5 커밋 실패 — 커밋 이벤트 없음, CommitAbility가 사유(코스트 실패 태그)를 채움", lateActivated && !committed && noCommitEvent && commitFailureTags.HasTagExact(failCostTag));

                asc.SetAttributeBaseValue(stamina, 50f);
                instance.BlockActivation = true;
                events.Clear();
                bool overrideActivated = asc.TryActivateAbility(handle);
                instance.BlockActivation = false;
                check("O-6 재정의 조건이 사유 없이 거부 — 사유 = 일반 거부 태그 하나", !overrideActivated && events.Count == 1 && failureTags.HasTagExact(failCanActivateTag) && failureTags.Num() == 1);
                RecordingAbility.Hook = null;

                // 활성 중 재발동·취소
                GameplayAbilityAsset cancelAsset = MakeAsset("OCancel", created);
                var cancelSpec = new GameplayAbilitySpec(cancelAsset);
                GameplayAbilitySpecHandle cancelHandle = asc.GiveAbility(cancelSpec);
                asc.TryActivateAbility(cancelHandle);
                events.Clear();
                bool reactivated = asc.TryActivateAbility(cancelHandle);
                check("O-7 Per Actor 활성 중 재발동은 false지만 실패 이벤트는 없음", !reactivated && events.Count == 0);

                endedData.Clear();
                asc.CancelAbilityHandle(cancelHandle);
                bool cancelEvents = events.Count == 2 && events[0] == "Ended" && events[1] == "EndedWithData";
                check("O-8 취소 — 종료 이벤트 둘, 데이터는 취소", cancelEvents && endedData.Count == 1 && endedData[0].WasCancelled && endedData[0].AbilitySpecHandle.Equals(cancelHandle));

                // 차단·필요 태그 동시 실패 — 사유를 모두 모은다
                GameplayAbilityAsset tagAsset = MakeAsset("OTag", created);
                var tagTemplate = (RecordingAbility)tagAsset.AbilityTemplate;
                tagTemplate.ActivationRequiredTags.AddTag(requiredTag);
                tagTemplate.ActivationBlockedTags.AddTag(blockedTag);
                GameplayAbilitySpecHandle tagHandle = asc.GiveAbility(new GameplayAbilitySpec(tagAsset));
                asc.AddLooseGameplayTag(blockedChildTag);
                failureTags = null;
                bool tagActivated = asc.TryActivateAbility(tagHandle);
                asc.RemoveLooseGameplayTag(blockedChildTag);
                bool blockedReason = failureTags != null && failureTags.HasTagExact(failBlockedTag) && failureTags.HasTagExact(blockedChildTag) && !failureTags.HasTagExact(blockedTag);
                bool missingReason = failureTags != null && failureTags.HasTagExact(failMissingTag) && failureTags.HasTagExact(requiredTag);
                check("O-9 차단·필요 동시 실패 — 차단 실패 태그 + 막은 Owner 태그, 필요 실패 태그 + 빠진 필요 태그", !tagActivated && blockedReason && missingReason);

                // 종료 구독자가 락 밖에서 해제를 불러도 끝까지 정리된다
                GameplayAbilityAsset execAsset = MakeAsset("OExec", created, GameplayAbilityInstancingPolicy.InstancedPerExecution);
                var execSpec = new GameplayAbilitySpec(execAsset);
                GameplayAbilitySpecHandle execHandle = asc.GiveAbility(execSpec);
                asc.TryActivateAbility(execHandle);
                GameplayAbility execInstance = execSpec.GetAbilityInstances().Count == 1 ? execSpec.GetAbilityInstances()[0] : null;
                afterEnded = data =>
                {
                    afterEnded = null;
                    asc.ClearAbility(data.AbilitySpecHandle);
                };
                events.Clear();
                execInstance?.EndAbility(execHandle, asc.AbilityActorInfo, false);
                afterEnded = null;
                check("O-10 종료 구독자가 해제 — Spec이 빠지고 인스턴스도 정리", execInstance != null && events.Contains("Ended") && asc.FindAbilitySpecFromHandle(execHandle) == null && execSpec.GetAbilityInstances().Count == 0);
            }
            finally
            {
                RecordingAbility.Hook = null;
                asc.OnAbilityActivated -= onActivated;
                asc.OnAbilityCommitted -= onCommitted;
                asc.OnAbilityFailed -= onFailed;
                asc.OnAbilityEnded -= onEnded;
                asc.OnAbilityEndedWithData -= onEndedWithData;
                asc.OnAbilityEndedWithData -= onEndedHook;
                settingsField.SetValue(null, previousSettings);
                asc.ClearAllAbilities();
            }
        }

        // ── P: 어빌리티 태그·차단·취소 ────────────────────────────────────────────

        private static void RunBlockCancelChecks(AbilitySystemComponent asc, List<UnityEngine.Object> created, Action<string, bool> check)
        {
            asc.ClearAllAbilities();
            EnsureInitialized(asc);

            GameplayTag attackTag = AttackAbilityTag.GetTag();
            GameplayTag meleeTag = MeleeAbilityTag.GetTag();
            GameplayTag moveTag = MoveAbilityTag.GetTag();
            bool tagsValid = attackTag.IsValid() && meleeTag.IsValid() && moveTag.IsValid();
            check("P-0 native 태그 Test.GA.Ability.* 유효", tagsValid);
            if (!tagsValid)
            {
                return;
            }

            var attackTags = new GameplayTagContainer(attackTag);
            var meleeTags = new GameplayTagContainer(meleeTag);
            var moveTags = new GameplayTagContainer(moveTag);

            GameplayAbilitySpecHandle Give(string name, GameplayTagContainer assetTags, GameplayTagContainer blockTags, GameplayTagContainer cancelTags, GameplayAbilityInstancingPolicy policy = GameplayAbilityInstancingPolicy.InstancedPerActor)
            {
                GameplayAbilityAsset asset = MakeAsset(name, created, policy);
                GameplayAbility template = asset.AbilityTemplate;
                template.AssetTags.AppendTags(assetTags);
                template.BlockAbilitiesWithTag.AppendTags(blockTags);
                template.CancelAbilitiesWithTag.AppendTags(cancelTags);
                return asc.GiveAbility(new GameplayAbilitySpec(asset));
            }

            var none = new GameplayTagContainer();
            GameplayAbilitySpecHandle blockHandle = Give("PBlock", none, attackTags, none);
            GameplayAbilitySpecHandle meleeHandle = Give("PMelee", meleeTags, none, none);
            GameplayAbilitySpecHandle moveHandle = Give("PMove", moveTags, none, none);
            GameplayAbilitySpec blockSpec = asc.FindAbilitySpecFromHandle(blockHandle);
            GameplayAbilitySpec meleeSpec = asc.FindAbilitySpecFromHandle(meleeHandle);
            GameplayAbilitySpec moveSpec = asc.FindAbilitySpecFromHandle(moveHandle);

            GameplayTagContainer failureTags = null;
            Action<GameplayAbility, GameplayTagContainer> onFailed = (ability, tags) => failureTags = new GameplayTagContainer(tags);
            asc.OnAbilityFailed += onFailed;
            try
            {
                // 차단 — 활성인 동안 자식 태그를 가진 어빌리티까지 막고, 끝나면 풀린다
                asc.TryActivateAbility(blockHandle);
                bool blockedWhileActive = asc.AreAbilityTagsBlocked(meleeTags) && asc.GetBlockedAbilityTags().HasTagExact(attackTag) && !asc.AreAbilityTagsBlocked(moveTags);
                check("P-1 차단 어빌리티 활성 중 — 막힌 태그 Attack, 자식 Attack.Melee도 막힘, Move는 안 막힘", blockSpec.IsActive && blockedWhileActive);

                failureTags = null;
                bool meleeActivated = asc.TryActivateAbility(meleeHandle);
                GameplayTag failBlockedTag = GameplayAbilitiesSettings.Get().ActivateFailTagsBlockedTag;
                bool blockedReason = failureTags != null && failureTags.HasTagExact(meleeTag) && (!failBlockedTag.IsValid() || failureTags.HasTagExact(failBlockedTag));
                check("P-2 막힌 어빌리티는 발동 실패 — 사유 = 차단 실패 태그 + 막힌 자기 어빌리티 태그", !meleeActivated && blockedReason && asc.TryActivateAbility(moveHandle));
                asc.CancelAbilityHandle(moveHandle);

                asc.CancelAbilityHandle(blockHandle);
                check("P-3 차단 어빌리티가 끝나면 풀려 발동", asc.GetBlockedAbilityTags().IsEmpty() && asc.TryActivateAbility(meleeHandle));
                asc.CancelAbilityHandle(meleeHandle);

                // 같은 태그를 둘이 막으면 둘 다 끝나야 풀린다
                GameplayAbilitySpecHandle execBlockHandle = Give("PBlockExec", none, attackTags, none, GameplayAbilityInstancingPolicy.InstancedPerExecution);
                GameplayAbilitySpec execBlockSpec = asc.FindAbilitySpecFromHandle(execBlockHandle);
                asc.TryActivateAbility(execBlockHandle);
                asc.TryActivateAbility(execBlockHandle);
                GameplayAbility firstBlocker = execBlockSpec.GetAbilityInstances()[0];
                firstBlocker.EndAbility(execBlockHandle, asc.AbilityActorInfo, false);
                bool stillBlocked = asc.AreAbilityTagsBlocked(meleeTags);
                asc.CancelAbilityHandle(execBlockHandle);
                check("P-4 같은 태그를 두 인스턴스가 막으면 하나 끝나도 막혀 있고, 둘 다 끝나면 풀림", stillBlocked && !asc.AreAbilityTagsBlocked(meleeTags));

                // 활성 중 차단 끄고 켜기 — 꺼 둔 채 끝나도 개수가 어긋나지 않는다
                asc.TryActivateAbility(blockHandle);
                GameplayAbility blocker = blockSpec.GetPrimaryInstance();
                blocker.SetShouldBlockOtherAbilities(false);
                bool unblockedBySwitch = !asc.AreAbilityTagsBlocked(meleeTags) && !blocker.IsBlockingOtherAbilities();
                blocker.SetShouldBlockOtherAbilities(true);
                bool reblocked = asc.AreAbilityTagsBlocked(meleeTags);
                blocker.SetShouldBlockOtherAbilities(false);
                asc.CancelAbilityHandle(blockHandle);
                asc.TryActivateAbility(blockHandle);
                bool blocksAfterReuse = asc.AreAbilityTagsBlocked(meleeTags);
                asc.CancelAbilityHandle(blockHandle);
                check("P-5 SetShouldBlockOtherAbilities로 끄고 켬, 꺼 둔 채 끝난 뒤 다시 발동해도 정상으로 막고 풀림", unblockedBySwitch && reblocked && blocksAfterReuse && !asc.AreAbilityTagsBlocked(meleeTags));

                // 취소 — 발동하면 취소 태그를 가진 활성 어빌리티를 취소하되, 자기 자신은 빼고
                GameplayAbilitySpecHandle cancelHandle = Give("PCancel", attackTags, none, attackTags);
                GameplayAbilitySpec cancelSpec = asc.FindAbilitySpecFromHandle(cancelHandle);
                asc.TryActivateAbility(meleeHandle);
                asc.TryActivateAbility(moveHandle);
                RecordingAbility.Log.Clear();
                asc.TryActivateAbility(cancelHandle);
                check("P-6 취소 태그 Attack — 활성 Attack.Melee는 취소, Move는 유지, 같은 태그를 가진 자기 자신은 유지", RecordingAbility.Log.Contains("PMelee.Cancel") && !meleeSpec.IsActive && moveSpec.IsActive && cancelSpec.IsActive);
                asc.CancelAbilityHandle(cancelHandle);

                // ASC CancelAbilities — 포함·제외 태그
                asc.TryActivateAbility(meleeHandle);
                asc.CancelAbilities(null, moveTags);
                bool withoutPass = !meleeSpec.IsActive && moveSpec.IsActive;
                asc.CancelAbilities(moveTags);
                check("P-7 CancelAbilities — 제외 태그 Move면 Melee만 취소, 포함 태그 Move면 Move 취소", withoutPass && !moveSpec.IsActive);
            }
            finally
            {
                asc.OnAbilityFailed -= onFailed;
                asc.ClearAllAbilities();
            }
        }

        // ── 도우미 ────────────────────────────────────────────────────────────────

        private static GameplayModifier MakeAddBaseModifier(GameplayAttributeHandle handle, float magnitude)
        {
            object boxed = default(GameplayModifier);
            SetPrivateField(boxed, "attribute", new GameplayAttribute(handle));
            SetPrivateField(boxed, "operation", GameplayModifierOperation.AddBase);
            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);
            SetPrivateField(boxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);
            return (GameplayModifier)boxed;
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

        // 에디터에서 AddComponent하면 Awake가 불리지 않아 GE 컨테이너가 없다.
        private static void EnsureInitialized(AbilitySystemComponent asc)
        {
            const BindingFlags nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", nonPublicInstance);
            if (containerField != null && containerField.GetValue(asc) == null)
            {
                typeof(AbilitySystemComponent).GetMethod("Awake", nonPublicInstance)?.Invoke(asc, null);
            }
        }

        // 인스펙터와 같은 경로(SerializedObject)로 모디파이어 없는 GE를 메모리에 만든다.
        private static GameplayEffectAsset MakeEffect(GameplayEffectType type, List<UnityEngine.Object> created)
        {
            GameplayEffectAsset effect = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            effect.name = $"GE_SelfCheck_{type}";
            created.Add(effect);

            var serializedObject = new SerializedObject(effect);
            serializedObject.FindProperty("type").enumValueIndex = (int)type;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        // 어트리뷰트 하나에 AddBase 모디파이어 하나를 가진 Instant GE(코스트·효과용).
        private static GameplayEffectAsset MakeInstantAddBaseEffect(GameplayAttributeHandle attribute, float magnitude, List<UnityEngine.Object> created)
        {
            GameplayEffectAsset effect = MakeEffect(GameplayEffectType.Instant, created);
            SetPrivateField(effect, "modifiers", new List<GameplayModifier> { MakeAddBaseModifier(attribute, magnitude) });
            return effect;
        }

        // 지속시간 동안 태그 하나를 부여하는 Duration GE(쿨다운용).
        private static GameplayEffectAsset MakeCooldownEffect(GameplayTag cooldownTag, float duration, List<UnityEngine.Object> created)
        {
            GameplayEffectAsset effect = MakeEffect(GameplayEffectType.Duration, created);
            SetPrivateField(effect, "duration", duration);
            TargetTagsGameplayEffectComponent grantCooldown = new TargetTagsGameplayEffectComponent();
            grantCooldown.GetConfiguredTargetTagChanges().AddTag(cooldownTag);
            SetPrivateField(effect, "gameplayEffectComponents", new List<GameplayEffectComponent> { grantCooldown });
            effect.OnGameplayEffectChanged();
            return effect;
        }

        // 인스펙터와 같은 경로로 원형의 코스트·쿨다운 GE 칸을 채운다.
        private static void AssignCostAndCooldown(GameplayAbilityAsset asset, GameplayEffectAsset costEffect, GameplayEffectAsset cooldownEffect)
        {
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("abilityTemplate.costGameplayEffect").objectReferenceValue = costEffect;
            serializedObject.FindProperty("abilityTemplate.cooldownGameplayEffect").objectReferenceValue = cooldownEffect;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        // 인스펙터와 같은 경로로 TestGameplayAbility 원형(정책은 기본값)을 넣고 코스트·쿨다운·효과 GE 칸을 채운 정의 에셋.
        private static GameplayAbilityAsset MakeTestAbilityAsset(GameplayEffectAsset costEffect, GameplayEffectAsset cooldownEffect, GameplayEffectAsset testEffect, List<UnityEngine.Object> created)
        {
            GameplayAbilityAsset asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            asset.name = "GA_SelfCheck_Test";
            created.Add(asset);

            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("abilityTemplate").managedReferenceValue = new TestGameplayAbility();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            serializedObject.FindProperty("abilityTemplate._testEffect").objectReferenceValue = testEffect;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssignCostAndCooldown(asset, costEffect, cooldownEffect);
            return asset;
        }

        // 에디터에선 Time.time이 흐르지 않아, 컨테이너 시계를 지금 시각 + seconds로 바꿔 끼운다. Tick은 하지 않는다(프레임이 시작돼 시간만 흐른 상태).
        private static ActiveGameplayEffectsContainer AdvanceClock(AbilitySystemComponent asc, float seconds)
        {
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", NonPublicInstance);
            var container = (ActiveGameplayEffectsContainer)containerField.GetValue(asc);
            float worldTime = container.GetWorldTime() + seconds;
            Func<float> worldTimeSource = () => worldTime;
            typeof(ActiveGameplayEffectsContainer).GetMethod("SetWorldTimeSource", NonPublicInstance).Invoke(container, new object[] { worldTimeSource });
            return container;
        }

        // 에디터에선 ASC.Update가 돌지 않아 시간을 흘린 뒤 컨테이너 Tick을 직접 부른다.
        private static void Tick(AbilitySystemComponent asc, float deltaTime)
        {
            AdvanceClock(asc, deltaTime).Tick();
        }

        // 인스펙터와 같은 경로(SerializedObject)로 원형을 넣은 정의 에셋을 메모리에 만든다.
        // A~J는 부여 때 인스턴스가 생기는 Per Actor를 전제로 한다.
        private static GameplayAbilityAsset MakeAsset(string name, List<UnityEngine.Object> created, GameplayAbilityInstancingPolicy policy = GameplayAbilityInstancingPolicy.InstancedPerActor)
        {
            GameplayAbilityAsset asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            asset.name = $"GA_SelfCheck_{name}";
            created.Add(asset);

            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("abilityTemplate").managedReferenceValue = new RecordingAbility { Name = name };
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            serializedObject.FindProperty("abilityTemplate.instancingPolicy").enumValueIndex = (int)policy;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static bool Sequence(string first, string second)
        {
            int a = RecordingAbility.Log.IndexOf(first);
            int b = RecordingAbility.Log.IndexOf(second);
            return a >= 0 && b > a;
        }

        private static bool IsListed(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            foreach (GameplayAbilitySpec listed in asc.GetActivatableAbilities())
            {
                if (listed == spec)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Count(string entry)
        {
            int count = 0;
            foreach (string logged in RecordingAbility.Log)
            {
                if (logged == entry)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
