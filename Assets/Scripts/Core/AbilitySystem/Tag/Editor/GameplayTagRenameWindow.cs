using System;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>레지스트리 태그의 이름을 바꾸는 대화창.</summary>
    /// <remarks>UE: SRenameGameplayTagDialog</remarks>
    internal sealed class GameplayTagRenameWindow : EditorWindow
    {
        private const string NewTagNameControlName = "GameplayTagRenameWindow.NewTagName";
        private const float ButtonWidth = 80f;

        private static readonly Vector2 _windowSize = new(420f, 84f);

        private string _tagToRename;
        private string _newTagName;
        private Action<bool, string> _onGameplayTagRenamed;
        private bool _focusNewTagName = true;

        private void OnGUI()
        {
            Event currentEvent = Event.current;
            bool isSubmitKey = currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);
            bool submit = isSubmitKey && GUI.GetNameOfFocusedControl() == NewTagNameControlName;
            bool cancel = currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape;

            EditorGUILayout.LabelField("Current Tag:", _tagToRename);
            GUI.SetNextControlName(NewTagNameControlName);
            _newTagName = EditorGUILayout.TextField("New Tag:", _newTagName);

            if (_focusNewTagName)
            {
                _focusNewTagName = false;
                EditorGUI.FocusTextInControl(NewTagNameControlName);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!IsRenameEnabled()))
            {
                if (GUILayout.Button("Rename", GUILayout.Width(ButtonWidth)))
                {
                    submit = true;
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(ButtonWidth)))
            {
                cancel = true;
            }

            EditorGUILayout.EndHorizontal();

            if (submit && IsRenameEnabled())
            {
                RenameAndClose();
                GUIUtility.ExitGUI();
            }
            else if (cancel)
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }

        /// <summary>대화창을 띄우고 닫힐 때까지 기다린다.</summary>
        /// <param name="onGameplayTagRenamed">이름을 바꾸려 시도한 뒤 (성공 여부, 결과 알림)으로 호출된다. 취소하면 호출되지 않는다.</param>
        public static void Open(string tagToRename, Action<bool, string> onGameplayTagRenamed)
        {
            var window = CreateInstance<GameplayTagRenameWindow>();
            window.titleContent = new GUIContent("Rename Gameplay Tag");
            window._tagToRename = tagToRename;
            window._newTagName = tagToRename;
            window._onGameplayTagRenamed = onGameplayTagRenamed;
            window.minSize = _windowSize;
            window.maxSize = _windowSize;
            window.ShowModalUtility();
        }

        /// <remarks>UE: SRenameGameplayTagDialog::IsRenameEnabled</remarks>
        private bool IsRenameEnabled()
        {
            return !string.IsNullOrEmpty(_newTagName) && _newTagName != _tagToRename;
        }

        /// <remarks>UE: SRenameGameplayTagDialog::RenameAndClose</remarks>
        private void RenameAndClose()
        {
            bool isRenamed = GameplayTagRegistryEditing.RenameTagInRegistry(_tagToRename, _newTagName, out string message);
            _onGameplayTagRenamed?.Invoke(isRenamed, message);
            Close();
        }
    }
}
