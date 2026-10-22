using System;
using System.Reflection;
using Game.Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Effect.Editor;
using UnityEditor;
using UnityEngine;

namespace Test.Editor
{
    // 클릭 한 번으로 Execution Calculation Modifier의 편집기·데이터·런타임 적용을 검증하는 에디터 전용 셀프체크(Play 모드 불필요).
    // A: Backing Data 후보(Defs) / B: 추가 시 기본값 / C: 요약·높이 / D: 저장 후 다시 읽기 / E: 런타임 적용
    // 편집기 함수는 GameplayEffectAssetDrawer의 private static이라 리플렉션으로 부른다.
    public static class CalculationModifierSelfCheck
    {
        private const string SourceGePath = "Assets/_Test/GE_Speed.asset";
        private const string TempFolder = "Assets/_SelfCheckTemp";
        private const string TempGePath = TempFolder + "/GE_CalculationModifierSelfCheck.asset";
        private const float Eps = 0.001f;

        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Tools/Ability System/Run Calculation Modifier Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[CalcModifier PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[CalcModifier FAIL] {label}");
                }
            }

            try
            {
                CreateTempGe();
                RunBackingDataChecks(Check);
                RunAddDefaultChecks(Check);
                RunSummaryAndHeightChecks(Check);
                RunPersistenceChecks(Check);
                RunRuntimeChecks(Check);
            }
            catch (Exception e)
            {
                Exception inner = e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                Check($"검사 중 예외: {inner.GetType().Name} — {inner.Message}", false);
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempGePath);
                AssetDatabase.DeleteAsset(TempFolder);
            }

            Debug.Log($"[CalcModifier Self-Check] passed {passed} / failed {failed}");
        }

        // ── A. Backing Data 후보 ──
        private static void RunBackingDataChecks(Action<string, bool> check)
        {
            var defs = GetCaptureDefinitions(typeof(SpeedBoostExecution).AssemblyQualifiedName);
            check("A-1 SpeedBoostExecution의 후보 = Defs() 2개(순서·값 같음)", defs.Length == 2 && defs[0].Equals(SpeedBoostExecution.defs[0]) && defs[1].Equals(SpeedBoostExecution.defs[1]));
            check("A-2 계산 클래스가 비었으면 후보 없음", GetCaptureDefinitions(string.Empty).Length == 0);
            check("A-3 Execution이 아닌 타입이면 후보 없음", GetCaptureDefinitions(typeof(CharacterAttributeSet).AssemblyQualifiedName).Length == 0);
        }

        // ── B. 추가 시 기본값 ──
        private static void RunAddDefaultChecks(Action<string, bool> check)
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameplayEffectAsset>(TempGePath));
            SerializedProperty executions = so.FindProperty("executions");
            SerializedProperty first = executions.GetArrayElementAtIndex(0);
            SerializedProperty calculationModifiers = first.FindPropertyRelative("calculationModifiers");
            var defs = GetCaptureDefinitions(typeof(SpeedBoostExecution).AssemblyQualifiedName);

            CallDrawer("AddScopedModifier", calculationModifiers, defs);
            SerializedProperty added = calculationModifiers.GetArrayElementAtIndex(0);
            check("B-1 첫 Calculation Modifier: 대상 = 첫 후보(health Target Snapshot)", ((GameplayEffectAttributeCaptureDefinition)added.FindPropertyRelative("capturedAttribute").boxedValue).Equals(defs[0]));
            check("B-2 기본값: Add (Base), Scalable Float 0, AttributeBased coefficient 1", added.FindPropertyRelative("modifierOp").enumValueIndex == (int)GameplayModifierOperation.AddBase
                && added.FindPropertyRelative("modifierMagnitude.magnitudeCalculationType").enumValueIndex == (int)MagnitudeCalculationType.ScalableFloat
                && Approx(added.FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue, 0f)
                && Approx(added.FindPropertyRelative("modifierMagnitude.attributeBasedMagnitude.coefficient").floatValue, 1f));
            check("B-3 새 칸은 칸·Magnitude·AttributeBased 묶음이 펼쳐진 채", added.isExpanded && added.FindPropertyRelative("modifierMagnitude").isExpanded && added.FindPropertyRelative("modifierMagnitude.attributeBasedMagnitude").isExpanded);

            // 앞 칸을 바꿔 둔 뒤 하나 더 — 삽입이 앞 칸을 복사해도 새 칸은 빈 값이어야 한다
            added.FindPropertyRelative("modifierOp").enumValueIndex = (int)GameplayModifierOperation.MultiplyAdditive;
            added.FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue = 25f;
            added.FindPropertyRelative("sourceTags._requireTags._gameplayTags").arraySize = 1;
            CallDrawer("AddScopedModifier", calculationModifiers, defs);
            SerializedProperty second = calculationModifiers.GetArrayElementAtIndex(1);
            check("B-4 두 번째 칸은 앞 칸 값을 물려받지 않는다(Add (Base)·0·태그 없음)", second.FindPropertyRelative("modifierOp").enumValueIndex == (int)GameplayModifierOperation.AddBase
                && Approx(second.FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue, 0f)
                && second.FindPropertyRelative("sourceTags._requireTags._gameplayTags").arraySize == 0);
            added.FindPropertyRelative("sourceTags._requireTags._gameplayTags").arraySize = 0;

            CallDrawer("AddScopedModifier", calculationModifiers, Array.Empty<GameplayEffectAttributeCaptureDefinition>());
            SerializedProperty noDefs = calculationModifiers.GetArrayElementAtIndex(2);
            var noDefsCapture = (GameplayEffectAttributeCaptureDefinition)noDefs.FindPropertyRelative("capturedAttribute").boxedValue;
            check("B-5 후보가 없을 때 추가하면 대상은 비어 있다", !noDefsCapture.Attribute.IsSet);
            calculationModifiers.DeleteArrayElementAtIndex(2);

            // 두 번째 칸은 stamina(Source)를 대상으로 바꿔 둔다 — D에서 저장·다시 읽기로 본다
            second.FindPropertyRelative("capturedAttribute").boxedValue = defs[1];
            second.FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue = 7f;

            // Execution 추가: 삽입이 앞 Execution(Calculation Modifier 2개)을 복사해도 새 Execution은 비어야 한다
            int executionCount = executions.arraySize;
            executions.arraySize++;
            SerializedProperty newExecution = executions.GetArrayElementAtIndex(executionCount);
            CallDrawer("AddExecution", newExecution, typeof(SpeedBoostExecution));
            check("B-6 새 Execution: 계산 클래스만 들어가고 Calculation Modifier 0개, 펼쳐진 채", newExecution.FindPropertyRelative("calculationClass").stringValue == typeof(SpeedBoostExecution).AssemblyQualifiedName
                && newExecution.FindPropertyRelative("calculationModifiers").arraySize == 0
                && newExecution.isExpanded && newExecution.FindPropertyRelative("calculationModifiers").isExpanded);
            executions.DeleteArrayElementAtIndex(executionCount);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── C. 요약·높이 ──
        private static void RunSummaryAndHeightChecks(Action<string, bool> check)
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameplayEffectAsset>(TempGePath));
            SerializedProperty execution = so.FindProperty("executions").GetArrayElementAtIndex(0);
            SerializedProperty calculationModifiers = execution.FindPropertyRelative("calculationModifiers");

            string executionSummary = (string)CallDrawer("GetExecutionSummary", execution.FindPropertyRelative("calculationClass"), calculationModifiers);
            check($"C-1 Execution 요약 = \"Speed Boost Execution — Calculation Modifiers 2\" (실제 \"{executionSummary}\")", executionSummary == "Speed Boost Execution   —   Calculation Modifiers 2");

            string scopedSummary = (string)CallDrawer("GetScopedModifierSummary", calculationModifiers.GetArrayElementAtIndex(0));
            check($"C-2 Calculation Modifier 요약 = Modifier와 같은 꼴 \"CharacterAttributeSet.health (Target, Snapshot) — Multiply (Additive)\" (실제 \"{scopedSummary}\")", scopedSummary == "CharacterAttributeSet.health (Target, Snapshot)   —   Multiply (Additive)");

            // 높이: 접으면 머리 줄만, 펼치면 늘고, 칸이 접히면 줄어든다(높이 계산과 그리기가 같은 함수라 그리기도 같이 맞는다)
            execution.isExpanded = false;
            float collapsed = MeasureExecution(execution);
            execution.isExpanded = true;
            calculationModifiers.isExpanded = false;
            float listCollapsed = MeasureExecution(execution);
            calculationModifiers.isExpanded = true;
            calculationModifiers.GetArrayElementAtIndex(0).isExpanded = true;
            float itemExpanded = MeasureExecution(execution);
            calculationModifiers.GetArrayElementAtIndex(0).isExpanded = false;
            float itemCollapsed = MeasureExecution(execution);
            check($"C-3 Execution 높이: 접힘 {collapsed:0} < 목록 접힘 {listCollapsed:0} < 칸 접힘 {itemCollapsed:0} < 칸 펼침 {itemExpanded:0}", collapsed < listCollapsed && listCollapsed < itemCollapsed && itemCollapsed < itemExpanded);
        }

        // ── D. 저장 후 다시 읽기 ──
        private static void RunPersistenceChecks(Action<string, bool> check)
        {
            var edited = AssetDatabase.LoadAssetAtPath<GameplayEffectAsset>(TempGePath);
            EditorUtility.SetDirty(edited);
            AssetDatabase.SaveAssetIfDirty(edited);
            Resources.UnloadAsset(edited);
            AssetDatabase.ImportAsset(TempGePath, ImportAssetOptions.ForceUpdate);

            var ge = AssetDatabase.LoadAssetAtPath<GameplayEffectAsset>(TempGePath);
            GameplayEffectExecutionDefinition execution = ge.Executions[0];
            var modifiers = execution.CalculationModifiers;
            check("D-1 다시 읽은 에셋: Execution 1개, Calculation Modifier 2개", ge.Executions.Count == 1 && modifiers.Count == 2);

            GameplayEffectExecutionScopedModifierInfo first = modifiers[0];
            GameplayEffectModifierMagnitude firstMagnitude = first.ModifierMagnitude;
            check("D-2 첫 칸: health(Target, Snapshot) · Multiply (Additive) · Scalable Float 25", first.CapturedAttribute.Equals(SpeedBoostExecution.defs[0])
                && first.ModifierOp == GameplayModifierOperation.MultiplyAdditive
                && firstMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.ScalableFloat);

            GameplayEffectExecutionScopedModifierInfo second = modifiers[1];
            check("D-3 둘째 칸: stamina(Source) · Add (Base), 태그 조건 비어 있음", second.CapturedAttribute.Equals(SpeedBoostExecution.defs[1])
                && second.ModifierOp == GameplayModifierOperation.AddBase
                && second.SourceTags.IsEmpty() && second.TargetTags.IsEmpty());

            var so = new SerializedObject(ge);
            SerializedProperty reloaded = so.FindProperty("executions").GetArrayElementAtIndex(0).FindPropertyRelative("calculationModifiers");
            check("D-4 magnitude 값 유지(25, 7)", Approx(reloaded.GetArrayElementAtIndex(0).FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue, 25f)
                && Approx(reloaded.GetArrayElementAtIndex(1).FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue, 7f));
        }

        // ── E. 런타임 적용 ──
        // 임시 GE의 Calculation Modifier: health(Target, Snapshot) × 25, stamina(Source) + 7. 자신에게 걸어 Source = Target.
        // SpeedBoostExecution = (health + stamina) * 0.01 + 10 → Calculation Modifier가 없을 때보다 ((100 * 25 + 57) - (100 + 50)) * 0.01 만큼 커진다.
        private static void RunRuntimeChecks(Action<string, bool> check)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameplayEffectAsset>(SourceGePath);
            var withModifiers = AssetDatabase.LoadAssetAtPath<GameplayEffectAsset>(TempGePath);

            GameObject go = new GameObject("CalculationModifierSelfCheck_Temp");
            try
            {
                AbilitySystemComponent asc = go.AddComponent<AbilitySystemComponent>();
                EnsureInitialized(asc);
                asc.AddSpawnedAttribute(new CharacterAttributeSet
                {
                    health = new AttributeData(100f, 100f),
                    stamina = new AttributeData(50f, 50f),
                });

                GameplayEffectSpec spec = asc.MakeOutgoingSpec(withModifiers);
                check("E-1 Calculation Modifier가 붙은 GE도 Execution 해석 정상(SpeedBoostExecution 1개)", spec.Executions.Count == 1 && spec.Executions[0] is SpeedBoostExecution);

                float speedWithout = ApplyAndReadSpeed(asc, source);
                float speedWith = ApplyAndReadSpeed(asc, withModifiers);
                const float expectedIncrease = ((100f * 25f + 57f) - (100f + 50f)) * 0.01f;
                check($"E-2 Execution이 Calculation Modifier를 건 캡처값을 읽는다 — Speed 차이 {speedWith - speedWithout:0.###} (기대 {expectedIncrease:0.###})", Approx(speedWith - speedWithout, expectedIncrease));

                check("E-3 실제 어트리뷰트는 그대로(health 100, stamina 50)", Approx(asc.GetAttributeCurrentValue(CharacterAttributeSet.Health), 100f)
                    && Approx(asc.GetAttributeCurrentValue(CharacterAttributeSet.Stamina), 50f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ── 헬퍼 ──

        // GE_Speed 사본에 Calculation Modifier를 붙여 쓴다. 원본은 건드리지 않는다.
        private static void CreateTempGe()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_SelfCheckTemp");
            }

            if (!AssetDatabase.CopyAsset(SourceGePath, TempGePath))
            {
                throw new InvalidOperationException($"{SourceGePath} 복사 실패");
            }
        }

        // 적용 전후 Speed Base 차이. Execution은 적용 때 실행되고(GE_Speed는 주기·적용 시 실행) BaseValue를 바꾼다.
        private static float ApplyAndReadSpeed(AbilitySystemComponent asc, GameplayEffectAsset ge)
        {
            asc.SetAttributeBaseValue(CharacterAttributeSet.Speed, 0f);
            ActiveGameplayEffectHandle handle = asc.ApplyGameplayEffectToSelf(ge);
            float speed = asc.GetAttributeBaseValue(CharacterAttributeSet.Speed);
            asc.RemoveActiveGameplayEffect(handle);
            return speed;
        }

        private static float MeasureExecution(SerializedProperty execution)
        {
            return (float)CallDrawer("LayoutExecutionElement", new Rect(0f, 0f, 400f, 0f), execution, false);
        }

        private static GameplayEffectAttributeCaptureDefinition[] GetCaptureDefinitions(string calculationClassName)
        {
            return (GameplayEffectAttributeCaptureDefinition[])CallDrawer("GetCaptureDefinitions", calculationClassName);
        }

        private static object CallDrawer(string methodName, params object[] args)
        {
            MethodInfo method = typeof(GameplayEffectAssetDrawer).GetMethod(methodName, PrivateStatic);
            if (method == null)
            {
                throw new InvalidOperationException($"GameplayEffectAssetDrawer.{methodName}을 찾지 못함");
            }

            return method.Invoke(null, args);
        }

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < Eps;
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
