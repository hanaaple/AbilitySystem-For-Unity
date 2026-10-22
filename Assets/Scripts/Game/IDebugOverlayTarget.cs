using System.Collections.Generic;
using Core.AbilitySystem;
using UnityEngine;

namespace Game
{
    /// <summary>디버그 오버레이에서 겨눴을 때 보일 대상. ASC가 있으면 오버레이가 GAS 상태를 그리고, 그 밖의 게임 정보는 대상이 줄로 덧붙인다.</summary>
    public interface IDebugOverlayTarget
    {
        string DebugName { get; }

        /// <summary>패널 제목 옆에 보일 미리 찍어 둔 썸네일. 없으면 null.</summary>
        Texture2D DebugThumbnail { get; }

        /// <summary>GAS 상태를 보일 ASC. 없는 대상(구간 등)이면 null.</summary>
        AbilitySystemComponent DebugAbilitySystem { get; }

        /// <summary>GAS 밖의 정보(구간 안의 캐릭터, 재생성 상태 등)를 한 줄씩 더한다.</summary>
        void AppendDebugLines(List<string> lines);
    }
}
