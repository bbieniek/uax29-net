using System.Runtime.CompilerServices;

namespace Uax29.Net
{
    public static partial class WordBreakTokenizer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLetterOrDigitWB(WB wb)
        {
            return wb == WB.ALetter || wb == WB.HebrewLetter || wb == WB.Numeric
                || wb == WB.Katakana || wb == WB.ExtendNumLet;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAHLetter(WB wb) => wb == WB.ALetter || wb == WB.HebrewLetter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAHLetterOrNumeric(WB wb) => wb == WB.ALetter || wb == WB.HebrewLetter || wb == WB.Numeric;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMidLetterLike(WB wb) => wb == WB.MidLetter || wb == WB.MidNumLet || wb == WB.SingleQuote;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMidNumLike(WB wb) => wb == WB.MidNum || wb == WB.MidNumLet || wb == WB.SingleQuote;
    }
}