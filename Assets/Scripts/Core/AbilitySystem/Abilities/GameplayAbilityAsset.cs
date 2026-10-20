using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티의 정의(불변 데이터). 동작과 액터별 상태는 <see cref="AbilityTemplate"/>의 <see cref="GameplayAbility"/> 하위 클래스가 맡는다.</summary>
    /// <remarks>UE: UGameplayAbility</remarks>
    [CreateAssetMenu(menuName = "Ability System/Gameplay Ability", fileName = "GA_New")]
    public sealed class GameplayAbilityAsset : ScriptableObject
    {
        // 런타임 클래스마다 필드가 달라 SerializeReference로 담는다. 부여 때마다 이 인스턴스를 원형으로 복제하므로 직접 실행하지 않는다.
        [SerializeReference] [SubclassSelector(typeof(GameplayAbility))] private GameplayAbility abilityTemplate;

        /// <summary>부여 때 복제되는 원형. 비었으면 null.</summary>
        /// <remarks>UE: UGameplayAbility CDO</remarks>
        public GameplayAbility AbilityTemplate => abilityTemplate;
    }
}
