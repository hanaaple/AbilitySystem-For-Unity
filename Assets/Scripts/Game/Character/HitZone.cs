using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.Character
{
    /// <summary>캐릭터의 맞는 부위(머리 등). 부위 콜라이더와 같은 오브젝트에 두면, 피해 계산이 맞은 콜라이더의 이 태그로 부위를 가린다.</summary>
    /// <remarks>
    /// 부위는 이동용 몸 콜라이더 안에 들어 있어 트리거로 둔다 — 이동·물리 충돌에 끼지 않고, 투사체가 몸에 닿은 뒤 따로 찾는다.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class HitZone : MonoBehaviour
    {
        [SerializeField] private GameplayTag zoneTag;

        public GameplayTag ZoneTag => zoneTag;
    }
}
