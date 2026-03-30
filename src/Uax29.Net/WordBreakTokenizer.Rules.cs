using System.Runtime.CompilerServices;

namespace Uax29.Net
{
    public static partial class WordBreakTokenizer
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
        private static bool ShouldBreak(string text, WB[] props, int pos, int len)
        {
            var left = props[pos - 1];
            var right = props[pos];

            // --- Fast-path invariants ---
            // Fast path: same-type sequences never break
            if (left == WB.ALetter && right == WB.ALetter)
            {
                return false;    // WB5
            }

            if (left == WB.Numeric && right == WB.Numeric)
            {
                return false;    // WB8
            }

            if (left == WB.WSegSpace && right == WB.WSegSpace)
            {
                return false; // WB3d
            }

            // --- Line break handling (WB3, WB3a, WB3b) ---
            // WB3: CRLF
            if (left == WB.CR && right == WB.LF)
            {
                return false;
            }

            // WB3a/3b: Break around newlines
            if (left == WB.Newline || left == WB.CR || left == WB.LF)
            {
                return true;
            }

            if (right == WB.Newline || right == WB.CR || right == WB.LF)
            {
                return true;
            }

            // --- ZWJ and ignorable format handling (WB3c, WB4) ---
            // WB3c: ZWJ x Extended_Pictographic
            if (left == WB.ZWJ && IsExtendedPictographic(text, pos))
            {
                return false;
            }

            // WB4: Don't break before Extend/Format/ZWJ
            if (right == WB.Extend || right == WB.Format || right == WB.ZWJ)
            {
                return false;
            }

            // Effective left (skipping Extend/Format/ZWJ)
            var effLeft = (left == WB.Extend || left == WB.Format || left == WB.ZWJ)
                ? GetEffectiveLeft(props, pos - 1) : left;
            var effRight = right;

            // --- Custom keep_hyphens extension ---
            // keep_hyphens: (AHLetter|Numeric) x Hyphen x (AHLetter|Numeric)
            if (IsAHLetterOrNumeric(effLeft) && right == WB.Hyphen)
            {
                if (pos + 1 < len && IsAHLetterOrNumeric(GetEffectiveRight(props, pos + 1, len)))
                {
                    return false;
                }
            }
            if (left == WB.Hyphen && IsAHLetterOrNumeric(effRight))
            {
                if (pos >= 2 && IsAHLetterOrNumeric(GetEffectiveLeft(props, pos - 2)))
                {
                    return false;
                }
            }

            // --- Core ALetter/Numeric adjacency (WB5, WB8, WB9, WB10) ---
            // WB5: AHLetter x AHLetter
            if (IsAHLetter(effLeft) && IsAHLetter(effRight))
            {
                return false;
            }

            // WB9: AHLetter x Numeric
            if (IsAHLetter(effLeft) && effRight == WB.Numeric)
            {
                return false;
            }

            // WB10: Numeric x AHLetter
            if (effLeft == WB.Numeric && IsAHLetter(effRight))
            {
                return false;
            }

            // WB8: Numeric x Numeric (with Extend in between)
            if (effLeft == WB.Numeric && effRight == WB.Numeric)
            {
                return false;
            }

            // --- MidLetter and MidNumLet bridges (WB6, WB7) ---
            // WB6: AHLetter x (MidLetter|MidNumLet|Single_Quote) AHLetter
            if (IsAHLetter(effLeft) && IsMidLetterLike(right) && pos + 1 < len)
            {
                if (IsAHLetter(GetEffectiveRight(props, pos + 1, len)))
                {
                    return false;
                }
            }

            // WB7: AHLetter (MidLetter|MidNumLet|Single_Quote) x AHLetter
            if (IsMidLetterLike(left) && IsAHLetter(effRight) && pos >= 2)
            {
                if (IsAHLetter(GetEffectiveLeft(props, pos - 2)))
                {
                    return false;
                }
            }

            // --- Hebrew quote rules (WB7a, WB7b, WB7c) ---
            // WB7a: Hebrew_Letter x Single_Quote
            if (effLeft == WB.HebrewLetter && effRight == WB.SingleQuote)
            {
                return false;
            }

            // WB7b: Hebrew_Letter x Double_Quote Hebrew_Letter
            if (effLeft == WB.HebrewLetter && right == WB.DoubleQuote && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len) == WB.HebrewLetter)
                {
                    return false;
                }
            }

            // WB7c: Hebrew_Letter Double_Quote x Hebrew_Letter
            if (left == WB.DoubleQuote && effRight == WB.HebrewLetter && pos >= 2)
            {
                if (GetEffectiveLeft(props, pos - 2) == WB.HebrewLetter)
                {
                    return false;
                }
            }

            // --- Numeric punctuation bridges (WB11, WB12) ---
            // WB11: Numeric (MidNum|MidNumLet|Single_Quote) x Numeric
            if (IsMidNumLike(left) && effRight == WB.Numeric && pos >= 2)
            {
                if (GetEffectiveLeft(props, pos - 2) == WB.Numeric)
                {
                    return false;
                }
            }

            // WB12: Numeric x (MidNum|MidNumLet|Single_Quote) Numeric
            if (effLeft == WB.Numeric && IsMidNumLike(right) && pos + 1 < len)
            {
                if (GetEffectiveRight(props, pos + 1, len) == WB.Numeric)
                {
                    return false;
                }
            }

            // --- Katakana and ExtendNumLet rules (WB13, WB13a, WB13b) ---
            // WB13: Katakana x Katakana
            if (effLeft == WB.Katakana && effRight == WB.Katakana)
            {
                return false;
            }

            // WB13a: (AHLetter|Numeric|Katakana|ExtendNumLet) x ExtendNumLet
            if (effRight == WB.ExtendNumLet &&
                (IsAHLetter(effLeft) || effLeft == WB.Numeric || effLeft == WB.Katakana || effLeft == WB.ExtendNumLet))
            {
                return false;
            }

            // WB13b: ExtendNumLet x (AHLetter|Numeric|Katakana)
            if (effLeft == WB.ExtendNumLet &&
                (IsAHLetter(effRight) || effRight == WB.Numeric || effRight == WB.Katakana))
            {
                return false;
            }

            // WB15/WB16: Keep Regional_Indicator in pairs (flag sequences).
            if (effLeft == WB.RegionalIndicator && effRight == WB.RegionalIndicator)
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
                while (i >= 0 && (props[i] == WB.Extend || props[i] == WB.Format || props[i] == WB.ZWJ))
                {
                    i--;
                }

                if (i < 0 || props[i] != WB.RegionalIndicator)
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