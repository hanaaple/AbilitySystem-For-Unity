namespace Core.AbilitySystem.Tag
{
    /// <summary><see cref="GameplayTagQueryExpression"/>의 식 유형.</summary>
    public enum GameplayTagQueryExprType
    {
        Undefined = 0,
        AnyTagsMatch,
        AllTagsMatch,
        NoTagsMatch,
        AnyExprMatch,
        AllExprMatch,
        NoExprMatch,
        AnyTagsExactMatch,
        AllTagsExactMatch,
    }
}
