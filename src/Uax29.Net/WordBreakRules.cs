using System.Runtime.CompilerServices;

namespace Uax29.Net
{
    internal static partial class WordBreakRules
    {
        // Rule index for quick scanning inside ShouldBreak:
        // WB3     : CR x LF
        // WB3a/b  : break around Newline/CR/LF
        // WB3c    : ZWJ x Extended_Pictographic
        // WB4     : ignore Extend/Format/ZWJ on right
        // WB5     : AHLetter x AHLetter
        // WB6/WB7 : AHLetter + MidLetter/MidNumLet/SingleQuote + AHLetter
        // WB7a-c  : Hebrew quote rules
        // WB8     : Numeric x Numeric
        // WB9/10  : AHLetter <-> Numeric
        // WB11/12 : Numeric + MidNum/MidNumLet/SingleQuote + Numeric
        // WB13    : Katakana x Katakana
        // WB13a/b : ExtendNumLet adjacency
        // WB999   : otherwise break
        // keep_hyphens: custom extension for infix hyphens between AHLetter/Numeric
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldBreak(string text, WB[] props, int pos, int len)
        {
            return ShouldBreakCore(props, pos, len, GetCodePointAt(text, pos));
        }

        private static int GetCodePointAt(string text, int pos)
        {
            if (pos >= text.Length) return 0;
            var c = text[pos];
            if (char.IsHighSurrogate(c) && pos + 1 < text.Length && char.IsLowSurrogate(text[pos + 1]))
                return char.ConvertToUtf32(c, text[pos + 1]);
            return c;
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldBreak(System.ReadOnlySpan<char> text, WB[] props, int pos, int len)
        {
            return ShouldBreakCore(props, pos, len, GetCodePointAt(text, pos));
        }

        private static int GetCodePointAt(System.ReadOnlySpan<char> text, int pos)
        {
            if (pos >= text.Length) return 0;
            var c = text[pos];
            if (char.IsHighSurrogate(c) && pos + 1 < text.Length && char.IsLowSurrogate(text[pos + 1]))
                return char.ConvertToUtf32(c, text[pos + 1]);
            return c;
        }
#endif

        private static bool ShouldBreakCore(WB[] props, int pos, int len, int codePointAtPos)
        {
            var left = props[pos - 1];
            var right = props[pos];

            // --- Fast-path invariants ---
            // Fast path: same-type sequences never break
            if (left.Is(WB.ALetter) && right.Is(WB.ALetter))
            {
                return false;    // WB5
            }

            if (left.Is(WB.Numeric) && right.Is(WB.Numeric))
            {
                return false;    // WB8
            }

            if (left.Is(WB.WSegSpace) && right.Is(WB.WSegSpace))
            {
                return false; // WB3d
            }

            // --- Line break handling (WB3, WB3a, WB3b) ---
            // WB3: CRLF
            if (left.Is(WB.CR) && right.Is(WB.LF))
            {
                return false;
            }

            // WB3a/3b: Break around newlines
            if (left.Is(WB.LineBreak))
            {
                return true;
            }

            if (right.Is(WB.LineBreak))
            {
                return true;
            }

            // --- ZWJ and ignorable format handling (WB3c, WB4) ---
            // WB3c: ZWJ x Extended_Pictographic
            if (left.Is(WB.ZWJ) && IsInExtendedPictographicRanges(codePointAtPos))
            {
                return false;
            }

            // WB4: Don't break before Extend/Format/ZWJ
            if (right.Is(WB.Ignorable))
            {
                return false;
            }

            // Effective left (skipping Extend/Format/ZWJ)
            var effLeft = left.Is(WB.Ignorable)
                ? GetEffectiveLeft(props, pos - 1) : left;
            var effRight = right;

            // --- Custom keep_hyphens extension ---
            // keep_hyphens: (AHLetter|Numeric) x Hyphen x (AHLetter|Numeric)
            if (effLeft.Is(WB.AHLetterOrNumeric) && right.Is(WB.Hyphen))
            {
                if (pos + 1 < len && GetEffectiveRight(props, pos + 1, len).Is(WB.AHLetterOrNumeric))
                {
                    return false;
                }
            }
            if (left.Is(WB.Hyphen) && effRight.Is(WB.AHLetterOrNumeric))
            {
                if (pos >= 2 && GetEffectiveLeft(props, pos - 2).Is(WB.AHLetterOrNumeric))
                {
                    return false;
                }
            }

            // --- Core ALetter/Numeric adjacency (WB5, WB8, WB9, WB10) ---
            // WB5: AHLetter x AHLetter
            if (effLeft.Is(WB.AHLetter) && effRight.Is(WB.AHLetter))
            {
                return false;
            }

            // WB9: AHLetter x Numeric
            if (effLeft.Is(WB.AHLetter) && effRight.Is(WB.Numeric))
            {
                return false;
            }

            // WB10: Numeric x AHLetter
            if (effLeft.Is(WB.Numeric) && effRight.Is(WB.AHLetter))
            {
                return false;
            }

            // WB8: Numeric x Numeric (with Extend in between)
            if (effLeft.Is(WB.Numeric) && effRight.Is(WB.Numeric))
            {
                return false;
            }

            // --- MidLetter and MidNumLet bridges (WB6, WB7) ---
            // WB6: AHLetter x (MidLetter|MidNumLet|Single_Quote) AHLetter
            if (effLeft.Is(WB.AHLetter) && right.Is(WB.MidLetterLike) && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len).Is(WB.AHLetter))
                {
                    return false;
                }
            }

