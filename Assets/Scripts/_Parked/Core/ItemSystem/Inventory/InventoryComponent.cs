using System.Collections.Generic;
using UnityEngine;

namespace Core.ItemSystem.Inventory
{
    // Inventory(순수 POCO)를 캐릭터/씬에 바인딩하는 뷰 컴포넌트.
    // 소비처(장비·UI)는 Inventory 프로퍼티를 읽고 OnChanged로 갱신한다 — 폴링 금지.
    //
    // 지금은 이 컴포넌트가 POCO를 직접 소유한다(단일 몸 전제라 최소 구성).
    // 빙의로 몸이 바뀌어도 인벤이 살아남아야 하면 소유를 별도 계층으로 올리고 여기선 참조만 주입받도록 전환한다.
    public class InventoryComponent : MonoBehaviour
    {
        // 시작 시 담을 아이템(인스펙터에서 지정). 테스트/초기 지급용.
        [SerializeField] private List<ItemDataAsset> startingItems = new();

        private readonly ItemSystem.Inventory.Inventory _inventory = new();
        private CharacterBase _owner;

        public ItemSystem.Inventory.Inventory Inventory => _inventory;

        private void Awake()
        {
            _owner = GetComponent<CharacterBase>();

            foreach (ItemDataAsset item in startingItems)
            {
                if (item != null)
                {
                    _inventory.Add(item);
                }
            }
        }

        private void OnEnable()
        {
            if (_owner != null)
            {
                _owner.OnDeath += HandleOwnerDeath;
            }
        }

        private void OnDisable()
        {
            if (_owner != null)
            {
                _owner.OnDeath -= HandleOwnerDeath;
            }
        }

        // "사망 = 소실"은 GameObject 파괴가 아니라 소유자가 Clear()를 구동하는 것.
        private void HandleOwnerDeath(CharacterBase _)
        {
            _inventory.Clear();
        }
    }
}
