using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.AbilitySystem
{
    /// <summary>어빌리티 시스템 프로젝트 설정 — 발동 실패 사유 태그. Resources 하위 고정 경로의 에셋 하나를 쓴다. 칸이 비었거나 에셋이 없으면 그 실패 사유 태그는 붙지 않는다(태그 자체는 레지스트리에 등록돼 있어야 고를 수 있다).</summary>
    /// <remarks>UE: UGameplayAbilitiesDeveloperSettings</remarks>
    public sealed class GameplayAbilitiesSettings : ScriptableObject
    {
        /// <summary>Resources 기준 고정 경로.</summary>
        public const string ResourcesPath = "AbilitySystem/GameplayAbilitiesSettings";

        private static GameplayAbilitiesSettings Instance;

        [Tooltip("CanActivateAbility가 실패 사유 태그 없이 거부했을 때 붙는 태그(재정의한 발동 조건 등).")]
        [SerializeField] [FormerlySerializedAs("activateFailCanActivateAbilityTag")] private GameplayTag _activateFailCanActivateAbilityTag;

        [Tooltip("쿨다운 중이라 발동하지 못했을 때 붙는 태그.")]
        [SerializeField] [FormerlySerializedAs("activateFailCooldownTag")] private GameplayTag _activateFailCooldownTag;

        [Tooltip("코스트를 감당하지 못해 발동하지 못했을 때 붙는 태그.")]
        [SerializeField] [FormerlySerializedAs("activateFailCostTag")] private GameplayTag _activateFailCostTag;

        [Tooltip("차단 태그 때문에 발동하지 못했을 때 붙는 태그.")]
        [SerializeField] [FormerlySerializedAs("activateFailTagsBlockedTag")] private GameplayTag _activateFailTagsBlockedTag;

        [Tooltip("필요 태그가 없어 발동하지 못했을 때 붙는 태그.")]
        [SerializeField] [FormerlySerializedAs("activateFailTagsMissingTag")] private GameplayTag _activateFailTagsMissingTag;

        /// <remarks>UE: UGameplayAbilitiesDeveloperSettings::ActivateFailCanActivateAbilityTag</remarks>
        public GameplayTag ActivateFailCanActivateAbilityTag => _activateFailCanActivateAbilityTag;

        /// <remarks>UE: UGameplayAbilitiesDeveloperSettings::ActivateFailCooldownTag</remarks>
        public GameplayTag ActivateFailCooldownTag => _activateFailCooldownTag;

        /// <remarks>UE: UGameplayAbilitiesDeveloperSettings::ActivateFailCostTag</remarks>
        public GameplayTag ActivateFailCostTag => _activateFailCostTag;

        /// <remarks>UE: UGameplayAbilitiesDeveloperSettings::ActivateFailTagsBlockedTag</remarks>
        public GameplayTag ActivateFailTagsBlockedTag => _activateFailTagsBlockedTag;

        /// <remarks>UE: UGameplayAbilitiesDeveloperSettings::ActivateFailTagsMissingTag</remarks>
        public GameplayTag ActivateFailTagsMissingTag => _activateFailTagsMissingTag;

        /// <summary>고정 경로의 설정. 없으면 경고 후 빈 설정(모든 실패 사유 태그가 비어 있음)을 돌려준다.</summary>
        public static GameplayAbilitiesSettings Get()
        {
            if (Instance == null)
            {
                Instance = Resources.Load<GameplayAbilitiesSettings>(ResourcesPath);
                if (Instance == null)
                {
                    Debug.LogWarning($"{nameof(GameplayAbilitiesSettings)}를 찾지 못해 실패 사유 태그 없이 동작합니다. (Resources/{ResourcesPath})");
                    Instance = CreateInstance<GameplayAbilitiesSettings>();
                }
            }

            return Instance;
        }
    }
}
