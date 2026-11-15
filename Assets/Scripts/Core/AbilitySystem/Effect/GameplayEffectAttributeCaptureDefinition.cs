using System;
using Core.AbilitySystem.Attribute;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// "무엇을·어디서 캡처할지"를 정의하는 불변 식별자 — AttributeBased magnitude와 Execution이 공유하는 캡처 대상 명세이며, 값은 담지 않는다(실제 값은 이 정의를 키로 캡처 컨테이너가 보관).
    /// 소스는 <see cref="AttributeCaptureSource"/>를 재사용하고, 대상 어트리뷰트는 직렬화 가능한 <see cref="GameplayAttribute"/>로 담아 <see cref="ToResolvedAttribute"/>로 변환한다 — modifier 대상과 같은 타입이라 전용 드로어도 공유된다.
    /// </summary>
    [Serializable]
    public struct GameplayEffectAttributeCaptureDefinition : IEquatable<GameplayEffectAttributeCaptureDefinition>
    {
        [SerializeField] [FormerlySerializedAs("captureSource")] private AttributeCaptureSource _captureSource;
        [SerializeField] [FormerlySerializedAs("attribute")] private GameplayAttribute _attribute;

        // 스냅샷: true면 Spec 생성 시점의 값을 한 번 캡처해 고정(예: 시전 순간의 공격력).
        // false면 캡처가 필요한 시점(적용/Execution 실행)에 재조회(예: 매 틱 현재 방어력).
        [SerializeField] [FormerlySerializedAs("snapshot")] private bool _snapshot;

        public AttributeCaptureSource CaptureSource => _captureSource;
        public GameplayAttribute Attribute => _attribute;
        public bool Snapshot => _snapshot;

        public GameplayEffectAttributeCaptureDefinition(GameplayAttribute attribute, AttributeCaptureSource captureSource, bool snapshot)
        {
            _attribute = attribute;
            _captureSource = captureSource;
            _snapshot = snapshot;
        }

        public GameplayEffectAttributeCaptureDefinition(GameplayAttributeHandle gameplayAttributeHandle, AttributeCaptureSource captureSource, bool snapshot)
        {
            _attribute = new GameplayAttribute(gameplayAttributeHandle);
            _captureSource = captureSource;
            _snapshot = snapshot;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayEffectAttributeCaptureDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_captureSource, _attribute, _snapshot);
        }

        public override string ToString()
        {
            return $"{_captureSource}:{_attribute}{(_snapshot ? " (snapshot)" : "")}";
        }

        /// <summary>캡처 대상 핸들로 해석한다. 해석 실패 시 default(무효).</summary>
        public GameplayAttributeHandle ToResolvedAttribute()
        {
            return _attribute.ToAttributeHandle();
        }

        // 캡처 컨테이너에서 "같은 대상을 두 번 캡처하지 않게" 하는 키로 쓰이므로 값 동등성을 정의한다.
        public bool Equals(GameplayEffectAttributeCaptureDefinition other)
        {
            return _captureSource == other._captureSource && _attribute.Equals(other._attribute) && _snapshot == other._snapshot;
        }
    }
}
