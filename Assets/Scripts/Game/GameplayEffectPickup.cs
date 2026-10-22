using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game
{
    // 트리거에 들어온 액터의 ASC에 GE를 적용하고, 지정된 GA가 있으면 발동한다.
    // 물리 시뮬레이션 없이 트리거 감지로만 동작한다.
    [RequireComponent(typeof(Collider))]
    public class GameplayEffectPickup : MonoBehaviour, IDebugOverlayTarget
    {
        [SerializeField] private GameplayEffectAsset effect;
        [SerializeField] private float level = 1f;

        // Instant GE는 태그를 부여할 수 없어, 먹은 순간의 연출 태그 같은 것은 따로 거는 지속형 GE로 준다
        [Tooltip("effect와 함께 같은 대상에게 거는 GE들(예: 잠깐 연출 태그를 주는 지속형 GE).")]
        [SerializeField] private List<GameplayEffectAsset> additionalEffects = new();

        [Tooltip("먹은 대상에게 바로 발동하는 GA. 대상에게 없으면 부여한 뒤 발동하고, 다음에 또 먹을 때 쓰도록 부여한 채 둔다.")]
        [SerializeField] private GameplayAbilityAsset ability;

        [Tooltip("디버그 오버레이에서 겨눴을 때 보일 썸네일.")]
        [SerializeField] private Texture2D debugThumbnail;

        // 끄면 반복 적용 테스트용으로 남는다(트리거를 다시 밟을 때마다 적용).
        [Tooltip("재생성 쿨다운 GE가 비었을 때만 쓴다.")]
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Respawn")]
        [Tooltip("픽업 자신의 ASC에 거는 Duration GE. 이 GE가 주는 태그가 붙어 있는 동안 숨고, 빠지면 다시 나타난다. 비우면 재생성하지 않는다.")]
        [SerializeField] private GameplayEffectAsset respawnCooldownEffect;

        [Tooltip("재생성 쿨다운 GE가 주는 태그. 그 GE의 부여 태그와 같아야 한다.")]
        [SerializeField] private GameplayTag respawnCooldownTag;

        [Tooltip("쿨다운 동안 숨기는 겉모습.")]
        [SerializeField] private GameObject visual;

        [Tooltip("겉모습이 도는 속도(도/초).")]
        [SerializeField] private float rotationSpeed = 90f;

        [Tooltip("겉모습이 위아래로 떠오르는 폭(m)과 속도(회/초).")]
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobSpeed = 0.6f;

        // 같은 프레임에 같은 액터가 중복 적용되는 것을 막는다.
        // OnTriggerEnter는 **콜라이더 단위**로 오므로, 한 액터에 콜라이더가 여러 개면
        // (예: Player 루트의 CapsuleCollider + CharacterController) 콜백이 그 수만큼 온다.
        // Destroy는 프레임 끝까지 지연되므로 destroyOnPickup만으로는 막히지 않는다.
        private AbilitySystemComponent _lastTarget;
        private int _lastFrame = -1;

        private AbilitySystemComponent _selfAbilitySystem;
        private Collider _trigger;
        private Vector3 _visualBasePosition;

        private bool UsesRespawn => respawnCooldownEffect != null && _selfAbilitySystem != null;

        public string DebugName => name;

        public Texture2D DebugThumbnail => debugThumbnail;

        // 픽업 자신의 ASC — 재생성 쿨다운 GE가 여기 걸린다
        public AbilitySystemComponent DebugAbilitySystem => _selfAbilitySystem;

        public void AppendDebugLines(List<string> lines)
        {
            lines.Add("Grants\t" + (ability != null ? ability.name : effect != null ? effect.name : "-"));
            bool isAvailable = !UsesRespawn || !_selfAbilitySystem.HasMatchingGameplayTag(respawnCooldownTag);
            lines.Add("State\t" + (isAvailable ? "<color=#7CFC9A>available</color>" : "<color=#FFB86B>respawning</color>"));
        }

        private void Reset()
        {
            // 픽업은 트리거로 동작한다(에디터에서 컴포넌트 추가 시 자동 설정).
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            _selfAbilitySystem = GetComponent<AbilitySystemComponent>();
            if (visual != null)
            {
                _visualBasePosition = visual.transform.localPosition;
            }
        }

        private void Start()
        {
            if (UsesRespawn)
            {
                _selfAbilitySystem.RegisterGameplayTagEvent(respawnCooldownTag, OnRespawnCooldownChanged);
            }
        }

        private void Update()
        {
            if (visual != null)
            {
                visual.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
                float bob = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobHeight;
                visual.transform.localPosition = _visualBasePosition + Vector3.up * bob;
            }
        }

        private void OnDestroy()
        {
            if (UsesRespawn)
            {
                _selfAbilitySystem.UnregisterGameplayTagEvent(OnRespawnCooldownChanged, respawnCooldownTag);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPickUp(other);
        }

        private void TryPickUp(Collider other)
        {
            if (effect == null && ability == null)
            {
                return;
            }

            if (UsesRespawn && _selfAbilitySystem.HasMatchingGameplayTag(respawnCooldownTag))
            {
                return;
            }

            // 콜라이더가 자식에 달린 구성을 허용한다.
            AbilitySystemComponent asc = other.GetComponentInParent<AbilitySystemComponent>();
            if (asc == null || asc == _selfAbilitySystem)
            {
                return;
            }

            if (asc == _lastTarget && Time.frameCount == _lastFrame)
            {
                return;
            }

            _lastTarget = asc;
            _lastFrame = Time.frameCount;

            if (effect != null)
            {
                asc.ApplyGameplayEffectToSelf(effect, default, level);
            }

            foreach (GameplayEffectAsset additionalEffect in additionalEffects)
            {
                if (additionalEffect != null)
                {
                    asc.ApplyGameplayEffectToSelf(additionalEffect, default, level);
                }
            }

            if (ability != null)
            {
                ActivateAbility(asc);
            }

            Debug.Log($"[GEPickup] '{name}' → '{asc.name}' 적용");

            if (UsesRespawn)
            {
                _selfAbilitySystem.ApplyGameplayEffectToSelf(respawnCooldownEffect);
                return;
            }

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }

        private void ActivateAbility(AbilitySystemComponent asc)
        {
            GameplayAbilitySpec spec = asc.FindAbilitySpecFromAsset(ability);
            GameplayAbilitySpecHandle handle = spec != null ? spec.Handle : asc.GiveAbility(new GameplayAbilitySpec(ability));
            asc.TryActivateAbility(handle);
        }

        // 다시 나타날 때 이미 올라서 있던 액터는 트리거 진입이 다시 오지 않으므로 직접 찾는다
        /// <remarks>UE(Lyra): ALyraWeaponSpawner::CheckForExistingOverlaps</remarks>
        private void PickUpExistingOverlaps()
        {
            Bounds bounds = _trigger.bounds;
            foreach (Collider other in Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                TryPickUp(other);
            }
        }

        private void OnRespawnCooldownChanged(GameplayTag changedTag, int newCount)
        {
            bool isAvailable = newCount == 0;
            if (visual != null)
            {
                visual.SetActive(isAvailable);
            }

            if (isAvailable)
            {
                PickUpExistingOverlaps();
            }
        }
    }
}
