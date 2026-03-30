using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

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
    public static class WordBreakTokenizer
    {
        // UAX #29 Word_Break property values
        private enum WB : byte
        {
            Other,
            CR,
            LF,
            Newline,
            ALetter,
            HebrewLetter,
            Numeric,
            MidLetter,
            MidNum,
            MidNumLet,
            SingleQuote,
            DoubleQuote,
            ExtendNumLet,
            Katakana,
            WSegSpace,
            Extend,
            Format,
            ZWJ,
            Hyphen,
        }

        // Pre-computed lookup table for ASCII (0-127).
        private static readonly WB[] AsciiTable = BuildAsciiTable();

        private static WB[] BuildAsciiTable()
        {
            var t = new WB[128];
            for (int i = 0; i < 128; i++)
                t[i] = WB.Other;

            t['\r'] = WB.CR;
            t['\n'] = WB.LF;
            t['\u000B'] = WB.Newline;
            t['\u000C'] = WB.Newline;

            t[' '] = WB.WSegSpace;
            t['\t'] = WB.WSegSpace;

            t['\''] = WB.SingleQuote;
            t['"'] = WB.DoubleQuote;

            t[','] = WB.MidNum;
            t[';'] = WB.MidNum;

            t['.'] = WB.MidNumLet;

            t['-'] = WB.Hyphen;

            t['_'] = WB.ExtendNumLet;

            // @ is ALetter in ICU's word.txt
            t['@'] = WB.ALetter;

            for (int i = '0'; i <= '9'; i++)
                t[i] = WB.Numeric;
            for (int i = 'A'; i <= 'Z'; i++)
                t[i] = WB.ALetter;
            for (int i = 'a'; i <= 'z'; i++)
                t[i] = WB.ALetter;

            return t;
        }

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
                return new List<TokenSpan>();

            int len = text.Length;
            var spans = new List<TokenSpan>();

            var props = new WB[len];
            ClassifyAll(text, props);

            int tokenStart = 0;
            bool tokenHasLetterOrDigit = IsLetterOrDigitWB(props[0]);

            for (int pos = 1; pos < len; pos++)
            {
                if (char.IsHighSurrogate(text[pos - 1]) && char.IsLowSurrogate(text[pos]))
                    continue;

                if (ShouldBreak(text, props, pos, len))
                {
                    spans.Add(new TokenSpan(tokenStart, pos - tokenStart, tokenHasLetterOrDigit));
                    tokenStart = pos;
                    tokenHasLetterOrDigit = IsLetterOrDigitWB(props[pos]);
                }
                else
                {
                    tokenHasLetterOrDigit = tokenHasLetterOrDigit || IsLetterOrDigitWB(props[pos]);
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
                result.Add(text.Substring(span.Start, span.Length));
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLetterOrDigitWB(WB wb)
        {
            return wb == WB.ALetter || wb == WB.HebrewLetter || wb == WB.Numeric
                || wb == WB.Katakana || wb == WB.ExtendNumLet;
        }

        private static void ClassifyAll(string text, WB[] props)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c < 128)
                {
                    props[i] = AsciiTable[c];
                    continue;
                }

                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    props[i] = ClassifyCodePoint(CharUnicodeInfo.GetUnicodeCategory(text, i));
                    props[i + 1] = WB.Extend;
                    i++;
                    continue;
                }

                props[i] = ClassifyNonAscii(c);
            }
        }

        private static WB ClassifyNonAscii(char c)
        {
            if (c == '\u0085' || c == '\u2028' || c == '\u2029')
                return WB.Newline;

            if (c == '\u200D') return WB.ZWJ;

            // Latin-1 Supplement + Latin Extended-A/B fast path
            if (c >= '\u00C0' && c <= '\u024F')
            {
                if (c == '\u00D7' || c == '\u00F7')
                    return WB.Other;
                return WB.ALetter;
            }

            // WSegSpace
            if (c == '\u00A0' || c == '\u1680' ||
                (c >= '\u2000' && c <= '\u200A') ||
                c == '\u202F' || c == '\u205F' || c == '\u3000')
                return WB.WSegSpace;

            // MidLetter (colon excluded per ICU word.txt)
            if (c == '\u00B7' || c == '\u0387' || c == '\u05F4' || c == '\u2027' || c == '\uFE13')
                return WB.MidLetter;

            // MidNum
            if (c == '\u037E' || c == '\u0589' ||
                c == '\u060C' || c == '\u060D' || c == '\u066C' || c == '\u07F8' ||
                c == '\u2044' || c == '\uFE10' || c == '\uFE14')
                return WB.MidNum;

            // MidNumLet (non-ASCII)
            if (c == '\u2018' || c == '\u2019' || c == '\u2024' ||
                c == '\uFE52' || c == '\uFF07' || c == '\uFF0E')
                return WB.MidNumLet;

            // Hebrew letters
            if (c >= '\u05D0' && c <= '\u05EA')
                return WB.HebrewLetter;

            // Katakana
            if ((c >= '\u30A0' && c <= '\u30FF') ||
                (c >= '\u31F0' && c <= '\u31FF') ||
                (c >= '\uFF65' && c <= '\uFF9F'))
                return WB.Katakana;

            var cat = CharUnicodeInfo.GetUnicodeCategory(c);

            if (cat == UnicodeCategory.NonSpacingMark ||
                cat == UnicodeCategory.SpacingCombiningMark ||
                cat == UnicodeCategory.EnclosingMark)
                return WB.Extend;

            if (cat == UnicodeCategory.Format)
                return WB.Format;

            if (cat == UnicodeCategory.DashPunctuation)
                return WB.Hyphen;

            if (cat == UnicodeCategory.DecimalDigitNumber)
                return WB.Numeric;

            if (cat == UnicodeCategory.ConnectorPunctuation)
                return WB.ExtendNumLet;

            if (cat == UnicodeCategory.UppercaseLetter ||
                cat == UnicodeCategory.LowercaseLetter ||
                cat == UnicodeCategory.TitlecaseLetter ||
                cat == UnicodeCategory.ModifierLetter ||
                cat == UnicodeCategory.OtherLetter ||
                cat == UnicodeCategory.LetterNumber)
                return WB.ALetter;

            return WB.Other;
        }

        private static WB ClassifyCodePoint(UnicodeCategory cat)
        {
            if (cat == UnicodeCategory.OtherLetter ||
                cat == UnicodeCategory.UppercaseLetter ||
                cat == UnicodeCategory.LowercaseLetter)
                return WB.ALetter;

            if (cat == UnicodeCategory.DecimalDigitNumber)
                return WB.Numeric;

            if (cat == UnicodeCategory.NonSpacingMark ||
                cat == UnicodeCategory.SpacingCombiningMark)
                return WB.Extend;

            if (cat == UnicodeCategory.Format)
                return WB.Format;

            return WB.Other;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldBreak(string text, WB[] props, int pos, int len)
        {
            WB left = props[pos - 1];
            WB right = props[pos];

            // Fast path: same-type sequences never break
            if (left == WB.ALetter && right == WB.ALetter) return false;    // WB5
            if (left == WB.Numeric && right == WB.Numeric) return false;    // WB8
            if (left == WB.WSegSpace && right == WB.WSegSpace) return false; // WB3d

            // WB3: CRLF
            if (left == WB.CR && right == WB.LF) return false;

            // WB3a/3b: Break around newlines
            if (left == WB.Newline || left == WB.CR || left == WB.LF) return true;
            if (right == WB.Newline || right == WB.CR || right == WB.LF) return true;

            // WB3c: ZWJ x Extended_Pictographic
            if (left == WB.ZWJ && IsExtendedPictographic(text, pos)) return false;

            // WB4: Don't break before Extend/Format/ZWJ
            if (right == WB.Extend || right == WB.Format || right == WB.ZWJ) return false;

            // Effective left (skipping Extend/Format/ZWJ)
            WB effLeft = (left == WB.Extend || left == WB.Format || left == WB.ZWJ)
                ? GetEffectiveLeft(props, pos - 1) : left;
            WB effRight = right;

            // keep_hyphens: (AHLetter|Numeric) x Hyphen x (AHLetter|Numeric)
            if (IsAHLetterOrNumeric(effLeft) && right == WB.Hyphen)
            {
                if (pos + 1 < len && IsAHLetterOrNumeric(GetEffectiveRight(props, pos + 1, len)))
                    return false;
            }
            if (left == WB.Hyphen && IsAHLetterOrNumeric(effRight))
            {
                if (pos >= 2 && IsAHLetterOrNumeric(GetEffectiveLeft(props, pos - 2)))
                    return false;
            }

            // WB5: AHLetter x AHLetter
            if (IsAHLetter(effLeft) && IsAHLetter(effRight)) return false;

            // WB9: AHLetter x Numeric
            if (IsAHLetter(effLeft) && effRight == WB.Numeric) return false;

            // WB10: Numeric x AHLetter
            if (effLeft == WB.Numeric && IsAHLetter(effRight)) return false;

            // WB8: Numeric x Numeric (with Extend in between)
            if (effLeft == WB.Numeric && effRight == WB.Numeric) return false;

            // WB6: AHLetter x (MidLetter|MidNumLet|Single_Quote) AHLetter
            if (IsAHLetter(effLeft) && IsMidLetterLike(right) && pos + 1 < len)
            {
                if (IsAHLetter(GetEffectiveRight(props, pos + 1, len)))
                    return false;
            }

            // WB7: AHLetter (MidLetter|MidNumLet|Single_Quote) x AHLetter
            if (IsMidLetterLike(left) && IsAHLetter(effRight) && pos >= 2)
            {
                if (IsAHLetter(GetEffectiveLeft(props, pos - 2)))
                    return false;
            }

            // WB7a: Hebrew_Letter x Single_Quote
            if (effLeft == WB.HebrewLetter && effRight == WB.SingleQuote) return false;

            // WB7b: Hebrew_Letter x Double_Quote Hebrew_Letter
            if (effLeft == WB.HebrewLetter && right == WB.DoubleQuote && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len) == WB.HebrewLetter)
                    return false;
            }

            // WB7c: Hebrew_Letter Double_Quote x Hebrew_Letter
            if (left == WB.DoubleQuote && effRight == WB.HebrewLetter && pos >= 2)
            {
                if (GetEffectiveLeft(props, pos - 2) == WB.HebrewLetter)
                    return false;
            }

            // WB11: Numeric (MidNum|MidNumLet|Single_Quote) x Numeric
            if (IsMidNumLike(left) && effRight == WB.Numeric && pos >= 2)
            {
                if (GetEffectiveLeft(props, pos - 2) == WB.Numeric)
                    return false;
            }

            // WB12: Numeric x (MidNum|MidNumLet|Single_Quote) Numeric
            if (effLeft == WB.Numeric && IsMidNumLike(right) && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len) == WB.Numeric)
                    return false;
            }

            // WB13: Katakana x Katakana
            if (effLeft == WB.Katakana && effRight == WB.Katakana) return false;

            // WB13a: (AHLetter|Numeric|Katakana|ExtendNumLet) x ExtendNumLet
            if (effRight == WB.ExtendNumLet &&
                (IsAHLetter(effLeft) || effLeft == WB.Numeric || effLeft == WB.Katakana || effLeft == WB.ExtendNumLet))
                return false;

            // WB13b: ExtendNumLet x (AHLetter|Numeric|Katakana)
            if (effLeft == WB.ExtendNumLet &&
                (IsAHLetter(effRight) || effRight == WB.Numeric || effRight == WB.Katakana))
                return false;

            // WB999: Otherwise, break
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAHLetter(WB wb) => wb == WB.ALetter || wb == WB.HebrewLetter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAHLetterOrNumeric(WB wb) => wb == WB.ALetter || wb == WB.HebrewLetter || wb == WB.Numeric;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMidLetterLike(WB wb) => wb == WB.MidLetter || wb == WB.MidNumLet || wb == WB.SingleQuote;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMidNumLike(WB wb) => wb == WB.MidNum || wb == WB.MidNumLet || wb == WB.SingleQuote;

        private static WB GetEffectiveLeft(WB[] props, int pos)
        {
            while (pos >= 0 && (props[pos] == WB.Extend || props[pos] == WB.Format || props[pos] == WB.ZWJ))
                pos--;
            return pos >= 0 ? props[pos] : WB.Other;
        }

        private static WB GetEffectiveRight(WB[] props, int pos, int length)
        {
            while (pos < length && (props[pos] == WB.Extend || props[pos] == WB.Format || props[pos] == WB.ZWJ))
                pos++;
            return pos < length ? props[pos] : WB.Other;
        }

        private static bool IsExtendedPictographic(string text, int pos)
        {
            if (pos >= text.Length) return false;
            char c = text[pos];
            if (c >= '\u2600' && c <= '\u27BF') return true;
            if (char.IsHighSurrogate(c) && pos + 1 < text.Length && char.IsLowSurrogate(text[pos + 1]))
            {
                int cp = char.ConvertToUtf32(c, text[pos + 1]);
                if (cp >= 0x1F000 && cp <= 0x1FAFF) return true;
                if (cp >= 0x1F1E0 && cp <= 0x1F1FF) return true;
            }
            return false;
        }
    }
}
