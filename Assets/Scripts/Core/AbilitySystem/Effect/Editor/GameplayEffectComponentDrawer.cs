using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>
    /// GE Component 본문 드로어. GE 인스펙터의 Components 목록이 요소 틀(머리 줄·Component Class 줄)을 그린 뒤 본문을 이 드로어에 맡긴다.
    /// 필드를 선언 순서대로 그리고, 라벨·툴팁은 필드 이름과 [Tooltip]에서 온다 — Component마다 설명을 바꾸려면 드로어가 아니라 필드 Attribute를 고친다.
    /// 그리기 자체가 달라야 하는 Component만 이 클래스를 상속해 <see cref="Layout"/>을 바꾼다. 머리 줄 접기는 목록이 그리므로 여기선 접기 없이 필드만 그린다.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayEffectComponent), useForChildren: true)]
    public class GameplayEffectComponentDrawer : PropertyDrawer
    {
        protected const float LineGap = 4f;

        // GE 인스펙터의 본문 안 접기(Magnitude·Tags)와 화살표 자리를 맞추고, 라벨 폭을 그만큼 줄여 값 열을 다른 목록과 같은 x에 둔다.
        protected const float FoldoutIndent = 10f;

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Layout(new Rect(0f, 0f, 0f, 0f), property, false);
        }

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = prevLabelWidth - FoldoutIndent;
            Layout(position, property, true);
            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        /// <summary>본문을 그리거나(<paramref name="draw"/>) 높이만 잰다. 높이와 그리기가 같은 함수라 어긋나지 않는다.</summary>
        /// <returns>본문 높이.</returns>
        protected virtual float Layout(Rect position, SerializedProperty component, bool draw)
        {
            float y = position.y;
            SerializedProperty child = component.Copy();
            SerializedProperty end = component.GetEndProperty();
            bool enterChildren = true;
            while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                y = LayoutField(position, y, child, draw);
            }

            return y - position.y;
        }

        /// <summary>필드 한 줄(여러 줄짜리면 그 높이만큼)을 그리고 다음 줄 y를 돌려준다. 줄 위에 LineGap을 둔다.</summary>
        protected static float LayoutField(Rect position, float y, SerializedProperty field, bool draw)
        {
            y += LineGap;
            float height = EditorGUI.GetPropertyHeight(field, true);
            if (draw)
            {
                EditorGUI.PropertyField(new Rect(position.x + FoldoutIndent, y, position.width - FoldoutIndent, height), field, true);
            }

            return y + height;
        }
    }
}
