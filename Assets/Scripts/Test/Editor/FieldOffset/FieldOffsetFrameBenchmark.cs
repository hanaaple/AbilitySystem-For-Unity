using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace Test.Editor.FieldOffset
{
    // 접근 방식별 초기화·매 프레임 Update 비용을 에디터 프레임(EditorApplication.update)에서 돌려 Unity Profiler로 캡처·집계한다(Play 모드 불필요).
    // 규모: Set 100개 × 속성 4개 = 프레임당 방식마다 400회 Get+Set. 각 방식 구간을 ProfilerMarker로 감싸 프레임 데이터에서 시간·GC.Alloc·GC.Collect를 모은다.
    // 초기화·Update 모두 웜업(JIT·리플렉션 캐시)을 한 번 돌린 뒤 잰다.
    // 끝나면 프로파일러 설정(enabled·profileEditor)을 원래대로 돌린다. 캡처한 프레임은 Profiler 창에 남는다.
    public static unsafe class FieldOffsetFrameBenchmark
    {
        private const int SetCount = 100;
        private const int UpdateFrames = 200;
        private const int ProfilerStartFrames = 3;
        private const int ProfilerFlushFrames = 3;
        private const int MainThreadIndex = 0;
        private const string MarkerPrefix = "FieldOffset.";
        private const string GcCollectSampleName = "GC.Collect";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] AttributeNames = { nameof(BenchSet.Health), nameof(BenchSet.Mana), nameof(BenchSet.Stamina), nameof(BenchSet.Speed) };
        private static readonly int RawDataOffset = UnsafeUtility.GetFieldOffset(typeof(RawData).GetField(nameof(RawData.Data), InstanceFlags));

        private static readonly ProfilerMarker InitFieldInfoMarker = new("FieldOffset.Init.FieldInfo");
        private static readonly ProfilerMarker InitOffsetMarker = new("FieldOffset.Init.Offset");
        private static readonly ProfilerMarker UpdateFieldInfoMarker = new("FieldOffset.Update.FieldInfo");
        private static readonly ProfilerMarker UpdateFixedMarker = new("FieldOffset.Update.Fixed");
        private static readonly ProfilerMarker UpdateDirectMarker = new("FieldOffset.Update.Direct");

        private static BenchSet[] Sets;
        private static FieldInfo[] Fields;
        private static int[] RelativeOffsets;
        private static int Frame;
        private static int FirstProfilerFrame;
        private static bool PreviousProfilerEnabled;
        private static bool PreviousProfileEditor;
        private static bool Running;
        private static float Value;
        private static float Sink;

        private static int InitFrame => ProfilerStartFrames;

        private static int UpdateEndFrame => InitFrame + UpdateFrames + 1;

        private static int AnalyzeFrame => UpdateEndFrame + ProfilerFlushFrames;

        [MenuItem("Tools/Ability System/Run Field Offset Frame Benchmark")]
        public static void Run()
        {
            if (Running)
            {
                Debug.LogWarning("[FieldOffset Frame] 이미 실행 중");
                return;
            }
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[FieldOffset Frame] Play 중에는 실행하지 않는다 — 에디터 프레임 기준 측정");
                return;
            }

            Running = true;
            Frame = 0;
            PreviousProfilerEnabled = ProfilerDriver.enabled;
            PreviousProfileEditor = ProfilerDriver.profileEditor;
            ProfilerDriver.profileEditor = true;
            ProfilerDriver.enabled = true;
            EditorApplication.update += Tick;
            Debug.Log($"[FieldOffset Frame] 시작 — Set {SetCount} × 속성 {AttributeNames.Length}, Update {UpdateFrames}프레임");
        }

        private static void Tick()
        {
            try
            {
                Frame++;
                if (Frame == InitFrame)
                {
                    FirstProfilerFrame = ProfilerDriver.lastFrameIndex + 1;
                    Sets = CreateSets();
                    Warmup();
                    RunInit();
                }
                else if (Frame > InitFrame && Frame < UpdateEndFrame)
                {
                    RunUpdate();
                }
                else if (Frame >= AnalyzeFrame)
                {
                    Analyze();
                    Finish();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FieldOffset Frame] 예외로 중단: {e}");
                Finish();
            }
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            ProfilerDriver.enabled = PreviousProfilerEnabled;
            ProfilerDriver.profileEditor = PreviousProfileEditor;
            Sets = null;
            Running = false;
        }

        private static BenchSet[] CreateSets()
        {
            var sets = new BenchSet[SetCount];
            for (int i = 0; i < SetCount; i++)
            {
                sets[i] = new BenchSet { Health = new AttributeData(100f, 100f), Mana = new AttributeData(50f, 50f), Stamina = new AttributeData(30f, 30f), Speed = new AttributeData(5f, 5f) };
            }
            return sets;
        }

        private static void Warmup()
        {
            InitFieldInfo();
            InitOffset();
            UpdateFieldInfo();
            UpdateFixed();
            UpdateDirect();
        }

        private static void RunInit()
        {
            using (InitFieldInfoMarker.Auto())
            {
                InitFieldInfo();
            }
            using (InitOffsetMarker.Auto())
            {
                InitOffset();
            }
        }

        private static void RunUpdate()
        {
            using (UpdateFieldInfoMarker.Auto())
            {
                UpdateFieldInfo();
            }
            using (UpdateFixedMarker.Auto())
            {
                UpdateFixed();
            }
            using (UpdateDirectMarker.Auto())
            {
                UpdateDirect();
            }
        }

        // 현재 GameplayAttributeHandle 생성과 같은 일 — 속성마다 FieldInfo 조회
        private static void InitFieldInfo()
        {
            Fields = ResolveFields();
        }

        // FieldInfo 조회 + 오프셋 계산(이후엔 FieldInfo를 들고 있지 않아도 된다)
        private static void InitOffset()
        {
            FieldInfo[] fields = ResolveFields();
            RelativeOffsets = new int[fields.Length];
            for (int a = 0; a < fields.Length; a++)
            {
                RelativeOffsets[a] = UnsafeUtility.GetFieldOffset(fields[a]) - RawDataOffset;
            }
        }

        private static FieldInfo[] ResolveFields()
        {
            var fields = new FieldInfo[AttributeNames.Length];
            for (int a = 0; a < AttributeNames.Length; a++)
            {
                fields[a] = typeof(BenchSet).GetField(AttributeNames[a], InstanceFlags);
            }
            return fields;
        }

        private static void UpdateFieldInfo()
        {
            foreach (BenchSet attributeSet in Sets)
            {
                foreach (FieldInfo field in Fields)
                {
                    var data = (AttributeData)field.GetValue(attributeSet);
                    Touch(ref data);
                    field.SetValue(attributeSet, data);
                }
            }
        }

        private static void UpdateFixed()
        {
            foreach (BenchSet element in Sets)
            {
                BenchSet attributeSet = element;
                foreach (int relativeOffset in RelativeOffsets)
                {
                    ref RawData raw = ref UnsafeUtility.As<BenchSet, RawData>(ref attributeSet);
                    fixed (byte* dataStart = &raw.Data)
                    {
                        Touch(ref UnsafeUtility.AsRef<AttributeData>(dataStart + relativeOffset));
                    }
                }
            }
        }

        private static void UpdateDirect()
        {
            foreach (BenchSet attributeSet in Sets)
            {
                Touch(ref attributeSet.Health);
                Touch(ref attributeSet.Mana);
                Touch(ref attributeSet.Stamina);
                Touch(ref attributeSet.Speed);
            }
        }

        // 방식마다 같은 일을 하게 한 접근 1회 — Current 쓰기 + Base 읽기
        private static void Touch(ref AttributeData data)
        {
            Value += 1f;
            data.SetCurrentValue(Value);
            Sink += data.GetBaseValue();
        }

        private static void Analyze()
        {
            var stats = new Dictionary<string, MarkerStats>();
            int firstFrame = Math.Max(FirstProfilerFrame, ProfilerDriver.firstFrameIndex);
            int lastFrame = ProfilerDriver.lastFrameIndex;
            for (int frame = firstFrame; frame <= lastFrame; frame++)
            {
                using HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(frame, MainThreadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnDontSort, false);
                if (view == null || !view.valid)
                {
                    continue;
                }
                CollectMarkers(view, view.GetRootItemID(), stats);
            }

            int accessesPerFrame = SetCount * AttributeNames.Length;
            Debug.Log($"[FieldOffset Frame] 프로파일러 프레임 {firstFrame}~{lastFrame} 집계 (Update 기대 {UpdateFrames}프레임, 프레임당 {accessesPerFrame}회 접근)");
            foreach (KeyValuePair<string, MarkerStats> pair in stats.OrderBy(p => p.Key))
            {
                MarkerStats s = pair.Value;
                double avgMs = s.TotalMs / s.Frames;
                string perAccess = pair.Key.Contains(".Update.") ? $", 접근 1회 {avgMs * 1_000_000.0 / accessesPerFrame:F1} ns" : "";
                Debug.Log($"[FieldOffset Frame] {pair.Key}: {s.Frames}프레임, 프레임당 {avgMs * 1000.0:F2} µs{perAccess}, GC.Alloc 프레임당 {s.GcBytes / s.Frames:F0} B, GC.Collect {s.GcCollectCalls}회 {s.GcCollectMs:F2} ms(합)");
            }
            if (stats.Count == 0)
            {
                Debug.LogError("[FieldOffset Frame] 프로파일러 데이터에서 마커를 못 찾음 — 에디터 프로파일링이 기록되지 않았을 수 있음");
            }
        }

        private static void CollectMarkers(HierarchyFrameDataView view, int itemId, Dictionary<string, MarkerStats> stats)
        {
            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (int child in children)
            {
                string name = view.GetItemName(child);
                if (!name.StartsWith(MarkerPrefix))
                {
                    CollectMarkers(view, child, stats);
                    continue;
                }

                if (!stats.TryGetValue(name, out MarkerStats s))
                {
                    s = new MarkerStats();
                    stats[name] = s;
                }
                s.Frames++;
                s.TotalMs += view.GetItemColumnDataAsDouble(child, HierarchyFrameDataView.columnTotalTime);
                s.GcBytes += view.GetItemColumnDataAsDouble(child, HierarchyFrameDataView.columnGcMemory);
                CollectGcCollect(view, child, s);
            }
        }

        private static void CollectGcCollect(HierarchyFrameDataView view, int itemId, MarkerStats s)
        {
            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (int child in children)
            {
                if (view.GetItemName(child) == GcCollectSampleName)
                {
                    s.GcCollectCalls += (int)view.GetItemColumnDataAsDouble(child, HierarchyFrameDataView.columnCalls);
                    s.GcCollectMs += view.GetItemColumnDataAsDouble(child, HierarchyFrameDataView.columnTotalTime);
                }
                CollectGcCollect(view, child, s);
            }
        }

        private sealed class MarkerStats
        {
            public int Frames;
            public double TotalMs;
            public double GcBytes;
            public int GcCollectCalls;
            public double GcCollectMs;
        }

        private sealed class BenchSet : AttributeSet
        {
            public AttributeData Health;
            public AttributeData Mana;
            public AttributeData Stamina;
            public AttributeData Speed;
        }
    }
}
