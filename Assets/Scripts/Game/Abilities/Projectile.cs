using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Game.Character;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Abilities
{
    /// <summary>앞으로 직진하다 무언가에 닿거나 수명이 다하면 멈추는 투사체. 관통이면 캐릭터는 뚫고 지나가며 하나씩 맞힌다. 연출은 자식 VFX 이벤트, 또는 탄두를 숨기고 명중 이펙트를 생성하는 것으로 한다.</summary>
    public class Projectile : MonoBehaviour
    {
        private const string CreateEvent = "create";
        private const string HitEvent = "hit";
        private const string StopEvent = "stop";

        [Tooltip("있으면 발사·명중·정지 이벤트를 보낸다.")]
        [SerializeField] private VisualEffect visualEffect;

        [Tooltip("멈추면 숨기는 탄두. 꼬리 궤적처럼 남아서 사라져야 하는 것은 여기 넣지 않는다.")]
        [SerializeField] private GameObject body;

        [Tooltip("명중 지점에 생성하는 이펙트. 스스로 사라져야 한다(파티클 Stop Action = Destroy 등).")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Tooltip("이 태그의 부위(HitZone)에 맞으면 hitEffectPrefab 대신 headHitEffectPrefab을 생성한다.")]
        [SerializeField] private GameplayTag headZoneTag;
        [Tooltip("비워 두면 머리에 맞아도 hitEffectPrefab을 쓴다.")]
        [SerializeField] private GameObject headHitEffectPrefab;

        [SerializeField] private float speed = 15f;
        [SerializeField] private float radius = 0.2f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Tooltip("켜면 캐릭터(ASC)를 뚫고 지나가며 닿은 캐릭터마다 한 번씩 GE를 건다. 캐릭터가 아닌 것(벽 등)에 닿으면 멈춘다.")]
        [SerializeField] private bool piercesCharacters;

        [Tooltip("몸에 닿은 뒤 같은 방향으로 이만큼 더 들어가며 맞은 부위(머리 등)를 찾는다. 캐릭터 몸 두께보다 길게.")]
        [SerializeField] private float hitZoneSearchDepth = 1f;

        // 멈춘 뒤에도 이미 뿌린 파티클이 사라질 때까지 오브젝트를 남겨 둔다
        [SerializeField] private float despawnDelay = 2f;

        private readonly HashSet<AbilitySystemComponent> _hitTargets = new();
        private GameObject _instigator;
        private IReadOnlyList<GameplayEffectSpec> _hitEffectSpecs = Array.Empty<GameplayEffectSpec>();
        private float _remainingLifeTime;
        private bool _isFlying;

        private void Update()
        {
            if (!_isFlying)
            {
                return;
            }

            float distance = speed * Time.deltaTime;
            bool hasStopped = piercesCharacters ? MovePiercing(distance) : MoveUntilHit(distance);
            if (hasStopped)
            {
                return;
            }

            _remainingLifeTime -= Time.deltaTime;
            if (_remainingLifeTime <= 0f)
            {
                Stop(null);
            }
        }

        /// <summary>날리기 시작한다. instigator 자신(자식 콜라이더 포함)에는 맞지 않는다. hitEffectSpecs는 맞힌 대상마다 모두 걸린다(비면 연출만).</summary>
        public void Launch(GameObject instigator, IReadOnlyList<GameplayEffectSpec> hitEffectSpecs)
        {
            _instigator = instigator;
            _hitEffectSpecs = hitEffectSpecs ?? Array.Empty<GameplayEffectSpec>();
            _remainingLifeTime = lifeTime;
            _isFlying = true;
            if (visualEffect != null)
            {
                visualEffect.SendEvent(CreateEvent);
            }
        }

        // 처음 닿은 것에서 멈춘다. 멈췄으면 true.
        private bool MoveUntilHit(float distance)
        {
            if (!TryFindHit(distance, out RaycastHit hit))
            {
                transform.position += transform.forward * distance;
                return false;
            }

            // 시작부터 겹친 충돌은 hit.point가 비어 있어 거리로 위치를 정한다
            transform.position += transform.forward * hit.distance;
            RaycastHit partHit = ResolveHitPart(hit);
            ApplyHitEffects(partHit);
            SpawnHitEffect(partHit.collider);
            Stop(HitEvent);
            return true;
        }

        // 이번 이동 거리 안에서 닿은 것을 가까운 순으로 본다 — 캐릭터는 처음 닿을 때만 맞히고 지나가고, 캐릭터가 아닌 것에 닿으면 거기서 멈춘다. 멈췄으면 true.
        private bool MovePiercing(float distance)
        {
            Vector3 start = transform.position;
            Vector3 direction = transform.forward;
            RaycastHit[] hits = Physics.SphereCastAll(start, radius, direction, distance, hitLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(_instigator.transform))
                {
                    continue;
                }

                // 부위 탐색이 지금 위치에서 레이를 쏘므로 닿은 자리로 옮겨 둔다
                transform.position = start + direction * hit.distance;
                var target = hit.collider.GetComponentInParent<AbilitySystemComponent>();
                if (target == null)
                {
                    SpawnHitEffect(hit.collider);
                    Stop(HitEvent);
                    return true;
                }

                // 한 캐릭터에 콜라이더가 여럿이라 같은 대상이 여러 번 잡힌다
                if (!_hitTargets.Add(target))
                {
                    continue;
                }

                RaycastHit partHit = ResolveHitPart(hit);
                ApplyHitEffects(partHit);
                SpawnHitEffect(partHit.collider);
                if (visualEffect != null)
                {
                    visualEffect.SendEvent(HitEvent);
                }
            }

            transform.position = start + direction * distance;
            return false;
        }

        private bool TryFindHit(float distance, out RaycastHit closestHit)
        {
            closestHit = default;
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, transform.forward, distance, hitLayers, QueryTriggerInteraction.Ignore);
            bool found = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(_instigator.transform))
                {
                    continue;
                }

                if (!found || hit.distance < closestHit.distance)
                {
                    closestHit = hit;
                    found = true;
                }
            }

            return found;
        }

        // 캐릭터에 맞았으면 그 부위(HitZone)의 충돌로 바꾼다. 부위를 못 찾았거나 캐릭터가 아니면 그대로.
        private RaycastHit ResolveHitPart(RaycastHit hit)
        {
            var target = hit.collider.GetComponentInParent<AbilitySystemComponent>();
            if (target == null)
            {
                return hit;
            }

            return TryFindHitZone(target, out RaycastHit zoneHit) ? zoneHit : hit;
        }

        private void ApplyHitEffects(RaycastHit partHit)
        {
            // 콜라이더가 자식에 달린 캐릭터도 맞힌 것으로 본다
            var target = partHit.collider.GetComponentInParent<AbilitySystemComponent>();
            if (target == null)
            {
                return;
            }

            foreach (GameplayEffectSpec hitEffectSpec in _hitEffectSpecs)
            {
                // 관통하면 한 Spec이 여러 대상에 걸린다 — 대상마다 Spec·컨텍스트를 복제해 피격 정보가 섞이지 않게 한다(늦게 실행되는 GE가 마지막 대상의 부위를 읽지 않게).
                // UE: FGameplayAbilityTargetData::ApplyGameplayEffectSpec
                GameplayEffectSpec specToApply = hitEffectSpec.Clone();
                GameplayEffectContextHandle context = hitEffectSpec.Context.Duplicate();
                specToApply.SetContext(context);
                context.AddHitResult(partHit);
                target.ApplyGameplayEffectSpecToSelf(specToApply);
            }
        }

        // 부위 콜라이더는 이동용 몸 콜라이더 안쪽 트리거라 첫 충돌로는 안 잡힌다 — 몸에 닿은 자리에서 조금 더 들어가며 그 대상의 부위를 찾는다.
        // 탄 반경이 아닌 중심선으로 찾는다 — 반경만큼 부위가 넓어지지 않게, 십자선이 가리킨 부위(PlayerCharacter.FindAimedHitZone)와 같게.
        private bool TryFindHitZone(AbilitySystemComponent target, out RaycastHit zoneHit)
        {
            zoneHit = default;
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, hitZoneSearchDepth, hitLayers, QueryTriggerInteraction.Collide);
            bool found = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponent<HitZone>() == null || hit.collider.GetComponentInParent<AbilitySystemComponent>() != target)
                {
                    continue;
                }

                if (!found || hit.distance < zoneHit.distance)
                {
                    zoneHit = hit;
                    found = true;
                }
            }

            return found;
        }

        private void SpawnHitEffect(Collider partCollider)
        {
            GameObject effectPrefab = IsHeadHit(partCollider) && headHitEffectPrefab != null ? headHitEffectPrefab : hitEffectPrefab;
            if (effectPrefab != null)
            {
                // 날아온 쪽으로 튀게 반대 방향을 본다
                Instantiate(effectPrefab, transform.position, Quaternion.LookRotation(-transform.forward));
            }
        }

        private bool IsHeadHit(Collider partCollider)
        {
            var hitZone = partCollider.GetComponent<HitZone>();
            if (hitZone == null || !headZoneTag.IsValid())
            {
                return false;
            }

            GameplayTag zoneTag = hitZone.ZoneTag;
            return zoneTag.MatchesTag(headZoneTag);
        }

        private void Stop(string finishEvent)
        {
            _isFlying = false;
            if (body != null)
            {
                body.SetActive(false);
            }

            if (visualEffect != null)
            {
                visualEffect.SendEvent(StopEvent);
                if (finishEvent != null)
                {
                    visualEffect.SendEvent(finishEvent);
                }
            }

            Destroy(gameObject, despawnDelay);
        }
    }
}
