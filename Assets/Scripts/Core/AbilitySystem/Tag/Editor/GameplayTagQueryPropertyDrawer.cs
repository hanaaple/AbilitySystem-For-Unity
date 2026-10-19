using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// <see cref="GameplayTagQuery"/> 필드를 설명 한 줄 + Edit·Clear 버튼으로 그린다. 설명이나 Edit를 누르면 <see cref="GameplayTagQueryEditorWindow"/>를 연다.
    /// 타입에 붙는 드로어라 어느 class·struct의 필드, List·배열 요소에도 적용된다. 필드의 <see cref="GameplayTagCategoriesAttribute"/>는 창 안 태그 피커로 넘긴다.
    /// </summary>
    /// <remarks>UE: FGameplayTagQueryCustomization</remarks>
    [CustomPropertyDrawer(typeof(GameplayTagQuery))]
    public sealed class GameplayTagQueryPropertyDrawer : PropertyDrawer
    {
        private const float EditButtonWidth = 36f;
        private const float ClearButtonWidth = 20f;
        private const float ButtonSpacing = 2f;

        private static readonly GUIContent _emptyContent = new("Empty", "Empty Gameplay Tag Query");
        private static readonly GUIContent _multipleSelectedContent = new("Multiple Selected", "Multiple Selected");
        private static readonly GUIContent _editContent = new("Edit", "Edit Gameplay Tag Query.");
        private static readonly GUIContent _clearContent = new("×", "Clear Query");

        private static GUIStyle _grayStyle;

        private static GUIStyle GrayStyle => _grayStyle ??= new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        /// <remarks>UE: FGameplayTagQueryCustomization::CustomizeHeader</remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            int indentLevel = EditorGUI.indentLevel;

            // 창을 여는 경로가 GUI를 중단(ExitGUI)하므로 들여쓰기·프로퍼티 스택을 반드시 되돌린다
            try
            {
                Rect valueRect = EditorGUI.PrefixLabel(position, label);
                EditorGUI.indentLevel = 0;
                DrawEntryBox(valueRect, property);
            }
            finally
            {
                EditorGUI.indentLevel = indentLevel;
                EditorGUI.EndProperty();
            }
        }

        /// <remarks>UE: SGameplayTagQueryEntryBox::Construct</remarks>
        private void DrawEntryBox(Rect rect, SerializedProperty property)
        {
            var clearRect = new Rect(rect.xMax - ClearButtonWidth, rect.y, ClearButtonWidth, rect.height);
            var editRect = new Rect(clearRect.x - ButtonSpacing - EditButtonWidth, rect.y, EditButtonWidth, rect.height);
            var descriptionRect = new Rect(rect.x, rect.y, editRect.x - ButtonSpacing - rect.x, rect.height);

            GUIContent description = GetDescriptionContent(property);
            GUIStyle descriptionStyle = description == _emptyContent ? GrayStyle : EditorStyles.label;

            bool descriptionClicked = GUI.Button(descriptionRect, description, descriptionStyle);
            bool editClicked = GUI.Button(editRect, _editContent, EditorStyles.miniButton);
            bool clearClicked = GUI.Button(clearRect, _clearContent, EditorStyles.miniButton);

            if (clearClicked)
            {
                ClearQuery(property);
            }

            if (descriptionClicked || editClicked)
            {
                OpenEditor(rect, property);
            }
        }

        /// <remarks>UE: SGameplayTagQueryEntryBox::OnEditButtonClicked</remarks>
        private void OpenEditor(Rect rect, SerializedProperty property)
        {
            GameplayTagCategoriesAttribute categoriesAttribute = fieldInfo?.GetCustomAttribute<GameplayTagCategoriesAttribute>();
            Rect anchorRect = GUIUtility.GUIToScreenRect(rect);
            GameplayTagQueryEditorWindow.Open(property, categoriesAttribute?.Categories, anchorRect);
            GUIUtility.ExitGUI();
        }

        /// <remarks>UE: SGameplayTagQueryEntryBox::OnClearAllButtonClicked</remarks>
        private static void ClearQuery(SerializedProperty property)
        {
            property.boxedValue = new GameplayTagQuery();
            SerializedObject serializedObject = property.serializedObject;
            serializedObject.ApplyModifiedProperties();
            Undo.SetCurrentGroupName("Clear Query");
        }

        /// <remarks>UE: SGameplayTagQueryEntryBox::CacheQueryList</remarks>
        private static GUIContent GetDescriptionContent(SerializedProperty property)
        {
            if (property.hasMultipleDifferentValues)
            {
                return _multipleSelectedContent;
            }

            var query = (GameplayTagQuery)property.boxedValue;
            if (query.IsEmpty())
            {
                return _emptyContent;
            }

            // UE는 런타임 Build가 자동 설명을 만들지 않아 여기서 다시 만들지만, 우리 Build는 늘 만들어 그대로 쓴다
            string description = query.GetDescription();
            return new GUIContent(description, description);
        }
    }
}
