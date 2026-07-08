using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>
    /// baseType의 구체 서브클래스를 검색 가능한 네이티브 팝업(Add Component 창과 동일한 AdvancedDropdown)으로 고른다.
    /// 첫 항목은 None(선택 해제). 선택 결과는 onSelected 콜백으로 넘긴다.
    /// </summary>
    internal sealed class SubclassAdvancedDropdown : AdvancedDropdown
    {
        // 팝업 내용이 꽉 차는 기준 크기. 드로어가 정렬 폭 기준으로도 참조한다.
        public const float MinWidth = 260f;
        public const float MinHeight = 320f;

        private readonly Type _baseType;
        private readonly Action<Type> _onSelected;

        // 목록에서 제외할 타입의 AssemblyQualifiedName(중복 선택 방지). null이면 제외 없음.
        private readonly IReadOnlyCollection<string> _excludedAqns;

        public SubclassAdvancedDropdown(Type baseType, IReadOnlyCollection<string> excludedAqns, Action<Type> onSelected, AdvancedDropdownState state)
            : base(state)
        {
            _baseType = baseType;
            _excludedAqns = excludedAqns;
            _onSelected = onSelected;
            minimumSize = new Vector2(MinWidth, MinHeight);
        }

        // 선택 결과(타입)를 항목에 직접 담는다. id 매핑에 의존하면 기본 id(0)가 "미선택"처럼 취급돼 None이 콜백에서 누락되므로,
        // 타입을 필드로 들고 id는 1부터 부여한다. Type == null 이면 None(선택 해제).
        private sealed class TypeItem : AdvancedDropdownItem
        {
            public readonly Type Type;

            public TypeItem(string name, Type type, int id) : base(name)
            {
                Type = type;
                this.id = id;
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(ObjectNames.NicifyVariableName(_baseType.Name));

            int id = 1;
            root.AddChild(new TypeItem("None", null, id++));
            root.AddSeparator();

            foreach (Type type in EditorTypeUtility.GetConcreteSubclasses(_baseType))
            {
                if (_excludedAqns != null && _excludedAqns.Contains(type.AssemblyQualifiedName))
                {
                    continue;
                }

                root.AddChild(new TypeItem(ObjectNames.NicifyVariableName(type.Name), type, id++));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeItem typeItem)
            {
                _onSelected(typeItem.Type);
            }
        }
    }
}
