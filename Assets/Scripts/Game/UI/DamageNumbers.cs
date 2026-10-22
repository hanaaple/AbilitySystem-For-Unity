using System.Collections.Generic;
using Core.AbilitySystem.Effect;
using Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>허수아비가 맞을 때마다 맞은 자리에 피해량 숫자를 띄운다. 숫자는 떠오르며 사라지고, 헤드샷은 더 크고 다른 색으로 튀어나온다. 한 타격이 실드와 체력을 함께 깎으면 숫자 하나로 합친다.</summary>
    public class DamageNumbers : MonoBehaviour
    {
        [SerializeField] private TrainingDummy dummy;
        [Tooltip("피격 정보가 없을 때(주기 피해 등) 숫자가 뜨는 높이.")]
        [SerializeField] private float fallbackHeight = 1.6f;

        [Header("Motion")]
        [SerializeField] private float lifeTime = 0.8f;
        [SerializeField] private float riseDistance = 0.8f;
        [Tooltip("같은 자리 연타가 겹치지 않게 옆으로 흩는 폭(m).")]
        [SerializeField] private float horizontalJitter = 0.25f;

        [Header("Look")]
        [SerializeField] private Font font;
        [Tooltip("월드 1m당 글자 크기 — 캔버스 배율.")]
        [SerializeField] private float worldScale = 0.01f;
        [SerializeField] private int fontSize = 36;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private int headFontSize = 56;
        [SerializeField] private Color headColor = new(1f, 0.85f, 0.1f, 1f);
        [Tooltip("헤드샷이 처음에 이 배율로 커졌다가 원래 크기로 줄어든다.")]
        [SerializeField] private float headPopScale = 1.6f;

        private readonly List<Number> _numbers = new();
        private Transform _cameraTransform;

        private void Start()
        {
            Camera mainCamera = Camera.main;
            _cameraTransform = mainCamera != null ? mainCamera.transform : null;
            dummy.OnDamaged += OnDamaged;
        }

        private void LateUpdate()
        {
            for (int i = _numbers.Count - 1; i >= 0; i--)
            {
                Number number = _numbers[i];
                number.Age += Time.deltaTime;
                float progress = number.Age / lifeTime;
                if (progress >= 1f)
                {
                    Destroy(number.Root);
                    _numbers.RemoveAt(i);
                    continue;
                }

                Transform root = number.Root.transform;
                root.position = number.Origin + Vector3.up * (riseDistance * Mathf.Sin(progress * Mathf.PI * 0.5f));
                if (_cameraTransform != null)
                {
                    root.rotation = Quaternion.LookRotation(root.position - _cameraTransform.position);
                }

                // 헤드샷은 처음 20% 동안 커졌다가 원래 크기로 돌아온다
                float pop = number.IsHead ? Mathf.Lerp(headPopScale, 1f, Mathf.Clamp01(progress / 0.2f)) : 1f;
                root.localScale = Vector3.one * (worldScale * pop);

                Color color = number.Text.color;
                color.a = 1f - Mathf.Clamp01((progress - 0.5f) / 0.5f);
                number.Text.color = color;
            }
        }

        private void OnDestroy()
        {
            if (dummy != null)
            {
                dummy.OnDamaged -= OnDamaged;
            }

            foreach (Number number in _numbers)
            {
                Destroy(number.Root);
            }
        }

        private void OnDamaged(float damage, GameplayEffectSpec source)
        {
            // 같은 GE의 같은 프레임 피해(실드 + 체력)는 마지막 숫자에 더한다
            if (_numbers.Count > 0)
            {
                Number last = _numbers[^1];
                if (last.Source == source && last.Frame == Time.frameCount)
                {
                    last.Damage += damage;
                    last.Text.text = FormatDamage(last.Damage, last.IsHead);
                    return;
                }
            }

            GameplayEffectContextHandle context = source != null ? source.Context : default;
            bool isHead = source != null && DamageExecution.IsHeadHit(context);
            Vector3 origin = context.TryGetHitResult(out RaycastHit hit) && hit.collider != null
                ? hit.point
                : transform.position + Vector3.up * fallbackHeight;
            Vector2 jitter = Random.insideUnitCircle * horizontalJitter;
            origin += new Vector3(jitter.x, 0f, jitter.y);

            _numbers.Add(CreateNumber(damage, isHead, origin, source));
        }

        private Number CreateNumber(float damage, bool isHead, Vector3 origin, GameplayEffectSpec source)
        {
            var root = new GameObject("DamageNumber", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.transform.position = origin;
            root.transform.localScale = Vector3.one * worldScale;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(root.transform, false);
            var rect = (RectTransform)textObject.transform;
            rect.sizeDelta = new Vector2(400f, 120f);

            var text = textObject.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = isHead ? headFontSize : fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = isHead ? headColor : normalColor;
            text.text = FormatDamage(damage, isHead);

            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = isHead ? new Vector2(3f, -3f) : new Vector2(2f, -2f);

            return new Number { Root = root, Text = text, Origin = origin, Damage = damage, IsHead = isHead, Source = source, Frame = Time.frameCount };
        }

        private static string FormatDamage(float damage, bool isHead)
        {
            string value = Mathf.RoundToInt(damage).ToString();
            return isHead ? value + "!" : value;
        }

        private class Number
        {
            public GameObject Root;
            public Text Text;
            public Vector3 Origin;
            public float Damage;
            public bool IsHead;
            public GameplayEffectSpec Source;
            public int Frame;
            public float Age;
        }
    }
}
