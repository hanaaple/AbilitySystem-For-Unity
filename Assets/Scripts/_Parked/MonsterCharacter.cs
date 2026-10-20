using UnityEngine;
using UnityEngine.AI;

public class MonsterCharacter : CharacterBase
{
    private NavMeshAgent _agent;

    protected void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void Chase(Vector3 worldPosition)
    {
        _agent.SetDestination(worldPosition);
    }

    public override void MoveDelta(Vector3 normailizedDirection, float delta)
    {

    }

    public override void StopMoving()
    {
        _agent.ResetPath();
    }
}
