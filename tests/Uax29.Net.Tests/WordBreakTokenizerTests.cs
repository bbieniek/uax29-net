using System.Collections.Generic;
using NUnit.Framework;
using Uax29.Net;

namespace Uax29.Net.Tests
{
    [TestFixture]
    public class WordBreakTokenizerTests
    {
        private static void AssertTokens(string input, params string[] expected)
        {
            var actual = WordBreakTokenizer.TokenizeToStrings(input);
            Assert.That(actual, Is.EqualTo(new List<string>(expected)),
                $"Tokenization mismatch for input: \"{input}\"");
        }

        // --- Basic / null / empty ---

        [Test]
        public void Null_ReturnsEmpty()
            => Assert.That(WordBreakTokenizer.Tokenize(null!), Is.Empty);

        [Test]
        public void Empty_ReturnsEmpty()
            => Assert.That(WordBreakTokenizer.Tokenize(""), Is.Empty);

        [Test]
        public void SingleWord()
            => AssertTokens("hello", "hello");

        [Test]
        public void SingleNumber()
            => AssertTokens("12345", "12345");

        [Test]
        public void SinglePunctuation()
            => AssertTokens("!", "!");

        // --- Comma separation (R-verified) ---

        [Test]
        public void CommaSeparatedWords()
            => AssertTokens("hello,world", "hello", ",", "world");

        [Test]
        public void CommaSeparatedWithSpace()
            => AssertTokens("New York,London", "New", " ", "York", ",", "London");

        [Test]
        public void MultipleCommas()
            => AssertTokens("a,b,c,d", "a", ",", "b", ",", "c", ",", "d");

        // --- Hyphens (R-verified) ---

        [Test]
        public void HyphenBetweenLetters_Preserved()
            => AssertTokens("self-aware robot", "self-aware", " ", "robot");

        [Test]
        public void HyphenBetweenDigits_Preserved()
            => AssertTokens("id 20010101-1234", "id", " ", "20010101-1234");

        [Test]
        public void HyphenInPhoneNumber()
            => AssertTokens("call 08-555 12 34", "call", " ", "08-555", " ", "12", " ", "34");

        [Test]
        public void WellKnown_HyphenPreserved()
            => AssertTokens("well-known fact", "well-known", " ", "fact");

        // --- Punctuation boundaries (R-verified) ---

        [Test]
        public void Parentheses()
            => AssertTokens("(hello)", "(", "hello", ")");

        [Test]
        public void ColonJoins_MidLetter()
            => AssertTokens("key:value", "key:value");

        [Test]
        public void PipeSeparator()
            => AssertTokens("price|100", "price", "|", "100");

        [Test]
        public void TrailingPeriod()
            => AssertTokens("end.", "end", ".");

        // --- MidNum / MidLetter / MidNumLet rules (R-verified) ---

        [Test]
        public void CommaInNumber_MidNum()
            => AssertTokens("1,000,000.50 dollars", "1,000,000.50", " ", "dollars");

        [Test]
        public void PeriodBetweenLetters_MidLetter()
            => AssertTokens("e.g. example", "e.g", ".", " ", "example");

        [Test]
        public void Contraction_Apostrophe()
            => AssertTokens("don't stop", "don't", " ", "stop");

        [Test]
        public void RightSingleQuote_MidNumLet()
            => AssertTokens("a\u2019t", "a\u2019t");

        // --- Whitespace handling (R-verified) ---

        [Test]
        public void MultipleSpaces_Grouped()
            => AssertTokens("hello   world", "hello", "   ", "world");

        [Test]
        public void Tab_Preserved()
            => AssertTokens("test\ttab", "test", "\t", "tab");

        [Test]
        public void NonBreakingSpace_IsSeparator()
            => AssertTokens("\u00a0test", "\u00a0", "test");

        [Test]
        public void EmSpace_IsSeparator()
            => AssertTokens("hello\u2003world", "hello", "\u2003", "world");

        // --- Newlines (R-verified) ---

        [Test]
        public void NextLine_Breaks()
            => AssertTokens("test\u0085next", "test", "\u0085", "next");

        // --- Unicode scripts (R-verified) ---

        [Test]
        public void GreekLetters()
            => AssertTokens("\u0391\u0392\u0393 test", "\u0391\u0392\u0393", " ", "test");

