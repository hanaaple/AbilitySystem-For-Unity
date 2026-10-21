using System;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티 조회 시 스코프 락으로 보류된 부여·해제를 볼지.</summary>
    /// <remarks>UE: EConsiderPending</remarks>
    [Flags]
    public enum ConsiderPending
    {
        None = 0,

        /// <summary>보류된 부여(아직 목록에 없음)도 찾는다.</summary>
        PendingAdd = 1 << 0,

        /// <summary>해제가 보류된(목록엔 아직 있음) Spec도 찾는다.</summary>
        PendingRemove = 1 << 1,

        All = PendingAdd | PendingRemove
    }
}
