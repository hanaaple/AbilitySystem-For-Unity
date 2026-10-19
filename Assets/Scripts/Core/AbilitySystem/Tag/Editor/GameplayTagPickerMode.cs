namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>피커가 어디서 쓰이느냐에 따른 동작.</summary>
    /// <remarks>UE: EGameplayTagPickerMode</remarks>
    internal enum GameplayTagPickerMode
    {
        /// <summary>필드 값으로 태그를 고른다(체크박스).</summary>
        SelectionMode,

        /// <summary>태그 정의를 관리한다(이름 변경·삭제). 고를 값이 없다.</summary>
        ManagementMode,
    }
}
