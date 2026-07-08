using System;
using System.Collections.Generic;
using Core.Common;
using Core.ItemSystem.Module;
using UnityEngine;

namespace Core.ItemSystem
{
    // 모든 아이템(장비/소비/중요)의 공통 정보만 보유한다.
    // 종류별 데이터(장비의 부위 등)는 서브클래스가 갖는다 — 소비 아이템에 slot 같은 무의미한 필드가 붙지 않도록.
    public abstract class ItemData : ScriptableObject
    {
        public uint uid;
        public string displayName;
        public Sprite icon;

        public GameObject prefab;

        // D6/INV-10: 아이템 전용 자유형 로직(ItemRuntime)의 타입만 지정한다(SO 무상태 — INV-1).
        // 실제 객체는 ItemInstance가 per-instance로 생성(CreateRuntime). 인스펙터 드롭다운으로 선택(None=미지정).
        [SerializeField, SubclassSelector(typeof(ItemRuntime))]
        private string runtimeClass;

        [SerializeReference]
        public List<ItemModule> modules = new();

        // 선택된 타입으로 새 ItemRuntime을 만든다(미지정/해결 실패 시 null). 매 호출 새 객체 — SO에 상태를 두지 않는다(INV-1).
        public ItemRuntime CreateRuntime()
        {
            if (string.IsNullOrEmpty(runtimeClass))
            {
                return null;
            }

            Type type = Type.GetType(runtimeClass);
            if (type == null)
            {
                Debug.LogWarning($"[ItemData] ItemRuntime 타입을 찾지 못했습니다: {runtimeClass}");
                return null;
            }

            return (ItemRuntime)Activator.CreateInstance(type);
        }
    }
}
