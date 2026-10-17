using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>태그 정의를 관리하는 창. 피커를 관리 모드로 담아 추가·이름 변경·삭제를 한다.</summary>
    internal sealed class GameplayTagManagerWindow : EditorWindow
    {
        private const string WindowTitle = "Gameplay Tag Manager";
        private const string SettingsName = "Manager";

        private static readonly Vector2 _defaultWindowSize = new(800f, 800f);

        [SerializeField] private string _filter = string.Empty;

        private GameplayTagPicker _tagWidget;

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            CreateTagWidget();
        }

        private void OnDisable()
        {
            _tagWidget?.Dispose();
            _tagWidget = null;
        }

        private void OnGUI()
        {
            _tagWidget.OnGUI(new Rect(0f, 0f, position.width, position.height));
        }

        /// <summary>관리 창을 열고 검색창에 포커스를 준다. 이미 열려 있으면 그 창을 그대로 쓴다(필터도 유지).</summary>
        /// <param name="filter">새로 열 때 루트를 제한하는 필터.</param>
        /// <param name="highlightedTagName">펼쳐 보여 줄 태그. 없으면 null.</param>
        /// <remarks>UE: UE::GameplayTags::Editor::OpenGameplayTagManager</remarks>
        public static void Open(string filter, string highlightedTagName)
        {
            bool isAlreadyOpen = HasOpenInstances<GameplayTagManagerWindow>();
            var window = GetWindow<GameplayTagManagerWindow>(WindowTitle, true);
            if (!isAlreadyOpen)
            {
                window.Initialize(filter);
            }

            window.Highlight(highlightedTagName);
        }

        private void Initialize(string filter)
        {
            _filter = filter ?? string.Empty;
            Rect windowPosition = position;
            position = new Rect(windowPosition.x, windowPosition.y, _defaultWindowSize.x, _defaultWindowSize.y);
            CreateTagWidget();
        }

        private void Highlight(string highlightedTagName)
        {
            _tagWidget.FocusSearchBox();
            _tagWidget.RequestScrollToView(highlightedTagName);
            Repaint();
        }

        private void CreateTagWidget()
        {
            _tagWidget?.Dispose();
            _tagWidget = new GameplayTagPicker(GameplayTagPickerMode.ManagementMode, null, false, _filter, SettingsName, false, null);
            _tagWidget.HostWindow = this;
        }
    }
}
