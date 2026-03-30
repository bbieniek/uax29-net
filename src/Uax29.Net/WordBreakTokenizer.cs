using System;
using System.Collections.Generic;

namespace Uax29.Net
{
    /// <summary>
    /// Managed implementation of Unicode UAX #29 word boundary segmentation.
    /// <para>
    /// Implements the word break rules from Unicode Standard Annex #29
    /// plus the <c>keep_hyphens</c> rule that preserves infix hyphens
    /// between letters/digits (matching quanteda/ICU default behavior).
    /// </para>
    /// </summary>
    public static class WordBreakTokenizer
    {
        /// <summary>
        /// Tokenizes <paramref name="text"/> into word and separator spans
        /// using UAX #29 word boundary rules.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>
        /// A list of <see cref="TokenSpan"/> values. Each span is either a word
        /// (<see cref="TokenSpan.IsWord"/> = true) or a separator (false).
        /// Concatenating all spans reproduces the original text.
        /// </returns>
        public static List<TokenSpan> Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return [];
            }

            var len = text.Length;
            var spans = new List<TokenSpan>(len / 5 + 1);

            var props = new WB[len];
            WordBreakClassifier.ClassifyAll(text, props);

            var tokenStart = 0;
            var tokenHasLetterOrDigit = props[0].Is(WB.LetterOrDigit);

            for (var pos = 1; pos < len; pos++)
            {
                if (char.IsHighSurrogate(text[pos - 1]) && char.IsLowSurrogate(text[pos]))
                {
                    continue;
                }

                if (WordBreakRules.ShouldBreak(text, props, pos, len))
                {
                    spans.Add(new TokenSpan(tokenStart, pos - tokenStart, tokenHasLetterOrDigit));
                    tokenStart = pos;
                    tokenHasLetterOrDigit = props[pos].Is(WB.LetterOrDigit);
                }
                else
                {
                    tokenHasLetterOrDigit = tokenHasLetterOrDigit || props[pos].Is(WB.LetterOrDigit);
                }
            }

            spans.Add(new TokenSpan(tokenStart, len - tokenStart, tokenHasLetterOrDigit));
            return spans;
        }

        /// <summary>
        /// Tokenizes <paramref name="text"/> into word and separator spans
        /// using UAX #29 word boundary rules with the specified options.
        /// </summary>
        public static List<TokenSpan> Tokenize(string text, WordBreakOptions options)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<TokenSpan>();
            }

            var len = text.Length;
            var spans = new List<TokenSpan>(len / 5 + 1);

            var props = new WB[len];
            WordBreakClassifier.ClassifyAll(text, props);

            if (options.HasExclusions)
            {
                WordBreakClassifier.ApplyMidLetterExclusions(text, props, options);
            }

            var tokenStart = 0;
            var tokenHasLetterOrDigit = props[0].Is(WB.LetterOrDigit);

            for (var pos = 1; pos < len; pos++)
            {
                if (char.IsHighSurrogate(text[pos - 1]) && char.IsLowSurrogate(text[pos]))
                {
                    continue;
                }

                if (WordBreakRules.ShouldBreak(text, props, pos, len))
                {
                    spans.Add(new TokenSpan(tokenStart, pos - tokenStart, tokenHasLetterOrDigit));
                    tokenStart = pos;
                    tokenHasLetterOrDigit = props[pos].Is(WB.LetterOrDigit);
                }
                else
                {
                    tokenHasLetterOrDigit = tokenHasLetterOrDigit || props[pos].Is(WB.LetterOrDigit);
                }
            }

            spans.Add(new TokenSpan(tokenStart, len - tokenStart, tokenHasLetterOrDigit));
            return spans;
        }

        /// <summary>
        /// Tokenizes <paramref name="text"/> and returns the token strings.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>A list of token strings. Concatenating them reproduces the original text.</returns>
        public static List<string> TokenizeToStrings(string text)
        {
            var spans = Tokenize(text);
            var result = new List<string>(spans.Count);
            foreach (var span in spans)
            {
                result.Add(text.Substring(span.Start, span.Length));
            }
            return result;
        }

        /// <summary>
        /// Tokenizes <paramref name="text"/> and returns the token strings
        /// using the specified options.
        /// </summary>
        public static List<string> TokenizeToStrings(string text, WordBreakOptions options)
        {
            var spans = Tokenize(text, options);
            var result = new List<string>(spans.Count);
            foreach (var span in spans)
            {
                result.Add(text.Substring(span.Start, span.Length));
            }
            return result;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Returns a zero-allocation enumerator over word boundary tokens.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>A <see cref="WordTokenEnumerator"/> that yields tokens without heap allocation.</returns>
        public static WordTokenEnumerator EnumerateWords(ReadOnlySpan<char> text)
        {
            return new WordTokenEnumerator(text);
        }

        /// <summary>
        /// Returns a zero-allocation enumerator over word boundary tokens
        /// using the specified options.
        /// </summary>
        public static WordTokenEnumerator EnumerateWords(ReadOnlySpan<char> text, WordBreakOptions options)
        {
            return new WordTokenEnumerator(text, options);
        }
#endif
    }
}
