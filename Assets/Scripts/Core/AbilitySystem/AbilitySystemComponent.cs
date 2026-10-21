using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem
{
    public class AbilitySystemComponent : MonoBehaviour
    {
        [SerializeField] private AttributeDefinitionAsset attributeInitData;

        private readonly Dictionary<Type, AttributeSet> _spawnedAttributeSets = new();

        // 활성 GE의 소유·수명·실행·집계 + 반응성 aggregator를 이 컨테이너가 맡는다. ASC는 공개 API 위임 래퍼와 AttributeSet 저장만 남긴다.
        private ActiveGameplayEffectsContainer _activeGameplayEffectsContainer;

        // 소유 태그. Loose 태그(코드가 직접 넣는 태그)와 활성 GE의 부여 태그가 들어온다.
        private readonly GameplayTagCountContainer _gameplayTagCountContainer = new();

        private readonly GameplayAbilitySpecContainer _activatableAbilities = new();

        // 어빌리티 목록 스코프 락. 잠긴 동안의 부여·해제·전체 해제는 아래에 보류했다가 마지막 락이 풀릴 때 처리한다.
        private int _abilityScopeLockCount;
        private readonly List<GameplayAbilitySpec> _abilityPendingAdds = new();
        private readonly List<GameplayAbilitySpecHandle> _abilityPendingRemoves = new();
        private bool _abilityPendingClearAll;

        // 활성 어빌리티들이 막고 있는 어빌리티 태그. 여러 어빌리티가 같은 태그를 막을 수 있어 개수로 센다.
        private readonly GameplayTagCountContainer _blockedAbilityTags = new();

        // 발동 시도마다 비우고 다시 쓴다. 실패 이벤트 구독자에게 그대로 넘어가므로 구독자는 보관하지 않는다.
        private readonly GameplayTagContainer _internalTryActivateAbilityFailureTags = new();

        /// <summary>어빌리티가 발동(시작)될 때마다. <see cref="GameplayAbility.ActivateAbility"/> 직전, 활성 중 태그가 붙은 뒤에 온다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::AbilityActivatedCallbacks</remarks>
        public event Action<GameplayAbility> OnAbilityActivated;

        /// <summary>어빌리티가 커밋(쿨다운·코스트 적용)될 때마다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::AbilityCommittedCallbacks</remarks>
        public event Action<GameplayAbility> OnAbilityCommitted;

        /// <summary>발동 시도가 발동 조건(<see cref="GameplayAbility.CanActivateAbility"/>)에서 거부될 때마다. 두 번째 인자는 실패 사유 태그 — 다음 시도에서 비워지므로 보관하려면 복사한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::AbilityFailedCallbacks</remarks>
        public event Action<GameplayAbility, GameplayTagContainer> OnAbilityFailed;

        /// <summary>어빌리티가 끝날 때마다(정상 종료·취소 모두).</summary>
        /// <remarks>UE: UAbilitySystemComponent::AbilityEndedCallbacks</remarks>
        public event Action<GameplayAbility> OnAbilityEnded;

        /// <summary>어빌리티가 끝날 때마다, Spec 핸들·취소 여부를 함께. <see cref="OnAbilityEnded"/> 바로 뒤에 온다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::OnAbilityEnded</remarks>
        public event Action<AbilityEndedData> OnAbilityEndedWithData;

        /// <summary>어빌리티가 동작하는 액터 정보. 기본값은 Owner = Avatar = 이 GameObject.</summary>
        /// <remarks>UE: UAbilitySystemComponent::AbilityActorInfo</remarks>
        public GameplayAbilityActorInfo AbilityActorInfo { get; } = new();

        private void Awake()
        {
            _activeGameplayEffectsContainer = new ActiveGameplayEffectsContainer(this);
            _activatableAbilities.RegisterWithOwner(this);

            InitAbilityActorInfo(gameObject, gameObject);

            // AddSet은 AttributeSet 필드에 값을 직접 세팅할 뿐 CurrentValue 갱신 경로를 거치지 않아
            // 초기화 중 AttributeChanged가 발화하지 않는다(불필요한 재평가 없음).
            AddSet(attributeInitData);
        }

        private void Update()
        {
            _activeGameplayEffectsContainer.Tick();
        }

        /// <remarks>UE: UAbilitySystemComponent::OnComponentDestroyed</remarks>
        private void OnDestroy()
        {
            DestroyActiveState();

            // C#엔 강제 무효화(UE MarkAsGarbage)가 없어 ASC가 더는 내주지 않게 목록에서만 뺀다.
            _spawnedAttributeSets.Clear();
        }

        // ── AttributeSet ──────────────────────────────────────────────────────────

        public bool AddSpawnedAttribute(AttributeSet set)
        {
            if (!_spawnedAttributeSets.TryAdd(set.GetType(), set))
            {
                return false;
            }

            set.SetOwner(this);
            return true;
        }

        public void RemoveAttributeSet(AttributeSet set)
        {
            _spawnedAttributeSets.Remove(set.GetType());
        }

        /// <summary>핸들이 가리키는 AttributeSet 타입(<see cref="GameplayAttributeHandle.SetType"/>)의 인스턴스를 반환한다. 미등록이면 null.</summary>
        internal AttributeSet GetAttributeSet(GameplayAttributeHandle handle)
        {
            return _spawnedAttributeSets.GetValueOrDefault(handle.SetType);
        }

        public float GetAttributeBaseValue(GameplayAttributeHandle handle)
        {
            return _activeGameplayEffectsContainer.GetAttributeBaseValue(handle);
        }

        public float GetAttributeCurrentValue(GameplayAttributeHandle handle)
        {
            return _activeGameplayEffectsContainer.GetAttributeCurrentValue(handle);
        }

        /// <summary>어트리뷰트의 aggregator를 찾거나 없으면 생성해 반환한다 — 캡처(<see cref="Core.AbilitySystem.Effect.GameplayEffectAttributeCaptureSpec"/>)가 값·의존 추적의 출처로 쓴다.</summary>
        internal AttributeAggregator FindOrCreateAttributeAggregator(GameplayAttributeHandle handle)
        {
            return _activeGameplayEffectsContainer.FindOrCreateAttributeAggregator(handle);
        }

        public void SetAttributeBaseValue(GameplayAttributeHandle handle, float newValue)
        {
            _activeGameplayEffectsContainer.SetAttributeBaseValue(handle, newValue);
        }

        /// <summary>어트리뷰트 CurrentValue가 다시 계산될 때(값이 그대로여도) 울리는 이벤트. 반환값의 <see cref="GameplayAttributeValueChangeEvent.OnValueChange"/>에 구독·해제한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetGameplayAttributeValueChangeDelegate</remarks>
        public GameplayAttributeValueChangeEvent GetGameplayAttributeValueChangeDelegate(GameplayAttributeHandle handle)
        {
            return _activeGameplayEffectsContainer.GetGameplayAttributeValueChangeDelegate(handle);
        }

        // ── Gameplay Tag ──────────────────────────────────────────────────────────

        /// <summary>소유 태그를 <paramref name="tagContainer"/>에 채운다. 기존 내용은 비운다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetOwnedGameplayTags</remarks>
        public void GetOwnedGameplayTags(GameplayTagContainer tagContainer)
        {
            tagContainer.Reset();
            tagContainer.AppendTags(GetOwnedGameplayTags());
        }

        /// <summary>소유 태그. 읽기 전용으로 쓴다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetOwnedGameplayTags</remarks>
        public GameplayTagContainer GetOwnedGameplayTags()
        {
            return _gameplayTagCountContainer.GetExplicitGameplayTags();
        }

        /// <summary>태그를 가졌는지. 자식 태그만 가져도 부모 태그로 매칭된다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::HasMatchingGameplayTag</remarks>
        public bool HasMatchingGameplayTag(GameplayTag tagToCheck)
        {
            return _gameplayTagCountContainer.HasMatchingGameplayTag(tagToCheck);
        }

        /// <summary>주어진 태그 중 하나라도 가졌는지. 자식 태그만 가져도 부모 태그로 매칭된다. 비어 있으면 false.</summary>
        /// <remarks>UE: UAbilitySystemComponent::HasAnyMatchingGameplayTags</remarks>
        public bool HasAnyMatchingGameplayTags(GameplayTagContainer tagContainer)
        {
            GameplayTagContainer ownedTags = GetOwnedGameplayTags();
            return ownedTags.HasAny(tagContainer);
        }

        /// <summary>태그 개수. 자식 태그 개수를 포함한다(A.B와 A.C가 있으면 A는 2).</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetTagCount</remarks>
        public int GetTagCount(GameplayTag tagToCheck)
        {
            return _gameplayTagCountContainer.GetTagCount(tagToCheck);
        }

        /// <summary>GE 없이 코드가 직접 태그를 더한다. 같은 태그를 여러 번 더했으면 그만큼 빼야 없어진다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::AddLooseGameplayTag</remarks>
        public void AddLooseGameplayTag(GameplayTag gameplayTag, int count = 1)
        {
            _gameplayTagCountContainer.UpdateTagCount(gameplayTag, count);
        }

        /// <remarks>UE: UAbilitySystemComponent::AddLooseGameplayTags</remarks>
        public void AddLooseGameplayTags(GameplayTagContainer gameplayTags, int count = 1)
        {
            _gameplayTagCountContainer.UpdateTagCount(gameplayTags, count);
        }

        /// <remarks>UE: UAbilitySystemComponent::RemoveLooseGameplayTag</remarks>
        public void RemoveLooseGameplayTag(GameplayTag gameplayTag, int count = 1)
        {
            _gameplayTagCountContainer.UpdateTagCount(gameplayTag, -count);
        }

        /// <remarks>UE: UAbilitySystemComponent::RemoveLooseGameplayTags</remarks>
        public void RemoveLooseGameplayTags(GameplayTagContainer gameplayTags, int count = 1)
        {
            _gameplayTagCountContainer.UpdateTagCount(gameplayTags, -count);
        }

        /// <summary>Loose 태그 개수를 <paramref name="newCount"/>로 맞춘다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::SetLooseGameplayTagCount</remarks>
        public void SetLooseGameplayTagCount(GameplayTag gameplayTag, int newCount)
        {
            _gameplayTagCountContainer.SetTagCount(gameplayTag, newCount);
        }

        /// <summary>활성 GE가 부여 태그를 올리고 내릴 때 쓴다. Loose 태그와 같은 개수에 합쳐진다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::UpdateTagMap</remarks>
        internal void UpdateTagMap(GameplayTagContainer container, int countDelta)
        {
            // 부여 태그가 없는 GE가 대부분이라 빈 컨테이너는 개수 갱신 준비(지연 콜백 목록 할당)부터 건너뛴다.
            if (!container.IsEmpty())
            {
                _gameplayTagCountContainer.UpdateTagCount(container, countDelta);
            }
        }

        /// <summary>태그 개수가 바뀔 때 부를 콜백을 등록한다. 기본은 태그가 새로 생기거나 완전히 없어질 때만.</summary>
        /// <remarks>UE: UAbilitySystemComponent::RegisterGameplayTagEvent</remarks>
        public void RegisterGameplayTagEvent(GameplayTag tag, OnGameplayEffectTagCountChanged callback, GameplayTagEventType eventType = GameplayTagEventType.NewOrRemoved)
        {
            _gameplayTagCountContainer.RegisterGameplayTagEvent(tag, callback, eventType);
        }

        /// <returns>등록돼 있어서 뺐으면 true.</returns>
        /// <remarks>UE: UAbilitySystemComponent::UnregisterGameplayTagEvent</remarks>
        public bool UnregisterGameplayTagEvent(OnGameplayEffectTagCountChanged callback, GameplayTag tag, GameplayTagEventType eventType = GameplayTagEventType.NewOrRemoved)
        {
            return _gameplayTagCountContainer.UnregisterGameplayTagEvent(callback, tag, eventType);
        }

        // ── Context / Spec 팩토리 ─────────────────────────────────────────────────

        /// <summary>자신(this)을 Instigator로 하는 GE 컨텍스트를 만든다. SourceObject(무기·아이템 등)가 필요하면 호출처에서 AddSourceObject로 넣는다.</summary>
        public GameplayEffectContextHandle MakeEffectContext()
        {
            var context = new GameplayEffectContextHandle(new GameplayEffectContext());
            context.AddInstigator(this);
            return context;
        }

        /// <summary>GameplayEffectAsset(SO)로부터 적용 대기 상태의 Spec을 만든다.</summary>
        public GameplayEffectSpec MakeOutgoingSpec(GameplayEffectAsset effect, GameplayEffectContextHandle context = default, float level = 1f)
        {
            if (effect == null)
            {
                Debug.LogWarning($"[ASC] '{name}' 에서 GameplayEffectAsset이 null이라 Spec을 만들 수 없습니다.");
                return null;
            }

            if (!context.IsValid)
            {
                context = MakeEffectContext();
            }

            return new GameplayEffectSpec(effect, context, level);
        }

        /// <summary>GE의 AddBase 모디파이어를 지금 적용해도 어트리뷰트가 음수가 되지 않는지. 어빌리티 코스트 확인에 쓴다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::CanApplyAttributeModifiers</remarks>
        public bool CanApplyAttributeModifiers(GameplayEffectAsset effect, float level, GameplayEffectContextHandle context)
        {
            return _activeGameplayEffectsContainer.CanApplyAttributeModifiers(effect, level, context);
        }

        // ── Apply / Remove (컨테이너에 위임) ──────────────────────────────────────

        /// <summary>GameplayEffectAsset SO로 Spec을 생성해 자신에게 적용한다. Instant는 즉시 실행 후 Invalid Handle, Duration/Infinite는 핸들 반환.</summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToSelf(GameplayEffectAsset effect, GameplayEffectContextHandle context = default, float level = 1f)
        {
            return ApplyGameplayEffectSpecToSelf(MakeOutgoingSpec(effect, context, level));
        }

        /// <summary>이미 만들어진 Spec을 자신에게 적용한다. 실제 적용·캡처·실행은 컨테이너가 수행한다.</summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(GameplayEffectSpec spec)
        {
            return _activeGameplayEffectsContainer.ApplyGameplayEffectSpec(spec);
        }

        /// <summary>GameplayEffectAsset SO로 Spec을 생성해 대상 ASC에 적용한다(자신이 Instigator). context가 비면 자신을 Instigator로 생성 — AttributeBased Source 캡처용.</summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToTarget(GameplayEffectAsset effect, AbilitySystemComponent target, GameplayEffectContextHandle context = default, float level = 1f)
        {
            if (!context.IsValid)
            {
                context = MakeEffectContext();
            }

            return ApplyGameplayEffectSpecToTarget(MakeOutgoingSpec(effect, context, level), target);
        }

        /// <summary>이미 만들어진 Spec을 대상 ASC에 적용한다. 실제 적용은 대상 ASC가 자신에게 수행한다.</summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToTarget(GameplayEffectSpec spec, AbilitySystemComponent target)
        {
            if (target == null)
            {
                return ActiveGameplayEffectHandle.Invalid;
            }

            return target.ApplyGameplayEffectSpecToSelf(spec);
        }

        public bool RemoveActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            return _activeGameplayEffectsContainer.RemoveActiveGameplayEffect(handle);
        }

        /// <summary>활성 GE를 끄거나 켠다(제거하지 않음). 켜고 끄는 중에 GE가 제거됐으면 Invalid를 돌려준다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::SetActiveGameplayEffectInhibit</remarks>
        public ActiveGameplayEffectHandle SetActiveGameplayEffectInhibit(ActiveGameplayEffectHandle activeGameplayEffectHandle, bool inhibit)
        {
            return _activeGameplayEffectsContainer.SetActiveGameplayEffectInhibit(activeGameplayEffectHandle, inhibit);
        }

        /// <summary>핸들의 활성 GE. 없으면(제거됐거나 Instant) null.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveGameplayEffect</remarks>
        public ActiveGameplayEffect GetActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            return _activeGameplayEffectsContainer.GetActiveGameplayEffect(handle);
        }

        /// <summary>핸들의 활성 GE 수명 이벤트. 활성 GE가 없으면 null.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectEventSet</remarks>
        public ActiveGameplayEffectEvents GetActiveEffectEventSet(ActiveGameplayEffectHandle handle)
        {
            ActiveGameplayEffect active = _activeGameplayEffectsContainer.GetActiveGameplayEffect(handle);
            return active?.EventSet;
        }

        // ── 활성 GE 질의 (GameplayEffectQuery, 컨테이너에 위임) ───────────────────

        /// <summary>질의에 맞는 활성 GE 핸들.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffects</remarks>
        public List<ActiveGameplayEffectHandle> GetActiveEffects(GameplayEffectQuery query)
        {
            return _activeGameplayEffectsContainer.GetActiveEffects(query);
        }

        /// <summary>Asset Tags가 주어진 태그를 모두 가진 활성 GE 핸들.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsWithAllTags</remarks>
        public List<ActiveGameplayEffectHandle> GetActiveEffectsWithAllTags(GameplayTagContainer tags)
        {
            return GetActiveEffects(GameplayEffectQuery.MakeQuery_MatchAllEffectTags(tags));
        }

        /// <summary>질의에 맞는 켜진 GE 수.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetAggregatedStackCount</remarks>
        public int GetAggregatedStackCount(GameplayEffectQuery query)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectCount(query);
        }

        /// <summary>질의에 맞는 GE마다 남은 시간. 끝나는 시간이 없는 GE는 <see cref="GameplayEffectConstants.InfiniteDuration"/>.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsTimeRemaining</remarks>
        public List<float> GetActiveEffectsTimeRemaining(GameplayEffectQuery query)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectsTimeRemaining(query);
        }

        /// <summary>질의에 맞는 GE마다 지속시간. 끝나는 시간이 없는 GE는 <see cref="GameplayEffectConstants.InfiniteDuration"/>.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsDuration</remarks>
        public List<float> GetActiveEffectsDuration(GameplayEffectQuery query)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectsDuration(query);
        }

        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsTimeRemainingAndDuration</remarks>
        public List<(float TimeRemaining, float Duration)> GetActiveEffectsTimeRemainingAndDuration(GameplayEffectQuery query)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectsTimeRemainingAndDuration(query);
        }

        /// <summary>질의에 맞는 GE 중 가장 늦게 끝나는 시각(<see cref="Time.time"/> 기준).</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsEndTime</remarks>
        public float GetActiveEffectsEndTime(GameplayEffectQuery query)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectsEndTime(query, new List<AbilitySystemComponent>());
        }

        /// <summary>질의에 맞는 GE 중 가장 늦게 끝나는 시각. <paramref name="instigators"/>에 맞는 GE의 시전자를 더한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsEndTimeWithInstigators</remarks>
        public float GetActiveEffectsEndTimeWithInstigators(GameplayEffectQuery query, List<AbilitySystemComponent> instigators)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectsEndTime(query, instigators);
        }

        /// <remarks>UE: UAbilitySystemComponent::GetActiveEffectsEndTimeAndDuration</remarks>
        public bool GetActiveEffectsEndTimeAndDuration(GameplayEffectQuery query, out float endTime, out float duration)
        {
            return _activeGameplayEffectsContainer.GetActiveEffectsEndTimeAndDuration(query, out endTime, out duration, new List<AbilitySystemComponent>());
        }

        /// <summary>질의에 맞는 활성 GE를 제거한다.</summary>
        /// <param name="stacksToRemove">UE 시그니처 자리. GE 스택이 아직 없어 늘 통째로 제거한다.</param>
        /// <returns>제거한 GE 수.</returns>
        /// <remarks>UE: UAbilitySystemComponent::RemoveActiveEffects</remarks>
        public int RemoveActiveEffects(GameplayEffectQuery query, int stacksToRemove = -1)
        {
            return _activeGameplayEffectsContainer.RemoveActiveEffects(query, stacksToRemove);
        }

        /// <summary>Asset Tags가 주어진 태그와 하나라도 겹치는 활성 GE를 제거한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::RemoveActiveEffectsWithTags</remarks>
        public int RemoveActiveEffectsWithTags(GameplayTagContainer tags)
        {
            return RemoveActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(tags));
        }

        /// <summary>시전자 Spec 태그가 주어진 태그와 하나라도 겹치는 활성 GE를 제거한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::RemoveActiveEffectsWithSourceTags</remarks>
        public int RemoveActiveEffectsWithSourceTags(GameplayTagContainer tags)
        {
            return RemoveActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnySourceSpecTags(tags));
        }

        /// <summary>Owning 태그(Asset+Granted)가 주어진 태그와 하나라도 겹치는 활성 GE를 제거한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::RemoveActiveEffectsWithAppliedTags</remarks>
        public int RemoveActiveEffectsWithAppliedTags(GameplayTagContainer tags)
        {
            return RemoveActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(tags));
        }

        /// <summary>Owning 태그(Asset+Granted)가 주어진 태그와 하나라도 겹치는 활성 GE를 제거한다. UE도 <see cref="RemoveActiveEffectsWithAppliedTags"/>와 같은 질의를 쓴다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::RemoveActiveEffectsWithGrantedTags</remarks>
        public int RemoveActiveEffectsWithGrantedTags(GameplayTagContainer tags)
        {
            return RemoveActiveEffects(GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(tags));
        }

        /// <remarks>UE: UAbilitySystemComponent::SetActiveGameplayEffectLevel</remarks>
        public void SetActiveGameplayEffectLevel(ActiveGameplayEffectHandle handle, float newLevel)
        {
            _activeGameplayEffectsContainer.SetActiveGameplayEffectLevel(handle, newLevel);
        }

        /// <remarks>UE: UAbilitySystemComponent::SetActiveGameplayEffectLevelUsingQuery</remarks>
        public void SetActiveGameplayEffectLevelUsingQuery(GameplayEffectQuery query, float newLevel)
        {
            foreach (ActiveGameplayEffectHandle handle in GetActiveEffects(query))
            {
                SetActiveGameplayEffectLevel(handle, newLevel);
            }
        }

        /// <summary>aggregator가 dirty해질 때 그 OnDirty가 호출하는 진입점.</summary>
        internal void OnAttributeAggregatorDirty(AttributeAggregator aggregator, GameplayAttributeHandle gameplayAttributeHandle)
        {
            _activeGameplayEffectsContainer.OnAttributeAggregatorDirty(aggregator, gameplayAttributeHandle);
        }

        /// <summary>
        /// 소스 aggregator가 dirty해졌을 때 dirty 전파가 부르는 진입점 — 그에 non-snapshot 의존하는 활성 GE(<paramref name="handle"/>)의 magnitude를 재평가한다.
        /// aggregator는 dependent 핸들만 알아 그 핸들로 소유 ASC를 찾으므로 진입점이 ASC에 있고, 실제 재평가는 컨테이너에 위임한다.
        /// </summary>
        internal void OnMagnitudeDependencyChange(ActiveGameplayEffectHandle handle, AttributeAggregator changedAggregator)
        {
            _activeGameplayEffectsContainer.OnMagnitudeDependencyChange(handle, changedAggregator);
        }

        // ── AttributeData 접근 (컨테이너가 Owner를 통해 사용) ──────────────────────

        internal bool TryGetAttributeData(GameplayAttributeHandle handle, out AttributeData data)
        {
            if (_spawnedAttributeSets.TryGetValue(handle.SetType, out AttributeSet set))
            {
                return handle.TryGetData(set, out data);
            }

            data = default;
            return false;
        }

        // ── 초기화 ────────────────────────────────────────────────────────────────

        /// <summary>정의(SO)로 AttributeSet들을 생성해 등록한다. 같은 타입이 이미 있으면 건너뛴다(TryAdd). Awake 자체 초기화 외에 외부(예: BoxRoom)의 Source 캡처용 어트리뷰트 주입에도 쓴다.</summary>
        public void AddSet(AttributeDefinitionAsset data)
        {
            if (data == null)
            {
                return;
            }

            foreach (AttributeSetDefinition attributeSetData in data.AttributeSets)
            {
                Type attributeSetType = attributeSetData.GetAttributeSetType();
                if (attributeSetType == null || !typeof(AttributeSet).IsAssignableFrom(attributeSetType))
                {
                    Debug.LogWarning($"[ASC] '{attributeSetData.GetType().Name}' 에서 유효하지 않은 AttributeSet 타입.");
                    continue;
                }

                var set = (AttributeSet)Activator.CreateInstance(attributeSetType);
                foreach (AttributeFieldDefinition fieldData in attributeSetData.Attributes)
                {
                    FieldInfo field = attributeSetType.GetField(fieldData.FieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (field == null)
                    {
                        Debug.LogWarning($"[ASC] 필드 '{fieldData.FieldName}'을 {attributeSetType.Name}에서 찾을 수 없음.");
                        continue;
                    }
                    field.SetValue(set, fieldData.Data);
                }

                AddSpawnedAttribute(set);
            }
        }

        public bool HasAttributeSetForAttribute(GameplayAttributeHandle attributeHandle)
        {
            return attributeHandle.IsValid && GetAttributeSet(attributeHandle) != null;
        }

        // ── 어빌리티 ──────────────────────────────────────────────────────────────

        /// <summary>액터 정보를 초기화한다. Avatar가 바뀌면 부여된 어빌리티들에 <see cref="GameplayAbility.OnAvatarSet"/>을 알린다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::InitAbilityActorInfo</remarks>
        public void InitAbilityActorInfo(GameObject ownerActor, GameObject avatarActor)
        {
            if (ownerActor == null)
            {
                Debug.LogError("[ASC] InitAbilityActorInfo: ownerActor가 null.");
                return;
            }

            bool avatarChanged = avatarActor != AbilityActorInfo.AvatarActor;

            AbilityActorInfo.InitFromActor(ownerActor, avatarActor, this);

            if (!avatarChanged)
            {
                return;
            }

            using (new ScopedAbilityListLock(this))
            {
                foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
                {
                    GameplayAbility template = spec.Ability.AbilityTemplate;
                    if (template.InstancingPolicy == GameplayAbilityInstancingPolicy.InstancedPerActor)
                    {
                        // 인스턴스가 없으면 이미 해제됐거나, 만들어질 때 OnGiveAbility에서 알린다.
                        GameplayAbility abilityInstance = spec.GetPrimaryInstance();
                        abilityInstance?.OnAvatarSet(AbilityActorInfo, spec);
                    }
                    else
                    {
                        template.OnAvatarSet(AbilityActorInfo, spec);
                    }
                }
            }
        }

        /// <summary>
        /// 어빌리티를 부여한다. Per Actor면 정의의 원형을 복제해 이 ASC 전용 인스턴스를 만든다(Per Execution은 발동 때). 같은 정의라도 Spec마다 따로 부여된다.
        /// 정의가 null이거나 원형이 비었으면 Invalid. 스코프 락 중이면 보류하고 Spec의 핸들을 돌려준다(락이 풀릴 때 부여).
        /// </summary>
        /// <remarks>UE: UAbilitySystemComponent::GiveAbility</remarks>
        public GameplayAbilitySpecHandle GiveAbility(GameplayAbilitySpec spec)
        {
            if (spec == null || spec.Ability == null)
            {
                Debug.LogError("[ASC] GiveAbility: Spec 또는 어빌리티 정의가 null.");
                return GameplayAbilitySpecHandle.Invalid;
            }

            if (spec.Ability.AbilityTemplate == null)
            {
                Debug.LogError($"[ASC] GiveAbility: '{spec.Ability.name}'의 어빌리티 클래스가 비었거나 찾을 수 없음.");
                return GameplayAbilitySpecHandle.Invalid;
            }

            if (_abilityScopeLockCount > 0)
            {
                _abilityPendingAdds.Add(spec);
                return spec.Handle;
            }

            using (new ScopedAbilityListLock(this))
            {
                _activatableAbilities.Items.Add(spec);
                OnGiveAbility(spec);
            }

            return spec.Handle;
        }

        /// <summary>부여된 어빌리티를 제거한다. 없는 핸들이면 아무것도 하지 않는다. 스코프 락 중이면 보류한다(락이 풀릴 때 제거).</summary>
        /// <remarks>UE: UAbilitySystemComponent::ClearAbility</remarks>
        public void ClearAbility(GameplayAbilitySpecHandle handle)
        {
            for (int i = 0; i < _abilityPendingAdds.Count; i++)
            {
                if (_abilityPendingAdds[i].Handle.Equals(handle))
                {
                    RemoveAtSwap(_abilityPendingAdds, i);
                    return;
                }
            }

            List<GameplayAbilitySpec> items = _activatableAbilities.Items;
            for (int i = 0; i < items.Count; i++)
            {
                GameplayAbilitySpec spec = items[i];
                if (!spec.Handle.Equals(handle))
                {
                    continue;
                }

                if (_abilityScopeLockCount > 0)
                {
                    if (!spec.PendingRemove)
                    {
                        spec.PendingRemove = true;
                        _abilityPendingRemoves.Add(handle);
                    }
                }
                else
                {
                    // OnRemoveAbility가 다시 부여·해제를 부를 수 있어 잠근 채 알리고 지운다.
                    using (new ScopedAbilityListLock(this))
                    {
                        OnRemoveAbility(spec);
                        RemoveAtSwap(items, i);
                    }
                }

                return;
            }
        }

        // UE TArray::RemoveAtSwap — 마지막 요소를 빈 자리로 옮겨 지운다(순서 보존 안 함).
        private static void RemoveAtSwap<T>(List<T> list, int index)
        {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        /// <summary>부여된 어빌리티를 모두 제거한다. 스코프 락 중이면 락이 풀릴 때 처리한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::ClearAllAbilities</remarks>
        public void ClearAllAbilities()
        {
            if (_abilityScopeLockCount > 0)
            {
                _abilityPendingClearAll = true;
                return;
            }

            using (new ScopedAbilityListLock(this))
            {
                foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
                {
                    OnRemoveAbility(spec);
                }

                _activatableAbilities.Items.Clear();
                _abilityPendingClearAll = false;
            }
        }

        /// <summary>핸들의 Spec. 없으면 null. 기본은 해제가 보류된 Spec까지 찾는다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::FindAbilitySpecFromHandle</remarks>
        public GameplayAbilitySpec FindAbilitySpecFromHandle(GameplayAbilitySpecHandle handle, ConsiderPending considerPending = ConsiderPending.PendingRemove)
        {
            bool considerPendingRemove = (considerPending & ConsiderPending.PendingRemove) != 0;

            foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
            {
                if (spec.Handle.Equals(handle) && (!spec.PendingRemove || considerPendingRemove))
                {
                    return spec;
                }
            }

            if ((considerPending & ConsiderPending.PendingAdd) != 0)
            {
                foreach (GameplayAbilitySpec spec in _abilityPendingAdds)
                {
                    if (spec.Handle.Equals(handle) && (!spec.PendingRemove || considerPendingRemove))
                    {
                        return spec;
                    }
                }
            }

            return null;
        }

        /// <summary>정의로 부여된 첫 Spec. 없으면 null.</summary>
        /// <remarks>UE: UAbilitySystemComponent::FindAbilitySpecFromClass</remarks>
        public GameplayAbilitySpec FindAbilitySpecFromAsset(GameplayAbilityAsset ability)
        {
            foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
            {
                if (spec.Ability != null && spec.Ability == ability)
                {
                    return spec;
                }
            }

            return null;
        }

        /// <summary>부여된 어빌리티 전체(읽기 전용).</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetActivatableAbilities</remarks>
        public IReadOnlyList<GameplayAbilitySpec> GetActivatableAbilities()
        {
            return _activatableAbilities.Items;
        }

        /// <summary>
        /// 어빌리티 발동을 시도한다. 해제 대기 중이거나, Owner·Avatar가 없거나, <see cref="GameplayAbility.CanActivateAbility"/>가 거부하거나, Per Actor가 이미 활성이면 false.
        /// 그중 <see cref="GameplayAbility.CanActivateAbility"/> 거부만 <see cref="OnAbilityFailed"/>로 실패 사유와 함께 알린다.
        /// Per Execution은 발동마다 새 인스턴스를 만들어 여러 개가 동시에 활성일 수 있다.
        /// true면 <see cref="GameplayAbility.ActivateAbility"/>까지 불린 것이다(그 안에서 이미 끝났을 수도 있다).
        /// </summary>
        /// <remarks>UE: UAbilitySystemComponent::TryActivateAbility</remarks>
        public bool TryActivateAbility(GameplayAbilitySpecHandle handle)
        {
            GameplayAbilitySpec spec = FindAbilitySpecFromHandle(handle);
            if (spec == null)
            {
                Debug.LogWarning("[ASC] TryActivateAbility: 없는 핸들.");
                return false;
            }

            if (spec.PendingRemove)
            {
                return false;
            }

            if (AbilityActorInfo.OwnerActor == null || AbilityActorInfo.AvatarActor == null)
            {
                return false;
            }

            // 발동 중 콜백이 부여·해제를 불러도 이 Spec이 목록에서 빠지지 않게 잠근다.
            using (new ScopedAbilityListLock(this))
            {
                GameplayAbility template = spec.Ability.AbilityTemplate;
                bool instancedPerActor = template.InstancingPolicy == GameplayAbilityInstancingPolicy.InstancedPerActor;

                // Per Execution은 아직 인스턴스가 없어 원형이 판단한다.
                GameplayAbility instancedAbility = spec.GetPrimaryInstance();
                GameplayAbility abilitySource = instancedAbility ?? template;
                _internalTryActivateAbilityFailureTags.Reset();
                if (!abilitySource.CanActivateAbility(handle, AbilityActorInfo, _internalTryActivateAbilityFailureTags))
                {
                    // 재정의한 조건이 사유 없이 거부해도 거부됐다는 사실은 알린다.
                    if (_internalTryActivateAbilityFailureTags.IsEmpty())
                    {
                        GameplayAbilitiesSettings settings = GameplayAbilitiesSettings.Get();
                        _internalTryActivateAbilityFailureTags.AddTag(settings.ActivateFailCanActivateAbilityTag);
                    }

                    NotifyAbilityFailed(handle, abilitySource, _internalTryActivateAbilityFailureTags);
                    return false;
                }

                if (instancedPerActor)
                {
                    // 인스턴스가 하나라 활성 중에 다시 발동하면 진행 중인 상태를 덮어쓴다.
                    if (spec.IsActive)
                    {
                        return false;
                    }

                    if (instancedAbility == null)
                    {
                        Debug.LogWarning($"[ASC] TryActivateAbility: '{spec.Ability.name}'의 인스턴스가 없음.");
                        return false;
                    }
                }
                else
                {
                    instancedAbility = CreateNewInstanceOfAbility(spec);
                }

                instancedAbility.CallActivateAbility(handle, AbilityActorInfo);
            }

            return true;
        }

        /// <summary>정의로 부여된 Spec들을 모두 취소한다(활성 중인 것만 실제로 끝난다).</summary>
        /// <remarks>UE: UAbilitySystemComponent::CancelAbility</remarks>
        public void CancelAbility(GameplayAbilityAsset ability)
        {
            using (new ScopedAbilityListLock(this))
            {
                foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
                {
                    if (spec.Ability == ability)
                    {
                        CancelAbilitySpec(spec);
                    }
                }
            }
        }

        /// <summary>핸들의 어빌리티를 취소한다. 없는 핸들이거나 비활성이면 아무것도 하지 않는다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::CancelAbilityHandle</remarks>
        public void CancelAbilityHandle(GameplayAbilitySpecHandle handle)
        {
            using (new ScopedAbilityListLock(this))
            {
                foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
                {
                    if (spec.Handle.Equals(handle))
                    {
                        CancelAbilitySpec(spec);
                        return;
                    }
                }
            }
        }

        /// <summary>활성 중인 어빌리티를 모두 취소한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::CancelAllAbilities</remarks>
        public void CancelAllAbilities()
        {
            using (new ScopedAbilityListLock(this))
            {
                foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
                {
                    if (spec.IsActive)
                    {
                        CancelAbilitySpec(spec);
                    }
                }
            }
        }

        /// <summary>파괴 직전에 활성 어빌리티를 모두 취소하고 부여된 어빌리티를 모두 해제한다 — 어빌리티가 취소·해제 콜백에서 정리할 기회를 준다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::DestroyActiveState</remarks>
        private void DestroyActiveState()
        {
            // 씬 언로드처럼 함께 파괴되는 중이면 Owner가 먼저 사라졌을 수 있어, 그땐 취소 콜백이 죽은 Owner를 만지지 않게 건너뛴다.
            if (AbilityActorInfo.OwnerActor != null && AbilityActorInfo.AbilitySystemComponent != null)
            {
                CancelAllAbilities();
            }

            // 취소가 건너뛰어졌어도 해제는 남은 활성 인스턴스를 EndAbility로 끝낸다.
            ClearAllAbilities();
        }

        /// <summary>
        /// 어빌리티 태그로 활성 어빌리티를 취소한다. <paramref name="withTags"/> 중 하나라도 가지고 <paramref name="withoutTags"/>는 하나도 없는 것만 취소한다(null이면 그 조건은 통과).
        /// <paramref name="ignore"/> 인스턴스는 취소하지 않는다.
        /// </summary>
        /// <remarks>UE: UAbilitySystemComponent::CancelAbilities</remarks>
        public void CancelAbilities(GameplayTagContainer withTags = null, GameplayTagContainer withoutTags = null, GameplayAbility ignore = null)
        {
            using (new ScopedAbilityListLock(this))
            {
                foreach (GameplayAbilitySpec spec in _activatableAbilities.Items)
                {
                    if (!spec.IsActive || spec.Ability == null)
                    {
                        continue;
                    }

                    GameplayAbility template = spec.Ability.AbilityTemplate;
                    GameplayTagContainer abilityTags = template.AssetTags;
                    bool withTagPass = withTags == null || abilityTags.HasAny(withTags);
                    bool withoutTagPass = withoutTags == null || !abilityTags.HasAny(withoutTags);
                    if (withTagPass && withoutTagPass)
                    {
                        CancelAbilitySpec(spec, ignore);
                    }
                }
            }
        }

        /// <summary>어빌리티가 발동·종료하거나 차단을 켜고 끌 때 부른다 — 차단 태그를 막거나 풀고, 요청하면 취소 태그를 가진 활성 어빌리티를 취소한다(요청한 어빌리티는 빼고).</summary>
        /// <remarks>UE: UAbilitySystemComponent::ApplyAbilityBlockAndCancelTags</remarks>
        public virtual void ApplyAbilityBlockAndCancelTags(GameplayTagContainer abilityTags, GameplayAbility requestingAbility, bool enableBlockTags, GameplayTagContainer blockTags, bool executeCancelTags, GameplayTagContainer cancelTags)
        {
            if (enableBlockTags)
            {
                BlockAbilitiesWithTags(blockTags);
            }
            else
            {
                UnBlockAbilitiesWithTags(blockTags);
            }

            if (executeCancelTags)
            {
                CancelAbilities(cancelTags, null, requestingAbility);
            }
        }

        /// <summary>주어진 어빌리티 태그 중 하나라도 막혀 있는지. 막힌 태그의 자식 태그도 막힌다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::AreAbilityTagsBlocked</remarks>
        public virtual bool AreAbilityTagsBlocked(GameplayTagContainer tags)
        {
            return tags.HasAny(_blockedAbilityTags.GetExplicitGameplayTags());
        }

        /// <summary>어빌리티 태그를 막는다. 막은 만큼 풀어야 풀린다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::BlockAbilitiesWithTags</remarks>
        public void BlockAbilitiesWithTags(GameplayTagContainer tags)
        {
            _blockedAbilityTags.UpdateTagCount(tags, 1);
        }

        /// <remarks>UE: UAbilitySystemComponent::UnBlockAbilitiesWithTags</remarks>
        public void UnBlockAbilitiesWithTags(GameplayTagContainer tags)
        {
            _blockedAbilityTags.UpdateTagCount(tags, -1);
        }

        /// <summary>지금 막혀 있는 어빌리티 태그. 읽기 전용으로 쓴다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::GetBlockedAbilityTags</remarks>
        public GameplayTagContainer GetBlockedAbilityTags()
        {
            return _blockedAbilityTags.GetExplicitGameplayTags();
        }

        /// <remarks>UE: UAbilitySystemComponent::CancelAbilitySpec</remarks>
        private void CancelAbilitySpec(GameplayAbilitySpec spec, GameplayAbility ignore = null)
        {
            // 취소로 끝난 Per Execution 인스턴스는 Spec에서 빠지므로 사본을 돈다.
            var instances = new List<GameplayAbility>(spec.GetAbilityInstances());
            foreach (GameplayAbility instance in instances)
            {
                if (instance != ignore)
                {
                    instance.CancelAbility(spec.Handle, AbilityActorInfo);
                }
            }
        }

        /// <summary>발동을 알린다(<see cref="OnAbilityActivated"/>). <see cref="GameplayAbility.PreActivate"/>가 부른다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::NotifyAbilityActivated</remarks>
        public virtual void NotifyAbilityActivated(GameplayAbilitySpecHandle handle, GameplayAbility ability)
        {
            OnAbilityActivated?.Invoke(ability);
        }

        /// <summary>커밋을 알린다(<see cref="OnAbilityCommitted"/>). <see cref="GameplayAbility.CommitAbility"/>가 부른다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::NotifyAbilityCommit</remarks>
        public virtual void NotifyAbilityCommit(GameplayAbility ability)
        {
            OnAbilityCommitted?.Invoke(ability);
        }

        /// <summary>발동 실패를 사유와 함께 알린다(<see cref="OnAbilityFailed"/>).</summary>
        /// <remarks>UE: UAbilitySystemComponent::NotifyAbilityFailed</remarks>
        public virtual void NotifyAbilityFailed(GameplayAbilitySpecHandle handle, GameplayAbility ability, GameplayTagContainer failureReason)
        {
            OnAbilityFailed?.Invoke(ability, failureReason);
        }

        /// <summary>어빌리티가 끝났음을 받아 Spec의 활성 횟수를 줄이고, 종료를 알린 뒤, Per Execution 인스턴스면 Spec에서 뺀다. <see cref="GameplayAbility.EndAbility"/>가 부른다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::NotifyAbilityEnded</remarks>
        public virtual void NotifyAbilityEnded(GameplayAbilitySpecHandle handle, GameplayAbility ability, bool wasCancelled)
        {
            // 끝나는 도중 Spec이 해제됐으면 정리할 것이 없다.
            GameplayAbilitySpec spec = FindAbilitySpecFromHandle(handle);
            if (spec == null)
            {
                return;
            }

            if (spec.ActiveCount > 0)
            {
                spec.ActiveCount--;
            }
            else
            {
                Debug.LogError($"[ASC] NotifyAbilityEnded: '{ability.GetType().Name}'의 ActiveCount가 0 이하에서 종료됨.");
            }

            OnAbilityEnded?.Invoke(ability);
            OnAbilityEndedWithData?.Invoke(new AbilityEndedData(ability, handle, wasCancelled));

            // 구독자가 해제를 불렀으면 Spec이 이미 빠지며 인스턴스도 정리됐다.
            spec = FindAbilitySpecFromHandle(handle);
            if (spec == null)
            {
                return;
            }

            if (ability.InstancingPolicy == GameplayAbilityInstancingPolicy.InstancedPerExecution)
            {
                spec.RemoveInstance(ability);
            }
        }

        /// <remarks>UE: UAbilitySystemComponent::IncrementAbilityListLock</remarks>
        public void IncrementAbilityListLock()
        {
            _abilityScopeLockCount++;
        }

        /// <summary>락을 하나 푼다. 마지막 락이 풀리면 보류된 전체 해제, 또는 부여·해제를 처리한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::DecrementAbilityListLock</remarks>
        public void DecrementAbilityListLock()
        {
            if (--_abilityScopeLockCount != 0)
            {
                return;
            }

            if (_abilityPendingClearAll)
            {
                ClearAllAbilities();

                // 전체 해제는 수명 끝 정리일 가능성이 커서 같은 락 안의 부여보다 우선한다.
                if (_abilityPendingAdds.Count > 0)
                {
                    Debug.LogWarning("[ASC] 같은 스코프 락 안에서 GiveAbility와 ClearAllAbilities가 함께 호출돼 보류된 부여를 버리고 전체 해제를 우선한다.");
                    _abilityPendingAdds.Clear();
                }

                _abilityPendingRemoves.Clear();
            }
            else if (_abilityPendingAdds.Count > 0 || _abilityPendingRemoves.Count > 0)
            {
                // 처리 중 다시 보류될 수 있어 먼저 비운 사본을 돈다.
                var adds = new List<GameplayAbilitySpec>(_abilityPendingAdds);
                var removes = new List<GameplayAbilitySpecHandle>(_abilityPendingRemoves);
                _abilityPendingAdds.Clear();
                _abilityPendingRemoves.Clear();

                foreach (GameplayAbilitySpec spec in adds)
                {
                    GiveAbility(spec);
                }

                foreach (GameplayAbilitySpecHandle handle in removes)
                {
                    ClearAbility(handle);
                }
            }
        }

        /// <summary>부여 직후 Per Actor면 인스턴스를 만들고, 그 인스턴스(Per Execution이면 원형)에 알린다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::OnGiveAbility</remarks>
        protected virtual void OnGiveAbility(GameplayAbilitySpec spec)
        {
            GameplayAbility template = spec.Ability.AbilityTemplate;
            bool instancedPerActor = template.InstancingPolicy == GameplayAbilityInstancingPolicy.InstancedPerActor;
            if (instancedPerActor && spec.GetAbilityInstances().Count == 0)
            {
                CreateNewInstanceOfAbility(spec);
            }

            GameplayAbility primaryInstance = instancedPerActor ? spec.GetPrimaryInstance() : null;
            if (primaryInstance != null)
            {
                primaryInstance.OnGiveAbility(AbilityActorInfo, spec);
            }
            else
            {
                template.OnGiveAbility(AbilityActorInfo, spec);
            }
        }

        /// <summary>제거 직전 활성 인스턴스를 모두 끝내고(취소 아님), 대표 인스턴스(Per Execution이면 원형)에 알린 뒤 인스턴스를 떼어낸다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::OnRemoveAbility</remarks>
        protected virtual void OnRemoveAbility(GameplayAbilitySpec spec)
        {
            GameplayAbility template = spec.Ability.AbilityTemplate;
            if (template == null)
            {
                return;
            }

            // 끝날 때 Per Execution 인스턴스는 Spec에서 빠지므로 사본을 돈다.
            var instances = new List<GameplayAbility>(spec.GetAbilityInstances());
            foreach (GameplayAbility instance in instances)
            {
                if (instance.IsActive)
                {
                    instance.EndAbility(instance.CurrentAbilitySpecHandle, instance.CurrentActorInfo, false);
                }
            }

            // Per Actor인데 인스턴스가 없으면 이미 해제된 것이라 알리지 않는다.
            GameplayAbility primaryInstance = spec.GetPrimaryInstance();
            if (primaryInstance != null)
            {
                primaryInstance.OnRemoveAbility(AbilityActorInfo, spec);
            }
            else if (template.InstancingPolicy != GameplayAbilityInstancingPolicy.InstancedPerActor)
            {
                template.OnRemoveAbility(AbilityActorInfo, spec);
            }

            spec.ClearInstances();
        }

        /// <summary>정의의 원형을 복제해 이 ASC 전용 인스턴스를 만들어 Spec에 더한다. 원형의 저장된 필드 값으로 시작한다.</summary>
        /// <remarks>UE: UAbilitySystemComponent::CreateNewInstanceOfAbility</remarks>
        protected virtual GameplayAbility CreateNewInstanceOfAbility(GameplayAbilitySpec spec)
        {
            GameplayAbility abilityInstance = SerializedDeepCopy.Clone(spec.Ability.AbilityTemplate);
            abilityInstance.MarkInstantiated();
            spec.AddInstance(abilityInstance);
            return abilityInstance;
        }
    }
}
