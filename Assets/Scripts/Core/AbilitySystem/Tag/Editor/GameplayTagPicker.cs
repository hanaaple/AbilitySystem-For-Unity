using System;
using System.Collections.Generic;
using Core.Common;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// 태그 트리를 검색·계층 트리로 보여 주는 피커 본체. 필드 값을 고르는 팝업(선택 모드)과 태그 정의를 관리하는 창(관리 모드)이 같은 것을 담는다.
    /// 선택 모드는 필드 값을 오브젝트별 명시 태그 목록으로 들고, 체크·해제 규칙을 적용한 뒤 목록째 다시 쓴다.
    /// </summary>
    /// <remarks>UE: SGameplayTagPicker</remarks>
    internal sealed class GameplayTagPicker : IDisposable
    {
        private const string SettingsKeyPrefix = "GameplayTagPicker.";
        private const string NewTagNameControlName = "GameplayTagPicker.NewTagName";
        private const float IconButtonWidth = 20f;
        private const float NewTagNameLabelWidth = 42f;
        private const float SearchHintIndent = 16f;
        private const float SourceColumnRatio = 0.4f;
        private const float ExplicitSourceAlpha = 0.5f;
        private const float ImplicitSourceAlpha = 0.25f;

        private static readonly Color _menuItemHoverColor = new(0.5f, 0.5f, 0.5f, 0.2f);
        private static readonly Color _separatorColor = new(0.5f, 0.5f, 0.5f, 0.4f);

        private static GUIContent _addNewTagContent;
        private static GUIContent _closeSectionContent;
        private static GUIContent _settingsContent;
        private static GUIStyle _menuItemStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _sourceStyle;

        private readonly GameplayTagPickerMode _gameplayTagPickerMode;
        private readonly GameplayTagPropertyHandle _propertyHandle;
        private readonly bool _multiSelect;
        private readonly bool _showMenuItems;
        private readonly string _rootFilterString;
        private readonly string _settingsName;
        private readonly Action _onTagChanged;
        private readonly List<GameplayTagNode> _tagItems = new();
        private readonly HashSet<string> _cachedExpandedItems = new();
        private readonly Dictionary<string, List<string>> _sourceNamesCache = new();
        private readonly SearchField _searchTagBox = new();
        private readonly GameplayTagTreeView _tagTreeWidget;

        private List<List<string>> _tagContainers = new();
        private HashSet<int> _lastExpandedIds = new();
        private string _filterString = string.Empty;
        private string _requestedScrollToTag;
        private bool _delayRefresh;
        private bool _persistExpansionChange = true;
        private bool _hasDrawnTree;
        private bool _newTagWidgetVisible;
        private bool _focusNewTagName;
        private string _newTagName = string.Empty;
        private string _notification;
        private MessageType _notificationType;

        private enum CheckBoxState
        {
            Unchecked,
            Checked,
            Undetermined,
        }

        private enum GameplayTagAdd
        {
            Root,
            Child,
            Duplicate,
        }

        /// <summary>피커를 담은 창. 비동기 갱신(트리 재구성·메뉴 콜백) 뒤 다시 그리게 한다.</summary>
        public EditorWindow HostWindow { get; set; }

        private static GUIContent AddNewTagContent => _addNewTagContent ??= EditorGUIUtility.TrIconContent("Toolbar Plus", "Add New Gameplay Tag");

        private static GUIContent CloseSectionContent => _closeSectionContent ??= EditorGUIUtility.TrIconContent("Toolbar Minus", "Close Section");

        private static GUIContent SettingsContent => _settingsContent ??= EditorGUIUtility.TrIconContent("_Popup", "View Settings");

        private static GUIStyle MenuItemStyle => _menuItemStyle ??= new GUIStyle(EditorStyles.label) { padding = new RectOffset(8, 4, 2, 2) };

        private static GUIStyle HintStyle => _hintStyle ??= new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };

        private static GUIStyle SourceStyle => _sourceStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, clipping = TextClipping.Clip };

        /// <param name="propertyHandle">선택 모드에서 고칠 필드. 관리 모드면 null.</param>
        /// <param name="filter">루트를 제한하는 필터(<see cref="GameplayTagsManager.GetFilteredGameplayRootTags"/>).</param>
        /// <param name="settingsName">펼침 상태를 저장할 때 키를 나누는 이름. 쓰임새(팝업·관리 창)마다 따로 기억한다.</param>
        /// <param name="showMenuItems">맨 위에 Clear Selection·Manage Gameplay Tags... 메뉴를 둔다(팝업용).</param>
        /// <param name="onTagChanged">필드 값을 고친 뒤 호출된다.</param>
        public GameplayTagPicker(GameplayTagPickerMode mode, GameplayTagPropertyHandle propertyHandle, bool multiSelect, string filter, string settingsName, bool showMenuItems, Action onTagChanged)
        {
            _gameplayTagPickerMode = mode;
            _propertyHandle = propertyHandle;
            _multiSelect = multiSelect;
            _rootFilterString = filter ?? string.Empty;
            _settingsName = settingsName ?? string.Empty;
            _showMenuItems = showMenuItems;
            _onTagChanged = onTagChanged;
            _tagTreeWidget = new GameplayTagTreeView(new TreeViewState<int>(), this);

            if (_propertyHandle != null)
            {
                _tagContainers = _propertyHandle.GetEditableTagContainers();
            }

            GameplayTagsManager.OnEditorRefreshGameplayTagTree += RefreshOnNextTick;

            GameplayTagsManager manager = GameplayTagsManager.Get();
            manager.GetFilteredGameplayRootTags(_rootFilterString, _tagItems);
            _tagTreeWidget.Reload();

            // 처음엔 모두 접고, 저장된 펼침 상태(없으면 체크된 태그)만 편다
            var expandedIds = new HashSet<int>();
            foreach (GameplayTagNode tagItem in _tagItems)
            {
                LoadTagNodeItemExpansion(tagItem, expandedIds);
            }

            SetExpandedIds(expandedIds, false);
        }

        public void Dispose()
        {
            GameplayTagsManager.OnEditorRefreshGameplayTagTree -= RefreshOnNextTick;
        }

        public void OnGUI(Rect rect)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.Layout)
            {
                Tick();
            }

            GUILayout.BeginArea(rect);

            if (_showMenuItems)
            {
                DrawMenuItems();
            }

            DrawTreeControls();

            if (_newTagWidgetVisible)
            {
                DrawAddNewTagWidget();
            }

            if (!string.IsNullOrEmpty(_notification))
            {
                EditorGUILayout.HelpBox(_notification, _notificationType);
            }

            Rect treeRect = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f);
            _tagTreeWidget.OnGUI(treeRect);
            if (currentEvent.type == EventType.Repaint)
            {
                _hasDrawnTree = true;
            }

            GUILayout.EndArea();

            if (currentEvent.type == EventType.MouseMove)
            {
                RepaintHost();
            }
        }

        /// <summary>다음 그리기에서 태그까지 부모를 펼치고 그 행을 선택·스크롤한다.</summary>
        /// <remarks>UE: SGameplayTagPicker::RequestScrollToView</remarks>
        public void RequestScrollToView(string tagName)
        {
            _requestedScrollToTag = string.IsNullOrEmpty(tagName) ? null : tagName;
        }

        /// <remarks>UE: SGameplayTagPicker::GetWidgetToFocusOnOpen</remarks>
        public void FocusSearchBox()
        {
            _searchTagBox.SetFocus();
        }

        /// <summary>필드에서 더는 등록되지 않은 태그를 지우고 알린다. 피커를 열기 직전에 부른다.</summary>
        /// <remarks>UE: SGameplayTagPicker::VerifyAssetTagValidity</remarks>
        public static void VerifyAssetTagValidity(GameplayTagPropertyHandle propertyHandle)
        {
            List<List<string>> tagContainers = propertyHandle.GetEditableTagContainers();
            var invalidTagNames = new List<string>();
            foreach (List<string> tagContainer in tagContainers)
            {
                for (int i = tagContainer.Count - 1; i >= 0; i--)
                {
                    string tagName = tagContainer[i];
                    if (GameplayTagEditorUtility.IsRegisteredTag(tagName))
                    {
                        continue;
                    }

                    if (!invalidTagNames.Contains(tagName))
                    {
                        invalidTagNames.Add(tagName);
                    }

                    tagContainer.RemoveAt(i);
                }
            }

            if (invalidTagNames.Count == 0)
            {
                return;
            }

            propertyHandle.SetPerObjectValues("Remove Invalid Gameplay Tags", tagContainers);
            EditorUtility.DisplayDialog("Warning", $"Invalid Tags that have been removed: \n\n{string.Join("\n", invalidTagNames)}", "OK");
        }

        private void Tick()
        {
            // 열린 동안 Undo·다른 창 편집으로 값이 바뀔 수 있어 매 프레임 다시 읽는다
            if (_propertyHandle != null)
            {
                _tagContainers = _propertyHandle.GetEditableTagContainers();
            }

            if (_delayRefresh)
            {
                RefreshTags();
            }

            // 프레이밍은 트리 영역 크기를 알아야 하므로 한 번 그려진 뒤에 한다
            if (_requestedScrollToTag != null && _hasDrawnTree)
            {
                ScrollToTag(_requestedScrollToTag);
                _requestedScrollToTag = null;
            }
        }

        /// <remarks>UE: SGameplayTagPicker::RefreshTags</remarks>
        private void RefreshTags()
        {
            _delayRefresh = false;
            _sourceNamesCache.Clear();

            GameplayTagsManager manager = GameplayTagsManager.Get();
            manager.GetFilteredGameplayRootTags(_rootFilterString, _tagItems);

            _cachedExpandedItems.Clear();
            var loadedIds = new HashSet<int>();
            foreach (GameplayTagNode tagItem in _tagItems)
            {
                LoadTagNodeItemExpansion(tagItem, loadedIds);
            }

            FilterTagTree();
        }

        /// <remarks>UE: SGameplayTagPicker::FilterTagTree</remarks>
        private void FilterTagTree()
        {
            _tagTreeWidget.Reload();

            var expandedIds = new HashSet<int>();
            foreach (GameplayTagNode tagItem in _tagItems)
            {
                if (string.IsNullOrEmpty(_filterString))
                {
                    CollectDefaultTagNodeItemExpansion(tagItem, expandedIds);
                }
                else if (FilterChildrenCheck(tagItem))
                {
                    CollectAllIds(tagItem, expandedIds);
                }
            }

            SetExpandedIds(expandedIds, false);
        }

        private void ScrollToTag(string tagName)
        {
            GameplayTagNode node = FindNode(tagName);
            if (node == null)
            {
                return;
            }

            // 검색에 걸러져 트리에 없는 태그는 선택할 행이 없다
            int id = _tagTreeWidget.GetId(node);
            if (_tagTreeWidget.FindNode(id) == null)
            {
                return;
            }

            // 부모를 먼저 펼쳐야 행이 생긴다. 이 펼침은 사용자 펼침처럼 저장된다.
            for (GameplayTagNode parentNode = node.GetParentTagNode(); parentNode != null; parentNode = parentNode.GetParentTagNode())
            {
                _tagTreeWidget.SetExpanded(_tagTreeWidget.GetId(parentNode), true);
            }

            _tagTreeWidget.SetSelection(new List<int> { id }, TreeViewSelectionOptions.RevealAndFrame);
            RepaintHost();
        }

        private void DrawMenuItems()
        {
            GUILayout.Label(_multiSelect ? "GameplayTag Container" : "GameplayTag", EditorStyles.miniBoldLabel);

            if (MenuItemButton("Clear Selection"))
            {
                OnClearAllClicked();
            }

            if (MenuItemButton("Manage Gameplay Tags..."))
            {
                OnManageTagsClicked(null);
                GUIUtility.ExitGUI();
            }

            Rect separatorRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(separatorRect, _separatorColor);
            GUILayout.Space(2f);
        }

        private void DrawTreeControls()
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent addContent = _newTagWidgetVisible ? CloseSectionContent : AddNewTagContent;
            if (GUILayout.Button(addContent, EditorStyles.iconButton, GUILayout.Width(IconButtonWidth)))
            {
                OnAddButtonClicked();
                GUIUtility.ExitGUI();
            }

            Rect searchRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            string filterString = _searchTagBox.OnToolbarGUI(searchRect, _filterString);
            DrawHint(searchRect, filterString, _searchTagBox.HasFocus(), "Search Gameplay Tags", SearchHintIndent);
            if (filterString != _filterString)
            {
                OnFilterTextChanged(filterString);
            }

            Rect settingsRect = GUILayoutUtility.GetRect(SettingsContent, EditorStyles.iconButton, GUILayout.Width(IconButtonWidth));
            if (EditorGUI.DropdownButton(settingsRect, SettingsContent, FocusType.Passive, EditorStyles.iconButton))
            {
                GenericMenu settingsMenu = MakeSettingsMenu();
                settingsMenu.DropDown(settingsRect);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <remarks>UE: SAddNewGameplayTagWidget</remarks>
        private void DrawAddNewTagWidget()
        {
            Event currentEvent = Event.current;
            bool isSubmitKey = currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);
            bool submit = isSubmitKey && GUI.GetNameOfFocusedControl() == NewTagNameControlName;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(NewTagNameLabelWidth));
            Rect nameRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField, GUILayout.ExpandWidth(true));
            GUI.SetNextControlName(NewTagNameControlName);
            _newTagName = EditorGUI.TextField(nameRect, _newTagName);
            DrawHint(nameRect, _newTagName, false, "X.Y.Z", 0f);
            EditorGUILayout.EndHorizontal();

            if (_focusNewTagName)
            {
                _focusNewTagName = false;
                EditorGUI.FocusTextInControl(NewTagNameControlName);
            }

            if (GUILayout.Button("Add New Tag") || submit)
            {
                CreateNewGameplayTag();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawRowContent(Rect rect, GameplayTagNode node)
        {
            string tagName = node.GetCompleteTagString();
            InternedName simpleTagName = node.GetSimpleTagName();
            List<string> sourceNames = GetSourceNames(node);
            bool isExplicitTag = GameplayTagRegistryEditing.IsDictionaryTag(tagName);
            string tooltip = $"{tagName}\n({(isExplicitTag ? string.Empty : "Implicit ")}{string.Join(", ", sourceNames)})";
            var content = new GUIContent(simpleTagName.ToString(), tooltip);

            if (_gameplayTagPickerMode == GameplayTagPickerMode.SelectionMode)
            {
                CheckBoxState checkState = IsTagChecked(node);
                EditorGUI.showMixedValue = checkState == CheckBoxState.Undetermined;
                EditorGUI.BeginChangeCheck();
                bool isChecked = EditorGUI.ToggleLeft(rect, content, checkState == CheckBoxState.Checked);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    OnTagCheckStatusChanged(isChecked, node);
                }

                return;
            }

            float sourceWidth = rect.width * SourceColumnRatio;
            var labelRect = new Rect(rect.x, rect.y, rect.width - sourceWidth, rect.height);
            var sourceRect = new Rect(labelRect.xMax, rect.y, sourceWidth, rect.height);
            EditorGUI.LabelField(labelRect, content);

            Color previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * (isExplicitTag ? ExplicitSourceAlpha : ImplicitSourceAlpha));
            EditorGUI.LabelField(sourceRect, new GUIContent(string.Join(", ", sourceNames), tooltip), SourceStyle);
            GUI.color = previousColor;
        }

        private static bool MenuItemButton(string text)
        {
            var content = new GUIContent(text);
            Rect rect = GUILayoutUtility.GetRect(content, MenuItemStyle, GUILayout.ExpandWidth(true));
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.Repaint && rect.Contains(currentEvent.mousePosition))
            {
                EditorGUI.DrawRect(rect, _menuItemHoverColor);
            }

            return GUI.Button(rect, content, MenuItemStyle);
        }

        private static void DrawHint(Rect rect, string text, bool hasFocus, string hint, float indent)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.Repaint || !string.IsNullOrEmpty(text) || hasFocus)
            {
                return;
            }

            var hintRect = new Rect(rect.x + indent + 2f, rect.y, rect.width - indent - 2f, rect.height);
            GUI.Label(hintRect, hint, HintStyle);
        }

        /// <remarks>UE: SGameplayTagPicker::MakeTagActionsMenu</remarks>
        private GenericMenu MakeTagActionsMenu(GameplayTagNode node)
        {
            string tagName = node.GetCompleteTagString();
            bool showManagement = _gameplayTagPickerMode == GameplayTagPickerMode.ManagementMode;
            var menu = new GenericMenu();

            GameplayTagEditorUtility.AddMenuItem(menu, "Add Sub Tag", CanAddNewSubTag(node), () => OnAddSubTag(tagName));
            GameplayTagEditorUtility.AddMenuItem(menu, "Duplicate Tag", CanAddNewSubTag(node), () => OnDuplicateTag(tagName));
            menu.AddSeparator(string.Empty);

            if (showManagement)
            {
                bool canModifyTag = GameplayTagRegistryEditing.CanModifyTag(node);
                GameplayTagEditorUtility.AddMenuItem(menu, "Rename Tag", canModifyTag, () => OnRenameTag(tagName));
                GameplayTagEditorUtility.AddMenuItem(menu, "Delete Tag", canModifyTag, () => OnDeleteTag(tagName));
                menu.AddSeparator(string.Empty);
            }

            if (_multiSelect)
            {
                if (IsExactTagInCollection(tagName))
                {
                    GameplayTagEditorUtility.AddMenuItem(menu, "Unselect Exact Tag", true, () => OnUnselectExactTag(tagName));
                }
                else
                {
                    GameplayTagEditorUtility.AddMenuItem(menu, "Select Exact Tag", true, () => OnSelectExactTag(tagName));
                }

                menu.AddSeparator(string.Empty);
            }

            if (!showManagement)
            {
                GameplayTagEditorUtility.AddMenuItem(menu, "Open Tag in Manager...", true, () => OnManageTagsClicked(tagName));
            }

            GameplayTagEditorUtility.AddMenuItem(menu, "Copy Name to Clipboard", true, () => OnCopyTagNameToClipboard(tagName));
            return menu;
        }

        /// <remarks>UE: SGameplayTagPicker::MakeSettingsMenu</remarks>
        private GenericMenu MakeSettingsMenu()
        {
            var menu = new GenericMenu();
            GameplayTagEditorUtility.AddMenuItem(menu, "Expand All", true, OnExpandAllClicked);
            GameplayTagEditorUtility.AddMenuItem(menu, "Collapse All", true, OnCollapseAllClicked);
            return menu;
        }

        private void OnTagCheckStatusChanged(bool isChecked, GameplayTagNode nodeChanged)
        {
            if (isChecked)
            {
                OnTagChecked(nodeChanged);
            }
            else
            {
                OnTagUnchecked(nodeChanged);
            }
        }

        /// <summary>키보드로 행을 토글한다. 미정 상태는 건드리지 않는다.</summary>
        /// <remarks>UE: SGameplayTagPicker::OnTreeKeyDown</remarks>
        private bool ToggleTag(GameplayTagNode node)
        {
            if (!CanSelectTags())
            {
                return false;
            }

            CheckBoxState state = IsTagChecked(node);
            if (state == CheckBoxState.Unchecked)
            {
                OnTagChecked(node);
            }
            else if (state == CheckBoxState.Checked)
            {
                OnTagUnchecked(node);
            }

            return true;
        }

        /// <summary>태그를 더하고 그 위 부모의 명시는 뺀다(자식이 부모를 대신한다). 단일 선택이면 먼저 비운다.</summary>
        /// <remarks>UE: SGameplayTagPicker::OnTagChecked</remarks>
        private void OnTagChecked(GameplayTagNode nodeChecked)
        {
            foreach (List<string> tagContainer in _tagContainers)
            {
                bool removeParents = false;
                for (GameplayTagNode currentNode = nodeChecked; currentNode != null; currentNode = currentNode.GetParentTagNode())
                {
                    string tagName = currentNode.GetCompleteTagString();
                    if (!removeParents)
                    {
                        removeParents = true;
                        if (!_multiSelect)
                        {
                            tagContainer.Clear();
                        }

                        AddUnique(tagContainer, tagName);
                    }
                    else
                    {
                        tagContainer.Remove(tagName);
                    }
                }
            }

            OnContainersChanged("Select Gameplay Tags");
        }

        /// <summary>태그와 그 하위를 모두 뺀다. 형제가 하나도 남지 않으면 부모를 명시로 넣는다.</summary>
        /// <remarks>UE: SGameplayTagPicker::OnTagUnchecked</remarks>
        private void OnTagUnchecked(GameplayTagNode nodeUnchecked)
        {
            string tagName = nodeUnchecked.GetCompleteTagString();
            GameplayTagNode parentNode = nodeUnchecked.GetParentTagNode();

            foreach (List<string> tagContainer in _tagContainers)
            {
                tagContainer.Remove(tagName);

                if (parentNode != null)
                {
                    bool otherSiblings = false;
                    foreach (GameplayTagNode siblingNode in parentNode.GetChildTagNodes())
                    {
                        if (tagContainer.Contains(siblingNode.GetCompleteTagString()))
                        {
                            otherSiblings = true;
                            break;
                        }
                    }

                    if (!otherSiblings)
                    {
                        AddUnique(tagContainer, parentNode.GetCompleteTagString());
                    }
                }

                foreach (GameplayTagNode childNode in nodeUnchecked.GetChildTagNodes())
                {
                    UncheckChildren(childNode, tagContainer);
                }
            }

            OnContainersChanged("Remove Gameplay Tags");
        }

        /// <remarks>UE: SGameplayTagPicker::UncheckChildren</remarks>
        private static void UncheckChildren(GameplayTagNode nodeUnchecked, List<string> editableContainer)
        {
            editableContainer.Remove(nodeUnchecked.GetCompleteTagString());

            foreach (GameplayTagNode childNode in nodeUnchecked.GetChildTagNodes())
            {
                UncheckChildren(childNode, editableContainer);
            }
        }

        /// <summary>오브젝트 전부가 이 태그(또는 그 자손)를 가지면 체크, 일부면 미정.</summary>
        /// <remarks>UE: SGameplayTagPicker::IsTagChecked</remarks>
        private CheckBoxState IsTagChecked(GameplayTagNode node)
        {
            string tagName = node.GetCompleteTagString();
            int numValidAssets = 0;
            int numAssetsTagIsAppliedTo = 0;

            foreach (List<string> tagContainer in _tagContainers)
            {
                numValidAssets++;
                if (ContainerHasTag(tagContainer, tagName))
                {
                    numAssetsTagIsAppliedTo++;
                }
            }

            if (numAssetsTagIsAppliedTo == 0)
            {
                return CheckBoxState.Unchecked;
            }

            return numAssetsTagIsAppliedTo == numValidAssets ? CheckBoxState.Checked : CheckBoxState.Undetermined;
        }

        /// <remarks>UE: SGameplayTagPicker::IsExactTagInCollection</remarks>
        private bool IsExactTagInCollection(string tagName)
        {
            foreach (List<string> tagContainer in _tagContainers)
            {
                if (tagContainer.Contains(tagName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <remarks>UE: SGameplayTagPicker::CanSelectTags</remarks>
        private bool CanSelectTags()
        {
            return _gameplayTagPickerMode == GameplayTagPickerMode.SelectionMode;
        }

        /// <remarks>UE: SGameplayTagPicker::OnContainersChanged</remarks>
        private void OnContainersChanged(string undoName)
        {
            if (_propertyHandle != null)
            {
                _propertyHandle.SetPerObjectValues(undoName, _tagContainers);
            }

            _onTagChanged?.Invoke();
            RepaintHost();
        }

        // 명시 태그이거나 명시 태그의 부모면 가진 것으로 친다(컨테이너 HasTag의 부모 매칭과 같은 규칙)
        private static bool ContainerHasTag(List<string> tagContainer, string tagName)
        {
            foreach (string explicitTagName in tagContainer)
            {
                if (explicitTagName == tagName)
                {
                    return true;
                }

                if (explicitTagName.Length > tagName.Length && explicitTagName[tagName.Length] == '.' && explicitTagName.StartsWith(tagName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUnique(List<string> tagContainer, string tagName)
        {
            if (!tagContainer.Contains(tagName))
            {
                tagContainer.Add(tagName);
            }
        }

        private void OnAddButtonClicked()
        {
            if (_newTagWidgetVisible)
            {
                _newTagWidgetVisible = false;
                return;
            }

            // 선택된 행이 있으면 그 하위로, 없으면 최상위로 추가한다
            IList<int> selection = _tagTreeWidget.GetSelection();
            GameplayTagNode selectedNode = selection.Count > 0 ? _tagTreeWidget.FindNode(selection[0]) : null;
            if (selectedNode != null)
            {
                ShowInlineAddTagWidget(GameplayTagAdd.Child, selectedNode.GetCompleteTagString());
            }
            else
            {
                ShowInlineAddTagWidget(GameplayTagAdd.Root, null);
            }
        }

        /// <remarks>UE: SGameplayTagPicker::ShowInlineAddTagWidget</remarks>
        private void ShowInlineAddTagWidget(GameplayTagAdd mode, string tagName)
        {
            if (mode == GameplayTagAdd.Duplicate)
            {
                _newTagName = tagName ?? string.Empty;
            }
            else
            {
                _newTagName = string.IsNullOrEmpty(tagName) ? string.Empty : tagName + ".";
            }

            _newTagWidgetVisible = true;
            _focusNewTagName = true;
            RepaintHost();
        }

        /// <remarks>UE: SAddNewGameplayTagWidget::CreateNewGameplayTag</remarks>
        private void CreateNewGameplayTag()
        {
            string tagName = _newTagName;
            if (!GameplayTagRegistryEditing.AddNewGameplayTagToRegistry(tagName, out string message))
            {
                SetNotification(message, MessageType.Error);
                return;
            }

            SetNotification(null, MessageType.None);
            OnGameplayTagAdded(tagName);
            _newTagName = string.Empty;
        }

        /// <summary>새 태그가 보이도록 부모를 펼침 상태로 저장하고 그 행으로 스크롤한다.</summary>
        /// <remarks>UE: SGameplayTagPicker::OnGameplayTagAdded</remarks>
        private void OnGameplayTagAdded(string tagName)
        {
            GameplayTagNode tagNode = FindNode(tagName);
            for (GameplayTagNode parentTagNode = tagNode; parentTagNode != null; parentTagNode = parentTagNode.GetParentTagNode())
            {
                string parentTagName = parentTagNode.GetCompleteTagString();
                SaveExpansion(parentTagName, true);
                _cachedExpandedItems.Add(parentTagName);
            }

            RefreshTags();
            RequestScrollToView(tagName);
        }

        /// <remarks>UE: SGameplayTagPicker::OnAddSubTag</remarks>
        private void OnAddSubTag(string tagName)
        {
            ShowInlineAddTagWidget(GameplayTagAdd.Child, tagName);
        }

        /// <remarks>UE: SGameplayTagPicker::OnDuplicateTag</remarks>
        private void OnDuplicateTag(string tagName)
        {
            ShowInlineAddTagWidget(GameplayTagAdd.Duplicate, tagName);
        }

        /// <remarks>UE: SGameplayTagPicker::OnRenameTag</remarks>
        private void OnRenameTag(string tagName)
        {
            GameplayTagRenameWindow.Open(tagName, OnGameplayTagRenamed);
        }

        /// <remarks>UE: SGameplayTagPicker::OnDeleteTag</remarks>
        private void OnDeleteTag(string tagName)
        {
            GameplayTagNode node = FindNode(tagName);
            if (node == null)
            {
                return;
            }

            bool isDeleted = GameplayTagRegistryEditing.DeleteTagFromRegistry(node, out string message);
            SetNotification(message, isDeleted ? MessageType.Info : MessageType.Error);
            RepaintHost();
        }

        /// <remarks>UE: SGameplayTagPicker::CanAddNewSubTag</remarks>
        private static bool CanAddNewSubTag(GameplayTagNode node)
        {
            return node != null;
        }

        /// <remarks>UE: SGameplayTagPicker::OnSelectExactTag</remarks>
        private void OnSelectExactTag(string tagName)
        {
            foreach (List<string> tagContainer in _tagContainers)
            {
                AddUnique(tagContainer, tagName);
            }

            OnContainersChanged("Select Gameplay Tags");
        }

        /// <remarks>UE: SGameplayTagPicker::OnUnselectExactTag</remarks>
        private void OnUnselectExactTag(string tagName)
        {
            foreach (List<string> tagContainer in _tagContainers)
            {
                tagContainer.Remove(tagName);
            }

            OnContainersChanged("Select Gameplay Tags");
        }

        /// <remarks>UE: SGameplayTagPicker::OnClearAllClicked</remarks>
        private void OnClearAllClicked()
        {
            foreach (List<string> tagContainer in _tagContainers)
            {
                tagContainer.Clear();
            }

            OnContainersChanged("Remove All Gameplay Tags");
        }

        /// <remarks>UE: SGameplayTagPicker::OnManageTagsClicked</remarks>
        private void OnManageTagsClicked(string highlightedTagName)
        {
            GameplayTagManagerWindow.Open(_rootFilterString, highlightedTagName);
        }

        /// <remarks>UE: SGameplayTagPicker::OnCopyTagNameToClipboard</remarks>
        private static void OnCopyTagNameToClipboard(string tagName)
        {
            EditorGUIUtility.systemCopyBuffer = tagName;
        }

        /// <remarks>UE: SGameplayTagPicker::OnExpandAllClicked</remarks>
        private void OnExpandAllClicked()
        {
            var expandedIds = new HashSet<int>();
            foreach (GameplayTagNode tagItem in _tagItems)
            {
                CollectAllIds(tagItem, expandedIds);
            }

            SetExpandedIds(expandedIds, true);
            RepaintHost();
        }

        /// <remarks>UE: SGameplayTagPicker::OnCollapseAllClicked</remarks>
        private void OnCollapseAllClicked()
        {
            SetExpandedIds(new HashSet<int>(), true);
            RepaintHost();
        }

        /// <summary>저장된 펼침 상태를 읽어 캐시를 채우고 펼칠 노드를 모은다. 루트가 하나뿐이면 항상, 저장값이 없으면 체크된 노드를 편다.</summary>
        /// <remarks>UE: SGameplayTagPicker::LoadTagNodeItemExpansion</remarks>
        private void LoadTagNodeItemExpansion(GameplayTagNode node, HashSet<int> expandedIds)
        {
            string tagName = node.GetCompleteTagString();
            int id = _tagTreeWidget.GetId(node);

            if (IsNodeSingleRoot(node))
            {
                expandedIds.Add(id);
            }
            else if (TryLoadExpansion(tagName, out bool isExpanded))
            {
                if (isExpanded)
                {
                    expandedIds.Add(id);
                    _cachedExpandedItems.Add(tagName);
                }
            }
            else if (IsTagChecked(node) == CheckBoxState.Checked)
            {
                expandedIds.Add(id);
            }

            foreach (GameplayTagNode childNode in node.GetChildTagNodes())
            {
                LoadTagNodeItemExpansion(childNode, expandedIds);
            }
        }

        /// <remarks>UE: SGameplayTagPicker::SetDefaultTagNodeItemExpansion</remarks>
        private void CollectDefaultTagNodeItemExpansion(GameplayTagNode node, HashSet<int> expandedIds)
        {
            string tagName = node.GetCompleteTagString();
            if (_cachedExpandedItems.Contains(tagName) || IsTagChecked(node) == CheckBoxState.Checked || IsNodeSingleRoot(node))
            {
                expandedIds.Add(_tagTreeWidget.GetId(node));
            }

            foreach (GameplayTagNode childNode in node.GetChildTagNodes())
            {
                CollectDefaultTagNodeItemExpansion(childNode, expandedIds);
            }
        }

        private void CollectAllIds(GameplayTagNode node, HashSet<int> ids)
        {
            ids.Add(_tagTreeWidget.GetId(node));
            foreach (GameplayTagNode childNode in node.GetChildTagNodes())
            {
                CollectAllIds(childNode, ids);
            }
        }

        private void SetExpandedIds(HashSet<int> expandedIds, bool persistExpansion)
        {
            if (persistExpansion)
            {
                PersistExpansionChanges(_lastExpandedIds, expandedIds);
            }

            _persistExpansionChange = false;
            _tagTreeWidget.SetExpanded(new List<int>(expandedIds));
            _persistExpansionChange = true;
            _lastExpandedIds = new HashSet<int>(expandedIds);
        }

        /// <remarks>UE: SGameplayTagPicker::OnExpansionChanged</remarks>
        private void OnExpansionChanged()
        {
            var currentIds = new HashSet<int>(_tagTreeWidget.GetExpanded());
            if (_persistExpansionChange)
            {
                PersistExpansionChanges(_lastExpandedIds, currentIds);
            }

            _lastExpandedIds = currentIds;
        }

        private void PersistExpansionChanges(HashSet<int> previousIds, HashSet<int> currentIds)
        {
            foreach (int id in currentIds)
            {
                if (!previousIds.Contains(id))
                {
                    PersistExpansion(id, true);
                }
            }

            foreach (int id in previousIds)
            {
                if (!currentIds.Contains(id))
                {
                    PersistExpansion(id, false);
                }
            }
        }

        private void PersistExpansion(int id, bool isExpanded)
        {
            string tagName = _tagTreeWidget.FindTagName(id);
            if (tagName == null)
            {
                return;
            }

            SaveExpansion(tagName, isExpanded);
            if (isExpanded)
            {
                _cachedExpandedItems.Add(tagName);
            }
            else
            {
                _cachedExpandedItems.Remove(tagName);
            }
        }

        private void SaveExpansion(string tagName, bool isExpanded)
        {
            EditorPrefs.SetBool(GetExpansionKey(tagName), isExpanded);
        }

        private bool TryLoadExpansion(string tagName, out bool isExpanded)
        {
            string key = GetExpansionKey(tagName);
            if (!EditorPrefs.HasKey(key))
            {
                isExpanded = false;
                return false;
            }

            isExpanded = EditorPrefs.GetBool(key);
            return true;
        }

        // EditorPrefs는 모든 프로젝트가 공유하므로 프로젝트 경로로 나눈다
        private string GetExpansionKey(string tagName)
        {
            return $"{SettingsKeyPrefix}{Application.dataPath}.{_settingsName}{tagName}.Expanded";
        }

        /// <remarks>UE: SGameplayTagPicker::IsNodeSingleRoot</remarks>
        private bool IsNodeSingleRoot(GameplayTagNode node)
        {
            return _tagItems.Count == 1 && _tagItems[0] == node;
        }

        /// <remarks>UE: SGameplayTagPicker::OnFilterTextChanged</remarks>
        private void OnFilterTextChanged(string filterText)
        {
            _filterString = filterText ?? string.Empty;
            FilterTagTree();
        }

        /// <summary>전체 이름에 검색어가 들어 있거나(대소문자 무시) 자손 중 하나라도 그러면 보인다.</summary>
        /// <remarks>UE: SGameplayTagPicker::FilterChildrenCheck</remarks>
        private bool FilterChildrenCheck(GameplayTagNode node)
        {
            if (node == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_filterString))
            {
                return true;
            }

            string tagName = node.GetCompleteTagString();
            if (tagName.IndexOf(_filterString, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            foreach (GameplayTagNode childNode in node.GetChildTagNodes())
            {
                if (FilterChildrenCheck(childNode))
                {
                    return true;
                }
            }

            return false;
        }

        private List<string> GetSourceNames(GameplayTagNode node)
        {
            string tagName = node.GetCompleteTagString();
            if (!_sourceNamesCache.TryGetValue(tagName, out List<string> sourceNames))
            {
                sourceNames = GameplayTagRegistryEditing.GetAllSourceNames(node);
                _sourceNamesCache.Add(tagName, sourceNames);
            }

            return sourceNames;
        }

        private static GameplayTagNode FindNode(string tagName)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.FindTagNode(new InternedName(tagName));
        }

        private void SetNotification(string message, MessageType messageType)
        {
            _notification = message;
            _notificationType = messageType;
        }

        private void RepaintHost()
        {
            if (HostWindow != null)
            {
                HostWindow.Repaint();
            }
        }

        /// <remarks>UE: SGameplayTagPicker::RefreshOnNextTick</remarks>
        private void RefreshOnNextTick()
        {
            _delayRefresh = true;
            RepaintHost();
        }

        /// <remarks>UE: SGameplayTagPicker::OnGameplayTagRenamed</remarks>
        private void OnGameplayTagRenamed(bool isRenamed, string message)
        {
            SetNotification(message, isRenamed ? MessageType.Info : MessageType.Error);
            RepaintHost();
        }

        /// <summary>피커의 태그 트리. 행마다 체크박스(선택 모드) 또는 이름·출처(관리 모드)와 동작 메뉴 버튼을 그린다.</summary>
        private sealed class GameplayTagTreeView : TreeView<int>
        {
            private const float ActionsButtonWidth = 16f;

            private static readonly GUIContent _actionsContent = new("▾", "More Actions...");

            private static GUIStyle _actionsButtonStyle;

            private readonly GameplayTagPicker _picker;
            // 검색으로 트리를 다시 만들어도 펼침·선택이 유지되도록 태그 이름마다 고정 ID를 준다
            private readonly Dictionary<string, int> _idByTagName = new();
            private readonly Dictionary<int, string> _tagNameById = new();
            private readonly Dictionary<int, GameplayTagNode> _nodeById = new();

            private static GUIStyle ActionsButtonStyle => _actionsButtonStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0) };

            public GameplayTagTreeView(TreeViewState<int> state, GameplayTagPicker picker) : base(state)
            {
                _picker = picker;
                showBorder = true;
            }

            protected override TreeViewItem<int> BuildRoot()
            {
                _nodeById.Clear();
                var root = new TreeViewItem<int>(0, -1, "Root");
                AddChildItems(root, _picker._tagItems, 0);

                root.children ??= new List<TreeViewItem<int>>();
                return root;
            }

            protected override bool CanMultiSelect(TreeViewItem<int> item)
            {
                return false;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = (GameplayTagTreeViewItem)args.item;
                Rect rowRect = args.rowRect;
                float contentIndent = GetContentIndent(item);
                var contentRect = new Rect(rowRect.x + contentIndent, rowRect.y, rowRect.width - contentIndent - ActionsButtonWidth, rowRect.height);
                var actionsRect = new Rect(rowRect.xMax - ActionsButtonWidth, rowRect.y, ActionsButtonWidth, rowRect.height);

                _picker.DrawRowContent(contentRect, item.Node);

                if (EditorGUI.DropdownButton(actionsRect, _actionsContent, FocusType.Passive, ActionsButtonStyle))
                {
                    GenericMenu actionsMenu = _picker.MakeTagActionsMenu(item.Node);
                    actionsMenu.DropDown(actionsRect);
                }
            }

            /// <remarks>UE: SGameplayTagPicker::OnTreeContextMenuOpening</remarks>
            protected override void ContextClickedItem(int id)
            {
                GameplayTagNode node = FindNode(id);
                if (node == null)
                {
                    return;
                }

                GenericMenu actionsMenu = _picker.MakeTagActionsMenu(node);
                actionsMenu.ShowAsContext();
            }

            /// <remarks>UE: SGameplayTagPicker::OnTreeKeyDown</remarks>
            protected override void KeyEvent()
            {
                Event currentEvent = Event.current;
                if (currentEvent.type != EventType.KeyDown)
                {
                    return;
                }

                KeyCode keyCode = currentEvent.keyCode;
                if (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter && keyCode != KeyCode.Space)
                {
                    return;
                }

                IList<int> selection = GetSelection();
                GameplayTagNode node = selection.Count > 0 ? FindNode(selection[0]) : null;
                if (node != null && _picker.ToggleTag(node))
                {
                    currentEvent.Use();
                }
            }

            protected override void ExpandedStateChanged()
            {
                _picker.OnExpansionChanged();
            }

            public int GetId(GameplayTagNode node)
            {
                string tagName = node.GetCompleteTagString();
                if (!_idByTagName.TryGetValue(tagName, out int id))
                {
                    // 0은 보이지 않는 루트 항목이 쓴다
                    id = _idByTagName.Count + 1;
                    _idByTagName.Add(tagName, id);
                    _tagNameById.Add(id, tagName);
                }

                return id;
            }

            /// <summary>지금 트리(검색 결과)에 있는 노드. 없으면 null.</summary>
            public GameplayTagNode FindNode(int id)
            {
                return _nodeById.GetValueOrDefault(id);
            }

            public string FindTagName(int id)
            {
                return _tagNameById.GetValueOrDefault(id);
            }

            private void AddChildItems(TreeViewItem<int> parentItem, IReadOnlyList<GameplayTagNode> nodes, int depth)
            {
                var sortedNodes = new List<GameplayTagNode>(nodes);
                sortedNodes.Sort(CompareSimpleTagNames);

                foreach (GameplayTagNode node in sortedNodes)
                {
                    if (!_picker.FilterChildrenCheck(node))
                    {
                        continue;
                    }

                    int id = GetId(node);
                    _nodeById[id] = node;

                    InternedName simpleTagName = node.GetSimpleTagName();
                    var item = new GameplayTagTreeViewItem(id, depth, simpleTagName.ToString(), node);
                    parentItem.AddChild(item);
                    AddChildItems(item, node.GetChildTagNodes(), depth + 1);
                }
            }

            private static int CompareSimpleTagNames(GameplayTagNode left, GameplayTagNode right)
            {
                InternedName leftName = left.GetSimpleTagName();
                InternedName rightName = right.GetSimpleTagName();
                return GameplayTagEditorUtility.CompareTagNames(leftName.ToString(), rightName.ToString());
            }
        }

        private sealed class GameplayTagTreeViewItem : TreeViewItem<int>
        {
            public GameplayTagNode Node { get; }

            public GameplayTagTreeViewItem(int id, int depth, string displayName, GameplayTagNode node) : base(id, depth, displayName)
            {
                Node = node;
            }
        }
    }
}
