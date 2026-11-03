using System;
using System.Diagnostics;
using System.Text;
using Unity.Profiling;

namespace Core.Common
{
    public enum TagProfileCounter
    {
        NameCreate,
        NameDeserialize,
        NameEquals,
        NodeMapLookup,
        CountMapLookup,
        CountMapUpdate,
        NativeTagConvert,
        Count,
    }

    public enum TagProfileSection
    {
        TagTreeConstruct,
        ContainerDeserialize,
        AbilityActivate,
        EffectApply,
        EffectRemove,
        EffectTick,
        TagMapUpdate,
        Count,
    }

    /// <summary>
    /// 태그 이름 구현(InternedName vs IndexedName) A/B 측정용 집계. 횟수와 구간 시간을 모아 단계별로 보고한다.
    /// 뜨거운 경로의 호출은 TAG_PROFILING이 없으면 컴파일에서 사라진다 — 측정이 아닐 때 비용 0.
    /// </summary>
    public static class TagProfiler
    {
        public const string DefineSymbol = "TAG_PROFILING";
        public const string MarkerPrefix = "Tag.";

        private static readonly long[] _counters = new long[(int)TagProfileCounter.Count];
        private static readonly long[] _sectionTicks = new long[(int)TagProfileSection.Count];
        private static readonly long[] _sectionCalls = new long[(int)TagProfileSection.Count];
        // 성능 테스트(Measure.ProfilerMarkers)가 define 없이도 구간을 읽을 수 있게 마커는 항상 발행한다 — 프로파일러가 꺼져 있으면 Begin/End는 수 ns.
        private static readonly ProfilerMarker[] _markers = CreateMarkers();

        public static string GetMarkerName(TagProfileSection section)
        {
            return MarkerPrefix + Enum.GetName(typeof(TagProfileSection), section);
        }

        public static long GetCount(TagProfileCounter counter)
        {
            return _counters[(int)counter];
        }

        private static ProfilerMarker[] CreateMarkers()
        {
            var markers = new ProfilerMarker[(int)TagProfileSection.Count];
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = new ProfilerMarker(GetMarkerName((TagProfileSection)i));
            }

            return markers;
        }

        public static bool IsEnabled
        {
            get
            {
#if TAG_PROFILING
                return true;
#else
                return false;
#endif
            }
        }

        [Conditional(DefineSymbol)]
        public static void Count(TagProfileCounter counter)
        {
            _counters[(int)counter]++;
        }

        public static Scope Begin(TagProfileSection section)
        {
            return new Scope(section);
        }

        public static void Reset()
        {
            Array.Clear(_counters, 0, _counters.Length);
            Array.Clear(_sectionTicks, 0, _sectionTicks.Length);
            Array.Clear(_sectionCalls, 0, _sectionCalls.Length);
        }

        public static string Report(string stage, string mode, int frames, double wallMs, double averageFrameMs)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[Tag Profile] {stage} | mode={mode} | frames={frames} wall={wallMs:F1} ms avgFrame={averageFrameMs:F2} ms");
            builder.AppendLine($"  {"section",-22}{"calls",10}{"total ms",12}{"ms/frame",12}{"us/call",10}");
            for (int i = 0; i < (int)TagProfileSection.Count; i++)
            {
                long calls = _sectionCalls[i];
                double totalMs = _sectionTicks[i] * 1000.0 / Stopwatch.Frequency;
                double msPerFrame = frames > 0 ? totalMs / frames : 0.0;
                double usPerCall = calls > 0 ? totalMs * 1000.0 / calls : 0.0;
                builder.AppendLine($"  {Enum.GetName(typeof(TagProfileSection), i),-22}{calls,10}{totalMs,12:F3}{msPerFrame,12:F4}{usPerCall,10:F2}");
            }

            builder.Append("  counters:");
            for (int i = 0; i < (int)TagProfileCounter.Count; i++)
            {
                builder.Append($" {Enum.GetName(typeof(TagProfileCounter), i)}={_counters[i]}");
            }

            return builder.ToString();
        }

        public readonly struct Scope : IDisposable
        {
            private readonly int _section;
#if TAG_PROFILING
            private readonly long _start;
#endif

            public Scope(TagProfileSection section)
            {
                _section = (int)section;
                _markers[_section].Begin();
#if TAG_PROFILING
                _start = Stopwatch.GetTimestamp();
#endif
            }

            public void Dispose()
            {
#if TAG_PROFILING
                _sectionTicks[_section] += Stopwatch.GetTimestamp() - _start;
                _sectionCalls[_section]++;
#endif
                _markers[_section].End();
            }
        }
    }
}
