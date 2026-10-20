using System;
using System.Collections.Generic;
using System.IO;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 GA 부여·해제가 UE 5.6처럼 동작하는지 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // 호출을 기록하는 테스트 전용 어빌리티(RecordingAbility)를 원형으로 넣은 정의 에셋을 메모리에 만들어 본다.
    // A: 부여 기본 / B: 원형 깊은 복사 / C: 같은 정의 여러 번 / D: 해제 / E: 잘못된 입력 / F: 스코프 락·보류 / G: 액터 정보 / H: 정의 에셋 인스펙터·저장
    public static class GameplayAbilitySelfCheck
    {
        private const string TempAssetPath = "Assets/_Test/GA_SelfCheck_Temp.asset";

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
            check("A-10 OnGiveAbility 뒤 CurrentSpecHandle·CurrentActorInfo", instance != null && instance.CurrentSpecHandle.Equals(handle) && instance.CurrentActorInfo == asc.AbilityActorInfo);
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

        // ── 도우미 ────────────────────────────────────────────────────────────────

        // 인스펙터와 같은 경로(SerializedObject)로 원형을 넣은 정의 에셋을 메모리에 만든다.
        private static GameplayAbilityAsset MakeAsset(string name, List<UnityEngine.Object> created)
        {
            GameplayAbilityAsset asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            asset.name = $"GA_SelfCheck_{name}";
            created.Add(asset);

            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("abilityTemplate").managedReferenceValue = new RecordingAbility { Name = name };
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
