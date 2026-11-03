using System;
using System.Collections.Generic;
using Core.AbilitySystem.Tag;
using Core.Common;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// InternedName 비용 측정. 바뀐 비용은 생성·역직렬화(정본 접기 + 캐시 조회)에 있으므로 그 경로를 잰다.
    /// 구현 비교는 scripting define(TAG_NAME_LEGACY / TAG_NAME_INDEXED)을 바꿔 같은 테스트를 다시 돌린다 — 로그의 ImplementationName으로 실제 구현을 확인한다.
    /// 측정 수치는 Performance Test Report(Window ▸ Analysis)의 중앙값으로 읽는다.
    /// </summary>
    [TestFixture]
    [Category("InternedName")]
    [Category("Performance")]
    public sealed class InternedNamePerformanceTests
    {
        private const int WarmupCount = 10;
        private const int MeasurementCount = 20;
        private const int NameCount = 10000;
        private const int EqualsIterations = 1000000;

        private static int _saltCounter;

        [Serializable]
        private sealed class NameArrayJson
        {
            public InternedName[] names;
        }

        [Serializable]
        private sealed class ContainerJson
        {
            public GameplayTagContainer container;
        }

        [SetUp]
        public void LogImplementation()
        {
            TestContext.WriteLine($"ImplementationName={InternedName.ImplementationName} profiler={(TagProfiler.IsEnabled ? "on" : "off")}");
        }

        // 매 측정마다 처음 보는 이름이어야 "최초 생성"(접기·인턴·사전 추가)이 잰다. 측정 횟수만큼 인턴 풀이 커지는 대가는 받아들인다(에디터 테스트 한정).
        private static string[] MakeFreshNames()
        {
            int salt = ++_saltCounter;
            var names = new string[NameCount];
            for (int i = 0; i < NameCount; i++)
            {
                names[i] = (i & 1) == 0 ? $"Perf.S{salt}.Node{i}" : $"PERF.s{salt}.node{i}";
            }

            return names;
        }

        [Test, Performance]
        public void Create_FreshNames()
        {
            string[] names = null;
            Measure.Method(() =>
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        var created = new InternedName(names[i]);
                    }
                })
                .SetUp(() => names = MakeFreshNames())
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SampleGroup(new SampleGroup("Create.Fresh(10k)", SampleUnit.Millisecond))
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Create_KnownNames()
        {
            string[] names = MakeFreshNames();
            for (int i = 0; i < names.Length; i++)
            {
                var warm = new InternedName(names[i]);
            }

            Measure.Method(() =>
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        var created = new InternedName(names[i]);
                    }
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SampleGroup(new SampleGroup("Create.Known(10k)", SampleUnit.Millisecond))
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Deserialize_FreshNames()
        {
            var holder = new NameArrayJson { names = Array.Empty<InternedName>() };
            string json = null;
            Measure.Method(() => JsonUtility.FromJsonOverwrite(json, holder))
                .SetUp(() => json = ToJson(MakeFreshNames()))
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SampleGroup(new SampleGroup("Deserialize.Fresh(10k)", SampleUnit.Millisecond))
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Deserialize_KnownNames()
        {
            var holder = new NameArrayJson { names = Array.Empty<InternedName>() };
            string json = ToJson(MakeFreshNames());
            JsonUtility.FromJsonOverwrite(json, holder);

            Measure.Method(() => JsonUtility.FromJsonOverwrite(json, holder))
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SampleGroup(new SampleGroup("Deserialize.Known(10k)", SampleUnit.Millisecond))
                .GC()
                .Run();
        }

        private static string ToJson(string[] names)
        {
            var source = new NameArrayJson { names = new InternedName[names.Length] };
            for (int i = 0; i < names.Length; i++)
            {
                source.names[i] = new InternedName(names[i]);
            }

            return JsonUtility.ToJson(source);
        }

        [Test, Performance]
        public void StructSize()
        {
            int size;
            try
            {
                size = UnsafeUtility.SizeOf<InternedName>();
            }
            catch (Exception exception)
            {
                Assert.Ignore($"SizeOf 측정 불가: {exception.GetType().Name}");
                return;
            }

            TestContext.WriteLine($"sizeof(InternedName) = {size} B");
            Measure.Custom(new SampleGroup("Size", SampleUnit.Byte), size);
        }

        [Test, Performance]
        public void Equals_And_GetHashCode()
        {
            var a = new InternedName("Perf.Equals.Alpha");
            var b = new InternedName("PERF.EQUALS.ALPHA");
            var c = new InternedName("Perf.Equals.Beta");
            Measure.Method(() =>
                {
                    int acc = 0;
                    for (int i = 0; i < EqualsIterations; i++)
                    {
                        acc += a.Equals(b) ? 1 : 0;
                        acc += a.Equals(c) ? 1 : 0;
                        acc ^= a.GetHashCode();
                    }

                    if (acc == int.MinValue)
                    {
                        // 최적화로 루프가 사라지지 않게 결과를 쓴다.
                        TestContext.WriteLine(acc);
                    }
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(1)
                .SampleGroup(new SampleGroup("Equals+Hash(1M)", SampleUnit.Millisecond))
                .Run();
        }

        [Test, Performance]
        public void TagTreeRebuild_WarmCache()
        {
            // 로딩의 TagTreeConstruct와 같은 경로를 반복 측정한다. 두 번째부터는 정본 캐시 적중이므로 "따뜻한" 비용이다 —
            // 차가운 비용은 Create_FreshNames가 대신 잰다(정본 사전을 비우면 살아 있는 태그의 비교가 깨져 비울 수 없다).
            GameplayTagsManager manager = GameplayTagsManager.Get();
            Assume.That(manager, Is.Not.Null);

            // 한 번이 0.5 ms 수준이라 측정 하나가 1 ms를 넘도록 묶는다(패키지 지침: 1 ms 미만 측정은 노이즈에 민감).
            Measure.Method(() => manager.EditorRefreshGameplayTagTree())
                .WarmupCount(3)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(5)
                .SampleGroup(new SampleGroup("TagTreeRebuild", SampleUnit.Millisecond))
                .GC()
                .Run();
        }

        [Test, Performance]
        public void ContainerDeserialize_RegistryTags()
        {
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            Assume.That(registry, Is.Not.Null, "레지스트리 에셋 없음");

            var container = new GameplayTagContainer();
            foreach (string tagName in registry.Tags)
            {
                GameplayTag tag = GameplayTag.RequestGameplayTag(tagName, false);
                if (tag.IsValid())
                {
                    container.AddTag(tag);
                }
            }

            TestContext.WriteLine($"컨테이너 태그 {container.Num()}개");
            string json = JsonUtility.ToJson(new ContainerJson { container = container });
            var holder = new ContainerJson { container = new GameplayTagContainer() };

            Measure.Method(() => JsonUtility.FromJsonOverwrite(json, holder))
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(10)
                .SampleGroup(new SampleGroup("ContainerDeserialize(registry)", SampleUnit.Microsecond))
                .GC()
                .Run();
        }

        [Test]
        public void Counters_TagTreeRebuild_MatchExpectedCallShape()
        {
            if (!TagProfiler.IsEnabled)
            {
                Assert.Ignore($"{TagProfiler.DefineSymbol} 없음 — 카운터 비활성");
            }

            GameplayTagsManager manager = GameplayTagsManager.Get();
            TagProfiler.Reset();
            manager.EditorRefreshGameplayTagTree();

            long create = TagProfiler.GetCount(TagProfileCounter.NameCreate);
            long deserialize = TagProfiler.GetCount(TagProfileCounter.NameDeserialize);
            long equals = TagProfiler.GetCount(TagProfileCounter.NameEquals);
            TestContext.WriteLine($"NameCreate={create} NameDeserialize={deserialize} NameEquals={equals}");

            Assert.That(create, Is.GreaterThan(0), "트리 구성은 이름을 만든다");
        }
    }
}
