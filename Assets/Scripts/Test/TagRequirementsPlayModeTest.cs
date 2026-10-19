using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.GameplayEffectComponents;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// PlayMode에서 GE 태그 연동(Granted Tags·Target Tag Requirements)이 시간이 흐르는 동안 맞게 도는지 본다 — ASC.Update가 컨테이너 Tick(만료·주기 실행)을 돌리는 실제 프레임 위에서.
    /// 에디터 셀프체크(GameplayEffectComponentSelfCheck)는 한 번에 호출해 보는 것이고, 여기선 만료로 부여 태그가 사라지거나 꺼진 주기 GE가 프레임이 지나도 실행되지 않는 것처럼 시간에 걸린 동작을 본다.
    /// GE는 리플렉션으로 메모리에 만든다(에셋 파일 없음). 저장 안 하는 씬에 이 컴포넌트만 붙이고 Play → <c>[TagPlayMode Self-Check] passed N / failed N</c>.
    /// </summary>
    public sealed class TagRequirementsPlayModeTest : MonoBehaviour
    {
        // GameplayEffectComponentSelfCheck의 native 테스트 태그(에디터 어셈블리)를 이름으로 찾는다
        private const string GrantedTagName = "Test.GEComponent.Other";
        private const string RemovalTagName = "Test.GEComponent.Asset.Child";

        private const float Eps = 0.001f;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<GameplayEffectAsset> _created = new();
        private AbilitySystemComponent _asc;
        private int _passed;
        private int _failed;

        private IEnumerator Start()
        {
            GameplayTag grantedTag = GameplayTag.RequestGameplayTag(GrantedTagName);
            GameplayTag removalTag = GameplayTag.RequestGameplayTag(RemovalTagName);
            if (!grantedTag.IsValid() || !removalTag.IsValid())
            {
                Debug.LogError("[TagPlayMode Self-Check] 테스트 태그를 찾지 못해 중단 — 에디터에서 Play해야 한다(native 태그가 에디터 어셈블리에 있다).");
                yield break;
            }

            _asc = gameObject.AddComponent<AbilitySystemComponent>();
            _asc.AddSpawnedAttribute(new CharacterAttributeSet { health = new AttributeData(100f, 100f) });

            yield return ExpiringGrantTurnsDependentOff(grantedTag);
            yield return InhibitedPeriodicDoesNotExecute(grantedTag);
            yield return RemovalWhileTicking(removalTag);
            yield return InhibitedDurationStillExpires(grantedTag);

            Check("P-9 정리: 활성 GE 0, Health 100, 테스트 태그 0", ActiveCount() == 0 && Approx(Health(), 100f) && _asc.GetTagCount(grantedTag) == 0 && _asc.GetTagCount(removalTag) == 0);

            foreach (GameplayEffectAsset ge in _created)
            {
                Destroy(ge);
            }

            Debug.Log($"[TagPlayMode Self-Check] passed {_passed} / failed {_failed}");
        }

        // 부여 GE가 만료되면 그 태그로 켜져 있던 GE가 꺼진다
        private IEnumerator ExpiringGrantTurnsDependentOff(GameplayTag grantedTag)
        {
            GameplayEffectAsset dependent = MakeEffect(GameplayEffectType.Infinite, 0f, 0f, 50f, MakeRequirements(ongoingRequire: grantedTag));
            GameplayEffectAsset expiringGrant = MakeEffect(GameplayEffectType.Duration, 0.3f, 0f, 0f, MakeGrant(grantedTag));

            ActiveGameplayEffectHandle dependentHandle = _asc.ApplyGameplayEffectToSelf(dependent);
            _asc.ApplyGameplayEffectToSelf(expiringGrant);
            Check("P-1 부여 GE(0.3초) 적용 중: 의존 GE 켜짐(Health 150)", Approx(Health(), 150f) && _asc.GetTagCount(grantedTag) == 1);

            yield return new WaitForSeconds(0.6f);
            Check("P-2 부여 GE 만료 → 태그 없어짐 → 의존 GE 꺼짐(100), 의존 GE는 활성 목록에 남음", Approx(Health(), 100f) && _asc.GetTagCount(grantedTag) == 0 && IsActive(dependentHandle) && IsInhibited(dependentHandle));

            _asc.RemoveActiveGameplayEffect(dependentHandle);
        }

        // 꺼진 주기 GE는 프레임이 지나도 실행되지 않고, 켜지면 다시 실행된다
        private IEnumerator InhibitedPeriodicDoesNotExecute(GameplayTag grantedTag)
        {
            GameplayEffectAsset periodic = MakeEffect(GameplayEffectType.Infinite, 0f, 0.1f, 10f, MakeRequirements(ongoingRequire: grantedTag));
            ActiveGameplayEffectHandle handle = _asc.ApplyGameplayEffectToSelf(periodic);

            yield return new WaitForSeconds(0.45f);
            Check("P-3 꺼진 주기 GE(0.1초): 0.45초 동안 실행 없음(Base 100)", Approx(_asc.GetAttributeBaseValue(CharacterAttributeSet.Health), 100f) && IsInhibited(handle));

            _asc.AddLooseGameplayTag(grantedTag);
            yield return new WaitForSeconds(0.45f);
            float baseAfterOn = _asc.GetAttributeBaseValue(CharacterAttributeSet.Health);
            Check($"P-4 태그 생겨 켜진 뒤 0.45초: 주기 실행 재개(Base > 100, 실제 {baseAfterOn})", baseAfterOn > 100f + Eps && !IsInhibited(handle));

            _asc.RemoveLooseGameplayTag(grantedTag);
            yield return null;
            float baseWhenOff = _asc.GetAttributeBaseValue(CharacterAttributeSet.Health);
            yield return new WaitForSeconds(0.45f);
            Check("P-5 태그 없어져 다시 꺼진 뒤 0.45초: 더 실행 안 됨", Approx(_asc.GetAttributeBaseValue(CharacterAttributeSet.Health), baseWhenOff) && IsInhibited(handle));

            _asc.RemoveActiveGameplayEffect(handle);
            _asc.SetAttributeBaseValue(CharacterAttributeSet.Health, 100f);
        }

        // Tick이 도는 중에 Removal 조건을 맞추면 제거되고, 그 뒤 Tick이 문제없이 돈다
        private IEnumerator RemovalWhileTicking(GameplayTag removalTag)
        {
            GameplayEffectAsset removable = MakeEffect(GameplayEffectType.Duration, 5f, 0.1f, 10f, MakeRequirements(removalRequire: removalTag));
            ActiveGameplayEffectHandle handle = _asc.ApplyGameplayEffectToSelf(removable);

            yield return new WaitForSeconds(0.25f);
            _asc.AddLooseGameplayTag(removalTag);
            Check("P-6 주기 GE가 도는 중 Removal 태그 생김 → 즉시 제거", !IsActive(handle));

            float baseAfterRemoval = _asc.GetAttributeBaseValue(CharacterAttributeSet.Health);
            yield return new WaitForSeconds(0.3f);
            Check("P-7 제거 뒤 Tick이 돌아도 더 실행 안 됨", Approx(_asc.GetAttributeBaseValue(CharacterAttributeSet.Health), baseAfterRemoval));

            _asc.RemoveLooseGameplayTag(removalTag);
            _asc.SetAttributeBaseValue(CharacterAttributeSet.Health, 100f);
        }

        // 꺼진 Duration GE도 시간이 다 되면 만료돼 제거된다(꺼짐은 수명을 멈추지 않는다)
        private IEnumerator InhibitedDurationStillExpires(GameplayTag grantedTag)
        {
            GameplayEffectAsset inhibitedDuration = MakeEffect(GameplayEffectType.Duration, 0.3f, 0f, 50f, MakeRequirements(ongoingRequire: grantedTag));
            ActiveGameplayEffectHandle handle = _asc.ApplyGameplayEffectToSelf(inhibitedDuration);
            bool startsOff = IsActive(handle) && IsInhibited(handle);

            yield return new WaitForSeconds(0.6f);
            Check("P-8 꺼진 채인 Duration GE(0.3초): 만료되면 활성 목록에서 빠짐", startsOff && !IsActive(handle) && Approx(Health(), 100f));
        }

        // ── 헬퍼 ──

        private float Health()
        {
            return _asc.GetAttributeCurrentValue(CharacterAttributeSet.Health);
        }

        // ASC가 컨테이너를 공개하지 않아 셀프체크들처럼 리플렉션으로 연다
        private int ActiveCount()
        {
            FieldInfo containerField = typeof(AbilitySystemComponent).GetField("_activeGameplayEffectsContainer", NonPublicInstance);
            var container = (ActiveGameplayEffectsContainer)containerField.GetValue(_asc);
            return container.ActiveGameplayEffects.Count;
        }

        private bool IsActive(ActiveGameplayEffectHandle handle)
        {
            return _asc.GetActiveGameplayEffect(handle) != null;
        }

        private bool IsInhibited(ActiveGameplayEffectHandle handle)
        {
            ActiveGameplayEffect active = _asc.GetActiveGameplayEffect(handle);
            return active != null && active.IsInhibited;
        }

        private static TargetTagRequirementsGameplayEffectComponent MakeRequirements(GameplayTag ongoingRequire = default, GameplayTag removalRequire = default)
        {
            var component = new TargetTagRequirementsGameplayEffectComponent();
            if (ongoingRequire.IsValid())
            {
                GameplayTagContainer requireTags = component.OngoingTagRequirements.RequireTags;
                requireTags.AddTag(ongoingRequire);
            }

            if (removalRequire.IsValid())
            {
                GameplayTagContainer requireTags = component.RemovalTagRequirements.RequireTags;
                requireTags.AddTag(removalRequire);
            }

            return component;
        }

        private static TargetTagsGameplayEffectComponent MakeGrant(GameplayTag tag)
        {
            var component = new TargetTagsGameplayEffectComponent();
            InheritedTagContainer grantedTags = component.GetConfiguredTargetTagChanges();
            grantedTags.AddTag(tag);
            return component;
        }

        // Health += magnitude 한 줄짜리 GE
        private GameplayEffectAsset MakeEffect(GameplayEffectType type, float duration, float period, float magnitude, GameplayEffectComponent component)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", type);
            SetPrivateField(ge, "duration", duration);
            SetPrivateField(ge, "period", period);
            SetPrivateField(ge, "executePeriodicEffectOnApplication", false);

            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);

            object modifierBoxed = default(GameplayModifier);
            SetPrivateField(modifierBoxed, "attribute", new GameplayAttribute(CharacterAttributeSet.Health));
            SetPrivateField(modifierBoxed, "operation", GameplayModifierOperation.AddBase);
            SetPrivateField(modifierBoxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);

            SetPrivateField(ge, "modifiers", new List<GameplayModifier> { (GameplayModifier)modifierBoxed });
            SetPrivateField(ge, "gameplayEffectComponents", new List<GameplayEffectComponent> { component });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, NonPublicInstance);
            if (field == null)
            {
                throw new InvalidOperationException($"필드 '{name}'을 {target.GetType().Name}에서 찾지 못함.");
            }

            field.SetValue(target, value);
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
                Debug.Log($"[TagPlayMode PASS] {label}");
            }
            else
            {
                _failed++;
                Debug.LogError($"[TagPlayMode FAIL] {label}");
            }
        }
    }
}
