using System;
using UnityEngine;

namespace Game.Abilities
{
    /// <summary>투사체를 쏠 수 있는 캐릭터. 투사체가 나갈 위치와 겨누는 지점을 내준다.</summary>
    public interface IProjectileCaster
    {
        /// <summary>
        /// 겨눌 준비가 되면 launch(나갈 위치, 겨누는 지점)를 부른다.
        /// 같은 프레임 안이지만 바로 부르지 않을 수 있다(카메라가 이번 프레임 위치로 옮겨진 뒤 등).
        /// </summary>
        void RequestLaunch(Action<Vector3, Vector3> launch);
    }
}
