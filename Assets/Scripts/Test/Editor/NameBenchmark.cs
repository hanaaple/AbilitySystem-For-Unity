using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core.Common;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Test.Editor
{
    // InternedName(인터닝된 string 참조 + 해시 캐시) vs IndexedName(정수 인덱스 2개) vs string 키(인터닝·캐시 없음)를
    // 같은 이름 집합·같은 접근 순서로 잰다. 에디터 Mono 수치라 IL2CPP 빌드와 다를 수 있다.
    // A: struct 크기 / B: 생성(최초 삽입·기존 이름 재생성) / C: Dictionary 조회 / D: Equals / E: 대량 배열이 살아 있을 때 GC.Collect 시간.
    public static class NameBenchmark
    {
        private const int NameCount = 10000;
        private const int LookupCount = 1000000;
        private const int GcArrayLength = 1000000;
        private const int GcCollectRuns = 5;

        [MenuItem("Tools/Ability System/Run Name Benchmark")]
        public static void Run()
        {
            // 메뉴를 다시 눌러도 "최초 삽입"이 재현되게 매 실행 다른 이름을 쓴다 — 인턴 풀·테이블은 비울 수 없다.
            string salt = DateTime.Now.Ticks.ToString("x");
            string[] names = BuildNames(NameCount, salt);
            int[] order = BuildAccessOrder(LookupCount, NameCount);

            Debug.Log($"[Name Benchmark] names {NameCount:N0} / lookups {LookupCount:N0} / gc array {GcArrayLength:N0} — Editor Mono, {(Debugger.IsAttached ? "debugger attached" : "no debugger")}");
            Debug.Log($"[Name Benchmark A] sizeof InternedName {UnsafeUtility.SizeOf<InternedName>()} B / IndexedName {UnsafeUtility.SizeOf<IndexedName>()} B / string ref {IntPtr.Size} B");

            // JIT 워밍업 — 수치는 버린다.
            RunSeries("warmup", names, order, n => new InternedName(n), false);
            RunSeries("warmup", names, order, n => new IndexedName(n), false);
            RunSeries("warmup", names, order, n => new string(n.ToCharArray()), false);

            RunSeries("InternedName", names, order, n => new InternedName(n), true);
            RunSeries("IndexedName", names, order, n => new IndexedName(n), true);
            RunSeries("string(no intern)", names, order, n => new string(n.ToCharArray()), true);

            RunGcSeries(names);

            Debug.Log($"[Name Benchmark] IndexedName table — comparison entries {IndexedName.ComparisonEntryCount:N0} / display entries {IndexedName.DisplayEntryCount:N0}");
        }

        private static void RunSeries<T>(string label, string[] names, int[] order, Func<string, T> create, bool report) where T : IEquatable<T>
        {
            // B. 생성 — 첫 패스는 테이블 삽입을 포함, 둘째 패스는 기존 이름 조회만.
            T[] keys = new T[names.Length];
            double firstCreateMs = Time(() =>
            {
                for (int i = 0; i < names.Length; i++)
                {
                    keys[i] = create(names[i]);
                }
            });
            T[] keysAgain = new T[names.Length];
            double secondCreateMs = Time(() =>
            {
                for (int i = 0; i < names.Length; i++)
                {
                    keysAgain[i] = create(names[i]);
                }
            });

            // C. Dictionary 조회 — 키는 사전에 넣은 인스턴스가 아니라 두 번째 패스의 인스턴스로 찾는다(string은 다른 참조·같은 내용).
            var map = new Dictionary<T, int>(names.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                map.Add(keys[i], i);
            }
            long lookupAllocBefore = GC.GetTotalMemory(false);
            int hits = 0;
            double lookupMs = Time(() =>
            {
                for (int i = 0; i < order.Length; i++)
                {
                    if (map.TryGetValue(keysAgain[order[i]], out int value))
                    {
                        hits += value & 1;
                    }
                }
            });
            long lookupAllocBytes = GC.GetTotalMemory(false) - lookupAllocBefore;

            // D. Equals — 사전 없이 비교 비용만.
            int equalCount = 0;
            double equalsMs = Time(() =>
            {
                for (int i = 0; i < order.Length; i++)
                {
                    int index = order[i];
                    if (keys[index].Equals(keysAgain[index]))
                    {
                        equalCount++;
                    }
                }
            });

            if (!report)
            {
                return;
            }

            Debug.Log($"[Name Benchmark B] {label} create first {firstCreateMs:F2} ms ({NsPerOp(firstCreateMs, names.Length):F0} ns/op) / again {secondCreateMs:F2} ms ({NsPerOp(secondCreateMs, names.Length):F0} ns/op)");
            Debug.Log($"[Name Benchmark C] {label} dictionary lookup {lookupMs:F2} ms ({NsPerOp(lookupMs, order.Length):F1} ns/op), alloc {lookupAllocBytes:N0} B, hits {hits:N0}");
            Debug.Log($"[Name Benchmark D] {label} equals {equalsMs:F2} ms ({NsPerOp(equalsMs, order.Length):F1} ns/op), equal {equalCount:N0}/{order.Length:N0}");
        }

        // E. 참조를 품은 struct 배열은 GC 스캔 대상이고 int만 든 배열은 아니다 — 큰 배열이 살아 있을 때 수집 시간 차이로 본다.
        private static void RunGcSeries(string[] names)
        {
            double baselineMs = MeasureGcCollectMs<int>(null);
            Debug.Log($"[Name Benchmark E] GC.Collect baseline (no array) best {baselineMs:F2} ms");

            long before = GC.GetTotalMemory(true);
            InternedName[] interned = new InternedName[GcArrayLength];
            for (int i = 0; i < interned.Length; i++)
            {
                interned[i] = new InternedName(names[i % names.Length]);
            }
            long internedBytes = GC.GetTotalMemory(true) - before;
            double internedMs = MeasureGcCollectMs(interned);
            Debug.Log($"[Name Benchmark E] InternedName[{GcArrayLength:N0}] ≈ {internedBytes:N0} B, GC.Collect best {internedMs:F2} ms");
            interned = null;

            before = GC.GetTotalMemory(true);
            IndexedName[] indexed = new IndexedName[GcArrayLength];
            for (int i = 0; i < indexed.Length; i++)
            {
                indexed[i] = new IndexedName(names[i % names.Length]);
            }
            long indexedBytes = GC.GetTotalMemory(true) - before;
            double indexedMs = MeasureGcCollectMs(indexed);
            Debug.Log($"[Name Benchmark E] IndexedName[{GcArrayLength:N0}] ≈ {indexedBytes:N0} B, GC.Collect best {indexedMs:F2} ms");
            indexed = null;
        }

        private static double MeasureGcCollectMs<T>(T[] keepAlive)
        {
            double best = double.MaxValue;
            for (int i = 0; i < GcCollectRuns; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                stopwatch.Stop();
                best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds);
            }
            GC.KeepAlive(keepAlive);
            return best;
        }

        private static double Time(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private static double NsPerOp(double ms, int count)
        {
            return ms * 1000000.0 / count;
        }

        // 실제 태그처럼 점으로 나뉜 3단 이름. 뒤쪽 숫자로만 달라 해시가 끝까지 훑어야 갈린다.
        private static string[] BuildNames(int count, string salt)
        {
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = $"Status.Bench{salt}.Name{i:D5}";
            }
            return names;
        }

        // 순차 접근의 캐시 이점을 피하려고 LCG로 흩뿌린 인덱스 — 세 타입이 같은 순서를 쓴다.
        private static int[] BuildAccessOrder(int count, int range)
        {
            int[] order = new int[count];
            uint state = 12345u;
            for (int i = 0; i < count; i++)
            {
                state = state * 1664525u + 1013904223u;
                order[i] = (int)(state % (uint)range);
            }
            return order;
        }
    }
}
