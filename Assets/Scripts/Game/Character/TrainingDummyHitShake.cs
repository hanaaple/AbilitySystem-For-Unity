using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Game.Character
{
    /// <summary>허수아비가 맞을 때마다 모델을 뒤로 젖혔다가 흔들리며 돌아오게 한다. 히트박스·체력 바는 그대로 두고 겉모습만 흔든다.</summary>
    public class TrainingDummyHitShake : MonoBehaviour
    {
        [SerializeField] private TrainingDummy dummy;
        [Tooltip("흔들 겉모습. 피벗(바닥)을 축으로 기운다.")]
        [SerializeField] private Transform model;

        [SerializeField] private float angle = 8f;
        [SerializeField] private float headAngle = 14f;
        [Tooltip("초당 흔들림 횟수.")]
        [SerializeField] private float frequency = 9f;
        [Tooltip("클수록 빨리 멈춘다.")]
        [SerializeField] private float damping = 7f;

        private Quaternion _baseRotation;
        private float _amplitude;
        private float _time;

        private void Awake()
        {
            _baseRotation = model.localRotation;
        }

        private void OnEnable()
        {
            dummy.OnDamaged += OnDamaged;
        }

        private void LateUpdate()
        {
            if (_amplitude <= 0f)
            {
                return;
            }

            _time += Time.deltaTime;
            float decay = Mathf.Exp(-damping * _time);
            if (decay < 0.01f)
            {
                _amplitude = 0f;
                model.localRotation = _baseRotation;
                return;
            }

            // 앞(+Z)에서 맞으니 뒤로 젖혀지는 쪽(-X 회전)부터 흔들린다
            float tilt = -_amplitude * decay * Mathf.Cos(_time * frequency * Mathf.PI * 2f);
            model.localRotation = _baseRotation * Quaternion.Euler(tilt, 0f, 0f);
        }

        private void OnDisable()
        {
            dummy.OnDamaged -= OnDamaged;
            model.localRotation = _baseRotation;
        }

        // 연타는 흔들림을 처음부터 다시 시작하되, 더 센 흔들림이 진행 중이면 그 세기를 유지한다
        private void OnDamaged(float damage, GameplayEffectSpec source)
        {
            bool isHead = source != null && DamageExecution.IsHeadHit(source.Context);
            float target = isHead ? headAngle : angle;
            float current = _amplitude * Mathf.Exp(-damping * _time);
            _amplitude = Mathf.Max(target, current);
            _time = 0f;
        }
    }
}
