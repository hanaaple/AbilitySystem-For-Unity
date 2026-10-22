using System;
using System.Collections.Generic;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Core.AbilitySystem.Abilities
{
    /// <summary>어빌리티의 런타임 부분. 정의 에셋(<see cref="GameplayAbilityAsset"/>)이 하위 클래스 인스턴스를 원형으로 들고(필드 값 저장), 부여될 때마다 원형을 복제해 GameObject별 상태와 동작을 갖는다. 하위 클래스도 [Serializable]이어야 저장된다.</summary>
    /// <remarks>UE: UGameplayAbility</remarks>
    [Serializable]
    public abstract class GameplayAbility
    {
        [SerializeField] private GameplayAbilityInstancingPolicy instancingPolicy = GameplayAbilityInstancingPolicy.InstancedPerExecution;

        [Tooltip("커밋 때 Owner에 적용하는 코스트 GE(마나·스태미나 등). 발동 전엔 감당 가능한지(AddBase 모디파이어 결과가 음수가 아닌지)만 본다.")]
        [AbilityGameplayEffectSlot(AbilityGameplayEffectSlot.Cost)]
        [SerializeField] private GameplayEffectAsset costGameplayEffect;

        [Tooltip("커밋 때 Owner에 적용하는 쿨다운 GE(Duration). 이 GE가 부여하는 태그가 Owner에 있는 동안 발동할 수 없다.")]
        [AbilityGameplayEffectSlot(AbilityGameplayEffectSlot.Cooldown)]
        [SerializeField] private GameplayEffectAsset cooldownGameplayEffect;

        [Tooltip("이 어빌리티를 가리키는 태그. 다른 어빌리티의 차단·취소 태그가 이 태그로 이 어빌리티를 찾는다.")]
        [SerializeField] private GameplayTagContainer assetTags = new();

        [Tooltip("발동할 때 이 태그를 가진 활성 어빌리티를 취소한다.")]
        [SerializeField] private GameplayTagContainer cancelAbilitiesWithTag = new();

        [Tooltip("활성인 동안 이 태그를 가진 어빌리티의 발동을 막는다.")]
        [SerializeField] private GameplayTagContainer blockAbilitiesWithTag = new();

        [Tooltip("활성인 동안 Owner에 붙는 태그.")]
        [SerializeField] private GameplayTagContainer activationOwnedTags = new();

        [Tooltip("Owner가 이 태그를 모두 가져야 발동할 수 있다.")]
        [SerializeField] private GameplayTagContainer activationRequiredTags = new();

        [Tooltip("Owner가 이 태그 중 하나라도 가지면 발동할 수 없다.")]
        [SerializeField] private GameplayTagContainer activationBlockedTags = new();

        private bool _isActive;
        private bool _isBlockingOtherAbilities;

        // 복제로 만든 인스턴스만 true. 에셋 원형은 모든 GameObject가 공유하므로 GameObject 정보를 쓰지 않는 기준이 된다.
        private bool _isInstantiated;

        /// <remarks>UE: UGameplayAbility::GetInstancingPolicy</remarks>
        public GameplayAbilityInstancingPolicy InstancingPolicy => instancingPolicy;

        /// <summary>이 어빌리티를 가리키는 태그. 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::GetAssetTags</remarks>
        public GameplayTagContainer AssetTags => assetTags;

        /// <summary>발동할 때 취소하는 어빌리티 태그. 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::CancelAbilitiesWithTag</remarks>
        public GameplayTagContainer CancelAbilitiesWithTag => cancelAbilitiesWithTag;

        /// <summary>활성인 동안 막는 어빌리티 태그. 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::BlockAbilitiesWithTag</remarks>
        public GameplayTagContainer BlockAbilitiesWithTag => blockAbilitiesWithTag;

        /// <summary>활성인 동안 Owner에 붙는 태그. 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::ActivationOwnedTags</remarks>
        public GameplayTagContainer ActivationOwnedTags => activationOwnedTags;

        /// <summary>발동에 필요한 Owner 태그(모두). 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::ActivationRequiredTags</remarks>
        public GameplayTagContainer ActivationRequiredTags => activationRequiredTags;

        /// <summary>발동을 막는 Owner 태그(하나라도). 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::ActivationBlockedTags</remarks>
        public GameplayTagContainer ActivationBlockedTags => activationBlockedTags;

        /// <summary>발동 중인지. 발동(<see cref="PreActivate"/>) 때 켜지고 <see cref="EndAbility"/>에서 꺼진다. 원형은 항상 false.</summary>
        /// <remarks>UE: UGameplayAbility::IsActive</remarks>
        public bool IsActive => _isActive;

        /// <summary>부여·발동 때 원형을 복제해 만든 인스턴스인지. 에셋 원형(UE CDO 역할)이면 false.</summary>
        /// <remarks>UE: UGameplayAbility::IsInstantiated</remarks>
        public bool IsInstantiated => _isInstantiated;

        /// <summary>이 인스턴스가 속한 GameObject 정보. 부여 전엔 null.</summary>
        /// <remarks>UE: UGameplayAbility::GetCurrentActorInfo</remarks>
        public GameplayAbilityGameObjectInfo CurrentGameObjectInfo { get; private set; }

        /// <summary>이 인스턴스의 Spec 핸들.</summary>
        /// <remarks>UE: UGameplayAbility::GetCurrentAbilitySpecHandle</remarks>
        public GameplayAbilitySpecHandle CurrentAbilitySpecHandle { get; private set; }

        /// <summary>지금 발동할 수 있는지. 부작용이 없어야 한다. 재정의하면 base 결과도 함께 따진다.</summary>
        /// <param name="optionalRelevantTags">실패하면 실패 사유 태그(<see cref="GameplayAbilitiesSettings"/>)와 원인 태그를 더한다. null이면 모으지 않는다.</param>
        /// <remarks>UE: UGameplayAbility::CanActivateAbility</remarks>
        public virtual bool CanActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayTagContainer optionalRelevantTags = null)
        {
            if (gameObjectInfo == null || gameObjectInfo.AvatarGameObject == null)
            {
                return false;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            if (abilitySystemComponent == null)
            {
                return false;
            }

            if (abilitySystemComponent.FindAbilitySpecFromHandle(handle) == null)
            {
                Debug.LogWarning("[GA] CanActivateAbility: 없는 핸들.");
                return false;
            }

            if (!CheckCooldown(handle, gameObjectInfo, optionalRelevantTags))
            {
                return false;
            }

            if (!CheckCost(handle, gameObjectInfo, optionalRelevantTags))
            {
                return false;
            }

            if (!DoesAbilitySatisfyTagRequirements(abilitySystemComponent, optionalRelevantTags))
            {
                return false;
            }

            return true;
        }

        /// <summary>발동 전 공통 준비 — 활성 표시, GameObject 정보 갱신, 활성 중 태그 부여, 발동 알림, 다른 어빌리티 차단·취소, Spec의 활성 횟수 증가.</summary>
        /// <remarks>UE: UGameplayAbility::PreActivate</remarks>
        public virtual void PreActivate(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            _isActive = true;
            _isBlockingOtherAbilities = true;
            SetCurrentGameObjectInfo(handle, gameObjectInfo);

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            abilitySystemComponent.AddLooseGameplayTags(activationOwnedTags);
            abilitySystemComponent.NotifyAbilityActivated(handle, this);

            // 활성 횟수는 취소 뒤에 올린다 — 먼저 올리면 발동 중인 자기 Spec이 취소 대상으로 잡힐 수 있다.
            abilitySystemComponent.ApplyAbilityBlockAndCancelTags(assetTags, this, true, blockAbilitiesWithTag, true, cancelAbilitiesWithTag);

            GameplayAbilitySpec spec = abilitySystemComponent.FindAbilitySpecFromHandle(handle);
            if (spec == null)
            {
                Debug.LogWarning("[GA] PreActivate: 핸들에 맞는 Spec이 없음.");
                return;
            }

            spec.ActiveCount++;
        }

        /// <summary>발동 본체. 직접 부르지 않는다(<see cref="AbilitySystemComponent.TryActivateAbility"/>가 부른다). 끝낼 때 <see cref="EndAbility"/>를 불러야 한다.</summary>
        /// <remarks>UE: UGameplayAbility::ActivateAbility</remarks>
        public virtual void ActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
        }

        /// <summary>
        /// 어빌리티를 끝낸다. 정상 종료와 취소 모두 여기를 지난다. 활성 상태가 아니면 아무것도 하지 않는다.
        /// 재정의해 정리할 땐 <see cref="IsEndAbilityValid"/>로 확인하고 base를 마지막에 부른다.
        /// </summary>
        /// <remarks>UE: UGameplayAbility::EndAbility</remarks>
        public virtual void EndAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, bool wasCancelled)
        {
            if (!IsEndAbilityValid(handle, gameObjectInfo))
            {
                return;
            }

            _isActive = false;

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            abilitySystemComponent.RemoveLooseGameplayTags(activationOwnedTags);

            // 활성 중에 차단을 꺼 뒀으면 이미 풀린 상태라 다시 풀지 않는다.
            if (IsBlockingOtherAbilities())
            {
                abilitySystemComponent.ApplyAbilityBlockAndCancelTags(assetTags, this, false, blockAbilitiesWithTag, false, cancelAbilitiesWithTag);
            }

            abilitySystemComponent.NotifyAbilityEnded(handle, this, wasCancelled);
        }

        /// <summary>어빌리티를 취소한다(<see cref="EndAbility"/>를 취소로 부른다).</summary>
        /// <remarks>UE: UGameplayAbility::CancelAbility</remarks>
        public virtual void CancelAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            EndAbility(handle, gameObjectInfo, true);
        }

        /// <summary>활성 중에 이 Spec의 입력이 다시 눌렸다. 비활성일 때 누르면 이 대신 발동을 시도한다.</summary>
        /// <remarks>UE: UGameplayAbility::InputPressed</remarks>
        public virtual void InputPressed(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
        }

        /// <summary>활성 중에 이 Spec의 입력이 떼졌다. 누르는 동안만 유지하는 어빌리티는 여기서 끝낸다.</summary>
        /// <remarks>UE: UGameplayAbility::InputReleased</remarks>
        public virtual void InputReleased(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
        }

        /// <summary>ASC에 부여된 직후 호출된다(Spec은 이미 ASC 목록에 있다). 재정의하면 base를 먼저 불러야 GameObject 정보가 채워진다.</summary>
        /// <remarks>UE: UGameplayAbility::OnGiveAbility</remarks>
        public virtual void OnGiveAbility(GameplayAbilityGameObjectInfo gameObjectInfo, GameplayAbilitySpec spec)
        {
            SetCurrentGameObjectInfo(spec.Handle, gameObjectInfo);

            if (gameObjectInfo != null && gameObjectInfo.AvatarGameObject != null)
            {
                OnAvatarSet(gameObjectInfo, spec);
            }
        }

        /// <summary>ASC에서 제거되기 직전에 호출된다(Spec은 아직 ASC 목록에 있다).</summary>
        /// <remarks>UE: UGameplayAbility::OnRemoveAbility</remarks>
        public virtual void OnRemoveAbility(GameplayAbilityGameObjectInfo gameObjectInfo, GameplayAbilitySpec spec)
        {
        }

        /// <summary>Avatar가 정해지거나 바뀌면 호출된다. 패시브 시작 등 BeginPlay 성격의 처리를 둔다.</summary>
        /// <remarks>UE: UGameplayAbility::OnAvatarSet</remarks>
        public virtual void OnAvatarSet(GameplayAbilityGameObjectInfo gameObjectInfo, GameplayAbilitySpec spec)
        {
        }

        /// <summary>이 어빌리티가 거는 GE의 컨텍스트. Instigator = GameObject 정보의 ASC, EffectCauser = Avatar, SourceObject = Spec의 출처.</summary>
        /// <remarks>UE: UGameplayAbility::MakeEffectContext</remarks>
        public virtual GameplayEffectContextHandle MakeEffectContext(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (gameObjectInfo == null)
            {
                Debug.LogError("[GA] MakeEffectContext: gameObjectInfo가 null.");
                return default;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            var context = new GameplayEffectContextHandle(new GameplayEffectContext());
            context.AddInstigator(abilitySystemComponent, gameObjectInfo.AvatarGameObject);

            GameplayAbilitySpec spec = abilitySystemComponent != null ? abilitySystemComponent.FindAbilitySpecFromHandle(handle) : null;

            // Spec의 출처는 Object, 컨텍스트의 출처는 GameObject라 GameObject 출처만 넘어간다.
            if (spec?.SourceObject is GameObject sourceObject)
            {
                context.AddSourceObject(sourceObject);
            }

            return context;
        }

        /// <summary>이 어빌리티의 컨텍스트로 적용 대기 GE Spec을 만든다. GE가 null이거나 GameObject 정보가 없으면 null.</summary>
        /// <remarks>UE: UGameplayAbility::MakeOutgoingGameplayEffectSpec</remarks>
        public virtual GameplayEffectSpec MakeOutgoingGameplayEffectSpec(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayEffectAsset effect, float level = 1f)
        {
            if (gameObjectInfo == null)
            {
                Debug.LogError("[GA] MakeOutgoingGameplayEffectSpec: gameObjectInfo가 null.");
                return null;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            return abilitySystemComponent.MakeOutgoingSpec(effect, MakeEffectContext(handle, gameObjectInfo), level);
        }

        /// <summary>
        /// 코스트·쿨다운을 다시 확인하고 통과하면 쿨다운·코스트 GE를 Owner에 적용하고 커밋을 알린다. 실패하면 false(아무것도 적용하지 않음).
        /// 보통 <see cref="ActivateAbility"/> 안에서 부르고, 실패하면 <see cref="EndAbility"/>로 끝낸다.
        /// </summary>
        /// <param name="optionalRelevantTags">실패하면 실패 사유 태그를 더한다. null이면 모으지 않는다.</param>
        /// <remarks>UE: UGameplayAbility::CommitAbility</remarks>
        public virtual bool CommitAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayTagContainer optionalRelevantTags = null)
        {
            // 발동 뒤 커밋 전에 자원이 바뀌었을 수 있어 마지막으로 다시 확인한다.
            if (!CommitCheck(handle, gameObjectInfo, optionalRelevantTags))
            {
                return false;
            }

            CommitExecute(handle, gameObjectInfo);

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            abilitySystemComponent.NotifyAbilityCommit(this);
            return true;
        }

        /// <summary>커밋할 수 있는지 — 핸들·GameObject 정보·Spec이 유효하고 쿨다운·코스트를 통과해야 한다. 태그 조건은 다시 보지 않는다.</summary>
        /// <param name="optionalRelevantTags">실패하면 실패 사유 태그를 더한다. null이면 모으지 않는다.</param>
        /// <remarks>UE: UGameplayAbility::CommitCheck</remarks>
        public virtual bool CommitCheck(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayTagContainer optionalRelevantTags = null)
        {
            AbilitySystemComponent abilitySystemComponent = gameObjectInfo?.AbilitySystemComponent;
            bool validSpecFound = abilitySystemComponent != null && abilitySystemComponent.FindAbilitySpecFromHandle(handle) != null;
            if (!handle.IsValid || !validSpecFound)
            {
                Debug.LogWarning("[GA] CommitCheck: 핸들·GameObject 정보가 무효이거나 Spec이 없음.");
                return false;
            }

            if (!CheckCooldown(handle, gameObjectInfo, optionalRelevantTags))
            {
                return false;
            }

            if (!CheckCost(handle, gameObjectInfo, optionalRelevantTags))
            {
                return false;
            }

            return true;
        }

        /// <summary>쿨다운 → 코스트 순으로 GE를 적용한다.</summary>
        /// <remarks>UE: UGameplayAbility::CommitExecute</remarks>
        public virtual void CommitExecute(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            ApplyCooldown(handle, gameObjectInfo);
            ApplyCost(handle, gameObjectInfo);
        }

        /// <summary>쿨다운 중이 아닌지 — 쿨다운 GE가 부여하는 태그를 Owner가 하나라도 가지면 false.</summary>
        /// <param name="optionalRelevantTags">실패하면 쿨다운 실패 태그와 Owner가 가진 쿨다운 태그를 더한다. null이면 모으지 않는다.</param>
        /// <remarks>UE: UGameplayAbility::CheckCooldown</remarks>
        public virtual bool CheckCooldown(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayTagContainer optionalRelevantTags = null)
        {
            if (gameObjectInfo == null)
            {
                Debug.LogError("[GA] CheckCooldown: gameObjectInfo가 null.");
                return true;
            }

            GameplayTagContainer cooldownTags = GetCooldownTags();
            if (cooldownTags == null || cooldownTags.IsEmpty())
            {
                return true;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            if (abilitySystemComponent == null || !abilitySystemComponent.HasAnyMatchingGameplayTags(cooldownTags))
            {
                return true;
            }

            if (optionalRelevantTags != null)
            {
                GameplayAbilitiesSettings settings = GameplayAbilitiesSettings.Get();
                AddFailureTag(optionalRelevantTags, settings.ActivateFailCooldownTag);

                // 어느 쿨다운이 막았는지 호출측이 알 수 있게 Owner가 가진 쿨다운 태그도 넘긴다.
                optionalRelevantTags.AppendMatchingTags(abilitySystemComponent.GetOwnedGameplayTags(), cooldownTags);
            }

            return false;
        }

        /// <summary>쿨다운 GE를 어빌리티 레벨로 Owner에 적용한다. 없으면 아무것도 하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::ApplyCooldown</remarks>
        public virtual void ApplyCooldown(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            GameplayEffectAsset cooldownEffect = GetCooldownGameplayEffect();
            if (cooldownEffect != null)
            {
                ApplyGameplayEffectToOwner(handle, gameObjectInfo, cooldownEffect, GetAbilityLevel(handle, gameObjectInfo));
            }
        }

        /// <summary>코스트를 감당할 수 있는지 — 코스트 GE의 AddBase 모디파이어를 적용해도 Owner 어트리뷰트가 음수가 되지 않아야 한다.</summary>
        /// <param name="optionalRelevantTags">실패하면 코스트 실패 태그를 더한다. null이면 모으지 않는다.</param>
        /// <remarks>UE: UGameplayAbility::CheckCost</remarks>
        public virtual bool CheckCost(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayTagContainer optionalRelevantTags = null)
        {
            GameplayEffectAsset costEffect = GetCostGameplayEffect();
            if (costEffect == null)
            {
                return true;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo?.AbilitySystemComponent;
            if (abilitySystemComponent == null)
            {
                Debug.LogError("[GA] CheckCost: ASC가 없음.");
                return true;
            }

            if (abilitySystemComponent.CanApplyAttributeModifiers(costEffect, GetAbilityLevel(handle, gameObjectInfo), MakeEffectContext(handle, gameObjectInfo)))
            {
                return true;
            }

            if (optionalRelevantTags != null)
            {
                GameplayAbilitiesSettings settings = GameplayAbilitiesSettings.Get();
                AddFailureTag(optionalRelevantTags, settings.ActivateFailCostTag);
            }

            return false;
        }

        /// <summary>코스트 GE를 어빌리티 레벨로 Owner에 적용한다. 없으면 아무것도 하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::ApplyCost</remarks>
        public virtual void ApplyCost(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            GameplayEffectAsset costEffect = GetCostGameplayEffect();
            if (costEffect != null)
            {
                ApplyGameplayEffectToOwner(handle, gameObjectInfo, costEffect, GetAbilityLevel(handle, gameObjectInfo));
            }
        }

        /// <remarks>UE: UGameplayAbility::GetCooldownGameplayEffect</remarks>
        public virtual GameplayEffectAsset GetCooldownGameplayEffect()
        {
            return cooldownGameplayEffect;
        }

        /// <remarks>UE: UGameplayAbility::GetCostGameplayEffect</remarks>
        public virtual GameplayEffectAsset GetCostGameplayEffect()
        {
            return costGameplayEffect;
        }

        /// <summary>이 어빌리티를 쿨다운에 넣는 태그 = 쿨다운 GE가 부여하는 태그. 쿨다운 GE가 없으면 null. 수정하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::GetCooldownTags</remarks>
        public virtual GameplayTagContainer GetCooldownTags()
        {
            GameplayEffectAsset cooldownEffect = GetCooldownGameplayEffect();
            return cooldownEffect != null ? cooldownEffect.GetGrantedTags() : null;
        }

        /// <summary>남은 쿨다운(초). 쿨다운 태그를 부여하는 활성 GE 중 가장 긴 것. 없으면 0.</summary>
        /// <remarks>UE: UGameplayAbility::GetCooldownTimeRemaining</remarks>
        public virtual float GetCooldownTimeRemaining(GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            AbilitySystemComponent abilitySystemComponent = gameObjectInfo?.AbilitySystemComponent;
            GameplayTagContainer cooldownTags = GetCooldownTags();
            if (abilitySystemComponent == null || cooldownTags == null || cooldownTags.IsEmpty())
            {
                return 0f;
            }

            GameplayEffectQuery query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(cooldownTags);
            List<float> timesRemaining = abilitySystemComponent.GetActiveEffectsTimeRemaining(query);
            float longest = 0f;
            foreach (float timeRemaining in timesRemaining)
            {
                longest = Mathf.Max(longest, timeRemaining);
            }

            return longest;
        }

        /// <summary>남은 쿨다운과 그 쿨다운의 전체 길이(초) — HUD 게이지용. 남은 시간이 가장 긴 GE 기준. 없으면 둘 다 0.</summary>
        /// <remarks>UE: UGameplayAbility::GetCooldownTimeRemainingAndDuration</remarks>
        public virtual void GetCooldownTimeRemainingAndDuration(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, out float timeRemaining, out float cooldownDuration)
        {
            timeRemaining = 0f;
            cooldownDuration = 0f;

            GameplayTagContainer cooldownTags = GetCooldownTags();
            AbilitySystemComponent abilitySystemComponent = gameObjectInfo?.AbilitySystemComponent;
            if (cooldownTags == null || cooldownTags.IsEmpty() || abilitySystemComponent == null)
            {
                return;
            }

            GameplayEffectQuery query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(cooldownTags);
            List<(float TimeRemaining, float Duration)> durations = abilitySystemComponent.GetActiveEffectsTimeRemainingAndDuration(query);
            foreach ((float remaining, float duration) in durations)
            {
                if (remaining > timeRemaining)
                {
                    timeRemaining = remaining;
                    cooldownDuration = duration;
                }
            }
        }

        /// <summary>발동 태그 조건을 만족하는지 — ASC가 이 어빌리티 태그를 막고 있거나, Owner가 차단 태그를 하나라도 가지면 false, 필요 태그가 하나라도 없으면 false.</summary>
        /// <param name="optionalRelevantTags">실패하면 차단 실패 태그 + 막은 태그, 필요 태그 없음 실패 태그 + 빠진 필요 태그를 더한다. null이면 모으지 않는다.</param>
        /// <remarks>UE: UGameplayAbility::DoesAbilitySatisfyTagRequirements</remarks>
        public virtual bool DoesAbilitySatisfyTagRequirements(AbilitySystemComponent abilitySystemComponent, GameplayTagContainer optionalRelevantTags = null)
        {
            GameplayTagContainer ownedTags = abilitySystemComponent.GetOwnedGameplayTags();
            bool blocked = false;

            void CheckForBlocked(GameplayTagContainer containerA, GameplayTagContainer containerB)
            {
                if (containerA.IsEmpty() || containerB.IsEmpty() || !containerA.HasAny(containerB))
                {
                    return;
                }

                if (optionalRelevantTags != null)
                {
                    // 차단 실패 태그는 막힌 곳이 여럿이어도 한 번만 붙인다.
                    if (!blocked)
                    {
                        GameplayAbilitiesSettings settings = GameplayAbilitiesSettings.Get();
                        AddFailureTag(optionalRelevantTags, settings.ActivateFailTagsBlockedTag);
                    }

                    optionalRelevantTags.AppendMatchingTags(containerA, containerB);
                }

                blocked = true;
            }

            // 차단과 필요를 둘 다 따져 실패 사유를 모두 모은다. 차단을 먼저 봐서 사유에 차단 쪽이 앞에 온다.
            CheckForBlocked(assetTags, abilitySystemComponent.GetBlockedAbilityTags());
            CheckForBlocked(ownedTags, activationBlockedTags);

            bool missing = !activationRequiredTags.IsEmpty() && !ownedTags.HasAll(activationRequiredTags);
            if (missing && optionalRelevantTags != null)
            {
                GameplayAbilitiesSettings settings = GameplayAbilitiesSettings.Get();
                AddFailureTag(optionalRelevantTags, settings.ActivateFailTagsMissingTag);
                var missingTags = new GameplayTagContainer(activationRequiredTags);
                missingTags.RemoveTags(ownedTags.GetGameplayTagParents());
                optionalRelevantTags.AppendTags(missingTags);
            }

            // ASC가 AreAbilityTagsBlocked를 재정의해 막으면 어느 태그인지 알 수 없어 차단 실패 태그만 붙인다.
            if (!blocked && !missing)
            {
                blocked = abilitySystemComponent.AreAbilityTagsBlocked(assetTags);
                if (blocked && optionalRelevantTags != null)
                {
                    GameplayAbilitiesSettings settings = GameplayAbilitiesSettings.Get();
                    AddFailureTag(optionalRelevantTags, settings.ActivateFailTagsBlockedTag);
                }
            }

            return !blocked && !missing;
        }

        /// <summary>다른 어빌리티를 막고 있는지. 발동 때 켜지고, <see cref="SetShouldBlockOtherAbilities"/>로 끌 수 있다.</summary>
        /// <remarks>UE: UGameplayAbility::IsBlockingOtherAbilities</remarks>
        public virtual bool IsBlockingOtherAbilities()
        {
            return _isBlockingOtherAbilities;
        }

        /// <summary>활성 중에 다른 어빌리티 차단(<see cref="BlockAbilitiesWithTag"/>)을 켜거나 끈다. 비활성이거나 이미 그 상태면 아무것도 하지 않는다. 취소는 다시 하지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::SetShouldBlockOtherAbilities</remarks>
        public virtual void SetShouldBlockOtherAbilities(bool shouldBlockAbilities)
        {
            if (!_isActive || shouldBlockAbilities == _isBlockingOtherAbilities)
            {
                return;
            }

            _isBlockingOtherAbilities = shouldBlockAbilities;

            AbilitySystemComponent abilitySystemComponent = GetAbilitySystemComponentFromInfo();
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.ApplyAbilityBlockAndCancelTags(assetTags, this, _isBlockingOtherAbilities, blockAbilitiesWithTag, false, cancelAbilitiesWithTag);
            }
        }

        /// <summary><see cref="PreActivate"/> 후 <see cref="ActivateAbility"/>를 부른다.</summary>
        /// <remarks>UE: UGameplayAbility::CallActivateAbility</remarks>
        public void CallActivateAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            PreActivate(handle, gameObjectInfo);
            ActivateAbility(handle, gameObjectInfo);
        }

        /// <summary>지금 끝낼 수 있는지 — 활성 중이고 ASC가 있으며 Spec도 활성이어야 한다. 이미 끝난 뒤의 중복 종료를 막는다.</summary>
        /// <remarks>UE: UGameplayAbility::IsEndAbilityValid</remarks>
        public bool IsEndAbilityValid(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (!_isActive)
            {
                return false;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo?.AbilitySystemComponent;
            if (abilitySystemComponent == null)
            {
                return false;
            }

            GameplayAbilitySpec spec = abilitySystemComponent.FindAbilitySpecFromHandle(handle);
            bool isSpecActive = spec != null ? spec.IsActive : _isActive;
            return isSpecActive;
        }

        /// <remarks>UE: UGameplayAbility::GetAbilitySystemComponentFromActorInfo</remarks>
        public AbilitySystemComponent GetAbilitySystemComponentFromInfo()
        {
            return CurrentGameObjectInfo?.AbilitySystemComponent;
        }

        /// <remarks>UE: UGameplayAbility::GetOwningActorFromActorInfo</remarks>
        public GameObject GetOwnerGameObjectFromInfo()
        {
            return CurrentGameObjectInfo?.OwnerGameObject;
        }

        /// <remarks>UE: UGameplayAbility::GetAvatarActorFromActorInfo</remarks>
        public GameObject GetAvatarGameObjectFromInfo()
        {
            return CurrentGameObjectInfo?.AvatarGameObject;
        }

        /// <summary>현재 Spec·GameObject 정보로 <see cref="MakeOutgoingGameplayEffectSpec(GameplayAbilitySpecHandle, GameplayAbilityGameObjectInfo, GameplayEffectAsset, float)"/>.</summary>
        /// <remarks>UE: UGameplayAbility::MakeOutgoingGameplayEffectSpec</remarks>
        public GameplayEffectSpec MakeOutgoingGameplayEffectSpec(GameplayEffectAsset effect, float level = 1f)
        {
            return MakeOutgoingGameplayEffectSpec(CurrentAbilitySpecHandle, CurrentGameObjectInfo, effect, level);
        }

        /// <summary>GE를 이 어빌리티의 Owner ASC에 적용한다. GE가 null이면 Invalid. Instant는 실행 후 Invalid.</summary>
        /// <remarks>UE: UGameplayAbility::ApplyGameplayEffectToOwner</remarks>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToOwner(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayEffectAsset effect, float level)
        {
            if (effect == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            GameplayEffectSpec spec = MakeOutgoingGameplayEffectSpec(handle, gameObjectInfo, effect, level);
            return ApplyGameplayEffectSpecToOwner(handle, gameObjectInfo, spec);
        }

        /// <summary>만들어 둔 Spec을 Owner ASC에 적용한다. Spec이 null이면 Invalid.</summary>
        /// <remarks>UE: UGameplayAbility::ApplyGameplayEffectSpecToOwner</remarks>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToOwner(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayEffectSpec spec)
        {
            if (spec == null || gameObjectInfo == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            return abilitySystemComponent.ApplyGameplayEffectSpecToSelf(spec);
        }

        /// <summary>GE를 대상 ASC에 적용한다(Instigator는 이 어빌리티의 ASC). GE가 null이면 에러 로그 후 Invalid.</summary>
        /// <remarks>UE: UGameplayAbility::ApplyGameplayEffectToTarget</remarks>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToTarget(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, AbilitySystemComponent target, GameplayEffectAsset effect, float level)
        {
            if (effect == null)
            {
                Debug.LogError("[GA] ApplyGameplayEffectToTarget: GE가 null.");
                return ActiveGameplayEffectHandle.Invalid;
            }

            GameplayEffectSpec spec = MakeOutgoingGameplayEffectSpec(handle, gameObjectInfo, effect, level);
            if (spec == null)
            {
                Debug.LogWarning("[GA] ApplyGameplayEffectToTarget: Spec을 만들지 못함.");
                return ActiveGameplayEffectHandle.Invalid;
            }

            return ApplyGameplayEffectSpecToTarget(handle, gameObjectInfo, spec, target);
        }

        /// <summary>만들어 둔 Spec을 대상 ASC에 적용한다. Spec이나 대상이 null이면 Invalid.</summary>
        /// <remarks>UE: UGameplayAbility::ApplyGameplayEffectSpecToTarget</remarks>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToTarget(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, GameplayEffectSpec spec, AbilitySystemComponent target)
        {
            if (spec == null || gameObjectInfo == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            AbilitySystemComponent abilitySystemComponent = gameObjectInfo.AbilitySystemComponent;
            return abilitySystemComponent.ApplyGameplayEffectSpecToTarget(spec, target);
        }

        /// <summary>현재 Spec의 레벨. 부여 전이면 1.</summary>
        /// <remarks>UE: UGameplayAbility::GetAbilityLevel</remarks>
        public int GetAbilityLevel()
        {
            if (CurrentGameObjectInfo == null)
            {
                return 1;
            }

            return GetAbilityLevel(CurrentAbilitySpecHandle, CurrentGameObjectInfo);
        }

        /// <summary>핸들의 Spec 레벨. Spec이 없으면 경고 후 1.</summary>
        /// <remarks>UE: UGameplayAbility::GetAbilityLevel</remarks>
        public int GetAbilityLevel(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            AbilitySystemComponent abilitySystemComponent = gameObjectInfo?.AbilitySystemComponent;
            GameplayAbilitySpec spec = abilitySystemComponent != null ? abilitySystemComponent.FindAbilitySpecFromHandle(handle) : null;
            if (spec == null)
            {
                Debug.LogWarning("[GA] GetAbilityLevel: 핸들에 맞는 Spec이 없어 레벨 1을 돌려준다.");
                return 1;
            }

            return spec.Level;
        }

        internal void MarkInstantiated()
        {
            _isInstantiated = true;
        }

        /// <summary>현재 GameObject 정보·Spec 핸들을 기억한다. 원형에는 쓰지 않는다.</summary>
        /// <remarks>UE: UGameplayAbility::SetCurrentActorInfo</remarks>
        protected void SetCurrentGameObjectInfo(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo)
        {
            if (!_isInstantiated)
            {
                return;
            }

            CurrentGameObjectInfo = gameObjectInfo;
            CurrentAbilitySpecHandle = handle;
        }

        // 설정에서 칸을 비워 둔 실패 사유는 붙이지 않는다.
        private static void AddFailureTag(GameplayTagContainer optionalRelevantTags, GameplayTag failureTag)
        {
            if (failureTag.IsValid())
            {
                optionalRelevantTags.AddTag(failureTag);
            }
        }
    }
}