        [Test]
        public void CyrillicLetters()
            => AssertTokens("\u0411\u0412\u0413 test", "\u0411\u0412\u0413", " ", "test");

        [Test]
        public void HebrewLetters()
            => AssertTokens("\u05d0\u05d1\u05d2", "\u05d0\u05d1\u05d2");

        [Test]
        public void AccentedWords()
            => AssertTokens("caf\u00e9 na\u00efve", "caf\u00e9", " ", "na\u00efve");

        // --- Latin-1 fast path edge cases (R-verified) ---

        [Test]
        public void MultiplicationSign_Splits()
            => AssertTokens("3\u00d74=12", "3", "\u00d7", "4", "=", "12");

        [Test]
        public void ZWJ_AttachesToWord()
            => AssertTokens("a\u200db", "a\u200db");

        [Test]
        public void RegionalIndicatorFlag_StaysTogether_QuantedaVerified()
            => AssertTokens("\U0001F1FA\U0001F1F8", "\U0001F1FA\U0001F1F8");

        [Test]
        public void SupplementaryLetterNumber_WithinWord_QuantedaVerified()
            => AssertTokens("a\U00010140b", "a\U00010140b");

        [Test]
        public void SupplementaryLetterNumber_Repeated_QuantedaVerified()
            => AssertTokens("\U00010140\U00010140", "\U00010140\U00010140");

        [Test]
        public void CopyrightZwJCopyright_StaysTogether_QuantedaVerified()
            => AssertTokens("\u00A9\u200D\u00A9", "\u00A9\u200D\u00A9");

        [Test]
        public void RegionalIndicators_FourCodepoints_SplitIntoFlagPairs_QuantedaVerified()
            => AssertTokens("\U0001F1FA\U0001F1F8\U0001F1E8\U0001F1E6", "\U0001F1FA\U0001F1F8", "\U0001F1E8\U0001F1E6");

        [Test]
        public void RegionalIndicators_ThreeCodepoints_LastSeparates_QuantedaVerified()
            => AssertTokens("\U0001F1FA\U0001F1F8\U0001F1FA", "\U0001F1FA\U0001F1F8", "\U0001F1FA");

        [Test]
        public void KissEmojiZwJSequence_StaysTogether_QuantedaVerified()
            => AssertTokens("\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F468", "\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F468");

        [Test]
        public void RainbowFlagZwJSequence_StaysTogether_QuantedaVerified()
            => AssertTokens("\U0001F3F3\uFE0F\u200D\U0001F308", "\U0001F3F3\uFE0F\u200D\U0001F308");

        // --- TokenSpan properties ---

        [Test]
        public void TokenSpan_IsWord_TrueForWords()
        {
            var spans = WordBreakTokenizer.Tokenize("hello world");
            Assert.That(spans[0].IsWord, Is.True);   // "hello"
            Assert.That(spans[1].IsWord, Is.False);   // " "
            Assert.That(spans[2].IsWord, Is.True);    // "world"
        }

        [Test]
        public void TokenSpan_IsWord_TrueForNumbers()
        {
            var spans = WordBreakTokenizer.Tokenize("42");
            Assert.That(spans[0].IsWord, Is.True);
        }

        [Test]
        public void TokenSpan_IsWord_FalseForPunctuation()
        {
            var spans = WordBreakTokenizer.Tokenize(",");
            Assert.That(spans[0].IsWord, Is.False);
        }

        [Test]
        public void TokenSpan_Roundtrip()
        {
            var input = "hello, world! 1,000 self-aware (test)";
            var spans = WordBreakTokenizer.Tokenize(input);
            var rebuilt = new System.Text.StringBuilder();
            foreach (var span in spans)
                rebuilt.Append(input, span.Start, span.Length);
            Assert.That(rebuilt.ToString(), Is.EqualTo(input));
        }

        [Test]
        public void TokenSpan_StartAndLength_Correct()
        {
            var spans = WordBreakTokenizer.Tokenize("ab,cd");
            Assert.That(spans[0].Start, Is.EqualTo(0));
            Assert.That(spans[0].Length, Is.EqualTo(2));
            Assert.That(spans[1].Start, Is.EqualTo(2));
            Assert.That(spans[1].Length, Is.EqualTo(1));
            Assert.That(spans[2].Start, Is.EqualTo(3));
            Assert.That(spans[2].Length, Is.EqualTo(2));
        }
    }
}
