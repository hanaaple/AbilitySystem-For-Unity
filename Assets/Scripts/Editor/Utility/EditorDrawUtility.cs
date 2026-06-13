using UnityEditor;
using UnityEngine;

namespace Editor.Utility
{
    /// <summary>
    /// CustomEditor·PropertyDrawer 사이에서 반복되는 GUI 그리기 헬퍼 모음.
    /// </summary>
    public static class EditorDrawUtility
    {
        private const float DragHandleWidth = 15f;

        private static GUIStyle _boldFoldoutStyle;

        /// <summary>볼드 폰트 Foldout 스타일.</summary>
        // OnGUI가 매 프레임 호출되므로 한 번만 생성해 재사용
        public static GUIStyle BoldFoldoutStyle => _boldFoldoutStyle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        /// <summary>드래그 핸들 너비만큼 들여쓴 Foldout 한 줄 Rect를 반환한다.</summary>
        public static Rect GetFoldoutRect(Rect rect)
        {
            return new Rect(rect.x + DragHandleWidth, rect.y, rect.width - DragHandleWidth, EditorGUIUtility.singleLineHeight);
        }
    }
}
