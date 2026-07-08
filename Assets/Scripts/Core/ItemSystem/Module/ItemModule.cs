using System;
using UnityEngine;

namespace Core.ItemSystem.Module
{
    public interface IModuleState
    {
    }

    [Serializable]
    public abstract class ItemModule
    {
        [SerializeField] private bool enabled = true;

        public bool Enabled => enabled;

        public abstract IModuleState CreateState();

        public virtual bool IsValidFor(ItemDataAsset ItemDataAsset)
        {
            return ItemDataAsset != null;
        }

        // 상태가 필요한 모듈을 위해 자기 상태를 함께 받는다(무상태 모듈은 state=null).
        // 순회·상태 정렬은 ItemInstance가 담당한다 — 상태 배열은 거기 캡슐화(INV-5).
        public virtual void OnEquip(ModuleContext context, IModuleState state)
        {
        }

        public virtual void OnUnEquip(ModuleContext context, IModuleState state)
        {
        }

        public virtual void Tick(ModuleContext context, float deltaTime)
        {
        }
    }

    public abstract class ItemModule<TState> : ItemModule where TState : class, IModuleState, new()
    {
        public sealed override IModuleState CreateState() => new TState();

        // INV-5: IModuleState → 구체 상태 캐스트는 이 제네릭 베이스 한 곳에만 둔다.
        public sealed override void OnEquip(ModuleContext context, IModuleState state) => OnEquip(context, (TState)state);
        public sealed override void OnUnEquip(ModuleContext context, IModuleState state) => OnUnEquip(context, (TState)state);

        protected virtual void OnEquip(ModuleContext context, TState state)
        {
        }

        protected virtual void OnUnEquip(ModuleContext context, TState state)
        {
        }
    }

    public abstract class StatelessModule : ItemModule
    {
        public sealed override IModuleState CreateState() => null;
    }
}
