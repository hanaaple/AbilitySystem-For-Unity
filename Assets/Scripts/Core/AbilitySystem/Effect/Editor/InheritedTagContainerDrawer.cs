using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>
    /// InheritedTagContainer를 접기 제목 + 필드 세 줄로 그린다. 라벨·툴팁은 필드 이름·[Tooltip]을 그대로 쓰고,
    /// 결과(_combinedTags)만 읽기 전용으로 둔다 — GE가 OnGameplayEffectChanged(편집 시 OnValidate)에서 다시 계산하므로 고쳐도 덮어쓰인다.
    /// </summary>
    [CustomPropertyDrawer(typeof(InheritedTagContainer))]
    public sealed class InheritedTagContainerDrawer : PropertyDrawer
    {
        private const string CombinedTagsName = "_combinedTags";
        private const float LineGap = 4f;

        // 부모 인스펙터의 다른 접기와 화살표 자리·값 열을 맞춘다.
        private const float FoldoutIndent = 10f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Layout(new Rect(0f, 0f, 0f, 0f), property, label, false);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Layout(position, property, label, true);
        }

        private static float Layout(Rect position, SerializedProperty property, GUIContent label, bool draw)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            if (draw)
            {
                property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineH), property.isExpanded, label, true);
            }

            float y = position.y + lineH;
            if (!property.isExpanded)
            {
                return y - position.y;
            }

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = prevLabelWidth - FoldoutIndent;

            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enterChildren = true;
            while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                y += LineGap;
                float height = EditorGUI.GetPropertyHeight(child, true);
                if (draw)
                {
                    using (new EditorGUI.DisabledScope(child.name == CombinedTagsName))
                    {
                        EditorGUI.PropertyField(new Rect(position.x + FoldoutIndent, y, position.width - FoldoutIndent, height), child, true);
                    }
                }

                y += height;
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;
            return y - position.y;
        }
    }
}
