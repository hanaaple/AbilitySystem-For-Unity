using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEngine;

namespace Editor.AbilitySystem
{
    [CustomEditor(typeof(AbilitySystemComponent))]
    public sealed class AbilitySystemComponentDrawer : UnityEditor.Editor
    {
        private const string SpawnedAttributeSetsFieldName = "_spawnedAttributeSets";
        private const string ActiveEffectsFieldName = "_activeEffects";

        private const float NameColumnWidth = 0.40f;
        private const float BaseColumnX = 0.42f;
        private const float BaseColumnWidth = 0.27f;
        private const float CurrentColumnX = 0.72f;
        private const float CurrentColumnWidth = 0.28f;

        private static readonly FieldInfo SpawnedAttributeSetsField = typeof(AbilitySystemComponent)
            .GetField(SpawnedAttributeSetsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ActiveEffectsField = typeof(AbilitySystemComponent)
            .GetField(ActiveEffectsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private bool _showRuntimeAttributes = true;
        private bool _showActiveEffects = true;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            DrawRuntimeAttributes();
            EditorGUILayout.Space(4f);
            DrawActiveEffects();
        }

        // ── Runtime Attributes ────────────────────────────────────────────────────

        private void DrawRuntimeAttributes()
        {
            _showRuntimeAttributes = EditorGUILayout.Foldout(
                _showRuntimeAttributes,
                "Runtime Attributes",
                true,
                EditorStyles.foldoutHeader);

            if (!_showRuntimeAttributes)
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

            var asc = (AbilitySystemComponent)target;
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

        private void DrawActiveEffects()
        {
            _showActiveEffects = EditorGUILayout.Foldout(
                _showActiveEffects,
                "Active Effects",
                true,
                EditorStyles.foldoutHeader);

            if (!_showActiveEffects)
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

            var asc = (AbilitySystemComponent)target;
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
            GameplayEffect def = active.Spec.Definition;

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

            EditorGUILayout.LabelField("Modifiers", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            foreach (GameplayModifierSpec mod in active.Spec.Modifiers)
            {
                EditorGUILayout.LabelField($"{mod.Handle}  {mod.Operation}  {mod.EvaluatedMagnitude:0.###}");
            }

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
    }
}