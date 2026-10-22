using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Game.Character;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>화면 왼쪽 아래의 내 체력. 캐릭터 얼굴 아이콘 옆에 체력 줄과 마나 줄을 같은 모양(이름 · 바 · 현재/최대 숫자)으로 쌓는다. 맞을 때마다 깎인 구간이 어두운 조각으로 남았다가 조각마다 잠시 뒤 줄어든다. 실드가 있으면 체력 끝에 파란 칸으로 이어 붙인다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerHealthHud : MonoBehaviour
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private Texture2D portrait;

        // 패널 기준 해상도(1920×1080)의 픽셀. 화면이 작으면 함께 줄어든다.
        [Header("Layout (px)")]
        [SerializeField] private float screenMargin = 32f;
        [SerializeField] private float portraitSize = 88f;
        [SerializeField] private float barWidth = 360f;
        [SerializeField] private float barHeight = 16f;
        [SerializeField] private float valueFontSize = 20f;
        [Tooltip("줄 앞의 이름(HP·MP) 글자 크기와 칸 너비. 너비가 같아야 두 바의 시작이 맞는다.")]
        [SerializeField] private float resourceNameFontSize = 16f;
        [SerializeField] private float resourceNameWidth = 30f;

        [Header("Bar")]
        [SerializeField] private Color fillColor = Color.white;
        [SerializeField] private Color barBackgroundColor = new(0f, 0f, 0f, 0.75f);
        [Tooltip("깎인 구간 조각 색. 바가 흰색이라 어둡게 두되, 빈 칸(barBackgroundColor)과는 구분되게 한다.")]
        [SerializeField] private Color trailColor = new(0.45f, 0.45f, 0.45f, 1f);
        [Tooltip("조각마다, 생긴 뒤 줄어들기 시작할 때까지(초).")]
        [Min(0f)] [SerializeField] private float trailHoldTime = 0.6f;
        [Tooltip("조각이 줄어드는 속도(바 전체 길이 / 초).")]
        [Min(0.01f)] [SerializeField] private float trailShrinkSpeed = 1.5f;
        [SerializeField] private Color shieldColor = new(0.3f, 0.65f, 1f, 1f);

        [Header("Mana")]
        [SerializeField] private float manaBarHeight = 8f;
        [SerializeField] private float manaFontSize = 15f;
        [Tooltip("실드(파랑)와 구분되게 보라 쪽으로 둔다.")]
        [SerializeField] private Color manaColor = new(0.55f, 0.45f, 1f, 1f);

        [Header("Ticks")]
        [Min(1f)] [SerializeField] private float tickInterval = 100f;
        [Tooltip("화면이 기준보다 작으면 함께 줄어드는데, 2px 이하면 위치에 따라 0px로 반올림돼 사라진다.")]
        [SerializeField] private float tickWidth = 3f;
        [SerializeField] private Color tickColor = new(0f, 0f, 0f, 0.5f);

        [Header("Status")]
        [Tooltip("걸려 있으면 체력 바 색을 바꾸고 바 위에 아이콘·남은 시간을 띄우는 상태들. 여럿이면 목록 앞쪽의 색을 쓴다.")]
        [SerializeField] private List<StatusDisplay> statuses = new();
        [SerializeField] private float statusIconSize = 36f;
        [Tooltip("이 태그 아래 상태는 버프, 나머지는 디버프로 보인다.")]
        [SerializeField] private GameplayTag buffParentTag;
        [Tooltip("버프 아이콘 테두리. 디버프와 색·모서리로 구분한다.")]
        [SerializeField] private Color buffBorderColor = new(0.45f, 0.85f, 1f, 1f);
        [SerializeField] private Color debuffBorderColor = new(0.95f, 0.15f, 0.1f, 1f);

        private readonly List<VisualElement> _trailChunkElements = new();
        private readonly List<StatusIcon> _statusIcons = new();
        private HealthTrail _trail;
        private VisualElement _fill;
        private VisualElement _shieldFill;
        private VisualElement _trailLayer;
        private VisualElement _tickLayer;
        private Label _valueLabel;
        private VisualElement _manaRow;
        private VisualElement _manaFill;
        private Label _manaLabel;
        private string _shieldColorHex;
        private float _tickedBarScale;

        // 바 전체 길이에 해당하는 값. 평소엔 최대 체력, 체력+실드가 넘치면 그 합이다.
        private float _barScale;

        private void OnEnable()
        {
            _trail = new HealthTrail(trailHoldTime, trailShrinkSpeed);
            BuildLayout(GetComponent<UIDocument>().rootVisualElement);
        }

        // 값 변경 이벤트는 ASC Awake에서 만든 GE 컨테이너에 있어 Start에서 구독한다
        private void Start()
        {
            abilitySystemComponent.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Health).OnValueChange += OnHealthChanged;
        }

        private void Update()
        {
            float maxHealth = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.MaxHealth);
            if (maxHealth <= 0f)
            {
                return;
            }

            float health = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.Health);
            float shield = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.Shield);
            _barScale = Mathf.Max(maxHealth, health + shield);
            if (!Mathf.Approximately(_barScale, _tickedBarScale))
            {
                RebuildTicks(_barScale);
            }

            float healthRatio = Mathf.Clamp01(health / _barScale);
            _fill.style.width = Length.Percent(healthRatio * 100f);
            _shieldFill.style.display = shield > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            _shieldFill.style.left = Length.Percent(healthRatio * 100f);
            _shieldFill.style.width = Length.Percent(Mathf.Clamp01(shield / _barScale) * 100f);

            // 소수 피해가 쌓여도 0.3 남은 체력을 0으로 보이지 않게 올림한다
            string value = $"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}";
            _valueLabel.text = shield > 0f ? $"{value} <color=#{_shieldColorHex}>+{Mathf.CeilToInt(shield)}</color>" : value;

            _trail.Tick(Time.deltaTime);
            SyncTrailChunks();
            UpdateStatuses();
            UpdateMana();
        }

        private void OnDestroy()
        {
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Health).OnValueChange -= OnHealthChanged;
            }
        }

        private void BuildLayout(VisualElement root)
        {
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1;

            var hud = new VisualElement { pickingMode = PickingMode.Ignore };
            hud.style.position = Position.Absolute;
            hud.style.left = screenMargin;
            hud.style.bottom = screenMargin;
            hud.style.flexDirection = FlexDirection.Row;
            hud.style.alignItems = Align.FlexEnd;
            root.Add(hud);

            var portraitFrame = new VisualElement { pickingMode = PickingMode.Ignore };
            portraitFrame.style.width = portraitSize;
            portraitFrame.style.height = portraitSize;
            portraitFrame.style.backgroundColor = barBackgroundColor;
            portraitFrame.style.backgroundImage = portrait;
            SetBorder(portraitFrame, 2f, fillColor);
            hud.Add(portraitFrame);

            var column = new VisualElement { pickingMode = PickingMode.Ignore };
            column.style.marginLeft = 12f;
            hud.Add(column);
            column.Add(BuildStatusRow());

            // 체력·마나 줄은 같은 모양 — 숫자가 어느 바의 것인지 헷갈리지 않게 값을 바 바로 오른쪽에 둔다
            VisualElement healthRow = MakeResourceRow("HP", fillColor);
            column.Add(healthRow);

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.width = barWidth;
            bar.style.height = barHeight;
            bar.style.backgroundColor = barBackgroundColor;
            healthRow.Add(bar);

            _valueLabel = MakeValueLabel(valueFontSize, fillColor);
            healthRow.Add(_valueLabel);

            // 조각은 체력 칸 오른쪽(깎인 자리)에 생기므로 그리는 순서는 상관없지만, 겹치는 순간에도 체력이 위로 보이게 먼저 깐다
            _trailLayer = MakeFullLayer(bar);
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.style.position = Position.Absolute;
            _fill.style.left = 0f;
            _fill.style.top = 0f;
            _fill.style.bottom = 0f;
            _fill.style.backgroundColor = fillColor;
            bar.Add(_fill);

            _shieldFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _shieldFill.style.position = Position.Absolute;
            _shieldFill.style.top = 0f;
            _shieldFill.style.bottom = 0f;
            _shieldFill.style.backgroundColor = shieldColor;
            bar.Add(_shieldFill);
            _shieldColorHex = ColorUtility.ToHtmlStringRGB(shieldColor);

            _tickLayer = MakeFullLayer(bar);
            column.Add(BuildManaRow());
        }

        // 체력 줄과 같은 모양. 체력보다 덜 중요해 바와 숫자를 조금 작게 둔다.
        private VisualElement BuildManaRow()
        {
            _manaRow = MakeResourceRow("MP", manaColor);
            _manaRow.style.marginTop = 6f;

            var manaBar = new VisualElement { pickingMode = PickingMode.Ignore };
            manaBar.style.width = barWidth;
            manaBar.style.height = manaBarHeight;
            manaBar.style.backgroundColor = barBackgroundColor;
            _manaRow.Add(manaBar);

            _manaFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _manaFill.style.position = Position.Absolute;
            _manaFill.style.left = 0f;
            _manaFill.style.top = 0f;
            _manaFill.style.bottom = 0f;
            _manaFill.style.backgroundColor = manaColor;
            manaBar.Add(_manaFill);

            _manaLabel = MakeValueLabel(manaFontSize, manaColor);
            _manaRow.Add(_manaLabel);
            return _manaRow;
        }

        // 이름 · 바 · 숫자가 가운데 줄에 맞춰 나란히 놓이는 한 줄. 바와 숫자는 호출부가 붙인다.
        private VisualElement MakeResourceRow(string resourceName, Color color)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var nameLabel = new Label(resourceName) { pickingMode = PickingMode.Ignore };
            nameLabel.style.width = resourceNameWidth;
            nameLabel.style.fontSize = resourceNameFontSize;
            nameLabel.style.color = color;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.paddingLeft = 0f;
            nameLabel.style.paddingRight = 0f;
            nameLabel.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = new Color(0f, 0f, 0f, 0.8f) };
            row.Add(nameLabel);
            return row;
        }

        private static Label MakeValueLabel(float fontSize, Color color)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginLeft = 8f;
            label.style.paddingLeft = 0f;
            label.style.paddingTop = 0f;
            label.style.paddingBottom = 0f;
            label.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = new Color(0f, 0f, 0f, 0.8f) };
            return label;
        }

        // 최대 마나가 없는 캐릭터면 줄째 숨긴다
        private void UpdateMana()
        {
            float maxMana = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.MaxMana);
            _manaRow.style.display = maxMana > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (maxMana <= 0f)
            {
                return;
            }

            float mana = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.Mana);
            _manaFill.style.width = Length.Percent(Mathf.Clamp01(mana / maxMana) * 100f);
            // 코스트를 낼 수 있는지 숫자로 바로 보이게 내림한다(29.6이면 30이 아니라 29)
            _manaLabel.text = $"{Mathf.FloorToInt(mana)} / {Mathf.CeilToInt(maxMana)}";
        }

        private static VisualElement MakeFullLayer(VisualElement parent)
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0f;
            layer.style.right = 0f;
            layer.style.top = 0f;
            layer.style.bottom = 0f;
            parent.Add(layer);
            return layer;
        }

        private static void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        // 조각 수만큼 요소를 보이고 위치를 맞춘다. 남는 요소는 숨겨 두고 다음에 다시 쓴다.
        private void SyncTrailChunks()
        {
            IReadOnlyList<HealthTrail.Chunk> chunks = _trail.Chunks;
            while (_trailChunkElements.Count < chunks.Count)
            {
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.style.position = Position.Absolute;
                element.style.top = 0f;
                element.style.bottom = 0f;
                element.style.backgroundColor = trailColor;
                _trailLayer.Add(element);
                _trailChunkElements.Add(element);
            }

            for (int i = 0; i < _trailChunkElements.Count; i++)
            {
                VisualElement element = _trailChunkElements[i];
                bool isUsed = i < chunks.Count;
                element.style.display = isUsed ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isUsed)
                {
                    continue;
                }

                HealthTrail.Chunk chunk = chunks[i];
                element.style.left = Length.Percent(chunk.LowRatio * 100f);
                element.style.width = Length.Percent((chunk.HighRatio - chunk.LowRatio) * 100f);
            }
        }

        private void RebuildTicks(float barScale)
        {
            _tickedBarScale = barScale;
            _tickLayer.Clear();

            // 끝과 겹치는 눈금은 그리지 않는다
            for (float value = tickInterval; value < barScale; value += tickInterval)
            {
                var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                tick.style.position = Position.Absolute;
                tick.style.left = Length.Percent(value / barScale * 100f);
                tick.style.top = 0f;
                tick.style.bottom = 0f;
                tick.style.width = tickWidth;
                tick.style.marginLeft = -tickWidth / 2f;
                tick.style.backgroundColor = tickColor;
                _tickLayer.Add(tick);
            }
        }

        private void OnHealthChanged(OnAttributeChangeData data)
        {
            if (_barScale <= 0f)
            {
                return;
            }

            _trail.OnHealthRatioChanged(data.OldValue / _barScale, data.NewValue / _barScale);
        }

        private VisualElement BuildStatusRow()
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4f;

            foreach (StatusDisplay status in statuses)
            {
                var frame = new VisualElement { pickingMode = PickingMode.Ignore };
                frame.style.width = statusIconSize;
                frame.style.height = statusIconSize;
                frame.style.marginRight = 4f;
                frame.style.backgroundImage = status.icon;
                frame.style.justifyContent = Justify.FlexEnd;
                frame.style.alignItems = Align.FlexEnd;
                frame.style.overflow = Overflow.Hidden;
                // 버프는 둥근 하늘색, 디버프는 각진 빨강 — 아이콘 그림과 상관없이 한눈에 갈린다
                bool isBuff = buffParentTag.IsValid() && status.tag.MatchesTag(buffParentTag);
                SetBorder(frame, 3f, isBuff ? buffBorderColor : debuffBorderColor);
                float radius = isBuff ? statusIconSize * 0.3f : 0f;
                frame.style.borderTopLeftRadius = radius;
                frame.style.borderTopRightRadius = radius;
                frame.style.borderBottomLeftRadius = radius;
                frame.style.borderBottomRightRadius = radius;
                if (status.icon == null)
                {
                    frame.style.backgroundColor = status.barColor;
                }
                row.Add(frame);

                var timeLabel = new Label { pickingMode = PickingMode.Ignore };
                timeLabel.style.fontSize = statusIconSize * 0.4f;
                timeLabel.style.color = Color.white;
                timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                timeLabel.style.paddingRight = 2f;
                timeLabel.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = Color.black };
                var tags = new GameplayTagContainer();
                tags.AddTag(status.tag);
                var icon = new StatusIcon(frame, timeLabel, GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(tags));

                // 지난 시간만큼 어두운 부채꼴이 12시부터 시계 방향으로 차오른다. 시간 숫자가 덮개 위에 오게 먼저 깐다.
                var clock = new VisualElement { pickingMode = PickingMode.Ignore };
                clock.style.position = Position.Absolute;
                clock.style.left = 0f;
                clock.style.right = 0f;
                clock.style.top = 0f;
                clock.style.bottom = 0f;
                clock.generateVisualContent += context => RadialFill.Draw(context, icon.ElapsedRatio, new Color(0f, 0f, 0f, 0.6f));
                frame.Add(clock);
                frame.Add(timeLabel);

                icon.Clock = clock;
                _statusIcons.Add(icon);
            }

            return row;
        }

        // 체력 바 색은 걸린 상태 중 목록 앞쪽 것을, 아이콘은 걸린 상태마다 남은 시간과 함께 보인다
        private void UpdateStatuses()
        {
            Color barColor = fillColor;
            bool hasBarColor = false;
            for (int i = 0; i < statuses.Count; i++)
            {
                StatusDisplay status = statuses[i];
                StatusIcon icon = _statusIcons[i];
                bool isActive = abilitySystemComponent.HasMatchingGameplayTag(status.tag);
                icon.Frame.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isActive)
                {
                    continue;
                }

                if (!hasBarColor && status.tintsHealthBar)
                {
                    barColor = status.barColor;
                    hasBarColor = true;
                }

                UpdateStatusTime(icon);
            }

            _fill.style.backgroundColor = barColor;
        }

        // 가장 늦게 끝나는 GE 기준. 끝나는 시간이 없는 GE(구간 안에 있는 동안 등)가 하나라도 있으면 시간·시계를 비운다.
        private void UpdateStatusTime(StatusIcon icon)
        {
            float longest = 0f;
            float duration = 0f;
            bool isInfinite = false;
            foreach ((float remaining, float total) in abilitySystemComponent.GetActiveEffectsTimeRemainingAndDuration(icon.Query))
            {
                if (Mathf.Approximately(remaining, GameplayEffectConstants.InfiniteDuration))
                {
                    isInfinite = true;
                    break;
                }

                if (remaining > longest)
                {
                    longest = remaining;
                    duration = total;
                }
            }

            bool hasTime = !isInfinite && longest > 0f && duration > 0f;
            icon.TimeLabel.text = hasTime ? Mathf.CeilToInt(longest).ToString() : string.Empty;
            float elapsedRatio = hasTime ? 1f - longest / duration : 0f;
            if (!Mathf.Approximately(elapsedRatio, icon.ElapsedRatio))
            {
                icon.ElapsedRatio = elapsedRatio;
                icon.Clock.MarkDirtyRepaint();
            }
        }

        [System.Serializable]
        private class StatusDisplay
        {
            public GameplayTag tag;
            [Tooltip("걸려 있는 동안 체력 칸을 barColor로 칠한다. 끄면 아이콘만 띄운다(아이콘 그림이 없으면 barColor 칸).")]
            public bool tintsHealthBar = true;
            public Color barColor = new(1f, 0.45f, 0.1f, 1f);
            public Texture2D icon;
        }

        private class StatusIcon
        {
            public readonly VisualElement Frame;
            public readonly Label TimeLabel;
            public readonly GameplayEffectQuery Query;
            public VisualElement Clock;
            public float ElapsedRatio;

            public StatusIcon(VisualElement frame, Label timeLabel, GameplayEffectQuery query)
            {
                Frame = frame;
                TimeLabel = timeLabel;
                Query = query;
            }
        }
    }
}
