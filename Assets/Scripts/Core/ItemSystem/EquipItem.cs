using UnityEngine;

namespace Core.ItemSystem
{
    public enum SlotType
    {
        Weapon,
        Armor,
        Accessory,
    }

    // INV-11: 장비 시스템(EquipmentComponent)은 구체 타입 EquipItem이 아니라 이 능력으로 장착 대상을 받는다.
    // 지금은 소비하는 시스템이 실재하는 능력이 장착뿐이라, 능력 인터페이스도 이것만 노출한다.
    public interface IEquippable
    {
        SlotType Slot { get; }
    }

    [CreateAssetMenu(menuName = "Item/Equip Item", fileName = "EquipItem")]
    public class EquipItem : ItemData, IEquippable
    {
        [SerializeField] private SlotType slot;

        public SlotType Slot => slot;
    }
}
