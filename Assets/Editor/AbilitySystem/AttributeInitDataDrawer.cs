using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Attribute;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor.AbilitySystem
{
    [CustomEditor(typeof(AttributeInitData))]
    public sealed class AttributeInitDataDrawer : UnityEditor.Editor
    {
        private const string AttributeSetsPropertyName = "attributeSets";
        private const string TypeNamePropertyName = "attributeSetTypeName";
        private const string AttributesPropertyName = "attributes";

        private const float ElementVerticalPadding = 2f;

        private SerializedProperty _attributeSetsProperty;
        private ReorderableList _list;

        private void OnEnable()
        {
            _attributeSetsProperty = serializedObject.FindProperty(AttributeSetsPropertyName);

            _list = new ReorderableList(
                serializedObject,
                _attributeSetsProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                onAddDropdownCallback = OnAddDropdown
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Attribute Sets");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _attributeSetsProperty.GetArrayElementAtIndex(index);
            rect.height -= ElementVerticalPadding;
            EditorGUI.PropertyField(rect, element, true);
        }

        private float GetElementHeight(int index)
        {
            SerializedProperty element = _attributeSetsProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + ElementVerticalPadding;
        }

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            Type[] allTypes = AttributeReflectionUtility.GetAttributeSetTypes();

            HashSet<string> usedNames = new HashSet<string>();
            for (int i = 0; i < _attributeSetsProperty.arraySize; i++)
            {
                SerializedProperty element = _attributeSetsProperty.GetArrayElementAtIndex(i);
                SerializedProperty typeNameProp = element.FindPropertyRelative(TypeNamePropertyName);

                if (!string.IsNullOrEmpty(typeNameProp.stringValue))
                {
                    usedNames.Add(typeNameProp.stringValue);
                }
            }

            Type[] unusedTypes = allTypes
                .Where(t => !usedNames.Contains(t.AssemblyQualifiedName))
                .ToArray();

            GenericMenu menu = new GenericMenu();

            if (unusedTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("추가 가능한 AttributeSet 없음"));
                menu.ShowAsContext();
                return;
            }

            foreach (Type type in unusedTypes)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    serializedObject.Update();

                    int newIndex = _attributeSetsProperty.arraySize;
                    _attributeSetsProperty.InsertArrayElementAtIndex(newIndex);

                    SerializedProperty newElement = _attributeSetsProperty.GetArrayElementAtIndex(newIndex);
                    newElement.FindPropertyRelative(TypeNamePropertyName).stringValue = type.AssemblyQualifiedName;
                    newElement.FindPropertyRelative(AttributesPropertyName).ClearArray();

                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}
