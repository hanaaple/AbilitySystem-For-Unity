using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Test.Editor.FieldOffset
{
    // GameplayAttributeHandle의 FieldInfo boxing을 field offset 접근으로 대체할 수 있는지 보는 에디터 전용 셀프체크(Play 모드 불필요).
    // fixed: 객체 참조를 RawData로 재해석해 fixed (byte* p = &raw.Data)로 데이터 시작 주소를 얻고, 필드 주소 = p + (필드 오프셋 - RawData.Data 오프셋).
    // Pin: PinGCObjectAndGetAddress가 준 객체 주소 + 필드 오프셋(GetFieldOffset은 헤더 포함).
    // A: 두 방식의 주소가 실제 필드를 가리키는가(부모 선언 필드 포함). B: 접근 방식별 1회당 시간·managed 할당·측정 중 GC 횟수.
    // C: 고정 핸들을 오래 들고 있을 때 GC.Collect 시간(관측).
    // 시간은 에디터 Mono·현재 코드 최적화 모드 기준이라 IL2CPP 플레이어 수치와 다르다 — 방식 간 상대 비교로만 본다.
    public static unsafe class FieldOffsetSelfCheck
    {
        private const int Iterations = 1_000_000;
        private const int WarmupIterations = 10_000;
        private const int AllocationIterations = 10_000;
        private const int ShortIterations = 100_000;
        private const int CollectRepeats = 5;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly int[] PinnedHandleCounts = { 100, 10_000 };

        private static FieldInfo HealthField;
        private static int HealthOffset;
        private static int RawDataOffset;

        [MenuItem("Tools/Ability System/Run Field Offset Self-Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[FieldOffset PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FieldOffset FAIL] {label}");
                }
            }

            HealthField = typeof(OffsetTestSet).GetField(nameof(OffsetTestSet.Health), InstanceFlags);
            HealthOffset = UnsafeUtility.GetFieldOffset(HealthField);
            RawDataOffset = UnsafeUtility.GetFieldOffset(typeof(RawData).GetField(nameof(RawData.Data), InstanceFlags));

            bool addressValid = RunAddressChecks(Check) && RunInheritanceChecks(Check);
            LogAddressStability();

            // 오프셋이 틀렸으면 포인터 쓰기 루프가 엉뚱한 메모리를 덮으므로 비용 측정을 건너뛴다
            if (addressValid)
            {
                RunCostChecks(Check);
                LogPinnedGcCost();
            }
            else
            {
                Debug.LogError("[FieldOffset] 주소 검증 실패로 비용 측정 생략 — 포인터 쓰기가 메모리를 덮을 수 있음");
            }

            Debug.Log($"[FieldOffset Self-Check] passed {passed} / failed {failed}");
        }

        private static bool RunAddressChecks(Action<string, bool> check)
        {
            int manaOffset = UnsafeUtility.GetFieldOffset(typeof(OffsetTestSet).GetField(nameof(OffsetTestSet.Mana), InstanceFlags));
            int staminaOffset = UnsafeUtility.GetFieldOffset(typeof(DerivedOffsetTestSet).GetField(nameof(DerivedOffsetTestSet.Stamina), InstanceFlags));
            Debug.Log($"[FieldOffset INFO] offset RawData.Data={RawDataOffset} Health={HealthOffset} Mana={manaOffset} Stamina={staminaOffset} / IntPtr.Size={IntPtr.Size} / sizeof(AttributeData)={UnsafeUtility.SizeOf<AttributeData>()}");

            OffsetTestSet attributeSet = new DerivedOffsetTestSet { Health = new AttributeData(11f, 12f), Mana = new AttributeData(21f, 22f), Stamina = new AttributeData(31f, 32f) };
            long fixedDataStart;

            ref RawData raw = ref UnsafeUtility.As<OffsetTestSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                fixedDataStart = (long)dataStart;
                ref AttributeData health = ref UnsafeUtility.AsRef<AttributeData>(dataStart + (HealthOffset - RawDataOffset));
                ref AttributeData mana = ref UnsafeUtility.AsRef<AttributeData>(dataStart + (manaOffset - RawDataOffset));
                ref AttributeData stamina = ref UnsafeUtility.AsRef<AttributeData>(dataStart + (staminaOffset - RawDataOffset));

                bool healthRead = Matches(health, 11f, 12f);
                bool manaRead = Matches(mana, 21f, 22f);
                bool staminaRead = Matches(stamina, 31f, 32f);
                check("fixed 읽기 — 부모 선언 필드(Health)를 파생 인스턴스에서 부모 FieldInfo 오프셋으로", healthRead);
                check("fixed 읽기 — 부모 선언 필드(Mana)", manaRead);
                check("fixed 읽기 — 파생 선언 필드(Stamina)", staminaRead);
                if (!healthRead || !manaRead || !staminaRead)
                {
                    Debug.LogError($"[FieldOffset] 읽은 값 Health=({health.GetBaseValue()}, {health.GetCurrentValue()}) — 데이터 시작 주소·오프셋 기준점이 어긋남");
                    return false;
                }

                health.SetCurrentValue(99f);
            }

            var derived = (DerivedOffsetTestSet)attributeSet;
            check("fixed 쓰기 — 포인터로 쓴 값이 C# 필드에 그대로 보인다", derived.Health.GetCurrentValue() == 99f && derived.Health.GetBaseValue() == 11f);
            check("fixed 쓰기 — 이웃 필드(Mana·Stamina)는 그대로", Matches(derived.Mana, 21f, 22f) && Matches(derived.Stamina, 31f, 32f));

            byte* objectStart = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
            try
            {
                ref AttributeData pinnedHealth = ref UnsafeUtility.AsRef<AttributeData>(objectStart + HealthOffset);
                ref AttributeData pinnedStamina = ref UnsafeUtility.AsRef<AttributeData>(objectStart + staminaOffset);

                bool pinRead = Matches(pinnedHealth, 11f, 99f) && Matches(pinnedStamina, 31f, 32f);
                check("Pin 읽기 — 객체 주소 + 오프셋(헤더 포함)으로 부모·파생 선언 필드", pinRead);
                check("Pin 주소 + RawData 오프셋 == fixed로 얻은 데이터 시작 주소", (long)(objectStart + RawDataOffset) == fixedDataStart);
                if (!pinRead)
                {
                    return false;
                }

                pinnedHealth.SetCurrentValue(77f);
            }
            finally
            {
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }

            check("Pin 쓰기 — 포인터로 쓴 값이 C# 필드에 그대로 보이고 이웃 필드는 그대로", derived.Health.GetCurrentValue() == 77f && Matches(derived.Mana, 21f, 22f));
            return true;
        }

        // B : A : AttributeSet — A·B 각각의 public 필드와 [SerializeField] private 필드를 B 인스턴스에서 fixed·Pin·FieldInfo로 읽고 쓴다.
        // A의 private 필드는 B 타입 GetField로 안 보여 부모를 따라 올라가 찾는다(GameplayAttributeHandle.FindField와 같은 방식).
        private static bool RunInheritanceChecks(Action<string, bool> check)
        {
            FieldInfo healthFromA = typeof(InheritBaseSet).GetField(nameof(InheritBaseSet.Health), InstanceFlags);
            FieldInfo healthFromB = typeof(InheritDerivedSet).GetField(nameof(InheritBaseSet.Health), InstanceFlags);
            FieldInfo armor = FindFieldInHierarchy(typeof(InheritDerivedSet), InheritBaseSet.ArmorFieldName);
            FieldInfo stamina = typeof(InheritDerivedSet).GetField(nameof(InheritDerivedSet.Stamina), InstanceFlags);
            FieldInfo shield = FindFieldInHierarchy(typeof(InheritDerivedSet), InheritDerivedSet.ShieldFieldName);
            FieldInfo[] fields = { healthFromA, armor, stamina, shield };
            int[] offsets = fields.Select(field => UnsafeUtility.GetFieldOffset(field)).ToArray();
            Debug.Log($"[FieldOffset INFO] 상속 offset A.Health={offsets[0]} A._armor={offsets[1]} B.Stamina={offsets[2]} B._shield={offsets[3]}");

            check("상속 — B 타입 GetField로는 A의 private 필드가 안 보인다(부모를 따라 올라가야 함)", typeof(InheritDerivedSet).GetField(InheritBaseSet.ArmorFieldName, InstanceFlags) == null && armor != null);
            check("상속 — A의 public 필드는 A·B 어느 타입으로 얻어도 오프셋이 같다", UnsafeUtility.GetFieldOffset(healthFromB) == offsets[0]);

            AttributeSet attributeSet = new InheritDerivedSet();
            var derived = (InheritDerivedSet)attributeSet;
            derived.Health = new AttributeData(1f, 2f);
            derived.ArmorRef() = new AttributeData(3f, 4f);
            derived.Stamina = new AttributeData(5f, 6f);
            derived.ShieldRef() = new AttributeData(7f, 8f);

            bool fieldInfoRead = true;
            for (int i = 0; i < fields.Length; i++)
            {
                fieldInfoRead &= Matches((AttributeData)fields[i].GetValue(attributeSet), i * 2 + 1f, i * 2 + 2f);
            }
            check("상속 FieldInfo 읽기 — A·B의 public·private 필드 4개", fieldInfoRead);

            bool fixedRead = true;
            ref RawData raw = ref UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    fixedRead &= Matches(UnsafeUtility.AsRef<AttributeData>(dataStart + (offsets[i] - RawDataOffset)), i * 2 + 1f, i * 2 + 2f);
                }
                check("상속 fixed 읽기 — A·B의 public·private 필드 4개", fixedRead);
                if (fixedRead)
                {
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        UnsafeUtility.AsRef<AttributeData>(dataStart + (offsets[i] - RawDataOffset)).SetCurrentValue(100f + i);
                    }
                }
            }
            if (!fixedRead)
            {
                return false;
            }
            check("상속 fixed 쓰기 — 4개 필드 모두 C# 필드에 보이고 Base는 그대로", Matches(derived.Health, 1f, 100f) && Matches(derived.ArmorRef(), 3f, 101f) && Matches(derived.Stamina, 5f, 102f) && Matches(derived.ShieldRef(), 7f, 103f));

            bool pinRead = true;
            byte* objectStart = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
            try
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    pinRead &= Matches(UnsafeUtility.AsRef<AttributeData>(objectStart + offsets[i]), i * 2 + 1f, 100f + i);
                }
                check("상속 Pin 읽기 — A·B의 public·private 필드 4개", pinRead);
                if (pinRead)
                {
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        UnsafeUtility.AsRef<AttributeData>(objectStart + offsets[i]).SetBaseValue(200f + i);
                    }
                }
            }
            finally
            {
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
            if (!pinRead)
            {
                return false;
            }
            check("상속 Pin 쓰기 — 4개 필드 모두 C# 필드에 보인다", Matches(derived.Health, 200f, 100f) && Matches(derived.ArmorRef(), 201f, 101f) && Matches(derived.Stamina, 202f, 102f) && Matches(derived.ShieldRef(), 203f, 103f));

            // A 인스턴스(B 아님)에서도 A 필드 오프셋은 그대로 맞는다
            AttributeSet baseOnly = new InheritBaseSet { Health = new AttributeData(9f, 10f) };
            ref RawData baseRaw = ref UnsafeUtility.As<AttributeSet, RawData>(ref baseOnly);
            fixed (byte* dataStart = &baseRaw.Data)
            {
                check("상속 — A 인스턴스에서 A 필드 오프셋으로 fixed 읽기", Matches(UnsafeUtility.AsRef<AttributeData>(dataStart + (offsets[0] - RawDataOffset)), 9f, 10f));
            }

            RunInheritanceCost(check, derived, armor, InheritBaseSet.ArmorFieldName + "(A private)");
            RunInheritanceCost(check, derived, shield, InheritDerivedSet.ShieldFieldName + "(B private)");
            return true;
        }

        // 짧은 측정(10만 회) — 상속 필드에서도 앞의 단가와 같은지만 본다
        private static void RunInheritanceCost(Action<string, bool> check, InheritDerivedSet derived, FieldInfo field, string label)
        {
            AttributeSet attributeSet = derived;
            int offset = UnsafeUtility.GetFieldOffset(field);
            int relativeOffset = offset - RawDataOffset;

            long fieldInfoAlloc = MeasureShort($"상속 {label} FieldInfo", count => InheritFieldInfoLoop(attributeSet, field, count));
            long fixedAlloc = MeasureShort($"상속 {label} fixed", count => InheritFixedLoop(attributeSet, relativeOffset, count));
            long pinAlloc = MeasureShort($"상속 {label} Pin 매 접근", count => InheritPinLoop(attributeSet, offset, count));
            long pinOnceAlloc = MeasureShort($"상속 {label} Pin 1회", count => InheritPinOnceLoop(attributeSet, offset, count));
            MeasureShort("상속 B.Stamina 직접 (기준선)", count => InheritDirectLoop(derived, count));

            check($"상속 {label} — FieldInfo만 할당, fixed·Pin은 할당 0", fieldInfoAlloc > 0 && fixedAlloc == 0 && pinAlloc == 0 && pinOnceAlloc == 0);
        }

        private static long MeasureShort(string label, Func<int, float> loop)
        {
            loop(WarmupIterations);

            var stopwatch = Stopwatch.StartNew();
            float sink = loop(ShortIterations);
            stopwatch.Stop();

            int gcCountBefore = GC.CollectionCount(0);
            long heapBefore = GC.GetTotalMemory(false);
            sink += loop(AllocationIterations);
            long allocated = GC.GetTotalMemory(false) - heapBefore;
            if (GC.CollectionCount(0) != gcCountBefore)
            {
                allocated = -1;
            }

            Debug.Log($"[FieldOffset COST] {label}: {stopwatch.Elapsed.TotalMilliseconds * 1_000_000.0 / ShortIterations:F1} ns/op, 할당 {(allocated < 0 ? "측정 중 GC 발생(무효)" : $"{(double)allocated / AllocationIterations:F2} B/op")} ({ShortIterations}회, sink {sink})");
            return allocated;
        }

        private static float InheritFieldInfoLoop(AttributeSet attributeSet, FieldInfo field, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                var data = (AttributeData)field.GetValue(attributeSet);
                data.SetCurrentValue(i);
                field.SetValue(attributeSet, data);
                sink += data.GetBaseValue();
            }
            return sink;
        }

        private static float InheritFixedLoop(AttributeSet attributeSet, int relativeOffset, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                ref RawData raw = ref UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
                fixed (byte* dataStart = &raw.Data)
                {
                    ref AttributeData data = ref UnsafeUtility.AsRef<AttributeData>(dataStart + relativeOffset);
                    data.SetCurrentValue(i);
                    sink += data.GetBaseValue();
                }
            }
            return sink;
        }

        private static float InheritPinLoop(AttributeSet attributeSet, int offset, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                byte* objectStart = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
                try
                {
                    ref AttributeData data = ref UnsafeUtility.AsRef<AttributeData>(objectStart + offset);
                    data.SetCurrentValue(i);
                    sink += data.GetBaseValue();
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(gcHandle);
                }
            }
            return sink;
        }

        private static float InheritPinOnceLoop(AttributeSet attributeSet, int offset, int count)
        {
            float sink = 0f;
            byte* objectStart = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    ref AttributeData data = ref UnsafeUtility.AsRef<AttributeData>(objectStart + offset);
                    data.SetCurrentValue(i);
                    sink += data.GetBaseValue();
                }
            }
            finally
            {
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
            return sink;
        }

        private static float InheritDirectLoop(InheritDerivedSet attributeSet, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                attributeSet.Stamina.SetCurrentValue(i);
                sink += attributeSet.Stamina.GetBaseValue();
            }
            return sink;
        }

        private static FieldInfo FindFieldInHierarchy(Type setType, string fieldName)
        {
            for (Type type = setType; type != null && type != typeof(AttributeSet); type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, InstanceFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }

        // 판정 아닌 관측: GC 전후로 같은 객체의 주소가 바뀌는지(객체를 옮기는 GC인지)
        private static void LogAddressStability()
        {
            var attributeSet = new OffsetTestSet();
            long before = GetDataStart(ref attributeSet);

            for (int i = 0; i < 10_000; i++)
            {
                _ = new byte[64];
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long after = GetDataStart(ref attributeSet);
            Debug.Log($"[FieldOffset INFO] GC 전후 데이터 시작 주소 {(before == after ? "동일" : "변경")} (0x{before:X} → 0x{after:X})");
        }

        // 고정이 풀린 뒤의 주소라 값 비교(관측)에만 쓴다
        private static long GetDataStart(ref OffsetTestSet attributeSet)
        {
            ref RawData raw = ref UnsafeUtility.As<OffsetTestSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                return (long)dataStart;
            }
        }

        private static void RunCostChecks(Action<string, bool> check)
        {
            Debug.Log($"[FieldOffset INFO] codeOptimization={CompilationPipeline.codeOptimization}, iterations={Iterations}");

            var attributeSet = new OffsetTestSet { Health = new AttributeData(1f, 1f) };

            (long fieldInfoAlloc, _) = Measure("FieldInfo Get+Set (현재 방식)", FieldInfoLoop, attributeSet);
            (long fixedAlloc, int fixedGc) = Measure("호출마다 fixed+오프셋 접근", FixedOffsetLoop, attributeSet);
            (long pinPerAccessAlloc, int pinPerAccessGc) = Measure("호출마다 Pin+접근+Release", PinPerAccessLoop, attributeSet);
            (long pinOnlyAlloc, int pinOnlyGc) = Measure("Pin+Release만 (접근 없음)", PinOnlyLoop, attributeSet);
            (long pinOnceAlloc, int pinOnceGc) = Measure("Pin 1회 후 포인터 접근", PinOnceLoop, attributeSet);
            Measure("직접 필드 접근 (기준선)", DirectFieldLoop, attributeSet);

            check("측정 수단 유효 — FieldInfo 방식에서 할당이 잡힌다(boxing)", fieldInfoAlloc > 0);
            check("fixed+오프셋 — managed 할당 0, 측정 중 GC 0회", fixedAlloc == 0 && fixedGc == 0);
            check("호출마다 Pin — managed 할당 0, 측정 중 GC 0회", pinPerAccessAlloc == 0 && pinPerAccessGc == 0);
            check("Pin+Release만 — managed 할당 0, 측정 중 GC 0회", pinOnlyAlloc == 0 && pinOnlyGc == 0);
            check("Pin 1회 — managed 할당 0, 측정 중 GC 0회", pinOnceAlloc == 0 && pinOnceGc == 0);
        }

        // 반환: (managed 할당 바이트 — 무효면 -1, 시간 측정 중 일어난 GC 횟수)
        private static (long allocated, int gcDuringTiming) Measure(string label, Func<OffsetTestSet, int, float> loop, OffsetTestSet attributeSet)
        {
            loop(attributeSet, WarmupIterations);

            int gcBeforeTiming = GC.CollectionCount(0);
            var stopwatch = Stopwatch.StartNew();
            float sink = loop(attributeSet, Iterations);
            stopwatch.Stop();
            int gcDuringTiming = GC.CollectionCount(0) - gcBeforeTiming;
            double nsPerOp = stopwatch.Elapsed.TotalMilliseconds * 1_000_000.0 / Iterations;

            // 에디터 Mono(Boehm)에선 GC.GetAllocatedBytesForCurrentThread가 boxing이 있어도 0을 돌려줘 힙 크기 차이로 잰다.
            // 그 사이 GC가 돌면 차이가 줄어 틀리므로 GC 없이 끝난 측정만 쓴다(무효면 -1).
            // 직전에 GC.Collect()를 부르면 새 할당이 수거로 빈 자리에 들어가 차이가 거의 안 잡혀(4096 B/1만 회) 부르지 않는다.
            int gcCountBefore = GC.CollectionCount(0);
            long heapBefore = GC.GetTotalMemory(false);
            sink += loop(attributeSet, AllocationIterations);
            long allocated = GC.GetTotalMemory(false) - heapBefore;
            if (GC.CollectionCount(0) != gcCountBefore)
            {
                allocated = -1;
            }

            string allocText = allocated < 0 ? "측정 중 GC 발생(무효)" : $"{(double)allocated / AllocationIterations:F2} B/op (총 {allocated} B / {AllocationIterations}회)";
            Debug.Log($"[FieldOffset COST] {label}: {nsPerOp:F1} ns/op, 할당 {allocText}, 시간 측정 중 GC {gcDuringTiming}회 (sink {sink})");
            return (allocated, gcDuringTiming);
        }

        // 판정 아닌 관측: 고정 핸들을 오래 들고 있으면(Set 수명 동안 Pin) GC 한 번이 느려지는지. 고정 없음은 앞뒤로 재서 편차를 본다.
        private static void LogPinnedGcCost()
        {
            foreach (int count in PinnedHandleCounts)
            {
                var sets = new OffsetTestSet[count];
                for (int i = 0; i < count; i++)
                {
                    sets[i] = new OffsetTestSet();
                }

                double unpinnedBeforeMs = MeasureCollectMs();
                var handles = new ulong[count];
                double pinnedMs;
                for (int i = 0; i < count; i++)
                {
                    UnsafeUtility.PinGCObjectAndGetAddress(sets[i], out handles[i]);
                }
                try
                {
                    pinnedMs = MeasureCollectMs();
                }
                finally
                {
                    for (int i = 0; i < count; i++)
                    {
                        UnsafeUtility.ReleaseGCObject(handles[i]);
                    }
                }
                double unpinnedAfterMs = MeasureCollectMs();

                Debug.Log($"[FieldOffset GC] 고정 핸들 {count}개 유지 중 GC.Collect 평균 {pinnedMs:F2} ms / 고정 없음 {unpinnedBeforeMs:F2} ms(전)·{unpinnedAfterMs:F2} ms(후), {CollectRepeats}회 평균");
                GC.KeepAlive(sets);
            }
        }

        private static double MeasureCollectMs()
        {
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < CollectRepeats; i++)
            {
                GC.Collect();
            }
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds / CollectRepeats;
        }

        private static float FieldInfoLoop(OffsetTestSet attributeSet, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                var data = (AttributeData)HealthField.GetValue(attributeSet);
                data.SetCurrentValue(i);
                HealthField.SetValue(attributeSet, data);
                sink += data.GetBaseValue();
            }
            return sink;
        }

        // 실제 핸들에서 쓸 모양 그대로 — 접근 1회마다 fixed 블록 하나
        private static float FixedOffsetLoop(OffsetTestSet attributeSet, int count)
        {
            float sink = 0f;
            int relativeOffset = HealthOffset - RawDataOffset;
            for (int i = 0; i < count; i++)
            {
                ref RawData raw = ref UnsafeUtility.As<OffsetTestSet, RawData>(ref attributeSet);
                fixed (byte* dataStart = &raw.Data)
                {
                    ref AttributeData data = ref UnsafeUtility.AsRef<AttributeData>(dataStart + relativeOffset);
                    data.SetCurrentValue(i);
                    sink += data.GetBaseValue();
                }
            }
            return sink;
        }

        // 실제 핸들에서 쓸 모양 그대로 — 접근 1회마다 Pin·Release 한 쌍
        private static float PinPerAccessLoop(OffsetTestSet attributeSet, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                byte* objectStart = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
                try
                {
                    ref AttributeData data = ref UnsafeUtility.AsRef<AttributeData>(objectStart + HealthOffset);
                    data.SetCurrentValue(i);
                    sink += data.GetBaseValue();
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(gcHandle);
                }
            }
            return sink;
        }

        private static float PinOnlyLoop(OffsetTestSet attributeSet, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                void* objectStart = UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
                sink += objectStart != null ? 1f : 0f;
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
            return sink;
        }

        // Set 수명 동안 고정해 두는 경우 — 고정은 루프 밖 한 번
        private static float PinOnceLoop(OffsetTestSet attributeSet, int count)
        {
            float sink = 0f;
            byte* objectStart = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(attributeSet, out ulong gcHandle);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    ref AttributeData data = ref UnsafeUtility.AsRef<AttributeData>(objectStart + HealthOffset);
                    data.SetCurrentValue(i);
                    sink += data.GetBaseValue();
                }
            }
            finally
            {
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
            return sink;
        }

        private static float DirectFieldLoop(OffsetTestSet attributeSet, int count)
        {
            float sink = 0f;
            for (int i = 0; i < count; i++)
            {
                attributeSet.Health.SetCurrentValue(i);
                sink += attributeSet.Health.GetBaseValue();
            }
            return sink;
        }

        private static bool Matches(AttributeData data, float baseValue, float currentValue)
        {
            return data.GetBaseValue() == baseValue && data.GetCurrentValue() == currentValue;
        }

        private class OffsetTestSet : AttributeSet
        {
            public AttributeData Health;
            public AttributeData Mana;
        }

        private sealed class DerivedOffsetTestSet : OffsetTestSet
        {
            public AttributeData Stamina;
        }

        private class InheritBaseSet : AttributeSet
        {
            public const string ArmorFieldName = nameof(_armor);

            public AttributeData Health;
            [SerializeField] private AttributeData _armor;

            public ref AttributeData ArmorRef()
            {
                return ref _armor;
            }
        }

        private sealed class InheritDerivedSet : InheritBaseSet
        {
            public const string ShieldFieldName = nameof(_shield);

            public AttributeData Stamina;
            [SerializeField] private AttributeData _shield;

            public ref AttributeData ShieldRef()
            {
                return ref _shield;
            }
        }
    }
}
