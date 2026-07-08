namespace Core.ItemSystem.Inventory
{
    // 인벤토리 한 칸. ("Slot"은 장비 SlotType과 혼동되므로 Entry로 명명.)
    // 값은 Inventory만 바꾼다(internal set) — 외부 소비처는 읽기만 한다.
    public class InventoryEntry
    {
        public ItemDataAsset Data { get; }

        // 스택 아이템: 1..MaxStack / 비스택(장비 등): 항상 1.
        public int Count { get; internal set; }

        // INV-5: 비스택 아이템의 per-item 상태. 스택 아이템은 상태가 없어 null.
        public ItemInstance Instance { get; }

        internal InventoryEntry(ItemDataAsset data, int count, ItemInstance instance)
        {
            Data = data;
            Count = count;
            Instance = instance;
        }
    }
}
