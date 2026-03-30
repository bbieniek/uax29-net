using System.Globalization;

namespace Bbieniek.Uax29
{
    internal static class WordBreakClassifier
    {
        // Pre-computed lookup table for ASCII (0-127).
        internal static readonly WB[] AsciiTable = BuildAsciiTable();

        private static WB[] BuildAsciiTable()
        {
            var t = new WB[128];
            for (var i = 0; i < 128; i++)
                t[i] = WB.Other;

            t['\r'] = WB.CR;
            t['\n'] = WB.LF;
            t['\u000B'] = WB.Newline;
            t['\u000C'] = WB.Newline;

            t[' '] = WB.WSegSpace;
            t['\t'] = WB.WSegSpace;

            t['\''] = WB.SingleQuote;
            t['"'] = WB.DoubleQuote;

            t[':'] = WB.MidLetter;

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

        internal static void ClassifyAll(string text, WB[] props)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c < 128)
                {
                    props[i] = AsciiTable[c];
                    continue;
                }

                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    var codePoint = char.ConvertToUtf32(c, text[i + 1]);
                    props[i] = ClassifyCodePoint(codePoint, CharUnicodeInfo.GetUnicodeCategory(text, i));
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
            {
                return WB.Newline;
            }

            if (c == '\u200D')
            {
                return WB.ZWJ;
            }

            // Latin-1 Supplement + Latin Extended-A/B fast path
            if (c >= '\u00C0' && c <= '\u024F')
            {
                if (c == '\u00D7' || c == '\u00F7')
                {
                    return WB.Other;
                }

                return WB.ALetter;
            }

            // WSegSpace
            if (c == '\u00A0' || c == '\u1680' ||
                (c >= '\u2000' && c <= '\u200A') ||
                c == '\u202F' || c == '\u205F' || c == '\u3000')
            {
                return WB.WSegSpace;
            }

            // MidLetter (colon excluded per ICU word.txt)
            if (c == '\u00B7' || c == '\u0387' || c == '\u05F4' || c == '\u2027' || c == '\uFE13')
            {
                return WB.MidLetter;
            }

            // MidNum
            if (c == '\u037E' || c == '\u0589' ||
                c == '\u060C' || c == '\u060D' || c == '\u066C' || c == '\u07F8' ||
                c == '\u2044' || c == '\uFE10' || c == '\uFE14')
            {
                return WB.MidNum;
            }

            // MidNumLet (non-ASCII)
            if (c == '\u2018' || c == '\u2019' || c == '\u2024' ||
                c == '\uFE52' || c == '\uFF07' || c == '\uFF0E')
            {
                return WB.MidNumLet;
            }

            // Hebrew letters
            if (c >= '\u05D0' && c <= '\u05EA')
            {
                return WB.HebrewLetter;
            }

            // Katakana
            if ((c >= '\u3031' && c <= '\u3035') ||
                (c >= '\u309B' && c <= '\u309C') ||
                (c >= '\u30A0' && c <= '\u30FF') ||
                (c >= '\u31F0' && c <= '\u31FF') ||
                (c >= '\u32D0' && c <= '\u32FE') ||
                (c >= '\u3300' && c <= '\u3357') ||
                (c >= '\uFF65' && c <= '\uFF9F'))
            {
                return WB.Katakana;
            }

            var cat = CharUnicodeInfo.GetUnicodeCategory(c);

            if (cat == UnicodeCategory.NonSpacingMark ||
                cat == UnicodeCategory.SpacingCombiningMark ||
                cat == UnicodeCategory.EnclosingMark)
            {
                return WB.Extend;
            }

            if (cat == UnicodeCategory.Format)
            {
                return WB.Format;
            }

            if (cat == UnicodeCategory.DashPunctuation)
            {
                return WB.Hyphen;
            }

            if (cat == UnicodeCategory.DecimalDigitNumber)
            {
                return WB.Numeric;
            }

