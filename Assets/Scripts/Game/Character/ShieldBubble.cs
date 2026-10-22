using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 캐릭터를 감싸는 실드 막. 실드 태그가 붙어 있는 동안 보이고, 진하기가 남은 실드를 따라간다.
    /// 처음 차오르는 동안은 0부터 찬 만큼, 다 찬 뒤 깎일 땐 하한까지만 옅어진다. 태그가 빠지면 끝나는 순간의 진하기에서 완전히 사라진다 — 깨졌으면(실드 0) 빠르게, 시간이 다 됐으면 천천히.
    /// </summary>
    public class ShieldBubble : MonoBehaviour
    {
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [Tooltip("실드가 켜져 있는 동안 붙는 태그. 실드 어빌리티의 Activation Owned Tags와 같아야 한다.")]
        [SerializeField] private GameplayTag shieldTag;
        [Tooltip("막 렌더러. 재질에 투명도 속성(_Alpha)이 있어야 한다.")]
        [SerializeField] private Renderer bubbleRenderer;

        [Tooltip("이 실드량일 때 가장 진하다. 0이면 실드량과 상관없이 같은 진하기.")]
        [Min(0f)] [SerializeField] private float fullShieldAmount;
        [Tooltip("다 찬 뒤 깎일 때 이 비율 아래로는 옅어지지 않는다 — 깨지는 순간이 보이게.")]
        [Range(0f, 1f)] [SerializeField] private float minDamagedAlpha = 0.35f;

        [SerializeField] private float fadeInDuration = 0.15f;
        [Tooltip("시간이 다 돼 사라질 때 걸리는 시간(초).")]
        [SerializeField] private float fadeOutDuration = 0.8f;
        [Tooltip("깨질 때 사라지는 데 걸리는 시간(초).")]
        [SerializeField] private float breakDuration = 0.25f;

        private Material _material;
        private bool _isShielded;
        private bool _hasFilled;
        private float _weight;
        private float _fadeSpeed;
        private float _endAlpha;

        private void Awake()
        {
            _material = bubbleRenderer.material;
            bubbleRenderer.enabled = false;
        }

        private void Start()
        {
            _isShielded = abilitySystemComponent.HasMatchingGameplayTag(shieldTag);
            abilitySystemComponent.RegisterGameplayTagEvent(shieldTag, OnTagCountChanged);
        }

        private void Update()
        {
            _weight = Mathf.MoveTowards(_weight, _isShielded ? 1f : 0f, _fadeSpeed * Time.deltaTime);
            float alpha = _isShielded ? GetShieldAlpha() : _endAlpha;
            bubbleRenderer.enabled = _weight > 0f;
            _material.SetFloat(AlphaId, alpha * _weight);
        }

        private void OnDestroy()
        {
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, shieldTag);
            }

            Destroy(_material);
        }

        private float GetShieldAlpha()
        {
            if (fullShieldAmount <= 0f)
            {
                return 1f;
            }

            float ratio = Mathf.Clamp01(abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.Shield) / fullShieldAmount);
            if (ratio >= 1f)
            {
                _hasFilled = true;
            }

            return _hasFilled ? Mathf.Lerp(minDamagedAlpha, 1f, ratio) : ratio;
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            bool isShielded = newCount > 0;
            if (isShielded == _isShielded)
            {
                return;
            }

            if (isShielded)
            {
                _hasFilled = false;
                _fadeSpeed = 1f / Mathf.Max(fadeInDuration, 0.01f);
                _isShielded = true;
                return;
            }

            // 실드를 비우기 전(시간 만료)이나 0이 된 직후(깨짐)의 진하기에서 사라지기 시작한다
            _endAlpha = GetShieldAlpha();
            bool isBroken = abilitySystemComponent.GetAttributeCurrentValue(CharacterAttributeSet.Shield) <= 0f;
            _fadeSpeed = 1f / Mathf.Max(isBroken ? breakDuration : fadeOutDuration, 0.01f);
            _isShielded = false;
        }
    }
}
