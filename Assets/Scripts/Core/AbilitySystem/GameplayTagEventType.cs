namespace Core.AbilitySystem
{
    /// <summary>태그 개수 이벤트를 언제 받을지.</summary>
    /// <remarks>UE: EGameplayTagEventType::Type</remarks>
    public enum GameplayTagEventType
    {
        /// <summary>태그가 새로 생기거나(0→양수) 완전히 없어질 때(→0)만.</summary>
        NewOrRemoved,

        /// <summary>개수가 바뀔 때마다.</summary>
        AnyCountChange
    }
}
