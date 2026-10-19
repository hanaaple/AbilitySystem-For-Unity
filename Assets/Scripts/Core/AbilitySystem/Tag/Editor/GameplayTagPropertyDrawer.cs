using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// <see cref="GameplayTag"/> 필드를 콤보(태그 칩 + 피커 팝업)로 그린다. 타입에 붙는 드로어라 어느 class·struct의 필드, List·배열 요소에도 적용된다.
    /// 필드에 <see cref="GameplayTagCategoriesAttribute"/>가 있으면 피커에 보일 태그를 제한한다.
    /// </summary>
    /// <remarks>UE: FGameplayTagCustomization</remarks>
    [CustomPropertyDrawer(typeof(GameplayTag))]
    public sealed class GameplayTagPropertyDrawer : PropertyDrawer
    {
        private const string NoneText = "None";
        private const string MultipleValuesText = "Multiple Values";
        private const float ArrowWidth = 14f;

        private static readonly GUIContent _arrowContent = new("▾");

        private static GUIStyle _arrowStyle;

        private readonly Dictionary<string, GameplayTagPropertyHandle> _propertyHandles = new();

        private static GUIStyle ArrowStyle => _arrowStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0) };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        /// <remarks>UE: FGameplayTagCustomization::CustomizeHeader</remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            int indentLevel = EditorGUI.indentLevel;

            // 값이 다른 오브젝트를 함께 고르면 칩이 Multiple Values를 직접 보이므로, Unity가 칩·▾ 글자를 '—'로 바꾸지 않게 끈다
            bool showMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = false;

            // 피커를 여는 경로가 GUI를 중단(ExitGUI)할 수 있어 들여쓰기·프로퍼티 스택을 반드시 되돌린다
            try
            {
                Rect comboRect = EditorGUI.PrefixLabel(position, label);
                EditorGUI.indentLevel = 0;

                GameplayTagPropertyHandle propertyHandle = GameplayTagDrawerUtility.GetPropertyHandle(_propertyHandles, property, false, fieldInfo);
                DrawCombo(comboRect, property, propertyHandle);
            }
            finally
            {
                EditorGUI.showMixedValue = showMixedValue;
                EditorGUI.indentLevel = indentLevel;
                EditorGUI.EndProperty();
            }
        }

        /// <summary>칩은 값이 있으면 태그 이름과 ×, 없으면 흐린 None, 오브젝트마다 다르면 Multiple Values를 보인다.</summary>
        /// <remarks>UE: SGameplayTagCombo</remarks>
        private static void DrawCombo(Rect rect, SerializedProperty property, GameplayTagPropertyHandle propertyHandle)
        {
            bool hasMultipleValues = property.hasMultipleDifferentValues;
            List<string> tagNames = GameplayTagPropertyHandle.ReadTagNames(property, false);
            string tagName = !hasMultipleValues && tagNames.Count > 0 ? tagNames[0] : null;
            bool isValid = tagName != null;

            string text = hasMultipleValues ? MultipleValuesText : tagName ?? NoneText;
            var content = new GUIContent(text, tagName ?? string.Empty);
            bool showClearButton = hasMultipleValues || isValid;
            float chipWidth = Mathf.Min(GameplayTagChip.GetWidth(content, showClearButton), rect.width - ArrowWidth);
            var chipRect = new Rect(rect.x, rect.y + 1f, chipWidth, rect.height - 2f);
            var arrowRect = new Rect(chipRect.xMax, rect.y, ArrowWidth, rect.height);

            switch (GameplayTagChip.Draw(chipRect, content, showClearButton, isValid))
            {
                case GameplayTagChip.ChipAction.Edit:
                {
                    GameplayTagDrawerUtility.OpenPicker(rect, propertyHandle, tagName);
                    break;
                }
                case GameplayTagChip.ChipAction.Clear:
                {
                    OnClearTag(propertyHandle);
                    break;
                }
                case GameplayTagChip.ChipAction.Menu:
                {
                    ShowTagMenu(propertyHandle, tagName);
                    break;
                }
                default:
                {
                    break;
                }
            }

            if (EditorGUI.DropdownButton(arrowRect, _arrowContent, FocusType.Keyboard, ArrowStyle))
            {
                GameplayTagDrawerUtility.OpenPicker(rect, propertyHandle, tagName);
            }
        }

        /// <remarks>UE: SGameplayTagCombo::OnTagMenu</remarks>
        private static void ShowTagMenu(GameplayTagPropertyHandle propertyHandle, string tagName)
        {
            EditorWindow ownerWindow = EditorWindow.focusedWindow;
            bool isValidTag = !string.IsNullOrEmpty(tagName);

            var menu = new GenericMenu();
            GameplayTagEditorUtility.AddMenuItem(menu, "Copy", isValidTag, () => OnCopyTag(tagName));
            GameplayTagEditorUtility.AddMenuItem(menu, "Paste", CanPaste(), () =>
            {
                OnPasteTag(propertyHandle);
                GameplayTagDrawerUtility.RepaintWindow(ownerWindow);
            });
            GameplayTagEditorUtility.AddMenuItem(menu, "Clear Gameplay Tag", isValidTag, () =>
            {
                OnClearTag(propertyHandle);
                GameplayTagDrawerUtility.RepaintWindow(ownerWindow);
            });
            menu.ShowAsContext();
        }

        /// <remarks>UE: SGameplayTagCombo::OnClearTag</remarks>
        private static void OnClearTag(GameplayTagPropertyHandle propertyHandle)
        {
            GameplayTagDrawerUtility.SetAllObjectValues(propertyHandle, "Clear Gameplay Tag", new List<string>());
        }

        /// <summary>태그를 문자열 그대로 복사한다. 붙여넣기는 이 형식을 받는다.</summary>
        /// <remarks>UE: SGameplayTagCombo::OnCopyTag</remarks>
        private static void OnCopyTag(string tagToCopy)
        {
            EditorGUIUtility.systemCopyBuffer = tagToCopy;
        }

        /// <remarks>UE: SGameplayTagCombo::OnPasteTag</remarks>
        private static void OnPasteTag(GameplayTagPropertyHandle propertyHandle)
        {
            if (GameplayTagEditorUtility.TryImportTag(EditorGUIUtility.systemCopyBuffer, out string pastedTag))
            {
                GameplayTagDrawerUtility.SetAllObjectValues(propertyHandle, "Paste Gameplay Tag", new List<string> { pastedTag });
            }
        }

        /// <remarks>UE: SGameplayTagCombo::CanPaste</remarks>
        private static bool CanPaste()
        {
            return GameplayTagEditorUtility.TryImportTag(EditorGUIUtility.systemCopyBuffer, out _);
        }
    }
}
