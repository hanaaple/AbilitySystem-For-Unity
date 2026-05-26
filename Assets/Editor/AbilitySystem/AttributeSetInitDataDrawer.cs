using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using UnityEditor;
using UnityEngine;

namespace Editor.AbilitySystem
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
        private static readonly HashSet<string> _usedTypeNamesBuffer = new();

        // propertyPath → 마지막으로 sync한 타입 이름 (변경 시에만 sync 실행)
        private static readonly Dictionary<string, string> _lastSyncedTypeName = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty attributeSetTypeNameProperty = property.FindPropertyRelative(AttributeSetTypeNamePropertyName);

            SerializedProperty attributesProperty = property.FindPropertyRelative(AttributesPropertyName);

            float dragHandleWidth = EditorGUIUtility.singleLineHeight;

            Type resolvedType = ResolveType(attributeSetTypeNameProperty.stringValue);
            GUIContent foldoutLabel = resolvedType != null ? new GUIContent(resolvedType.Name) : label;

            Rect foldoutRect = new Rect(position.x + dragHandleWidth, position.y, position.width - dragHandleWidth, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            float y = position.y + EditorGUIUtility.singleLineHeight + LineGap;

            // resolvedType을 내려보내 중복 ResolveType 호출 방지
            Type selectedType = DrawAttributeSetPopup(position, ref y, attributeSetTypeNameProperty, resolvedType);

            if (selectedType != null)
            {
                SyncAttributeFieldsIfChanged(selectedType, attributesProperty, attributeSetTypeNameProperty);

                DrawAttributeFields(position, ref y, selectedType, attributesProperty);
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

        private static Type DrawAttributeSetPopup(Rect position, ref float y, SerializedProperty typeNameProperty, Type currentType)
        {
            HashSet<string> usedByOthers = GetUsedTypeNamesByOthers(typeNameProperty);

            Type[] setTypes = AttributeReflectionUtility.GetAttributeSetTypes()
                .Where(t => !usedByOthers.Contains(t.AssemblyQualifiedName) || t.AssemblyQualifiedName == typeNameProperty.stringValue)
                .ToArray();

            Rect rect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);

            if (setTypes.Length == 0)
            {
                EditorGUI.LabelField(rect, "Attribute Set", "No AttributeSet found");
                y += EditorGUIUtility.singleLineHeight + LineGap;
                return null;
            }

            string[] options = setTypes.Select(t => t.Name).ToArray();

            int currentIndex = 0;
            for (int i = 0; i < setTypes.Length; i++)
            {
                if (setTypes[i] == currentType)
                {
                    currentIndex = i;
                    break;
                }
            }

            int selectedIndex = EditorGUI.Popup(rect, "Attribute Set", currentIndex, options);

            Type selectedType = setTypes[selectedIndex];
            typeNameProperty.stringValue = selectedType.AssemblyQualifiedName;

            y += EditorGUIUtility.singleLineHeight + LineGap;

            return selectedType;
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

        /// <summary>
        /// 첫 로드 or 타입 변경 시 AttributeSetType Change 발생으로 인한 Sync
        /// </summary>
        private static void SyncAttributeFieldsIfChanged(Type selectedType, SerializedProperty attributesProperty, SerializedProperty attributeSetTypeNameProperty)
        {
            string path = attributeSetTypeNameProperty.propertyPath;
            string currentTypeName = attributeSetTypeNameProperty.stringValue;

            if (_lastSyncedTypeName.TryGetValue(path, out string last) && last == currentTypeName)
            {
                return;
            }

            SyncAttributeFields(selectedType, attributesProperty);
            _lastSyncedTypeName[path] = currentTypeName;
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

        private static HashSet<string> GetUsedTypeNamesByOthers(SerializedProperty typeNameProperty)
        {
            _usedTypeNamesBuffer.Clear();

            string path = typeNameProperty.propertyPath;

            int arrayDataIdx = path.IndexOf(".Array.data[", StringComparison.Ordinal);
            if (arrayDataIdx < 0)
            {
                return _usedTypeNamesBuffer;
            }

            string arrayPath = path.Substring(0, arrayDataIdx);

            int bracketStart = path.IndexOf('[', arrayDataIdx) + 1;
            int bracketEnd = path.IndexOf(']', bracketStart);
            if (!int.TryParse(path.Substring(bracketStart, bracketEnd - bracketStart), out int currentIndex))
            {
                return _usedTypeNamesBuffer;
            }

            SerializedProperty arrayProp = typeNameProperty.serializedObject.FindProperty(arrayPath);

            if (arrayProp == null || !arrayProp.isArray)
            {
                return _usedTypeNamesBuffer;
            }

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (i == currentIndex)
                {
                    continue;
                }

                SerializedProperty siblingTypeName = arrayProp.GetArrayElementAtIndex(i).FindPropertyRelative(AttributeSetTypeNamePropertyName);

                if (!string.IsNullOrEmpty(siblingTypeName.stringValue))
                {
                    _usedTypeNamesBuffer.Add(siblingTypeName.stringValue);
                }
            }

            return _usedTypeNamesBuffer;
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
