using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor.AbilitySystem.Effect
{
    [CustomEditor(typeof(GameplayEffect))]
    public sealed class GameplayEffectDrawer : UnityEditor.Editor
    {
        private const string TypePropertyName = "type";
        private const string DurationPropertyName = "duration";
        private const string PeriodPropertyName = "period";
        private const string ExecuteOnApplicationPropertyName = "executePeriodicEffectOnApplication";
        private const string ModifiersPropertyName = "modifiers";

        private const string AttributeSetTypeNamePropertyName = "attributeSetTypeName";
        private const string FieldNamePropertyName = "fieldName";
        private const string OperationPropertyName = "operation";
        private const string MagnitudeCalculationTypePropertyName = "magnitudeCalculationType";
        private const string MagnitudePropertyName = "magnitude";

        private const float LineGap = 4f;
        private const float SectionGap = 8f;
        private const float ElementVerticalPadding = 6f;
        private const float ModifierLabelWidth = 140f;

        private static readonly Dictionary<GameplayModifierOperation, string> _operationDisplayNames = new()
        {
            { GameplayModifierOperation.AddBase,          "Add (Base)"          },
            { GameplayModifierOperation.MultiplyAdditive, "Multiply (Additive)" },
            { GameplayModifierOperation.DivideAdditive,   "Divide (Additive)"   },
            { GameplayModifierOperation.MultiplyCompound, "Multiply (Compound)" },
            { GameplayModifierOperation.AddFinal,         "Add (Final)"         },
            { GameplayModifierOperation.Override,         "Override"            },
        };

        private static readonly GameplayModifierOperation[] _operationValues =
            (GameplayModifierOperation[])Enum.GetValues(typeof(GameplayModifierOperation));

        private static readonly string[] _operationPopupOptions =
            _operationValues.Select(op => _operationDisplayNames[op]).ToArray();

        private SerializedProperty _type;
        private SerializedProperty _duration;
        private SerializedProperty _period;
        private SerializedProperty _executeOnApplication;
        private SerializedProperty _modifiers;

        private ReorderableList _list;

        private static string[] _setDisplayNames;
        private static readonly Dictionary<Type, string[]> _fieldNameCache = new();

        private void OnEnable()
        {
            _type = serializedObject.FindProperty(TypePropertyName);
            _duration = serializedObject.FindProperty(DurationPropertyName);
            _period = serializedObject.FindProperty(PeriodPropertyName);
            _executeOnApplication = serializedObject.FindProperty(ExecuteOnApplicationPropertyName);
            _modifiers = serializedObject.FindProperty(ModifiersPropertyName);

            BuildReorderableList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDurationPolicy();
            EditorGUILayout.Space(6f);
            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDurationPolicy()
        {
            EditorGUILayout.LabelField("Duration Policy", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_type, new GUIContent("Effect Type"));

            GameplayEffectType effectType = (GameplayEffectType)_type.enumValueIndex;

            if (effectType == GameplayEffectType.Duration)
            {
                EditorGUILayout.PropertyField(_duration, new GUIContent("Duration (s)"));
            }

            if (effectType != GameplayEffectType.Instant)
            {
                EditorGUILayout.PropertyField(_period, new GUIContent("Period (s)",
                    "0이면 주기 실행 없음. 0 초과면 해당 간격마다 반복 적용."));

                if (_period.floatValue > Mathf.Epsilon)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_executeOnApplication,
                        new GUIContent("Execute on Application",
                            "true: 적용 즉시 1회 실행 후 주기마다 실행\nfalse: 첫 주기 이후부터 실행"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        private void BuildReorderableList()
        {
            _list = new ReorderableList(
                serializedObject,
                _modifiers,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = DrawModifiersHeader,
                drawElementCallback = DrawModifierElement,
                elementHeightCallback = GetModifierElementHeight,
            };
        }

        private static void DrawModifiersHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Modifiers");
        }

        private float GetModifierElementHeight(int index)
        {
            SerializedProperty typeName = _modifiers.GetArrayElementAtIndex(index)
                .FindPropertyRelative(AttributeSetTypeNamePropertyName);

            if (string.IsNullOrEmpty(typeName.stringValue))
            {
                return EditorGUIUtility.singleLineHeight + ElementVerticalPadding * 2;
            }

            return EditorGUIUtility.singleLineHeight * 6 + LineGap * 5 + ElementVerticalPadding * 2 + SectionGap * 2;
        }

        private void DrawModifierElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty modifier = _modifiers.GetArrayElementAtIndex(index);
            SerializedProperty typeName  = modifier.FindPropertyRelative(AttributeSetTypeNamePropertyName);
            SerializedProperty fieldName = modifier.FindPropertyRelative(FieldNamePropertyName);
            SerializedProperty operation = modifier.FindPropertyRelative(OperationPropertyName);
            SerializedProperty calcType  = modifier.FindPropertyRelative(MagnitudeCalculationTypePropertyName);
            SerializedProperty magnitude = modifier.FindPropertyRelative(MagnitudePropertyName);

            rect.y += ElementVerticalPadding;

            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = lineH + LineGap;
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = ModifierLabelWidth;

            Type[] setTypes = AttributeReflectionUtility.GetAttributeSetTypes();
            _setDisplayNames ??= new[] { "None" }.Concat(setTypes.Select(t => t.Name)).ToArray();

            // Row 0: Attribute Set (popup index 0 = None, 1+ = 실제 타입)
            int setPopupIndex = GetSetPopupIndex(setTypes, typeName.stringValue);
            int newSetPopupIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width, lineH), "Attribute Set", setPopupIndex, _setDisplayNames);

            if (newSetPopupIndex != setPopupIndex)
            {
                typeName.stringValue = newSetPopupIndex == 0
                    ? string.Empty
                    : setTypes[newSetPopupIndex - 1].AssemblyQualifiedName;
                fieldName.stringValue = string.Empty;
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;
            DrawSeparatorIfNeeded(rect, index);

            // None이면 이하 전체 무시
            if (newSetPopupIndex == 0)
            {
                return;
            }

            EditorGUIUtility.labelWidth = ModifierLabelWidth;

            // Row 1: Attribute Field (SectionGap으로 Attribute Set과 분리)
            int resolvedTypeIndex = newSetPopupIndex - 1;
            float row1Y = rect.y + spacing + SectionGap;
            Rect row1 = new Rect(rect.x, row1Y, rect.width, lineH);

            string[] fieldNames = GetFieldNames(setTypes[resolvedTypeIndex]);
            int fieldIndex = Array.IndexOf(fieldNames, fieldName.stringValue);
            int newFieldIndex = EditorGUI.Popup(row1, "Attribute", fieldIndex, fieldNames);

            if (newFieldIndex >= 0 && newFieldIndex < fieldNames.Length)
            {
                fieldName.stringValue = fieldNames[newFieldIndex];
            }

            // Row 2: Modifier Op
            GameplayModifierOperation currentOp = (GameplayModifierOperation)operation.enumValueIndex;
            int opPopupIndex = Array.IndexOf(_operationValues, currentOp);
            int newOpPopupIndex = EditorGUI.Popup(
                new Rect(rect.x, row1Y + spacing, rect.width, lineH),
                "Modifier Op", opPopupIndex, _operationPopupOptions);
            operation.enumValueIndex = (int)_operationValues[newOpPopupIndex];

            DrawMagnitudeSection(
                new Rect(rect.x, row1Y + spacing * 2 + SectionGap, rect.width, lineH),
                lineH, spacing, calcType, magnitude);

            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        private static void DrawMagnitudeSection(Rect topLeft, float lineH, float spacing, SerializedProperty calcType, SerializedProperty magnitude)
        {
            const float boxPadX = 4f;
            const float boxPadY = 3f;
            float boxHeight = lineH + spacing * 2 + boxPadY * 2;
            Rect boxRect = new Rect(topLeft.x - boxPadX, topLeft.y - boxPadY, topLeft.width + boxPadX * 2, boxHeight);
            EditorGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.12f));

            EditorGUI.LabelField(
                new Rect(topLeft.x, topLeft.y, topLeft.width, lineH),
                "Magnitude", EditorStyles.boldLabel);

            EditorGUI.PropertyField(
                new Rect(topLeft.x, topLeft.y + spacing, topLeft.width, lineH),
                calcType, new GUIContent("Calc Type"));

            EditorGUI.PropertyField(
                new Rect(topLeft.x, topLeft.y + spacing * 2, topLeft.width, lineH),
                magnitude, new GUIContent("Magnitude"));
        }

        private void DrawSeparatorIfNeeded(Rect rect, int index)
        {
            if (index >= _modifiers.arraySize - 1)
            {
                return;
            }

            float separatorY = rect.y - ElementVerticalPadding + GetModifierElementHeight(index) - 1f;
            EditorGUI.DrawRect(new Rect(rect.x, separatorY, rect.width, 1f), new Color(0.35f, 0.35f, 0.35f, 0.6f));
        }

        private static int GetSetPopupIndex(Type[] setTypes, string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return 0;
            }

            for (int i = 0; i < setTypes.Length; i++)
            {
                if (setTypes[i].AssemblyQualifiedName == assemblyQualifiedName)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private static string[] GetFieldNames(Type setType)
        {
            if (_fieldNameCache.TryGetValue(setType, out string[] cached))
            {
                return cached;
            }

            string[] names = AttributeReflectionUtility.GetAttributeDataFields(setType)
                .Select(f => f.Name)
                .ToArray();

            _fieldNameCache[setType] = names;
            return names;
        }
    }
}
