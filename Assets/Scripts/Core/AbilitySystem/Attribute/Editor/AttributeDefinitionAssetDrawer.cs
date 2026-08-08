using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Attribute;
using Core.Common.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Core.AbilitySystem.Attribute.Editor
{
    [CustomEditor(typeof(AttributeDefinitionAsset))]
    public sealed class AttributeDefinitionAssetDrawer : UnityEditor.Editor
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

            _list = TypeChoiceList.Create(
                serializedObject,
                _attributeSetsProperty,
                "Attribute Sets",
                GetAddableTypes,
                AddAttributeSet,
                "추가 가능한 AttributeSet 없음",
                drawElement: DrawElement,
                elementHeight: GetElementHeight,
                newScriptBaseType: typeof(AttributeSet),
                newScriptTypeRelPath: TypeNamePropertyName,
                newScriptClearArrayRelPath: AttributesPropertyName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
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

        // AttributeSet은 타입당 하나만 — 이미 쓰인 타입은 후보에서 제외(모듈과 달리 중복 불가).
        // 수집 코어는 요소 드로어(AttributeSetDefinitionDrawer)의 중복 제외와 공유한다(SubclassSelectorDrawer.CollectArrayValues).
        // 여기선 "새로 추가"할 타입이라 제외 인덱스 없이 배열 전체를 본다.
        private IEnumerable<Type> GetAddableTypes()
        {
            HashSet<string> usedNames = SubclassSelectorDrawer.CollectArrayValues(_attributeSetsProperty, TypeNamePropertyName);

            return AttributeReflectionUtility.GetAttributeSetTypes()
                .Where(t => !usedNames.Contains(t.AssemblyQualifiedName));
        }

        // AttributeSet은 SerializeReference가 아니라 typeName(AQN) 문자열 + 필드별 baseValue 모델이라
        // 인스턴스 대신 타입 이름만 저장하고 하위 필드는 비운다(드로어가 코드 정의 필드에 맞춰 sync).
        private void AddAttributeSet(SerializedProperty element, Type type)
        {
            element.FindPropertyRelative(TypeNamePropertyName).stringValue = type.AssemblyQualifiedName;
            element.FindPropertyRelative(AttributesPropertyName).ClearArray();
        }
    }
}
