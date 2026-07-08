using Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using Core.ItemSystem.Equipment;
using UnityEngine;

[RequireComponent(typeof(EquipmentComponent))]
[RequireComponent(typeof(AbilitySystemComponent))]
public class PlayerCharacter : CharacterBase
{
    [SerializeField] private Transform model;
    [SerializeField] private float rotationSmoothing = 15f;
    [SerializeField] private GameplayEffect possessEffect;

    private AbilitySystemComponent _asc;
    private EquipmentComponent _equipment;
    private ActiveGameplayEffectHandle _possessEffectHandle;

    protected virtual void Awake()
    {
        _asc = GetComponent<AbilitySystemComponent>();
        _equipment = GetComponent<EquipmentComponent>();
    }

    // 평타 입력 진입점. 실제 공격 판정은 장비 시스템(장착 무기의 능력)이 수행한다.
    public void Attack()
    {
        _equipment.TriggerAttack();
    }

    public override void OnPossessed(ControllerBase controller)
    {
        base.OnPossessed(controller);

        if (possessEffect != null)
        {
            _possessEffectHandle = _asc.ApplyGameplayEffectToSelf(possessEffect);
        }
    }

    public override void OnUnPossessed()
    {
        base.OnUnPossessed();

        if (_possessEffectHandle.IsValid)
        {
            _asc.RemoveActiveGameplayEffect(_possessEffectHandle);
            _possessEffectHandle = ActiveGameplayEffectHandle.Invalid;
        }
    }

    public override void MoveDelta(Vector3 normailizedDirection, float delta)
    {
        if (normailizedDirection.sqrMagnitude > 0.01f)
        {
            float speed = _asc.GetAttributeCurrentValue(CharacterAttributeSet.Speed);
            transform.position += normailizedDirection * (speed * delta);
            Quaternion target = Quaternion.LookRotation(normailizedDirection);
            model.rotation = Quaternion.Slerp(model.rotation, target, rotationSmoothing * delta);
        }
    }

    public override void StopMoving()
    {
    }
}
