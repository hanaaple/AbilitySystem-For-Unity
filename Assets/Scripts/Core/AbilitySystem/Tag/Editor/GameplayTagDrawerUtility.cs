using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary><see cref="GameplayTagPropertyDrawer"/>·<see cref="GameplayTagContainerPropertyDrawer"/>가 함께 쓰는 손잡이 캐시·피커 열기·메뉴 헬퍼.</summary>
    internal static class GameplayTagDrawerUtility
    {
        // 질의 편집 창의 편집용 오브젝트 → 원래 Query 필드의 Categories. 창 안 컨테이너 필드는 자기 fieldInfo에 속성이 없어 여기서 돌려받는다
        private static readonly Dictionary<Object, string> _categoriesOverrides = new();

        /// <summary><paramref name="target"/> 아래 필드에 <see cref="GameplayTagCategoriesAttribute"/>가 없으면 <paramref name="categories"/>를 쓰게 한다.</summary>
        /// <remarks>UE: SGameplayTagQueryWidget::OnGetCategoriesMetaFromPropertyHandle</remarks>
        public static void SetCategoriesOverride(Object target, string categories)
        {
            _categoriesOverrides[target] = categories;
        }

        public static void RemoveCategoriesOverride(Object target)
        {
            _categoriesOverrides.Remove(target);
        }

        /// <summary>
        /// 필드의 손잡이를 캐시에서 꺼내거나 만든다. 같은 드로어 인스턴스가 List 요소마다 불리므로 경로로 나눈다.
        /// 손잡이는 태그 값을 캐시하지 않고 매번 다시 읽으므로, 요소가 밀려 경로가 다른 요소를 가리켜도 틀린 값을 내지 않는다.
        /// </summary>
        public static GameplayTagPropertyHandle GetPropertyHandle(Dictionary<string, GameplayTagPropertyHandle> propertyHandles, SerializedProperty property, bool isContainer, FieldInfo fieldInfo)
        {
            string propertyPath = property.propertyPath;
            if (propertyHandles.TryGetValue(propertyPath, out GameplayTagPropertyHandle propertyHandle) && propertyHandle.Matches(property))
            {
                return propertyHandle;
            }

            GameplayTagCategoriesAttribute categoriesAttribute = fieldInfo?.GetCustomAttribute<GameplayTagCategoriesAttribute>();
            string categories = categoriesAttribute?.Categories;
            if (categories == null)
            {
                SerializedObject serializedObject = property.serializedObject;
                _categoriesOverrides.TryGetValue(serializedObject.targetObject, out categories);
            }

            propertyHandle = new GameplayTagPropertyHandle(property, isContainer, categories);
            propertyHandles[propertyPath] = propertyHandle;
            return propertyHandle;
        }

        /// <summary>필드 아래에 피커 팝업을 연다. 열기 전에 미등록 태그를 지운다.</summary>
        /// <param name="tagToHilight">펼쳐 보여 줄 태그. 없으면 null.</param>
        public static void OpenPicker(Rect activatorRect, GameplayTagPropertyHandle propertyHandle, string tagToHilight)
        {
            EditorWindow ownerWindow = EditorWindow.focusedWindow;
            GameplayTagPicker.VerifyAssetTagValidity(propertyHandle);
            PopupWindow.Show(activatorRect, new GameplayTagPickerPopup(propertyHandle, tagToHilight, ownerWindow));
        }

        /// <summary>모든 오브젝트에 같은 태그 목록을 쓴다.</summary>
        public static void SetAllObjectValues(GameplayTagPropertyHandle propertyHandle, string undoName, List<string> tagNames)
        {
            var perObjectTagNames = new List<List<string>>(propertyHandle.TargetCount);
            for (int i = 0; i < propertyHandle.TargetCount; i++)
            {
                perObjectTagNames.Add(new List<string>(tagNames));
            }

            propertyHandle.SetPerObjectValues(undoName, perObjectTagNames);
        }

        public static void RepaintWindow(EditorWindow window)
        {
            if (window != null)
            {
                window.Repaint();
            }
        }
    }
}
