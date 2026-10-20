using System.Collections.Generic;
using Core.ItemSystem.Module;

namespace Core.ItemSystem
{
    // 아이템의 런타임 상태를 소유하는 POCO.
    // ItemDataAsset(SO)는 무상태 템플릿이고, 아이템별 가변 상태는 여기서만 산다.
    // 아이템별 상속은 두지 않는다 — 특수화는 modules 조합으로.
    public class ItemInstance
    {
        // 템플릿 참조. 읽기 전용으로만 다룬다 — SO에 런타임 값을 쓰지 않는다.
        private readonly ItemDataAsset _data;

        // _data.modules와 인덱스 1:1로 정렬된 모듈별 상태. 무상태 모듈 슬롯은 null.
        private readonly IModuleState[] _states;

        // 아이템 전용 자유형 로직. 합성 멤버(상속 아님). SO는 타입만, 여기서 per-instance 생성. 미지정이면 null.
        private readonly ItemRuntime _runtime;

        public ItemDataAsset Data => _data;
        public ItemRuntime Runtime => _runtime;

        public ItemInstance(ItemDataAsset data)
        {
            _data = data;

            List<ItemModule> modules = data.modules;
            _states = new IModuleState[modules.Count];
            for (int i = 0; i < modules.Count; i++)
            {
                _states[i] = modules[i].CreateState(); // 무상태 모듈은 null 반환
            }

            _runtime = data.CreateRuntime(); // 타입 미지정이면 null
            _runtime?.Bind(this);
        }

        // IModuleState → 구체 상태 캐스트는 이 제네릭 접근자 한 곳에만 둔다(모듈 코드가 직접 캐스팅하지 않도록).
        public TState GetState<TState>(int moduleIndex) where TState : class, IModuleState
        {
            return _states[moduleIndex] as TState;
        }

        // 장착/해제 시 각 모듈을 자기 상태(_states[i])와 함께 구동한다.
        // 상태 배열은 여기 캡슐화되어 외부로 새지 않는다.
        public void OnEquip(ModuleContext context)
        {
            List<ItemModule> modules = _data.modules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].Enabled)
                {
                    modules[i].OnEquip(context, _states[i]);
                }
            }

            _runtime?.OnEquip(context);
        }

        public void OnUnEquip(ModuleContext context)
        {
            List<ItemModule> modules = _data.modules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].Enabled)
                {
                    modules[i].OnUnEquip(context, _states[i]);
                }
            }

            _runtime?.OnUnEquip(context);
        }
    }
}
