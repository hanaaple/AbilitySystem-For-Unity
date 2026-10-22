using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Game.Character
{
    /// <summary> 플레이어만 쓰는 어트리뷰트(점프·구르기·마나). 허수아비처럼 이 동작이 없는 캐릭터에는 붙이지 않는다. </summary>
    public class PlayerAttributeSet : AttributeSet
    {
        public static readonly GameplayAttributeHandle JumpHeight   = new(typeof(PlayerAttributeSet), nameof(jumpHeight));
        public static readonly GameplayAttributeHandle DashDistance = new(typeof(PlayerAttributeSet), nameof(dashDistance));
        public static readonly GameplayAttributeHandle Mana         = new(typeof(PlayerAttributeSet), nameof(mana));
        public static readonly GameplayAttributeHandle MaxMana      = new(typeof(PlayerAttributeSet), nameof(maxMana));

        /// <summary> 점프 높이(m). </summary>
        public AttributeData jumpHeight;

        /// <summary> 구르기 한 번에 나아가는 거리(m). </summary>
        public AttributeData dashDistance;

        /// <summary> 스킬 코스트로 쓰는 자원. 코스트 GE가 깎고, 재생 GE·아이템이 채운다. </summary>
        public AttributeData mana;
        public AttributeData maxMana;

        // Instant·주기 GE는 Base를, Duration/Infinite mod는 Current만 바꾸므로 두 경로 모두 막는다.
        public override void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            ClampNonNegative(attributeHandle, ref newValue);
            ClampMana(attributeHandle, ref newValue);
        }

        public override void PreAttributeChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            ClampNonNegative(attributeHandle, ref newValue);
            ClampMana(attributeHandle, ref newValue);
        }

        // 감속처럼 값을 줄이는 GE가 겹쳐도 거꾸로 뛰거나 구르지 않게 0 아래를 막는다.
        private void ClampNonNegative(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != JumpHeight && attributeHandle != DashDistance)
            {
                return;
            }

            newValue = Mathf.Max(newValue, 0f);
        }

        private void ClampMana(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
            if (attributeHandle != Mana)
            {
                return;
            }

            // 최대 마나를 초기화하지 않은 데이터(0)에선 상한을 걸지 않는다 — 걸면 마나가 0으로 눌린다.
            float max = maxMana.GetCurrentValue();
            if (max > 0f)
            {
                newValue = Mathf.Min(newValue, max);
            }

            newValue = Mathf.Max(newValue, 0f);
        }
    }
}
