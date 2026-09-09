using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>
    /// 다형(polymorphic) 리스트를 "타입 선택 팝업이 달린 +버튼"까지 배선한 <see cref="ReorderableList"/>로 만들어 주는 빌더. 드로어는 OnEnable에서 Create 한 번만 부르면 된다.
    /// 호출부가 정하는 것: (1) 후보 타입 출처(typeProvider) (2) 고른 타입으로 새 요소를 채우는 법(onAdd — SerializeReference면 managedReferenceValue, typeName 방식이면 문자열)
    /// (3) (선택) New Script — newScriptBaseType을 주면 add 팝업에 "New Script..."가 뜨고 새 서브클래스를 만들어 리로드 뒤 자동 추가(typeName 방식 전용).
    /// 타입 팝업은 검색 가능한 <see cref="SubclassAdvancedDropdown"/>. 요소 삽입·Apply·빈 상태·배선은 빌더가, 요소 그리기/높이는 기본(PropertyField) 또는 콜백으로.
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
            ReorderableList.ElementHeightCallbackDelegate elementHeight = null,
            Type newScriptBaseType = null,
            string newScriptTypeRelPath = null,
            string newScriptClearArrayRelPath = null)
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
                    ShowAddDropdown(serializedObject, elements, header, typeProvider(), onAdd, emptyMessage,
                        buttonRect, newScriptBaseType, newScriptTypeRelPath, newScriptClearArrayRelPath),
            };
        }

        // 후보 타입을 검색 가능한 AdvancedDropdown으로 띄우고, 선택 시 요소를 삽입한 뒤 onAdd로 채운다.
        // newScriptBaseType이 있으면 "New Script..."로 새 서브클래스를 만들어 리로드 뒤 새 요소로 추가한다.
        private static void ShowAddDropdown(
            SerializedObject serializedObject,
            SerializedProperty elements,
            string header,
            IEnumerable<Type> types,
            Action<SerializedProperty, Type> onAdd,
            string emptyMessage,
            Rect buttonRect,
            Type newScriptBaseType,
            string newScriptTypeRelPath,
            string newScriptClearArrayRelPath)
        {
            IReadOnlyList<Type> typeList = types as IReadOnlyList<Type> ?? types.ToList();

            Action<Type> onSelected = type =>
            {
                serializedObject.Update();

                int newIndex = elements.arraySize;
                elements.InsertArrayElementAtIndex(newIndex);
                onAdd(elements.GetArrayElementAtIndex(newIndex), type);

                serializedObject.ApplyModifiedProperties();
            };

            Action onNewScript = null;
            if (newScriptBaseType != null)
            {
                // 리로드로 무효화되므로 대상·배열 경로를 지금 잡아 콜백에 캡처한다.
                UnityEngine.Object target = serializedObject.targetObject;
                string arrayPath = elements.propertyPath;
                Rect activatorScreenRect = new Rect(GUIUtility.GUIToScreenPoint(buttonRect.position), buttonRect.size);

                onNewScript = () => NewSubclassScript.OpenForListAdd(
                    activatorScreenRect, newScriptBaseType, target, arrayPath, newScriptTypeRelPath, newScriptClearArrayRelPath);
            }

            var dropdown = new SubclassAdvancedDropdown(
                header, typeList, includeNone: false, onSelected, onNewScript, emptyMessage, new AdvancedDropdownState());
            dropdown.Show(buttonRect);
        }
    }
}
