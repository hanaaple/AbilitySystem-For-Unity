using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// AttributeSet 훅 검증용 Set. <c>damage</c>는 meta attribute — GE가 여기에 데미지를 넣으면 <see cref="PostGameplayEffectExecute"/>가 Health에서 빼고 0으로 되돌린다.
    /// Health CurrentValue는 [0, <see cref="MaxHealth"/>]로 클램프한다.
    /// </summary>
    public class TestAtrributeSet : AttributeSet
    {
        public const float MaxHealth = 100f;

        public static readonly GameplayAttributeHandle Health = new(typeof(TestAtrributeSet), nameof(health));
        public static readonly GameplayAttributeHandle Damage = new(typeof(TestAtrributeSet), nameof(damage));

        public AttributeData health;
        public AttributeData damage;

        /// <summary>true면 <see cref="PreGameplayEffectExecute"/>가 모든 실행을 버린다.</summary>
        public bool RejectExecute { get; set; }

        /// <summary><see cref="PreGameplayEffectExecute"/>가 Damage 수치에 곱하는 배율.</summary>
        public float DamageScale { get; set; } = 1f;

        public int PreExecuteCount { get; private set; }
        public float LastExecutedDamage { get; private set; }
        public float LastChangeOldValue { get; private set; }
        public float LastChangeNewValue { get; private set; }

        public override bool PreGameplayEffectExecute(GameplayEffectModCallbackData data)
        {
            PreExecuteCount++;

            if (RejectExecute)
            {
                return false;
            }

            GameplayModifierEvaluatedData evaluated = data.EvaluatedData;
            if (evaluated.Handle.Equals(Damage))
            {
                data.EvaluatedData = new GameplayModifierEvaluatedData(evaluated.Handle, evaluated.Magnitude * DamageScale, evaluated.Operation);
            }

            return true;
        }

        public override void PostGameplayEffectExecute(GameplayEffectModCallbackData data)
        {
            GameplayModifierEvaluatedData evaluated = data.EvaluatedData;
            if (!evaluated.Handle.Equals(Damage))
            {
                return;
            }

            float damageDone = damage.GetBaseValue();
            LastExecutedDamage = damageDone;

            SetBaseValue(Damage, 0f);
            SetBaseValue(Health, health.GetBaseValue() - damageDone);
        }

        public override void PreAttributeChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle.Equals(Health))
            {
                newValue = Mathf.Clamp(newValue, 0f, MaxHealth);
            }
        }

        public override void PostAttributeChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue)
        {
            if (attributeHandle.Equals(Health))
            {
                LastChangeOldValue = oldValue;
                LastChangeNewValue = newValue;
            }
        }
    }
}