            if (cat == UnicodeCategory.ConnectorPunctuation)
            {
                return WB.ExtendNumLet;
            }

            if (cat == UnicodeCategory.UppercaseLetter ||
                cat == UnicodeCategory.LowercaseLetter ||
                cat == UnicodeCategory.TitlecaseLetter ||
                cat == UnicodeCategory.ModifierLetter ||
                cat == UnicodeCategory.OtherLetter ||
                cat == UnicodeCategory.LetterNumber)
            {
                return WB.ALetter;
            }

            return WB.Other;
        }

        private static WB ClassifyCodePoint(int codePoint, UnicodeCategory cat)
        {
            if (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF)
            {
                return WB.RegionalIndicator;
            }

            // Emoji skin tone modifiers: Word_Break = Extend (Unicode 15.0)
            if (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF)
            {
                return WB.Extend;
            }

            // Supplementary Katakana ranges (Unicode 15.0 WordBreakProperty.txt)
            if ((codePoint >= 0x1AFF0 && codePoint <= 0x1AFF3) ||
                (codePoint >= 0x1AFF5 && codePoint <= 0x1AFFB) ||
                (codePoint >= 0x1AFFD && codePoint <= 0x1AFFE) ||
                codePoint == 0x1B000 ||
                (codePoint >= 0x1B120 && codePoint <= 0x1B122) ||
                codePoint == 0x1B155 ||
                (codePoint >= 0x1B164 && codePoint <= 0x1B167))
            {
                return WB.Katakana;
            }

            if (cat == UnicodeCategory.UppercaseLetter ||
                cat == UnicodeCategory.LowercaseLetter ||
                cat == UnicodeCategory.TitlecaseLetter ||
                cat == UnicodeCategory.ModifierLetter ||
                cat == UnicodeCategory.OtherLetter ||
                cat == UnicodeCategory.LetterNumber)
            {
                return WB.ALetter;
            }

            if (cat == UnicodeCategory.DecimalDigitNumber)
            {
                return WB.Numeric;
            }

            if (cat == UnicodeCategory.NonSpacingMark ||
                cat == UnicodeCategory.SpacingCombiningMark ||
                cat == UnicodeCategory.EnclosingMark)
            {
                return WB.Extend;
            }

            if (cat == UnicodeCategory.Format)
            {
                return WB.Format;
            }

            if (cat == UnicodeCategory.DashPunctuation)
            {
                return WB.Hyphen;
            }

            if (cat == UnicodeCategory.ConnectorPunctuation)
            {
                return WB.ExtendNumLet;
            }

            return WB.Other;
        }

        internal static void ApplyMidLetterExclusions(string text, WB[] props, WordBreakOptions options)
        {
            foreach (var ch in options.MidLetterExclusions)
            {
                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == ch && props[i].Is(WB.MidLetter))
                    {
                        props[i] = WB.Other;
                    }
                }
            }
        }

#if NET8_0_OR_GREATER
        internal static void ApplyMidLetterExclusions(System.ReadOnlySpan<char> text, WB[] props, WordBreakOptions options)
        {
            foreach (var ch in options.MidLetterExclusions)
            {
                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == ch && props[i].Is(WB.MidLetter))
                    {
                        props[i] = WB.Other;
                    }
                }
            }
        }

        internal static void ClassifyAll(System.ReadOnlySpan<char> text, WB[] props, int offset)
        {
            for (var i = offset; i < text.Length; i++)
            {
                var c = text[i];

                if (c < 128)
                {
                    props[i] = AsciiTable[c];
                    continue;
                }

                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    var codePoint = char.ConvertToUtf32(c, text[i + 1]);
                    props[i] = ClassifyCodePoint(codePoint, System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint));
                    props[i + 1] = WB.Extend;
                    i++;
                    continue;
                }

                props[i] = ClassifyNonAscii(c);
            }
        }
#endif
    }
}
