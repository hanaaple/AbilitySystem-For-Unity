using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Attribute.Editor
{
    /// <summary>
    /// <see cref="GameplayAttribute"/>를 "Attribute Set" + "Attribute" 두 팝업으로 그리는 PropertyDrawer.
    /// 이 타입의 모든 직렬화 필드에 자동 적용되므로, modifier·캡처 정의 등 소비처는 두 문자열을 직접 그리지 않고
    /// 이 필드 하나를 <c>PropertyField</c>로 위임하면 된다.
    /// popup index 0 = "None", 1+ = <see cref="AttributeReflectionUtility.GetAttributeSetTypes"/> 순서.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayAttribute))]
    public sealed class GameplayAttributeDrawer : PropertyDrawer
    {
        private const string AttributeSetTypeNameName = "attributeSetTypeName";
        private const string FieldNameName = "fieldName";
        private const float RowGap = 2f;

        private static string[] _setDisplayNames;
        private static readonly Dictionary<Type, string[]> _fieldNameCache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + RowGap;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeName = property.FindPropertyRelative(AttributeSetTypeNameName);
            SerializedProperty fieldName = property.FindPropertyRelative(FieldNameName);

            float lineH = EditorGUIUtility.singleLineHeight;
            var row = new Rect(position.x, position.y, position.width, lineH);

            // 두 행을 한 단 들여써서 "이 어트리뷰트 참조"가 하나의 묶음임을 시각적으로 드러낸다(부모 필드 아래 중첩).
            EditorGUI.indentLevel++;

            // Attribute Set (0 = None → 하위 Field 초기화)
            Type[] setTypes = AttributeReflectionUtility.GetAttributeSetTypes();
            string[] setNames = GetSetDisplayNames(setTypes);
            int setIndex = GetSetPopupIndex(setTypes, typeName.stringValue);
            int newSetIndex = EditorGUI.Popup(row, "Attribute Set", setIndex, setNames);
            if (newSetIndex != setIndex)
            {
                typeName.stringValue = newSetIndex == 0
                    ? string.Empty
                    : setTypes[newSetIndex - 1].AssemblyQualifiedName;
                fieldName.stringValue = string.Empty;
            }

            // Attribute (field) — Set이 정해진 경우만 선택 가능
            row.y += lineH + RowGap;
            if (newSetIndex > 0)
            {
                string[] fieldNames = GetFieldNames(setTypes[newSetIndex - 1]);
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

            EditorGUI.indentLevel--;
        }

        private static string[] GetSetDisplayNames(Type[] setTypes)
        {
            return _setDisplayNames ??= new[] { "None" }.Concat(setTypes.Select(t => t.Name)).ToArray();
        }

        private static int GetSetPopupIndex(Type[] setTypes, string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return 0;
            }

            for (int i = 0; i < setTypes.Length; i++)
            {
                if (setTypes[i].AssemblyQualifiedName == assemblyQualifiedName)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private static string[] GetFieldNames(Type setType)
        {
            if (_fieldNameCache.TryGetValue(setType, out string[] cached))
            {
                return cached;
            }

            string[] names = AttributeReflectionUtility.GetAttributeDataFields(setType)
                .Select(f => f.Name)
                .ToArray();

            _fieldNameCache[setType] = names;
            return names;
        }
    }
}
