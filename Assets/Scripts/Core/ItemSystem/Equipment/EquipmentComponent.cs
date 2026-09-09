
using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.ItemSystem.Module;
using Item.Module;
using UnityEngine;

namespace Core.ItemSystem.Equipment
{
    // 장착 상태 관리 + 장착 무기 공격 구동 + 장착 프리팹의 소켓 부착(표현)을 겸하는 시스템 컴포넌트. 특정 캐릭터 클래스에 의존하지 않는다(장비 상태가 캐릭터 수명과 얽히지 않게).
    // slot은 딕셔너리 필드로 관리(Module 아님). 표현(소켓 소환)도 겸한다(장비=한 개념) — 소켓 미지정 슬롯은 비주얼 없이 장착만.
    [RequireComponent(typeof(AbilitySystemComponent))]
    public class EquipmentComponent : MonoBehaviour
    {
        // 판정 원점을 캐릭터 정면으로 살짝 밀어내는 오프셋(자기 자신만 겹치는 걸 피함).
        private const float AttackOriginForwardOffset = 1f;

        // 부위별 프리팹 부착점. 인스펙터에서 손/등 등 Transform 지정(미지정 슬롯은 비주얼 없음).
        [SerializeField] private SocketBinding[] sockets;

        private AbilitySystemComponent _asc;

        // 부위 → 장착된 아이템 인스턴스. per-item 상태는 ItemInstance가 소유.
        private readonly Dictionary<SlotType, ItemInstance> _equipped = new();

        // 부위 → 소켓 Transform(인스펙터 sockets에서 빌드) / 소환된 표현 오브젝트.
        private readonly Dictionary<SlotType, Transform> _socketBySlot = new();
        private readonly Dictionary<SlotType, GameObject> _spawnedVisual = new();

        [Serializable]
        private struct SocketBinding
        {
            public SlotType slot;
            public Transform socket;
        }

        // 자기 자신을 판정에서 제외하기 위한 소유자 ASC 재사용. 오버랩 버퍼는 매 공격 재사용.
        private readonly Collider[] _hitBuffer = new Collider[16];

        public event Action<SlotType, ItemInstance> OnEquipped;
        public event Action<SlotType, ItemInstance> OnUnequipped;

        private void Awake()
        {
            _asc = GetComponent<AbilitySystemComponent>();

            if (sockets != null)
            {
                foreach (SocketBinding binding in sockets)
                {
                    if (binding.socket != null)
                    {
                        _socketBySlot[binding.slot] = binding.socket;
                    }
                }
            }
        }

        // 인벤토리에서 넘겨받은 ItemInstance를 그 아이템의 부위에 장착한다.
        // 장비 여부는 카테고리가 아니라 IEquippable 능력으로 판별한다.
        public bool Equip(ItemInstance instance)
        {
            if (instance?.Data is not IEquippable equippable)
            {
                return false;
            }

            SlotType slot = equippable.Slot;
            if (_equipped.ContainsKey(slot))
            {
                Unequip(slot);
            }

            _equipped[slot] = instance;

            // 모듈 순회·상태 정렬은 ItemInstance가 담당한다. 컨텍스트로 소유자 ASC를 넘긴다.
            instance.OnEquip(new AbilitySystemModuleContext(_asc));

            SpawnVisual(slot, instance);

            OnEquipped?.Invoke(slot, instance);
            return true;
        }

        public bool Unequip(SlotType slot)
        {
            if (!_equipped.TryGetValue(slot, out ItemInstance instance))
            {
                return false;
            }

            instance.OnUnEquip(new AbilitySystemModuleContext(_asc));

            DespawnVisual(slot);

            _equipped.Remove(slot);
            OnUnequipped?.Invoke(slot, instance);
            return true;
        }

        // 무기 슬롯의 IWeaponAttack 능력을 읽어 평타를 수행한다.
        // 모듈은 데미지 효과·사거리라는 "결정"만 주고, 오버랩 감지·GE 적용은 여기(시스템)서 한다.
        public void TriggerAttack()
        {
            if (!_equipped.TryGetValue(SlotType.Weapon, out ItemInstance weapon))
            {
                return;
            }

            if (!TryGetAttack(weapon, out IWeaponAttack attack) || attack.DamageEffect == null)
            {
                return;
            }

            Vector3 origin = transform.position + transform.forward * AttackOriginForwardOffset;
            int count = Physics.OverlapSphereNonAlloc(origin, attack.Range, _hitBuffer);
            for (int i = 0; i < count; i++)
            {
                // 콜라이더가 자식에 있을 수 있으므로 부모까지 훑어 대상 ASC를 찾는다.
                var targetAsc = _hitBuffer[i].GetComponentInParent<AbilitySystemComponent>();
                if (targetAsc == null || targetAsc == _asc)
                {
                    continue;
                }

                // Ability System 재사용: 대상이 자신에게 Instant 데미지 효과를 적용 → Health BaseValue 감소.
                targetAsc.ApplyGameplayEffectToSelf(attack.DamageEffect);
            }
        }

        // 구체 타입이 아니라 능력 인터페이스로 공격 모듈을 찾는다.
        private static bool TryGetAttack(ItemInstance instance, out IWeaponAttack attack)
        {
            foreach (ItemModule module in instance.Data.modules)
            {
                if (module.Enabled && module is IWeaponAttack found)
                {
                    attack = found;
                    return true;
                }
            }

            attack = null;
            return false;
        }

        // 장착 아이템의 표현 프리팹을 부위 소켓에 소환하고 ItemBehaviour를 인스턴스와 연결한다.
        // 소환·연결 로직은 여기(시스템)에 있고 ItemBehaviour엔 없다. 프리팹/소켓 없으면 조용히 스킵.
        private void SpawnVisual(SlotType slot, ItemInstance instance)
        {
            GameObject prefab = instance.Data.prefab;
            if (prefab == null)
            {
                return; // 표현 프리팹이 없는 아이템은 비주얼 없이 장착만.
            }

            if (!_socketBySlot.TryGetValue(slot, out Transform socket))
            {
                return; // 소켓 미지정 슬롯은 비주얼 없이 장착만.
            }

            GameObject go = Instantiate(prefab, socket, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // 프리팹이 ItemBehaviour를 지녔으면 자기 인스턴스와 연결. 없으면 순수 비주얼로 둔다.
            if (go.TryGetComponent(out ItemBehaviour behaviour))
            {
                behaviour.Bind(instance);
            }

            _spawnedVisual[slot] = go;
        }

        private void DespawnVisual(SlotType slot)
        {
            if (_spawnedVisual.TryGetValue(slot, out GameObject go))
            {
                Destroy(go);
                _spawnedVisual.Remove(slot);
            }
        }
    }
}
