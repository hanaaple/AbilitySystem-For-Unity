using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Character
{
    /// <summary>
    /// 캐릭터가 태그를 가진 동안 화면 전체 후처리(Volume)를 켠다. GE가 부여한 상태를 화면으로 보여 준다.
    /// 같은 후처리 항목(예: Vignette)을 둘이 덮어쓰면 Volume끼리 섞여 한 효과처럼 보이므로, 항목마다 한 효과만 쓰게 하고 겹치면 경고한다. 여러 상태를 겹쳐 보여야 하면 <see cref="TagScreenOverlay"/>를 쓴다.
    /// </summary>
    public class TagScreenEffect : TagFadeEffect
    {
        // 후처리 항목 종류 → 그 항목을 덮어쓰는 효과
        private static readonly Dictionary<Type, TagScreenEffect> _ownersByComponent = new();

        [Tooltip("weight 0에서 시작하는 전역 Volume.")]
        [SerializeField] private Volume volume;

        protected override void Start()
        {
            base.Start();
            ClaimComponents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (VolumeComponent component in GetOverriddenComponents())
            {
                if (_ownersByComponent.TryGetValue(component.GetType(), out TagScreenEffect owner) && owner == this)
                {
                    _ownersByComponent.Remove(component.GetType());
                }
            }
        }

        protected override void ApplyWeight(float weight)
        {
            volume.weight = weight;
        }

        private void ClaimComponents()
        {
            foreach (VolumeComponent component in GetOverriddenComponents())
            {
                Type type = component.GetType();
                if (_ownersByComponent.TryGetValue(type, out TagScreenEffect owner) && owner != null && owner != this)
                {
                    Debug.LogWarning($"TagScreenEffect: '{name}'과 '{owner.name}'이 같은 후처리 항목 {type.Name}을 덮어써 동시에 켜지면 섞입니다. 항목을 나누거나 TagScreenOverlay를 쓰세요.", this);
                    continue;
                }

                _ownersByComponent[type] = this;
            }
        }

        private IEnumerable<VolumeComponent> GetOverriddenComponents()
        {
            if (volume == null || volume.sharedProfile == null)
            {
                yield break;
            }

            foreach (VolumeComponent component in volume.sharedProfile.components)
            {
                if (component.active && component.AnyPropertiesIsOverridden())
                {
                    yield return component;
                }
            }
        }
    }
}
