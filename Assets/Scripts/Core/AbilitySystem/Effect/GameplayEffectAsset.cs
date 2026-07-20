using System.Collections.Generic;
using Core.Common;
using UnityEngine;


namespace Core.AbilitySystem.Effect
{
    [CreateAssetMenu(menuName = "Ability System/Gameplay Effect", fileName = "GE_New")]
    public sealed class GameplayEffectAsset : ScriptableObject
    {
        [SerializeField] private GameplayEffectType type;
        [Min(0f)] [SerializeField] private float duration;
        [Min(0f)] [SerializeField] private float period;

        [Tooltip("true면 적용 즉시 1회 실행 후 주기마다 실행. false면 첫 주기 이후부터 실행.")]
        [SerializeField] private bool executePeriodicEffectOnApplication = true;

        [SerializeField] private List<GameplayModifier> modifiers;

        // Execution은 SO가 아니라 클래스라(→decisions D7) 타입 이름(AQN)만 저장한다.
        // 실제 인스턴스화는 GameplayEffectSpec 생성 시 1회 수행(→D2 resolve 캐싱과 같은 방식).
        [SerializeField] [SubclassSelector(typeof(GameplayEffectExecution))]
        private List<string> executionTypeNames;

        // TODO GE 스택 구현

        // TODO Gameplay Cue

        public GameplayEffectType Type => type;
        public float Duration => duration;
        public float Period => period;
        public bool ExecutePeriodicEffectOnApplication => executePeriodicEffectOnApplication;
        public IReadOnlyList<GameplayModifier> Modifiers => modifiers;
        public IReadOnlyList<string> ExecutionTypeNames => executionTypeNames;
    }
}
