using System.Collections.Generic;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>ASC에 부여된 어빌리티(<see cref="GameplayAbilitySpec"/>) 목록과 그 목록을 가진 ASC. 부여·해제 절차는 ASC가 <see cref="Items"/>를 직접 다루며 수행한다.</summary>
    /// <remarks>UE: FGameplayAbilitySpecContainer</remarks>
    public class GameplayAbilitySpecContainer
    {
        /// <summary>부여된 어빌리티 목록.</summary>
        /// <remarks>UE: FGameplayAbilitySpecContainer::Items</remarks>
        public List<GameplayAbilitySpec> Items { get; } = new();

        /// <summary>이 목록을 가진 ASC.</summary>
        /// <remarks>UE: FGameplayAbilitySpecContainer::Owner</remarks>
        public AbilitySystemComponent Owner { get; private set; }

        /// <remarks>UE: FGameplayAbilitySpecContainer::RegisterWithOwner</remarks>
        public void RegisterWithOwner(AbilitySystemComponent owner)
        {
            Owner = owner;
        }
    }
}
