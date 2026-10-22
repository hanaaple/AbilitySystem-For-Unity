using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>허수아비 머리 위 체력 바. 맞을 때마다 깎인 구간이 하얀 조각으로 남았다가 조각마다 잠시 뒤 줄어들고, 옆에 피해량·초당 피해량·누적 피해량을 띄운다. 실드가 있으면 체력 끝에 파란 칸으로 이어 붙인다. 항상 카메라를 본다.</summary>
    public class TrainingDummyHealthBar : MonoBehaviour
    {
        [SerializeField] private TrainingDummy dummy;

        [Header("Bar")]
        [Tooltip("체력 비율만큼 오른쪽 끝(anchorMax.x)이 움직인다.")]
        [SerializeField] private RectTransform healthFill;
        [Tooltip("체력 끝에서 실드 비율만큼 이어지는 칸(anchorMin.x·anchorMax.x가 움직인다). 비우면 실드를 그리지 않는다.")]
        [SerializeField] private RectTransform shieldFill;
        [Tooltip("healthFill 뒤에 깔리는 칸. 깎일 때마다 그 구간의 하얀 조각을 이 아래에 만든다.")]
        [SerializeField] private RectTransform damageTrailContainer;
        [SerializeField] private Color damageTrailColor = Color.white;
        [Tooltip("조각마다, 생긴 뒤 줄어들기 시작할 때까지(초).")]
        [Min(0f)] [SerializeField] private float trailHoldTime = 0.6f;
        [Tooltip("조각이 줄어드는 속도(바 전체 길이 / 초).")]
        [Min(0.01f)] [SerializeField] private float trailShrinkSpeed = 1.5f;

        [Header("Ticks")]
        [SerializeField] private RectTransform tickContainer;
        [Min(1f)] [SerializeField] private float tickInterval = 100f;
        [SerializeField] private float tickWidth = 2f;
        [SerializeField] private Color tickColor = new(0f, 0f, 0f, 0.6f);

        [Header("Text")]
        [SerializeField] private Text damageText;
        [SerializeField] private Text dpsText;
        [SerializeField] private Text totalText;

        private readonly List<RectTransform> _trailChunkRects = new();
        private HealthTrail _trail;
        private AbilitySystemComponent _asc;
        private Transform _cameraTransform;
        private float _tickedBarScale;

        // 바 전체 길이에 해당하는 값. 평소엔 최대 체력, 체력+실드가 넘치면 그 합이다.
        private float _barScale;

        private void Start()
        {
            _trail = new HealthTrail(trailHoldTime, trailShrinkSpeed);
            _asc = dummy.AbilitySystemComponent;
            _asc.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Health).OnValueChange += OnHealthChanged;
            dummy.OnDamaged += OnDummyDamaged;
            RefreshTexts();
        }

        private void LateUpdate()
        {
            float maxHealth = _asc.GetAttributeCurrentValue(CharacterAttributeSet.MaxHealth);
            if (maxHealth <= 0f)
            {
                return;
            }

            float health = _asc.GetAttributeCurrentValue(CharacterAttributeSet.Health);
            float shield = _asc.GetAttributeCurrentValue(CharacterAttributeSet.Shield);
            _barScale = Mathf.Max(maxHealth, health + shield);
            if (!Mathf.Approximately(_barScale, _tickedBarScale))
            {
                RebuildTicks(_barScale);
            }

            float healthRatio = Mathf.Clamp01(health / _barScale);
            SetRightEdge(healthFill, healthRatio);
            if (shieldFill != null)
            {
                shieldFill.gameObject.SetActive(shield > 0f);
                SetSpan(shieldFill, healthRatio, Mathf.Clamp01((health + shield) / _barScale));
            }
            _trail.Tick(Time.deltaTime);
            SyncTrailChunks();
            FaceCamera();
            // 초당 피해량은 최근 몇 초 평균이라 안 맞아도 시간이 지나며 바뀐다
            RefreshTexts();
        }

        private void OnDestroy()
        {
            if (_asc != null)
            {
                _asc.GetGameplayAttributeValueChangeDelegate(CharacterAttributeSet.Health).OnValueChange -= OnHealthChanged;
            }

            if (dummy != null)
            {
                dummy.OnDamaged -= OnDummyDamaged;
            }
        }

        // 조각 수만큼 Image를 켜고 위치를 맞춘다. 남는 Image는 꺼 두고 다음에 다시 쓴다.
        private void SyncTrailChunks()
        {
            IReadOnlyList<HealthTrail.Chunk> chunks = _trail.Chunks;
            while (_trailChunkRects.Count < chunks.Count)
            {
                _trailChunkRects.Add(CreateTrailChunkRect());
            }

            for (int i = 0; i < _trailChunkRects.Count; i++)
            {
                RectTransform rect = _trailChunkRects[i];
                bool isUsed = i < chunks.Count;
                rect.gameObject.SetActive(isUsed);
                if (!isUsed)
                {
                    continue;
                }

                HealthTrail.Chunk chunk = chunks[i];
                rect.anchorMin = new Vector2(chunk.LowRatio, 0f);
                rect.anchorMax = new Vector2(chunk.HighRatio, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private RectTransform CreateTrailChunkRect()
        {
            var chunk = new GameObject("TrailChunk", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)chunk.transform;
            rect.SetParent(damageTrailContainer, false);

            var image = chunk.GetComponent<Image>();
            image.color = damageTrailColor;
            image.raycastTarget = false;
            return rect;
        }


        private void RebuildTicks(float barScale)
        {
            _tickedBarScale = barScale;
            for (int i = tickContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(tickContainer.GetChild(i).gameObject);
            }

            // 끝과 겹치는 눈금은 테두리와 겹치므로 그리지 않는다
            for (float value = tickInterval; value < barScale; value += tickInterval)
            {
                CreateTick(value / barScale);
            }
        }

        private void CreateTick(float ratio)
        {
            var tick = new GameObject("Tick", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)tick.transform;
            rect.SetParent(tickContainer, false);
            rect.anchorMin = new Vector2(ratio, 0f);
            rect.anchorMax = new Vector2(ratio, 1f);
            rect.sizeDelta = new Vector2(tickWidth, 0f);

            var image = tick.GetComponent<Image>();
            image.color = tickColor;
            image.raycastTarget = false;
        }

        private void FaceCamera()
        {
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }

                _cameraTransform = mainCamera.transform;
            }

            transform.rotation = _cameraTransform.rotation;
        }

        private void RefreshTexts()
        {
            damageText.text = $"피해량 {dummy.LastDamage:N0}";
            dpsText.text = $"초당 피해량 {dummy.DamagePerSecond:N0}";
            totalText.text = $"누적 피해량 {dummy.TotalDamage:N0}";
        }

        private static void SetRightEdge(RectTransform rect, float ratio)
        {
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = ratio;
            rect.anchorMax = anchorMax;
        }

        private static void SetSpan(RectTransform rect, float fromRatio, float toRatio)
        {
            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            anchorMin.x = fromRatio;
            anchorMax.x = toRatio;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }

        private void OnHealthChanged(OnAttributeChangeData data)
        {
            if (_barScale <= 0f)
            {
                return;
            }

            _trail.OnHealthRatioChanged(data.OldValue / _barScale, data.NewValue / _barScale);
        }

        private void OnDummyDamaged(float damage, GameplayEffectSpec source)
        {
            RefreshTexts();
        }
    }
}
