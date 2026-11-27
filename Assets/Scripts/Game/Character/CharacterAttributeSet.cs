using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Game.Character
{
    public class CharacterAttributeSet : AttributeSet
    {
        // 외부에서 어트리뷰트를 식별하는 정적 핸들
        public static readonly GameplayAttributeHandle HealthAttribute    = new(typeof(CharacterAttributeSet), nameof(Health));
        public static readonly GameplayAttributeHandle MaxHealthAttribute = new(typeof(CharacterAttributeSet), nameof(MaxHealth));
        public static readonly GameplayAttributeHandle StaminaAttribute   = new(typeof(CharacterAttributeSet), nameof(Stamina));
        public static readonly GameplayAttributeHandle MaxStaminaAttribute = new(typeof(CharacterAttributeSet), nameof(MaxStamina));
        public static readonly GameplayAttributeHandle SpeedAttribute     = new(typeof(CharacterAttributeSet), nameof(Speed));
        public static readonly GameplayAttributeHandle AttackAttribute    = new(typeof(CharacterAttributeSet), nameof(Attack));
        public static readonly GameplayAttributeHandle ShieldAttribute    = new(typeof(CharacterAttributeSet), nameof(Shield));
        public static readonly GameplayAttributeHandle AttackSpeedAttribute = new(typeof(CharacterAttributeSet), nameof(AttackSpeed));

        // 공격속도가 0에 가까우면 쿨다운이 끝없이 길어지므로 하한을 둔다.
        private const float MinAttackSpeed = 0.1f;


        // 연습 모드엔 죽음이 없다 — 체력은 이 값 아래로 내려가지 않는다.
        private const float MinHealth = 1f;

        public AttributeData Health;
        public AttributeData MaxHealth;
        public AttributeData Stamina;
        public AttributeData MaxStamina;
        /// <summary> 이동 속도(m/s). 달리기·조준 등은 이 값을 바꾸는 GE로 나타난다. </summary>
        public AttributeData Speed;
        public AttributeData Attack;

        /// <summary> 체력보다 먼저 피해를 받아 내는 양. 피해 계산(DamageExecution)이 이것부터 깎는다. </summary>
        public AttributeData Shield;

        /// <summary> 공격 속도 배율(1 = 기본). 공격속도를 따르는 어빌리티는 쿨다운을 이 값으로 나눈다. </summary>
        public AttributeData AttackSpeed;

        // Instant·주기 데미지는 Base를, Duration/Infinite mod는 Current만 바꾸므로 두 경로 모두 막는다.
        public override void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            ClampHealth(attributeHandle, ref newValue);
            ClampNonNegative(attributeHandle, ref newValue);
            ClampShield(attributeHandle, ref newValue);
            ClampAttackSpeed(attributeHandle, ref newValue);
        }

        public override void PreAttributeChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            ClampHealth(attributeHandle, ref newValue);
            ClampNonNegative(attributeHandle, ref newValue);
            ClampShield(attributeHandle, ref newValue);
            ClampAttackSpeed(attributeHandle, ref newValue);
        }

        private void ClampHealth(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != HealthAttribute)
            {
                return;
            }

            // maxHealth를 초기화하지 않은 데이터(0)에선 상한을 걸지 않는다 — 걸면 체력이 0으로 눌린다.
            float max = MaxHealth.GetCurrentValue();
            if (max > 0f)
            {
                newValue = Mathf.Min(newValue, max);
            }

            newValue = Mathf.Max(newValue, MinHealth);
        }

        // 감속이 겹쳐도 뒤로 걷지 않게 0 아래를 막는다.
        private void ClampNonNegative(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != SpeedAttribute)
            {
                return;
            }

            newValue = Mathf.Max(newValue, 0f);
        }

        private void ClampShield(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != ShieldAttribute)
            {
                return;
            }

            newValue = Mathf.Max(newValue, 0f);
        }

        private void ClampAttackSpeed(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != AttackSpeedAttribute)
            {
                return;
            }

            newValue = Mathf.Max(newValue, MinAttackSpeed);
        }
    }
}
