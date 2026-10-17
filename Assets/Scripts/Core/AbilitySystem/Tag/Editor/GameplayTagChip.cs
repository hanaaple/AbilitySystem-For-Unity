using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>태그 하나를 칩(이름 + 지우기 버튼)으로 그린다. 단일 태그 콤보와 컨테이너 목록이 함께 쓴다.</summary>
    /// <remarks>UE: SGameplayTagChip</remarks>
    internal static class GameplayTagChip
    {
        public enum ChipAction
        {
            None,
            Edit,
            Clear,
            Menu,
        }

        private const float ClearButtonWidth = 16f;
        private const float HorizontalPadding = 6f;
        private const float UnselectedAlpha = 0.5f;

        private static readonly GUIContent _clearContent = new("×", "Clear Tag");

        private static GUIStyle _textStyle;
        private static GUIStyle _clearButtonStyle;

        private static GUIStyle TextStyle => _textStyle ??= new GUIStyle(EditorStyles.label) { padding = new RectOffset((int)HorizontalPadding, 2, 0, 0), alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };

        private static GUIStyle ClearButtonStyle => _clearButtonStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0) };

        /// <summary>칩이 글자를 감싸는 데 필요한 너비.</summary>
        public static float GetWidth(GUIContent content, bool showClearButton)
        {
            Vector2 textSize = TextStyle.CalcSize(content);
            return textSize.x + HorizontalPadding + (showClearButton ? ClearButtonWidth : HorizontalPadding);
        }

        /// <summary>칩을 그리고 이번 이벤트에서 일어난 동작을 돌려준다. 본문 클릭 = 편집, × = 지우기, 우클릭 = 메뉴.</summary>
        /// <param name="isSelected">false면 흐리게 그린다(값이 없거나 일부 오브젝트에만 있는 태그).</param>
        public static ChipAction Draw(Rect rect, GUIContent content, bool showClearButton, bool isSelected)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.ContextClick && rect.Contains(currentEvent.mousePosition))
            {
                currentEvent.Use();
                return ChipAction.Menu;
            }

            // 우클릭의 MouseDown/Up이 버튼을 누르지 않게 한다. 메뉴는 이어서 오는 ContextClick으로 연다.
            if (currentEvent.isMouse && currentEvent.button != 0)
            {
                return ChipAction.None;
            }

            Color previousColor = GUI.color;
            if (!isSelected)
            {
                GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * UnselectedAlpha);
            }

            if (currentEvent.type == EventType.Repaint)
            {
                GUIStyle backgroundStyle = EditorStyles.miniButton;
                backgroundStyle.Draw(rect, false, false, false, false);
            }

            ChipAction action = ChipAction.None;
            Rect bodyRect = rect;
            if (showClearButton)
            {
                bodyRect.width -= ClearButtonWidth;
                var clearRect = new Rect(bodyRect.xMax, rect.y, ClearButtonWidth, rect.height);

                // 본문 버튼보다 먼저 그려야 × 영역의 클릭을 먼저 가져간다
                if (GUI.Button(clearRect, _clearContent, ClearButtonStyle))
                {
                    action = ChipAction.Clear;
                }
            }

            if (EditorGUI.DropdownButton(bodyRect, content, FocusType.Passive, TextStyle))
            {
                action = ChipAction.Edit;
            }

            GUI.color = previousColor;
            return action;
        }
    }
}
