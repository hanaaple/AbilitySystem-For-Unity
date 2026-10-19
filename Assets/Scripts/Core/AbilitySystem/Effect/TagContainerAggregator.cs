using Core.AbilitySystem.Tag;

namespace Core.AbilitySystem.Effect
{
    /// <summary>
    /// Spec이 캡처한 한쪽(Source 또는 Target)의 태그. 액터(ASC) 소유 태그와 Spec 자체 태그를 따로 들고, 둘을 합친 태그를 캐시해 돌려준다.
    /// modifier 실행 판정은 액터 태그만, 캡처 어트리뷰트 평가는 합친 태그를 쓴다.
    /// </summary>
    /// <remarks>UE: FTagContainerAggregator</remarks>
    public sealed class TagContainerAggregator
    {
        private readonly GameplayTagContainer _capturedActorTags = new();
        private readonly GameplayTagContainer _capturedSpecTags = new();
        private readonly GameplayTagContainer _cachedAggregator = new();
        private bool _cacheIsValid;

        public TagContainerAggregator()
        {
        }

        /// <summary>Spec 복제용 깊은 복사. 대상별 복사본이 서로의 태그를 덮어쓰지 않게 컨테이너를 새로 만든다.</summary>
        public TagContainerAggregator(TagContainerAggregator other)
        {
            _capturedActorTags.AppendTags(other._capturedActorTags);
            _capturedSpecTags.AppendTags(other._capturedSpecTags);
        }

        /// <summary>Source·Target 액터(ASC)의 소유 태그. 받은 쪽이 고칠 수 있으므로 합친 태그 캐시를 무효로 한다.</summary>
        /// <remarks>UE: FTagContainerAggregator::GetActorTags</remarks>
        public GameplayTagContainer GetActorTags()
        {
            _cacheIsValid = false;
            return _capturedActorTags;
        }

        /// <summary>Spec에서 온 태그(GE 에셋 태그 등). 받은 쪽이 고칠 수 있으므로 합친 태그 캐시를 무효로 한다.</summary>
        /// <remarks>UE: FTagContainerAggregator::GetSpecTags</remarks>
        public GameplayTagContainer GetSpecTags()
        {
            _cacheIsValid = false;
            return _capturedSpecTags;
        }

        /// <summary>액터 태그 + Spec 태그. 읽기 전용으로 쓴다.</summary>
        /// <remarks>UE: FTagContainerAggregator::GetAggregatedTags</remarks>
        public GameplayTagContainer GetAggregatedTags()
        {
            if (!_cacheIsValid)
            {
                _cacheIsValid = true;
                _cachedAggregator.Reset(_capturedActorTags.Num() + _capturedSpecTags.Num());
                _cachedAggregator.AppendTags(_capturedActorTags);
                _cachedAggregator.AppendTags(_capturedSpecTags);
            }

            return _cachedAggregator;
        }
    }
}
