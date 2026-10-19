using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// <see cref="GameplayTagQuery"/> 필드 하나를 편집하는 창. 질의를 편집용 사본(<see cref="EditableGameplayTagQuery"/>)으로 바꿔 그리고, 고칠 때마다 대상 필드에 바로 쓴다.
    /// OK는 닫기, Cancel은 연 시점의 값으로 되돌린 뒤 닫기. 창은 한 번에 하나만 열린다.
    /// </summary>
    /// <remarks>UE: SGameplayTagQueryWidget</remarks>
    public sealed class GameplayTagQueryEditorWindow : EditorWindow
    {
        private const string DefaultTitle = "Tag Query Editor";
        private const string UndoName = "Edit Gameplay Tag Query";
        private const float AnchorMargin = 20f;
        private const float DialogButtonWidth = 80f;
        private const float ListButtonWidth = 20f;
        private const float ButtonSpacing = 2f;
        private const float AddButtonWidth = 100f;
        private const float SectionSpacing = 4f;
        private const float SeparatorSpacing = 5f;
        private const float SeparatorThickness = 1f;

        // 하위 식 줄의 라벨 칸("▼ [10]"). 종류 팝업은 폭을 고정해 남는 자리에 접힌 식의 설명을 띄운다
        private const float ChildLabelWidth = 56f;
        private const float ExprTypePopupWidth = 170f;
        private const float SummarySpacing = 6f;

        // 본문은 한 깊이에 한 번만 들여쓴다. 안내선은 위 줄의 접기 화살표 아래에 온다
        private const int BodyPaddingLeft = 16;
        private const int BodyPaddingVertical = 2;
        private const float GuideOffset = 6f;
        private const float GuideWidth = 2f;

        // GameplayTagQuery._userDescription. 런타임 직렬화 필드 이름을 바꾸면 여기도 바꿔야 한다.
        private const string QueryUserDescriptionPath = "_userDescription";

        // EditableGameplayTagQuery·EditableGameplayTagQueryExpression의 직렬화 필드 이름. 이름을 바꾸면 그쪽도 바꿔야 한다.
        private const string EditableUserDescriptionPath = "_userDescription";
        private const string RootExpressionPath = "_rootExpression";
        private const string ExprTypePath = "_exprType";
        private const string TagsPath = "_tags";
        private const string ExpressionsPath = "_expressions";

        private static readonly Vector2 WindowSize = new(600f, 400f);

        // 어두운·밝은 스킨 어느 쪽에서도 보이도록 반투명 회색. 본문 칠은 겹칠수록 진해져 깊이가 드러난다
        private static readonly Color SeparatorColor = new(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Color BodyTint = new(0.5f, 0.5f, 0.5f, 0.06f);

        // 깊이마다 안내선 색을 돌려, 멀리 떨어진 줄도 같은 깊이인지 색으로 알아보게 한다
        private static readonly Color[] _guideColors =
        {
            new(0.35f, 0.6f, 0.9f, 0.8f),
            new(0.45f, 0.75f, 0.45f, 0.8f),
            new(0.9f, 0.65f, 0.3f, 0.8f),
            new(0.7f, 0.5f, 0.85f, 0.8f),
        };

        // 팝업 0번은 빈 식(None). Exact 두 유형은 UE 5.6 에디터에 없어 덧붙였다
        private static readonly GameplayTagQueryExprType[] _exprTypes =
        {
            GameplayTagQueryExprType.AnyTagsMatch,
            GameplayTagQueryExprType.AllTagsMatch,
            GameplayTagQueryExprType.NoTagsMatch,
            GameplayTagQueryExprType.AnyTagsExactMatch,
            GameplayTagQueryExprType.AllTagsExactMatch,
            GameplayTagQueryExprType.AnyExprMatch,
            GameplayTagQueryExprType.AllExprMatch,
            GameplayTagQueryExprType.NoExprMatch,
        };

        private static readonly GUIContent[] _exprTypeOptions =
        {
            new("None"),
            new("Any Tags Match"),
            new("All Tags Match"),
            new("No Tags Match"),
            new("Any Tags Exact Match"),
            new("All Tags Exact Match"),
            new("Any Expressions Match"),
            new("All Expressions Match"),
            new("No Expressions Match"),
        };

        private static readonly GUIContent _addContent = new("+ Expression", "하위 식 추가");
        private static readonly GUIContent _removeContent = new("−", "이 식 제거");

        private static GUIStyle _bodyStyle;
        private static GUIStyle _summaryStyle;

        private static GUIStyle BodyStyle => _bodyStyle ??= new GUIStyle { padding = new RectOffset(BodyPaddingLeft, 0, BodyPaddingVertical, BodyPaddingVertical) };
        private static GUIStyle SummaryStyle => _summaryStyle ??= new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray }, clipping = TextClipping.Clip };

        private Object[] _targets;
        private string _propertyPath;
        private SerializedObject[] _targetSerializedObjects;
        private GameplayTagQuery[] _originalQueries;
        private string _categories;
        private bool _hasCommitted;

        // 사본을 만들 때 대상 필드가 있었는지. 대상 오브젝트나 필드가 지워졌으면 빈 편집기로 두고 아무 데도 쓰지 않는다
        private bool _hasTarget;

        private EditableGameplayTagQuery _editableQuery;
        private SerializedObject _editableSerializedObject;

        // 변경 감지는 질의 값 비교가 아니라 오브젝트 변경 횟수로 한다.
        // 태그 비교가 인터닝된 이름의 참조 비교라, 직렬화 경로를 거친 사본끼리는 내용이 같아도 다르게 나올 수 있다
        private int _lastTargetDirtyCount;
        private int _lastEditableDirtyCount;

        private Vector2 _scrollPosition;

        // 접은 식의 사본 속성 경로. 기본을 펼침으로 두어 새로 고른 식은 내용이 바로 보인다.
        // 경로는 Undo 등으로 사본을 다시 만들어도 같아 상태가 남는다. 대신 칸을 지우면 뒤 칸 경로가 바뀌어 옮겨 줘야 한다
        private readonly HashSet<string> _collapsedPaths = new();

        private void OnGUI()
        {
            // 도메인 리로드 뒤엔 편집용 SerializedObject가 사라져 있다. 대상과의 연결을 끊고 빈 편집기로 둔다
            if (_editableQuery == null || _editableSerializedObject == null)
            {
                ClearTarget();
                RebuildEditableQuery();
            }

            // 다시 만들면 그릴 내용이 바뀌므로 레이아웃 단계에서만 한다
            if (Event.current.type == EventType.Layout)
            {
                RebuildIfTargetChanged();
            }

            _editableSerializedObject.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawQuery();
            EditorGUILayout.EndScrollView();

            _editableSerializedObject.ApplyModifiedProperties();
            CommitIfEdited();

            DrawDialogButtons();
        }

        private void OnDestroy()
        {
            DestroyEditableQuery();
        }

        /// <summary>필드의 질의를 편집하는 창을 연다. 이미 열린 창은 닫는다.</summary>
        /// <param name="categories">창 안 태그 피커에 걸 필터(<see cref="GameplayTagCategoriesAttribute"/>). 없으면 null.</param>
        /// <param name="anchorRect">인스펙터의 질의 줄(화면 좌표). 창을 그 왼쪽에 띄운다.</param>
        /// <remarks>UE: UE::GameplayTags::Editor::OpenGameplayTagQueryWindow</remarks>
        public static void Open(SerializedProperty property, string categories, Rect anchorRect)
        {
            CloseOpenWindow();

            var window = CreateInstance<GameplayTagQueryEditorWindow>();
            window.Initialize(property, categories);
            window.titleContent = new GUIContent(BuildTitle(property));
            window.ShowUtility();
            window.position = GetWindowRect(anchorRect);
        }

        // 정적 참조로 들고 있으면 도메인 리로드 때 비어 이전 창이 안 닫히고 창이 여러 개가 된다. 열린 창을 매번 찾아 닫는다
        /// <remarks>UE: UE::GameplayTags::Editor::CloseGameplayTagQueryWindow</remarks>
        private static void CloseOpenWindow()
        {
            foreach (GameplayTagQueryEditorWindow window in Resources.FindObjectsOfTypeAll<GameplayTagQueryEditorWindow>())
            {
                window.Close();
            }
        }

        /// <remarks>UE: SGameplayTagQueryEntryBox::BuildWindowArgs</remarks>
        private static string BuildTitle(SerializedProperty property)
        {
            SerializedObject serializedObject = property.serializedObject;
            Object[] targets = serializedObject.targetObjects;

            if (targets.Length > 1)
            {
                return $"{DefaultTitle}: {property.displayName} {targets.Length} Assets";
            }

            if (targets.Length > 0 && targets[0] != null)
            {
                return $"{DefaultTitle}: {property.displayName} {targets[0].name}";
            }

            return DefaultTitle;
        }

        // UE처럼 인스펙터 줄의 왼쪽, 조금 위에 띄운다. 화면 왼쪽·위로 나가지 않게만 막는다
        private static Rect GetWindowRect(Rect anchorRect)
        {
            float x = Mathf.Max(0f, anchorRect.xMin - AnchorMargin - WindowSize.x);
            float y = Mathf.Max(0f, anchorRect.yMin - AnchorMargin - WindowSize.y * 0.2f);
            return new Rect(x, y, WindowSize.x, WindowSize.y);
        }

        private void Initialize(SerializedProperty property, string categories)
        {
            SerializedObject serializedObject = property.serializedObject;
            _targets = serializedObject.targetObjects;
            _propertyPath = property.propertyPath;
            _targetSerializedObjects = new SerializedObject[_targets.Length];
            _categories = categories;

            _originalQueries = new GameplayTagQuery[_targets.Length];
            for (int i = 0; i < _targets.Length; i++)
            {
                SerializedProperty targetProperty = FindTargetProperty(i);
                _originalQueries[i] = targetProperty != null ? (GameplayTagQuery)targetProperty.boxedValue : new GameplayTagQuery();
            }

            RebuildEditableQuery();
        }

        /// <remarks>UE: UEditableGameplayTagQuery 디테일 뷰</remarks>
        private void DrawQuery()
        {
            SerializedProperty userDescription = _editableSerializedObject.FindProperty(EditableUserDescriptionPath);
            EditorGUILayout.PropertyField(userDescription);
            EditorGUILayout.Space(SectionSpacing);

            // 루트 줄은 라벨 폭을 User Description과 같게 해 두 값 열을 맞춘다. 라벨·툴팁은 필드 이름·[Tooltip]에서
            SerializedProperty rootExpression = _editableSerializedObject.FindProperty(RootExpressionPath);
            DrawExpression(rootExpression, new GUIContent(rootExpression.displayName, rootExpression.tooltip), EditorGUIUtility.labelWidth, null, -1, 0);
        }

        /// <summary>
        /// 식 한 칸을 그린다 — 접기 화살표·종류 팝업·(목록의 칸이면) 제거 버튼 한 줄, 펼쳐 있으면 그 아래 본문(태그 컨테이너나 하위 식 목록).
        /// 접혀 있으면 본문 대신 그 식의 설명을 줄 끝에 흐리게 띄운다.
        /// </summary>
        /// <param name="labelWidth">이 줄의 라벨 칸 폭. 종류 팝업과 본문의 태그 상자가 이 폭 뒤에서 시작한다.</param>
        /// <param name="parentExpressions">이 칸이 든 하위 식 목록. 루트면 null.</param>
        /// <param name="depth">루트가 0. 본문 안내선 색을 고른다.</param>
        private void DrawExpression(SerializedProperty expression, GUIContent label, float labelWidth, SerializedProperty parentExpressions, int index, int depth)
        {
            bool isEmpty = string.IsNullOrEmpty(expression.managedReferenceFullTypename);
            SerializedProperty exprType = isEmpty ? null : expression.FindPropertyRelative(ExprTypePath);
            int optionIndex = exprType != null ? Array.IndexOf(_exprTypes, (GameplayTagQueryExprType)exprType.enumValueIndex) + 1 : 0;
            bool isFoldable = exprType != null;
            bool canRemove = parentExpressions != null;
            string path = expression.propertyPath;
            bool isExpanded = !_collapsedPaths.Contains(path);

            // 들여쓰기는 본문 여백으로만 준다(indentLevel을 쓰지 않음). 그래서 줄 Rect를 그대로 나눠 쓴다
            Rect rowRect = EditorGUILayout.GetControlRect();
            var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
            var removeRect = new Rect(rowRect.xMax - ListButtonWidth, rowRect.y, ListButtonWidth, rowRect.height);
            float fieldXMin = rowRect.x + labelWidth;
            float fieldXMax = canRemove ? removeRect.x - ButtonSpacing : rowRect.xMax;
            var popupRect = new Rect(fieldXMin, rowRect.y, Mathf.Min(ExprTypePopupWidth, fieldXMax - fieldXMin), rowRect.height);
            Rect summaryRect = Rect.MinMaxRect(popupRect.xMax + SummarySpacing, rowRect.y, fieldXMax, rowRect.yMax);

            bool newIsExpanded = DrawExpressionLabel(labelRect, label, isFoldable, isExpanded);
            int newOptionIndex = EditorGUI.Popup(popupRect, optionIndex, _exprTypeOptions);
            if (isFoldable && !isExpanded)
            {
                DrawSummary(summaryRect, expression);
            }

            bool removeClicked = canRemove && GUI.Button(removeRect, _removeContent, EditorStyles.miniButton);

            if (newOptionIndex != optionIndex)
            {
                // UE는 식 종류를 바꾸면 편집 오브젝트를 새로 만든다. 같은 칸에 새 노드를 넣어 태그·하위 식도 비운다
                ForgetCollapsedPaths(path);
                expression.managedReferenceValue = newOptionIndex == 0 ? null : new EditableGameplayTagQueryExpression(_exprTypes[newOptionIndex - 1]);
                ApplyStructuralChange();
            }

            if (removeClicked)
            {
                RemoveExpression(parentExpressions, index);
                ShiftCollapsedPaths(parentExpressions.propertyPath, index);
                ApplyStructuralChange();
            }

            // 이번 이벤트는 바뀌기 전 상태로 마저 그려야 레이아웃 단계에서 잡은 칸 수와 맞는다. 바뀐 상태는 다음 그리기부터 쓴다
            if (newIsExpanded != isExpanded)
            {
                SetCollapsed(path, !newIsExpanded);
                Repaint();
            }

            if (!isFoldable || !isExpanded)
            {
                return;
            }

            var queryExpression = new GameplayTagQueryExpression { ExprType = (GameplayTagQueryExprType)exprType.enumValueIndex };

            Rect bodyRect = EditorGUILayout.BeginVertical(BodyStyle);
            DrawBodyBackground(bodyRect, depth);

            if (queryExpression.UsesTagSet())
            {
                DrawTags(expression.FindPropertyRelative(TagsPath), fieldXMin);
            }
            else if (queryExpression.UsesExprSet())
            {
                DrawExpressionList(expression.FindPropertyRelative(ExpressionsPath), depth + 1);
            }

            EditorGUILayout.EndVertical();
        }

        /// <param name="depth">목록 칸들의 깊이.</param>
        private void DrawExpressionList(SerializedProperty expressions, int depth)
        {
            for (int i = 0; i < expressions.arraySize; i++)
            {
                // 여러 줄짜리 칸이 이어지면 경계가 흐려져, 같은 목록의 칸 사이에 줄을 긋는다
                if (i > 0)
                {
                    DrawSeparator();
                }

                DrawExpression(expressions.GetArrayElementAtIndex(i), new GUIContent($"[{i}]"), ChildLabelWidth, expressions, i, depth);
            }

            // 추가 버튼을 목록 끝(본문 안)에 두어 어느 묶음에 넣는지 위치로 드러낸다
            Rect addRect = EditorGUILayout.GetControlRect();
            addRect.width = AddButtonWidth;
            if (GUI.Button(addRect, _addContent, EditorStyles.miniButton))
            {
                // UE처럼 빈 칸을 넣고 종류는 팝업으로 고르게 한다. 늘린 칸이 이전 칸의 참조를 물려받을 수 있어 비운다
                int newIndex = expressions.arraySize;
                expressions.arraySize++;
                SerializedProperty added = expressions.GetArrayElementAtIndex(newIndex);
                added.managedReferenceValue = null;
                ApplyStructuralChange();
            }
        }

        /// <summary>라벨은 직접 그리고 컨테이너 드로어엔 빈 라벨을 넘긴다. 전역 labelWidth를 바꾸지 않고도 태그 상자를 위 줄의 종류 팝업과 같은 열에 맞추려고.</summary>
        /// <param name="fieldXMin">위 줄 종류 팝업의 왼쪽 x. 루트 줄과 본문 안 줄은 왼쪽 여백이 달라, 라벨 폭으로 계산하면 루트에서 몇 px 어긋난다.</param>
        private static void DrawTags(SerializedProperty tags, float fieldXMin)
        {
            float height = EditorGUI.GetPropertyHeight(tags, GUIContent.none, true);
            Rect rect = EditorGUILayout.GetControlRect(true, height);
            var labelRect = new Rect(rect.x, rect.y, fieldXMin - rect.x, EditorGUIUtility.singleLineHeight);
            Rect fieldRect = Rect.MinMaxRect(fieldXMin, rect.y, rect.xMax, rect.yMax);

            EditorGUI.LabelField(labelRect, new GUIContent(tags.displayName, tags.tooltip));
            EditorGUI.PropertyField(fieldRect, tags, GUIContent.none, true);
        }

        // 본문을 옅게 칠하고 왼쪽에 깊이별 색 안내선을 긋는다. 들여쓰기만으로는 깊을 때 어느 묶음인지 놓치기 쉬워서
        private static void DrawBodyBackground(Rect bodyRect, int depth)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(bodyRect, BodyTint);
            var guideRect = new Rect(bodyRect.x + GuideOffset, bodyRect.y, GuideWidth, bodyRect.height);
            EditorGUI.DrawRect(guideRect, _guideColors[depth % _guideColors.Length]);
        }

        private static void DrawSummary(Rect rect, SerializedProperty expression)
        {
            if (rect.width <= 0f)
            {
                return;
            }

            string summary = BuildSummary(expression);
            var content = new GUIContent(summary, summary);
            GUI.Label(rect, content, SummaryStyle);
        }

        // 런타임 식으로 바꿔 질의 자동 설명과 같은 문구(" ANY( A, B )" 꼴)를 만든다
        private static string BuildSummary(SerializedProperty expression)
        {
            var editableExpr = expression.managedReferenceValue as EditableGameplayTagQueryExpression;
            GameplayTagQueryExpression expr = EditableGameplayTagQueryExpression.ToQueryExpression(editableExpr);

            var description = new StringBuilder();
            expr.EmitTokens(new List<byte>(), new List<GameplayTag>(), description);
            string summary = description.ToString();
            return summary.Trim();
        }

        /// <returns>그린 뒤의 펼침 상태. 접을 내용이 없는 칸은 받은 값 그대로.</returns>
        private static bool DrawExpressionLabel(Rect rect, GUIContent label, bool isFoldable, bool isExpanded)
        {
            if (isFoldable)
            {
                return EditorGUI.Foldout(rect, isExpanded, label, true);
            }

            // 화살표가 없는 칸도 글자 시작을 화살표 있는 칸에 맞춘다
            RectOffset foldoutPadding = EditorStyles.foldout.padding;
            RectOffset labelPadding = EditorStyles.label.padding;
            rect.xMin += foldoutPadding.left - labelPadding.left;
            EditorGUI.LabelField(rect, label);
            return isExpanded;
        }

        private static void DrawSeparator()
        {
            Rect lineRect = EditorGUILayout.GetControlRect(false, SeparatorSpacing);
            lineRect.y += (lineRect.height - SeparatorThickness) * 0.5f;
            lineRect.height = SeparatorThickness;
            EditorGUI.DrawRect(lineRect, SeparatorColor);
        }

        private void SetCollapsed(string path, bool isCollapsed)
        {
            if (isCollapsed)
            {
                _collapsedPaths.Add(path);
            }
            else
            {
                _collapsedPaths.Remove(path);
            }
        }

        // 종류를 바꾸면 새 노드라, 이전 노드와 그 하위 식의 접힘 상태를 버린다
        private void ForgetCollapsedPaths(string path)
        {
            string childPrefix = path + ".";
            _collapsedPaths.RemoveWhere(collapsedPath => collapsedPath == path || collapsedPath.StartsWith(childPrefix, StringComparison.Ordinal));
        }

        // 칸을 지우면 뒤 칸들의 경로 인덱스가 하나씩 당겨지므로 접힘 상태도 같이 옮긴다. 지운 칸과 그 하위 식의 상태는 버린다
        private void ShiftCollapsedPaths(string listPath, int removedIndex)
        {
            string elementPrefix = listPath + ".Array.data[";
            var shiftedPaths = new List<string>();

            foreach (string path in _collapsedPaths)
            {
                if (!path.StartsWith(elementPrefix, StringComparison.Ordinal))
                {
                    shiftedPaths.Add(path);
                    continue;
                }

                int indexEnd = path.IndexOf(']', elementPrefix.Length);
                int elementIndex = int.Parse(path.Substring(elementPrefix.Length, indexEnd - elementPrefix.Length));
                if (elementIndex < removedIndex)
                {
                    shiftedPaths.Add(path);
                }
                else if (elementIndex > removedIndex)
                {
                    shiftedPaths.Add(elementPrefix + (elementIndex - 1) + path.Substring(indexEnd));
                }
            }

            _collapsedPaths.Clear();
            _collapsedPaths.UnionWith(shiftedPaths);
        }

        /// <remarks>UE: SGameplayTagQueryWidget::OnOkClicked / OnCancelClicked</remarks>
        private void DrawDialogButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool okClicked = GUILayout.Button("OK", GUILayout.Width(DialogButtonWidth));
            bool cancelClicked = GUILayout.Button("Cancel", GUILayout.Width(DialogButtonWidth));
            EditorGUILayout.EndHorizontal();

            if (cancelClicked)
            {
                RestoreOriginalQueries();
            }

            if (okClicked || cancelClicked)
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }

        // 참조형 배열은 첫 삭제가 칸을 비우기만 하는 경우가 있어, 크기가 그대로면 한 번 더 지운다
        private static void RemoveExpression(SerializedProperty expressions, int index)
        {
            int sizeBefore = expressions.arraySize;
            expressions.DeleteArrayElementAtIndex(index);
            if (expressions.arraySize == sizeBefore)
            {
                expressions.DeleteArrayElementAtIndex(index);
            }
        }

        // 칸 수가 바뀌면 같은 이벤트의 남은 레이아웃이 어긋나므로, 반영한 뒤 이번 GUI를 끝낸다
        private void ApplyStructuralChange()
        {
            _editableSerializedObject.ApplyModifiedProperties();
            CommitIfEdited();
            GUIUtility.ExitGUI();
        }

        /// <summary>사본이 바뀌었으면(이 창의 입력이든 창 안 태그 피커든) 질의로 만들어 모든 대상에 쓴다.</summary>
        /// <remarks>UE: SGameplayTagQueryWidget::NotifyPostChange</remarks>
        private void CommitIfEdited()
        {
            int editableDirtyCount = EditorUtility.GetDirtyCount(_editableQuery);
            if (editableDirtyCount == _lastEditableDirtyCount)
            {
                return;
            }

            if (!_hasTarget)
            {
                _lastEditableDirtyCount = editableDirtyCount;
                return;
            }

            GameplayTagQuery query = _editableQuery.BuildQuery();
            var perObjectQueries = new GameplayTagQuery[_targets.Length];
            Array.Fill(perObjectQueries, query);

            // 사본의 변경과 같은 Undo 단계로 묶으려고 그룹을 새로 열지 않는다
            WriteQueries(perObjectQueries);
            _hasCommitted = true;

            _lastEditableDirtyCount = editableDirtyCount;
            _lastTargetDirtyCount = GetTargetDirtyCount();
        }

        /// <remarks>UE: SGameplayTagQueryWidget::OnCancelClicked</remarks>
        private void RestoreOriginalQueries()
        {
            if (!_hasCommitted || _originalQueries == null)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            WriteQueries(_originalQueries);
        }

        /// <remarks>UE: SGameplayTagQueryEntryBox::OnQueriesCommitted</remarks>
        private void WriteQueries(GameplayTagQuery[] perObjectQueries)
        {
            if (_targets == null)
            {
                return;
            }

            Undo.SetCurrentGroupName(UndoName);

            for (int i = 0; i < _targets.Length; i++)
            {
                SerializedProperty property = FindTargetProperty(i);
                if (property == null)
                {
                    continue;
                }

                property.boxedValue = perObjectQueries[i];
                _targetSerializedObjects[i].ApplyModifiedProperties();
            }
        }

        // Undo·인스펙터 Clear처럼 창 밖에서 대상이 바뀌면 사본을 그 값으로 다시 만든다. 같은 에셋의 다른 필드가 바뀌어도 다시 만들지만, 값은 같아 잃는 것이 없다.
        // 대상 오브젝트나 필드가 지워진 것도 여기서 알아채 연결을 끊고 빈 편집기로 바꾼다
        private void RebuildIfTargetChanged()
        {
            if (!_hasTarget)
            {
                return;
            }

            if (FindTargetProperty(0) == null)
            {
                ClearTarget();
                RebuildEditableQuery();
                return;
            }

            if (GetTargetDirtyCount() != _lastTargetDirtyCount)
            {
                RebuildEditableQuery();
            }
        }

        /// <summary>첫 대상의 지금 값으로 편집용 사본을 다시 만든다. 여러 오브젝트를 골랐으면 UE처럼 첫 오브젝트 값을 편집한다. 대상이 없으면 빈 질의.</summary>
        private void RebuildEditableQuery()
        {
            DestroyEditableQuery();

            SerializedProperty property = FindTargetProperty(0);
            _hasTarget = property != null;

            GameplayTagQuery query = new GameplayTagQuery();
            string userDescription = string.Empty;
            if (_hasTarget)
            {
                query = (GameplayTagQuery)property.boxedValue;
                SerializedProperty userDescriptionProperty = property.FindPropertyRelative(QueryUserDescriptionPath);
                userDescription = userDescriptionProperty.stringValue;
            }
            else
            {
                titleContent = new GUIContent(DefaultTitle);
            }

            _editableQuery = EditableGameplayTagQuery.CreateEditableQuery(query, userDescription);
            _editableSerializedObject = new SerializedObject(_editableQuery);
            GameplayTagDrawerUtility.SetCategoriesOverride(_editableQuery, _categories);

            _lastEditableDirtyCount = EditorUtility.GetDirtyCount(_editableQuery);
            _lastTargetDirtyCount = GetTargetDirtyCount();
        }

        // 빈 편집기로 바꿀 때 이전 대상에 쓰지 않도록(편집·Cancel 모두) 대상 정보를 모두 버린다
        private void ClearTarget()
        {
            _targets = null;
            _propertyPath = null;
            _targetSerializedObjects = null;
            _originalQueries = null;
            _hasCommitted = false;
        }

        private int GetTargetDirtyCount()
        {
            bool hasTargetObject = _targets != null && _targets.Length > 0 && _targets[0] != null;
            return hasTargetObject ? EditorUtility.GetDirtyCount(_targets[0]) : 0;
        }

        private void DestroyEditableQuery()
        {
            if (_editableQuery == null)
            {
                return;
            }

            GameplayTagDrawerUtility.RemoveCategoriesOverride(_editableQuery);
            _editableSerializedObject?.Dispose();
            DestroyImmediate(_editableQuery);

            _editableQuery = null;
            _editableSerializedObject = null;
        }

        private SerializedProperty FindTargetProperty(int index)
        {
            if (_targets == null || index >= _targets.Length || string.IsNullOrEmpty(_propertyPath))
            {
                return null;
            }

            Object target = _targets[index];
            if (target == null)
            {
                return null;
            }

            // 도메인 리로드 뒤엔 대상 참조는 남지만 SerializedObject 배열은 사라져 있다
            if (_targetSerializedObjects == null || _targetSerializedObjects.Length != _targets.Length)
            {
                _targetSerializedObjects = new SerializedObject[_targets.Length];
            }

            SerializedObject serializedObject = _targetSerializedObjects[index];
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                serializedObject = new SerializedObject(target);
                _targetSerializedObjects[index] = serializedObject;
            }

            serializedObject.UpdateIfRequiredOrScript();
            return serializedObject.FindProperty(_propertyPath);
        }
    }
}
