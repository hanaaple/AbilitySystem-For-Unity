using System.Collections.Generic;
using UnityEngine;

namespace Core.ItemSystem.Inventory
{
    // Inventory(순수 POCO)를 캐릭터/씬에 바인딩하는 뷰 컴포넌트(D8의 "바인딩 뷰").
    // 소비처(장비·UI)는 Inventory 프로퍼티를 읽고 OnChanged로 갱신한다(INV-8) — 폴링 금지.
    //
    // [test-stage] 지금은 이 컴포넌트가 POCO를 직접 소유한다.
    //   D8은 원래 "지속 소유자 계층이 POCO를 보유, 컴포넌트는 참조만" 이지만 그 계층이 아직 없다.
    //   재평가 트리거: 빙의(body-swap)로 몸이 바뀌어도 인벤이 살아남아야 하면 → 소유를 계층으로 올리고
    //   여기선 참조만 주입받도록 전환한다. 지금은 단일 몸 전제라 컴포넌트 소유가 최소해다(북극성).
    public class InventoryComponent : MonoBehaviour
    {
        // 시작 시 담을 아이템(인스펙터에서 지정). 테스트/초기 지급용.
        [SerializeField] private List<ItemData> startingItems = new();

        private readonly ItemSystem.Inventory.Inventory _inventory = new();
        private CharacterBase _owner;

        public ItemSystem.Inventory.Inventory Inventory => _inventory;

        private void Awake()
        {
            _owner = GetComponent<CharacterBase>();

            foreach (ItemData item in startingItems)
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

        // D6: "사망 = 소실"은 GameObject 파괴가 아니라 소유자가 Clear()를 구동하는 것.
        private void HandleOwnerDeath(CharacterBase _)
        {
            _inventory.Clear();
        }
    }
}
