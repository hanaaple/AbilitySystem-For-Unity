using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Effect;
using Core.Common.Editor;
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

        // 대상 어트리뷰트는 GameplayModifier.attribute(GameplayAttribute) 안에 있다.
        // Set/Attribute 팝업은 그 전용 드로어가 그리고, 여기선 요약·높이 계산을 위해 중첩 경로로 읽는다.
        private const string AttributePropertyName = "attribute";
        private const string AttributeSetTypeNamePropertyName = "attribute.attributeSetTypeName";
        private const string FieldNamePropertyName = "attribute.fieldName";
        private const string OperationPropertyName = "operation";
        private const string MagnitudeCalculationTypePropertyName = "magnitudeCalculationType";
        private const string MagnitudePropertyName = "magnitude";

        // AttributeBased payload는 자체 PropertyDrawer(AttributeBasedMagnitudeDrawer)가 그린다. 여기선 위임만.
        private const string AttributeBasedPropertyName = "attributeBased";

        // 새 modifier 추가 시 coefficient 기본값(1)을 세팅하기 위한 중첩 경로 조각(attributeBased.coefficient).
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
        private ReorderableList _executionList;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty(TypePropertyName);
            _duration = serializedObject.FindProperty(DurationPropertyName);
            _period = serializedObject.FindProperty(PeriodPropertyName);
            _executeOnApplication = serializedObject.FindProperty(ExecuteOnApplicationPropertyName);
            _modifiers = serializedObject.FindProperty(ModifiersPropertyName);
            _executions = serializedObject.FindProperty(ExecutionsPropertyName);

            BuildReorderableList();
            BuildExecutionList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDurationPolicy();
            EditorGUILayout.Space(6f);
            DrawModifiersList();

            EditorGUILayout.Space(6f);
            _executionList.DoLayoutList();

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

        private static void OnAddModifier(ReorderableList list)
        {
            int newIndex = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = newIndex;

            // 새로 추가한 요소는 바로 편집할 수 있게 펼친 상태로 시작한다(isExpanded 기본값은 false).
            SerializedProperty added = list.serializedProperty.GetArrayElementAtIndex(newIndex);
            added.isExpanded = true;

            // AttributeBased coefficient 기본값 1: AttributeBasedMagnitude의 `coefficient = 1f` 필드 이니셜라이저는
            // C# new 경로에서만 돌고, arraySize++ 직렬화 경로에선 실행되지 않아 float 기본값 0이 된다(class여도 동일).
            // 0이면 (value+Pre)*coef+Post 식이 통째로 0이 되므로 여기서 명시적으로 1을 넣는다.
            SerializedProperty coefficient = added.FindPropertyRelative(AttributeBasedPropertyName + "." + CoefficientPropertyName);
            if (coefficient != null)
            {
                coefficient.floatValue = 1f;
            }
        }

        // executionTypeNames는 AQN 문자열 리스트라, Modifiers와 달리 "타입 골라 추가"가 자연스럽다.
        // AttributeSet 리스트와 같은 검색형 Add(TypeChoiceList)를 쓴다. (New Script는 GameplayEffectExecution의
        // abstract Execute 때문에 빈 템플릿이 컴파일되지 않아 제외 — 필요하면 override 스텁 생성이 선행돼야 한다.)
        private void BuildExecutionList()
        {
            _executionList = TypeChoiceList.Create(
                serializedObject,
                _executions,
                "Executions",
                GetAddableExecutions,
                AddExecution,
                "추가 가능한 Execution 없음",
                drawElement: DrawExecutionElement,
                elementHeight: _ => EditorGUIUtility.singleLineHeight + 4f);
        }

        // 후보: GameplayEffectExecution 구체 서브클래스 중 이미 담긴 것은 제외(같은 Execution 중복 추가 방지).
        private IEnumerable<Type> GetAddableExecutions()
        {
            var used = new HashSet<string>();
            for (int i = 0; i < _executions.arraySize; i++)
            {
                string aqn = _executions.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(aqn))
                {
                    used.Add(aqn);
                }
            }

            return EditorTypeUtility.GetConcreteSubclasses(typeof(GameplayEffectExecution))
                .Where(type => !used.Contains(type.AssemblyQualifiedName));
        }

        // 요소 자체가 타입 이름(AQN)이라 새 요소에 바로 넣는다.
        private static void AddExecution(SerializedProperty element, Type type)
        {
            element.stringValue = type.AssemblyQualifiedName;
        }

        // 각 요소는 [SubclassSelector]가 붙은 문자열이라 그 전용 드로어(스크립트 필드 + 검색 드롭다운)로 그려진다.
        private void DrawExecutionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect line = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(line, _executions.GetArrayElementAtIndex(index), GUIContent.none);
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
            SerializedProperty attribute = modifier.FindPropertyRelative(AttributePropertyName);
            float attributeH = EditorGUI.GetPropertyHeight(attribute, true);

            // foldout 헤더 + Attribute(Set/Field, 가변).
            float height = spacing + attributeH;

            // Set 미지정이면 이하(Op·Magnitude) 숨김.
            if (string.IsNullOrEmpty(attribute.FindPropertyRelative("attributeSetTypeName").stringValue))
            {
                return height + ElementVerticalPadding * 2;
            }

            // Modifier Op + Magnitude(라벨 + Calc Type + 값 영역, 가변). SectionGap은 Op↔Magnitude 분리 + 하단 박스 여백.
            height += LineGap + lineH                                   // Modifier Op
                    + spacing + SectionGap + spacing * 2 + GetMagnitudeValueHeight(modifier)  // Magnitude 섹션
                    + SectionGap;                                       // 박스 하단 여백 버퍼
            return height + ElementVerticalPadding * 2;
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
            SerializedProperty modifier  = _modifiers.GetArrayElementAtIndex(index);
            SerializedProperty attribute = modifier.FindPropertyRelative(AttributePropertyName);
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

            // Attribute Set + Attribute — GameplayAttribute 전용 드로어가 2행으로 그린다.
            float attributeH = EditorGUI.GetPropertyHeight(attribute, true);
            float attrY = rect.y + spacing;
            EditorGUI.PropertyField(new Rect(rect.x, attrY, rect.width, attributeH), attribute, GUIContent.none, true);

            // Set 미지정이면 이하(Op·Magnitude) 전체 무시.
            if (string.IsNullOrEmpty(attribute.FindPropertyRelative("attributeSetTypeName").stringValue))
            {
                EditorGUIUtility.labelWidth = prevLabelWidth;
                return;
            }

            // Modifier Op
            float opY = attrY + attributeH + LineGap;
            GameplayModifierOperation currentOp = (GameplayModifierOperation)operation.enumValueIndex;
            int opPopupIndex = Array.IndexOf(_operationValues, currentOp);
            int newOpPopupIndex = EditorGUI.Popup(
                new Rect(rect.x, opY, rect.width, lineH),
                "Modifier Op", opPopupIndex, _operationPopupOptions);
            operation.enumValueIndex = (int)_operationValues[newOpPopupIndex];

            DrawMagnitudeSection(
                new Rect(rect.x, opY + spacing + SectionGap, rect.width, lineH),
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
