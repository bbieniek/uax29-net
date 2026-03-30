using System;

namespace Uax29.Net
{
    [Flags]
    internal enum WB : uint
    {
        Other           = 0,
        CR              = 1 << 0,
        LF              = 1 << 1,
        Newline         = 1 << 2,
        ALetter         = 1 << 3,
        HebrewLetter    = 1 << 4,
        Numeric         = 1 << 5,
        MidLetter       = 1 << 6,
        MidNum          = 1 << 7,
        MidNumLet       = 1 << 8,
        SingleQuote     = 1 << 9,
        DoubleQuote     = 1 << 10,
        ExtendNumLet    = 1 << 11,
        Katakana        = 1 << 12,
        WSegSpace       = 1 << 13,
        Extend          = 1 << 14,
        Format          = 1 << 15,
        ZWJ             = 1 << 16,
        Hyphen          = 1 << 17,
        RegionalIndicator = 1 << 18,

        // Combined masks for rule predicates
        AHLetter        = ALetter | HebrewLetter,
        MidLetterLike   = MidLetter | MidNumLet | SingleQuote,
        MidNumLike      = MidNum | MidNumLet | SingleQuote,
        AHLetterOrNumeric = ALetter | HebrewLetter | Numeric,
        Ignorable       = Extend | Format | ZWJ,
        LineBreak       = Newline | CR | LF,
        LetterOrDigit   = ALetter | HebrewLetter | Numeric | Katakana | ExtendNumLet,
    }

    internal static class WBExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static bool Is(this WB value, WB mask) => (value & mask) != 0;
    }
}
