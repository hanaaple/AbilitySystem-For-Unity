using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 체력 바에서 방금 깎인 구간들(0~1 비율). 깎일 때마다 그 구간을 조각 하나로 남기고, 조각마다 잠시 머물렀다가 아래쪽 끝으로 줄어든다.
    /// 그리기는 쓰는 쪽이 <see cref="Chunks"/>를 읽어 한다 — 허수아비 바(uGUI)와 내 HUD(UI Toolkit)가 같은 규칙을 쓴다.
    /// </summary>
    public sealed class HealthTrail
    {
        private readonly List<Chunk> _chunks = new();
        private readonly float _holdTime;
        private readonly float _shrinkSpeed;

        public IReadOnlyList<Chunk> Chunks => _chunks;

        /// <param name="holdTime">조각이 생긴 뒤 줄어들기 시작할 때까지(초).</param>
        /// <param name="shrinkSpeed">줄어드는 속도(바 전체 길이 / 초).</param>
        public HealthTrail(float holdTime, float shrinkSpeed)
        {
            _holdTime = holdTime;
            _shrinkSpeed = shrinkSpeed;
        }

        /// <summary>체력 비율이 바뀌었음을 알린다. 줄었으면 그 구간을 조각으로 남기고, 늘었으면(회복) 조각을 모두 지운다.</summary>
        public void OnHealthRatioChanged(float oldRatio, float newRatio)
        {
            if (newRatio > oldRatio)
            {
                _chunks.Clear();
                return;
            }

            if (newRatio < oldRatio)
            {
                _chunks.Add(new Chunk(Mathf.Clamp01(newRatio), Mathf.Clamp01(oldRatio), _holdTime));
            }
        }

        // 조각마다 따로 기다렸다 줄어든다 — 계속 맞아도 먼저 깎인 조각부터 사라진다
        public void Tick(float deltaTime)
        {
            for (int i = _chunks.Count - 1; i >= 0; i--)
            {
                Chunk chunk = _chunks[i];
                if (chunk.HoldTimer > 0f)
                {
                    chunk.HoldTimer -= deltaTime;
                }
                else
                {
                    chunk.HighRatio = Mathf.MoveTowards(chunk.HighRatio, chunk.LowRatio, _shrinkSpeed * deltaTime);
                }

                if (chunk.HighRatio <= chunk.LowRatio)
                {
                    _chunks.RemoveAt(i);
                }
            }
        }

        public sealed class Chunk
        {
            public float LowRatio { get; }
            public float HighRatio { get; set; }
            public float HoldTimer { get; set; }

            public Chunk(float lowRatio, float highRatio, float holdTimer)
            {
                LowRatio = lowRatio;
                HighRatio = highRatio;
                HoldTimer = holdTimer;
            }
        }
    }
}
