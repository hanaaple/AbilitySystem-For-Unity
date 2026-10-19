using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>AttributeBasedMagnitude를 스스로 그리는 PropertyDrawer. 부모(GameplayEffectAssetDrawer)는 Calc Type이 AttributeBased일 때 PropertyField 한 줄로 위임하고 높이는 GetPropertyHeight가 계산한다 — Calc Type별 레이아웃 지식을 각 payload가 소유. 첫 줄은 접기 제목이고 접으면 한 줄 요약만 남는다. 계산식: (capturedValue + Pre-Add) * Coefficient + Post-Add.</summary>
    [CustomPropertyDrawer(typeof(AttributeBasedMagnitude))]
    public sealed class AttributeBasedMagnitudeDrawer : PropertyDrawer
    {
        private const float RowGap = 2f;

        // 부모(GE 인스펙터)의 다른 접기와 화살표 자리를 맞춘다. 하위 행도 이만큼 밀고 라벨 폭을 줄여 값 열은 위 행들과 같게 둔다.
        private const float FoldoutIndent = 10f;

        private const string Title = "Attribute Based";

        // 캡처 대상(captureSource/attribute/snapshot)은 중첩된 backingAttribute(GameplayEffectAttributeCaptureDefinition) 안에 있다.
        // Set/Attribute 팝업은 attribute(GameplayAttribute)의 전용 드로어가 그린다.
        private const string CaptureSourceName = "backingAttribute.captureSource";
        private const string AttributeName = "backingAttribute.attribute";
        private const string AttributeFieldName = "backingAttribute.attribute.fieldName";
        private const string SnapshotName = "backingAttribute.snapshot";
        private const string CaptureValueTypeName = "captureValueType";
        private const string CoefficientName = "coefficient";
        private const string PreMultiplyAdditiveName = "preMultiplyAdditive";
        private const string PostMultiplyAdditiveName = "postMultiplyAdditive";

        // 단일행: Capture From / Snapshot / Value Type / Coefficient / Pre-Add / Post-Add
        private const int SingleLineRowCount = 6;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return lineH;
            }

            float attributeH = EditorGUI.GetPropertyHeight(property.FindPropertyRelative(AttributeName), true);
            return lineH + RowGap + lineH * SingleLineRowCount + attributeH + RowGap * SingleLineRowCount;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;

            string title = property.isExpanded ? Title : $"{Title}   —   {GetSummary(property)}";
            Rect foldoutRect = new Rect(position.x + FoldoutIndent, position.y, position.width - FoldoutIndent, lineH);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, title, true);

            if (!property.isExpanded)
            {
                return;
            }

            SerializedProperty captureSource = property.FindPropertyRelative(CaptureSourceName);
            SerializedProperty attribute     = property.FindPropertyRelative(AttributeName);
            SerializedProperty snapshot      = property.FindPropertyRelative(SnapshotName);
            SerializedProperty valueType     = property.FindPropertyRelative(CaptureValueTypeName);
            SerializedProperty coefficient   = property.FindPropertyRelative(CoefficientName);
            SerializedProperty preAdd        = property.FindPropertyRelative(PreMultiplyAdditiveName);
            SerializedProperty postAdd       = property.FindPropertyRelative(PostMultiplyAdditiveName);

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = prevLabelWidth - FoldoutIndent;

            float step = lineH + RowGap;
            var row = new Rect(position.x + FoldoutIndent, position.y + step, position.width - FoldoutIndent, lineH);

            EditorGUI.PropertyField(row, captureSource, new GUIContent("Capture From"));

            // Attribute Set + Attribute — GameplayAttribute 전용 드로어가 2행으로 그린다.
            row.y += step;
            float attributeH = EditorGUI.GetPropertyHeight(attribute, true);
            EditorGUI.PropertyField(new Rect(row.x, row.y, row.width, attributeH), attribute, GUIContent.none, true);
            row.y += attributeH + RowGap;

            EditorGUI.PropertyField(row, snapshot, new GUIContent("Snapshot", "true면 캡처(적용) 시점 값을 고정, false면 조회 시점 라이브 값"));
            row.y += step;
            EditorGUI.PropertyField(row, valueType, new GUIContent("Value Type"));
            row.y += step;
            EditorGUI.PropertyField(row, coefficient, new GUIContent("Coefficient"));
            row.y += step;
            EditorGUI.PropertyField(row, preAdd, new GUIContent("Pre-Add", "(value + Pre-Add) * Coefficient + Post-Add"));
            row.y += step;
            EditorGUI.PropertyField(row, postAdd, new GUIContent("Post-Add"));

            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        /// <summary>접었을 때 제목 옆에 붙이는 한 줄 요약 — 무엇을 캡처해 몇 배 하는지.</summary>
        private static string GetSummary(SerializedProperty property)
        {
            SerializedProperty captureSource = property.FindPropertyRelative(CaptureSourceName);
            SerializedProperty fieldName = property.FindPropertyRelative(AttributeFieldName);
            SerializedProperty coefficient = property.FindPropertyRelative(CoefficientName);

            string sourceLabel = captureSource.enumDisplayNames[captureSource.enumValueIndex];
            string fieldLabel = string.IsNullOrEmpty(fieldName.stringValue) ? "(Attribute 미지정)" : fieldName.stringValue;
            return $"{sourceLabel} {fieldLabel} × {coefficient.floatValue:0.###}";
        }
    }
}
