using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using UnityEngine;

// 트리거에 들어온 액터의 ASC에 GE를 적용한다.
// GE 검증용 최소 픽업 — 물리 시뮬레이션 없이 트리거 감지로만 동작한다.
[RequireComponent(typeof(Collider))]
public class GameplayEffectPickup : MonoBehaviour
{
    [SerializeField] private GameplayEffectAsset effect;
    [SerializeField] private float level = 1f;

    // 끄면 반복 적용 테스트용으로 남는다(트리거를 다시 밟을 때마다 적용).
    [SerializeField] private bool destroyOnPickup = true;

    // 같은 프레임에 같은 액터가 중복 적용되는 것을 막는다.
    // OnTriggerEnter는 **콜라이더 단위**로 오므로, 한 액터에 콜라이더가 여러 개면
    // (예: Player 루트의 CapsuleCollider + CharacterController) 콜백이 그 수만큼 온다.
    // Destroy는 프레임 끝까지 지연되므로 destroyOnPickup만으로는 막히지 않는다.
    private AbilitySystemComponent _lastTarget;
    private int _lastFrame = -1;

    private void Reset()
    {
        // 픽업은 트리거로 동작한다(에디터에서 컴포넌트 추가 시 자동 설정).
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (effect == null)
        {
            return;
        }

        // 콜라이더가 자식에 달린 구성을 허용한다(ItemPickup과 동일).
        AbilitySystemComponent asc = other.GetComponentInParent<AbilitySystemComponent>();
        if (asc == null)
        {
            return;
        }

        if (asc == _lastTarget && Time.frameCount == _lastFrame)
        {
            return;
        }

        _lastTarget = asc;
        _lastFrame = Time.frameCount;

        asc.ApplyGameplayEffectToSelf(effect, default, level);

        Debug.Log($"[GEPickup] '{effect.name}' → '{asc.name}' 적용");

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
