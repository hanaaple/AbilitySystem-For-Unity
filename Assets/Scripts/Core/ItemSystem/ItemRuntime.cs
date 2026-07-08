using Core.ItemSystem.Module;

namespace Core.ItemSystem
{
    // 아이템 전용 자유형 로직(INV-10). 여러 아이템에 재사용되지 않는 유니크 로직을 담는 베이스.
    // MonoBehaviour가 아니다 — 씬에 붙지 않는 POCO이고, 라이프 사이클은 ItemInstance가 수동 구동한다.
    // ItemInstance가 per-instance로 생성·보유(합성 — INV-2, 상속 아님), SO(ItemData)는 타입만 지정(무상태 — INV-1, D6).
    public abstract class ItemRuntime
    {
        // 소유 인스턴스. 생성 직후 ItemInstance가 Bind로 연결한다.
        protected ItemInstance Item { get; private set; }

        public void Bind(ItemInstance item)
        {
            Item = item;
        }

        // 라이프 사이클 훅 — ItemInstance.OnEquip/OnUnEquip이 forward한다(모듈 lifecycle과 대칭).
        public virtual void OnEquip(ModuleContext context)
        {
        }

        public virtual void OnUnEquip(ModuleContext context)
        {
        }
    }
}
