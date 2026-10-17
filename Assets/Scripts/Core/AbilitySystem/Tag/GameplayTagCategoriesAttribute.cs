using System;

namespace Core.AbilitySystem.Tag
{
    /// <summary>
    /// <see cref="GameplayTag"/>·<see cref="GameplayTagContainer"/> 필드의 인스펙터 피커에 보일 태그를 제한한다.
    /// 쉼표로 나눈 태그 경로마다 그 하위 트리만 보이고(<c>"Status,Damage.Fire"</c>), 일치하는 경로가 하나도 없으면 전체 트리를 보인다. List·배열 필드에 붙이면 모든 요소에 적용된다.
    /// </summary>
    /// <example><code>[GameplayTagCategories("Status.Debuff")] [SerializeField] private GameplayTag _debuff;</code></example>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GameplayTagCategoriesAttribute : System.Attribute
    {
        public string Categories { get; }

        public GameplayTagCategoriesAttribute(string categories)
        {
            Categories = categories;
        }
    }
}
