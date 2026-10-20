using Core.ItemSystem;
using Core.ItemSystem.Equipment;
using Core.ItemSystem.Inventory;
using UnityEngine;

namespace Item
{
    // 트리거에 들어온 액터의 인벤토리에 아이템을 넣고, 장비면 바로 장착한 뒤 자신을 소멸한다.
    // 필드 아이템(픽업) 최소 구현 — 정식 상호작용/드롭 시스템이 생기면 대체.
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDataAsset item;

        private void Reset()
        {
            // 픽업은 트리거로 동작한다(에디터에서 컴포넌트 추가 시 자동 설정).
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (item == null)
            {
                return;
            }

            InventoryComponent inventory = other.GetComponentInParent<InventoryComponent>();
            if (inventory == null)
            {
                return;
            }

            inventory.Inventory.Add(item);

            // 장비 능력이면 방금 넣은 엔트리를 뽑아 장착한다(인벤 → 장비).
            EquipmentComponent equipment = other.GetComponentInParent<EquipmentComponent>();
            if (equipment != null && item is IEquippable)
            {
                foreach (InventoryEntry entry in inventory.Inventory.Query(d => d == item))
                {
                    if (equipment.Equip(entry.Instance))
                    {
                        inventory.Inventory.Remove(entry);
                        break;
                    }
                }
            }

            Debug.Log($"[Pickup] '{item.displayName}' 획득{(item is IEquippable ? " · 장착" : "")}");
            Destroy(gameObject);
        }
    }
}
