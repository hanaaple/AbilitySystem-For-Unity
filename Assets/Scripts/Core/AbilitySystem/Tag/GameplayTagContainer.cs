using System.Collections.Generic;

namespace Core.AbilitySystem.Tag
{
    /// <summary>여러 <see cref="GameplayTag"/>를 담는 컨테이너. 소유 태그 집합·다중 매칭 질의의 단위.</summary>
    public struct GameplayTagContainer
    {
        public List<GameplayTag> GameplayTags;
        public List<GameplayTag> ParentTags;
    }
}
