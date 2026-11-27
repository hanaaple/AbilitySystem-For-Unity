using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Core.AbilitySystem.Attribute;
using Game;
using Game.Character;
using Test.FieldOffsetSamples;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

// 테스트 Set의 필드 값은 리플렉션으로만 읽고 쓴다 — 미사용·미할당 경고를 끈다
#pragma warning disable CS0169, CS0414, CS0649

namespace Test.Editor.FieldOffset
{
    // field offset 접근(fixed)의 정확도를 여러 AttributeSet 형태에서 본다(Play 모드 불필요). 기준값은 FieldInfo.GetValue/SetValue.
    // Set마다: 오프셋 겹침 없음 / fixed 읽기(참조 변수 정적 타입 object·AttributeSet·구체 타입) / fixed 쓰기 후 대상만 바뀌고 나머지 그대로.
    // 형태: 같은 어셈블리 A·B : A·C : A(형제)·BB : B(손자), 다른 asmdef의 A·B, 어셈블리를 넘는 상속, 제네릭, 실제 게임 Set, 생성 방식(Activator·생성자 없이).
    // 필드: 접근 제한자 6종, SerializeField·NonSerialized, readonly, 자동 프로퍼티 backing field, 이름 숨김(new), 사이에 낀 다른 타입 필드.
    // 마지막으로 형변환 후 안전성 — 필드 선언 타입 기준 가드가 어느 인스턴스를 허용·거부하는지.
    public static unsafe class FieldOffsetAccuracyCheck
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags DeclaredInstanceFlags = InstanceFlags | BindingFlags.DeclaredOnly;

        private static readonly int RawDataOffset = UnsafeUtility.GetFieldOffset(typeof(RawData).GetField(nameof(RawData.Data), InstanceFlags));
        private static readonly int AttributeDataSize = UnsafeUtility.SizeOf<AttributeData>();

        private interface ILocalMarker
        {
        }

