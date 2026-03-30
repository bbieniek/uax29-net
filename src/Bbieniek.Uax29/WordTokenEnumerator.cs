#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace Bbieniek.Uax29
{
    /// <summary>
    /// A token returned by the zero-allocation word enumerator.
    /// </summary>
    public readonly ref struct WordToken
    {
        /// <summary>The token characters.</summary>
        public readonly ReadOnlySpan<char> Span;

        /// <summary>Start index in the original input.</summary>
        public readonly int Start;

        /// <summary>Number of characters.</summary>
        public readonly int Length;

        /// <summary>True if token contains word content (letters, digits).</summary>
        public readonly bool IsWord;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WordToken(ReadOnlySpan<char> span, int start, int length, bool isWord)
        {
            Span = span;
            Start = start;
            Length = length;
            IsWord = isWord;
        }
    }

    /// <summary>
    /// Zero-allocation enumerator over word boundary tokens.
    /// </summary>
    public ref struct WordTokenEnumerator
    {
        private readonly ReadOnlySpan<char> _input;
        private readonly WB[] _props;
        private int _pos;
        private WordToken _current;

        internal WordTokenEnumerator(ReadOnlySpan<char> input)
        {
            _input = input;
            _pos = 0;
            _current = default;

            _props = input.Length > 0 ? new WB[input.Length] : Array.Empty<WB>();
            if (input.Length > 0)
                WordBreakClassifier.ClassifyAll(input, _props, 0);
        }

        internal WordTokenEnumerator(ReadOnlySpan<char> input, WordBreakOptions options)
        {
            _input = input;
            _pos = 0;
            _current = default;

            _props = input.Length > 0 ? new WB[input.Length] : Array.Empty<WB>();
            if (input.Length > 0)
            {
                WordBreakClassifier.ClassifyAll(input, _props, 0);
                if (options.HasExclusions)
                    WordBreakClassifier.ApplyMidLetterExclusions(input, _props, options);
            }
        }

        public readonly WordToken Current => _current;

        public bool MoveNext()
        {
            if (_pos >= _input.Length)
                return false;

            var tokenStart = _pos;
            var hasLetterOrDigit = _props[_pos].Is(WB.LetterOrDigit);

            for (var i = _pos + 1; i < _input.Length; i++)
            {
                if (char.IsHighSurrogate(_input[i - 1]) && char.IsLowSurrogate(_input[i]))
                    continue;

                if (WordBreakRules.ShouldBreak(_input, _props, i, _input.Length))
                {
                    _current = new WordToken(
                        _input.Slice(tokenStart, i - tokenStart),
                        tokenStart, i - tokenStart, hasLetterOrDigit);
                    _pos = i;
                    return true;
                }

                hasLetterOrDigit = hasLetterOrDigit || _props[i].Is(WB.LetterOrDigit);
            }

            _current = new WordToken(
                _input.Slice(tokenStart, _input.Length - tokenStart),
                tokenStart, _input.Length - tokenStart, hasLetterOrDigit);
            _pos = _input.Length;
            return true;
        }

        public readonly WordTokenEnumerator GetEnumerator() => this;
    }
}
#endif
