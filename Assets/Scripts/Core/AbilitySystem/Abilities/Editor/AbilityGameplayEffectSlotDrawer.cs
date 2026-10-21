using System.Collections.Generic;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Abilities.Editor
{
    /// <summary>
    /// 코스트·쿨다운 GE 칸 아래에, 넣은 GE가 그 용도에 맞게 설정됐는지 초록(적합)·빨강(부적합)으로 보여 준다. GE가 비어 있으면 아무것도 그리지 않는다.
    /// 줄마다 무엇이 되고 안 되는지만 짧게 쓰고, 이유는 마우스를 올리면 툴팁으로 뜬다(줄 끝 ⓘ).
    /// 기준은 런타임이 실제로 하는 일이다 — 코스트는 <see cref="GameplayAbility.CheckCost"/>가 AddBase 모디파이어만 보고 커밋 때 Owner에 한 번 적용되며, 쿨다운은 <see cref="GameplayAbility.CheckCooldown"/>이 GE가 부여하는 태그로만 막는다.
    /// </summary>
    [CustomPropertyDrawer(typeof(AbilityGameplayEffectSlotAttribute))]
    public sealed class AbilityGameplayEffectSlotDrawer : PropertyDrawer
    {
        private const float Padding = 4f;
        private const float StripeWidth = 3f;

        private static readonly Color PassColor = new(0.25f, 0.75f, 0.35f);
        private static readonly Color FailColor = new(0.9f, 0.3f, 0.3f);

        private enum Verdict
        {
            Pass,
            Fail,
            Unknown,
        }

        private readonly struct Check
        {
            public readonly Verdict Verdict;
            public readonly string Summary;
            public readonly string Detail;

            public Check(Verdict verdict, string summary, string detail)
            {
                Verdict = verdict;
                Summary = summary;
                Detail = detail;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.objectReferenceValue is GameplayEffectAsset effect)
            {
                int lineCount = 1 + Evaluate(effect).Count;
                height += EditorGUIUtility.standardVerticalSpacing + lineCount * EditorGUIUtility.singleLineHeight + Padding * 2f;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property, label);

            if (property.objectReferenceValue is not GameplayEffectAsset effect)
            {
                return;
            }

            List<Check> checks = Evaluate(effect);
            bool suitable = !checks.Exists(check => check.Verdict == Verdict.Fail);
            Color statusColor = suitable ? PassColor : FailColor;

            Rect box = EditorGUI.IndentedRect(new Rect(position.x, position.y + lineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, (1 + checks.Count) * lineHeight + Padding * 2f));
            EditorGUI.DrawRect(box, new Color(statusColor.r, statusColor.g, statusColor.b, 0.15f));
            EditorGUI.DrawRect(new Rect(box.x, box.y, StripeWidth, box.height), statusColor);

            // 상자 안 줄은 이미 들여 쓴 상자 기준이라 들여쓰기를 끈다
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float iconSize = lineHeight;
            float textX = box.x + StripeWidth + Padding;
            float textWidth = box.xMax - Padding - iconSize - textX;
            float y = box.y + Padding;

            string slotName = SlotName(((AbilityGameplayEffectSlotAttribute)attribute).Slot);
            string headerText = suitable ? $"{slotName} GE로 적합" : $"{slotName} GE로 부적합";
            EditorGUI.LabelField(new Rect(textX, y, textWidth, lineHeight), new GUIContent($"{headerText}   (ⓘ에 마우스를 올리면 이유)", "줄마다 마우스를 올리면 왜 그런지 나온다."), MakeStyle(EditorStyles.miniBoldLabel, statusColor));

            foreach (Check check in checks)
            {
                y += lineHeight;
                var content = new GUIContent($"{Mark(check.Verdict)} {check.Summary}", check.Detail);
                EditorGUI.LabelField(new Rect(textX, y, textWidth, lineHeight), content, MakeStyle(EditorStyles.miniLabel, VerdictColor(check.Verdict)));

                // 툴팁이 있다는 표시 — 아이콘 위에서도 같은 설명이 뜬다
                GUIContent icon = EditorGUIUtility.IconContent("_Help");
                EditorGUI.LabelField(new Rect(box.xMax - Padding - iconSize, y, iconSize, lineHeight), new GUIContent(icon.image, check.Detail));
            }

            EditorGUI.indentLevel = indent;
        }

        private List<Check> Evaluate(GameplayEffectAsset effect)
        {
            return ((AbilityGameplayEffectSlotAttribute)attribute).Slot == AbilityGameplayEffectSlot.Cost ? EvaluateCost(effect) : EvaluateCooldown(effect);
        }

        // 코스트: 커밋 때 Owner에 한 번 적용되고, 발동 전엔 AddBase 모디파이어로 "깎아도 음수가 안 되는지"만 본다.
        private static List<Check> EvaluateCost(GameplayEffectAsset effect)
        {
            var checks = new List<Check>();

            checks.Add(effect.Type == GameplayEffectType.Instant
                ? new Check(Verdict.Pass, "Instant", "커밋할 때 Owner에 한 번 적용돼 Base 값이 깎인다.")
                : new Check(Verdict.Fail, $"Instant가 아님({effect.Type})", "코스트는 Instant여야 한다. Duration·Infinite는 Base가 아니라 현재값만 바꿔 GE가 끝나면 되돌아오고, 주기(Period)가 있으면 주기마다 반복해서 깎인다."));

            var serializedEffect = new SerializedObject(effect);
            SerializedProperty modifiersProperty = serializedEffect.FindProperty("modifiers");

            int costModifierCount = 0;
            IReadOnlyList<GameplayModifier> modifiers = effect.Modifiers;
            for (int i = 0; modifiers != null && i < modifiers.Count; i++)
            {
                GameplayModifier modifier = modifiers[i];
                if (!modifier.Attribute.IsSet)
                {
                    checks.Add(new Check(Verdict.Fail, $"모디파이어 {i}: 어트리뷰트 없음", "깎을 어트리뷰트를 고르지 않았다."));
                    continue;
                }

                string name = modifier.Attribute.ToString();
                if (modifier.Operation != GameplayModifierOperation.AddBase)
                {
                    checks.Add(new Check(Verdict.Fail, $"{name}: {modifier.Operation}는 확인 안 됨", "발동 전 코스트 확인(CheckCost)은 AddBase 모디파이어만 본다. 이 모디파이어는 적용은 되지만, 자원이 모자라도 발동을 막지 못한다."));
                    continue;
                }

                costModifierCount++;
                MagnitudeCalculationType calculationType = modifier.ModifierMagnitude.MagnitudeCalculationType;
                if (calculationType != MagnitudeCalculationType.ScalableFloat)
                {
                    checks.Add(new Check(Verdict.Unknown, $"{name}: {calculationType} — 부호 확인 불가", "수치가 실행 때 계산돼 여기선 음수인지 알 수 없다. 음수여야 깎인다."));
                    continue;
                }

                float magnitude = modifiersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("modifierMagnitude.scalableFloatMagnitude").floatValue;
                checks.Add(magnitude < 0f
                    ? new Check(Verdict.Pass, $"{name} {magnitude:0.##}", $"발동 전엔 현재값 + ({magnitude:0.##}) ≥ 0인지 보고, 커밋 때 Base에 더한다.")
                    : new Check(Verdict.Fail, $"{name} {magnitude:+0.##;-0.##;0} — 음수가 아님", "AddBase 수치가 0 이상이면 자원을 깎지 않는다(양수면 오히려 늘어난다). 코스트 확인도 늘 통과한다."));
            }

            if (costModifierCount == 0)
            {
                checks.Add(new Check(Verdict.Fail, "AddBase 모디파이어 없음", "깎을 자원이 없어 코스트 확인이 늘 통과하고 아무것도 깎이지 않는다."));
            }

            if (effect.Executions.Count > 0)
            {
                checks.Add(new Check(Verdict.Fail, $"Execution {effect.Executions.Count}개는 확인 안 됨", "Execution 출력은 발동 전 코스트 확인에서 빠진다. 적용은 되지만 자원이 모자라도 발동을 막지 못한다."));
            }

            return checks;
        }

        // 쿨다운: 커밋 때 Owner에 적용되고, 이 GE가 부여한 태그를 Owner가 가진 동안 발동이 막힌다.
        private static List<Check> EvaluateCooldown(GameplayEffectAsset effect)
        {
            var checks = new List<Check>();

            switch (effect.Type)
            {
                case GameplayEffectType.Duration:
                {
                    checks.Add(new Check(Verdict.Pass, "Duration", "지속시간 동안 부여 태그가 Owner에 남아 발동을 막는다."));
                    checks.Add(effect.Duration > 0f
                        ? new Check(Verdict.Pass, $"{effect.Duration:0.##}초", "이 시간이 쿨다운 길이다(남은 쿨다운 조회도 이 GE 기준).")
                        : new Check(Verdict.Fail, "지속시간 0", "걸리자마자 끝나 태그가 남지 않아 막지 못한다."));
                    break;
                }
                case GameplayEffectType.Infinite:
                {
                    checks.Add(new Check(Verdict.Fail, "Infinite — 끝나지 않음", "쿨다운은 Duration이어야 한다. Infinite는 끝나지 않아 한 번 쓰면 영영 발동할 수 없다."));
                    break;
                }
                default:
                {
                    checks.Add(new Check(Verdict.Fail, $"{effect.Type} — 막지 못함", "쿨다운은 Duration이어야 한다. Instant는 부여 태그가 남지 않아 발동을 막지 못한다."));
                    break;
                }
            }

            GameplayTagContainer grantedTags = effect.GetGrantedTags();
            checks.Add(grantedTags != null && !grantedTags.IsEmpty()
                ? new Check(Verdict.Pass, $"태그 {grantedTags.ToStringSimple()}", "Owner가 이 태그 중 하나라도 가지면 발동이 실패한다(CheckCooldown).")
                : new Check(Verdict.Fail, "부여 태그 없음", "쿨다운 확인은 이 GE가 부여하는 태그로만 막아서, 태그가 없으면 늘 통과한다. GE Components에 Target Tags Gameplay Effect Component를 넣고 태그를 지정한다."));

            return checks;
        }

        private static string SlotName(AbilityGameplayEffectSlot slot)
        {
            return slot == AbilityGameplayEffectSlot.Cost ? "코스트" : "쿨다운";
        }

        private static string Mark(Verdict verdict)
        {
            switch (verdict)
            {
                case Verdict.Pass:
                {
                    return "✓";
                }
                case Verdict.Fail:
                {
                    return "✗";
                }
                default:
                {
                    return "?";
                }
            }
        }

        private static Color VerdictColor(Verdict verdict)
        {
            switch (verdict)
            {
                case Verdict.Pass:
                {
                    return PassColor;
                }
                case Verdict.Fail:
                {
                    return FailColor;
                }
                default:
                {
                    return EditorStyles.miniLabel.normal.textColor;
                }
            }
        }

        private static GUIStyle MakeStyle(GUIStyle baseStyle, Color textColor)
        {
            var style = new GUIStyle(baseStyle);
            style.normal.textColor = textColor;
            return style;
        }
    }
}