            // WB7: AHLetter (MidLetter|MidNumLet|Single_Quote) x AHLetter
            if (effLeft.Is(WB.MidLetterLike) && effRight.Is(WB.AHLetter))
            {
                // Find the position of the MidLetter (effLeft), skipping Ignorable backwards
                var midPos = pos - 1;
                while (midPos >= 0 && props[midPos].Is(WB.Ignorable))
                    midPos--;
                if (midPos >= 1)
                {
                    var beforeMid = GetEffectiveLeft(props, midPos - 1);
                    if (beforeMid.Is(WB.AHLetter))
                        return false;
                }
            }

            // --- Hebrew quote rules (WB7a, WB7b, WB7c) ---
            // WB7a: Hebrew_Letter x Single_Quote
            if (effLeft.Is(WB.HebrewLetter) && effRight.Is(WB.SingleQuote))
            {
                return false;
            }

            // WB7b: Hebrew_Letter x Double_Quote Hebrew_Letter
            if (effLeft.Is(WB.HebrewLetter) && right.Is(WB.DoubleQuote) && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len).Is(WB.HebrewLetter))
                {
                    return false;
                }
            }

            // WB7c: Hebrew_Letter Double_Quote x Hebrew_Letter
            if (left.Is(WB.DoubleQuote) && effRight.Is(WB.HebrewLetter) && pos >= 2)
            {
                if (GetEffectiveLeft(props, pos - 2).Is(WB.HebrewLetter))
                {
                    return false;
                }
            }

            // --- Numeric punctuation bridges (WB11, WB12) ---
            // WB11: Numeric (MidNum|MidNumLet|Single_Quote) x Numeric
            if (effLeft.Is(WB.MidNumLike) && effRight.Is(WB.Numeric))
            {
                var midPos = pos - 1;
                while (midPos >= 0 && props[midPos].Is(WB.Ignorable))
                    midPos--;
                if (midPos >= 1)
                {
                    var beforeMid = GetEffectiveLeft(props, midPos - 1);
                    if (beforeMid.Is(WB.Numeric))
                        return false;
                }
            }

            // WB12: Numeric x (MidNum|MidNumLet|Single_Quote) Numeric
            if (effLeft.Is(WB.Numeric) && right.Is(WB.MidNumLike) && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len).Is(WB.Numeric))
                {
                    return false;
                }
            }

            // --- Katakana and ExtendNumLet rules (WB13, WB13a, WB13b) ---
            // WB13: Katakana x Katakana
            if (effLeft.Is(WB.Katakana) && effRight.Is(WB.Katakana))
            {
                return false;
            }

            // WB13a: (AHLetter|Numeric|Katakana|ExtendNumLet) x ExtendNumLet
            if (effRight.Is(WB.ExtendNumLet) &&
                effLeft.Is(WB.AHLetter | WB.Numeric | WB.Katakana | WB.ExtendNumLet))
            {
                return false;
            }

            // WB13b: ExtendNumLet x (AHLetter|Numeric|Katakana)
            if (effLeft.Is(WB.ExtendNumLet) &&
                effRight.Is(WB.AHLetter | WB.Numeric | WB.Katakana))
            {
                return false;
            }

            // WB15/WB16: Keep Regional_Indicator in pairs (flag sequences).
            if (effLeft.Is(WB.RegionalIndicator) && effRight.Is(WB.RegionalIndicator))
            {
                if ((CountConsecutiveRegionalIndicatorsToLeft(props, pos) & 1) == 1)
                {
                    return false;
                }
            }

            // --- Fallback ---
            // WB999: Otherwise, break
            return true;
        }

        internal static WB GetEffectiveLeft(WB[] props, int pos)
        {
            while (pos >= 0 && props[pos].Is(WB.Ignorable))
                pos--;
            return pos >= 0 ? props[pos] : WB.Other;
        }

        internal static WB GetEffectiveRight(WB[] props, int pos, int length)
        {
            while (pos < length && props[pos].Is(WB.Ignorable))
                pos++;
            return pos < length ? props[pos] : WB.Other;
        }

        internal static bool IsExtendedPictographic(string text, int pos)
        {
            if (pos >= text.Length)
            {
                return false;
            }

            int codePoint;
            var c = text[pos];

            if (char.IsHighSurrogate(c) && pos + 1 < text.Length && char.IsLowSurrogate(text[pos + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[pos + 1]);
            }
            else
            {
                codePoint = c;
            }

            return IsInExtendedPictographicRanges(codePoint);
        }

        private static int CountConsecutiveRegionalIndicatorsToLeft(WB[] props, int pos)
        {
            var count = 0;
            var i = pos - 1;

            while (i >= 0)
            {
                while (i >= 0 && props[i].Is(WB.Ignorable))
                {
                    i--;
                }

                if (i < 0 || !props[i].Is(WB.RegionalIndicator))
                {
                    break;
                }

                count++;
                i--;
            }

            return count;
        }
    }
}
