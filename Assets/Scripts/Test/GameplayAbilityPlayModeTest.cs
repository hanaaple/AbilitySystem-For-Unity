using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.GameplayEffectComponents;
using Core.AbilitySystem.Tag;
using Game.Character;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// PlayMode에서 <see cref="TestGameplayAbility"/>의 부여 → 발동 → 커밋 → 효과 → 종료와 쿨다운 만료가 실제 프레임 위에서 맞게 도는지 본다 — ASC.Update가 컨테이너 Tick을 돌린다.
    /// 에디터 셀프체크(GameplayAbilitySelfCheck M)는 Tick을 직접 불러 시간을 흘리고, 여기선 진짜 시간이 흐른다.
    /// GE·GA 정의는 리플렉션으로 메모리에 만든다(에셋 파일 없음). Play 중인 씬에 이 컴포넌트만 붙이면 → <c>[GAPlayMode Self-Check] passed N / failed N</c>.
    /// </summary>
    public sealed class GameplayAbilityPlayModeTest : MonoBehaviour
    {
        // GameplayAbilitySelfCheck의 native 테스트 태그(에디터 어셈블리)를 이름으로 찾는다
        private const string CooldownTagName = "Test.GA.Cooldown";

        // 에디터가 포커스를 잃으면 한 프레임이 Time.maximumDeltaTime까지 길어져, 중간 대기 + 한 프레임이 쿨다운을 넘지 않을 만큼 길게 둔다.
        private const float CooldownDuration = 3f;
        private const float PartialWait = 1f;
        private const float Eps = 0.001f;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<ScriptableObject> _created = new();
        private AbilitySystemComponent _asc;
        private int _passed;
        private int _failed;

        private IEnumerator Start()
        {
            GameplayTag cooldownTag = GameplayTag.RequestGameplayTag(CooldownTagName);
            if (!cooldownTag.IsValid())
            {
                Debug.LogError("[GAPlayMode Self-Check] 테스트 태그를 찾지 못해 중단 — 에디터에서 Play해야 한다(native 태그가 에디터 어셈블리에 있다).");
                yield break;
            }

            _asc = gameObject.AddComponent<AbilitySystemComponent>();
            _asc.AddSpawnedAttribute(new CharacterAttributeSet { stamina = new AttributeData(50f, 50f), speed = new AttributeData(0f, 0f) });

            GameplayEffectAsset costEffect = MakeInstantEffect(CharacterAttributeSet.Stamina, -30f);
            GameplayEffectAsset cooldownEffect = MakeCooldownEffect(cooldownTag);
            GameplayEffectAsset speedEffect = MakeInstantEffect(CharacterAttributeSet.Speed, 5f);
            GameplayAbilityAsset asset = MakeTestAbilityAsset(costEffect, cooldownEffect, speedEffect);

            var spec = new GameplayAbilitySpec(asset);
            GameplayAbilitySpecHandle handle = _asc.GiveAbility(spec);
            GameplayAbility template = asset.AbilityTemplate;

            bool activated = _asc.TryActivateAbility(handle);
            Check("P-1 발동 → 커밋(Stamina 50 → 20, 쿨다운 태그) → 효과(Speed 0 → 5) → 종료", activated && Approx(Stamina(), 20f) && Approx(Speed(), 5f) && _asc.HasMatchingGameplayTag(cooldownTag) && !spec.IsActive);
            Check("P-2 같은 프레임에 다시 발동 — 쿨다운 중이라 실패", !_asc.TryActivateAbility(handle) && Approx(Stamina(), 20f));

            float committedAt = Time.time;
            float commitFrameDelta = Time.deltaTime;
            int commitFrame = Time.frameCount;
            float remainingAtCommitFrameEnd = -1f;
            yield return new WaitForEndOfFrame();
            if (Time.frameCount == commitFrame)
            {
                remainingAtCommitFrameEnd = template.GetCooldownTimeRemaining(_asc.AbilityActorInfo);
            }

            yield return new WaitForSeconds(PartialWait);
            float remaining = template.GetCooldownTimeRemaining(_asc.AbilityActorInfo);
            float elapsed = Time.time - committedAt;
            Check($"P-3 {elapsed:0.00}초 흐름 — 남은 쿨다운 {remaining:0.00}(= {CooldownDuration} - 흐른 시간), 여전히 쿨다운 태그 [커밋 프레임 deltaTime {commitFrameDelta:0.000}, 그 프레임 끝 남은 쿨다운 {remainingAtCommitFrameEnd:0.000}]", remaining > 0f && Mathf.Abs(remaining - (CooldownDuration - elapsed)) < 0.05f && (remainingAtCommitFrameEnd < 0f || Approx(remainingAtCommitFrameEnd, CooldownDuration)) && _asc.HasMatchingGameplayTag(cooldownTag));

            yield return new WaitForSeconds(CooldownDuration);
            Check("P-4 쿨다운 Duration이 지나면 프레임 Tick으로 쿨다운 태그가 빠지고 남은 쿨다운 0", !_asc.HasMatchingGameplayTag(cooldownTag) && Approx(template.GetCooldownTimeRemaining(_asc.AbilityActorInfo), 0f));

            _asc.SetAttributeBaseValue(CharacterAttributeSet.Stamina, 50f);
            bool reactivated = _asc.TryActivateAbility(handle);
            Check("P-5 쿨다운이 끝나면 다시 발동 — Stamina 20, Speed 10", reactivated && Approx(Stamina(), 20f) && Approx(Speed(), 10f));

            yield return new WaitForSeconds(CooldownDuration * 2f);
            _asc.ClearAllAbilities();
            Check("P-6 정리: 쿨다운 태그 0, 어빌리티 0", _asc.GetTagCount(cooldownTag) == 0 && _asc.GetActivatableAbilities().Count == 0);

            foreach (ScriptableObject obj in _created)
            {
                Destroy(obj);
            }

            Debug.Log($"[GAPlayMode Self-Check] passed {_passed} / failed {_failed}");
        }

        private float Stamina()
        {
            return _asc.GetAttributeCurrentValue(CharacterAttributeSet.Stamina);
        }

        private float Speed()
        {
            return _asc.GetAttributeCurrentValue(CharacterAttributeSet.Speed);
        }

        private GameplayEffectAsset MakeInstantEffect(GameplayAttributeHandle attribute, float magnitude)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", GameplayEffectType.Instant);

            object magnitudeBoxed = default(GameplayEffectModifierMagnitude);
            SetPrivateField(magnitudeBoxed, "magnitudeCalculationType", MagnitudeCalculationType.ScalableFloat);
            SetPrivateField(magnitudeBoxed, "scalableFloatMagnitude", magnitude);

            object modifierBoxed = default(GameplayModifier);
            SetPrivateField(modifierBoxed, "attribute", new GameplayAttribute(attribute));
            SetPrivateField(modifierBoxed, "operation", GameplayModifierOperation.AddBase);
            SetPrivateField(modifierBoxed, "modifierMagnitude", (GameplayEffectModifierMagnitude)magnitudeBoxed);

            SetPrivateField(ge, "modifiers", new List<GameplayModifier> { (GameplayModifier)modifierBoxed });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private GameplayEffectAsset MakeCooldownEffect(GameplayTag cooldownTag)
        {
            var ge = ScriptableObject.CreateInstance<GameplayEffectAsset>();
            SetPrivateField(ge, "type", GameplayEffectType.Duration);
            SetPrivateField(ge, "duration", CooldownDuration);
            SetPrivateField(ge, "modifiers", new List<GameplayModifier>());

            var grant = new TargetTagsGameplayEffectComponent();
            grant.GetConfiguredTargetTagChanges().AddTag(cooldownTag);
            SetPrivateField(ge, "gameplayEffectComponents", new List<GameplayEffectComponent> { grant });
            ge.OnGameplayEffectChanged();

            _created.Add(ge);
            return ge;
        }

        private GameplayAbilityAsset MakeTestAbilityAsset(GameplayEffectAsset costEffect, GameplayEffectAsset cooldownEffect, GameplayEffectAsset testEffect)
        {
            var ability = new TestGameplayAbility();
            SetPrivateField(ability, "costGameplayEffect", costEffect);
            SetPrivateField(ability, "cooldownGameplayEffect", cooldownEffect);
            SetPrivateField(ability, "_testEffect", testEffect);

            var asset = ScriptableObject.CreateInstance<GameplayAbilityAsset>();
            SetPrivateField(asset, "abilityTemplate", ability);

            _created.Add(asset);
            return asset;
        }

        // 기반 클래스의 private 필드(GameplayAbility의 코스트·쿨다운 칸)도 찾도록 상속 계층을 올라간다.
        private static void SetPrivateField(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, NonPublicInstance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            throw new InvalidOperationException($"필드 '{name}'을 {target.GetType().Name}에서 찾지 못함.");
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
                Debug.Log($"[GAPlayMode PASS] {label}");
            }
            else
            {
                _failed++;
                Debug.LogError($"[GAPlayMode FAIL] {label}");
            }
        }
    }
}
