using System;
using System.Collections.Generic;
using System.Linq;
using Core.Common;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>
    /// [SubclassSelector(typeof(Base))]가 붙은 AQN 문자열 필드를 Base의 구체 서브클래스 드롭다운으로 그린다. None=빈 문자열, 해결 실패="Missing". useForChildren로 상속한 별칭 속성도 처리하며, 베이스 타입은 속성 인스턴스가 들어 타입별 드로어 서브클래스가 필요 없다.
    /// 오브젝트 참조 필드처럼 보이게 선택된 타입의 소스 스크립트를 왼쪽 필드로 그린다 — 단일 클릭→Project ping, 더블 클릭→스크립트 열기. 타입 선택은 오른쪽 드롭다운 버튼.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute), useForChildren: true)]
    public sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        private const float DropdownWidth = 18f;
        private const float Gap = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 범용 드로어는 제외 없이 그린다(excluded: null). 특정 스코프 중복 제외가 필요한 커스텀 드로어는
            // 스스로 excluded를 모아 DrawSelector를 직접 호출한다 — 이 드로어는 "무엇을 제외할지"를 알지 않는다.
            var attr = (SubclassSelectorAttribute)attribute;
            DrawSelector(position, property, attr.BaseType, null, label);
        }

        /// <summary>
        /// SubclassSelector UI(스크립트 ping/open 필드 + 검색 팝업)를 그리는 재사용 진입점. <paramref name="excluded"/> AQN들은 팝업에서 뺀다(예: 리스트 중복 제외), null이면 제외 없음.
        /// 어느 범위에서 제외할지 수집은 이 드로어가 하지 않고 호출측이 모아 넘긴다(<see cref="CollectSiblingValues"/> 등).
        /// </summary>
        public static void DrawSelector(Rect position, SerializedProperty property, Type baseType, IReadOnlyCollection<string> excluded, GUIContent label, bool allowCreateNew = false)
        {
            position = EditorGUI.PrefixLabel(position, label);

            string aqn = property.stringValue;
            Type current = string.IsNullOrEmpty(aqn) ? null : Type.GetType(aqn);

            Rect scriptRect = new Rect(position.x, position.y, position.width - DropdownWidth - Gap, position.height);
            Rect dropdownRect = new Rect(position.xMax - DropdownWidth, position.y, DropdownWidth, position.height);

            DrawScriptField(scriptRect, current, aqn);

            // 오른쪽 작은 풀다운 버튼으로 타입 선택(None 포함) — 검색 가능한 네이티브 팝업.
            // 팝업 기준은 버튼이 아니라 필드 영역(왼쪽 정렬). 단 필드가 팝업 최소폭보다 넓으면 필드 폭을 따라 과하게 넓어지지 않도록 최소폭으로 고정한다.
            if (EditorGUI.DropdownButton(dropdownRect, GUIContent.none, FocusType.Keyboard, EditorStyles.miniPullDown))
            {
                Rect popupRect = position;
                if (popupRect.width > SubclassAdvancedDropdown.MinWidth)
                {
                    popupRect.width = SubclassAdvancedDropdown.MinWidth;
                }

                // New Script 팝업은 이 필드 영역 기준으로 띄우므로 화면 좌표를 지금(OnGUI 안) 계산해 넘긴다.
                Rect activatorScreenRect = allowCreateNew
                    ? new Rect(GUIUtility.GUIToScreenPoint(position.position), position.size)
                    : default;
                ShowTypeDropdown(popupRect, property, baseType, excluded, allowCreateNew, activatorScreenRect);
            }
        }

        // 선택된 런타임 타입의 소스 스크립트를 오브젝트 필드처럼 그린다.
        // 단일 클릭 → Project 하이라이트(ping), 더블 클릭 → 스크립트 열기. 오브젝트 참조 필드의 UX를 타입 문자열에 흉내낸다.
        private static void DrawScriptField(Rect rect, Type current, string aqn)
        {
            MonoScript script = current != null ? EditorTypeUtility.FindScript(current) : null;

            string display = current != null
                ? ObjectNames.NicifyVariableName(current.Name)
                : string.IsNullOrEmpty(aqn) ? "None" : "Missing";

            Texture icon = script != null ? AssetPreview.GetMiniThumbnail(script) : null;
            var content = new GUIContent(display, icon);

            // 스크립트를 못 찾으면(None·Missing·파일명≠클래스명) ping/open 대상이 없으므로 비활성 표시.
            // 시각만 오브젝트 필드 스타일 박스로 그린다.
            using (new EditorGUI.DisabledScope(script == null))
            {
                GUI.Label(rect, content, EditorStyles.objectField);
            }

            // 클릭/더블클릭은 MouseDown 이벤트에서 직접 처리한다.
            // GUI.Button은 MouseUp에 발동하는데 그 시점 clickCount가 더블클릭을 신뢰성 있게 담지 못한다.
            if (script != null && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount >= 2)
                {
                    AssetDatabase.OpenAsset(script);
                }
                else
                {
                    EditorGUIUtility.PingObject(script);
                }

                Event.current.Use();
            }
        }

        private static void ShowTypeDropdown(Rect rect, SerializedProperty property, Type baseType, IReadOnlyCollection<string> excluded, bool allowCreateNew, Rect activatorScreenRect)
        {
            // SerializedProperty는 콜백 시점에 무효화될 수 있어 serializedObject + path로 재조회한다.
            SerializedObject so = property.serializedObject;
            string path = property.propertyPath;
            UnityEngine.Object target = so.targetObject;

            IReadOnlyList<Type> types = EditorTypeUtility.GetConcreteSubclasses(baseType);
            if (excluded != null && excluded.Count > 0)
            {
                types = types.Where(type => !excluded.Contains(type.AssemblyQualifiedName)).ToList();
            }

            Action onNewScript = allowCreateNew
                ? () => NewSubclassScript.OpenForField(activatorScreenRect, baseType, target, path)
                : null;

            var dropdown = new SubclassAdvancedDropdown(
                ObjectNames.NicifyVariableName(baseType.Name), types, includeNone: true,
                type => Assign(so, path, type), onNewScript, emptyMessage: null, new AdvancedDropdownState());
            dropdown.Show(rect);
        }

        /// <summary>
        /// 배열 요소 필드 하나만 받아 "같은 배열의 다른 요소들이 이 필드에 담은 값"을 모은다(자신 제외).
        /// PropertyDrawer는 자기 필드 하나만 받아 이 필드가 어느 배열의 몇 번째인지 모르므로(요소는 부모 배열 참조가 없다), propertyPath에서 배열·인덱스를 역산해 <see cref="CollectArrayValues"/>에 위임한다 — 내 인덱스를 제외로 넘겨 형제들만 모은다.
        /// "리스트 내 중복 제외"가 필요한 드로어가 이걸로 excluded를 만들어 <see cref="DrawSelector"/>에 넘긴다. 배열 요소가 아니면 빈 집합.
        /// </summary>
        public static HashSet<string> CollectSiblingValues(SerializedProperty property)
        {
            // propertyPath에서 "내가 속한 배열"과 "내 인덱스"를 역산한다. 경로 문자열의 구조:
            //   "attributeSets.Array.data[2].attributeSetTypeName"
            //    └─ 배열 경로 ─┘            └─ 요소 안 필드 상대경로 ─┘
            //                          └ 내 인덱스(2) ┘
            string path = property.propertyPath;
            const string token = ".Array.data[";

            // 배열 마디를 찾는다. 없으면 배열 요소가 아니라는 뜻(→ 제외할 형제도 없음). 중첩 배열이면 나를 직접 감싼 "가장 안쪽" 배열을 잡는다(LastIndexOf).
            int idx = path.LastIndexOf(token, StringComparison.Ordinal);
            if (idx < 0)
            {
                return new HashSet<string>();
            }

            string arrayPath = path.Substring(0, idx); // token 앞부분 = 배열 경로

            // "[2]" 안의 숫자 = 내 인덱스.
            int bracketStart = idx + token.Length;
            int bracketEnd = path.IndexOf(']', bracketStart);
            if (bracketEnd < 0 || !int.TryParse(path.Substring(bracketStart, bracketEnd - bracketStart), out int currentIndex))
            {
                return new HashSet<string>();
            }

            string relativePath = path.Substring(bracketEnd + 2); // "]." 뒤 = 요소 안에서 이 필드까지의 상대 경로

            // 요소는 부모를 못 가리키므로, 배열은 경로로 루트에서 되찾는다.
            SerializedProperty arrayProp = property.serializedObject.FindProperty(arrayPath);

            // 재료가 다 모였으니 코어에 위임. 내 인덱스를 제외 → 형제들 값만 모인다.
            return CollectArrayValues(arrayProp, relativePath, currentIndex);
        }

        /// <summary>배열 각 요소에서 relativePath 필드의 문자열 값을 모은다(비어있지 않은 것만). excludeIndex는 건너뛴다(-1=전체). 요소 기반(<see cref="CollectSiblingValues"/>, 자기 제외)과 배열 기반(컨테이너 드로어, 전체) 수집이 공유하는 코어.</summary>
        public static HashSet<string> CollectArrayValues(SerializedProperty arrayProperty, string relativePath, int excludeIndex = -1)
        {
            var result = new HashSet<string>();
            if (arrayProperty == null || !arrayProperty.isArray)
            {
                return result;
            }

            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                if (i == excludeIndex)
                {
                    continue;
                }

                SerializedProperty field = arrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative(relativePath);
                if (field != null && !string.IsNullOrEmpty(field.stringValue))
                {
                    result.Add(field.stringValue);
                }
            }

            return result;
        }

        private static void Assign(SerializedObject so, string path, Type type)
        {
            so.Update();
            so.FindProperty(path).stringValue = type != null ? type.AssemblyQualifiedName : string.Empty;
            so.ApplyModifiedProperties();
        }
    }
}
