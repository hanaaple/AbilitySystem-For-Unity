using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Effect;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    [CustomEditor(typeof(GameplayEffectAsset))]
    public sealed class GameplayEffectAssetDrawer : UnityEditor.Editor
    {
        private const string TypePropertyName = "type";
        private const string DurationPropertyName = "duration";
        private const string PeriodPropertyName = "period";
        private const string ExecuteOnApplicationPropertyName = "executePeriodicEffectOnApplication";
        private const string ModifiersPropertyName = "modifiers";
        private const string ExecutionsPropertyName = "executionTypeNames";

        private const string AttributeSetTypeNamePropertyName = "attributeSetTypeName";
        private const string FieldNamePropertyName = "fieldName";
        private const string OperationPropertyName = "operation";
        private const string MagnitudeCalculationTypePropertyName = "magnitudeCalculationType";
        private const string MagnitudePropertyName = "magnitude";

        // AttributeBased payload는 자체 PropertyDrawer(AttributeBasedMagnitudeDrawer)가 그린다. 여기선 위임만.
        private const string AttributeBasedPropertyName = "attributeBased";
        private const string CoefficientPropertyName = "coefficient";

        private const float LineGap = 4f;
        private const float SectionGap = 8f;
        private const float ElementVerticalPadding = 6f;
        private const float ModifierLabelWidth = 140f;
        private const float CollapsedHeaderPadding = 4f;

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
        private SerializedProperty _executions;

        private ReorderableList _list;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty(TypePropertyName);
            _duration = serializedObject.FindProperty(DurationPropertyName);
            _period = serializedObject.FindProperty(PeriodPropertyName);
            _executeOnApplication = serializedObject.FindProperty(ExecuteOnApplicationPropertyName);
            _modifiers = serializedObject.FindProperty(ModifiersPropertyName);
            _executions = serializedObject.FindProperty(ExecutionsPropertyName);

            BuildReorderableList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDurationPolicy();
            EditorGUILayout.Space(6f);
            DrawModifiersList();

            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(_executions, new GUIContent("Executions",
                "Modifier로 표현할 수 없는 커스텀 계산(GameplayEffectExecution 서브클래스).\n" +
                "Instant 또는 Period > 0 인 GE에서만 실행된다."), true);

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
                onAddCallback = OnAddModifier,
            };
        }

        // Unity는 struct 필드 초기화값을 직렬화에 반영하지 않으므로(새 요소=전부 0),
        // AttributeBased의 coefficient 기본값 1을 요소 추가 시점에 세팅한다(나머지는 0이 기본).
        private static void OnAddModifier(ReorderableList list)
        {
            int newIndex = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = newIndex;

            SerializedProperty added = list.serializedProperty.GetArrayElementAtIndex(newIndex);
            added.FindPropertyRelative(AttributeBasedPropertyName)
                 .FindPropertyRelative(CoefficientPropertyName).floatValue = 1f;

            // 새로 추가한 요소는 바로 편집할 수 있게 펼친 상태로 시작한다(isExpanded 기본값은 false).
            added.isExpanded = true;
        }

        /// <summary>
        /// 접힘 상태면 ReorderableList 대신 헤더만 그린다(ReorderableList에는 내장 foldout이 없다).
        /// 접힘 상태는 `SerializedProperty.isExpanded`에 저장돼 선택을 옮겨도 유지된다.
        /// </summary>
        private void DrawModifiersList()
        {
            if (_modifiers.isExpanded)
            {
                _list.DoLayoutList();
                return;
            }

            float lineH = EditorGUIUtility.singleLineHeight;
            Rect headerRect = GUILayoutUtility.GetRect(0f, lineH + CollapsedHeaderPadding * 2, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                // 펼침 상태와 같은 헤더 배경. defaultBehaviours는 DoList 전에는 null일 수 있어 가드한다.
                if (ReorderableList.defaultBehaviours != null)
                {
                    ReorderableList.defaultBehaviours.DrawHeaderBackground(headerRect);
                }
                else
                {
                    EditorGUI.DrawRect(headerRect, new Color(0.35f, 0.35f, 0.35f, 0.4f));
                }
            }

            // 배경 안에서 상하 padding만큼 띄우고, 좌우는 DoLayoutList의 헤더 내부 여백과 맞춘다.
            Rect labelRect = new Rect(
                headerRect.x + 6f,
                headerRect.y + CollapsedHeaderPadding,
                headerRect.width - 12f,
                lineH);
            DrawModifiersHeader(labelRect);
        }

        private void DrawModifiersHeader(Rect rect)
        {
            // foldout 화살표 자리만큼 라벨을 밀어 펼침/접힘 양쪽에서 위치가 같게 한다.
            Rect foldoutRect = new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height);
            _modifiers.isExpanded = EditorGUI.Foldout(foldoutRect, _modifiers.isExpanded,
                $"Modifiers ({_modifiers.arraySize})", toggleOnLabelClick: true);
        }

        private float GetModifierElementHeight(int index)
        {
            SerializedProperty modifier = _modifiers.GetArrayElementAtIndex(index);
            float lineH = EditorGUIUtility.singleLineHeight;

            // 접힘: foldout 헤더 한 줄만.
            if (!modifier.isExpanded)
            {
                return lineH + ElementVerticalPadding * 2;
            }

            float spacing = lineH + LineGap;
            SerializedProperty typeName = modifier.FindPropertyRelative(AttributeSetTypeNamePropertyName);

            if (string.IsNullOrEmpty(typeName.stringValue))
            {
                return spacing + lineH + ElementVerticalPadding * 2;
            }

            // foldout 헤더 + 고정 5행(Attribute Set / Attribute / Modifier Op / Magnitude 라벨 / Calc Type) + magnitude 값 영역(가변).
            return spacing + spacing * 5 + SectionGap * 2 + GetMagnitudeValueHeight(modifier) + ElementVerticalPadding * 2;
        }

        /// <summary>접힘 상태에서 요소를 식별할 수 있게 하는 한 줄 요약.</summary>
        private static string GetModifierSummary(SerializedProperty modifier)
        {
            string typeName = modifier.FindPropertyRelative(AttributeSetTypeNamePropertyName).stringValue;
            if (string.IsNullOrEmpty(typeName))
            {
                return "(Attribute Set 미지정)";
            }

            Type setType = Type.GetType(typeName);
            string setLabel = setType != null ? setType.Name : "(알 수 없는 Attribute Set)";

            string fieldName = modifier.FindPropertyRelative(FieldNamePropertyName).stringValue;
            string fieldLabel = string.IsNullOrEmpty(fieldName) ? "(Attribute 미지정)" : fieldName;

            var op = (GameplayModifierOperation)modifier.FindPropertyRelative(OperationPropertyName).enumValueIndex;
            string opLabel = _operationDisplayNames.TryGetValue(op, out string display) ? display : op.ToString();

            return $"{setLabel}.{fieldLabel}   —   {opLabel}";
        }

        // Calc Type별 magnitude 값 영역 높이. AttributeBased는 payload 드로어가 높이를 계산한다.
        private static float GetMagnitudeValueHeight(SerializedProperty modifier)
        {
            var calc = (MagnitudeCalculationType)modifier.FindPropertyRelative(MagnitudeCalculationTypePropertyName).enumValueIndex;
            return calc == MagnitudeCalculationType.AttributeBased
                ? EditorGUI.GetPropertyHeight(modifier.FindPropertyRelative(AttributeBasedPropertyName), true)
                : EditorGUIUtility.singleLineHeight;
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

            // 요소 헤더: foldout + 요약. 접혀 있으면 여기서 끝.
            Rect elementFoldoutRect = new Rect(rect.x + 10f, rect.y, rect.width - 10f, lineH);
            modifier.isExpanded = EditorGUI.Foldout(elementFoldoutRect, modifier.isExpanded,
                GetModifierSummary(modifier), toggleOnLabelClick: true);

            DrawSeparatorIfNeeded(rect, index);

            if (!modifier.isExpanded)
            {
                return;
            }

            EditorGUIUtility.labelWidth = ModifierLabelWidth;

            Type[] setTypes = AttributeReferenceGUI.GetSetTypes();
            string[] setDisplayNames = AttributeReferenceGUI.GetSetDisplayNames(setTypes);

            // Row 0: Attribute Set (popup index 0 = None, 1+ = 실제 타입)
            float row0Y = rect.y + spacing;
            int setPopupIndex = AttributeReferenceGUI.GetSetPopupIndex(setTypes, typeName.stringValue);
            int newSetPopupIndex = EditorGUI.Popup(new Rect(rect.x, row0Y, rect.width, lineH), "Attribute Set", setPopupIndex, setDisplayNames);

            if (newSetPopupIndex != setPopupIndex)
            {
                typeName.stringValue = newSetPopupIndex == 0
                    ? string.Empty
                    : setTypes[newSetPopupIndex - 1].AssemblyQualifiedName;
                fieldName.stringValue = string.Empty;
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;

            // None이면 이하 전체 무시
            if (newSetPopupIndex == 0)
            {
                return;
            }

            EditorGUIUtility.labelWidth = ModifierLabelWidth;

            // Row 1: Attribute Field (SectionGap으로 Attribute Set과 분리)
            int resolvedTypeIndex = newSetPopupIndex - 1;
            float row1Y = row0Y + spacing + SectionGap;
            Rect row1 = new Rect(rect.x, row1Y, rect.width, lineH);

            string[] fieldNames = AttributeReferenceGUI.GetFieldNames(setTypes[resolvedTypeIndex]);
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
                lineH, spacing, modifier, calcType, magnitude);

            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        // Magnitude 라벨 + Calc Type + 값 영역을 그린다. 값 영역은 Calc Type별 payload가 스스로 그린다
        // (ScalableFloat=magnitude float / AttributeBased=AttributeBasedMagnitudeDrawer에 위임).
        private static void DrawMagnitudeSection(Rect topLeft, float lineH, float spacing,
            SerializedProperty modifier, SerializedProperty calcType, SerializedProperty magnitude)
        {
            var calc = (MagnitudeCalculationType)calcType.enumValueIndex;
            SerializedProperty attributeBased = modifier.FindPropertyRelative(AttributeBasedPropertyName);
            float valueHeight = calc == MagnitudeCalculationType.AttributeBased
                ? EditorGUI.GetPropertyHeight(attributeBased, true)
                : lineH;

            const float boxPadX = 4f;
            const float boxPadY = 3f;
            // 박스: Magnitude 라벨 + Calc Type + 값 영역(가변).
            float boxHeight = spacing * 2 + valueHeight + boxPadY * 2;
            Rect boxRect = new Rect(topLeft.x - boxPadX, topLeft.y - boxPadY, topLeft.width + boxPadX * 2, boxHeight);
            EditorGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.12f));

            EditorGUI.LabelField(
                new Rect(topLeft.x, topLeft.y, topLeft.width, lineH),
                "Modifier Magnitude", EditorStyles.boldLabel);

            EditorGUI.PropertyField(
                new Rect(topLeft.x, topLeft.y + spacing, topLeft.width, lineH),
                calcType, new GUIContent("Calc Type"));

            Rect valueRect = new Rect(topLeft.x, topLeft.y + spacing * 2, topLeft.width, valueHeight);
            switch (calc)
            {
                case MagnitudeCalculationType.ScalableFloat:
                    EditorGUI.PropertyField(valueRect, magnitude, new GUIContent("Magnitude"));
                    break;

                case MagnitudeCalculationType.AttributeBased:
                    EditorGUI.PropertyField(valueRect, attributeBased, GUIContent.none, true);
                    break;
            }
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
    }
}
