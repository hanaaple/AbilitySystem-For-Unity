using Game.Abilities;
using Game.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class PlayerController : ControllerBase
    {
        private PlayerInputActions _playerInputActions;
        private PlayerCharacter _playerCharacter;
        private Camera _camera;
        private Vector2 _moveInput;

        // 어빌리티를 발동하는 입력과 그 입력 ID. 누르고 뗄 때 모두 ASC에 알린다.
        private (InputAction Action, AbilityInputID InputID)[] _abilityInputs;

        private void Awake()
        {
            _playerInputActions = new PlayerInputActions();
            _camera = Camera.main;
            _abilityInputs = new[]
            {
                (_playerInputActions.Player.Attack, AbilityInputID.NormalAttack),
                (_playerInputActions.Player.Ability1, AbilityInputID.Ability1),
                (_playerInputActions.Player.Ability2, AbilityInputID.Ability2),
                (_playerInputActions.Player.Jump, AbilityInputID.Jump),
                (_playerInputActions.Player.Sprint, AbilityInputID.Sprint),
                (_playerInputActions.Player.Aim, AbilityInputID.Aim),
            };
        }

        private void OnEnable()
        {
            _playerInputActions.Player.Enable();
            SettingsMenu.OnOpenChanged += SetInputBlocked;
        }

        private void OnDisable()
        {
            SettingsMenu.OnOpenChanged -= SetInputBlocked;
            _playerInputActions.Player.Disable();
        }

        private void Update()
        {
            if (_playerCharacter == null)
            {
                return;
            }

            HandleMovement();
        }

        private void HandleMovement()
        {
            float cameraYaw = _camera != null ? _camera.transform.eulerAngles.y : 0f;
            _playerCharacter.SetViewYaw(cameraYaw);

            if (_moveInput == Vector2.zero)
            {
                _playerCharacter.StopMoving();
                return;
            }

            // 입력은 카메라가 보는 방향 기준
            Vector3 inputDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            Vector3 moveDir = Quaternion.Euler(0f, cameraYaw, 0f) * inputDirection;
            _playerCharacter.MoveDelta(moveDir, Time.deltaTime);
        }

        public override void Possess(CharacterBase character)
        {
            base.Possess(character);

            if (character is not PlayerCharacter playableCharacter)
            {
                Debug.LogError($"Expected PlayerCharacter, got {character?.GetType().Name}");
                return;
            }

            _playerCharacter = playableCharacter;
            _playerInputActions.Player.Move.performed += OnMoveInput;
            _playerInputActions.Player.Move.canceled += OnMoveInput;
            foreach ((InputAction action, AbilityInputID _) in _abilityInputs)
            {
                action.performed += OnAbilityInput;
                action.canceled += OnAbilityInput;
            }
        }

        public override void UnPossess()
        {
            if (_playerCharacter != null)
            {
                _playerInputActions.Player.Move.performed -= OnMoveInput;
                _playerInputActions.Player.Move.canceled -= OnMoveInput;
                foreach ((InputAction action, AbilityInputID inputID) in _abilityInputs)
                {
                    action.performed -= OnAbilityInput;
                    action.canceled -= OnAbilityInput;

                    // 누른 채로 빙의가 풀리면 뗌이 오지 않아 달리기처럼 누르는 동안 유지하는 어빌리티가 남는다
                    _playerCharacter.AbilityInputReleased(inputID);
                }

                _playerCharacter = null;
            }

            base.UnPossess();
        }

        // 맵을 끄면 눌려 있던 입력에 canceled가 와서 누르는 동안 유지하는 어빌리티·이동도 함께 풀린다
        private void SetInputBlocked(bool isBlocked)
        {
            if (isBlocked)
            {
                _playerInputActions.Player.Disable();
            }
            else
            {
                _playerInputActions.Player.Enable();
            }
        }

        private void OnMoveInput(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        private void OnAbilityInput(InputAction.CallbackContext ctx)
        {
            foreach ((InputAction action, AbilityInputID inputID) in _abilityInputs)
            {
                if (action != ctx.action)
                {
                    continue;
                }

                if (ctx.performed)
                {
                    _playerCharacter.AbilityInputPressed(inputID);
                }
                else
                {
                    _playerCharacter.AbilityInputReleased(inputID);
                }

                return;
            }
        }
    }
}
