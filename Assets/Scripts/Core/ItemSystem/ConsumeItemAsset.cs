using UnityEngine;

namespace Core.ItemSystem
{
    // 인벤토리는 구체 타입이 아니라 이 능력으로 "스택되는 아이템"을 판별한다.
    // 스택은 일부 아이템의 능력이지 전 아이템 속성이 아니다 — 장비(EquipItemAsset)는 이 능력이 없어 스택되지 않는다.
    // (IEquippable과 동형: 능력 인터페이스는 그 능력을 가진 카테고리 서브클래스에만 붙인다 — ItemDataAsset 베이스엔 두지 않음.)
    public interface IStackable
    {
        // 한 인벤 엔트리가 누적할 수 있는 최대 개수.
        int MaxStack { get; }
    }

    // 소비 아이템. 장비가 아니므로 slot을 갖지 않는다.
    // 사용 효과(Use)는 그걸 소비하는 시스템(사용 처리기)이 생길 때 IConsumable로 노출한다 — 지금은 껍데기.
    [CreateAssetMenu(menuName = "Item/Consume Item", fileName = "ConsumeItemAsset")]
    public class ConsumeItemAsset : ItemDataAsset, IStackable
    {
        [SerializeField] private int maxStack = 99;

        public int MaxStack => maxStack;

        private void OnValidate()
        {
            // 스택 상한은 최소 1 — 0/음수는 인벤 병합 로직을 깨뜨린다.
            if (maxStack < 1)
            {
                maxStack = 1;
            }
        }
    }
}
