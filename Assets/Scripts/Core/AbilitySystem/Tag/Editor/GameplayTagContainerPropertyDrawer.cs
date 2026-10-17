using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// <see cref="GameplayTagContainer"/> 필드를 태그 칩 목록 + 다중 선택 피커 팝업으로 그린다. 타입에 붙는 드로어라 어느 class·struct의 필드, List·배열 요소에도 적용된다.
    /// 필드에 <see cref="GameplayTagCategoriesAttribute"/>가 있으면 피커에 보일 태그를 제한한다.
    /// </summary>
    /// <remarks>UE: FGameplayTagContainerCustomization</remarks>
    [CustomPropertyDrawer(typeof(GameplayTagContainer))]
    public sealed class GameplayTagContainerPropertyDrawer : PropertyDrawer
    {
        private const float BoxPadding = 2f;
        private const float ChipSpacing = 2f;
        private const float ArrowWidth = 14f;
        private const float EmptyTextIndent = 4f;

        private static readonly GUIContent _emptyContent = new("Empty", "Empty Gameplay Tag container");
        private static readonly GUIContent _arrowContent = new("▾");

        private static GUIStyle _arrowStyle;
        private static GUIStyle _emptyStyle;

        private readonly Dictionary<string, GameplayTagPropertyHandle> _propertyHandles = new();

        private static GUIStyle ArrowStyle => _arrowStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0) };

        private static GUIStyle EmptyStyle => _emptyStyle ??= new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };

        // 높이와 그리기가 같은 목록(GetTagsToEdit)에서 줄 수를 정해야 칩과 영역이 어긋나지 않는다
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            List<EditableItem> tagsToEdit = GetTagsToEdit(property);
            return GetComboHeight(tagsToEdit.Count);
        }

        /// <remarks>UE: FGameplayTagContainerCustomization::CustomizeHeader</remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            int indentLevel = EditorGUI.indentLevel;

            // 피커를 여는 경로가 GUI를 중단(ExitGUI)할 수 있어 들여쓰기·프로퍼티 스택을 반드시 되돌린다
            try
            {
                var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                Rect valueRect = EditorGUI.PrefixLabel(labelRect, label);
                EditorGUI.indentLevel = 0;

                var comboRect = new Rect(valueRect.x, position.y, valueRect.width, position.height);
                DrawCombo(comboRect, property);
            }
            finally
            {
                EditorGUI.indentLevel = indentLevel;
                EditorGUI.EndProperty();
            }
        }

        /// <summary>
        /// 상자 안에 태그 칩을 한 줄에 하나씩 그린다. 칩을 누르면 그 태그를 펼친 채 피커를 열고, ×는 그 태그만 뺀다. 비었으면 Empty.
        /// 일부 오브젝트에만 있는 태그는 흐리게 보인다.
        /// </summary>
        /// <remarks>UE: SGameplayTagContainerCombo</remarks>
        private void DrawCombo(Rect rect, SerializedProperty property)
        {
            GameplayTagPropertyHandle propertyHandle = GetPropertyHandle(property);
            List<EditableItem> tagsToEdit = GetTagsToEdit(property);
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var boxRect = new Rect(rect.x, rect.y, rect.width - ArrowWidth, rect.height);
            var arrowRect = new Rect(boxRect.xMax, rect.y, ArrowWidth, lineHeight);
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.Repaint)
            {
                GUIStyle boxStyle = EditorStyles.helpBox;
                boxStyle.Draw(boxRect, false, false, false, false);
            }

            if (tagsToEdit.Count == 0)
            {
                var emptyRect = new Rect(boxRect.x + BoxPadding + EmptyTextIndent, boxRect.y + BoxPadding, boxRect.width - BoxPadding * 2f - EmptyTextIndent, lineHeight);
                EditorGUI.LabelField(emptyRect, _emptyContent, EmptyStyle);

                if (currentEvent.type == EventType.ContextClick && boxRect.Contains(currentEvent.mousePosition))
                {
                    currentEvent.Use();
                    ShowEmptyMenu(propertyHandle);
                }
            }

            float chipY = boxRect.y + BoxPadding;
            foreach (EditableItem item in tagsToEdit)
            {
                var content = new GUIContent(item.TagName, item.TagName);
                float chipWidth = Mathf.Min(GameplayTagChip.GetWidth(content, true), boxRect.width - BoxPadding * 2f);
                var chipRect = new Rect(boxRect.x + BoxPadding, chipY, chipWidth, lineHeight);

                switch (GameplayTagChip.Draw(chipRect, content, true, !item.HasMultipleValues))
                {
                    case GameplayTagChip.ChipAction.Edit:
                    {
                        GameplayTagDrawerUtility.OpenPicker(rect, propertyHandle, item.TagName);
                        break;
                    }
                    case GameplayTagChip.ChipAction.Clear:
                    {
                        OnClearTagClicked(propertyHandle, item.TagName);
                        break;
                    }
                    case GameplayTagChip.ChipAction.Menu:
                    {
                        ShowTagMenu(propertyHandle, item.TagName);
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }

                chipY += lineHeight + ChipSpacing;
            }

            string tagToHilight = tagsToEdit.Count > 0 ? tagsToEdit[0].TagName : null;
            if (EditorGUI.DropdownButton(arrowRect, _arrowContent, FocusType.Keyboard, ArrowStyle))
            {
                GameplayTagDrawerUtility.OpenPicker(rect, propertyHandle, tagToHilight);
            }

            // 칩 사이·옆의 빈 곳을 눌러도 연다. 칩은 자기 클릭을 먼저 가져간다.
            if (GUI.enabled && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && boxRect.Contains(currentEvent.mousePosition))
            {
                currentEvent.Use();
                GameplayTagDrawerUtility.OpenPicker(rect, propertyHandle, tagToHilight);
            }
        }

        /// <summary>표시할 태그 목록. 여러 오브젝트를 고쳐 쓰는 중이면 합집합이고, 모든 오브젝트에 있지 않은 태그는 표시해 둔다.</summary>
        /// <remarks>UE: SGameplayTagContainerCombo::RefreshTagContainers</remarks>
        private List<EditableItem> GetTagsToEdit(SerializedProperty property)
        {
            var tagsToEdit = new List<EditableItem>();
            SerializedObject serializedObject = property.serializedObject;
            if (!serializedObject.isEditingMultipleObjects)
            {
                foreach (string tagName in GameplayTagPropertyHandle.ReadTagNames(property, true))
                {
                    tagsToEdit.Add(new EditableItem(tagName, false));
                }
            }
            else
            {
                GameplayTagPropertyHandle propertyHandle = GetPropertyHandle(property);
                List<List<string>> tagContainers = propertyHandle.GetEditableTagContainers();
                var tagCounts = new Dictionary<string, int>();
                foreach (List<string> tagContainer in tagContainers)
                {
                    foreach (string tagName in tagContainer)
                    {
                        tagCounts[tagName] = tagCounts.GetValueOrDefault(tagName) + 1;
                    }
                }

                foreach (KeyValuePair<string, int> tagCount in tagCounts)
                {
                    tagsToEdit.Add(new EditableItem(tagCount.Key, tagCount.Value != tagContainers.Count));
                }
            }

            tagsToEdit.Sort(CompareEditableItems);
            return tagsToEdit;
        }

        private GameplayTagPropertyHandle GetPropertyHandle(SerializedProperty property)
        {
            return GameplayTagDrawerUtility.GetPropertyHandle(_propertyHandles, property, true, fieldInfo);
        }

        private static float GetComboHeight(int tagCount)
        {
            int rowCount = Mathf.Max(1, tagCount);
            return rowCount * EditorGUIUtility.singleLineHeight + (rowCount - 1) * ChipSpacing + BoxPadding * 2f;
        }

        private static int CompareEditableItems(EditableItem left, EditableItem right)
        {
            return GameplayTagEditorUtility.CompareTagNames(left.TagName, right.TagName);
        }

        /// <remarks>UE: SGameplayTagContainerCombo::OnTagMenu</remarks>
        private static void ShowTagMenu(GameplayTagPropertyHandle propertyHandle, string tagName)
        {
            EditorWindow ownerWindow = EditorWindow.focusedWindow;

            var menu = new GenericMenu();
            GameplayTagEditorUtility.AddMenuItem(menu, "Copy", true, () => OnCopyTag(tagName));
            GameplayTagEditorUtility.AddMenuItem(menu, "Paste", CanPaste(), () =>
            {
                OnPasteTag(propertyHandle);
                GameplayTagDrawerUtility.RepaintWindow(ownerWindow);
            });
            GameplayTagEditorUtility.AddMenuItem(menu, "Clear All Tags", true, () =>
            {
                OnClearAll(propertyHandle);
                GameplayTagDrawerUtility.RepaintWindow(ownerWindow);
            });
            menu.ShowAsContext();
        }

        /// <remarks>UE: SGameplayTagContainerCombo::OnEmptyMenu</remarks>
        private static void ShowEmptyMenu(GameplayTagPropertyHandle propertyHandle)
        {
            EditorWindow ownerWindow = EditorWindow.focusedWindow;

            var menu = new GenericMenu();
            GameplayTagEditorUtility.AddMenuItem(menu, "Paste", CanPaste(), () =>
            {
                OnPasteTag(propertyHandle);
                GameplayTagDrawerUtility.RepaintWindow(ownerWindow);
            });
            menu.ShowAsContext();
        }

        /// <remarks>UE: SGameplayTagContainerCombo::OnClearTagClicked</remarks>
        private static void OnClearTagClicked(GameplayTagPropertyHandle propertyHandle, string tagToClear)
        {
            List<List<string>> tagContainers = propertyHandle.GetEditableTagContainers();
            foreach (List<string> tagContainer in tagContainers)
            {
                tagContainer.Remove(tagToClear);
            }

            propertyHandle.SetPerObjectValues("Remove Gameplay Tag", tagContainers);
        }

        /// <remarks>UE: SGameplayTagContainerCombo::OnClearAll</remarks>
        private static void OnClearAll(GameplayTagPropertyHandle propertyHandle)
        {
            GameplayTagDrawerUtility.SetAllObjectValues(propertyHandle, "Clear All Tags", new List<string>());
        }

        /// <summary>태그를 문자열 그대로 복사한다. 붙여넣기는 이 형식을 받는다.</summary>
        /// <remarks>UE: SGameplayTagContainerCombo::OnCopyTag</remarks>
        private static void OnCopyTag(string tagToCopy)
        {
            EditorGUIUtility.systemCopyBuffer = tagToCopy;
        }

        /// <summary>태그 하나면 오브젝트마다 더하고, 태그 목록이면 오브젝트마다 그 목록으로 바꾼다.</summary>
        /// <remarks>UE: SGameplayTagContainerCombo::OnPasteTag</remarks>
        private static void OnPasteTag(GameplayTagPropertyHandle propertyHandle)
        {
            string pastedText = EditorGUIUtility.systemCopyBuffer;
            if (GameplayTagEditorUtility.TryImportTag(pastedText, out string pastedTag))
            {
                List<List<string>> tagContainers = propertyHandle.GetEditableTagContainers();
                foreach (List<string> tagContainer in tagContainers)
                {
                    if (!tagContainer.Contains(pastedTag))
                    {
                        tagContainer.Add(pastedTag);
                    }
                }

                propertyHandle.SetPerObjectValues("Paste Gameplay Tag", tagContainers);
                return;
            }

            var pastedTagNames = new List<string>();
            if (GameplayTagEditorUtility.TryImportTagContainer(pastedText, pastedTagNames))
            {
                GameplayTagDrawerUtility.SetAllObjectValues(propertyHandle, "Paste Gameplay Tag Container", pastedTagNames);
            }
        }

        /// <remarks>UE: SGameplayTagContainerCombo::CanPaste</remarks>
        private static bool CanPaste()
        {
            string pastedText = EditorGUIUtility.systemCopyBuffer;
            return GameplayTagEditorUtility.TryImportTag(pastedText, out _) || GameplayTagEditorUtility.TryImportTagContainer(pastedText, new List<string>());
        }

        /// <remarks>UE: SGameplayTagContainerCombo::FEditableItem</remarks>
        private readonly struct EditableItem
        {
            public readonly string TagName;

            /// <summary>선택된 오브젝트 중 일부에만 있는 태그.</summary>
            public readonly bool HasMultipleValues;

            public EditableItem(string tagName, bool hasMultipleValues)
            {
                TagName = tagName;
                HasMultipleValues = hasMultipleValues;
            }
        }
    }
}
