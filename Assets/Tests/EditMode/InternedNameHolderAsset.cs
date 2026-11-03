using System;
using Core.AbilitySystem.Tag;
using Core.Common;
using UnityEngine;

namespace Core.Tests.EditMode
{
    /// <summary>직렬화 테스트용 에셋. InternedName이 Unity 에셋 저장·재로드를 거쳐도 비교 정본이 복원되는지 보는 데 쓴다.</summary>
    public sealed class InternedNameHolderAsset : ScriptableObject
    {
        [SerializeField] private InternedName first;
        [SerializeField] private InternedName second;
        [SerializeField] private InternedName[] names = Array.Empty<InternedName>();
        [SerializeField] private GameplayTagContainer container = new();

        public InternedName First
        {
            get
            {
                return first;
            }
            set
            {
                first = value;
            }
        }

        public InternedName Second
        {
            get
            {
                return second;
            }
            set
            {
                second = value;
            }
        }

        public InternedName[] Names
        {
            get
            {
                return names;
            }
            set
            {
                names = value;
            }
        }

        public GameplayTagContainer Container
        {
            get
            {
                return container;
            }
            set
            {
                container = value;
            }
        }
    }
}
