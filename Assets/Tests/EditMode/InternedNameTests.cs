using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using Core.Common;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// InternedName 명세 검증. P1 동등성(대소문자 무시) · P2 표기 보존 · P3 해시 계약 · P4 역직렬화 경로 · P5 접기 규칙 일치 · P6 스레드.
    /// F 계열은 실제 데이터(레지스트리·native·에셋)의 대소문자 충돌을 보고만 한다 — 수정은 등록 차단 작업에서.
    /// </summary>
    [TestFixture]
    [Category("InternedName")]
    public sealed class InternedNameTests
    {
        private const string TempFolder = "Assets/Tests/_Temp";
        private const string ReloadAssetPath = TempFolder + "/InternedName_Reload.asset";

        // 다른 테스트가 먼저 같은 철자를 캐시에 넣어도 선착순 검사가 흐려지지 않게 테스트마다 새 이름을 쓴다.
        private static string Salt()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        [OneTimeSetUp]
        public void EnsureTempFolder()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets/Tests", "_Temp");
            }
        }

        [OneTimeTearDown]
        public void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }

        // ---------------------------------------------------------------- B1 동등성 (P1)

        [TestCase("Ability.Fire", "Ability.Fire", true)]
        [TestCase("Ability.Fire", "ABILITY.FIRE", true)]
        [TestCase("Ability.Fire", "ability.fire", true)]
        [TestCase("Ability.Fire", "aBiLiTy.FiRe", true)]
        [TestCase("Ability.Fire", "Ability.Fire2", false)]
        [TestCase("Ability.Fire", "Ability.Fire ", false)]
        [TestCase("Ability.Fire", "Ability_Fire", false)]
        [TestCase("Ability.Fire", "Ability", false)]
        [TestCase("", null, true)]
        [TestCase(null, null, true)]
        [TestCase("", " ", false)]
        public void Equals_MatchesOrdinalIgnoreCase(string left, string right, bool expected)
        {
            var a = new InternedName(left);
            var b = new InternedName(right);

            Assert.That(a.Equals(b), Is.EqualTo(expected), "a.Equals(b)");
            Assert.That(b.Equals(a), Is.EqualTo(expected), "b.Equals(a)");
            Assert.That(a.Equals(b), Is.EqualTo(string.Equals(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase)), "OrdinalIgnoreCase와 일치");
        }

        [Test]
        public void Equals_DefaultAndEmptyAndNull_AreAllEqual()
        {
            InternedName byDefault = default;
            var fromEmpty = new InternedName("");
            var fromNull = new InternedName(null);

            Assert.That(byDefault.Equals(fromEmpty), Is.True);
            Assert.That(byDefault.Equals(fromNull), Is.True);
            Assert.That(fromEmpty.Equals(fromNull), Is.True);
            Assert.That(byDefault.IsNullOrEmpty(), Is.True);
            Assert.That(fromEmpty.IsNullOrEmpty(), Is.True);
            Assert.That(fromNull.IsNullOrEmpty(), Is.True);
        }

        [Test]
        public void EqualsObject_OtherType_IsFalse()
        {
            var name = new InternedName("Ability.Fire");

            Assert.That(name.Equals((object)"Ability.Fire"), Is.False);
            Assert.That(name.Equals(null), Is.False);
            Assert.That(name.Equals((object)new InternedName("ABILITY.FIRE")), Is.True);
        }

        [Test]
        public void DictionaryKey_LookupIgnoresCase()
        {
            string salt = Salt();
            var map = new Dictionary<InternedName, int>
            {
                [new InternedName($"Key.{salt}.Alpha")] = 1,
            };

            Assert.That(map.TryGetValue(new InternedName($"key.{salt}.ALPHA"), out int value), Is.True);
            Assert.That(value, Is.EqualTo(1));
            Assert.That(map.ContainsKey(new InternedName($"Key.{salt}.Beta")), Is.False);
        }

        [Test]
        public void HashSet_CaseVariants_CollapseToOne()
        {
            string salt = Salt();
            var set = new HashSet<InternedName>
            {
                new InternedName($"Set.{salt}.Node"),
                new InternedName($"set.{salt}.NODE"),
                new InternedName($"SET.{salt}.node"),
            };

            Assert.That(set.Count, Is.EqualTo(1));
        }

        [Test]
        public void Equals_RandomPairs_MatchOrdinalIgnoreCase()
        {
            const int pairCount = 10000;
            const string pool = "aAbBcCdDeEfFxXyYzZ._0123456789";
            var random = new System.Random(20261028);
            var builder = new StringBuilder();

            for (int i = 0; i < pairCount; i++)
            {
                string left = RandomName(random, builder, pool);
                // 절반은 대소문자만 바꾼 쌍, 절반은 무관한 쌍 — 참·거짓 양쪽을 고르게 덮는다.
                string right = (i & 1) == 0 ? MutateCase(random, left) : RandomName(random, builder, pool);

                bool expected = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
                bool actual = new InternedName(left).Equals(new InternedName(right));
                Assert.That(actual, Is.EqualTo(expected), $"\"{left}\" vs \"{right}\"");
            }
        }

        private static string RandomName(System.Random random, StringBuilder builder, string pool)
        {
            builder.Clear();
            int length = random.Next(1, 12);
            for (int i = 0; i < length; i++)
            {
                builder.Append(pool[random.Next(pool.Length)]);
            }

            return builder.ToString();
        }

        private static string MutateCase(System.Random random, string source)
        {
            char[] chars = source.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (random.Next(2) == 0)
                {
                    chars[i] = char.IsUpper(chars[i]) ? char.ToLowerInvariant(chars[i]) : char.ToUpperInvariant(chars[i]);
                }
            }

            return new string(chars);
        }

        // ---------------------------------------------------------------- B2 표기 보존 (P2)

        [Test]
        public void ToString_PreservesSpelling()
        {
            string spelling = $"AbILity.{Salt()}.FiRe";

            Assert.That(new InternedName(spelling).ToString(), Is.EqualTo(spelling).Using((IComparer)StringComparer.Ordinal));
        }

        [Test]
        public void ToString_IsIndependentOfCreationOrder()
        {
            string salt = Salt();
            string upper = $"ORDER.{salt}.FIRE";
            string lower = $"order.{salt}.fire";

            var first = new InternedName(upper);
            var second = new InternedName(lower);

            Assert.That(first.ToString(), Is.EqualTo(upper).Using((IComparer)StringComparer.Ordinal));
            Assert.That(second.ToString(), Is.EqualTo(lower).Using((IComparer)StringComparer.Ordinal), "뒤에 만든 쪽이 먼저 들어온 철자로 바뀌면 안 된다");
            Assert.That(first.Equals(second), Is.True);
        }

        [Test]
        public void ToString_EmptyAndNull_ReturnNull()
        {
            Assert.That(new InternedName("").ToString(), Is.Null);
            Assert.That(new InternedName(null).ToString(), Is.Null);
            Assert.That(default(InternedName).ToString(), Is.Null);
        }

        // ---------------------------------------------------------------- B3 해시 (P3)

        [Test]
        public void GetHashCode_EqualNames_HaveSameHash()
        {
            string salt = Salt();
            var a = new InternedName($"Hash.{salt}.Node");
            var b = new InternedName($"HASH.{salt}.NODE");
            var c = new InternedName($"hash.{salt}.node");

            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.GetHashCode(), Is.EqualTo(c.GetHashCode()));
        }

        [Test]
        public void GetHashCode_Empty_IsZero()
        {
            Assert.That(default(InternedName).GetHashCode(), Is.EqualTo(0));
            Assert.That(new InternedName("").GetHashCode(), Is.EqualTo(0));
            Assert.That(new InternedName(null).GetHashCode(), Is.EqualTo(0));
        }

        [Test]
        public void GetHashCode_IsStableAcrossCalls()
        {
            var name = new InternedName($"Stable.{Salt()}");

            Assert.That(name.GetHashCode(), Is.EqualTo(name.GetHashCode()));
        }

        [Test]
        public void GetHashCode_RegistryNames_ReportCollisions()
        {
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            Assume.That(registry, Is.Not.Null, "레지스트리 에셋 없음");

            var seen = new Dictionary<int, string>();
            int collisions = 0;
            foreach (string tag in registry.Tags)
            {
                int hash = new InternedName(tag).GetHashCode();
                if (seen.TryGetValue(hash, out string other) && !string.Equals(other, tag, StringComparison.OrdinalIgnoreCase))
                {
                    collisions++;
                    TestContext.WriteLine($"해시 충돌: \"{other}\" / \"{tag}\" = {hash}");
                }
                else
                {
                    seen[hash] = tag;
                }
            }

            TestContext.WriteLine($"레지스트리 {registry.Tags.Count}개 중 해시 충돌 {collisions}개 (참고용, 합격 기준 없음)");
        }

        // ---------------------------------------------------------------- B4 접기 규칙 일치 (P5)

        [Test]
        public void CaseFolding_EveryBmpChar_UpperInvariantAgreesWithOrdinalIgnoreCase()
        {
            // 정본은 ToUpperInvariant로 접고 캐시 사전은 OrdinalIgnoreCase로 비교한다. 두 규칙이 어긋나는 글자가 있으면
            // 같은 이름이 정본을 두 개 가질 수 있으므로, 문서를 믿지 않고 실행 중인 런타임에서 BMP 전수를 잰다.
            var upperMismatches = new List<string>();
            var lowerMismatches = new List<string>();
            var lengthChanges = new List<string>();

            for (int codePoint = 0; codePoint <= 0xFFFF; codePoint++)
            {
                if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
                {
                    continue;
                }

                string s = ((char)codePoint).ToString();
                string upper = s.ToUpperInvariant();
                string lower = s.ToLowerInvariant();

                if (!string.Equals(s, upper, StringComparison.OrdinalIgnoreCase))
                {
                    upperMismatches.Add($"U+{codePoint:X4}");
                }

                bool ordinalSaysEqual = string.Equals(s, lower, StringComparison.OrdinalIgnoreCase);
                bool foldSaysEqual = string.Equals(upper, lower.ToUpperInvariant(), StringComparison.Ordinal);
                if (ordinalSaysEqual != foldSaysEqual)
                {
                    lowerMismatches.Add($"U+{codePoint:X4}");
                }

                if (upper.Length != 1)
                {
                    lengthChanges.Add($"U+{codePoint:X4}->{upper.Length}");
                }
            }

            TestContext.WriteLine($"ToUpperInvariant 길이 변화 글자: {lengthChanges.Count}개 {string.Join(" ", lengthChanges)}");
            Assert.That(upperMismatches, Is.Empty, "s와 s.ToUpperInvariant()를 OrdinalIgnoreCase가 다르다고 보는 글자");
            Assert.That(lowerMismatches, Is.Empty, "소문자 짝에 대해 OrdinalIgnoreCase와 ToUpperInvariant 접기의 판정이 다른 글자");
        }

        [TestCase("ı", "i")]
        [TestCase("ı", "I")]
        [TestCase("i", "I")]
        [TestCase("ß", "SS")]
        [TestCase("ﬁ", "FI")]
        [TestCase("Σ", "σ")]
        [TestCase("Σ", "ς")]
        [TestCase("é", "é")]
        [TestCase("K", "k")]
        public void Equals_KnownPitfalls_MatchOrdinalIgnoreCase(string left, string right)
        {
            bool expected = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            bool actual = new InternedName(left).Equals(new InternedName(right));

            TestContext.WriteLine($"{Describe(left)} vs {Describe(right)}: OrdinalIgnoreCase={expected} InternedName={actual}");
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static string Describe(string s)
        {
            var builder = new StringBuilder();
            foreach (char c in s)
            {
                builder.Append($"U+{(int)c:X4} ");
            }

            return builder.ToString().TrimEnd();
        }

        [Test]
        public void Equals_UnderTurkishCulture_IsUnchanged()
        {
            CultureInfo saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                string salt = Salt();

                Assert.That(new InternedName($"Ability.{salt}.Fire").Equals(new InternedName($"ABILITY.{salt}.FIRE")), Is.True);
                Assert.That(new InternedName("i").Equals(new InternedName("I")), Is.EqualTo(string.Equals("i", "I", StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        // ---------------------------------------------------------------- B5 스레드 (P6)

        [Test]
        public void Constructor_ConcurrentCreation_KeepsEquality()
        {
            const int threadCount = 4;
            const int nameCount = 1000;
            const int rounds = 10;
            string salt = Salt();
            var names = new string[nameCount];
            for (int i = 0; i < nameCount; i++)
            {
                names[i] = $"Thread.{salt}.Node{i}";
            }

            var exceptions = new List<Exception>();
            var results = new InternedName[threadCount, nameCount];

            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, thread =>
            {
                try
                {
                    for (int round = 0; round < rounds; round++)
                    {
                        for (int i = 0; i < nameCount; i++)
                        {
                            // 스레드마다 다른 대소문자 패턴으로 넣어 같은 이름이 동시에 여러 철자로 들어오는 상황을 만든다.
                            string spelling = (thread + round) % 2 == 0 ? names[i].ToUpperInvariant() : names[i].ToLowerInvariant();
                            results[thread, i] = new InternedName(spelling);
                        }
                    }
                }
                catch (Exception exception)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(exception);
                    }
                }
            });

            Assert.That(exceptions, Is.Empty);
            for (int i = 0; i < nameCount; i++)
            {
                var expected = new InternedName(names[i]);
                for (int thread = 0; thread < threadCount; thread++)
                {
                    Assert.That(results[thread, i].Equals(expected), Is.True, $"thread {thread} name {i}");
                }
            }
        }

        // ---------------------------------------------------------------- C 직렬화 (P4)

        [Test]
        public void AssetRoundTrip_PreservesSpellingAndRestoresComparison()
        {
            string path = TempFolder + "/InternedName_RoundTrip.asset";
            var asset = ScriptableObject.CreateInstance<InternedNameHolderAsset>();
            asset.First = new InternedName("Mixed.Case");
            asset.Second = new InternedName("MIXED.CASE");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            string yaml = File.ReadAllText(path);
            Assert.That(yaml, Does.Contain("name: Mixed.Case"), "표기가 저장돼야 한다");
            Assert.That(yaml, Does.Contain("name: MIXED.CASE"), "두 번째 철자도 그대로 저장돼야 한다");
            Assert.That(yaml, Does.Not.Contain("_comparisonName"), "비교 정본은 저장되지 않는다");

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<InternedNameHolderAsset>(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.First.Equals(loaded.Second), Is.True, "역직렬화 뒤에도 대소문자 무시 동등성이 성립해야 한다");
            Assert.That(loaded.First.ToString(), Is.EqualTo("Mixed.Case"));
            Assert.That(loaded.Second.ToString(), Is.EqualTo("MIXED.CASE"));
            Assert.That(loaded.First.Equals(new InternedName("mixed.case")), Is.True);
            Assert.That(loaded.First.GetHashCode(), Is.EqualTo(new InternedName("mixed.case").GetHashCode()));
        }

        [Serializable]
        private sealed class NameJson
        {
            public InternedName value;
            public InternedName[] names;
        }

        [Test]
        public void JsonRoundTrip_RestoresComparison()
        {
            var source = new NameJson
            {
                value = new InternedName("Json.Case"),
                names = new[] { new InternedName("JSON.CASE"), new InternedName("json.case") },
            };

            string json = JsonUtility.ToJson(source);
            Assert.That(json, Does.Contain("\"name\":\"Json.Case\""));

            var restored = JsonUtility.FromJson<NameJson>(json);

            Assert.That(restored.value.IsNullOrEmpty(), Is.False);
            Assert.That(restored.value.Equals(source.value), Is.True);
            Assert.That(restored.value.GetHashCode(), Is.EqualTo(source.value.GetHashCode()));
            Assert.That(restored.value.Equals(restored.names[0]), Is.True);
            Assert.That(restored.value.Equals(restored.names[1]), Is.True);
            Assert.That(restored.names[1].ToString(), Is.EqualTo("json.case"));
        }

        [Test]
        public void JsonRoundTrip_EmptyName_IsNullOrEmpty()
        {
            var restored = JsonUtility.FromJson<NameJson>("{\"value\":{\"name\":\"\"},\"names\":[]}");

            Assert.That(restored.value.IsNullOrEmpty(), Is.True);
            Assert.That(restored.value.ToString(), Is.Null);
            Assert.That(restored.value.GetHashCode(), Is.EqualTo(0));
            Assert.That(restored.value.Equals(default(InternedName)), Is.True);
        }

        [UnityTest]
        public IEnumerator DomainReload_AssetStaysEqualToFreshName()
        {
            var asset = ScriptableObject.CreateInstance<InternedNameHolderAsset>();
            asset.First = new InternedName("Reload.Case");
            asset.Second = new InternedName("RELOAD.CASE");
            AssetDatabase.CreateAsset(asset, ReloadAssetPath);
            AssetDatabase.SaveAssets();

            TestContext.WriteLine($"enterPlayModeOptionsEnabled={EditorSettings.enterPlayModeOptionsEnabled} options={EditorSettings.enterPlayModeOptions}");

            // Play 진입·종료로 도메인을 다시 올린다 — 정본 사전과 인턴 풀이 비워진 뒤 다시 채워져도 같은 결과여야 한다.
            yield return new EnterPlayMode();
            yield return new ExitPlayMode();

            var loaded = AssetDatabase.LoadAssetAtPath<InternedNameHolderAsset>(ReloadAssetPath);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.First.Equals(loaded.Second), Is.True);
            Assert.That(loaded.First.Equals(new InternedName("reload.case")), Is.True);
            Assert.That(loaded.First.ToString(), Is.EqualTo("Reload.Case"));
        }

        // ---------------------------------------------------------------- F 실데이터 충돌 점검 (보고만)

        [Test]
        public void Registry_HasNoCaseOnlyDuplicates()
        {
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            Assume.That(registry, Is.Not.Null, "레지스트리 에셋 없음");

            List<string> duplicates = FindCaseOnlyDuplicates(registry.Tags, "registry");
            ReportDuplicates("레지스트리", duplicates);
        }

        [Test]
        public void RegistryAndNativeTags_HaveNoCaseOnlyDuplicates()
        {
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            Assume.That(registry, Is.Not.Null, "레지스트리 에셋 없음");

            var labeled = new List<(string name, string origin)>();
            foreach (string tag in registry.Tags)
            {
                labeled.Add((tag, "registry"));
            }

            foreach (NativeGameplayTag nativeTag in NativeGameplayTag.GetRegisteredNativeTags())
            {
                labeled.Add((nativeTag.TagName.ToString(), "native"));
            }

            List<string> duplicates = FindCaseOnlyDuplicates(labeled);
            ReportDuplicates("레지스트리+native", duplicates);
        }

        [Test]
        public void Assets_TagSpellingsMatchRegistry()
        {
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
            Assume.That(registry, Is.Not.Null, "레지스트리 에셋 없음");

            var exact = new HashSet<string>(registry.Tags, StringComparer.Ordinal);
            var ignoreCase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string tag in registry.Tags)
            {
                ignoreCase[tag] = tag;
            }

            var caseMismatches = new List<string>();
            var unknown = new List<string>();
            int scanned = 0;
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(GameplayEffectAsset)} t:{nameof(GameplayAbilityAsset)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(asset);
                SerializedProperty iterator = serialized.GetIterator();
                while (iterator.NextVisible(true))
                {
                    // GameplayTag.name(InternedName) → InternedName.name 이라 태그 이름 문자열은 항상 "…name.name"으로 끝난다.
                    if (iterator.propertyType != SerializedPropertyType.String || !iterator.propertyPath.EndsWith(".name.name", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string value = iterator.stringValue;
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    scanned++;
                    if (exact.Contains(value))
                    {
                        continue;
                    }

                    if (ignoreCase.TryGetValue(value, out string registered))
                    {
                        caseMismatches.Add($"{path} :: {iterator.propertyPath} = \"{value}\" (레지스트리 \"{registered}\")");
                    }
                    else
                    {
                        unknown.Add($"{path} :: {iterator.propertyPath} = \"{value}\"");
                    }
                }
            }

            TestContext.WriteLine($"에셋 태그 이름 {scanned}개 검사 — 대소문자만 다른 것 {caseMismatches.Count}개, 레지스트리에 없는 것 {unknown.Count}개");
            foreach (string line in unknown)
            {
                TestContext.WriteLine("  레지스트리 없음: " + line);
            }

            ReportDuplicates("에셋 철자", caseMismatches);
        }

        private static List<string> FindCaseOnlyDuplicates(IEnumerable<string> names, string origin)
        {
            var labeled = new List<(string name, string origin)>();
            foreach (string name in names)
            {
                labeled.Add((name, origin));
            }

            return FindCaseOnlyDuplicates(labeled);
        }

        private static List<string> FindCaseOnlyDuplicates(List<(string name, string origin)> labeled)
        {
            var groups = new Dictionary<string, List<(string name, string origin)>>(StringComparer.OrdinalIgnoreCase);
            foreach ((string name, string origin) entry in labeled)
            {
                if (!groups.TryGetValue(entry.name, out List<(string name, string origin)> group))
                {
                    group = new List<(string name, string origin)>();
                    groups.Add(entry.name, group);
                }

                group.Add(entry);
            }

            var duplicates = new List<string>();
            foreach (List<(string name, string origin)> group in groups.Values)
            {
                var spellings = new HashSet<string>(StringComparer.Ordinal);
                foreach ((string name, string origin) entry in group)
                {
                    spellings.Add(entry.name);
                }

                if (spellings.Count > 1)
                {
                    var parts = new List<string>();
                    foreach ((string name, string origin) entry in group)
                    {
                        parts.Add($"\"{entry.name}\"({entry.origin})");
                    }

                    duplicates.Add(string.Join(" / ", parts));
                }
            }

            return duplicates;
        }

        private static void ReportDuplicates(string subject, List<string> duplicates)
        {
            foreach (string line in duplicates)
            {
                TestContext.WriteLine($"  {subject} 대소문자 충돌: {line}");
            }

            if (duplicates.Count > 0)
            {
                // 데이터 문제라 코드 실패로 보지 않는다 — 등록 차단 작업의 입력으로 남긴다.
                Assert.Inconclusive($"{subject}에 대소문자만 다른 이름 {duplicates.Count}건 — 출력 참조");
            }
        }
    }
}
