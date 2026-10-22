using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Game.Settings
{
    /// <summary> UI/Cancel(Esc)로 여닫는 설정 창. 열린 동안 게임을 멈추고 커서를 보인다. 닫혀 있으면 커서를 숨기고 잠근다. UI/ToggleFullscreen(F11)으로 전체 화면을 켜고 끈다. </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsMenu : MonoBehaviour
    {
        /// <summary> 창이 열리고 닫힐 때 알린다(true = 열림). </summary>
        public static event Action<bool> OnOpenChanged;

        private PlayerInputActions _inputActions;
        private VisualElement _panel;
        private Slider _sensitivitySlider;
        private bool _isOpen;
        private Vector2Int _windowedSize;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            BuildLayout(GetComponent<UIDocument>().rootVisualElement);
            SetOpen(false);

            _inputActions.UI.Cancel.performed += OnCancelInput;
            _inputActions.UI.ToggleFullscreen.performed += OnToggleFullscreenInput;
            _inputActions.UI.Enable();
        }

        private void OnDisable()
        {
            _inputActions.UI.Cancel.performed -= OnCancelInput;
            _inputActions.UI.ToggleFullscreen.performed -= OnToggleFullscreenInput;
            _inputActions.UI.Disable();

            if (_isOpen)
            {
                SetOpen(false);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 에디터는 Esc를 누르면 커서 잠금을 스스로 푼다. 창을 닫는 Esc와 겹치면 풀린 채 남으므로 닫혀 있는 동안 다시 건다.
        private void Update()
        {
            if (!_isOpen && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // 포커스를 잃으면 마지막 마우스 이동값이 남아 카메라가 계속 돌 수 있으므로 창을 열어 멈춘다
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !_isOpen)
            {
                SetOpen(true);
            }
        }

        private void BuildLayout(VisualElement root)
        {
            root.Clear();
            root.style.flexGrow = 1;
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            _panel = new VisualElement();
            _panel.style.width = 560;
            _panel.style.paddingLeft = _panel.style.paddingRight = 28;
            _panel.style.paddingTop = _panel.style.paddingBottom = 24;
            _panel.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.92f);
            _panel.style.borderTopLeftRadius = _panel.style.borderTopRightRadius = 6;
            _panel.style.borderBottomLeftRadius = _panel.style.borderBottomRightRadius = 6;
            root.Add(_panel);

            var title = new Label("Settings");
            title.style.fontSize = 32;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 20;
            _panel.Add(title);

            _sensitivitySlider = new Slider("Mouse Sensitivity", GameSettings.MinMouseSensitivity, GameSettings.MaxMouseSensitivity);
            _sensitivitySlider.showInputField = true;
            _sensitivitySlider.style.fontSize = 20;
            _sensitivitySlider.labelElement.style.color = Color.white;
            _sensitivitySlider.labelElement.style.minWidth = 200;
            _sensitivitySlider.RegisterValueChangedCallback(OnSensitivityChanged);
            _panel.Add(_sensitivitySlider);

            var closeButton = new Button(() => SetOpen(false)) { text = "Close" };
            closeButton.style.marginTop = 20;
            closeButton.style.height = 40;
            closeButton.style.fontSize = 20;
            _panel.Add(closeButton);
        }

        private void SetOpen(bool isOpen)
        {
            _isOpen = isOpen;
            _panel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

            if (isOpen)
            {
                _sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            }

            Time.timeScale = isOpen ? 0f : 1f;
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;
            OnOpenChanged?.Invoke(isOpen);
        }

        private void OnCancelInput(InputAction.CallbackContext ctx)
        {
            SetOpen(!_isOpen);
        }

        // 전체 화면은 모니터 해상도의 테두리 없는 창으로, 돌아올 땐 켜기 전 창 크기로 되돌린다. 에디터 Game 뷰에선 바뀌지 않는다(빌드에서만).
        private void OnToggleFullscreenInput(InputAction.CallbackContext ctx)
        {
            if (Screen.fullScreen)
            {
                Vector2Int size = _windowedSize.x > 0 ? _windowedSize : new Vector2Int(Screen.width, Screen.height);
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                return;
            }

            _windowedSize = new Vector2Int(Screen.width, Screen.height);
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
        }

        private void OnSensitivityChanged(ChangeEvent<float> evt)
        {
            GameSettings.MouseSensitivity = evt.newValue;
        }
    }
}
