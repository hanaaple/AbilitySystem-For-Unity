using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Game
{
    /// <summary>트리거 구역. 들어온 캐릭터에게 머무는 동안 GE를 걸고, 나가면 그 GE를 지운 뒤 나갈 때 GE를 건다.</summary>
    [RequireComponent(typeof(Collider))]
    public class GameplayEffectZone : MonoBehaviour
    {
        [Tooltip("안에 있는 동안 유지하는 GE(Infinite). 나가면 지운다.")]
        [SerializeField] private List<GameplayEffectAsset> whileInsideEffects = new();

        [Tooltip("나갈 때 거는 GE. 나와도 잠시 남는 디버프(Duration)용.")]
        [SerializeField] private List<GameplayEffectAsset> onExitEffects = new();

        // 한 캐릭터에 콜라이더가 여럿이면 Enter/Exit도 그 수만큼 오므로 ASC별로 겹친 콜라이더 수를 센다.
        private readonly Dictionary<AbilitySystemComponent, int> _overlapCounts = new();
        private readonly Dictionary<AbilitySystemComponent, List<ActiveGameplayEffectHandle>> _insideHandles = new();

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var asc = other.GetComponentInParent<AbilitySystemComponent>();
            if (asc == null)
            {
                return;
            }

            _overlapCounts.TryGetValue(asc, out int count);
            _overlapCounts[asc] = count + 1;
            if (count > 0)
            {
                return;
            }

            var handles = new List<ActiveGameplayEffectHandle>();
            foreach (GameplayEffectAsset effect in whileInsideEffects)
            {
                handles.Add(asc.ApplyGameplayEffectToSelf(effect));
            }

            _insideHandles[asc] = handles;
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
            if (_insideHandles.Remove(asc, out List<ActiveGameplayEffectHandle> handles))
            {
                foreach (ActiveGameplayEffectHandle handle in handles)
                {
                    asc.RemoveActiveGameplayEffect(handle);
                }
            }

            foreach (GameplayEffectAsset effect in onExitEffects)
            {
                asc.ApplyGameplayEffectToSelf(effect);
            }
        }
    }
}
