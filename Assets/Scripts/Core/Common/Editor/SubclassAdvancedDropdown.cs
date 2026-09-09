using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>타입 목록을 검색 가능한 네이티브 팝업(Add Component 창과 동일한 AdvancedDropdown)으로 고른다. 후보 타입·None 포함·"New Script..." 노출은 호출부가 정하고(필드 셀렉터·다형 리스트 add 공용), 선택은 onSelected·"New Script..."는 onNewScript 콜백으로 넘긴다.</summary>
    internal sealed class SubclassAdvancedDropdown : AdvancedDropdown
    {
        // 팝업 내용이 꽉 차는 기준 크기. 드로어가 정렬 폭 기준으로도 참조한다.
        public const float MinWidth = 260f;
        public const float MinHeight = 320f;

        private readonly string _title;
        private readonly IReadOnlyList<Type> _types;
        private readonly bool _includeNone;
        private readonly Action<Type> _onSelected;

        // "New Script..." 선택 시 콜백. null이면 항목을 아예 넣지 않는다(호출부별 opt-in).
        private readonly Action _onNewScript;

        // 후보가 하나도 없을 때 보여줄 비활성 안내(무반응처럼 보이지 않게). null이면 표시 안 함.
        private readonly string _emptyMessage;

        public SubclassAdvancedDropdown(
            string title,
            IReadOnlyList<Type> types,
            bool includeNone,
            Action<Type> onSelected,
            Action onNewScript,
            string emptyMessage,
            AdvancedDropdownState state)
            : base(state)
        {
            _title = title;
            _types = types;
            _includeNone = includeNone;
            _onSelected = onSelected;
            _onNewScript = onNewScript;
            _emptyMessage = emptyMessage;
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

        // "New Script..." 전용 마커 항목. 타입 선택이 아니라 새 스크립트 생성 흐름으로 분기시킨다.
        private sealed class NewScriptItem : AdvancedDropdownItem
        {
            public NewScriptItem(string name, int id) : base(name)
            {
                this.id = id;
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(_title);

            int id = 1;

            if (_includeNone)
            {
                root.AddChild(new TypeItem("None", null, id++));
                root.AddSeparator();
            }

            foreach (Type type in _types)
            {
                root.AddChild(new TypeItem(ObjectNames.NicifyVariableName(type.Name), type, id++));
            }

            if (_onNewScript != null)
            {
                root.AddSeparator();
                root.AddChild(new NewScriptItem("New Script...", id++));
            }

            // 고를 수 있는 항목(None·타입·New Script)이 하나도 없으면 안내를 비활성으로 보여준다.
            if (!_includeNone && _types.Count == 0 && _onNewScript == null && !string.IsNullOrEmpty(_emptyMessage))
            {
                root.AddChild(new AdvancedDropdownItem(_emptyMessage) { enabled = false });
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is NewScriptItem)
            {
                _onNewScript();
                return;
            }

            if (item is TypeItem typeItem)
            {
                _onSelected(typeItem.Type);
            }
        }
    }
}
