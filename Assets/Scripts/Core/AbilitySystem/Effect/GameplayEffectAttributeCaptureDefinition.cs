using System;
using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// "무엇을·어디서 캡처할지"를 정의하는 불변 식별자.
    /// AttributeBased magnitude와 Execution이 공유하는 캡처 대상 명세이며, 값 자체는 담지 않는다
    /// — 실제 캡처 값은 이 정의를 키로 캡처 컨테이너가 보관한다.
    ///
    /// <para>캡처 소스(Source/Target)는 <see cref="AttributeCaptureSource"/>를 재사용한다.
    /// 캡처 대상 어트리뷰트는 직렬화 가능한 <see cref="GameplayAttribute"/>(AQN 문자열 + 필드명)로 담고
    /// <see cref="ToResolvedAttribute"/>로 런타임 변환한다 — modifier 대상과 같은 타입을 공유하므로
    /// 전용 드로어(Set/Attribute 팝업)도 자동으로 공유된다.</para>
    /// </summary>
    [Serializable]
    public struct GameplayEffectAttributeCaptureDefinition : IEquatable<GameplayEffectAttributeCaptureDefinition>
    {
        [SerializeField] private AttributeCaptureSource captureSource;
        [SerializeField] private GameplayAttribute attribute;

        // 스냅샷: true면 Spec 생성 시점의 값을 한 번 캡처해 고정(예: 시전 순간의 공격력).
        // false면 캡처가 필요한 시점(적용/Execution 실행)에 재조회(예: 매 틱 현재 방어력).
        [SerializeField] private bool snapshot;

        public AttributeCaptureSource CaptureSource => captureSource;
        public GameplayAttribute Attribute => attribute;
        public bool Snapshot => snapshot;

        public GameplayEffectAttributeCaptureDefinition(GameplayAttribute attribute, AttributeCaptureSource captureSource, bool snapshot)
        {
            this.attribute = attribute;
            this.captureSource = captureSource;
            this.snapshot = snapshot;
        }
        public GameplayEffectAttributeCaptureDefinition(GameplayAttributeHandle gameplayAttributeHandle, AttributeCaptureSource captureSource, bool snapshot)
        {
            this.attribute = new GameplayAttribute(gameplayAttributeHandle);
            this.captureSource = captureSource;
            this.snapshot = snapshot;
        }

        /// <summary>캡처 대상 핸들로 해석한다. 해석 실패 시 default(무효).</summary>
        public GameplayAttributeHandle ToResolvedAttribute()
        {
            return attribute.ToAttributeHandle();
        }

        // 캡처 컨테이너에서 "같은 대상을 두 번 캡처하지 않게" 하는 키로 쓰이므로 값 동등성을 정의한다.
        public bool Equals(GameplayEffectAttributeCaptureDefinition other)
        {
            return captureSource == other.captureSource && attribute.Equals(other.attribute) && snapshot == other.snapshot;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayEffectAttributeCaptureDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(captureSource, attribute, snapshot);
        }

        public override string ToString()
        {
            return $"{captureSource}:{attribute}{(snapshot ? " (snapshot)" : "")}";
        }
    }
}
