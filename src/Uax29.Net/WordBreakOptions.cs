using System;
using System.Collections.Generic;

namespace Uax29.Net
{
    /// <summary>
    /// Options for customizing word boundary segmentation behavior.
    /// </summary>
    public sealed class WordBreakOptions
    {
        /// <summary>
        /// Characters to demote from MidLetter to Other.
        /// These characters will cause word breaks instead of joining letters.
        /// </summary>
        public IReadOnlyCollection<char> MidLetterExclusions { get; }

        /// <summary>
        /// Creates options with the specified MidLetter exclusions.
        /// </summary>
        public WordBreakOptions(IReadOnlyCollection<char> midLetterExclusions)
        {
            MidLetterExclusions = midLetterExclusions;
        }

        /// <summary>
        /// Default options: strict Unicode UAX #29 conformance (no exclusions).
        /// </summary>
        public static WordBreakOptions Default { get; } = new(
#if NET8_0_OR_GREATER
            []
#else
            []
#endif
        );

        /// <summary>
        /// Quanteda/ICU-compatible options: excludes colon and its fullwidth/small variants
        /// from MidLetter, so <c>"key:value"</c> splits into <c>["key", ":", "value"]</c>.
        /// </summary>
        public static WordBreakOptions Quanteda { get; } = new(
            [':', '\uFE55', '\uFF1A']
        );

        internal bool HasExclusions => MidLetterExclusions.Count > 0;
    }
}
