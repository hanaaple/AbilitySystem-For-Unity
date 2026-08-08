namespace Core.AbilitySystem.Aggregator
{
    // [스텁 — 미구현] 어트리뷰트 반응성(라이브 재평가)의 앵커.
    //
    // 역할(예정): UE FAggregator의 "반응성 책임"만 축소 재현한다 —
    //   ① 어떤 어트리뷰트가 바뀌면 그 값에 의존하는(non-snapshot 캡처) 모디파이어를 찾아
    //   ② magnitude를 라이브로 재평가(GameplayEffectSpec.CalculateModifierMagnitudes)하고
    //   ③ 영향받는 어트리뷰트를 재계산한다. 재진입 억제로 연쇄 1패스·자기참조 무한루프를 막는다.
    //
    // 비-역할: 모디파이어 채널 소유(R1)·CurrentValue 계산(R2)은 AbilitySystemComponent가 계속 담당한다.
    //          태그 자격(R5)은 여기서 하지 않고, 나중에 얹을 seam만 둔다.
    //
    // 설계·범위·UE 원문 근거:
    //   feature   dev-docs/agent/feature/ability-system/aggregator/progress.md (결정 D1/D2)
    //   아키텍처  dev-docs/project/architecture/ability-system/aggregator.md
    //
    // TODO 1: 어트리뷰트 변경 이벤트(옵저버) — ASC 쓰기 지점에서 발화.
    // TODO 2: non-snapshot 캡처 의존 등록/해지(apply/remove).
    // TODO 3: 변경 핸들러 — 의존 모디파이어 재평가 → 영향 어트리뷰트 재계산.
    // TODO 4: 재진입 억제(가드).
    // TODO 5: 태그 seam(자격 판정 함수 경계 + dirty 트리거 일반화) — 자리만.
    internal sealed class AttributeAggregator
    {
    }
}
