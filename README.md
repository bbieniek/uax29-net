# Uax29.Net

[![NuGet](https://img.shields.io/nuget/v/Uax29.Net.svg)](https://www.nuget.org/packages/Uax29.Net)
[![CI](https://github.com/bbieniek/uax29-net/actions/workflows/ci.yml/badge.svg)](https://github.com/bbieniek/uax29-net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Managed .NET implementation of [Unicode UAX #29](https://www.unicode.org/reports/tr29/) word boundary segmentation. Zero dependencies, no native ICU required.

## Installation

```bash
dotnet add package Uax29.Net
```

## Versioning

Package and assembly versions are generated automatically from Git tags using MinVer.

- Release by creating an annotated tag like `v1.2.3` and pushing it.
- The publish workflow runs on `v*` tags and packs/pushes that exact semantic version.
- Builds from non-tag commits produce prerelease versions (for example `1.2.4-preview.0.<height>`).

## Updating Unicode Data

The tokenizer's Extended_Pictographic table is generated at build time from `src/Uax29.Net/UnicodeData/emoji-data.txt`.

To update that source data file, run:

```bash
./scripts/update-unicode-data.sh           # defaults to Unicode 14.0.0
./scripts/update-unicode-data.sh 15.1.0    # fetch a specific Unicode version
```

After updating, run `dotnet test` and commit both the data file and any behavior/test changes.

## Quick start

```csharp
using Uax29.Net;

var tokens = WordBreakTokenizer.TokenizeToStrings("hello,world");
// → ["hello", ",", "world"]

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

## API

### `WordBreakTokenizer.Tokenize(string text)`

Returns a `List<TokenSpan>` where each span represents a word or separator segment. Concatenating all spans reproduces the original text.

### `WordBreakTokenizer.TokenizeToStrings(string text)`

Returns a `List<string>` of token strings. Convenience method for when you don't need span positions.

### `TokenSpan`

| Property | Type | Description |
|----------|------|-------------|
| `Start` | `int` | Start index in the original string |
| `Length` | `int` | Number of characters in this token |
| `IsWord` | `bool` | `true` for words/numbers, `false` for separators/punctuation |

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

The `keep_hyphens` rule matches the default behavior of ICU and [quanteda](https://quanteda.io/): hyphens (`\p{Pd}`) between letters or digits are preserved as part of the word token.

## Compatibility

Targets **netstandard2.0** — works on:

- .NET 8, 9, 10+
- .NET 6, 7
- .NET Framework 4.6.1+
- .NET Framework 4.0 (via source linking)
- SQL CLR

## Performance

- **ASCII fast path**: pre-computed lookup table for characters 0-127
- **Latin-1 fast path**: direct classification for U+00C0-U+024F (accented European characters)
- **Single-pass tokenization**: classifies properties and emits tokens without intermediate allocations
- **Inlined hot paths**: `AggressiveInlining` on frequently called helpers

## How it works

1. **Classify** each character's UAX #29 Word_Break property (using an ASCII lookup table for the fast path, then Unicode category lookups for non-ASCII)
2. **Apply break rules** between each pair of adjacent positions, following UAX #29 rules WB1-WB999 plus the `keep_hyphens` extension
3. **Emit tokens** with `IsWord` flag based on whether the token contains letters/digits

The implementation is verified against [quanteda](https://quanteda.io/) 4.3.1 (ICU 71.1) tokenization output and [ICU4N](https://github.com/NightOwl888/ICU4N) test cases.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for any new behavior
4. Ensure all tests pass (`dotnet test`)
5. Submit a pull request

When adding test cases for tokenization behavior, verify expected output against R's quanteda:

```r
library(quanteda)
tokens("your text here", what = "word", remove_separators = FALSE)
```

## License

[MIT](LICENSE)
