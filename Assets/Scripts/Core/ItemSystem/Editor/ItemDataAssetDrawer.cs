using System;
using Core.Common.Editor;
using Core.ItemSystem;
using Core.ItemSystem.Module;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Core.ItemSystem.Editor
{
    // ItemData는 abstract — 실제 에셋은 EquipItemAsset/ConsumeItemAsset 등 서브클래스라 editorForChildClasses가 없으면 이 에디터가 적용되지 않는다(기본 인스펙터로 떨어져 Add 팝업이 안 뜸).
    [CustomEditor(typeof(ItemDataAsset), editorForChildClasses: true)]
    public sealed class ItemDataAssetDrawer : UnityEditor.Editor
    {
        private const string UidPropertyName = "uid";
        private const string DisplayNamePropertyName = "displayName";
        private const string IconPropertyName = "icon";
        private const string RuntimeTypePropertyName = "runtimeClass";
        private const string ModulesPropertyName = "modules";

        private const float LineGap = 2f;
        private const float ElementVerticalPadding = 4f;

        private SerializedProperty _uidProperty;
        private SerializedProperty _displayNameProperty;
        private SerializedProperty _iconProperty;
        private SerializedProperty _runtimeTypeProperty;
        private SerializedProperty _modulesProperty;

        private ReorderableList _moduleList;

        private void OnEnable()
        {
            _uidProperty = serializedObject.FindProperty(UidPropertyName);
            _displayNameProperty = serializedObject.FindProperty(DisplayNamePropertyName);
            _iconProperty = serializedObject.FindProperty(IconPropertyName);
            _runtimeTypeProperty = serializedObject.FindProperty(RuntimeTypePropertyName);
            _modulesProperty = serializedObject.FindProperty(ModulesPropertyName);

            _moduleList = TypeChoiceList.Create(
                serializedObject,
                _modulesProperty,
                "Modules",
                () => EditorTypeUtility.GetConcreteSubclasses(typeof(ItemModule)),
                AddModule,
                "추가 가능한 Module 없음",
                drawElement: DrawModuleElement,
                elementHeight: GetModuleElementHeight);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawItemFields();
            EditorGUILayout.Space(8f);
            _moduleList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawItemFields()
        {
            EditorGUILayout.LabelField("Item Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_uidProperty);
            EditorGUILayout.PropertyField(_displayNameProperty);
            EditorGUILayout.PropertyField(_iconProperty);
            EditorGUILayout.PropertyField(_runtimeTypeProperty);
            EditorGUI.indentLevel--;
        }

        private void DrawModuleElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _modulesProperty.GetArrayElementAtIndex(index);

            Rect foldoutRect = EditorDrawUtility.GetFoldoutRect(rect);

            element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, GetModuleName(element), true, EditorDrawUtility.BoldFoldoutStyle);

            if (!element.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            float y = rect.y + ElementVerticalPadding + EditorGUIUtility.singleLineHeight + LineGap;

            SerializedProperty child = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;

            while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                float fieldHeight = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, fieldHeight), child, true);
                y += fieldHeight + LineGap;
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
        }

        private float GetModuleElementHeight(int index)
        {
            SerializedProperty element = _modulesProperty.GetArrayElementAtIndex(index);

            float height = EditorGUIUtility.singleLineHeight + ElementVerticalPadding * 2f;

            if (!element.isExpanded)
            {
                return height;
            }

            SerializedProperty child = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;

            while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                height += EditorGUI.GetPropertyHeight(child, true) + LineGap;
                enterChildren = false;
            }

            return height;
        }

        // 모듈은 [SerializeReference]라 인스턴스를 직접 만들어 managedReferenceValue로 넣는다(AttributeSet은 typeName 문자열 방식).
        private void AddModule(SerializedProperty element, Type moduleType)
        {
            element.managedReferenceValue = Activator.CreateInstance(moduleType);
            element.isExpanded = true;
        }

        private static string GetModuleName(SerializedProperty moduleProperty)
        {
            string typeName = moduleProperty.managedReferenceFullTypename;

            if (string.IsNullOrEmpty(typeName))
            {
                return "Missing Module";
            }

            int spaceIndex = typeName.IndexOf(' ');
            string qualifiedName = spaceIndex >= 0 ? typeName.Substring(spaceIndex + 1) : typeName;

            int lastDotIndex = qualifiedName.LastIndexOf('.');
            string className = lastDotIndex >= 0 ? qualifiedName.Substring(lastDotIndex + 1) : qualifiedName;

            return ObjectNames.NicifyVariableName(className);
        }
    }
}
