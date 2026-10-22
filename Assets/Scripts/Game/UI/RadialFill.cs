using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>요소를 12시부터 시계 방향으로 비율만큼 덮는 부채꼴. 상태 아이콘의 남은 시간·스킬 쿨타임 덮개가 같이 쓴다.</summary>
    public static class RadialFill
    {
        /// <summary>generateVisualContent에서 부른다. 요소 밖은 부모의 overflow: hidden으로 잘라야 모서리까지 깔끔하다.</summary>
        public static void Draw(MeshGenerationContext context, float ratio, Color color)
        {
            if (ratio <= 0f)
            {
                return;
            }

            Rect rect = context.visualElement.contentRect;
            Vector2 center = rect.center;
            // 모서리까지 덮게 반지름을 대각선 절반으로 잡는다
            float radius = rect.size.magnitude * 0.5f;
            Painter2D painter = context.painter2D;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.Arc(center, radius, -90f, -90f + 360f * Mathf.Clamp01(ratio));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
