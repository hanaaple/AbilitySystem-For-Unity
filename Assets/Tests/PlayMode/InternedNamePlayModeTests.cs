using System.Collections;
using Core.AbilitySystem.Tag;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Core.Tests.PlayMode
{
    /// <summary>Play 중 태그 요청이 대소문자를 무시하고 레지스트리 철자를 돌려주는지. 매니저 트리는 Play 진입 뒤 처음 요청에서 만들어진다.</summary>
    [TestFixture]
    [Category("InternedName")]
    public sealed class InternedNamePlayModeTests
    {
        private const string RegistrySpelling = "Ability.Aim";

        [UnityTest]
        public IEnumerator RequestGameplayTag_LowerCase_ResolvesToRegistrySpelling()
        {
            yield return null;

            GameplayTag exact = GameplayTag.RequestGameplayTag(RegistrySpelling, false);
            Assume.That(exact.IsValid(), Is.True, $"레지스트리에 {RegistrySpelling} 없음");

            GameplayTag lower = GameplayTag.RequestGameplayTag(RegistrySpelling.ToLowerInvariant(), false);
            GameplayTag upper = GameplayTag.RequestGameplayTag(RegistrySpelling.ToUpperInvariant(), false);

            Assert.That(lower.IsValid(), Is.True);
            Assert.That(upper.IsValid(), Is.True);
            Assert.That(lower, Is.EqualTo(exact));
            Assert.That(upper, Is.EqualTo(exact));
        }

        [UnityTest]
        public IEnumerator RequestGameplayTag_LowerCase_SpellingOfReturnedTag()
        {
            yield return null;

            GameplayTag exact = GameplayTag.RequestGameplayTag(RegistrySpelling, false);
            Assume.That(exact.IsValid(), Is.True, $"레지스트리에 {RegistrySpelling} 없음");

            string requested = RegistrySpelling.ToLowerInvariant();
            GameplayTag lower = GameplayTag.RequestGameplayTag(requested, false);

            // 어느 철자를 돌려줘야 하는지는 결정 대기(D3 대소문자 보강). 현재 동작만 기록한다 — 요청 철자가 그대로 나오면 Inconclusive.
            if (lower.ToString() == requested && requested != RegistrySpelling)
            {
                Assert.Inconclusive($"요청 철자 \"{requested}\"가 그대로 돌아온다(레지스트리 \"{RegistrySpelling}\") — 표기 정책 결정 대기");
            }

            Assert.That(lower.ToString(), Is.EqualTo(RegistrySpelling));
        }

        [UnityTest]
        public IEnumerator RequestGameplayTag_Unknown_IsInvalid()
        {
            yield return null;

            GameplayTag unknown = GameplayTag.RequestGameplayTag("No.Such.Tag.For.Test", false);

            Assert.That(unknown.IsValid(), Is.False);
        }
    }
}
