using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>필드 값을 고르는 피커를 콤보 아래 팝업으로 띄운다. 단일 태그는 단일 선택, 컨테이너는 다중 선택.</summary>
    /// <remarks>UE: SGameplayTagCombo::OnGetMenuContent</remarks>
    internal sealed class GameplayTagPickerPopup : PopupWindowContent
    {
        private const float Width = 300f;
        private const float SingleTagMaxHeight = 350f;
        private const float ContainerMaxHeight = 400f;
        private const float MenuItemsHeight = 64f;

        private readonly GameplayTagPicker _tagPicker;
        private readonly EditorWindow _ownerWindow;
        private readonly string _tagToHilight;
        private readonly float _maxHeight;

        /// <param name="tagToHilight">열 때 펼쳐 보여 줄 태그. 없으면 null.</param>
        /// <param name="ownerWindow">값이 바뀌면 다시 그릴 창(필드를 그린 인스펙터).</param>
        public GameplayTagPickerPopup(GameplayTagPropertyHandle propertyHandle, string tagToHilight, EditorWindow ownerWindow)
        {
            _ownerWindow = ownerWindow;
            _tagToHilight = tagToHilight;
            _maxHeight = propertyHandle.IsContainer ? ContainerMaxHeight : SingleTagMaxHeight;
            _tagPicker = new GameplayTagPicker(GameplayTagPickerMode.SelectionMode, propertyHandle, propertyHandle.IsContainer, propertyHandle.Categories, string.Empty, true, RepaintOwnerWindow);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, _maxHeight + MenuItemsHeight);
        }

        /// <remarks>UE: SGameplayTagCombo::OnMenuOpenChanged</remarks>
        public override void OnOpen()
        {
            editorWindow.wantsMouseMove = true;
            _tagPicker.HostWindow = editorWindow;
            _tagPicker.FocusSearchBox();
            _tagPicker.RequestScrollToView(_tagToHilight);
        }

        public override void OnGUI(Rect rect)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            _tagPicker.OnGUI(rect);
        }

        public override void OnClose()
        {
            _tagPicker.Dispose();
        }

        private void RepaintOwnerWindow()
        {
            if (_ownerWindow != null)
            {
                _ownerWindow.Repaint();
            }
        }
    }
}
