using UnityEngine;

public class PlayerCharacter : CharacterBase
{
    [SerializeField] private Transform model;

    [SerializeField] private float rotationSmoothing = 15f;
    [SerializeField] private float movementSpeed = 5f;

    public override void MoveDelta(Vector3 normailizedDirection, float delta)
    {
        if (normailizedDirection.sqrMagnitude > 0.01f)
        {
            transform.position += normailizedDirection * (movementSpeed * delta);
            Quaternion target = Quaternion.LookRotation(normailizedDirection);
            model.rotation = Quaternion.Slerp(model.rotation, target, rotationSmoothing * delta);
        }
    }

    public override void StopMoving()
    {
    }
}
