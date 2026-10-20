namespace Core.AbilitySystem.Abilities
{
    /// <summary>ASC에 부여된 어빌리티 하나의 런타임 레코드. 만들 때 핸들이 발급되고, <see cref="AbilitySystemComponent.GiveAbility"/>로 ASC에 넣는다.</summary>
    /// <remarks>UE: FGameplayAbilitySpec</remarks>
    public sealed class GameplayAbilitySpec
    {
        /// <summary>입력에 묶이지 않은 InputID.</summary>
        /// <remarks>UE: INDEX_NONE</remarks>
        public const int NoInputID = -1;

        private GameplayAbility _primaryInstance;

        public GameplayAbilitySpec(GameplayAbilityAsset ability, int level = 1, int inputID = NoInputID, UnityEngine.Object sourceObject = null)
        {
            Handle = GameplayAbilitySpecHandle.GenerateNewHandle();
            Ability = ability;
            Level = level;
            InputID = inputID;
            SourceObject = sourceObject;
        }

        public GameplayAbilitySpecHandle Handle { get; }

        public GameplayAbilityAsset Ability { get; }

        public int Level { get; }

        /// <summary>이 어빌리티를 발동하는 입력 ID. 입력에 묶이지 않으면 <see cref="NoInputID"/>.</summary>
        public int InputID { get; }

        /// <summary>이 어빌리티를 부여한 출처(예: 장비 아이템). 없으면 null.</summary>
        public UnityEngine.Object SourceObject { get; }

        /// <summary>현재 활성화 횟수.</summary>
        public int ActiveCount { get; internal set; }

        /// <summary>스코프 락 중에 해제가 요청돼 락이 풀리면 제거될 Spec.</summary>
        public bool PendingRemove { get; internal set; }

        /// <remarks>UE: FGameplayAbilitySpec::IsActive</remarks>
        public bool IsActive => Ability != null && ActiveCount > 0;

        /// <summary>부여될 때 만든 이 ASC 전용 런타임 인스턴스. 부여 전·해제 후엔 null.</summary>
        /// <remarks>UE: FGameplayAbilitySpec::GetPrimaryInstance</remarks>
        public GameplayAbility GetPrimaryInstance()
        {
            return _primaryInstance;
        }

        internal void SetPrimaryInstance(GameplayAbility instance)
        {
            _primaryInstance = instance;
        }
    }
}
