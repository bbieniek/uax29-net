#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Bbieniek.Uax29;

namespace Bbieniek.Uax29.Tests
{
    [TestFixture]
    public class WordTokenEnumeratorTests
    {
        [Test]
        public void EmptySpan_NoTokens()
        {
            var count = 0;
            foreach (var token in WordBreakTokenizer.EnumerateWords(ReadOnlySpan<char>.Empty))
                count++;
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void SingleWord()
        {
            var tokens = new List<string>();
            foreach (var token in WordBreakTokenizer.EnumerateWords("hello".AsSpan()))
                tokens.Add(token.Span.ToString());
            Assert.That(tokens, Is.EqualTo(["hello"]));
        }

        [Test]
        public void MatchesTokenize_SimpleText()
        {
            var text = "hello, world! 1,000 self-aware (test)";
            var expected = WordBreakTokenizer.TokenizeToStrings(text);

            var actual = new List<string>();
            foreach (var token in WordBreakTokenizer.EnumerateWords(text.AsSpan()))
                actual.Add(token.Span.ToString());

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void MatchesTokenize_Emoji()
        {
            var text = "\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F468";
            var expected = WordBreakTokenizer.TokenizeToStrings(text);

            var actual = new List<string>();
            foreach (var token in WordBreakTokenizer.EnumerateWords(text.AsSpan()))
                actual.Add(token.Span.ToString());

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void IsWord_MatchesTokenize()
        {
            var text = "hello world 123";
            var expectedSpans = WordBreakTokenizer.Tokenize(text);

            var idx = 0;
            foreach (var token in WordBreakTokenizer.EnumerateWords(text.AsSpan()))
            {
                Assert.That(token.IsWord, Is.EqualTo(expectedSpans[idx].IsWord),
                    $"IsWord mismatch at token {idx}: '{token.Span.ToString()}'");
                idx++;
            }
            Assert.That(idx, Is.EqualTo(expectedSpans.Count));
        }

        [Test]
        public void Roundtrip()
        {
            var text = "hello, world! caf\u00e9 \U0001F1FA\U0001F1F8";
            var sb = new StringBuilder();
            foreach (var token in WordBreakTokenizer.EnumerateWords(text.AsSpan()))
                sb.Append(token.Span);
            Assert.That(sb.ToString(), Is.EqualTo(text));
        }

        [Test]
        public void Start_And_Length_Correct()
        {
            var text = "ab,cd";
            var tokens = new List<(int Start, int Length)>();
            foreach (var token in WordBreakTokenizer.EnumerateWords(text.AsSpan()))
                tokens.Add((token.Start, token.Length));

            Assert.That(tokens[0], Is.EqualTo((0, 2)));
            Assert.That(tokens[1], Is.EqualTo((2, 1)));
            Assert.That(tokens[2], Is.EqualTo((3, 2)));
        }
    }
}
#endif
