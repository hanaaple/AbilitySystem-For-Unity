using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : ControllerBase
{
    private PlayerInputActions _playerInputActions;
    private PlayerCharacter _playerCharacter;
    private Vector2 _moveInput;

    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
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
        if (_moveInput == Vector2.zero)
        {
            _playerCharacter.StopMoving();
            return;
        }

        Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
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
    }

    public override void UnPossess()
    {
        if (_playerCharacter != null)
        {
            _playerInputActions.Player.Move.performed -= OnMoveInput;
            _playerInputActions.Player.Move.canceled -= OnMoveInput;
            _playerCharacter = null;
        }

        base.UnPossess();
    }

    private void OnMoveInput(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }
}
