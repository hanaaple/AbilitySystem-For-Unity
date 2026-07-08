using UnityEngine;

namespace Core.ItemSystem
{
    // 장착된 아이템의 씬 표현(비주얼) 앵커.
    // INV-4: 동작 로직은 두지 않는다 — 소유한 ItemInstance 참조 보유와 표현/이벤트 중계만.
    // 아이템별 특수 표현이 필요해지면(두 번째 사용처) 그때 파생 클래스로 연다 — 지금은 구체 1개로 충분.
    public class ItemBehaviour : MonoBehaviour
    {
        // 이 비주얼이 대변하는 런타임 인스턴스. 소환 직후 시스템이 Bind로 연결한다.
        public ItemInstance Item { get; private set; }

        public void Bind(ItemInstance item)
        {
            Item = item;
        }
    }
}
