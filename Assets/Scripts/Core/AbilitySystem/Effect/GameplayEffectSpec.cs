using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// GameplayEffectAsset(에셋)의 런타임 인스턴스. 생성 시점에 각 Modifier를 GameplayModifierSpec으로 변환해 캐싱한다.
    /// </summary>
    public sealed class GameplayEffectSpec
    {
        public GameplayEffectAsset Definition { get; }
        public GameplayEffectContextHandle Context { get; }
        public float Level { get; }

        // 정의(Definition.Modifiers)와 평행한 슬롯 배열(Modifiers[i] ↔ Definition.Modifiers[i]).
        // 배열로 노출 — 라이브 재평가 시 슬롯 magnitude를 제자리 갱신해야 하는데 List·IReadOnlyList 인덱서는 값 타입 복사본을 돌려줘 안 먹는다. 배열 교체는 private set으로 막아 평행 인덱스를 지킨다.
        public GameplayModifierSpec[] Modifiers { get; private set; }

        /// <summary>정의에 적힌 AQN을 resolve해 만든 Execution 인스턴스들. 실패한 항목은 경고 후 제외.</summary>
        public IReadOnlyList<GameplayEffectExecution> Executions { get; private set; }

        /// <summary>이 GE 계산(AttributeBased·Execution)이 참조하는 캡처 어트리뷰트 묶음. 생성 시 Source, 적용 시 Target을 캡처.</summary>
        public GameplayEffectAttributeCaptureSpecContainer CapturedRelevantAttributes { get; private set; }

        public GameplayEffectSpec(GameplayEffectAsset definition, GameplayEffectContextHandle context = default, float level = 1f)
        {
            Definition = definition;
            Context = context;
            Level = level;

            Initialize();
        }

        /// <summary>
        /// 적용 시점에 대상별로 복제하는 복사 생성자 — <see cref="Initialize"/>를 재실행하지 않고(Source 재캡처·execution 재resolve 없음) 상태만 옮긴다.
        /// 복사 심도: Definition·Executions는 공유(불변/무상태), Context·Level은 값 복사, Modifiers는 배열 값 복사(struct 슬롯이라 독립),
        /// CapturedRelevantAttributes는 깊은 복사 — 대상별 target 캡처가 서로 덮어쓰지 않게.
        /// </summary>
        private GameplayEffectSpec(GameplayEffectSpec other)
        {
            Definition = other.Definition;
            Context = other.Context;
            Level = other.Level;

            // struct 슬롯 배열이라 Clone()=값 복사=독립. magnitude는 target 캡처 후 CalculateModifierMagnitudes가 다시 채운다.
            Modifiers = (GameplayModifierSpec[])other.Modifiers.Clone();
            Executions = other.Executions;
            CapturedRelevantAttributes = other.CapturedRelevantAttributes.Clone();
        }

        /// <summary>적용 시점에 대상별로 복제해 target 캡처가 섞이지 않게 하는 독립 복사본. <see cref="CaptureAttributeDataFromTarget"/> 전에 호출.</summary>
        public GameplayEffectSpec Clone()
        {
            return new GameplayEffectSpec(this);
        }

        /// <summary>멤버 세팅 후 초기화 — Modifier·Execution 변환과 어트리뷰트 캡처. 생성자가 확정한 Definition·Context·Level에 의존.</summary>
        private void Initialize()
        {
            Executions = ResolveExecutions(Definition);
            BuildModifierSpecs();

            // 캡처: 필요한 캡처 정의를 컨테이너에 모으고 Source를 즉시 캡처. Target은 아직 미정이라 적용 시점에 캡처한다.
            CapturedRelevantAttributes = new GameplayEffectAttributeCaptureSpecContainer();
            SetupAttributeCaptureDefinitions();
            CaptureDataFromSource();

            // magnitude 계산은 캡처 이후 — AttributeBased가 Source 캡처값을 읽기 때문. Target 필요분은 적용 시점에 재계산.
            CalculateModifierMagnitudes();
        }

        /// <summary>
        /// 정의와 같은 개수·인덱스로 빈 magnitude 슬롯 배열을 준비하고, 대상 어트리뷰트가 무효인 정의는 경고한다.
        /// 무효도 빼지 않는다 — 빼면 인덱스가 밀려 정의 대응이 깨진다. 값은 <see cref="CalculateModifierMagnitudes"/>가 채운다.
        /// </summary>
        private void BuildModifierSpecs()
        {
            IReadOnlyList<GameplayModifier> defs = Definition.Modifiers;
            Modifiers = new GameplayModifierSpec[defs.Count];
            for (int i = 0; i < defs.Count; i++)
            {
                if (!defs[i].ToResolvedAttribute().IsValid)
                {
                    Debug.LogWarning(
                        $"[GESpec] ResolvedGameplayAttribute 해석 실패 — '{Definition.name}' 의 {i}번 modifier는 적용 시 반영되지 않는다.");
                }
            }
        }

        /// <summary>
        /// 각 슬롯 magnitude를 현재 캡처 기준으로 제자리 재계산한다. AttributeBased가 캡처값에 의존하므로
        /// 캡처가 갱신될 때마다(생성 시 Source, 적용 시 Target 후) 다시 부른다. 슬롯 배열은 <see cref="BuildModifierSpecs"/>가 준비.
        /// </summary>
        private void CalculateModifierMagnitudes()
        {
            for (int i = 0; i < Modifiers.Length; i++)
            {
                if (Definition.Modifiers[i].AttemptCalculateMagnitude(this, out float evaluatedMagnitude))
                {
                    Modifiers[i].EvaluatedMagnitude = evaluatedMagnitude;
                }
                else
                {
                    Modifiers[i].EvaluatedMagnitude = 0;
                    Debug.LogWarning($"[GESpec] '{Definition.name}'의 {i}번 modifier magnitude Calculation Failed - falling back to 0.");
                }
            }
        }

        /// <summary>
        /// 이 GE 계산이 필요로 하는 캡처 정의를 컨테이너에 등록한다 — ① AttributeBased modifier의 backing 캡처,
        /// ② 각 Execution이 선언한 캡처(<see cref="GameplayEffectExecution.Defs"/>).
        /// </summary>
        private void SetupAttributeCaptureDefinitions()
        {
            // ① AttributeBased magnitude를 쓰는 modifier의 backing 캡처 정의.
            foreach (GameplayModifier modifier in Definition.Modifiers)
            {
                if (modifier.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased)
                {
                    CapturedRelevantAttributes.AddCaptureDefinition(modifier.AttributeBased.BackingAttribute);
                }
            }

            // ② 각 Execution이 선언한 캡처 정의(GameplayEffectExecution.Defs).
            foreach (GameplayEffectExecution execution in Executions)
            {
                foreach (GameplayEffectAttributeCaptureDefinition definition in execution.Defs())
                {
                    CapturedRelevantAttributes.AddCaptureDefinition(definition);
                }
            }
        }

        /// <summary>Source(발동 주체) 어트리뷰트를 spec 생성 시 1회 캡처. Source ASC는 Context.Instigator — 없으면 캡처가 무효 표시된다.</summary>
        private void CaptureDataFromSource()
        {
            CapturedRelevantAttributes.CaptureAttributes(Context.GetInstigator(), AttributeCaptureSource.Source);
        }

        /// <summary>
        /// 적용 시점에 Target 어트리뷰트를 대상 ASC로 캡처한다. 호출부가 대상별 <see cref="Clone"/> 복사본에 부르므로 섞이지 않는다.
        /// 캡처 직후 <see cref="CalculateModifierMagnitudes"/>로 재계산 — AttributeBased modifier가 최신 대상 값을 반영하게.
        /// </summary>
        public void CaptureAttributeDataFromTarget(AbilitySystemComponent target)
        {
            CapturedRelevantAttributes.CaptureAttributes(target, AttributeCaptureSource.Target);
            CalculateModifierMagnitudes();
        }

        /// <summary>AQN 목록을 Execution 인스턴스 목록으로 변환한다. resolve 실패는 경고 후 건너뛴다.</summary>
        private static IReadOnlyList<GameplayEffectExecution> ResolveExecutions(GameplayEffectAsset definition)
        {
            IReadOnlyList<string> typeNames = definition.ExecutionTypeNames;
            if (typeNames == null || typeNames.Count == 0)
            {
                return Array.Empty<GameplayEffectExecution>();
            }

            var executions = new List<GameplayEffectExecution>(typeNames.Count);
            foreach (string typeName in typeNames)
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    continue;
                }

                Type type = Type.GetType(typeName);
                if (type == null || !typeof(GameplayEffectExecution).IsAssignableFrom(type) || type.IsAbstract)
                {
                    Debug.LogWarning($"[GESpec] Execution 타입 해석 실패 — '{definition.name}' 의 '{typeName}'을 건너뜀.");
                    continue;
                }

                executions.Add((GameplayEffectExecution)Activator.CreateInstance(type));
            }

            return executions;
        }

        public bool HasValidCapturedAttributes(IReadOnlyList<GameplayEffectAttributeCaptureDefinition> reqCaptureDefs)
        {
            return CapturedRelevantAttributes.HasValidCapturedAttributes(reqCaptureDefs);
        }
    }
}
