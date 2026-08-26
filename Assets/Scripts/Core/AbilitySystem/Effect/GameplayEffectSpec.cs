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

        // 정의(Definition.Modifiers)와 같은 인덱스로 평행한 슬롯 배열.
        // 배열로 노출한다 — 캡처 갱신·라이브 재평가 시 컨테이너가 슬롯 magnitude를 제자리로 갱신해야 하기 때문
        // (값 타입 슬롯이라 배열 원소 대입만 원본에 남고, List·IReadOnlyList 인덱서는 복사본을 돌려줘 안 먹는다).
        // get-only(private set): 원소 내용 변경은 허용하되 배열 자체 교체는 밖에서 막아 평행 인덱스 불변조건(Modifiers[i] ↔ Definition.Modifiers[i])을 지킨다.
        public GameplayModifierSpec[] Modifiers { get; private set; }

        /// <summary>정의에 적힌 AQN을 resolve해 만든 Execution 인스턴스들. 실패한 항목은 경고 후 제외.</summary>
        public IReadOnlyList<GameplayEffectExecution> Executions { get; private set; }

        /// <summary>
        /// 이 GE의 계산(AttributeBased magnitude·Execution)이 참조하는, 캡처된 어트리뷰트 묶음.
        /// 생성 시 Source를 캡처, 적용 시점에 Target을 캡처한다.
        /// </summary>
        public GameplayEffectAttributeCaptureSpecContainer CapturedRelevantAttributes { get; private set; }

        public GameplayEffectSpec(GameplayEffectAsset definition, GameplayEffectContextHandle context = default, float level = 1f)
        {
            Definition = definition;
            Context = context;
            Level = level;

            Initialize();
        }

        /// <summary>
        /// 적용 시점에 spec을 대상별로 복제하기 위한 복사 생성자. <see cref="Initialize"/>를 재실행하지 않고
        /// 이미 만들어진 상태를 옮긴다 — source 재캡처·execution 재resolve를 하지 않는다.
        ///
        /// <para>복사 심도:
        /// <list type="bullet">
        /// <item><see cref="Definition"/> — 공유. 불변 정의라 여러 인스턴스가 나눠 갖는다.</item>
        /// <item><see cref="Context"/> — struct 복사(내부 context는 공유). <see cref="Level"/> — 값 복사.</item>
        /// <item><see cref="Modifiers"/> — 배열 값 복사(<c>Clone()</c>). 슬롯이 struct라 값이 독립되고, 적용 시점
        ///   target 캡처 후 <see cref="CalculateModifierMagnitudes"/>가 복사본 자기 슬롯의 magnitude를 제자리로 다시 채운다.</item>
        /// <item><see cref="Executions"/> — 공유. per-apply 상태가 없는 로직이라 안전하다(인스턴스별 상태가 생기면 복사로 전환).</item>
        /// <item><see cref="CapturedRelevantAttributes"/> — 깊은 복사(<see cref="GameplayEffectAttributeCaptureSpecContainer.Clone"/>).
        ///   대상별 target 캡처가 서로의 컨테이너를 덮어쓰지 않게 하려면 필수다.</item>
        /// </list></para>
        /// </summary>
        private GameplayEffectSpec(GameplayEffectSpec other)
        {
            Definition = other.Definition;
            Context = other.Context;
            Level = other.Level;

            // struct 슬롯 배열이라 Clone()이 값 복사 = 독립. magnitude는 어차피 target 캡처 후
            // CalculateModifierMagnitudes로 다시 채워진다(identity는 슬롯이 아니라 공유 Definition이 든다).
            Modifiers = (GameplayModifierSpec[])other.Modifiers.Clone();
            Executions = other.Executions;
            CapturedRelevantAttributes = other.CapturedRelevantAttributes.Clone();
        }

        /// <summary>
        /// 이 spec의 독립 복사본을 만든다 — 적용 시점에 대상별로 복제해, target 캡처가 대상끼리 섞이지 않게 한다.
        /// <see cref="CaptureAttributeDataFromTarget"/>를 호출하기 전에 이 복사본을 만든다.
        /// </summary>
        public GameplayEffectSpec Clone()
        {
            return new GameplayEffectSpec(this);
        }

        /// <summary>
        /// Modifier·Execution 변환과 어트리뷰트 캡처 등, 멤버 세팅 이후에 수행하는 초기화 로직.
        /// 생성자에서 확정된 <see cref="Definition"/>·<see cref="Context"/>·<see cref="Level"/>에 의존한다.
        /// </summary>
        private void Initialize()
        {
            Executions = ResolveExecutions(Definition);
            BuildModifierSpecs();

            // 캡처 계층(UE 흐름): 필요한 캡처 정의를 컨테이너에 모으고(Setup) → Source를 이 시점에 즉시 캡처한다.
            // Target은 이 시점에 아직 정해지지 않았으므로 적용 시점에 캡처한다(CaptureAttributeDataFromTarget).
            CapturedRelevantAttributes = new GameplayEffectAttributeCaptureSpecContainer();
            SetupAttributeCaptureDefinitions();
            CaptureDataFromSource();

            // Modifier magnitude 계산은 캡처 이후에 한다 — AttributeBased가 Source 캡처값을 읽기 때문.
            // Target 캡처값이 필요한 경우는 적용 시점(CaptureAttributeDataFromTarget)에 다시 계산된다.
            CalculateModifierMagnitudes();
        }

        /// <summary>
        /// 정의와 <b>같은 개수·같은 인덱스</b>로 빈 magnitude 슬롯 배열을 준비하고, 대상 어트리뷰트가 무효인 정의를 경고한다.
        /// 슬롯은 magnitude만 담는 캐시라 여기선 0으로 초기화만 하고, 값은 <see cref="CalculateModifierMagnitudes"/>가 채운다.
        /// 무효한 것도 빼지 않는다 — 빼면 인덱스가 밀려 정의와의 대응이 깨지고, 한 어트리뷰트를 여러 modifier가 건드리는
        /// 경우 위치-key가 어긋난다. 무효 핸들은 경고만 남기고 적용 시 반영되지 않을 뿐이다.
        /// 생성 시 1회만 부른다.
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
        /// 각 슬롯의 magnitude를 현재 캡처 상태 기준으로 (재)계산해 <b>제자리로</b> 채운다.
        /// AttributeBased modifier는 캡처값에 의존하므로
        /// 캡처가 갱신될 때마다 — 생성 시 Source 캡처 후, 적용 시 Target 캡처 후 — 다시 호출해 최신 값을 반영한다.
        /// (ScalableFloat modifier는 캡처와 무관하게 같은 값이 나오지만, 분기 없이 함께 재계산해도 결과는 동일하다.)
        /// 슬롯은 <see cref="BuildModifierSpecs"/>가 이미 만들어 뒀으므로 여기선 magnitude만 갱신한다.
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
        /// 이 GE의 계산이 필요로 하는 캡처 대상 정의를 컨테이너에 등록한다.
        ///
        /// <para>두 공급원에서 정의를 모은다 — ① AttributeBased magnitude를 쓰는 modifier의 backing 캡처 정의,
        /// ② 각 Execution이 선언하는 캡처 정의.</para>
        /// <list type="bullet">
        /// <item>① modifier — <see cref="AttributeBasedMagnitude.BackingAttribute"/>를 등록한다(계산 타입이 AttributeBased인 것만).</item>
        /// <item>② execution — 각 Execution의 <see cref="GameplayEffectExecution.Defs"/>가 선언한 캡처 정의를 등록한다.</item>
        /// </list>
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

        /// <summary>
        /// Source(발동 주체) 어트리뷰트를 캡처한다. spec 생성 시점에 1회 호출한다 — Source는 이 시점에 이미 확정돼 있다.
        /// Source ASC는 Context의 Instigator다. Context가 없으면 null이 넘어가 캡처가 무효로 표시된다.
        /// </summary>
        private void CaptureDataFromSource()
        {
            CapturedRelevantAttributes.CaptureAttributes(Context.GetInstigator(), AttributeCaptureSource.Source);
        }

        /// <summary>
        /// Target(대상) 어트리뷰트를 적용 시점에 대상 ASC로 캡처한다.
        /// 호출부는 대상별로 <see cref="Clone"/>한 복사본에 이 메서드를 호출하므로, 대상끼리 캡처값이 섞이지 않는다.
        ///
        /// <para>Target 캡처가 갱신됐으니 곧바로 <see cref="CalculateModifierMagnitudes"/>로 Modifier magnitude를 다시 계산한다 —
        /// Target 캡처값을 읽는 AttributeBased modifier가 최신 대상 값을 반영하게 하기 위함이다.</para>
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
