using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// 트리거 방(Room) 디버그 도구. 방에 들어온 액터에게 <b>Room 자신의 ASC를 source(instigator)로</b> GE를 적용하고,
    /// 방을 나가면 적용했던 GE를 해제한다. Source=Room / Target=들어온 액터 구도라 AttributeBased 캡처(Source/Target)·
    /// snapshot·주기 실행을 씬에서 손쉽게 재현·관찰하기 위한 용도다(픽업 트리거로는 세팅이 번거로운 상황들).
    ///
    /// <para>두 용도의 GE를 꽂는다 — <see cref="otherEffect"/>는 들어온 액터(Other)에게, <see cref="selfEffect"/>는 Room 자신(Self)에게.
    /// 둘 다 진입 시 적용하고 이탈 시 해제한다(동일하게 껐다 켠다). GE의 성격(Instant/Buff/Tick)은 에셋 타입이 결정하고,
    /// Instant는 적용 즉시 실행 후 Invalid 핸들을 돌려주므로 해제 대상이 아니다.</para>
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AbilitySystemComponent))]
    public sealed class BoxRoom : MonoBehaviour
    {
        [Tooltip("들어온 액터(Other=Target)에게 적용할 GE.")]
        [SerializeField] private GameplayEffectAsset otherEffect;

        [Tooltip("Room 자신(Self=Source·Target)에게 적용할 GE — 자기 어트리뷰트 변화 테스트용.")]
        [SerializeField] private GameplayEffectAsset selfEffect;

        [Tooltip("Room ASC(=Source)에 초기화할 어트리뷰트. AttributeBased의 Source 캡처가 여기서 값을 읽는다.")]
        [SerializeField] private AttributeDefinitionAsset sourceAttributes;

        [SerializeField] private float level = 1f;

        // Room 자신의 ASC — 적용하는 GE의 source(instigator)가 된다(→ Source 캡처 대상). Self GE의 적용 대상이기도 하다.
        private AbilitySystemComponent _roomAsc;

        // 방 안에 있는 대상별로 적용해 둔 핸들(Other·Self). 나갈 때 이 핸들로 해제한다.
        // Instant는 적용 후 Invalid 핸들이라 담기더라도 해제가 no-op이다(RemoveActiveGameplayEffect가 IsValid 체크).
        private readonly Dictionary<AbilitySystemComponent, AppliedHandles> _appliedByTarget = new();

        private readonly struct AppliedHandles
        {
            public readonly ActiveGameplayEffectHandle Other;
            public readonly ActiveGameplayEffectHandle Self;

            public AppliedHandles(ActiveGameplayEffectHandle other, ActiveGameplayEffectHandle self)
            {
                Other = other;
                Self = self;
            }
        }

        private void Awake()
        {
            _roomAsc = GetComponent<AbilitySystemComponent>();

            // Room ASC를 Source로 캡처하려면 이 ASC에 어트리뷰트가 초기화돼 있어야 한다.
            // TryAdd라 ASC 자체 attributeInitData와 중복돼도 안전(먼저 등록된 쪽 유지).
            _roomAsc.AddSet(sourceAttributes);

            _roomAsc.TryGetAttributeData(CharacterAttributeSet.Health, out AttributeData health);
        }

        // 에디터에서 컴포넌트 추가 시 트리거로 자동 설정(방 영역은 트리거 콜라이더).
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 콜라이더가 자식에 달린 구성을 허용한다(GameplayEffectPickup과 동일).
            AbilitySystemComponent target = other.GetComponentInParent<AbilitySystemComponent>();
            if (target == null || target == _roomAsc)
            {
                return;
            }

            // 이미 방 안(중복 콜라이더로 인한 재진입 콜백 포함) — 중복 적용을 막는다.
            if (_appliedByTarget.ContainsKey(target))
            {
                return;
            }

            if (otherEffect == null && selfEffect == null)
            {
                Debug.LogWarning($"[BoxRoom] 꽂힌 GE가 없다(Other·Self 모두 비어 있음) — 인스펙터에서 지정해라.");
                return;
            }

            // Other: 들어온 액터에게. context=default → Room(_roomAsc)을 instigator로 하는 context 생성(= Source 캡처 대상).
            ActiveGameplayEffectHandle otherHandle = otherEffect != null
                ? _roomAsc.ApplyGameplayEffectToTarget(otherEffect, target, default, level)
                : ActiveGameplayEffectHandle.Invalid;

            // Self: Room 자신에게(Source=Target=Room). 자기 어트리뷰트 변화 테스트용.
            ActiveGameplayEffectHandle selfHandle = selfEffect != null
                ? _roomAsc.ApplyGameplayEffectToSelf(selfEffect, default, level)
                : ActiveGameplayEffectHandle.Invalid;

            // 진입을 처리했으면 항상 기록한다 — 재진입 콜백 중복 적용을 막고, 이탈 시 해제 대상을 찾기 위함.
            _appliedByTarget[target] = new AppliedHandles(otherHandle, selfHandle);

            Debug.Log($"[BoxRoom] 진입: {_roomAsc.name} ← {target.name} (Other='{(otherEffect != null ? otherEffect.name : "-")}', Self='{(selfEffect != null ? selfEffect.name : "-")}')");
        }

        private void OnTriggerExit(Collider other)
        {
            AbilitySystemComponent target = other.GetComponentInParent<AbilitySystemComponent>();
            if (target == null)
            {
                return;
            }

            if (!_appliedByTarget.TryGetValue(target, out AppliedHandles handles))
            {
                return;
            }

            // Instant였거나 미적용이면 각 핸들이 Invalid라 해제가 no-op이다.
            target.RemoveActiveGameplayEffect(handles.Other);   // Other는 target에게 적용됐다.
            _roomAsc.RemoveActiveGameplayEffect(handles.Self);  // Self는 Room 자신에게 적용됐다.
            _appliedByTarget.Remove(target);

            Debug.Log($"[BoxRoom] 이탈: {target.name} 나감 → GE 해제");
        }
    }
}
