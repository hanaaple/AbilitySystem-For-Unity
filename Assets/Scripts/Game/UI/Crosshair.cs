using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using Game.Character;
using Game.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>화면 가운데 십자선. 캐릭터가 조준 태그를 가진 동안 좁아지고 진해지며, 머리를 가리키면 빨갛게 바뀐다. 설정 창이 열리면 숨긴다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private GameplayTag aimingTag;
        [Tooltip("이 태그의 부위(HitZone)를 가리키면 headColor로 바뀐다.")]
        [SerializeField] private GameplayTag headZoneTag;

        // 패널 기준 해상도(1920×1080)의 픽셀. 화면이 작으면 함께 줄어든다.
        [Header("Shape (px)")]
        [SerializeField] private float lineLength = 16f;
        [SerializeField] private float thickness = 4f;
        [Tooltip("선 바깥에 두르는 테두리 두께. 선 두께에 더해진다.")]
        [SerializeField] private float outlineWidth = 1f;
        [SerializeField] private float idleGap = 14f;
        [SerializeField] private float aimGap = 6f;

        [Header("Color")]
        [SerializeField] private Color idleColor = new(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color aimColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color headColor = new(1f, 0.1f, 0.1f, 1f);
        [Tooltip("밝은 배경에서도 보이게 두르는 테두리.")]
        [SerializeField] private Color outlineColor = new(0f, 0f, 0f, 0.6f);
        [SerializeField] private float blendSpeed = 10f;

        // 위·아래·왼쪽·오른쪽 순서
        private readonly VisualElement[] _lines = new VisualElement[4];
        private VisualElement _root;
        private VisualElement _dot;
        private bool _isAiming;
        private float _weight;

        private void OnEnable()
        {
            BuildLayout(GetComponent<UIDocument>().rootVisualElement);
            SettingsMenu.OnOpenChanged += SetHidden;
        }

        private void Start()
        {
            _isAiming = abilitySystemComponent.HasMatchingGameplayTag(aimingTag);
            abilitySystemComponent.RegisterGameplayTagEvent(aimingTag, OnTagCountChanged);
        }

        private void Update()
        {
            _weight = Mathf.MoveTowards(_weight, _isAiming ? 1f : 0f, blendSpeed * Time.unscaledDeltaTime);
            float gap = Mathf.Lerp(idleGap, aimGap, _weight);
            // 머리 위를 지나는 순간을 놓치지 않게 섞지 않고 바로 바꾼다
            Color color = IsAimingAtHead() ? headColor : Color.Lerp(idleColor, aimColor, _weight);

            PlaceLine(_lines[0], -thickness / 2f, -gap - lineLength, thickness, lineLength);
            PlaceLine(_lines[1], -thickness / 2f, gap, thickness, lineLength);
            PlaceLine(_lines[2], -gap - lineLength, -thickness / 2f, lineLength, thickness);
            PlaceLine(_lines[3], gap, -thickness / 2f, lineLength, thickness);

            foreach (VisualElement line in _lines)
            {
                line.style.backgroundColor = color;
            }

            _dot.style.backgroundColor = color;
        }

        private void OnDisable()
        {
            SettingsMenu.OnOpenChanged -= SetHidden;
        }

        private void OnDestroy()
        {
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, aimingTag);
            }
        }

        private void BuildLayout(VisualElement root)
        {
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1;

            // 크기 0인 중심점을 화면 가운데에 두고, 선들을 그 점 기준으로 배치한다
            _root = new VisualElement { pickingMode = PickingMode.Ignore };
            _root.style.position = Position.Absolute;
            _root.style.left = Length.Percent(50);
            _root.style.top = Length.Percent(50);
            root.Add(_root);

            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i] = MakePart();
            }

            _dot = MakePart();
            PlaceLine(_dot, -thickness / 2f, -thickness / 2f, thickness, thickness);
        }

        private VisualElement MakePart()
        {
            var part = new VisualElement { pickingMode = PickingMode.Ignore };
            part.style.position = Position.Absolute;
            part.style.borderTopWidth = outlineWidth;
            part.style.borderBottomWidth = outlineWidth;
            part.style.borderLeftWidth = outlineWidth;
            part.style.borderRightWidth = outlineWidth;
            part.style.borderTopColor = outlineColor;
            part.style.borderBottomColor = outlineColor;
            part.style.borderLeftColor = outlineColor;
            part.style.borderRightColor = outlineColor;
            _root.Add(part);
            return part;
        }

        // 테두리는 요소 안쪽을 차지하므로 그만큼 바깥으로 넓혀 선 두께를 지킨다
        private void PlaceLine(VisualElement line, float left, float top, float width, float height)
        {
            line.style.left = left - outlineWidth;
            line.style.top = top - outlineWidth;
            line.style.width = width + outlineWidth * 2f;
            line.style.height = height + outlineWidth * 2f;
        }

        private bool IsAimingAtHead()
        {
            if (player == null || !headZoneTag.IsValid())
            {
                return false;
            }

            HitZone zone = player.FindAimedHitZone();
            return zone != null && zone.ZoneTag.MatchesTag(headZoneTag);
        }

        private void SetHidden(bool isHidden)
        {
            _root.style.display = isHidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            _isAiming = newCount > 0;
        }
    }
}
