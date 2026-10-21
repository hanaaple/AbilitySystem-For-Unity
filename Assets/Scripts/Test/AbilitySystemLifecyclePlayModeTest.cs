using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Game.Character;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// PlayMode에서 ASC가 붙은 GameObject의 복제(Instantiate)·파괴가 UE와 같은 결과를 내는지 본다 — Awake·OnDestroy는 Play 중에만 불려 에디터 셀프체크로는 못 본다.
    /// C: 복제 — 설정(attributeInitData)은 따라가고, 실행 중 상태(현재 값·어빌리티·GE·태그)는 따라가지 않는다.
    /// D: 파괴 — 활성 어빌리티는 모두 취소되고, 부여된 어빌리티는 모두 해제된다.
    /// X: ASC 간 연결 — 라이브 캡처로 이어진 상대 ASC가 파괴돼도 남은 쪽이 에러 없이 돈다.
    /// 정의는 리플렉션으로 메모리에 만든다(에셋 파일 없음). Play 중인 씬에 이 컴포넌트만 붙이면 → <c>[LifecyclePlayMode Self-Check] passed N / failed N</c>.
    /// </summary>
    public sealed class AbilitySystemLifecyclePlayModeTest : MonoBehaviour
    {
        // GameplayAbilitySelfCheck의 native 테스트 태그(에디터 어셈블리)를 이름으로 찾는다
        private const string LooseTagName = "Test.GA.Owned";

        private const float Eps = 0.001f;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<ScriptableObject> _created = new();
        private int _passed;
        private int _failed;
        private int _errorLogs;

        private IEnumerator Start()
        {
            GameplayTag looseTag = GameplayTag.RequestGameplayTag(LooseTagName);
            if (!looseTag.IsValid())
            {
                Debug.LogError("[LifecyclePlayMode Self-Check] 테스트 태그를 찾지 못해 중단 — 에디터에서 Play해야 한다(native 태그가 에디터 어셈블리에 있다).");
                yield break;
            }

            Application.logMessageReceived += CountErrorLogs;

            yield return CloneKeepsConfigOnly(looseTag);
            yield return DestroyCancelsAndClearsAbilities();
            yield return DestroyedPeerIsDroppedFromLiveCapture();

            Application.logMessageReceived -= CountErrorLogs;

            foreach (ScriptableObject obj in _created)
            {
                Destroy(obj);
            }

            Debug.Log($"[LifecyclePlayMode Self-Check] passed {_passed} / failed {_failed}");
        }

        // ── C: 복제 ──────────────────────────────────────────────────────────────

        private IEnumerator CloneKeepsConfigOnly(GameplayTag looseTag)
        {
            AttributeDefinitionAsset initData = MakeInitData(("stamina", 50f), ("speed", 10f));
            AbilitySystemComponent source = MakeAsc("Lifecycle_Source", initData);
            Check("C-0 원본은 attributeInitData로 초기화(Stamina 50, Speed 10)", Approx(Stamina(source), 50f) && Approx(Speed(source), 10f));

            source.SetAttributeBaseValue(CharacterAttributeSet.Stamina, 33f);
            source.AddLooseGameplayTag(looseTag);
            source.GiveAbility(new GameplayAbilitySpec(MakeAbilityAsset(new LingeringAbility { Name = "CloneSource" })));
            source.ApplyGameplayEffectToSelf(MakeEffect(GameplayEffectType.Infinite, MakeModifier(CharacterAttributeSet.Speed, 5f)));

            GameObject cloneObject = Instantiate(source.gameObject);
            cloneObject.name = "Lifecycle_Clone";
            var clone = cloneObject.GetComponent<AbilitySystemComponent>();

            Check("C-1 복제본 어트리뷰트는 원본 현재값(33·15)이 아니라 설정 초기값(Stamina 50, Speed 10)", Approx(Stamina(clone), 50f) && Approx(Speed(clone), 10f) && Approx(Stamina(source), 33f) && Approx(Speed(source), 15f));
            Check("C-2 복제본엔 어빌리티·태그·GE가 없음", clone.GetActivatableAbilities().Count == 0 && clone.GetTagCount(looseTag) == 0 && source.GetActivatableAbilities().Count == 1 && source.GetTagCount(looseTag) == 1);
            Check("C-3 복제본 GameObject 정보는 자기 자신(Owner·Avatar·ASC)", clone.AbilityGameObjectInfo.OwnerGameObject == cloneObject && clone.AbilityGameObjectInfo.AvatarGameObject == cloneObject && clone.AbilityGameObjectInfo.AbilitySystemComponent == clone);
            Check("C-4 설정(attributeInitData)은 같은 에셋을 가리킴", GetPrivateField(clone, "attributeInitData") == (object)initData);

            clone.SetAttributeBaseValue(CharacterAttributeSet.Stamina, 1f);
            clone.ApplyGameplayEffectToSelf(MakeEffect(GameplayEffectType.Infinite, MakeModifier(CharacterAttributeSet.Speed, 100f)));
            Check("C-5 복제본을 바꿔도 원본은 그대로(Stamina 33, Speed 15)", Approx(Stamina(source), 33f) && Approx(Speed(source), 15f) && Approx(Stamina(clone), 1f) && Approx(Speed(clone), 110f));

            Destroy(source.gameObject);
            Destroy(cloneObject);
            yield return null;
        }

        // ── D: 파괴 ──────────────────────────────────────────────────────────────

        private IEnumerator DestroyCancelsAndClearsAbilities()
        {
            LingeringAbility.Log.Clear();
            AbilitySystemComponent asc = MakeAsc("Lifecycle_Destroy", null);

            GameplayAbilitySpecHandle actorHandle = asc.GiveAbility(new GameplayAbilitySpec(MakeAbilityAsset(new LingeringAbility { Name = "Actor" }, GameplayAbilityInstancingPolicy.InstancedPerGameObject)));
            GameplayAbilitySpecHandle execHandle = asc.GiveAbility(new GameplayAbilitySpec(MakeAbilityAsset(new LingeringAbility { Name = "Exec" })));
            asc.GiveAbility(new GameplayAbilitySpec(MakeAbilityAsset(new LingeringAbility { Name = "Idle" }, GameplayAbilityInstancingPolicy.InstancedPerGameObject)));
            GameplayAbilitySpecHandle reentrantHandle = asc.GiveAbility(new GameplayAbilitySpec(MakeAbilityAsset(new LingeringAbility { Name = "Reentrant", DestroyOwnerOnCancel = true }, GameplayAbilityInstancingPolicy.InstancedPerGameObject)));

            bool activated = asc.TryActivateAbility(actorHandle) && asc.TryActivateAbility(execHandle) && asc.TryActivateAbility(execHandle) && asc.TryActivateAbility(reentrantHandle);
            Check("D-0 Spec 4개 — 활성 발동 4번(Per GameObject 1·Per Execution 2·취소 중 Owner를 다시 파괴하는 것 1), 비활성 1개", activated && asc.GetActivatableAbilities().Count == 4);

            int errorsBefore = _errorLogs;
            Destroy(asc.gameObject);
            yield return null;

            Check("D-1 파괴되면 활성 어빌리티가 모두 취소로 끝남(정상 종료 없음)", Count("Actor.Cancel") == 1 && Count("Exec.Cancel") == 2 && Count("Reentrant.Cancel") == 1 && !LingeringAbility.Log.Exists(entry => entry.EndsWith(".End")));
            Check("D-2 부여된 어빌리티가 모두 해제됨(비활성 포함, Spec마다 OnRemove 1번)", Count("Actor.OnRemove") == 1 && Count("Exec.OnRemove") == 1 && Count("Idle.OnRemove") == 1 && Count("Reentrant.OnRemove") == 1 && asc.GetActivatableAbilities().Count == 0);
            Check("D-3 취소가 해제보다 먼저", LingeringAbility.Log.IndexOf("Actor.Cancel") < LingeringAbility.Log.IndexOf("Actor.OnRemove"));
            Check("D-4 취소 콜백 안에서 Owner를 다시 파괴해도 에러 없이 한 번만 정리", Count("Reentrant.Cancel") == 1 && Count("Reentrant.OnRemove") == 1 && _errorLogs == errorsBefore);
        }

        // ── X: ASC 간 연결 ───────────────────────────────────────────────────────

        private IEnumerator DestroyedPeerIsDroppedFromLiveCapture()
        {
            AbilitySystemComponent source = MakeAsc("Lifecycle_LiveSource", null);
            AbilitySystemComponent target = MakeAsc("Lifecycle_LiveTarget", null);
            source.SetAttributeBaseValue(CharacterAttributeSet.Health, 100f);

            // 대상 Speed += Source Health(라이브) — Source가 바뀌면 대상 GE가 다시 계산된다
            var liveCapture = new GameplayEffectAttributeCaptureDefinition(CharacterAttributeSet.Health, AttributeCaptureSource.Source, false);
            GameplayEffectAsset liveEffect = MakeEffect(GameplayEffectType.Infinite, MakeAttributeBasedModifier(CharacterAttributeSet.Speed, liveCapture));
            source.ApplyGameplayEffectToTarget(liveEffect, target, source.MakeEffectContext());
            source.SetAttributeBaseValue(CharacterAttributeSet.Health, 150f);
            Check("X-1 라이브 캡처 연결 — Source Health 100 → 150이면 대상 Speed 150", Approx(Speed(target), 150f));

            int errorsBefore = _errorLogs;
            Destroy(target.gameObject);
            yield return null;
            source.SetAttributeBaseValue(CharacterAttributeSet.Health, 200f);
            Check("X-2 대상 ASC가 파괴된 뒤 Source 값이 바뀌어도 에러 없음(죽은 의존자는 빠짐)", _errorLogs == errorsBefore && Approx(source.GetAttributeCurrentValue(CharacterAttributeSet.Health), 200f));

            Destroy(source.gameObject);
            yield return null;
        }

        // ── 도우미 ────────────────────────────────────────────────────────────────

        // 취소·종료·해제를 기록하고, 발동 뒤 스스로 끝나지 않고 활성으로 남는 테스트 어빌리티
        [Serializable]
        private sealed class LingeringAbility : GameplayAbility
        {
            public static readonly List<string> Log = new();

            public string Name;
            public bool DestroyOwnerOnCancel;

            public override void EndAbility(GameplayAbilitySpecHandle handle, GameplayAbilityGameObjectInfo gameObjectInfo, bool wasCancelled)
            {
                if (IsEndAbilityValid(handle, gameObjectInfo))
                {
                    Log.Add(wasCancelled ? $"{Name}.Cancel" : $"{Name}.End");
                    if (wasCancelled && DestroyOwnerOnCancel)
                    {
                        UnityEngine.Object.Destroy(gameObjectInfo.OwnerGameObject);
                    }
                }

                base.EndAbility(handle, gameObjectInfo, wasCancelled);
            }

            public override void OnRemoveAbility(GameplayAbilityGameObjectInfo gameObjectInfo, GameplayAbilitySpec spec)
            {
                Log.Add($"{Name}.OnRemove");
            }
        }

        private static int Count(string entry)
        {
            return LingeringAbility.Log.FindAll(logged => logged == entry).Count;
        }

        private void CountErrorLogs(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (!message.Contains("[LifecyclePlayMode FAIL]"))
                {
                    _errorLogs++;
                }
            }
        }

        // 비활성으로 만들어 Awake 전에 설정을 넣고 켠다 — 씬에 놓인 오브젝트가 직렬화된 설정으로 깨어나는 것과 같은 순서.
        private static AbilitySystemComponent MakeAsc(string name, AttributeDefinitionAsset initData)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var asc = go.AddComponent<AbilitySystemComponent>();
            SetPrivateField(asc, "attributeInitData", initData);
            go.SetActive(true);

            if (initData == null)
            {
                asc.AddSpawnedAttribute(new CharacterAttributeSet());
            }

            return asc;
        }

        private AttributeDefinitionAsset MakeInitData(params (string FieldName, float BaseValue)[] fields)
        {
            var attributes = new List<AttributeFieldDefinition>();
            foreach ((string fieldName, float baseValue) in fields)
            {
                var field = new AttributeFieldDefinition();
                SetPrivateField(field, "fieldName", fieldName);
                SetPrivateField(field, "baseValue", baseValue);
                attributes.Add(field);
            }

            var setDefinition = new AttributeSetDefinition();
            SetPrivateField(setDefinition, "attributeSetTypeName", typeof(CharacterAttributeSet).AssemblyQualifiedName);
            SetPrivateField(setDefinition, "attributes", attributes);

            var initData = ScriptableObject.CreateInstance<AttributeDefinitionAsset>();
            SetPrivateField(initData, "attributeSets", new List<AttributeSetDefinition> { setDefinition });

            _created.Add(initData);
            return initData;
        }

        private GameplayAbilityAsset MakeAbilityAsset(GameplayAbility template, GameplayAbilityInstancingPolicy policy = GameplayAbilityInstancingPolicy.InstancedPerExecution)
        {
            SetPrivateField(template, "instancingPolicy", policy);

            var asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            SetPrivateField(asset, "abilityTemplate", template);

            _created.Add(asset);
            return asset;
        }

        private GameplayEffectAsset MakeEffect(GameplayEffectType type, GameplayModifier modifier)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", type);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier> { modifier });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private static GameplayModifier MakeModifier(GameplayAttributeHandle attribute, float magnitude)
        {
            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);
            return MakeModifier(attribute, (GameplayEffectModifierMagnitude)magnitudeBoxed);
        }

        private static GameplayModifier MakeAttributeBasedModifier(GameplayAttributeHandle attribute, GameplayEffectAttributeCaptureDefinition captureDefinition)
        {
            var attributeBased = new AttributeBasedMagnitude();
            SetPrivateField(attributeBased, "backingAttribute", captureDefinition);
            SetPrivateField(attributeBased, "captureValueType", AttributeCaptureValueType.CurrentValue);
            SetPrivateField(attributeBased, "coefficient", 1f);

            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.AttributeBased);
            SetPrivateField(magnitudeBoxed, "attributeBasedMagnitude", attributeBased);
            return MakeModifier(attribute, (GameplayEffectModifierMagnitude)magnitudeBoxed);
        }

        private static GameplayModifier MakeModifier(GameplayAttributeHandle attribute, GameplayEffectModifierMagnitude magnitude)
        {
            object modifierBoxed = default(GameplayModifier);
            SetPrivateField(modifierBoxed, "attribute", new GameplayAttribute(attribute));
            SetPrivateField(modifierBoxed, "operation", GameplayModifierOperation.AddBase);
            SetPrivateField(modifierBoxed, "modifierMagnitude", magnitude);
            return (GameplayModifier)modifierBoxed;
        }

        private static float Stamina(AbilitySystemComponent asc)
        {
            return asc.GetAttributeCurrentValue(CharacterAttributeSet.Stamina);
        }

        private static float Speed(AbilitySystemComponent asc)
        {
            return asc.GetAttributeCurrentValue(CharacterAttributeSet.Speed);
        }

        // 기반 클래스의 private 필드(GameplayAbility의 정책 칸)도 찾도록 상속 계층을 올라간다.
        private static FieldInfo FindPrivateField(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, NonPublicInstance);
                if (field != null)
                {
                    return field;
                }
            }

            throw new InvalidOperationException($"필드 '{name}'을 {type.Name}에서 찾지 못함.");
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FindPrivateField(target.GetType(), name).SetValue(target, value);
        }

        private static object GetPrivateField(object target, string name)
        {
            return FindPrivateField(target.GetType(), name).GetValue(target);
        }

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < Eps;
        }

        private void Check(string label, bool condition)
        {
            if (condition)
            {
                _passed++;
                Debug.Log($"[LifecyclePlayMode PASS] {label}");
            }
            else
            {
                _failed++;
                Debug.LogError($"[LifecyclePlayMode FAIL] {label}");
            }
        }
    }
}
