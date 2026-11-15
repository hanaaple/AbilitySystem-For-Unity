using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    /// <summary>트리거 구역. 들어온 캐릭터에게 GA를 (없으면 부여하고) 발동하고, 나가면 그 GA를 취소한다. 안팎에서 무엇을 걸지는 GA가 정한다.</summary>
    [RequireComponent(typeof(Collider))]
    public class AbilityZone : MonoBehaviour, IDebugOverlayTarget
    {
        [Tooltip("안에 있는 동안 활성으로 두는 GA. 나가면 취소한다.")]
        [SerializeField] [FormerlySerializedAs("ability")] private GameplayAbilityAsset _ability;

        [Tooltip("디버그 오버레이에서 겨눴을 때 보일 썸네일.")]
        [SerializeField] [FormerlySerializedAs("debugThumbnail")] private Texture2D _debugThumbnail;

        // 한 캐릭터에 콜라이더가 여럿이면 Enter/Exit도 그 수만큼 오므로 ASC별로 겹친 콜라이더 수를 센다.
        private readonly Dictionary<AbilitySystemComponent, int> _overlapCounts = new();

        public string DebugName => name;

        public Texture2D DebugThumbnail => _debugThumbnail;

        public AbilitySystemComponent DebugAbilitySystem => null;

        public void AppendDebugLines(List<string> lines)
        {
            lines.Add("Grants\t" + (_ability != null ? _ability.name : "-"));
            lines.Add($"Inside\t{_overlapCounts.Count}");
            foreach (AbilitySystemComponent asc in _overlapCounts.Keys)
            {
                lines.Add("  " + asc.name);
            }
        }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var asc = other.GetComponentInParent<AbilitySystemComponent>();
            if (asc == null || _ability == null)
            {
                return;
            }

            _overlapCounts.TryGetValue(asc, out int count);
            _overlapCounts[asc] = count + 1;
            if (count > 0)
            {
                return;
            }

            GameplayAbilitySpec spec = asc.FindAbilitySpecFromAsset(_ability);
            GameplayAbilitySpecHandle handle = spec != null ? spec.Handle : asc.GiveAbility(new GameplayAbilitySpec(_ability));
            asc.TryActivateAbility(handle);
        }

        private void OnTriggerExit(Collider other)
        {
            var asc = other.GetComponentInParent<AbilitySystemComponent>();
            if (asc == null || !_overlapCounts.TryGetValue(asc, out int count))
            {
                return;
            }

            if (count > 1)
            {
                _overlapCounts[asc] = count - 1;
                return;
            }

            _overlapCounts.Remove(asc);
            asc.CancelAbility(_ability);
        }
    }
}
