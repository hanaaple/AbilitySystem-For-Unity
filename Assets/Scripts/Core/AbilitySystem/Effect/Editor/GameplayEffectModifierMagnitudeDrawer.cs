using Core.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>
    /// GameplayEffectModifierMagnitude를 옅은 박스 안에 그린다 — 접기 제목 + Calc Type + 값 영역. 값 영역은 Calc Type별 payload가 그린다
    /// (ScalableFloat=float / AttributeBased=AttributeBasedMagnitudeDrawer / 미구현 타입=안내). 접으면 제목 옆 한 줄 요약만 남는다.
    /// GE Modifier와 Execution Calculation Modifier가 같이 쓴다.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayEffectModifierMagnitude))]
    public sealed class GameplayEffectModifierMagnitudeDrawer : PropertyDrawer
    {
        private const string MagnitudeCalculationTypeName = "magnitudeCalculationType";
        private const string ScalableFloatMagnitudeName = "scalableFloatMagnitude";
        private const string AttributeBasedMagnitudeName = "attributeBasedMagnitude";

        private const string Title = "Modifier Magnitude";
        private const float LineGap = 4f;
        private const float BoxPadX = 4f;
        private const float BoxPadY = 3f;
        private const int PlannedHelpBoxLines = 2;

        // 부모 인스펙터의 다른 접기(Source/Target Tags)와 화살표 자리를 맞춘다.
        private const float FoldoutIndent = 10f;

        // 접으면 제목 줄만, 펼치면 Calc Type + 값 영역까지. 각 줄 아래 LineGap을 포함한다.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = EditorGUIUtility.singleLineHeight + LineGap;
            if (!property.isExpanded)
            {
                return spacing;
            }

            return spacing * 2 + GetValueHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty calcType = property.FindPropertyRelative(MagnitudeCalculationTypeName);
            SerializedProperty scalableFloat = property.FindPropertyRelative(ScalableFloatMagnitudeName);
            SerializedProperty attributeBased = property.FindPropertyRelative(AttributeBasedMagnitudeName);
            var calc = (MagnitudeCalculationType)calcType.enumValueIndex;

            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = lineH + LineGap;

            // 박스는 내용보다 조금 넓게 칠해 위아래 행과 구분한다.
            Rect boxRect = new Rect(position.x - BoxPadX, position.y - BoxPadY, position.width + BoxPadX * 2, GetPropertyHeight(property, label) + BoxPadY * 2);
            EditorGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.12f));

            string title = property.isExpanded ? Title : $"{Title}   —   {GetSummary(calc, scalableFloat)}";
            Rect foldoutRect = new Rect(position.x + FoldoutIndent, position.y, position.width - FoldoutIndent, lineH);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, title, true, EditorDrawUtility.BoldFoldoutStyle);

            if (!property.isExpanded)
            {
                return;
            }

            EditorGUI.PropertyField(new Rect(position.x, position.y + spacing, position.width, lineH), calcType, new GUIContent("Calc Type"));

            Rect valueRect = new Rect(position.x, position.y + spacing * 2, position.width, GetValueHeight(property));
            switch (calc)
            {
                case MagnitudeCalculationType.ScalableFloat:
                {
                    EditorGUI.PropertyField(valueRect, scalableFloat, new GUIContent("Magnitude"));
                    break;
                }

                case MagnitudeCalculationType.AttributeBased:
                {
                    EditorGUI.PropertyField(valueRect, attributeBased, GUIContent.none, true);
                    break;
                }

                case MagnitudeCalculationType.CustomCalculationClass:
                case MagnitudeCalculationType.SetByCaller:
                {
                    EditorGUI.HelpBox(valueRect, $"{ObjectNames.NicifyVariableName(calc.ToString())}: 구현 예정. 지금은 적용하면 계산 실패로 0이 들어가고 경고가 남는다.", MessageType.Info);
                    break;
                }
            }
        }

        // Calc Type별 값 영역 높이. AttributeBased는 payload 드로어가 높이를 계산한다.
        private static float GetValueHeight(SerializedProperty property)
        {
            SerializedProperty calcType = property.FindPropertyRelative(MagnitudeCalculationTypeName);
            var calc = (MagnitudeCalculationType)calcType.enumValueIndex;
            switch (calc)
            {
                case MagnitudeCalculationType.AttributeBased:
                {
                    return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(AttributeBasedMagnitudeName), true);
                }

                case MagnitudeCalculationType.CustomCalculationClass:
                case MagnitudeCalculationType.SetByCaller:
                {
                    return EditorGUIUtility.singleLineHeight * PlannedHelpBoxLines;
                }

                default:
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }
        }

        /// <summary>접었을 때 제목 옆에 붙이는 한 줄 요약.</summary>
        private static string GetSummary(MagnitudeCalculationType calc, SerializedProperty scalableFloat)
        {
            string calcLabel = ObjectNames.NicifyVariableName(calc.ToString());
            switch (calc)
            {
                case MagnitudeCalculationType.ScalableFloat:
                {
                    return $"{calcLabel}  {scalableFloat.floatValue:0.###}";
                }

                case MagnitudeCalculationType.CustomCalculationClass:
                case MagnitudeCalculationType.SetByCaller:
                {
                    return $"{calcLabel} (구현 예정)";
                }

                default:
                {
                    return calcLabel;
                }
            }
        }
    }
}
