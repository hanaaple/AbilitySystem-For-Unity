using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Character
{
    /// <summary>
    /// 캐릭터가 태그를 가진 동안 화면 가장자리를 색으로 물들인다. 후처리가 아니라 UI 층이라 여러 상태가 동시에 켜져도 각자 색으로 겹쳐 보인다.
    /// HUD보다 아래에 그리도록 UIDocument의 Sort Order를 HUD보다 낮게 둔다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TagScreenOverlay : TagFadeEffect
    {
        private const int TextureSize = 128;

        [SerializeField] private Color color = new(0.2f, 0.9f, 0.35f, 0.6f);

        [Tooltip("화면 가운데에서 가장자리까지를 1로 볼 때 색이 나타나기 시작하는 거리. 클수록 테두리가 얇다.")]
        [Range(0f, 1.4f)] [SerializeField] private float innerRadius = 0.7f;

        private VisualElement _overlay;
        private Texture2D _edgeTexture;

        protected override void Start()
        {
            _edgeTexture = CreateEdgeTexture(innerRadius);

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0f;
            _overlay.style.top = 0f;
            _overlay.style.right = 0f;
            _overlay.style.bottom = 0f;
            _overlay.style.backgroundImage = _edgeTexture;
            _overlay.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(100f));
            _overlay.style.unityBackgroundImageTintColor = color;
            root.Add(_overlay);

            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(_edgeTexture);
        }

        protected override void ApplyWeight(float weight)
        {
            _overlay.style.opacity = weight;
        }

        // 가운데는 투명하고 가장자리로 갈수록 불투명한 흰 텍스처. 화면 비율대로 늘어나 타원 테두리가 된다
        private static Texture2D CreateEdgeTexture(float innerRadius)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 fromCenter = new Vector2(x + 0.5f, y + 0.5f) / TextureSize * 2f - Vector2.one;
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerRadius, Mathf.Sqrt(2f), fromCenter.magnitude));
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
