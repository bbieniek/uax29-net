using System.Collections.Generic;
using System.Linq;

namespace Uax29.Net
{
    /// <summary>
    /// Managed implementation of Unicode UAX #29 word boundary segmentation.
    /// <para>
    /// Implements the word break rules from Unicode Standard Annex #29
    /// (based on ICU word.txt, UAX #29 Revision 34 for Unicode 12.0)
    /// plus the <c>keep_hyphens</c> rule that preserves infix hyphens
    /// between letters/digits (matching quanteda/ICU default behavior).
    /// </para>
    /// <para>
    /// Zero dependencies. No native ICU required. Targets netstandard2.0.
    /// </para>
    /// </summary>
    public static partial class WordBreakTokenizer
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
                return new List<TokenSpan>();
            }

            var len = text.Length;
            var spans = new List<TokenSpan>();

            var props = new WB[len];
            ClassifyAll(text, props);

            var tokenStart = 0;
            var tokenHasLetterOrDigit = props[0].Is(WB.LetterOrDigit);

            for (var pos = 1; pos < len; pos++)
            {
                if (char.IsHighSurrogate(text[pos - 1]) && char.IsLowSurrogate(text[pos]))
                {
                    continue;
                }

                if (ShouldBreak(text, props, pos, len))
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
            result.AddRange(spans.Select(span => text.Substring(span.Start, span.Length)));
            return result;
        }
    }
}
