namespace Core.AbilitySystem.Tag
{
    /// <remarks>UE: EGameplayTagQueryStreamVersion::Type</remarks>
    public enum GameplayTagQueryStreamVersion
    {
        /// <summary>[식 유형][개수][태그 인덱스… | 하위 식…]. 버전 필드가 생기기 전에 저장된 질의도 이 값(0)으로 읽힌다.</summary>
        InitialVersion = 0,

        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1,
    }
}
