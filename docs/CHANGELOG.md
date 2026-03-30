# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-03-30

### Added

- `WordBreakTokenizer.Tokenize()` — returns `List<TokenSpan>` with word/separator spans
- `WordBreakTokenizer.TokenizeToStrings()` — returns `List<string>` of token strings
- UAX #29 word break rules WB3-WB13b, WB999
- `keep_hyphens` rule preserving infix hyphens between letters/digits
- ASCII lookup table fast path for characters 0-127
- Latin-1 fast path for U+00C0-U+024F
- Full Unicode script support (Latin, Greek, Cyrillic, Hebrew, Katakana, etc.)
- Emoji ZWJ sequence handling (rule WB3c)
- 36 test cases verified against quanteda 4.3.1 (ICU 71.1) and ICU4N

[1.0.0]: https://github.com/bbieniek/uax29-net/releases/tag/v1.0.0
