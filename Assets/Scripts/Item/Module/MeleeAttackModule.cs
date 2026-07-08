using System;
using Core.AbilitySystem.Effect;
using Core.ItemSystem.Module;
using UnityEngine;

namespace Item.Module
{
    // INV-11: 장비 시스템은 "공격"을 구체 모듈(MeleeAttackModule)이 아니라 이 능력으로 요구한다.
    // 총기 등 다른 공격 방식이 생겨도 시스템은 이 인터페이스만 알면 된다.
    public interface IWeaponAttack
    {
        // 명중 대상에게 적용할 데미지 효과. 모듈은 "무엇을" 적용할지 결정만 하고(INV-3),
        // 실제 오버랩 감지·적용은 시스템(EquipmentComponent)이 한다.
        GameplayEffectAsset DamageEffect { get; }

        // 판정 반경(전방). 실제 판정도 시스템 몫이다.
        float Range { get; }
    }

    // S1 근접 평타: 쿨다운·콤보 같은 per-item 상태가 없으므로 무상태 모듈이다(상태화는 S6).
    // 씬/GameObject를 만지지 않고 데미지 효과와 사거리라는 "결정"만 보유한다(INV-3).
    [Serializable]
    public sealed class MeleeAttackModule : StatelessModule, IWeaponAttack
    {
        [SerializeField] private GameplayEffectAsset damageEffect;
        [SerializeField] private float range = 2f;

        public GameplayEffectAsset DamageEffect => damageEffect;
        public float Range => range;
    }
}
