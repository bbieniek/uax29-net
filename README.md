# Bbieniek.Uax29

[![NuGet](https://img.shields.io/nuget/v/Bbieniek.Uax29.svg)](https://www.nuget.org/packages/Bbieniek.Uax29)
[![CI](https://github.com/bbieniek/uax29-net/actions/workflows/ci.yml/badge.svg)](https://github.com/bbieniek/uax29-net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A managed .NET **word tokenizer** implementing [Unicode UAX #29](https://www.unicode.org/reports/tr29/) word boundary segmentation (Unicode 15.0). Splits text into words, numbers, punctuation, and whitespace tokens — correctly handling multilingual text, emoji, contractions, and numeric formatting.

No native ICU required. Zero dependencies on netstandard2.0. SQL CLR compatible.

## Why use this?

Splitting on spaces or regex gives inconsistent results with real-world text. Unicode UAX #29 is the standard used by ICU, Lucene, quanteda, and most NLP tooling. This package gives you the same tokenization in pure managed .NET:

- `"don't"` stays as one token (apostrophe is MidLetter)
- `"1,000,000.50"` stays as one token (comma/period are MidNum)
- `"self-aware"` stays as one token (hyphen preservation)
- Emoji ZWJ sequences like `👩‍❤️‍💋‍👨` stay together
- Works with Latin, Greek, Cyrillic, Hebrew, Katakana, CJK, and all Unicode scripts

## Installation

```bash
dotnet add package Bbieniek.Uax29
```

## Quick start

```csharp
using Bbieniek.Uax29;

// Tokenize into strings
var tokens = WordBreakTokenizer.TokenizeToStrings("Hello, world! Price: $1,000.50");
// → ["Hello", ",", " ", "world", "!", " ", "Price", ":", " ", "$", "1,000.50"]

// Tokenize into spans with metadata
var spans = WordBreakTokenizer.Tokenize("self-aware robot");
foreach (var span in spans)
{
    var text = "self-aware robot".Substring(span.Start, span.Length);
    Console.WriteLine($"{text} (IsWord: {span.IsWord})");
}
// → self-aware (IsWord: True)
// →   (IsWord: False)
// → robot (IsWord: True)
```

### Zero-allocation API (net8.0+)

On .NET 8 and later, use the zero-allocation enumerator for high-throughput scenarios:

```csharp
foreach (var token in WordBreakTokenizer.EnumerateWords("hello, world".AsSpan()))
{
    // token.Span is ReadOnlySpan<char> — no heap allocation
    // token.IsWord, token.Start, token.Length also available
}
```

### Quanteda/ICU-compatible mode

By default, colon is treated as MidLetter per the Unicode spec (`"key:value"` → one token). Use `WordBreakOptions.Quanteda` to match ICU/quanteda behavior where colon splits words:

```csharp
var tokens = WordBreakTokenizer.TokenizeToStrings("key:value", WordBreakOptions.Quanteda);
// → ["key", ":", "value"]
```

## API

| Method | Returns | Description |
|--------|---------|-------------|
| `Tokenize(string)` | `List<TokenSpan>` | Word/separator spans with positions and `IsWord` flag |
| `Tokenize(string, WordBreakOptions)` | `List<TokenSpan>` | Same, with custom options |
| `TokenizeToStrings(string)` | `List<string>` | Token strings (convenience) |
| `TokenizeToStrings(string, WordBreakOptions)` | `List<string>` | Same, with custom options |
| `EnumerateWords(ReadOnlySpan<char>)` | `WordTokenEnumerator` | Zero-alloc enumerator (net8.0+) |
| `EnumerateWords(ReadOnlySpan<char>, WordBreakOptions)` | `WordTokenEnumerator` | Same, with custom options |

### TokenSpan

| Property | Type | Description |
|----------|------|-------------|
| `Start` | `int` | Start index in the original string |
| `Length` | `int` | Number of characters in this token |
| `IsWord` | `bool` | `true` for words/numbers, `false` for separators/punctuation |

### WordBreakOptions

| Preset | Behavior |
|--------|----------|
| `WordBreakOptions.Default` | Strict Unicode UAX #29 (colon is MidLetter) |
| `WordBreakOptions.Quanteda` | ICU/quanteda-compatible (colon splits words) |
| `new WordBreakOptions(chars)` | Custom MidLetter exclusions |

## UAX #29 rules implemented

| Rule | Behavior | Example |
|------|----------|---------|
| WB3 | Don't break within CRLF | `\r\n` stays together |
| WB3c | Emoji ZWJ sequences | `👩‍👩‍👧‍👧` stays together |
| WB3d | Group horizontal whitespace | `"a   b"` → `["a", "   ", "b"]` |
| WB4 | Ignore Extend/Format/ZWJ | Combining marks attach to base |
| WB5 | Don't break between letters | `"hello"` → one token |
| WB6/7 | MidLetter keeps words | `"e.g"` → one token |
| WB7a-c | Hebrew letter rules | Hebrew + quote combinations |
| WB8-10 | Numeric sequences | `"12345"` → one token |
| WB11/12 | MidNum keeps numbers | `"1,000,000.50"` → one token |
| WB13 | Katakana sequences | Adjacent katakana stay together |
| WB13a/b | ExtendNumLet (underscore) | `"hello_world"` → one token |
| **keep_hyphens** | Infix hyphens preserved | `"self-aware"` → one token |

## Compatibility

Targets **netstandard2.0** and **net8.0**:

- .NET 8, 9, 10+
- .NET 6, 7
- .NET Framework 4.6.1+
- SQL CLR (SAFE mode)

## Performance

- **Zero-allocation enumerator** (net8.0+): `ref struct` via `EnumerateWords()` — no heap allocation per token
- **Bitwise property matching**: `[Flags]` enum with single-op combined checks
- **ASCII fast path**: pre-computed lookup table for characters 0-127
- **Latin-1 fast path**: direct classification for U+00C0-U+024F
- **Inlined hot paths**: `AggressiveInlining` on frequently called predicates

## Unicode conformance

Validated against the [official Unicode 15.0 WordBreakTest](https://www.unicode.org/Public/15.0.0/ucd/auxiliary/WordBreakTest.txt) suite (1823 test cases), plus [quanteda](https://quanteda.io/) 4.3.1 and [ICU4N](https://github.com/NightOwl888/ICU4N) verification.

## Versioning

Package versions are generated from Git tags using MinVer. Release by pushing a tag like `v1.2.3`.

## License

[MIT](LICENSE)
