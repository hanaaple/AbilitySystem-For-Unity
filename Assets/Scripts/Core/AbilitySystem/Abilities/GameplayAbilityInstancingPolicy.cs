namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티 런타임 인스턴스를 언제 만드는가.</summary>
    /// <remarks>UE: EGameplayAbilityInstancingPolicy</remarks>
    public enum GameplayAbilityInstancingPolicy
    {
        /// <summary>부여 때 액터(ASC)마다 하나를 만들어 발동마다 재사용한다. 발동 사이에 상태가 남는다.</summary>
        InstancedPerActor,

        /// <summary>발동마다 새로 만들고 끝나면 버린다. 동시에 여러 개가 활성일 수 있다.</summary>
        InstancedPerExecution,
    }
}
