using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem.Aggregator;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Core.AbilitySystem
{
    public class AbilitySystemComponent : MonoBehaviour
    {
        [SerializeField] private AttributeDefinitionAsset attributeInitData;

        private readonly Dictionary<Type, AttributeSet> _spawnedAttributeSets = new();

        // 활성 GE의 소유·수명·실행·집계 + 반응성 aggregator를 이 컨테이너가 맡는다. ASC는 공개 API 위임 래퍼와 AttributeSet 저장만 남긴다.
        private ActiveGameplayEffectsContainer _activeGameplayEffectsContainer;

        private void Awake()
        {
            _activeGameplayEffectsContainer = new ActiveGameplayEffectsContainer(this);

            // AddSet은 AttributeSet 필드에 값을 직접 세팅할 뿐 CurrentValue 갱신 경로를 거치지 않아
            // 초기화 중 AttributeChanged가 발화하지 않는다(불필요한 재평가 없음).
            AddSet(attributeInitData);
        }

        private void Update()
        {
            _activeGameplayEffectsContainer.Tick(Time.deltaTime);
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

        // ── Context / Spec 팩토리 ─────────────────────────────────────────────────

        /// <summary>
        /// 자신(this)을 Instigator(주체 ASC)로 하는 GE 컨텍스트를 만든다.
        /// SourceObject(무기·아이템 등 출처)가 필요하면 호출처에서 AddSourceObject로 따로 넣는다.
        /// </summary>
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

        // ── Apply / Remove (컨테이너에 위임) ──────────────────────────────────────

        /// <summary>
        /// GameplayEffectAsset SO로부터 Spec을 생성해 자신에게 적용한다.
        /// Instant는 즉시 실행 후 Invalid Handle 반환. Duration/Infinite는 핸들 반환.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToSelf(GameplayEffectAsset effect, GameplayEffectContextHandle context = default, float level = 1f)
        {
            return ApplyGameplayEffectSpecToSelf(MakeOutgoingSpec(effect, context, level));
        }

        /// <summary>이미 만들어진 Spec을 자신에게 적용한다. 실제 적용·캡처·실행은 컨테이너가 수행한다.</summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(GameplayEffectSpec spec)
        {
            return _activeGameplayEffectsContainer.ApplyGameplayEffectSpec(spec);
        }

        /// <summary>
        /// GameplayEffectAsset SO로부터 Spec을 생성해 대상 ASC에 적용한다. 자신(this)이 Instigator가 된다.
        /// context가 비어 있으면 자신을 Instigator로 하는 context를 생성한다(AttributeBased의 Source 캡처용).
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectToTarget(GameplayEffectAsset effect, AbilitySystemComponent target, GameplayEffectContextHandle context = default, float level = 1f)
        {
            if (!context.IsValid)
            {
                context = MakeEffectContext();
            }

            return ApplyGameplayEffectSpecToTarget(MakeOutgoingSpec(effect, context, level), target);
        }

        /// <summary>
        /// 이미 만들어진 Spec을 대상 ASC에 적용한다. 실제 적용은 대상 ASC가 자신에게 수행한다.
        /// </summary>
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

        /// <summary>aggregator가 dirty해질 때 그 OnDirty가 호출하는 진입점.</summary>
        internal void OnAttributeAggregatorDirty(AttributeAggregator aggregator, GameplayAttributeHandle gameplayAttributeHandle)
        {
            _activeGameplayEffectsContainer.OnAttributeAggregatorDirty(aggregator, gameplayAttributeHandle);
        }

        /// <summary>
        /// 소스 aggregator가 dirty해졌을 때 그 dirty 전파가 호출하는 진입점 — 그 aggregator에 non-snapshot으로 의존하는
        /// 활성 GE(<paramref name="handle"/>)의 magnitude를 재평가한다. aggregator는 dependent 핸들만 알아 그 핸들로 소유 ASC를
        /// 찾아 호출하므로 진입점이 ASC에 있고, 실제 재평가는 컨테이너에 위임한다.
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

        /// <summary>
        /// 주어진 정의(SO)로 AttributeSet들을 생성해 등록한다. 이미 같은 타입이 등록돼 있으면 건너뛴다(AddSpawnedAttribute=TryAdd).
        /// Awake의 자체 초기화(attributeInitData) 외에, 외부(예: BoxRoom)가 Source 캡처용 어트리뷰트를 주입할 때도 쓴다.
        /// </summary>
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
    }
}
