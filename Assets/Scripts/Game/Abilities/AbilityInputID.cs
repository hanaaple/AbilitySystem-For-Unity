namespace Game.Abilities
{
    /// <summary>플레이어 입력과 어빌리티 Spec을 잇는 입력 ID. 부여할 때 Spec의 InputID로 넣고, 입력이 오면 ASC에 같은 ID로 알린다.</summary>
    public enum AbilityInputID
    {
        NormalAttack,
        Ability2,
        Jump,
        Sprint,
        Aim,

        // 값이 프리팹(스킬 바 칸)에 저장되므로 새 ID는 끝에 붙인다
        Ability1,
    }
}
