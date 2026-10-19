using System.Collections.Generic;
using Core.AbilitySystem.Tag;
using UnityEngine;


namespace Core.AbilitySystem.Effect
{

    /// <summary>GE의 정적 정의(무엇을·얼마나·어떻게 적용할지)를 담는 데이터 에셋. 런타임 적용 시 이 에셋으로부터 <see cref="GameplayEffectSpec"/>이 만들어진다.</summary>
    [CreateAssetMenu(menuName = "Ability System/Gameplay Effect", fileName = "GE_New")]
    public sealed class GameplayEffectAsset : ScriptableObject
    {
        [SerializeField] private GameplayEffectType type;
        [Min(0f)] [SerializeField] private float duration;
        [Tooltip("0이면 주기 실행 없음. 0 초과면 해당 간격마다 반복 적용.")]
        [Min(0f)] [SerializeField] private float period;

        [Tooltip("true면 적용 즉시 1회 실행 후 주기마다 실행. false면 첫 주기 이후부터 실행.")]
        [SerializeField] private bool executePeriodicEffectOnApplication = true;

        [SerializeField] private List<GameplayModifier> modifiers;

        [SerializeField] private List<GameplayEffectExecutionDefinition> executions = new();

        // Component 종류마다 타입이 달라 SerializeReference로 담는다.
        [SerializeReference] private List<GameplayEffectComponent> gameplayEffectComponents = new();

        // 설정값이 아니라 GE Component가 채우는 캐시 — 직렬화하지 않고 OnGameplayEffectChanged에서 다시 만든다.
        private readonly GameplayTagContainer _cachedAssetTags = new();
        private readonly GameplayTagContainer _cachedGrantedTags = new();

        // TODO GE 스택 구현

        // TODO Gameplay Cue

        public GameplayEffectType Type => type;
        public float Duration => duration;
        public float Period => period;
        public bool ExecutePeriodicEffectOnApplication => executePeriodicEffectOnApplication;
        public IReadOnlyList<GameplayModifier> Modifiers => modifiers;
        public IReadOnlyList<GameplayEffectExecutionDefinition> Executions => executions;

        // GE Component가 OnGameplayEffectChanged에서 채우는 통로. 밖에는 GetAssetTags()로 읽기만 내준다
        internal GameplayTagContainer CachedAssetTags => _cachedAssetTags;
        internal GameplayTagContainer CachedGrantedTags => _cachedGrantedTags;

        // UE PostLoad 시점 — 에셋 로드·도메인 리로드 후 캐시를 채운다.
        private void OnEnable()
        {
            OnGameplayEffectChanged();
        }

#if UNITY_EDITOR
        // UE는 Component 편집(PostEditChangeProperty) 때 다시 부른다. Component가 SO 안에 직렬화될 것이라 SO 편집 콜백으로 받는다.
        private void OnValidate()
        {
            OnGameplayEffectChanged();
        }
#endif

        /// <summary>캐시 태그를 비우고 각 GE Component에서 다시 모은다.</summary>
        /// <remarks>UE: UGameplayEffect::OnGameplayEffectChanged</remarks>
        public void OnGameplayEffectChanged()
        {
            _cachedAssetTags.Reset();
            _cachedGrantedTags.Reset();

            foreach (GameplayEffectComponent gameplayEffectComponent in gameplayEffectComponents)
            {
                // SerializeReference 목록은 타입을 고르지 않은 칸이 null로 남는다
                if (gameplayEffectComponent != null)
                {
                    gameplayEffectComponent.SetOwner(this);
                    gameplayEffectComponent.OnGameplayEffectChanged();
                }
            }
        }

        /// <summary>이 Spec을 컨테이너 주인에게 적용할 수 있는지. Component 중 하나라도 막으면 false.</summary>
        /// <remarks>UE: UGameplayEffect::CanApply</remarks>
        public bool CanApply(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
            foreach (GameplayEffectComponent gameplayEffectComponent in gameplayEffectComponents)
            {
                if (gameplayEffectComponent != null && !gameplayEffectComponent.CanGameplayEffectApply(activeGameplayEffectsContainer, spec))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>지속형 GE가 활성 목록에 들어갔음을 Component에 알린다. 하나라도 false면 GE는 꺼진 채로 시작한다.</summary>
        /// <remarks>UE: UGameplayEffect::OnAddedToActiveContainer</remarks>
        internal bool OnAddedToActiveContainer(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, ActiveGameplayEffect activeGameplayEffect)
        {
            bool shouldBeActive = true;
            foreach (GameplayEffectComponent gameplayEffectComponent in gameplayEffectComponents)
            {
                if (gameplayEffectComponent != null)
                {
                    // 앞에서 false가 나와도 나머지 Component를 모두 부른다 — 각자 이 GE에 대한 준비(태그 구독 등)를 해야 한다
                    shouldBeActive = gameplayEffectComponent.OnActiveGameplayEffectAdded(activeGameplayEffectsContainer, activeGameplayEffect) && shouldBeActive;
                }
            }

            return shouldBeActive;
        }

        /// <summary>GE가 실행됐음을 Component에 알린다(Instant 적용, Periodic 주기마다).</summary>
        /// <remarks>UE: UGameplayEffect::OnExecuted</remarks>
        internal void OnExecuted(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
            foreach (GameplayEffectComponent gameplayEffectComponent in gameplayEffectComponents)
            {
                if (gameplayEffectComponent != null)
                {
                    gameplayEffectComponent.OnGameplayEffectExecuted(activeGameplayEffectsContainer, spec);
                }
            }
        }

        /// <summary>GE가 적용됐음을 Component에 알린다(Instant·지속형 모두).</summary>
        /// <remarks>UE: UGameplayEffect::OnApplied</remarks>
        internal void OnApplied(ActiveGameplayEffectsContainer activeGameplayEffectsContainer, GameplayEffectSpec spec)
        {
            foreach (GameplayEffectComponent gameplayEffectComponent in gameplayEffectComponents)
            {
                if (gameplayEffectComponent != null)
                {
                    gameplayEffectComponent.OnGameplayEffectApplied(activeGameplayEffectsContainer, spec);
                }
            }
        }

        /// <summary>이 GE 자체가 가진 태그(대상에게 부여하지 않음). 반환값을 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayEffect::GetAssetTags</remarks>
        public GameplayTagContainer GetAssetTags()
        {
            return _cachedAssetTags;
        }

        /// <summary>이 GE가 활성인 동안 대상에게 부여하는 태그. 반환값을 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayEffect::GetGrantedTags</remarks>
        public GameplayTagContainer GetGrantedTags()
        {
            return _cachedGrantedTags;
        }
    }
}
