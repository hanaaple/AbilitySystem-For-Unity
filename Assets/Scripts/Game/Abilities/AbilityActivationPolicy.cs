namespace Game.Abilities
{
    /// <summary>입력에 묶인 어빌리티를 언제 발동할지.</summary>
    /// <remarks>UE: ELyraAbilityActivationPolicy</remarks>
    public enum AbilityActivationPolicy
    {
        /// <summary>입력을 누른 순간 한 번 발동을 시도한다.</summary>
        OnInputTriggered,

        /// <summary>입력이 눌려 있는 동안 비활성이면 매 프레임 다시 발동을 시도한다(연사·누르는 동안 유지).</summary>
        WhileInputActive,
    }
}