        [MenuItem("Tools/Ability System/Run Field Offset Accuracy Check")]
        public static void Run()
        {
            int passed = 0;
            int failed = 0;

            void Check(string label, bool condition)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[FieldOffset ACC PASS] {label}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FieldOffset ACC FAIL] {label}");
                }
            }

            RunCase(Check, "같은 asm A", () => new LocalBaseSet());
            RunCase(Check, "같은 asm B : A", () => new LocalDerivedSet());
            RunCase(Check, "같은 asm C : A (형제)", () => new LocalSiblingSet());
            RunCase(Check, "같은 asm BB : B (손자)", () => new LocalGrandChildSet());
            RunCase(Check, "외부 asm A", () => new ExternalBaseSet());
            RunCase(Check, "외부 asm B : A", () => new ExternalDerivedSet());
            RunCase(Check, "asm 넘는 상속 (테스트 asm) : 외부 A", () => new CrossAssemblyDerivedSet());
            RunCase(Check, "제네릭 <byte>", () => new GenericSet<byte>());
            RunCase(Check, "제네릭 <double>", () => new GenericSet<double>());
            RunCase(Check, "제네릭 <string>", () => new GenericSet<string>());
            RunCase(Check, "제네릭 <Vector3>", () => new GenericSet<Vector3>());
            RunCase(Check, "제네릭 파생 <byte> : 같은 asm A", () => new GenericDerivedSet<byte>());
            RunCase(Check, "제네릭 파생 <string> : 같은 asm A", () => new GenericDerivedSet<string>());
            RunCase(Check, "게임 CharacterAttributeSet", () => new CharacterAttributeSet());
            RunCase(Check, "게임 CombatAttributeSet", () => new CombatAttributeSet());
            RunCase(Check, "게임 PlayerAttributeSet", () => new PlayerAttributeSet());
            RunCase(Check, "게임 BoxAttributeSet", () => new BoxAttributeSet());
            RunCase(Check, "게임 TestAtrributeSet", () => new TestAtrributeSet());
            RunCase(Check, "B : A — Activator.CreateInstance", () => (LocalDerivedSet)Activator.CreateInstance(typeof(LocalDerivedSet)));
            RunCase(Check, "B : A — 생성자 없이(GetUninitializedObject)", () => (LocalDerivedSet)FormatterServices.GetUninitializedObject(typeof(LocalDerivedSet)));

            RunFieldDiscoveryChecks(Check);
            RunCastChecks(Check);
            RunGuardChecks(Check);

            Debug.Log($"[FieldOffset Accuracy] passed {passed} / failed {failed}");
        }

        private static void RunCase<TSet>(Action<string, bool> check, string label, Func<TSet> create) where TSet : AttributeSet
        {
            try
            {
                CheckSet(check, label, create());
            }
            catch (Exception e)
            {
                check($"{label} — 예외 없이 끝난다 ({e.GetType().Name}: {e.Message})", false);
            }
        }

        private static void CheckSet<TSet>(Action<string, bool> check, string label, TSet instance) where TSet : AttributeSet
        {
            List<FieldInfo> fields = CollectAttributeFields(instance.GetType());
            int[] offsets = fields.Select(field => UnsafeUtility.GetFieldOffset(field)).ToArray();
            Debug.Log($"[FieldOffset ACC] {label}: 필드 {fields.Count}개 — {string.Join(", ", fields.Select((field, i) => $"{FieldLabel(field)}={offsets[i]}"))}");

            bool disjoint = OffsetsDisjoint(offsets);
            check($"{label} — 오프셋 {fields.Count}개가 헤더 뒤에서 서로 겹치지 않는다", disjoint);
            if (!disjoint)
            {
                return;
            }

            var expected = new AttributeData[fields.Count];
            for (int i = 0; i < fields.Count; i++)
            {
                expected[i] = new AttributeData(i * 10f + 1f, i * 10f + 2f);
                fields[i].SetValue(instance, expected[i]);
            }

            object asObject = instance;
            AttributeSet asSet = instance;
            TSet asSelf = instance;
            var fixedMismatches = new List<string>();
            for (int i = 0; i < fields.Count; i++)
            {
                int relativeOffset = offsets[i] - RawDataOffset;
                var reflected = (AttributeData)fields[i].GetValue(instance);
                if (!Same(ReadFixed(ref asObject, relativeOffset), reflected) || !Same(ReadFixed(ref asSet, relativeOffset), reflected) || !Same(ReadFixed(ref asSelf, relativeOffset), reflected))
                {
                    fixedMismatches.Add(FieldLabel(fields[i]));
                }
            }
            Report(check, $"{label} — fixed 읽기(object·AttributeSet·{typeof(TSet).Name} 참조) {fields.Count}개", fixedMismatches);
            if (fixedMismatches.Count > 0)
            {
                return;
            }

            // 한 필드씩 쓰고 매번 전체를 리플렉션으로 확인 — 대상만 바뀌고 이웃은 그대로여야 한다
            var fixedWriteMismatches = new List<string>();
            for (int i = 0; i < fields.Count; i++)
            {
                WriteFixedCurrent(instance, offsets[i] - RawDataOffset, 1000f + i);
                expected[i].SetCurrentValue(1000f + i);
                CollectStateMismatches(instance, fields, expected, $"fixed 쓰기 {FieldLabel(fields[i])} 후", fixedWriteMismatches);
            }
            Report(check, $"{label} — fixed 쓰기 {fields.Count}개(대상만 바뀌고 나머지 그대로)", fixedWriteMismatches);
        }

        // 필드 수집이 기대한 것을 담는지 — static 제외, 숨긴 필드는 둘 다, 자동 프로퍼티 backing field 포함, 부모 private은 부모를 따라 올라가야 보임
        private static void RunFieldDiscoveryChecks(Action<string, bool> check)
        {
            List<FieldInfo> derivedFields = CollectAttributeFields(typeof(LocalDerivedSet));
            check("수집 — static AttributeData 필드는 제외", derivedFields.All(field => !field.IsStatic) && typeof(LocalBaseSet).GetField(nameof(LocalBaseSet.StaticData), BindingFlags.Static | BindingFlags.Public) != null);
            check("수집 — 자동 프로퍼티 backing field 포함", derivedFields.Any(field => field.Name.Contains(nameof(LocalBaseSet.AutoPropertyData))));

            FieldInfo[] hidden = derivedFields.Where(field => field.Name == nameof(LocalBaseSet.PublicData)).ToArray();
            check("수집 — 이름 숨김(new) PublicData는 A·B 두 필드, 오프셋이 다르다", hidden.Length == 2 && UnsafeUtility.GetFieldOffset(hidden[0]) != UnsafeUtility.GetFieldOffset(hidden[1]));
            check("수집 — B 타입 GetField(\"PublicData\")는 B가 숨긴 필드를 준다", typeof(LocalDerivedSet).GetField(nameof(LocalBaseSet.PublicData), InstanceFlags)?.DeclaringType == typeof(LocalDerivedSet));
            check("수집 — B 타입 GetField로는 A의 private 필드가 안 보인다", typeof(LocalDerivedSet).GetField(LocalBaseSet.SerializedPrivateFieldName, InstanceFlags) == null);
            check("수집 — 외부 asm A의 private 필드도 asm 넘는 파생 타입에서 부모를 따라 올라가면 보인다", FindFieldInHierarchy(typeof(CrossAssemblyDerivedSet), "_serializedPrivateData") != null);

            // 숨긴 필드 둘을 C# 코드로 따로 쓰고 포인터로 각각 맞게 읽히는지
            var attributeSet = new LocalDerivedSet { PublicData = new AttributeData(7f, 8f) };
            ((LocalBaseSet)attributeSet).PublicData = new AttributeData(5f, 6f);
            AttributeSet asSet = attributeSet;
            FieldInfo basePublic = hidden.First(field => field.DeclaringType == typeof(LocalBaseSet));
            FieldInfo derivedPublic = hidden.First(field => field.DeclaringType == typeof(LocalDerivedSet));
            check("이름 숨김 — A.PublicData·B.PublicData를 각자 오프셋으로 읽으면 C# 값과 같다", Same(ReadFixed(ref asSet, UnsafeUtility.GetFieldOffset(basePublic) - RawDataOffset), new AttributeData(5f, 6f)) && Same(ReadFixed(ref asSet, UnsafeUtility.GetFieldOffset(derivedPublic) - RawDataOffset), new AttributeData(7f, 8f)));
        }

        // 참조를 담은 변수의 정적 타입이 달라도(업캐스트·다운캐스트·인터페이스·object) 같은 데이터 시작 주소가 나와야 한다
        private static void RunCastChecks(Action<string, bool> check)
        {
            var derived = new LocalGrandChildSet();
            object asObject = derived;
            AttributeSet asSet = derived;
            LocalBaseSet asBase = derived;
            LocalDerivedSet asMiddle = derived;
            ILocalMarker asInterface = derived;
            var downcast = (LocalGrandChildSet)asSet;
            long[] starts = { DataStart(ref asObject), DataStart(ref asSet), DataStart(ref asBase), DataStart(ref asMiddle), DataStart(ref asInterface), DataStart(ref downcast) };
            check("형변환 — object·AttributeSet·A·B·인터페이스·다운캐스트 참조 모두 같은 데이터 시작 주소", starts.Distinct().Count() == 1);

            FieldInfo baseSerialized = FindFieldInHierarchy(typeof(LocalGrandChildSet), LocalBaseSet.SerializedPrivateFieldName);
            baseSerialized.SetValue(derived, new AttributeData(31f, 32f));
            check("형변환 — 인터페이스 참조로 A의 private 필드 fixed 읽기", Same(ReadFixed(ref asInterface, UnsafeUtility.GetFieldOffset(baseSerialized) - RawDataOffset), new AttributeData(31f, 32f)));
        }

        // 형변환 후 안전성: 필드를 선언한 타입이 인스턴스의 런타임 타입과 같거나 그 조상일 때만 오프셋을 써도 된다.
        // 아니면 오프셋이 그 객체 밖이나 다른 필드를 가리킨다 — 그 경우는 읽지 않고 가드 판정만 본다.
        private static void RunGuardChecks(Action<string, bool> check)
        {
            FieldInfo aField = typeof(LocalBaseSet).GetField(nameof(LocalBaseSet.InternalData), InstanceFlags);
            FieldInfo bField = typeof(LocalDerivedSet).GetField(nameof(LocalDerivedSet.DerivedPublicData), InstanceFlags);
            FieldInfo externalAField = typeof(ExternalBaseSet).GetField(nameof(ExternalBaseSet.PublicData), InstanceFlags);
            FieldInfo externalBField = typeof(ExternalDerivedSet).GetField(nameof(ExternalDerivedSet.DerivedPublicData), InstanceFlags);
            AttributeSet a = new LocalBaseSet();
            AttributeSet b = new LocalDerivedSet();
            AttributeSet c = new LocalSiblingSet();
            AttributeSet bb = new LocalGrandChildSet();
            AttributeSet cross = new CrossAssemblyDerivedSet();

            check("가드 — B 인스턴스(AttributeSet 참조) + A 필드 → 허용", CanAccess(b, aField));
            check("가드 — BB 인스턴스 + A 필드·B 필드 → 허용", CanAccess(bb, aField) && CanAccess(bb, bField));
            check("가드 — A 인스턴스 + B 필드 → 거부(오프셋이 객체 밖)", !CanAccess(a, bField));
            check("가드 — 형제 C 인스턴스 + B 필드 → 거부(다른 필드를 가리킴)", !CanAccess(c, bField));
            check("가드 — asm 넘는 파생 + 외부 A 필드 → 허용, 외부 B 필드 → 거부", CanAccess(cross, externalAField) && !CanAccess(cross, externalBField));
            check("가드 — null → 거부", !CanAccess(null, aField));

            // 판정 아닌 관측: 정확한 타입 일치만 보는 가드(현 GameplayAttributeHandle.HasField 방식)는 상속 필드를 거부한다
            Debug.Log($"[FieldOffset ACC INFO] 정확 타입 비교(attributeSet.GetType() == 선언 타입) — B 인스턴스 + A 필드: {b.GetType() == aField.DeclaringType}, BB + B 필드: {bb.GetType() == bField.DeclaringType}");

            // 판정 아닌 관측: readonly 필드도 포인터로 쓰면 바뀐다(언어 보호 밖)
            var readonlySet = new LocalBaseSet();
            AttributeSet readonlyAsSet = readonlySet;
            FieldInfo readonlyField = typeof(LocalBaseSet).GetField(nameof(LocalBaseSet.ReadonlyData), InstanceFlags);
            WriteFixedCurrent(readonlyAsSet, UnsafeUtility.GetFieldOffset(readonlyField) - RawDataOffset, 42f);
            Debug.Log($"[FieldOffset ACC INFO] readonly 필드 포인터 쓰기 후 C# 값 Current={readonlySet.ReadonlyData.GetCurrentValue()} (initonly={readonlyField.IsInitOnly})");
        }

        private static bool CanAccess(AttributeSet attributeSet, FieldInfo field)
        {
            return attributeSet != null && field.DeclaringType != null && field.DeclaringType.IsInstanceOfType(attributeSet);
        }

        private static AttributeData ReadFixed<TRef>(ref TRef reference, int relativeOffset) where TRef : class
        {
            ref RawData raw = ref UnsafeUtility.As<TRef, RawData>(ref reference);
            fixed (byte* dataStart = &raw.Data)
            {
                return UnsafeUtility.AsRef<AttributeData>(dataStart + relativeOffset);
            }
        }

        // 고정이 풀린 뒤의 주소라 값 비교에만 쓴다
        private static long DataStart<TRef>(ref TRef reference) where TRef : class
        {
            ref RawData raw = ref UnsafeUtility.As<TRef, RawData>(ref reference);
            fixed (byte* dataStart = &raw.Data)
            {
                return (long)dataStart;
            }
        }

        private static void WriteFixedCurrent(AttributeSet attributeSet, int relativeOffset, float value)
        {
            ref RawData raw = ref UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                UnsafeUtility.AsRef<AttributeData>(dataStart + relativeOffset).SetCurrentValue(value);
            }
        }

        // AttributeSet.GetAttributeDataFields와 달리 가려진 필드·직렬화되지 않는 필드까지 전부 모은다(정확도 검사 대상)
        private static List<FieldInfo> CollectAttributeFields(Type setType)
        {
            var fields = new List<FieldInfo>();
            for (Type type = setType; type != null && type != typeof(AttributeSet); type = type.BaseType)
            {
                fields.AddRange(type.GetFields(DeclaredInstanceFlags).Where(field => field.FieldType == typeof(AttributeData)));
            }
            return fields;
        }

        private static FieldInfo FindFieldInHierarchy(Type setType, string fieldName)
        {
            for (Type type = setType; type != null && type != typeof(AttributeSet); type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, DeclaredInstanceFlags);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }

        private static bool OffsetsDisjoint(int[] offsets)
        {
            int[] sorted = offsets.OrderBy(offset => offset).ToArray();
            if (sorted.Length > 0 && sorted[0] < RawDataOffset)
            {
                return false;
            }
            for (int i = 1; i < sorted.Length; i++)
            {
                if (sorted[i] - sorted[i - 1] < AttributeDataSize)
                {
                    return false;
                }
            }
            return true;
        }

        private static void CollectStateMismatches(AttributeSet attributeSet, List<FieldInfo> fields, AttributeData[] expected, string context, List<string> mismatches)
        {
            for (int j = 0; j < fields.Count; j++)
            {
                if (!Same((AttributeData)fields[j].GetValue(attributeSet), expected[j]))
                {
                    mismatches.Add($"{context}: {FieldLabel(fields[j])}");
                }
            }
        }

        private static void Report(Action<string, bool> check, string label, List<string> mismatches)
        {
            check(label, mismatches.Count == 0);
            if (mismatches.Count > 0)
            {
                Debug.LogError($"[FieldOffset ACC] {label} 불일치: {string.Join(" / ", mismatches.Take(10))}");
            }
        }

        private static bool Same(AttributeData left, AttributeData right)
        {
            return left.GetBaseValue() == right.GetBaseValue() && left.GetCurrentValue() == right.GetCurrentValue();
        }

        private static string FieldLabel(FieldInfo field)
        {
            return $"{field.DeclaringType?.Name}.{field.Name}";
        }

        private class LocalBaseSet : AttributeSet
        {
            public const string SerializedPrivateFieldName = nameof(_serializedPrivateData);

            public static AttributeData StaticData;

            public AttributeData PublicData;
            private byte _padByte;
            internal AttributeData InternalData;
            private double _padDouble;
            protected AttributeData ProtectedData;
            private string _padString;
            protected internal AttributeData ProtectedInternalData;
            private object _padObject;
            private protected AttributeData PrivateProtectedData;
            [SerializeField] private AttributeData _serializedPrivateData;
            [NonSerialized] public AttributeData NonSerializedPublicData;
            [NonSerialized] private AttributeData _nonSerializedPrivateData;
            public readonly AttributeData ReadonlyData;
            private int _padInt;

            public AttributeData AutoPropertyData { get; set; }
        }

        private class LocalDerivedSet : LocalBaseSet, ILocalMarker
        {
            public new AttributeData PublicData;
            public AttributeData DerivedPublicData;
            private byte _derivedPadByte;
            internal AttributeData DerivedInternalData;
            protected AttributeData DerivedProtectedData;
            private protected AttributeData DerivedPrivateProtectedData;
            [SerializeField] private AttributeData _derivedSerializedPrivateData;
            [NonSerialized] private AttributeData _derivedNonSerializedPrivateData;
        }

        private sealed class LocalSiblingSet : LocalBaseSet
        {
            public AttributeData SiblingPublicData;
            private string _siblingPadString;
            [SerializeField] private AttributeData _siblingSerializedPrivateData;
        }

        private sealed class LocalGrandChildSet : LocalDerivedSet
        {
            public AttributeData GrandChildPublicData;
            private double _grandChildPadDouble;
            [SerializeField] private AttributeData _grandChildSerializedPrivateData;
        }

        private class CrossAssemblyDerivedSet : ExternalBaseSet
        {
            public new AttributeData PublicData;
            public AttributeData CrossPublicData;
            private object _crossPadObject;
            protected AttributeData CrossProtectedData;
            [SerializeField] private AttributeData _crossSerializedPrivateData;
            [NonSerialized] private AttributeData _crossNonSerializedPrivateData;
        }

        private sealed class GenericSet<T> : AttributeSet
        {
            public T Before;
            public AttributeData Data;
            public T After;
            [SerializeField] private AttributeData _privateData;
            private T _privateGeneric;
        }

        private sealed class GenericDerivedSet<T> : LocalBaseSet
        {
            public T Extra;
            public AttributeData GenericData;
            [SerializeField] private AttributeData _genericPrivateData;
        }
    }
}
