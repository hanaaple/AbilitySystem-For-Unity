using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(AttributeSetInitData))]
    public sealed class AttributeSetInitDataDrawer : PropertyDrawer
    {
        private const float LineGap = 4f;
        private const float SectionGap = 10f;

        private const string AttributeSetTypeNamePropertyName = "attributeSetTypeName";
        private const string AttributesPropertyName = "attributes";

        private const string FieldNamePropertyName = "fieldName";
        private const string BaseValuePropertyName = "baseValue";

        private static readonly Dictionary<string, Type> _typeCache = new();
        private static readonly Dictionary<string, string> _nicifyVariableName = new();
        private static readonly Dictionary<string, SerializedProperty> _fieldMapBuffer = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty attributeSetTypeNameProperty = property.FindPropertyRelative(AttributeSetTypeNamePropertyName);

            SerializedProperty attributesProperty = property.FindPropertyRelative(AttributesPropertyName);

            Type resolvedType = ResolveType(attributeSetTypeNameProperty.stringValue);
            GUIContent foldoutLabel = resolvedType != null ? new GUIContent(resolvedType.Name) : label;

            Rect foldoutRect = EditorDrawUtility.GetFoldoutRect(position);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true, EditorDrawUtility.BoldFoldoutStyle);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            float y = position.y + EditorGUIUtility.singleLineHeight + LineGap;

            // 타입 선택 UI는 SubclassSelector 드로어를 재사용하되, 중복 제외 대상은 전용 드로어인 여기서 수집해 넘긴다.
            // (AttributeSetInitData[] 배열 안의 중복 방지는 이 케이스의 특수 요구라, 범용 드로어에 넣지 않고 여기서 조립한다.)
            Rect typeRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            HashSet<string> usedByOthers = SubclassSelectorDrawer.CollectSiblingValues(attributeSetTypeNameProperty);
            SubclassSelectorDrawer.DrawSelector(typeRect, attributeSetTypeNameProperty, typeof(AttributeSet), usedByOthers, new GUIContent("Attribute Set"));
            y += EditorGUIUtility.singleLineHeight + LineGap;

            // DrawSelector의 선택 변경은 팝업 콜백으로 다음 프레임에 반영되므로, 이번 프레임은 위에서 구한 resolvedType으로 그린다.
            if (resolvedType != null)
            {
                // 매 OnGUI마다 sync한다. SyncAttributeFields는 불일치가 있을 때만 배열을 바꾸는 self-guard라
                // 이미 맞으면 no-op이므로 비용이 없고, 인덱스 경로 기반 "이미 sync했다" 캐시(Add/Remove로
                // 요소가 밀리면 stale)로 인해 빈 attributes가 그대로 렌더되던 버그를 제거한다.
                SyncAttributeFields(resolvedType, attributesProperty);

                DrawAttributeFields(position, ref y, resolvedType, attributesProperty);
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            SerializedProperty typeNameProperty = property.FindPropertyRelative(AttributeSetTypeNamePropertyName);

            float height =
                EditorGUIUtility.singleLineHeight + LineGap + // foldout
                EditorGUIUtility.singleLineHeight + LineGap;  // AttributeSet popup

            Type selectedType = ResolveType(typeNameProperty.stringValue);

            if (selectedType == null)
            {
                return height;
            }

            height += SectionGap + EditorGUIUtility.singleLineHeight + LineGap; // Attributes header

            FieldInfo[] fields = AttributeReflectionUtility.GetAttributeDataFields(selectedType);

            height += fields.Length * (EditorGUIUtility.singleLineHeight + LineGap);

            return height;
        }

        private static void DrawAttributeFields(Rect position, ref float y, Type selectedType, SerializedProperty attributesProperty)
        {
            y += SectionGap;

            EditorGUI.LabelField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), "Attributes", EditorStyles.boldLabel);

            y += EditorGUIUtility.singleLineHeight + LineGap;

            FieldInfo[] fields = AttributeReflectionUtility.GetAttributeDataFields(selectedType);

            Dictionary<string, SerializedProperty> fieldMap = BuildFieldMap(attributesProperty);

            foreach (FieldInfo field in fields)
            {
                if (!fieldMap.TryGetValue(field.Name, out SerializedProperty element))
                {
                    continue;
                }

                SerializedProperty dataProperty = element.FindPropertyRelative(BaseValuePropertyName);

                float dataHeight = EditorGUI.GetPropertyHeight(dataProperty, true);

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, dataHeight), dataProperty, new GUIContent(GetNicifyVariableName(field.Name)), true);

                y += dataHeight + LineGap;
            }
        }

        private static void SyncAttributeFields(Type selectedType, SerializedProperty attributesProperty)
        {
            FieldInfo[] fields = AttributeReflectionUtility.GetAttributeDataFields(selectedType);

            HashSet<string> validFieldNames = fields.Select(f => f.Name).ToHashSet();

            // 1. 제거된 필드 정리
            for (int i = attributesProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = attributesProperty.GetArrayElementAtIndex(i);

                if (!validFieldNames.Contains(element.FindPropertyRelative(FieldNamePropertyName).stringValue))
                {
                    attributesProperty.DeleteArrayElementAtIndex(i);
                }
            }

            // 2. 기존 필드명 수집 후 누락된 필드만 추가
            HashSet<string> existingNames = new HashSet<string>(attributesProperty.arraySize);
            for (int i = 0; i < attributesProperty.arraySize; i++)
            {
                existingNames.Add(attributesProperty.GetArrayElementAtIndex(i).FindPropertyRelative(FieldNamePropertyName).stringValue);
            }

            foreach (FieldInfo field in fields)
            {
                if (existingNames.Contains(field.Name))
                {
                    continue;
                }

                int index = attributesProperty.arraySize;
                attributesProperty.InsertArrayElementAtIndex(index);
                attributesProperty.GetArrayElementAtIndex(index).FindPropertyRelative(FieldNamePropertyName).stringValue = field.Name;
            }
        }

        private static Dictionary<string, SerializedProperty> BuildFieldMap(SerializedProperty attributesProperty)
        {
            _fieldMapBuffer.Clear();

            for (int i = 0; i < attributesProperty.arraySize; i++)
            {
                SerializedProperty element = attributesProperty.GetArrayElementAtIndex(i);
                _fieldMapBuffer[element.FindPropertyRelative(FieldNamePropertyName).stringValue] = element;
            }

            return _fieldMapBuffer;
        }

        private static Type ResolveType(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return null;
            }

            if (_typeCache.TryGetValue(assemblyQualifiedName, out Type cached))
            {
                return cached;
            }

            Type type = Type.GetType(assemblyQualifiedName);
            _typeCache[assemblyQualifiedName] = type;

            return type;
        }

        private static string GetNicifyVariableName(string fieldName)
        {
            if (_nicifyVariableName.TryGetValue(fieldName, out string cached))
            {
                return cached;
            }

            string nicifyVariableName = ObjectNames.NicifyVariableName(fieldName);
            _nicifyVariableName[fieldName] = nicifyVariableName;

            return nicifyVariableName;
        }
    }
}
