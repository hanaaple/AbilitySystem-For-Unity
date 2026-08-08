using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Attribute.Editor;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Editor
{
    /// <summary>
    /// ASC의 런타임 상태(AttributeSet 값·Active Effect)를 IMGUI로 그리는 공용 로직.
    /// 컴포넌트 인스펙터(<see cref="AbilitySystemComponentDrawer"/>)와 독립 창(<see cref="AbilitySystemComponentWindow"/>)이
    /// 같은 표시를 공유하도록 한곳에 모은다 — 두 진입점이 화면을 중복 구현하지 않게 하는 것이 목적이다.
    /// </summary>
    public static class AbilitySystemInspectorGUI
    {
        private const string SpawnedAttributeSetsFieldName = "_spawnedAttributeSets";
        private const string ActiveEffectsFieldName = "_activeEffects";
        private const string SourceSpecsFieldName = "_sourceSpecs";
        private const string TargetSpecsFieldName = "_targetSpecs";

        private const float NameColumnWidth = 0.40f;
        private const float BaseColumnX = 0.42f;
        private const float BaseColumnWidth = 0.27f;
        private const float CurrentColumnX = 0.72f;
        private const float CurrentColumnWidth = 0.28f;

        private static readonly FieldInfo SpawnedAttributeSetsField = typeof(AbilitySystemComponent)
            .GetField(SpawnedAttributeSetsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ActiveEffectsField = typeof(AbilitySystemComponent)
            .GetField(ActiveEffectsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        // 캡처 컨테이너의 source/target 캡처 목록은 private이므로, ASC 필드와 같은 리플렉션 패턴으로 읽어
        // 프로덕션 API에 디버그 전용 표면을 만들지 않고 인스펙터에만 노출한다.
        private static readonly FieldInfo SourceSpecsField = typeof(GameplayEffectAttributeCaptureSpecContainer)
            .GetField(SourceSpecsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo TargetSpecsField = typeof(GameplayEffectAttributeCaptureSpecContainer)
            .GetField(TargetSpecsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        // ── Runtime Attributes ────────────────────────────────────────────────────

        public static void DrawRuntimeAttributes(AbilitySystemComponent asc, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, "Runtime Attributes", true, EditorStyles.foldoutHeader);
            if (!foldout)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime AttributeSets are available in Play Mode.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            if (SpawnedAttributeSetsField == null)
            {
                EditorGUILayout.HelpBox($"Field '{SpawnedAttributeSetsFieldName}' was not found.", MessageType.Error);
                EditorGUI.indentLevel--;
                return;
            }

            var spawnedSets = SpawnedAttributeSetsField.GetValue(asc) as IReadOnlyDictionary<Type, AttributeSet>;

            if (spawnedSets == null || spawnedSets.Count == 0)
            {
                EditorGUILayout.HelpBox("No spawned AttributeSets.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (KeyValuePair<Type, AttributeSet> pair in spawnedSets)
            {
                DrawAttributeSet(pair.Key, pair.Value);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawAttributeSet(Type setType, AttributeSet set)
        {
            EditorGUILayout.LabelField(setType.Name, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            FieldInfo[] fields = AttributeReflectionUtility.GetAttributeDataFields(setType);
            if (fields.Length == 0)
            {
                EditorGUILayout.LabelField("No AttributeData fields.");
                EditorGUI.indentLevel--;
                return;
            }

            foreach (FieldInfo field in fields)
            {
                var data = (AttributeData)field.GetValue(set);
                DrawAttributeData(field.Name, data);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawAttributeData(string fieldName, AttributeData data)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect nameRect = new Rect(rect.x, rect.y, rect.width * NameColumnWidth, rect.height);
            Rect baseRect = new Rect(rect.x + rect.width * BaseColumnX, rect.y, rect.width * BaseColumnWidth, rect.height);
            Rect currentRect = new Rect(rect.x + rect.width * CurrentColumnX, rect.y, rect.width * CurrentColumnWidth, rect.height);

            EditorGUI.LabelField(nameRect, fieldName);
            EditorGUI.LabelField(baseRect, $"Base: {data.BaseValue:0.###}");
            EditorGUI.LabelField(currentRect, $"Current: {data.CurrentValue:0.###}");
        }

        // ── Active Effects ────────────────────────────────────────────────────────

        public static void DrawActiveEffects(AbilitySystemComponent asc, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, "Active Effects", true, EditorStyles.foldoutHeader);
            if (!foldout)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Active Effects are available in Play Mode.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            if (ActiveEffectsField == null)
            {
                EditorGUILayout.HelpBox($"Field '{ActiveEffectsFieldName}' was not found.", MessageType.Error);
                EditorGUI.indentLevel--;
                return;
            }

            var activeEffects = ActiveEffectsField.GetValue(asc) as IReadOnlyDictionary<ActiveGameplayEffectHandle, ActiveGameplayEffect>;

            if (activeEffects == null || activeEffects.Count == 0)
            {
                EditorGUILayout.HelpBox("No active effects.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            int index = 0;
            foreach (ActiveGameplayEffect active in activeEffects.Values)
            {
                if (index > 0)
                {
                    EditorGUILayout.Space(2f);
                }

                DrawActiveEffect(index++, active);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawActiveEffect(int index, ActiveGameplayEffect active)
        {
            GameplayEffectAsset def = active.Spec.Definition;

            EditorGUILayout.LabelField($"[{index}] {def.name}  ({def.Type})", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (def.Type == GameplayEffectType.Duration)
            {
                EditorGUILayout.LabelField("Remaining", $"{active.RemainingDuration:0.##} / {def.Duration:0.##} s");
            }

            if (def.Period > 0f)
            {
                EditorGUILayout.LabelField("Period Timer", $"{active.PeriodTimer:0.##} / {def.Period:0.##} s");
            }

            DrawModifiers(active.Spec);
            DrawExecutions(active.Spec);
            DrawCapturedAttributes(active.Spec);

            EditorGUI.indentLevel--;
        }

        private static void DrawModifiers(GameplayEffectSpec spec)
        {
            EditorGUILayout.LabelField("Modifiers", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            if (spec.Modifiers.Count == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                foreach (GameplayModifierSpec mod in spec.Modifiers)
                {
                    EditorGUILayout.LabelField($"{mod.Handle}  {mod.Operation}  {mod.EvaluatedMagnitude:0.###}");
                }
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawExecutions(GameplayEffectSpec spec)
        {
            EditorGUILayout.LabelField("Executions", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            if (spec.Executions.Count == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                foreach (GameplayEffectExecution execution in spec.Executions)
                {
                    DrawExecutionRow(execution.GetType());
                }
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>Execution 한 줄. 더블클릭하면 해당 타입의 스크립트를 IDE로 연다(커서에 링크 힌트).</summary>
        private static void DrawExecutionRow(Type type)
        {
            Rect rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            EditorGUI.LabelField(rect, $"{type.Name}  (double-click to open)", EditorStyles.linkLabel);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.clickCount == 2 && rect.Contains(e.mousePosition))
            {
                OpenScriptForType(type);
                e.Use();
            }
        }

        /// <summary>
        /// 타입 이름과 일치하는 <see cref="MonoScript"/> 에셋을 찾아 연다. <see cref="MonoScript.GetClass"/>가
        /// 파일명=클래스명일 때만 타입을 돌려주므로, 그 규칙을 따르는 스크립트만 열린다(못 찾으면 경고).
        /// </summary>
        private static void OpenScriptForType(Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    AssetDatabase.OpenAsset(script);
                    return;
                }
            }

            Debug.LogWarning($"[ASC Inspector] '{type.Name}' 스크립트 에셋을 찾지 못했다(파일명=클래스명이 아닐 수 있음).");
        }

        private static void DrawCapturedAttributes(GameplayEffectSpec spec)
        {
            EditorGUILayout.LabelField("Captured Attributes", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            GameplayEffectAttributeCaptureSpecContainer container = spec.CapturedRelevantAttributes;
            if (container == null || SourceSpecsField == null || TargetSpecsField == null)
            {
                EditorGUILayout.LabelField("(unavailable)");
                EditorGUI.indentLevel--;
                return;
            }

            var sourceSpecs = SourceSpecsField.GetValue(container) as IReadOnlyList<GameplayEffectAttributeCaptureSpec>;
            var targetSpecs = TargetSpecsField.GetValue(container) as IReadOnlyList<GameplayEffectAttributeCaptureSpec>;

            int total = (sourceSpecs?.Count ?? 0) + (targetSpecs?.Count ?? 0);
            if (total == 0)
            {
                EditorGUILayout.LabelField("(none)");
                EditorGUI.indentLevel--;
                return;
            }

            DrawCaptureSpecList(spec, "Source", sourceSpecs);
            DrawCaptureSpecList(spec, "Target", targetSpecs);

            EditorGUI.indentLevel--;
        }

        private static void DrawCaptureSpecList(GameplayEffectSpec spec, string sourceLabel, IReadOnlyList<GameplayEffectAttributeCaptureSpec> specs)
        {
            if (specs == null)
            {
                return;
            }

            foreach (GameplayEffectAttributeCaptureSpec captureSpec in specs)
            {
                GameplayEffectAttributeCaptureDefinition def = captureSpec.BackingDefinition;
                string attributeName = def.Attribute.ToString();
                string snapshotTag = def.Snapshot ? "  (snapshot)" : "";
                string origin = ResolveCaptureOrigin(spec, def);

                string valueText;
                if (!captureSpec.IsValid)
                {
                    valueText = "미캡처/무효";
                }
                else
                {
                    captureSpec.TryGetCapturedValue(AttributeCaptureValueType.BaseValue, out float baseValue);
                    captureSpec.TryGetCapturedValue(AttributeCaptureValueType.CurrentValue, out float currentValue);
                    valueText = $"Base: {baseValue:0.###}  Current: {currentValue:0.###}";
                }

                // 2-인자 LabelField(label,value)는 왼쪽을 labelWidth 고정폭에 그려 Attribute·Field·snapshot이 잘린다.
                // 한 줄 단일 문자열로 그려 컨트롤 전체 폭을 쓰게 한다(창을 늘린 만큼 다 보이도록).
                EditorGUILayout.LabelField($"{sourceLabel}  {attributeName}{snapshotTag}  «by {origin}»   →   {valueText}");
            }
        }

        /// <summary>
        /// 이 캡처 정의를 컨테이너에 등록시킨 공급원을 역추적한다
        /// (<see cref="GameplayEffectSpec.SetupAttributeCaptureDefinitions"/>의 두 경로 — AttributeBased modifier / Execution의 Defs).
        /// 컨테이너는 정의를 중복 제거해 보관하므로 여러 공급원이 같은 정의를 등록했으면 모두 모아 표시한다.
        /// </summary>
        private static string ResolveCaptureOrigin(GameplayEffectSpec spec, GameplayEffectAttributeCaptureDefinition def)
        {
            var origins = new List<string>();

            IReadOnlyList<GameplayModifier> modifiers = spec.Definition.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                GameplayModifier modifier = modifiers[i];
                if (modifier.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased
                    && modifier.AttributeBased.BackingAttribute.Equals(def))
                {
                    origins.Add($"Modifier[{i}]");
                }
            }

            foreach (GameplayEffectExecution execution in spec.Executions)
            {
                foreach (GameplayEffectAttributeCaptureDefinition executionDef in execution.Defs())
                {
                    if (executionDef.Equals(def))
                    {
                        origins.Add(execution.GetType().Name);
                        break;
                    }
                }
            }

            return origins.Count == 0 ? "?" : string.Join(", ", origins);
        }
    }
}
