using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.Common.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>
    /// GE 에셋 인스펙터. Modifiers·Executions·Calculation Modifiers 세 목록은 같은 틀로 그린다 —
    /// 요소 = 위아래 여백 + 머리 줄(접기 + "대상 — 부가 정보" 요약) + 본문(라벨 열 고정, 값 열은 모든 목록에서 같은 x) + 형제 사이 1px 구분선.
    /// Modifier와 Calculation Modifier 본문은 같은 순서: 대상 → Modifier Op → Modifier Magnitude → Source/Target Tags.
    /// 높이와 그리기는 같은 Layout 함수(draw 플래그)로 계산해 어긋나지 않게 한다.
    /// </summary>
    [CustomEditor(typeof(GameplayEffectAsset))]
    public sealed class GameplayEffectAssetDrawer : UnityEditor.Editor
    {
        private const string TypePropertyName = "type";
        private const string DurationPropertyName = "duration";
        private const string PeriodPropertyName = "period";
        private const string ExecuteOnApplicationPropertyName = "executePeriodicEffectOnApplication";
        private const string ModifiersPropertyName = "modifiers";
        private const string ExecutionsPropertyName = "executions";
        private const string ComponentsPropertyName = "gameplayEffectComponents";
        private const string ComponentClassSuffix = "GameplayEffectComponent";

        // GameplayModifier 필드. 대상 어트리뷰트(GameplayAttribute)의 Set/Attribute 팝업은 그 전용 드로어가 그리고, 여기선 요약을 위해 중첩 경로로 읽는다.
        private const string AttributePropertyName = "attribute";
        private const string AttributeSetTypeNamePropertyName = "attribute.attributeSetTypeName";
        private const string FieldNamePropertyName = "attribute.fieldName";
        private const string OperationPropertyName = "operation";

        // Execution 정의(GameplayEffectExecutionDefinition)와 그 Calculation Modifier(GameplayEffectExecutionScopedModifierInfo) 필드.
        private const string CalculationClassPropertyName = "calculationClass";
        private const string CalculationModifiersPropertyName = "calculationModifiers";
        private const string CapturedAttributePropertyName = "capturedAttribute";
        private const string ModifierOpPropertyName = "modifierOp";

        // Modifier와 Calculation Modifier가 같은 이름으로 가진 필드. Magnitude·태그 조건은 각자의 전용 드로어가 그린다.
        private const string ModifierMagnitudePropertyName = "modifierMagnitude";
        private const string AttributeBasedMagnitudePropertyName = "modifierMagnitude.attributeBasedMagnitude";
        private const string CoefficientPropertyName = "modifierMagnitude.attributeBasedMagnitude.coefficient";
        private const string SourceTagsPropertyName = "sourceTags";
        private const string TargetTagsPropertyName = "targetTags";

        // ── 공통 규격 ──
        private const float LineGap = 4f;                 // 본문 줄 사이
        private const float SectionGap = 8f;              // 묶음(대상 / Magnitude / Tags) 사이
        private const float ElementVerticalPadding = 6f;  // 요소 위아래 여백
        private const float LabelWidth = 140f;            // 라벨 열. 안쪽 목록도 값 열이 같은 x에 오도록 이 폭에서 들여쓴 만큼 뺀다
        private const float HeaderIndent = 10f;           // 요소 머리 줄 접기 화살표가 드래그 핸들과 겹치지 않게
        private const float FoldoutIndent = 10f;          // 본문 안 접기(Magnitude·Tags·Calculation Modifiers)의 화살표 자리

        // ReorderableList가 머리 줄 배경 안에 글자를 두는 여백. 접힌 머리 줄도 같은 자리에 글자를 둔다
        private const float ListHeaderInsetX = 6f;
        private const float ListHeaderInsetY = 1f;

        // 안쪽 목록(Calculation Modifiers) 상자
        private const float NestedPadX = 14f;  // 안쪽 칸 접기 화살표가 상자 왼쪽 안내선과 겹치지 않게
        private const float NestedPadY = 4f;
        private const float NestedSeparatorGap = 8f;
        private const float RemoveButtonWidth = 22f;
        private const float AddButtonWidth = 150f;

        private static readonly Color SeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f);
        private static readonly Color NestedFillColor = new(0f, 0f, 0f, 0.06f);
        // 반투명 회색 하나로 밝은·어두운 스킨 모두에서 경계가 보이게 한다
        private static readonly Color NestedOutlineColor = new(0.5f, 0.5f, 0.5f, 0.8f);

        // Execution 클래스별 캡처 정의(Defs) 캐시. 타입이 키라 목록 순서가 바뀌어도 안전하다.
        private static readonly Dictionary<Type, GameplayEffectAttributeCaptureDefinition[]> _captureDefinitionsByExecution = new();

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
        private SerializedProperty _components;

        private ReorderableList _modifierList;
        private ReorderableList _executionList;
        private ReorderableList _componentList;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty(TypePropertyName);
            _duration = serializedObject.FindProperty(DurationPropertyName);
            _period = serializedObject.FindProperty(PeriodPropertyName);
            _executeOnApplication = serializedObject.FindProperty(ExecuteOnApplicationPropertyName);
            _modifiers = serializedObject.FindProperty(ModifiersPropertyName);
            _executions = serializedObject.FindProperty(ExecutionsPropertyName);
            _components = serializedObject.FindProperty(ComponentsPropertyName);

            BuildModifierList();
            BuildExecutionList();
            BuildComponentList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDurationPolicy();
            EditorGUILayout.Space(6f);
            DrawCollapsibleList(_componentList, _components, "Components");
            EditorGUILayout.Space(6f);
            DrawCollapsibleList(_modifierList, _modifiers, "Modifiers");
            EditorGUILayout.Space(6f);
            DrawCollapsibleList(_executionList, _executions, "Executions");

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
                // 라벨만 단위·짧은 이름으로 바꾸고 툴팁은 필드 [Tooltip]을 쓴다
                EditorGUILayout.PropertyField(_period, new GUIContent("Period (s)", _period.tooltip));

                if (_period.floatValue > Mathf.Epsilon)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_executeOnApplication, new GUIContent("Execute on Application", _executeOnApplication.tooltip));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        // ── 목록 공통 ────────────────────────────────────────────────────────────

        /// <summary>접힘 상태면 ReorderableList 대신 헤더만 그린다(ReorderableList엔 내장 foldout이 없다). 접힘 상태는 SerializedProperty.isExpanded에 저장돼 선택을 옮겨도 유지된다.</summary>
        private static void DrawCollapsibleList(ReorderableList list, SerializedProperty elements, string title)
        {
            if (elements.isExpanded)
            {
                list.DoLayoutList();
                return;
            }

            // 펼친 머리 줄과 같은 높이 — 배경 스타일이 높이를 고정해 더 크게 잡으면 글자가 배경 아래로 삐져나온다
            Rect headerRect = GUILayoutUtility.GetRect(0f, list.headerHeight, GUILayout.ExpandWidth(true));

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

            Rect labelRect = new Rect(headerRect.x + ListHeaderInsetX, headerRect.y + ListHeaderInsetY, headerRect.width - ListHeaderInsetX * 2, headerRect.height - ListHeaderInsetY * 2);
            DrawListHeader(labelRect, elements, title);
        }

        // foldout 화살표 자리만큼 라벨을 밀어 펼침/접힘 양쪽에서 위치가 같게 한다.
        private static void DrawListHeader(Rect rect, SerializedProperty elements, string title)
        {
            Rect foldoutRect = new Rect(rect.x + HeaderIndent, rect.y, rect.width - HeaderIndent, rect.height);
            elements.isExpanded = EditorGUI.Foldout(foldoutRect, elements.isExpanded, $"{title} ({elements.arraySize})", toggleOnLabelClick: true);
        }

        // 요소 머리 줄: 접기 + 요약. 펼침 상태는 요소 프로퍼티의 isExpanded.
        private static void DrawElementHeader(Rect rect, SerializedProperty element, string summary)
        {
            element.isExpanded = EditorGUI.Foldout(rect, element.isExpanded, summary, toggleOnLabelClick: true);
        }

        private static void DrawSeparator(float x, float y, float width)
        {
            EditorGUI.DrawRect(new Rect(x, y, width, 1f), SeparatorColor);
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        // ── Modifiers ───────────────────────────────────────────────────────────

        private void BuildModifierList()
        {
            _modifierList = new ReorderableList(serializedObject, _modifiers, draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true)
            {
                drawHeaderCallback = rect => DrawListHeader(rect, _modifiers, "Modifiers"),
                drawElementCallback = DrawModifierElement,
                elementHeightCallback = index => LayoutModifierElement(new Rect(0f, 0f, 0f, 0f), _modifiers.GetArrayElementAtIndex(index), false),
                onAddCallback = OnAddModifier,
            };
        }

        private static void OnAddModifier(ReorderableList list)
        {
            int newIndex = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = newIndex;

            SerializedProperty added = list.serializedProperty.GetArrayElementAtIndex(newIndex);
            added.isExpanded = true;
            ExpandMagnitudeAndSetDefaultCoefficient(added);
        }

        private void DrawModifierElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty modifier = _modifiers.GetArrayElementAtIndex(index);
            float height = LayoutModifierElement(rect, modifier, true);

            if (index < _modifiers.arraySize - 1)
            {
                DrawSeparator(rect.x, rect.y + height - 1f, rect.width);
            }
        }

        /// <returns>요소 높이. <paramref name="draw"/>가 false면 그리지 않고 높이만 잰다.</returns>
        private static float LayoutModifierElement(Rect rect, SerializedProperty modifier, bool draw)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float y = rect.y + ElementVerticalPadding;

            if (draw)
            {
                DrawElementHeader(new Rect(rect.x + HeaderIndent, y, rect.width - HeaderIndent, lineH), modifier, GetModifierSummary(modifier));
            }

            y += lineH;
            if (!modifier.isExpanded)
            {
                return y + ElementVerticalPadding - rect.y;
            }

            // 대상: Attribute Set + Attribute — GameplayAttribute 전용 드로어가 2행으로 그린다.
            y += LineGap;
            SerializedProperty attribute = modifier.FindPropertyRelative(AttributePropertyName);
            float attributeH = EditorGUI.GetPropertyHeight(attribute, true);
            if (draw)
            {
                WithLabelWidth(LabelWidth, () => EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, attributeH), attribute, GUIContent.none, true));
            }

            y += attributeH;

            // Set 미지정이면 이하(Op·Magnitude·Tags)는 의미가 없어 숨긴다.
            if (string.IsNullOrEmpty(modifier.FindPropertyRelative(AttributeSetTypeNamePropertyName).stringValue))
            {
                return y + ElementVerticalPadding - rect.y;
            }

            y += LineGap;
            y = LayoutModifierBody(rect.x, y, rect.width, modifier, OperationPropertyName, draw);
            return y + ElementVerticalPadding - rect.y;
        }

        // ── Executions ──────────────────────────────────────────────────────────

        // Execution은 계산 클래스를 골라 추가하는 게 자연스러워 AttributeSet 리스트와 같은 검색형 Add(TypeChoiceList)를 쓴다.
        // (New Script는 GameplayEffectExecution의 abstract Execute 때문에 빈 템플릿이 컴파일되지 않아 제외 — 필요하면 override 스텁 생성이 선행돼야 한다.)
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
                elementHeight: index => LayoutExecutionElement(new Rect(0f, 0f, 0f, 0f), _executions.GetArrayElementAtIndex(index), false));

            // Modifiers와 같은 머리 줄(접기 + 개수)
            _executionList.drawHeaderCallback = rect => DrawListHeader(rect, _executions, "Executions");
        }

        // 후보: GameplayEffectExecution 구체 서브클래스 중 이미 담긴 것은 제외(같은 Execution 중복 추가 방지).
        private IEnumerable<Type> GetAddableExecutions()
        {
            var used = new HashSet<string>();
            for (int i = 0; i < _executions.arraySize; i++)
            {
                SerializedProperty executionDefinition = _executions.GetArrayElementAtIndex(i);
                string aqn = executionDefinition.FindPropertyRelative(CalculationClassPropertyName).stringValue;
                if (!string.IsNullOrEmpty(aqn))
                {
                    used.Add(aqn);
                }
            }

            return EditorTypeUtility.GetConcreteSubclasses(typeof(GameplayEffectExecution))
                .Where(type => !used.Contains(type.AssemblyQualifiedName));
        }

        // 삽입된 새 요소는 앞 요소를 복사한 상태라 빈 정의로 덮은 뒤 계산 클래스만 넣는다.
        private static void AddExecution(SerializedProperty element, Type type)
        {
            element.boxedValue = new GameplayEffectExecutionDefinition();
            element.FindPropertyRelative(CalculationClassPropertyName).stringValue = type.AssemblyQualifiedName;
            element.isExpanded = true;
            SerializedProperty calculationModifiers = element.FindPropertyRelative(CalculationModifiersPropertyName);
            calculationModifiers.isExpanded = true;
        }

        private void DrawExecutionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty executionDefinition = _executions.GetArrayElementAtIndex(index);
            float height = LayoutExecutionElement(rect, executionDefinition, true);

            if (index < _executions.arraySize - 1)
            {
                DrawSeparator(rect.x, rect.y + height - 1f, rect.width);
            }
        }

        /// <returns>요소 높이. <paramref name="draw"/>가 false면 그리지 않고 높이만 잰다.</returns>
        private static float LayoutExecutionElement(Rect rect, SerializedProperty executionDefinition, bool draw)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            SerializedProperty calculationClass = executionDefinition.FindPropertyRelative(CalculationClassPropertyName);
            SerializedProperty calculationModifiers = executionDefinition.FindPropertyRelative(CalculationModifiersPropertyName);
            float y = rect.y + ElementVerticalPadding;

            if (draw)
            {
                DrawElementHeader(new Rect(rect.x + HeaderIndent, y, rect.width - HeaderIndent, lineH), executionDefinition, GetExecutionSummary(calculationClass, calculationModifiers));
            }

            y += lineH;
            if (!executionDefinition.isExpanded)
            {
                return y + ElementVerticalPadding - rect.y;
            }

            // 대상: 계산 클래스(스크립트 필드 + 검색 드롭다운)
            y += LineGap;
            if (draw)
            {
                WithLabelWidth(LabelWidth, () => EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), calculationClass, new GUIContent("Calculation Class")));
            }

            y += lineH + SectionGap;
            y = LayoutCalculationModifiers(rect, y, calculationClass, calculationModifiers, draw);
            return y + SectionGap + ElementVerticalPadding - rect.y;
        }

        // Calculation Modifiers: 본문 접기 줄 + 옅은 상자(왼쪽 안내선) 안의 안쪽 목록 + 상자 끝 추가 버튼.
        private static float LayoutCalculationModifiers(Rect rect, float y, SerializedProperty calculationClass, SerializedProperty calculationModifiers, bool draw)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            GameplayEffectAttributeCaptureDefinition[] captureDefinitions = GetCaptureDefinitions(calculationClass.stringValue);

            if (draw)
            {
                Rect foldoutRect = new Rect(rect.x + FoldoutIndent, y, rect.width - FoldoutIndent, lineH);
                calculationModifiers.isExpanded = EditorGUI.Foldout(foldoutRect, calculationModifiers.isExpanded,
                    $"Calculation Modifiers ({calculationModifiers.arraySize})", toggleOnLabelClick: true);
            }

            y += lineH;
            if (!calculationModifiers.isExpanded)
            {
                return y;
            }

            // 상자는 접기 화살표 아래에서 시작한다. 안쪽 칸은 상자 안 여백만큼 들여쓰고, 라벨 폭을 그만큼 줄여 값 열을 바깥 줄과 맞춘다.
            Rect box = new Rect(rect.x + FoldoutIndent, y + LineGap, rect.width - FoldoutIndent, 0f);
            float innerX = box.x + NestedPadX;
            float innerWidth = box.width - NestedPadX - NestedPadX / 2f;
            float innerLabelWidth = LabelWidth - (innerX - rect.x);

            if (draw)
            {
                // 칠이 안쪽 칸 아래에 깔리도록 높이를 먼저 재서 상자부터 그린다
                box.height = LayoutNestedBox(innerX, box.y, innerWidth, innerLabelWidth, calculationModifiers, captureDefinitions, false);
                EditorGUI.DrawRect(box, NestedFillColor);
                DrawOutline(box, NestedOutlineColor);
            }

            float boxHeight = LayoutNestedBox(innerX, box.y, innerWidth, innerLabelWidth, calculationModifiers, captureDefinitions, draw);
            return box.y + boxHeight;
        }

        /// <returns>상자 높이(안 여백 포함).</returns>
        private static float LayoutNestedBox(float x, float top, float width, float labelWidth, SerializedProperty calculationModifiers, GameplayEffectAttributeCaptureDefinition[] captureDefinitions, bool draw)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float y = top + NestedPadY;

            if (captureDefinitions.Length == 0 && calculationModifiers.arraySize == 0)
            {
                if (draw)
                {
                    EditorGUI.LabelField(new Rect(x, y, width, lineH), "이 Execution은 캡처 정의(Defs)가 없어 걸 대상이 없다", EditorStyles.miniLabel);
                }

                return y + lineH + NestedPadY - top;
            }

            for (int i = 0; i < calculationModifiers.arraySize; i++)
            {
                if (i > 0)
                {
                    if (draw)
                    {
                        DrawSeparator(x, y + NestedSeparatorGap / 2f, width);
                    }

                    y += NestedSeparatorGap;
                }

                SerializedProperty scopedModifier = calculationModifiers.GetArrayElementAtIndex(i);
                y += LayoutScopedModifier(x, y, width, labelWidth, scopedModifier, captureDefinitions, draw, out bool removeClicked);

                if (removeClicked)
                {
                    // 지운 뒤 남은 칸을 이번 프레임에 그리면 인덱스가 어긋나니 반영하고 GUI를 끝낸다(ExitGUI는 OnInspectorGUI 끝의 Apply를 건너뛴다)
                    calculationModifiers.DeleteArrayElementAtIndex(i);
                    calculationModifiers.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }

            // 추가 버튼은 칸 사이 간격만큼 띄워 마지막 칸의 일부로 보이지 않게 한다
            if (calculationModifiers.arraySize > 0)
            {
                y += NestedSeparatorGap;
            }

            if (draw)
            {
                using (new EditorGUI.DisabledScope(captureDefinitions.Length == 0))
                {
                    if (GUI.Button(new Rect(x, y, AddButtonWidth, lineH), "+ Calculation Modifier", EditorStyles.miniButton))
                    {
                        AddScopedModifier(calculationModifiers, captureDefinitions);
                    }
                }
            }

            y += lineH + NestedPadY;
            return y - top;
        }

        // 새 Calculation Modifier는 빈 값에서 시작하고, 고를 캡처가 있으면 첫 캡처를 대상으로 둔다.
        private static void AddScopedModifier(SerializedProperty calculationModifiers, GameplayEffectAttributeCaptureDefinition[] captureDefinitions)
        {
            int newIndex = calculationModifiers.arraySize;
            calculationModifiers.arraySize++;
            SerializedProperty added = calculationModifiers.GetArrayElementAtIndex(newIndex);
            added.boxedValue = new GameplayEffectExecutionScopedModifierInfo();
            added.isExpanded = true;
            ExpandMagnitudeAndSetDefaultCoefficient(added);

            if (captureDefinitions.Length > 0)
            {
                added.FindPropertyRelative(CapturedAttributePropertyName).boxedValue = captureDefinitions[0];
            }
        }

        /// <returns>칸 높이. 머리 줄 오른쪽 − 를 누르면 <paramref name="removeClicked"/>.</returns>
        private static float LayoutScopedModifier(float x, float top, float width, float labelWidth, SerializedProperty scopedModifier, GameplayEffectAttributeCaptureDefinition[] captureDefinitions, bool draw, out bool removeClicked)
        {
            removeClicked = false;
            float lineH = EditorGUIUtility.singleLineHeight;
            SerializedProperty capturedAttribute = scopedModifier.FindPropertyRelative(CapturedAttributePropertyName);
            float y = top;

            if (draw)
            {
                DrawElementHeader(new Rect(x, y, width - RemoveButtonWidth, lineH), scopedModifier, GetScopedModifierSummary(scopedModifier));
                removeClicked = GUI.Button(new Rect(x + width - RemoveButtonWidth, y, RemoveButtonWidth, lineH), "−", EditorStyles.miniButton);
            }

            y += lineH;
            if (!scopedModifier.isExpanded)
            {
                return y - top;
            }

            // 대상: Backing Data — 이 Execution 클래스가 Defs()로 선언한 캡처 중에서만 고른다(UE와 같음).
            y += LineGap;
            if (draw)
            {
                WithLabelWidth(labelWidth, () => DrawBackingDataPopup(new Rect(x, y, width, lineH), capturedAttribute, captureDefinitions));
            }

            y += lineH + LineGap;
            y = LayoutModifierBody(x, y, width, scopedModifier, ModifierOpPropertyName, draw, labelWidth);
            return y - top;
        }

        private static void DrawBackingDataPopup(Rect rect, SerializedProperty capturedAttribute, GameplayEffectAttributeCaptureDefinition[] captureDefinitions)
        {
            if (captureDefinitions.Length == 0)
            {
                EditorGUI.LabelField(rect, "Backing Data", "Execution 클래스에 캡처 정의(Defs)가 없다");
                return;
            }

            var current = (GameplayEffectAttributeCaptureDefinition)capturedAttribute.boxedValue;
            int currentIndex = Array.IndexOf(captureDefinitions, current);
            string[] options = captureDefinitions.Select(GetCaptureLabel).ToArray();
            if (currentIndex < 0)
            {
                // 선언에서 빠진 캡처를 들고 있으면 그 이름을 맨 앞에 보여 줘 무엇이 어긋났는지 드러낸다
                options = options.Prepend($"(선언에 없음) {GetCaptureLabel(current)}").ToArray();
            }

            int shownIndex = currentIndex < 0 ? 0 : currentIndex;
            int newShownIndex = EditorGUI.Popup(rect, "Backing Data", shownIndex, options);
            int newIndex = currentIndex < 0 ? newShownIndex - 1 : newShownIndex;
            if (newShownIndex != shownIndex && newIndex >= 0)
            {
                capturedAttribute.boxedValue = captureDefinitions[newIndex];
            }
        }

        // Execution 클래스를 한 번 만들어 Defs()를 읽는다(UE는 클래스 CDO에서 읽는다). 클래스가 없거나 못 만들면 빈 목록.
        private static GameplayEffectAttributeCaptureDefinition[] GetCaptureDefinitions(string calculationClassName)
        {
            Type type = string.IsNullOrEmpty(calculationClassName) ? null : Type.GetType(calculationClassName);
            if (type == null || type.IsAbstract || !typeof(GameplayEffectExecution).IsAssignableFrom(type))
            {
                return Array.Empty<GameplayEffectAttributeCaptureDefinition>();
            }

            if (!_captureDefinitionsByExecution.TryGetValue(type, out GameplayEffectAttributeCaptureDefinition[] definitions))
            {
                var execution = (GameplayEffectExecution)Activator.CreateInstance(type);
                definitions = execution.Defs().ToArray();
                _captureDefinitionsByExecution[type] = definitions;
            }

            return definitions;
        }

        // ── Components ──────────────────────────────────────────────────────────

        // Component는 [SerializeReference]라 타입을 골라 인스턴스를 넣는다(ItemData 모듈 목록과 같은 방식).
        private void BuildComponentList()
        {
            _componentList = TypeChoiceList.Create(
                serializedObject,
                _components,
                "Components",
                GetAddableComponents,
                AddComponent,
                "추가 가능한 Component 없음",
                drawElement: DrawComponentElement,
                elementHeight: index => LayoutComponentElement(new Rect(0f, 0f, 0f, 0f), _components.GetArrayElementAtIndex(index), false));

            _componentList.drawHeaderCallback = rect => DrawListHeader(rect, _components, "Components");
        }

        // 같은 종류는 하나만 — GE가 Component를 종류로 찾기 때문(에이전트 판단, UE는 FindComponent가 첫 번째만 돌려준다).
        private IEnumerable<Type> GetAddableComponents()
        {
            HashSet<Type> used = CollectComponentTypes(-1);
            return EditorTypeUtility.GetConcreteSubclasses(typeof(GameplayEffectComponent))
                .Where(type => !used.Contains(type));
        }

        // 목록에 이미 있는 Component 타입. exceptIndex 요소는 뺀다(그 요소 자신의 타입 바꾸기 후보에선 자기 타입을 남긴다).
        private HashSet<Type> CollectComponentTypes(int exceptIndex)
        {
            var used = new HashSet<Type>();
            for (int i = 0; i < _components.arraySize; i++)
            {
                object component = _components.GetArrayElementAtIndex(i).managedReferenceValue;
                if (i != exceptIndex && component != null)
                {
                    used.Add(component.GetType());
                }
            }

            return used;
        }

        // 삽입된 새 요소는 앞 요소와 같은 참조를 가리킬 수 있어 새 인스턴스로 바꾼다.
        private static void AddComponent(SerializedProperty element, Type type)
        {
            element.managedReferenceValue = Activator.CreateInstance(type);
            element.isExpanded = true;
        }

        private void DrawComponentElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty component = _components.GetArrayElementAtIndex(index);
            float height = LayoutComponentElement(rect, component, true, CollectComponentTypes(index));

            if (index < _components.arraySize - 1)
            {
                DrawSeparator(rect.x, rect.y + height - 1f, rect.width);
            }
        }

        /// <returns>요소 높이. <paramref name="draw"/>가 false면 그리지 않고 높이만 잰다.</returns>
        /// <param name="usedByOthers">다른 요소가 이미 쓰는 타입 — Component Class 드롭다운 후보에서 뺀다. 높이만 잴 때는 null.</param>
        private static float LayoutComponentElement(Rect rect, SerializedProperty component, bool draw, IReadOnlyCollection<Type> usedByOthers = null)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float y = rect.y + ElementVerticalPadding;

            if (draw)
            {
                DrawElementHeader(new Rect(rect.x + HeaderIndent, y, rect.width - HeaderIndent, lineH), component, GetComponentSummary(component));
            }

            y += lineH;
            if (!component.isExpanded)
            {
                return y + ElementVerticalPadding - rect.y;
            }

            // 대상: Component 종류(스크립트 필드 + 드롭다운으로 바꾸기) — Execution의 Calculation Class 줄과 같은 모양
            y += LineGap;
            if (draw)
            {
                float classY = y;
                WithLabelWidth(LabelWidth, () => SubclassSelectorDrawer.DrawManagedReferenceSelector(
                    new Rect(rect.x, classY, rect.width, lineH), component, typeof(GameplayEffectComponent), usedByOthers, new GUIContent("Component Class")));
            }

            y += lineH;
            if (component.managedReferenceValue == null)
            {
                return y + ElementVerticalPadding - rect.y;
            }

            // 본문은 Component 종류별 드로어(GameplayEffectComponentDrawer 상속)가 그린다.
            float bodyHeight = EditorGUI.GetPropertyHeight(component, GUIContent.none, true);
            if (draw)
            {
                float bodyY = y;
                WithLabelWidth(LabelWidth, () => EditorGUI.PropertyField(new Rect(rect.x, bodyY, rect.width, bodyHeight), component, GUIContent.none, true));
            }

            return y + bodyHeight + SectionGap + ElementVerticalPadding - rect.y;
        }

        // 예: "Asset Tags", "Target Tag Requirements". 타입을 못 찾으면(이름 변경·삭제) 그 사실을 표시한다.
        private static string GetComponentSummary(SerializedProperty component)
        {
            object instance = component.managedReferenceValue;
            if (instance == null)
            {
                return string.IsNullOrEmpty(component.managedReferenceFullTypename) ? "(빈 Component)" : "(타입을 찾을 수 없는 Component)";
            }

            string typeName = instance.GetType().Name;
            if (typeName.EndsWith(ComponentClassSuffix) && typeName.Length > ComponentClassSuffix.Length)
            {
                typeName = typeName.Substring(0, typeName.Length - ComponentClassSuffix.Length);
            }

            return ObjectNames.NicifyVariableName(typeName);
        }

        // ── Modifier·Calculation Modifier 공통 본문 ─────────────────────────────

        /// <summary>대상 줄 다음부터: Modifier Op → Modifier Magnitude → Source/Target Tags. 두 목록이 같은 순서·간격으로 그린다.</summary>
        /// <param name="labelWidth">라벨 폭. 안쪽 목록은 들여쓴 만큼 줄여 값 열(x + 라벨 폭)이 바깥 목록과 같은 x에 오게 한다.</param>
        /// <returns>본문 끝 y(마지막 묶음 아래 여백 포함).</returns>
        private static float LayoutModifierBody(float x, float y, float width, SerializedProperty element, string operationPropertyName, bool draw, float labelWidth = LabelWidth)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            SerializedProperty operation = element.FindPropertyRelative(operationPropertyName);
            SerializedProperty modifierMagnitude = element.FindPropertyRelative(ModifierMagnitudePropertyName);

            if (draw)
            {
                WithLabelWidth(labelWidth, () =>
                {
                    var currentOp = (GameplayModifierOperation)operation.enumValueIndex;
                    int opPopupIndex = Array.IndexOf(_operationValues, currentOp);
                    int newOpPopupIndex = EditorGUI.Popup(new Rect(x, y, width, lineH), "Modifier Op", opPopupIndex, _operationPopupOptions);
                    operation.enumValueIndex = (int)_operationValues[newOpPopupIndex];
                });
            }

            y += lineH + LineGap + SectionGap;

            float magnitudeHeight = EditorGUI.GetPropertyHeight(modifierMagnitude, true);
            if (draw)
            {
                float magnitudeY = y;
                WithLabelWidth(labelWidth, () => EditorGUI.PropertyField(new Rect(x, magnitudeY, width, magnitudeHeight), modifierMagnitude, true));
            }

            y += magnitudeHeight + SectionGap;

            float tagsHeight = GetTagRequirementsHeight(element);
            if (draw)
            {
                float tagsY = y;
                WithLabelWidth(labelWidth, () => DrawTagRequirements(x, tagsY, width, element));
            }

            return y + tagsHeight + SectionGap;
        }

        // 높이와 그리기가 같은 두 프로퍼티 높이를 써야 다음 요소와 겹치지 않는다.
        private static float GetTagRequirementsHeight(SerializedProperty element)
        {
            float sourceHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative(SourceTagsPropertyName), true);
            float targetHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative(TargetTagsPropertyName), true);
            return sourceHeight + LineGap + targetHeight;
        }

        // 본문 안 접기처럼 화살표 자리만큼 밀고, 값 열이 위 행들과 맞도록 라벨 폭을 그만큼 줄인다.
        private static void DrawTagRequirements(float x, float y, float width, SerializedProperty element)
        {
            SerializedProperty sourceTags = element.FindPropertyRelative(SourceTagsPropertyName);
            SerializedProperty targetTags = element.FindPropertyRelative(TargetTagsPropertyName);

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = prevLabelWidth - FoldoutIndent;

            float sourceHeight = EditorGUI.GetPropertyHeight(sourceTags, true);
            EditorGUI.PropertyField(new Rect(x + FoldoutIndent, y, width - FoldoutIndent, sourceHeight), sourceTags, true);

            float targetY = y + sourceHeight + LineGap;
            float targetHeight = EditorGUI.GetPropertyHeight(targetTags, true);
            EditorGUI.PropertyField(new Rect(x + FoldoutIndent, targetY, width - FoldoutIndent, targetHeight), targetTags, true);

            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        // 새 요소(GE Modifier·Calculation Modifier)는 바로 편집할 수 있게 Magnitude·AttributeBased 묶음을 펼친다(isExpanded 기본값은 false).
        // AttributeBased coefficient 기본값 1: AttributeBasedMagnitude의 `coefficient = 1f` 필드 이니셜라이저는
        // C# new 경로에서만 돌고, 직렬화 경로(arraySize++·boxedValue)에선 실행되지 않아 float 기본값 0이 된다.
        // 0이면 (value+Pre)*coef+Post 식이 통째로 0이 되므로 여기서 명시적으로 1을 넣는다.
        private static void ExpandMagnitudeAndSetDefaultCoefficient(SerializedProperty element)
        {
            SerializedProperty modifierMagnitude = element.FindPropertyRelative(ModifierMagnitudePropertyName);
            modifierMagnitude.isExpanded = true;
            SerializedProperty attributeBased = element.FindPropertyRelative(AttributeBasedMagnitudePropertyName);
            attributeBased.isExpanded = true;

            SerializedProperty coefficient = element.FindPropertyRelative(CoefficientPropertyName);
            if (coefficient != null)
            {
                coefficient.floatValue = 1f;
            }
        }

        private static void WithLabelWidth(float labelWidth, Action draw)
        {
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            draw();
            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        // ── 요약 문구 — 세 목록이 같은 "대상   —   부가 정보" 꼴 ─────────────────

        private const string SummarySeparator = "   —   ";

        private static string GetModifierSummary(SerializedProperty modifier)
        {
            string attributeLabel = GetAttributeLabel(
                modifier.FindPropertyRelative(AttributeSetTypeNamePropertyName).stringValue,
                modifier.FindPropertyRelative(FieldNamePropertyName).stringValue);
            var op = (GameplayModifierOperation)modifier.FindPropertyRelative(OperationPropertyName).enumValueIndex;
            return attributeLabel + SummarySeparator + GetOperationLabel(op);
        }

        private static string GetScopedModifierSummary(SerializedProperty scopedModifier)
        {
            var captured = (GameplayEffectAttributeCaptureDefinition)scopedModifier.FindPropertyRelative(CapturedAttributePropertyName).boxedValue;
            var op = (GameplayModifierOperation)scopedModifier.FindPropertyRelative(ModifierOpPropertyName).enumValueIndex;
            return GetCaptureLabel(captured) + SummarySeparator + GetOperationLabel(op);
        }

        private static string GetExecutionSummary(SerializedProperty calculationClass, SerializedProperty calculationModifiers)
        {
            Type type = string.IsNullOrEmpty(calculationClass.stringValue) ? null : Type.GetType(calculationClass.stringValue);
            string classLabel = type != null ? ObjectNames.NicifyVariableName(type.Name) : "(Calculation Class 미지정)";
            return $"{classLabel}{SummarySeparator}Calculation Modifiers {calculationModifiers.arraySize}";
        }

        // 예: "CharacterAttributeSet.health". 비었거나 못 찾으면 그 사실을 표시한다.
        private static string GetAttributeLabel(string attributeSetTypeName, string fieldName)
        {
            if (string.IsNullOrEmpty(attributeSetTypeName))
            {
                return "(Attribute Set 미지정)";
            }

            Type setType = Type.GetType(attributeSetTypeName);
            string setLabel = setType != null ? setType.Name : "(알 수 없는 Attribute Set)";
            string fieldLabel = string.IsNullOrEmpty(fieldName) ? "(Attribute 미지정)" : fieldName;
            return $"{setLabel}.{fieldLabel}";
        }

        // 예: "CharacterAttributeSet.health (Target, Snapshot)". Modifier 요약과 같은 어트리뷰트 표기에 캡처 출처를 붙인다.
        private static string GetCaptureLabel(GameplayEffectAttributeCaptureDefinition definition)
        {
            GameplayAttribute attribute = definition.Attribute;
            if (!attribute.IsSet)
            {
                return "(캡처 미지정)";
            }

            string snapshotLabel = definition.Snapshot ? ", Snapshot" : string.Empty;
            return $"{GetAttributeLabel(attribute.AttributeSetTypeName, attribute.FieldName)} ({definition.CaptureSource}{snapshotLabel})";
        }

        private static string GetOperationLabel(GameplayModifierOperation op)
        {
            return _operationDisplayNames.TryGetValue(op, out string display) ? display : op.ToString();
        }
    }
}
