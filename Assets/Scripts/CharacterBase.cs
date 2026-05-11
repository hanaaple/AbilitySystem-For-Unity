using System;
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour
{
    protected ControllerBase _controller;

    public WeaponInstance CurrentWeapon { get; private set; }

    public bool IsAlive { get; private set; } = true;

    public event Action<CharacterBase> OnDeath;

    public abstract void MoveDelta(Vector3 normailizedDirection, float delta);
    public abstract void StopMoving();

    // TODO: StatModifier 시스템 도입 시 HP 계산 구현
    public virtual void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            return;
        }
    }

    protected virtual void Die()
    {
        if (!IsAlive)
        {
            return;
        }

        IsAlive = false;
        OnDeath?.Invoke(this);
    }

    public virtual void OnPossessed(ControllerBase controller)
    {
        _controller = controller;
    }

    public virtual void OnUnPossessed()
    {
        _controller = null;
    }
}
