using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Game.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// 게임 화면 안의 GAS 디버그 패널(UE showdebug abilitysystem·Gameplay Debugger의 Abilities 카테고리에 해당).
    /// 왼쪽은 플레이어, 오른쪽은 십자선으로 겨눈 대상 — 다른 대상을 겨누기 전까지 유지해서 조준을 풀어도 사라지지 않는다.
    /// 토글 키로 끔 → 간단(어트리뷰트·GE·태그) → 상세(+ 어빌리티·이벤트 로그·대상 정보)를 돌린다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DebugOverlay : MonoBehaviour
    {
        private const char ColumnSeparator = '\t';
        private const char HeaderPrefix = '#';

        private enum Mode
        {
            Off,
            Compact,
            Full,
        }

        [SerializeField] private PlayerCharacter player;
        [SerializeField] private Key toggleKey = Key.F1;

        [Header("Target")]
        [SerializeField] private float targetDistance = 60f;
        [SerializeField] private LayerMask targetLayers = ~0;

        // 패널 기준 해상도(1920×1080)의 픽셀. 화면이 작으면 함께 줄어든다.
        [Header("Look (px)")]
        [SerializeField] private float panelWidth = 440f;
        [Tooltip("이보다 길면 아래를 자른다 — 왼쪽 아래 체력 HUD를 가리지 않게.")]
        [SerializeField] private float maxPanelHeight = 820f;
        [SerializeField] private float screenMargin = 16f;
        [SerializeField] private float fontSize = 14f;
        [SerializeField] private float thumbnailSize = 72f;
        [SerializeField] private Color panelColor = new(0.04f, 0.05f, 0.08f, 0.78f);
        [SerializeField] private Color headerColor = new(1f, 0.82f, 0.35f, 1f);
        [SerializeField] private Color textColor = new(0.92f, 0.94f, 0.97f, 1f);
        [Tooltip("값이 바뀐 어트리뷰트를 잠깐 칠하는 색.")]
        [SerializeField] private Color changedColor = new(1f, 0.9f, 0.25f, 1f);
        [SerializeField] private float changedHighlightDuration = 0.6f;
        [Tooltip("패널 내용을 다시 만드는 간격(초).")]
        [SerializeField] private float refreshInterval = 0.1f;

        private readonly Dictionary<AbilitySystemComponent, AbilitySystemDebugLog> _logs = new();
        private readonly Dictionary<(AbilitySystemComponent, string), ValueHistory> _valueHistory = new();
        private readonly List<GameplayAttributeHandle> _attributeBuffer = new();
        private readonly List<string> _lines = new();

        private Mode _mode = Mode.Off;
        private AbilitySystemComponent _playerAsc;
        private Panel _playerPanel;
        private Panel _targetPanel;
        private Label _modeLabel;
        private VisualElement _root;
        private bool _isSettingsOpen;
        private float _refreshTimer;

        private GameObject _targetObject;
        private IDebugOverlayTarget _targetProvider;
        private AbilitySystemComponent _targetAsc;

        private void OnEnable()
        {
            BuildLayout(GetComponent<UIDocument>().rootVisualElement);
            SettingsMenu.OnOpenChanged += OnSettingsOpenChanged;
        }

        private void Start()
        {
            _playerAsc = player.GetComponent<AbilitySystemComponent>();
            GetLog(_playerAsc);
            ApplyMode();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                _mode = (Mode)(((int)_mode + 1) % 3);
                ApplyMode();
                _refreshTimer = 0f;
            }

            if (_mode == Mode.Off)
            {
                return;
            }

            UpdateTarget();

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f)
            {
                return;
            }

            _refreshTimer = refreshInterval;
            RefreshPlayerPanel();
            RefreshTargetPanel();
        }

        private void OnDisable()
        {
            SettingsMenu.OnOpenChanged -= OnSettingsOpenChanged;
        }

        private void OnDestroy()
        {
            foreach (AbilitySystemDebugLog log in _logs.Values)
            {
                log.Dispose();
            }

            _logs.Clear();
        }

        // ── 대상 고르기 ─────────────────────────────────────────────────────────

        // 화면 가운데 레이에 처음 걸린 디버그 대상(IDebugOverlayTarget 또는 ASC). 아무것도 없으면 이전 대상을 그대로 둔다.
        private void UpdateTarget()
        {
            if (_targetObject == null)
            {
                ClearTarget();
            }

            Camera viewCamera = Camera.main;
            if (viewCamera == null)
            {
                return;
            }

            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // 카메라와 캐릭터 사이에 걸린 것은 겨눈 것이 아니다
            float minDistance = Vector3.Dot(player.transform.position + Vector3.up - ray.origin, ray.direction) - 0.5f;
            RaycastHit[] hits = Physics.RaycastAll(ray, targetDistance, targetLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                Collider hitCollider = hit.collider;
                if (hit.distance < minDistance || hitCollider.transform.IsChildOf(player.transform))
                {
                    continue;
                }

                var provider = hitCollider.GetComponentInParent<IDebugOverlayTarget>();
                AbilitySystemComponent asc = provider != null ? provider.DebugAbilitySystem : hitCollider.GetComponentInParent<AbilitySystemComponent>();
                if (provider != null || asc != null)
                {
                    SetTarget(provider != null ? ((Component)provider).gameObject : asc.gameObject, provider, asc);
                    return;
                }

                // 벽·바닥 같은 막힌 곳에 닿으면 그 뒤는 겨눈 게 아니다
                if (!hitCollider.isTrigger)
                {
                    return;
                }
            }
        }

        private void SetTarget(GameObject targetObject, IDebugOverlayTarget provider, AbilitySystemComponent asc)
        {
            if (targetObject == _targetObject)
            {
                return;
            }

            _targetObject = targetObject;
            _targetProvider = provider;
            _targetAsc = asc;
            if (asc != null)
            {
                GetLog(asc);
            }

            _refreshTimer = 0f;
        }

        private void ClearTarget()
        {
            _targetObject = null;
            _targetProvider = null;
            _targetAsc = null;
        }

        private AbilitySystemDebugLog GetLog(AbilitySystemComponent asc)
        {
            // 처음 볼 때부터 기록한다 — 대상을 바꿔도 각자의 로그는 계속 쌓인다
            if (!_logs.TryGetValue(asc, out AbilitySystemDebugLog log))
            {
                log = new AbilitySystemDebugLog(asc);
                _logs[asc] = log;
            }

            return log;
        }

        // ── 내용 ────────────────────────────────────────────────────────────────

        private void RefreshPlayerPanel()
        {
            _lines.Clear();
            AppendAbilitySystemLines(_playerAsc, _lines);
            _playerPanel.Set($"<b>PLAYER</b>  <color=#9AA4B0>{_playerAsc.name}</color>", _lines);
        }

        private void RefreshTargetPanel()
        {
            _lines.Clear();
            if (_targetObject == null)
            {
                _targetPanel.Set("<b>TARGET</b>  <color=#9AA4B0>허수아비·아이템·구간을 겨누면 표시</color>", _lines);
                return;
            }

            string targetName = _targetProvider != null ? _targetProvider.DebugName : _targetObject.name;
            if (_mode == Mode.Full && _targetProvider != null)
            {
                int start = _lines.Count;
                _targetProvider.AppendDebugLines(_lines);
                if (_lines.Count > start)
                {
                    _lines.Insert(start, HeaderPrefix + "Info");
                }
            }

            if (_targetAsc != null)
            {
                AppendAbilitySystemLines(_targetAsc, _lines);
            }

            _targetPanel.Set($"<b>TARGET</b>  <color=#9AA4B0>{targetName}</color>", _lines, _targetProvider?.DebugThumbnail);
        }

        private void AppendAbilitySystemLines(AbilitySystemComponent asc, List<string> lines)
        {
            AppendAttributes(asc, lines);
            AppendActiveEffects(asc, lines);
            if (_mode == Mode.Full)
            {
                AppendAbilities(asc, lines);
            }

            AppendOwnedTags(asc, lines);
            if (_mode == Mode.Full)
            {
                AppendEvents(asc, lines);
            }
        }

        private void AppendAttributes(AbilitySystemComponent asc, List<string> lines)
        {
            lines.Add(HeaderPrefix + "Attributes");
            foreach (AttributeSet set in asc.GetSpawnedAttributes())
            {
                _attributeBuffer.Clear();
                AttributeSet.GetAttributesFromSetClass(set.GetType(), _attributeBuffer);
                foreach (GameplayAttributeHandle attribute in _attributeBuffer)
                {
                    float current = asc.GetAttributeCurrentValue(attribute);
                    float baseValue = asc.GetAttributeBaseValue(attribute);
                    string value = FormatNumber(current);
                    // Base와 다르면 Duration·Infinite GE가 Current를 바꾸고 있다는 뜻 — 버프가 걸린 걸 바로 보이게
                    if (!Mathf.Approximately(current, baseValue))
                    {
                        value += $"  <color=#8A94A6>base {FormatNumber(baseValue)}</color>";
                    }

                    if (IsRecentlyChanged(asc, attribute.Name, current))
                    {
                        value = $"<color=#{ColorUtility.ToHtmlStringRGB(changedColor)}>{value}</color>";
                    }

                    lines.Add(attribute.Name + ColumnSeparator + value);
                }
            }
        }

        private void AppendActiveEffects(AbilitySystemComponent asc, List<string> lines)
        {
            var effects = new List<ActiveGameplayEffect>(asc.GetActiveGameplayEffects());
            lines.Add($"{HeaderPrefix}Active Effects ({effects.Count})");
            foreach (ActiveGameplayEffect effect in effects)
            {
                float duration = effect.GetDuration();
                string time = duration < 0f ? "inf" : $"{effect.GetTimeRemaining():0.0} / {duration:0.0}s";
                string name = AbilitySystemDebugLog.EffectName(effect.Spec);
                if (effect.IsInhibited)
                {
                    lines.Add($"<color=#6B7280>{name} (inhibited)</color>{ColumnSeparator}<color=#6B7280>{time}</color>");
                    continue;
                }

                lines.Add(name + ColumnSeparator + time);
            }
        }

        private void AppendAbilities(AbilitySystemComponent asc, List<string> lines)
        {
            IReadOnlyList<GameplayAbilitySpec> specs = asc.GetActivatableAbilities();
            lines.Add($"{HeaderPrefix}Abilities ({specs.Count})");
            foreach (GameplayAbilitySpec spec in specs)
            {
                string name = spec.Ability != null ? spec.Ability.name : "(null)";
                string status;
                GameplayAbility instance = spec.GetPrimaryInstance();
                float cooldown = instance != null ? instance.GetCooldownTimeRemaining(asc.AbilityGameObjectInfo) : 0f;
                if (spec.IsActive)
                {
                    status = "<color=#7CFC9A>Active</color>";
                }
                else if (cooldown > 0f)
                {
                    status = $"<color=#FFB86B>CD {cooldown:0.0}s</color>";
                }
                else
                {
                    status = "<color=#6B7280>ready</color>";
                }

                if (spec.InputPressed)
                {
                    status = "<color=#7CC8FF>input</color>  " + status;
                }

                lines.Add(name + ColumnSeparator + status);
            }
        }

        private void AppendOwnedTags(AbilitySystemComponent asc, List<string> lines)
        {
            IReadOnlyList<GameplayTag> tags = asc.GetOwnedGameplayTags().GetGameplayTagArray();
            lines.Add($"{HeaderPrefix}Owned Tags ({tags.Count})");
            foreach (GameplayTag tag in tags)
            {
                int count = asc.GetTagCount(tag);
                lines.Add(tag + (count > 1 ? $"{ColumnSeparator}x{count}" : string.Empty));
            }
        }

        private void AppendEvents(AbilitySystemComponent asc, List<string> lines)
        {
            lines.Add(HeaderPrefix + "Events");
            float now = Time.time;
            foreach (AbilitySystemDebugLog.Entry entry in GetLog(asc).Entries)
            {
                string count = entry.Count > 1 ? $" <color=#FFD36B>x{entry.Count}</color>" : string.Empty;
                lines.Add(entry.Text + count + ColumnSeparator + $"<color=#6B7280>{now - entry.Time:0.0}s</color>");
            }
        }

        private bool IsRecentlyChanged(AbilitySystemComponent asc, string attributeName, float value)
        {
            (AbilitySystemComponent, string) key = (asc, attributeName);
            if (!_valueHistory.TryGetValue(key, out ValueHistory history))
            {
                _valueHistory[key] = new ValueHistory { Value = value, ChangedUntil = 0f };
                return false;
            }

            if (!Mathf.Approximately(history.Value, value))
            {
                history.Value = value;
                history.ChangedUntil = Time.time + changedHighlightDuration;
            }

            _valueHistory[key] = history;
            return Time.time < history.ChangedUntil;
        }

        private static string FormatNumber(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
        }

        // ── 화면 ────────────────────────────────────────────────────────────────

        private void BuildLayout(VisualElement root)
        {
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1;
            _root = root;

            _modeLabel = new Label { pickingMode = PickingMode.Ignore };
            _modeLabel.style.position = Position.Absolute;
            _modeLabel.style.left = 0f;
            _modeLabel.style.right = 0f;
            _modeLabel.style.top = 6f;
            _modeLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            _modeLabel.style.fontSize = fontSize;
            _modeLabel.style.color = headerColor;
            _modeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _modeLabel.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = Color.black };
            root.Add(_modeLabel);

            _playerPanel = new Panel(this, root, isLeft: true);
            _targetPanel = new Panel(this, root, isLeft: false);
        }

        private void ApplyMode()
        {
            bool isVisible = _mode != Mode.Off && !_isSettingsOpen;
            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _modeLabel.text = $"GAS DEBUG · {_mode}  <color=#9AA4B0>[{toggleKey}]</color>";
        }

        private void OnSettingsOpenChanged(bool isOpen)
        {
            _isSettingsOpen = isOpen;
            ApplyMode();
        }

        private struct ValueHistory
        {
            public float Value;
            public float ChangedUntil;
        }

        // 한 패널. 줄 요소는 재사용하고, 줄 문자열의 탭 앞은 왼쪽, 뒤는 오른쪽 정렬로 그린다. '#'로 시작하면 소제목.
        private sealed class Panel
        {
            private readonly DebugOverlay _owner;
            private readonly Label _title;
            private readonly VisualElement _thumbnail;
            private readonly VisualElement _body;
            private readonly List<(VisualElement Row, Label Left, Label Right)> _rows = new();

            public Panel(DebugOverlay owner, VisualElement root, bool isLeft)
            {
                _owner = owner;
                var panel = new VisualElement { pickingMode = PickingMode.Ignore };
                panel.style.position = Position.Absolute;
                panel.style.top = owner.screenMargin + owner.fontSize * 2f;
                if (isLeft)
                {
                    panel.style.left = owner.screenMargin;
                }
                else
                {
                    panel.style.right = owner.screenMargin;
                }

                panel.style.width = owner.panelWidth;
                panel.style.maxHeight = owner.maxPanelHeight;
                panel.style.overflow = Overflow.Hidden;
                panel.style.backgroundColor = owner.panelColor;
                panel.style.paddingLeft = 12f;
                panel.style.paddingRight = 12f;
                panel.style.paddingTop = 8f;
                panel.style.paddingBottom = 10f;
                float radius = 8f;
                panel.style.borderTopLeftRadius = radius;
                panel.style.borderTopRightRadius = radius;
                panel.style.borderBottomLeftRadius = radius;
                panel.style.borderBottomRightRadius = radius;
                root.Add(panel);

                // 제목 줄: 썸네일(있을 때만) + 제목
                var header = new VisualElement { pickingMode = PickingMode.Ignore };
                header.style.flexDirection = FlexDirection.Row;
                header.style.alignItems = Align.Center;
                header.style.marginBottom = 6f;
                panel.Add(header);

                _thumbnail = new VisualElement { pickingMode = PickingMode.Ignore };
                _thumbnail.style.width = owner.thumbnailSize;
                _thumbnail.style.height = owner.thumbnailSize;
                _thumbnail.style.marginRight = 10f;
                _thumbnail.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f);
                _thumbnail.style.borderTopLeftRadius = 6f;
                _thumbnail.style.borderTopRightRadius = 6f;
                _thumbnail.style.borderBottomLeftRadius = 6f;
                _thumbnail.style.borderBottomRightRadius = 6f;
                _thumbnail.style.display = DisplayStyle.None;
                header.Add(_thumbnail);

                _title = MakeLabel(owner.fontSize * 1.1f, owner.textColor);
                _title.style.flexShrink = 1;
                header.Add(_title);

                _body = new VisualElement { pickingMode = PickingMode.Ignore };
                panel.Add(_body);
            }

            public void Set(string title, List<string> lines, Texture2D thumbnail = null)
            {
                _title.text = title;
                _thumbnail.style.display = thumbnail != null ? DisplayStyle.Flex : DisplayStyle.None;
                _thumbnail.style.backgroundImage = thumbnail;
                while (_rows.Count < lines.Count)
                {
                    _rows.Add(MakeRow());
                }

                for (int i = 0; i < _rows.Count; i++)
                {
                    (VisualElement row, Label left, Label right) = _rows[i];
                    bool isUsed = i < lines.Count;
                    row.style.display = isUsed ? DisplayStyle.Flex : DisplayStyle.None;
                    if (!isUsed)
                    {
                        continue;
                    }

                    string line = lines[i];
                    bool isHeader = line.Length > 0 && line[0] == HeaderPrefix;
                    if (isHeader)
                    {
                        line = line.Substring(1);
                    }

                    int separator = line.IndexOf(ColumnSeparator);
                    left.text = separator >= 0 ? line.Substring(0, separator) : line;
                    right.text = separator >= 0 ? line.Substring(separator + 1) : string.Empty;
                    left.style.color = isHeader ? _owner.headerColor : _owner.textColor;
                    left.style.unityFontStyleAndWeight = isHeader ? FontStyle.Bold : FontStyle.Normal;
                    left.style.paddingLeft = isHeader ? 0f : 10f;
                    row.style.marginTop = isHeader && i > 0 ? 6f : 0f;
                }
            }

            private (VisualElement, Label, Label) MakeRow()
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                Label left = MakeLabel(_owner.fontSize, _owner.textColor);
                // 왼쪽이 길면 말줄임으로 자르고, 값(오른쪽)은 줄어들지 않게 한다
                left.style.flexShrink = 1;
                left.style.overflow = Overflow.Hidden;
                left.style.textOverflow = TextOverflow.Ellipsis;
                Label right = MakeLabel(_owner.fontSize, _owner.textColor);
                right.style.flexShrink = 0;
                right.style.unityTextAlign = TextAnchor.UpperRight;
                right.style.marginLeft = 12f;
                row.Add(left);
                row.Add(right);
                _body.Add(row);
                return (row, left, right);
            }

            private static Label MakeLabel(float size, Color color)
            {
                var label = new Label { pickingMode = PickingMode.Ignore, enableRichText = true };
                label.style.fontSize = size;
                label.style.color = color;
                label.style.paddingLeft = 0f;
                label.style.paddingRight = 0f;
                label.style.marginLeft = 0f;
                label.style.marginRight = 0f;
                // Label 기본 위아래 여백을 없애 줄을 촘촘히 — 패널이 화면 아래 HUD까지 내려오지 않게
                label.style.paddingTop = 0f;
                label.style.paddingBottom = 0f;
                label.style.marginTop = 0f;
                label.style.marginBottom = 0f;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                return label;
            }
        }
    }
}
