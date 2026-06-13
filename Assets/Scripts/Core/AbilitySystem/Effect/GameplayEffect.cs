using System.Collections.Generic;
using UnityEngine;


namespace Core.AbilitySystem.Effect
{
    [CreateAssetMenu(menuName = "Ability System/Gameplay Effect", fileName = "GE_New")]
    public sealed class GameplayEffect : ScriptableObject
    {
        [SerializeField] private GameplayEffectType type;
        [Min(0f)] [SerializeField] private float duration;
        [Min(0f)] [SerializeField] private float period;

        [Tooltip("true면 적용 즉시 1회 실행 후 주기마다 실행. false면 첫 주기 이후부터 실행.")]
        [SerializeField] private bool executePeriodicEffectOnApplication = true;

        [SerializeField] private List<GameplayModifier> modifiers;
        [SerializeField] private List<GameplayEffectExecution> executions;

        // TODO GE 스택 구현

        // TODO Gameplay Cue

        public GameplayEffectType Type => type;
        public float Duration => duration;
        public float Period => period;
        public bool ExecutePeriodicEffectOnApplication => executePeriodicEffectOnApplication;
        public IReadOnlyList<GameplayModifier> Modifiers => modifiers;
        public IReadOnlyList<GameplayEffectExecution> Executions => executions;
    }
}
