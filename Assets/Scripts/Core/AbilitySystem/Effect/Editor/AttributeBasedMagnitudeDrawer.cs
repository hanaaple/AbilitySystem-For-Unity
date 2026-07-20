using System;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>
    /// AttributeBasedMagnitude를 스스로 그리는 PropertyDrawer.
    /// 부모(GameplayEffectAssetDrawer)는 Calc Type이 AttributeBased일 때 PropertyField 한 줄로 위임하고,
    /// 높이는 GetPropertyHeight가 계산한다 — Calc Type별 레이아웃 지식을 각 payload가 소유한다.
    /// 계산식: (capturedValue + Pre-Add) * Coefficient + Post-Add
    /// </summary>
    [CustomPropertyDrawer(typeof(AttributeBasedMagnitude))]
    public sealed class AttributeBasedMagnitudeDrawer : PropertyDrawer
    {
        // Capture From / Attribute Set / Attribute / Value Type / Coefficient / Pre-Add / Post-Add
        private const int RowCount = 7;
        private const float RowGap = 2f;

        private const string CaptureSourceName = "captureSource";
        private const string AttributeSetTypeNameName = "attributeSetTypeName";
        private const string FieldNameName = "fieldName";
        private const string CaptureValueTypeName = "captureValueType";
        private const string CoefficientName = "coefficient";
        private const string PreMultiplyAdditiveName = "preMultiplyAdditive";
        private const string PostMultiplyAdditiveName = "postMultiplyAdditive";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * RowCount + RowGap * (RowCount - 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty captureSource = property.FindPropertyRelative(CaptureSourceName);
            SerializedProperty typeName      = property.FindPropertyRelative(AttributeSetTypeNameName);
            SerializedProperty fieldName     = property.FindPropertyRelative(FieldNameName);
            SerializedProperty valueType     = property.FindPropertyRelative(CaptureValueTypeName);
            SerializedProperty coefficient   = property.FindPropertyRelative(CoefficientName);
            SerializedProperty preAdd        = property.FindPropertyRelative(PreMultiplyAdditiveName);
            SerializedProperty postAdd       = property.FindPropertyRelative(PostMultiplyAdditiveName);

            float lineH = EditorGUIUtility.singleLineHeight;
            float step = lineH + RowGap;
            var row = new Rect(position.x, position.y, position.width, lineH);

            EditorGUI.PropertyField(row, captureSource, new GUIContent("Capture From"));

            // Attribute Set (0 = None → 하위 Field 초기화)
            row.y += step;
            Type[] setTypes = AttributeReferenceGUI.GetSetTypes();
            string[] setNames = AttributeReferenceGUI.GetSetDisplayNames(setTypes);
            int setIndex = AttributeReferenceGUI.GetSetPopupIndex(setTypes, typeName.stringValue);
            int newSetIndex = EditorGUI.Popup(row, "Attribute Set", setIndex, setNames);
            if (newSetIndex != setIndex)
            {
                typeName.stringValue = newSetIndex == 0
                    ? string.Empty
                    : setTypes[newSetIndex - 1].AssemblyQualifiedName;
                fieldName.stringValue = string.Empty;
            }

            // Attribute (field) — Set이 정해진 경우만 선택 가능
            row.y += step;
            if (newSetIndex > 0)
            {
                string[] fieldNames = AttributeReferenceGUI.GetFieldNames(setTypes[newSetIndex - 1]);
                int fieldIndex = Array.IndexOf(fieldNames, fieldName.stringValue);
                int newFieldIndex = EditorGUI.Popup(row, "Attribute", fieldIndex, fieldNames);
                if (newFieldIndex >= 0 && newFieldIndex < fieldNames.Length)
                {
                    fieldName.stringValue = fieldNames[newFieldIndex];
                }
            }
            else
            {
                EditorGUI.LabelField(row, "Attribute", "—");
            }

            row.y += step;
            EditorGUI.PropertyField(row, valueType, new GUIContent("Value Type"));
            row.y += step;
            EditorGUI.PropertyField(row, coefficient, new GUIContent("Coefficient"));
            row.y += step;
            EditorGUI.PropertyField(row, preAdd, new GUIContent("Pre-Add", "(value + Pre-Add) * Coefficient + Post-Add"));
            row.y += step;
            EditorGUI.PropertyField(row, postAdd, new GUIContent("Post-Add"));
        }
    }
}
