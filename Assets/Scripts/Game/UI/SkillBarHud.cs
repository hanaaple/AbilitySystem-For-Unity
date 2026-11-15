using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Game.Abilities;
using Game.Settings;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// 화면 아래 가운데의 스킬 칸들. 기본 동작(공격·조준·달리기·점프)은 왼쪽에 작게, 쿨다운·코스트가 있는 스킬은 오른쪽에 크게 두어 무엇이 중요한지 갈라 보이게 한다.
    /// 칸마다 배경·아이콘·키 이름을 두고, 쿨다운 태그가 있으면 지난 만큼 덮개가 걷히며 차오르다 다 차는 순간 번쩍인다. 코스트가 있으면 오른쪽 아래에 그 양을 띄운다.
    /// 쓰는 동안 붙는 태그가 있으면 그동안 테두리를 밝힌다. 코스트(마나 등)를 못 내면 칸을 어둡게 덮는다 — 판단은 어빌리티의 CheckCost에 맡긴다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SkillBarHud : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("abilitySystemComponent")] private AbilitySystemComponent _abilitySystemComponent;
        [SerializeField] [FormerlySerializedAs("slots")] private List<SkillSlot> _slots = new();

        // 패널 기준 해상도(1920×1080)의 픽셀. 화면이 작으면 함께 줄어든다.
        [Header("Layout (px)")]
        [FormerlySerializedAs("slotSize")]
        [SerializeField] [FormerlySerializedAs("majorSlotSize")] private float _majorSlotSize = 84f;
        [SerializeField] [FormerlySerializedAs("minorSlotSize")] private float _minorSlotSize = 54f;
        [SerializeField] [FormerlySerializedAs("slotSpacing")] private float _slotSpacing = 10f;
        [Tooltip("기본 동작 묶음과 스킬 묶음 사이 간격.")]
        [SerializeField] [FormerlySerializedAs("groupSpacing")] private float _groupSpacing = 36f;
        [SerializeField] [FormerlySerializedAs("bottomMargin")] private float _bottomMargin = 32f;

        [Header("Look")]
        [SerializeField] [FormerlySerializedAs("backgroundColor")] private Color _backgroundColor = new(0.08f, 0.09f, 0.12f, 0.85f);
        [SerializeField] [FormerlySerializedAs("borderColor")] private Color _borderColor = new(1f, 1f, 1f, 0.35f);
        [Tooltip("기본 동작 칸의 테두리. 스킬 칸보다 흐리게 해 뒤로 물린다.")]
        [SerializeField] [FormerlySerializedAs("minorBorderColor")] private Color _minorBorderColor = new(1f, 1f, 1f, 0.15f);
        [Tooltip("코스트 배지 배경. 마나 바 색과 맞춘다.")]
        [SerializeField] [FormerlySerializedAs("costBadgeColor")] private Color _costBadgeColor = new(0.4f, 0.3f, 0.85f, 0.95f);
        [SerializeField] [FormerlySerializedAs("activeBorderColor")] private Color _activeBorderColor = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField] [FormerlySerializedAs("cooldownColor")] private Color _cooldownColor = new(0f, 0f, 0f, 0.65f);
        [Tooltip("코스트를 못 낼 때 칸 전체를 덮는 색. 마나 바 색과 맞춘다.")]
        [SerializeField] [FormerlySerializedAs("unaffordableColor")] private Color _unaffordableColor = new(0.2f, 0.12f, 0.45f, 0.7f);
        [SerializeField] [FormerlySerializedAs("nameColor")] private Color _nameColor = new(1f, 0.97f, 0.9f, 1f);
        [SerializeField] [FormerlySerializedAs("keyColor")] private Color _keyColor = new(0.85f, 0.9f, 1f, 1f);
        [SerializeField] [FormerlySerializedAs("keyBadgeColor")] private Color _keyBadgeColor = new(0f, 0f, 0f, 0.55f);
        [Tooltip("쿨다운이 끝나는 순간 덮는 흰빛이 사라지는 시간(초).")]
        [SerializeField] [FormerlySerializedAs("flashDuration")] private float _flashDuration = 0.25f;

        private readonly List<SlotView> _views = new();
        private VisualElement _bar;
        private VisualElement _minorGroup;
        private VisualElement _majorGroup;

        private void OnEnable()
        {
            BuildLayout(GetComponent<UIDocument>().rootVisualElement);
            SettingsMenu.OnOpenChanged += SetHidden;
        }

        private void Update()
        {
            foreach (SlotView view in _views)
            {
                UpdateCooldown(view);
                UpdateActive(view);
                UpdateCost(view);
            }
        }

        private void OnDisable()
        {
            SettingsMenu.OnOpenChanged -= SetHidden;
        }

        private void BuildLayout(VisualElement root)
        {
            root.Clear();
            _views.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1;

            _bar = new VisualElement { pickingMode = PickingMode.Ignore };
            _bar.style.position = Position.Absolute;
            _bar.style.left = 0f;
            _bar.style.right = 0f;
            _bar.style.bottom = _bottomMargin;
            _bar.style.flexDirection = FlexDirection.Row;
            _bar.style.justifyContent = Justify.Center;
            root.Add(_bar);

            _minorGroup = MakeGroup();
            _majorGroup = MakeGroup();
            _majorGroup.style.marginLeft = _groupSpacing;
            foreach (SkillSlot slot in _slots)
            {
                _views.Add(slot.IsMajor ? BuildSlot(slot, _majorSlotSize, _majorGroup) : BuildSlot(slot, _minorSlotSize, _minorGroup));
            }
        }

        // 칸들을 아래쪽에 맞춰 나란히 — 크기가 다른 두 묶음의 밑선이 같게
        private VisualElement MakeGroup()
        {
            var group = new VisualElement { pickingMode = PickingMode.Ignore };
            group.style.flexDirection = FlexDirection.Row;
            group.style.alignItems = Align.FlexEnd;
            _bar.Add(group);
            return group;
        }

        private SlotView BuildSlot(SkillSlot slot, float size, VisualElement group)
        {
            // 칸 + (작은 칸이면) 아래 이름 글자를 세로로 묶는다
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.style.alignItems = Align.Center;
            cell.style.marginLeft = _slotSpacing * 0.5f;
            cell.style.marginRight = _slotSpacing * 0.5f;
            group.Add(cell);

            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.style.width = size;
            frame.style.height = size;
            frame.style.backgroundColor = _backgroundColor;
            frame.style.backgroundImage = slot.Background;
            frame.style.overflow = Overflow.Hidden;
            SetRadius(frame, size * 0.14f);
            SetBorder(frame, 2f, slot.IsMajor ? _borderColor : _minorBorderColor);
            cell.Add(frame);

            var icon = MakeFullLayer(frame);
            icon.style.backgroundImage = slot.Icon;
            icon.style.marginLeft = 6f;
            icon.style.marginRight = 6f;
            icon.style.marginTop = 6f;
            icon.style.marginBottom = 6f;

            var view = new SlotView(slot, frame);
            if (slot.CooldownTag.IsValid())
            {
                var tags = new GameplayTagContainer();
                tags.AddTag(slot.CooldownTag);
                view.CooldownQuery = GameplayEffectQuery.MakeQueryMatchAnyOwningTags(tags);
            }

            // 지난 쿨다운이 아니라 남은 쿨다운만큼 덮는다 — 덮개가 걷히며 아이콘이 차오른다
            var cooldown = MakeFullLayer(frame);
            cooldown.generateVisualContent += context => RadialFill.Draw(context, view.RemainingRatio, _cooldownColor);
            view.Cooldown = cooldown;

            view.Unaffordable = MakeFullLayer(frame);
            view.Unaffordable.style.backgroundColor = _unaffordableColor;
            view.Unaffordable.style.display = DisplayStyle.None;

            view.Flash = MakeFullLayer(frame);
            view.Flash.style.backgroundColor = Color.white;
            view.Flash.style.opacity = 0f;

            // 아이콘 그림이 없으면 글자가 그 자리를 대신한다. 큰 칸(스킬)은 스킬 이름을(키 배지와 겹치지 않게 조금 아래),
            // 작은 칸(기본 동작)은 무엇을 하는지 뻔하므로 누를 키를 크게 두고 이름은 칸 아래 작은 글자로 단다.
            string centerText = slot.IsMajor ? slot.DisplayName : slot.KeyLabel;
            if (slot.Icon == null && !string.IsNullOrEmpty(centerText))
            {
                var nameLabel = new Label(centerText) { pickingMode = PickingMode.Ignore };
                nameLabel.style.position = Position.Absolute;
                nameLabel.style.left = 0f;
                nameLabel.style.right = 0f;
                nameLabel.style.top = slot.IsMajor ? size * 0.18f : 0f;
                nameLabel.style.bottom = 0f;
                nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                nameLabel.style.fontSize = size * (slot.IsMajor ? 0.23f : 0.26f);
                nameLabel.style.letterSpacing = size * 0.01f;
                nameLabel.style.color = _nameColor;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLabel.style.textShadow = new TextShadow { offset = new Vector2(0f, 2f), blurRadius = 4f, color = new Color(0f, 0f, 0f, 0.9f) };
                frame.Insert(frame.IndexOf(view.Cooldown), nameLabel);
            }

            if (!slot.IsMajor && !string.IsNullOrEmpty(slot.DisplayName))
            {
                var caption = new Label(slot.DisplayName) { pickingMode = PickingMode.Ignore };
                caption.style.marginTop = 3f;
                caption.style.paddingLeft = 0f;
                caption.style.paddingRight = 0f;
                caption.style.fontSize = size * 0.24f;
                caption.style.color = new Color(_nameColor.r, _nameColor.g, _nameColor.b, 0.75f);
                caption.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = new Color(0f, 0f, 0f, 0.9f) };
                cell.Add(caption);
            }

            // 큰 칸의 키는 왼쪽 위 배지 — 스킬 이름보다 작고 차분하게
            var keyBadge = new Label(slot.KeyLabel) { pickingMode = PickingMode.Ignore };
            keyBadge.style.display = slot.IsMajor ? DisplayStyle.Flex : DisplayStyle.None;
            keyBadge.style.position = Position.Absolute;
            keyBadge.style.left = 5f;
            keyBadge.style.top = 5f;
            keyBadge.style.paddingLeft = 5f;
            keyBadge.style.paddingRight = 5f;
            keyBadge.style.paddingTop = 1f;
            keyBadge.style.paddingBottom = 1f;
            keyBadge.style.fontSize = size * 0.17f;
            keyBadge.style.color = _keyColor;
            keyBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyBadge.style.backgroundColor = _keyBadgeColor;
            SetRadius(keyBadge, size * 0.06f);
            SetBorder(keyBadge, 1f, new Color(1f, 1f, 1f, 0.25f));
            frame.Add(keyBadge);

            view.TimeLabel = new Label { pickingMode = PickingMode.Ignore };
            view.TimeLabel.style.position = Position.Absolute;
            view.TimeLabel.style.left = 0f;
            view.TimeLabel.style.right = 0f;
            view.TimeLabel.style.top = 0f;
            view.TimeLabel.style.bottom = 0f;
            view.TimeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            view.TimeLabel.style.fontSize = size * 0.3f;
            view.TimeLabel.style.color = Color.white;
            view.TimeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            view.TimeLabel.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 3f, color = Color.black };
            frame.Add(view.TimeLabel);

            // 코스트 양은 어빌리티가 부여된 뒤에 알 수 있어 처음엔 숨겨 둔다(UpdateCost가 채운다)
            view.CostLabel = new Label { pickingMode = PickingMode.Ignore };
            view.CostLabel.style.position = Position.Absolute;
            view.CostLabel.style.right = 4f;
            view.CostLabel.style.bottom = 4f;
            view.CostLabel.style.paddingLeft = 4f;
            view.CostLabel.style.paddingRight = 4f;
            view.CostLabel.style.paddingTop = 0f;
            view.CostLabel.style.paddingBottom = 0f;
            view.CostLabel.style.fontSize = size * 0.16f;
            view.CostLabel.style.color = Color.white;
            view.CostLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            view.CostLabel.style.backgroundColor = _costBadgeColor;
            view.CostLabel.style.display = DisplayStyle.None;
            SetRadius(view.CostLabel, size * 0.06f);
            frame.Add(view.CostLabel);
            return view;
        }

        private void UpdateCooldown(SlotView view)
        {
            if (view.CooldownQuery == null)
            {
                return;
            }

            float remaining = 0f;
            float duration = 0f;
            foreach ((float timeRemaining, float total) in _abilitySystemComponent.GetActiveEffectsTimeRemainingAndDuration(view.CooldownQuery))
            {
                if (timeRemaining > remaining)
                {
                    remaining = timeRemaining;
                    duration = total;
                }
            }

            bool isCoolingDown = remaining > 0f && duration > 0f;
            float ratio = isCoolingDown ? remaining / duration : 0f;
            if (!Mathf.Approximately(ratio, view.RemainingRatio))
            {
                view.RemainingRatio = ratio;
                view.Cooldown.MarkDirtyRepaint();
            }

            view.TimeLabel.text = isCoolingDown && remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;

            if (view.WasCoolingDown && !isCoolingDown)
            {
                view.FlashTime = _flashDuration;
            }

            view.WasCoolingDown = isCoolingDown;
            view.FlashTime = Mathf.Max(0f, view.FlashTime - Time.deltaTime);
            view.Flash.style.opacity = _flashDuration > 0f ? view.FlashTime / _flashDuration * 0.8f : 0f;
        }

        private void UpdateActive(SlotView view)
        {
            SkillSlot slot = view.Slot;
            if (!slot.ActiveTag.IsValid() && !slot.ShowInputHeld)
            {
                return;
            }

            bool isActive = (slot.ActiveTag.IsValid() && _abilitySystemComponent.HasMatchingGameplayTag(slot.ActiveTag)) || (slot.ShowInputHeld && IsInputHeld(slot.InputID));
            SetBorder(view.Frame, isActive ? 3f : 2f, isActive ? _activeBorderColor : _borderColor);
        }

        private void UpdateCost(SlotView view)
        {
            GameplayAbilitySpec spec = FindSpec(view.Slot.InputID);
            GameplayAbility ability = spec != null ? spec.GetPrimaryInstance() ?? spec.Ability.AbilityTemplate : null;
            bool canAfford = ability == null || ability.CheckCost(spec.Handle, _abilitySystemComponent.AbilityGameObjectInfo);
            view.Unaffordable.style.display = canAfford ? DisplayStyle.None : DisplayStyle.Flex;

            if (ability != null && !view.HasCostLabel)
            {
                view.HasCostLabel = true;
                float cost = GetCostAmount(ability.GetCostGameplayEffect());
                view.CostLabel.text = Mathf.CeilToInt(cost).ToString();
                view.CostLabel.style.display = cost > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // 코스트 GE가 깎는 양(음수 AddBase modifier의 합). 레벨 커브가 없어 Spec 하나로 계산해 둔다.
        private float GetCostAmount(GameplayEffectAsset costEffect)
        {
            if (costEffect == null)
            {
                return 0f;
            }

            GameplayEffectSpec spec = _abilitySystemComponent.MakeOutgoingSpec(costEffect);
            float total = 0f;
            foreach (GameplayModifier modifier in costEffect.Modifiers)
            {
                if (modifier.Operation == GameplayModifierOperation.AddBase && modifier.ModifierMagnitude.AttemptCalculateMagnitude(spec, out float magnitude) && magnitude < 0f)
                {
                    total -= magnitude;
                }
            }

            return total;
        }

        private GameplayAbilitySpec FindSpec(AbilityInputID inputID)
        {
            foreach (GameplayAbilitySpec spec in _abilitySystemComponent.GetActivatableAbilities())
            {
                if (spec.InputID == (int)inputID)
                {
                    return spec;
                }
            }

            return null;
        }

        private bool IsInputHeld(AbilityInputID inputID)
        {
            GameplayAbilitySpec spec = FindSpec(inputID);
            return spec != null && spec.InputPressed;
        }

        private void SetHidden(bool isHidden)
        {
            _bar.style.display = isHidden ? DisplayStyle.None : DisplayStyle.Flex;
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

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
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

        [System.Serializable]
        private class SkillSlot
        {
            [Tooltip("칸에 표시할 키 이름(예: LMB, F).")]
            [FormerlySerializedAs("keyLabel")] public string KeyLabel;
            [Tooltip("아이콘 그림이 없을 때 칸 가운데에 대신 보이는 스킬 이름.")]
            [FormerlySerializedAs("displayName")] public string DisplayName;
            [FormerlySerializedAs("icon")] public Texture2D Icon;
            [Tooltip("비우면 기본 배경색만 쓴다.")]
            [FormerlySerializedAs("background")] public Texture2D Background;
            [Tooltip("이 태그를 주는 GE가 걸려 있는 동안 쿨다운으로 덮는다. 비우면 쿨다운을 표시하지 않는다.")]
            [FormerlySerializedAs("cooldownTag")] public GameplayTag CooldownTag;
            [Tooltip("이 태그가 있는 동안(쓰는 중) 테두리를 밝힌다. 비우면 표시하지 않는다.")]
            [FormerlySerializedAs("activeTag")] public GameplayTag ActiveTag;
            [Tooltip("켜면 이 입력에 묶인 어빌리티의 입력이 눌려 있는 동안에도 테두리를 밝힌다(누르고 있는 연사 등 쓰는 중 태그가 없는 것).")]
            [FormerlySerializedAs("showInputHeld")] public bool ShowInputHeld;
            [Tooltip("켜면 오른쪽 스킬 묶음에 큰 칸으로(쿨다운·코스트가 있는 스킬), 끄면 왼쪽 기본 동작 묶음에 작은 칸으로.")]
            [FormerlySerializedAs("isMajor")] public bool IsMajor;
            [FormerlySerializedAs("inputID")] public AbilityInputID InputID;
        }

        private class SlotView
        {
            public readonly SkillSlot Slot;
            public readonly VisualElement Frame;
            public GameplayEffectQuery CooldownQuery;
            public VisualElement Cooldown;
            public VisualElement Unaffordable;
            public Label CostLabel;
            public bool HasCostLabel;
            public VisualElement Flash;
            public Label TimeLabel;
            public float RemainingRatio;
            public bool WasCoolingDown;
            public float FlashTime;

            public SlotView(SkillSlot slot, VisualElement frame)
            {
                Slot = slot;
                Frame = frame;
            }
        }
    }
}
