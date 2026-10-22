using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Game.Character
{
    public class CharacterAttributeSet : AttributeSet
    {
        // 외부에서 어트리뷰트를 식별하는 정적 핸들
        public static readonly GameplayAttributeHandle Health    = new(typeof(CharacterAttributeSet), nameof(health));
        public static readonly GameplayAttributeHandle MaxHealth = new(typeof(CharacterAttributeSet), nameof(maxHealth));
        public static readonly GameplayAttributeHandle Stamina   = new(typeof(CharacterAttributeSet), nameof(stamina));
        public static readonly GameplayAttributeHandle MaxStamina = new(typeof(CharacterAttributeSet), nameof(maxStamina));
        public static readonly GameplayAttributeHandle Speed     = new(typeof(CharacterAttributeSet), nameof(speed));
        public static readonly GameplayAttributeHandle Attack    = new(typeof(CharacterAttributeSet), nameof(attack));
        public static readonly GameplayAttributeHandle CooldownReduction = new(typeof(CharacterAttributeSet), nameof(cooldownReduction));
        public static readonly GameplayAttributeHandle Shield    = new(typeof(CharacterAttributeSet), nameof(shield));
        public static readonly GameplayAttributeHandle AttackSpeed = new(typeof(CharacterAttributeSet), nameof(attackSpeed));
        public static readonly GameplayAttributeHandle JumpHeight = new(typeof(CharacterAttributeSet), nameof(jumpHeight));
        public static readonly GameplayAttributeHandle DashDistance = new(typeof(CharacterAttributeSet), nameof(dashDistance));
        public static readonly GameplayAttributeHandle Mana      = new(typeof(CharacterAttributeSet), nameof(mana));
        public static readonly GameplayAttributeHandle MaxMana   = new(typeof(CharacterAttributeSet), nameof(maxMana));

        // 공격속도가 0에 가까우면 쿨다운이 끝없이 길어지므로 하한을 둔다.
        private const float MinAttackSpeed = 0.1f;


        // 연습 모드엔 죽음이 없다 — 체력은 이 값 아래로 내려가지 않는다.
        private const float MinHealth = 1f;

        // 쿨다운이 0이 되면 스킬을 매 프레임 쓸 수 있으므로 감소 비율에 상한을 둔다.
        private const float MaxCooldownReduction = 0.8f;

        public AttributeData health;
        public AttributeData maxHealth;
        public AttributeData stamina;
        public AttributeData maxStamina;
        /// <summary> 이동 속도(m/s). 달리기·조준 등은 이 값을 바꾸는 GE로 나타난다. </summary>
        public AttributeData speed;
        public AttributeData attack;

        /// <summary> 쿨다운 GE 지속 시간을 줄이는 비율(0.3 → 30% 짧게). </summary>
        public AttributeData cooldownReduction;

        /// <summary> 체력보다 먼저 피해를 받아 내는 양. 피해 계산(DamageExecution)이 이것부터 깎는다. </summary>
        public AttributeData shield;

        /// <summary> 공격 속도 배율(1 = 기본). 공격속도를 따르는 어빌리티는 쿨다운을 이 값으로 나눈다. </summary>
        public AttributeData attackSpeed;

        /// <summary> 점프 높이(m). </summary>
        public AttributeData jumpHeight;

        /// <summary> 구르기 한 번에 나아가는 거리(m). </summary>
        public AttributeData dashDistance;

        /// <summary> 스킬 코스트로 쓰는 자원. 코스트 GE가 깎고, 재생 GE·아이템이 채운다. </summary>
        public AttributeData mana;
        public AttributeData maxMana;

        // Instant·주기 데미지는 Base를, Duration/Infinite mod는 Current만 바꾸므로 두 경로 모두 막는다.
        public override void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            ClampHealth(attributeHandle, ref newValue);
            ClampNonNegative(attributeHandle, ref newValue);
            ClampCooldownReduction(attributeHandle, ref newValue);
            ClampShield(attributeHandle, ref newValue);
            ClampAttackSpeed(attributeHandle, ref newValue);
            ClampMana(attributeHandle, ref newValue);
        }

        public override void PreAttributeChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            ClampHealth(attributeHandle, ref newValue);
            ClampNonNegative(attributeHandle, ref newValue);
            ClampCooldownReduction(attributeHandle, ref newValue);
            ClampShield(attributeHandle, ref newValue);
            ClampAttackSpeed(attributeHandle, ref newValue);
            ClampMana(attributeHandle, ref newValue);
        }

        private void ClampHealth(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != Health)
            {
                return;
            }

            // maxHealth를 초기화하지 않은 데이터(0)에선 상한을 걸지 않는다 — 걸면 체력이 0으로 눌린다.
            float max = maxHealth.GetCurrentValue();
            if (max > 0f)
            {
                newValue = Mathf.Min(newValue, max);
            }

            newValue = Mathf.Max(newValue, MinHealth);
        }

        // 감속이 겹쳐도 뒤로 걷지 않게(점프·구르기도 같은 이유로) 0 아래를 막는다.
        private void ClampNonNegative(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != Speed && attributeHandle != JumpHeight && attributeHandle != DashDistance)
            {
                return;
            }

            newValue = Mathf.Max(newValue, 0f);
        }

        private void ClampCooldownReduction(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != CooldownReduction)
            {
                return;
            }

            newValue = Mathf.Clamp(newValue, 0f, MaxCooldownReduction);
        }

        private void ClampShield(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != Shield)
            {
                return;
            }

            newValue = Mathf.Max(newValue, 0f);
        }

        private void ClampAttackSpeed(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != AttackSpeed)
            {
                return;
            }

            newValue = Mathf.Max(newValue, MinAttackSpeed);
        }

        private void ClampMana(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != Mana)
            {
                return;
            }

            // 체력과 같은 이유로 최대 마나를 초기화하지 않은 데이터(0)에선 상한을 걸지 않는다.
            float max = maxMana.GetCurrentValue();
            if (max > 0f)
            {
                newValue = Mathf.Min(newValue, max);
            }

            newValue = Mathf.Max(newValue, 0f);
        }
    }
}
