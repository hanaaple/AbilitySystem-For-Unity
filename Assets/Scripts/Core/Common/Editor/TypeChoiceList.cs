using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>
    /// 다형(polymorphic) 리스트를 "타입 선택 팝업이 달린 +버튼"까지 한 번에 배선한
    /// <see cref="ReorderableList"/>로 만들어 주는 빌더. 드로어는 OnEnable에서 Create 한 번만 부르면 된다.
    ///
    /// 호출부가 정하는 것은 두 가지뿐:
    /// (1) 후보 타입을 어디서 가져올지(typeProvider) — 예: TypeCache 파생 타입, 이미 쓴 타입 제외 등
    /// (2) 고른 타입으로 새 요소를 어떻게 채울지(onAdd) — SerializeReference면 managedReferenceValue, typeName 방식이면 문자열 세팅
    ///
    /// 요소 삽입·Update/Apply·빈 상태 안내 팝업·리스트 배선은 빌더가 처리한다.
    /// 요소 그리기/높이는 기본값(PropertyField)을 쓰거나 콜백으로 덮어쓴다.
    /// </summary>
    public static class TypeChoiceList
    {
        public static ReorderableList Create(
            SerializedObject serializedObject,
            SerializedProperty elements,
            string header,
            Func<IEnumerable<Type>> typeProvider,
            Action<SerializedProperty, Type> onAdd,
            string emptyMessage,
            ReorderableList.ElementCallbackDelegate drawElement = null,
            ReorderableList.ElementHeightCallbackDelegate elementHeight = null)
        {
            return new ReorderableList(
                serializedObject,
                elements,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, header),

                drawElementCallback = drawElement ?? ((rect, index, isActive, isFocused) =>
                    EditorGUI.PropertyField(rect, elements.GetArrayElementAtIndex(index), true)),

                elementHeightCallback = elementHeight ?? (index =>
                    EditorGUI.GetPropertyHeight(elements.GetArrayElementAtIndex(index), true)),

                onAddDropdownCallback = (buttonRect, list) =>
                    ShowAddMenu(serializedObject, elements, typeProvider(), onAdd, emptyMessage),
            };
        }

        // 후보 타입을 GenericMenu로 띄우고, 선택 시 요소를 삽입한 뒤 onAdd로 채운다.
        // 후보가 비면 클릭 불가한 안내 항목만 보여준다(무반응으로 보이지 않게).
        private static void ShowAddMenu(
            SerializedObject serializedObject,
            SerializedProperty elements,
            IEnumerable<Type> types,
            Action<SerializedProperty, Type> onAdd,
            string emptyMessage)
        {
            GenericMenu menu = new GenericMenu();
            bool hasAny = false;

            foreach (Type type in types)
            {
                hasAny = true;
                Type captured = type; // 클로저가 루프 변수를 직접 잡지 않도록 복사
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(captured.Name)), false, () =>
                {
                    serializedObject.Update();

                    int newIndex = elements.arraySize;
                    elements.InsertArrayElementAtIndex(newIndex);
                    onAdd(elements.GetArrayElementAtIndex(newIndex), captured);

                    serializedObject.ApplyModifiedProperties();
                });
            }

            if (!hasAny)
            {
                menu.AddDisabledItem(new GUIContent(emptyMessage));
            }

            menu.ShowAsContext();
        }
    }
}