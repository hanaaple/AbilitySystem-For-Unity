using System;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>
    /// ASC의 어빌리티 목록을 잠근다(<c>using</c> 블록). 목록을 도는 중 콜백(OnGiveAbility·OnRemoveAbility 등)이 부여·해제를 부르면 목록이 바뀌므로,
    /// 잠긴 동안의 부여·해제는 보류했다가 마지막 락이 풀릴 때 처리한다.
    /// </summary>
    /// <remarks>UE: FScopedAbilityListLock</remarks>
    public readonly struct ScopedAbilityListLock : IDisposable
    {
        private readonly AbilitySystemComponent _abilitySystemComponent;

        public ScopedAbilityListLock(AbilitySystemComponent abilitySystemComponent)
        {
            _abilitySystemComponent = abilitySystemComponent;
            _abilitySystemComponent.IncrementAbilityListLock();
        }

        public void Dispose()
        {
            _abilitySystemComponent.DecrementAbilityListLock();
        }
    }
}
