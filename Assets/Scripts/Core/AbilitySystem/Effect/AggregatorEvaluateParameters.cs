using Core.AbilitySystem.Tag;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// aggregator 평가 문맥. mod의 Source/Target 태그 조건을 여기 태그로 판정한다. 태그가 null이면 빈 태그로 본다.
    /// 캡처 어트리뷰트 평가(AttributeBased·Execution)가 캡처하는 Spec의 태그를 담아 넘긴다.
    /// </summary>
    /// <remarks>UE: FAggregatorEvaluateParameters</remarks>
    // TODO: UE의 AppliedSourceTagFilter·AppliedTargetTagFilter는 AttributeBased 태그 필터(progress 세부 TODO 9)와 함께 넣는다.
    public readonly struct AggregatorEvaluateParameters
    {
        public GameplayTagContainer SourceTags { get; }
        public GameplayTagContainer TargetTags { get; }

        public AggregatorEvaluateParameters(GameplayTagContainer sourceTags, GameplayTagContainer targetTags)
        {
            SourceTags = sourceTags;
            TargetTags = targetTags;
        }
    }
}
