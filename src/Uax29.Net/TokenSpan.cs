namespace Uax29.Net;

/// <summary>
/// A token span representing a segment of the input text.
/// </summary>
public readonly struct TokenSpan(int start, int length, bool isWord)
{
    /// <summary>Start index in the original string.</summary>
    public readonly int Start = start;

    /// <summary>Number of characters in this token.</summary>
    public readonly int Length = length;

    /// <summary>
    /// True if this token contains word content (letters, digits, etc.).
    /// False for separators (whitespace, punctuation).
    /// Corresponds to ICU rule status >= 100.
    /// </summary>
    public readonly bool IsWord = isWord;
}
