using UnityEngine;

public abstract class ControllerBase : MonoBehaviour
{
    protected CharacterBase _possessedCharacter;

    public CharacterBase PossessedCharacter => _possessedCharacter;

    // TODO: 멀티플레이 확장 시 구현
    protected bool HasAuthority()
    {
        return true;
    }

    public virtual void Possess(CharacterBase character)
    {
        if (_possessedCharacter != null)
        {
            UnPossess();
        }

        _possessedCharacter = character;
        _possessedCharacter.OnDeath += OnPossessedCharacterDied;
        _possessedCharacter.OnPossessed(this);
    }

    public virtual void UnPossess()
    {
        if (_possessedCharacter == null)
        {
            return;
        }

        _possessedCharacter.OnDeath -= OnPossessedCharacterDied;
        _possessedCharacter.OnUnPossessed();
        _possessedCharacter = null;
    }

    protected virtual void OnPossessedCharacterDied(CharacterBase character)
    {
        UnPossess();
    }
}