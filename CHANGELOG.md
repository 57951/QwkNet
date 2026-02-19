# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-02-19

### Fixed

- **Critical: message count severely under-reported on packets with large messages.**
`ParseMessages` allocated a `byte[]` buffer and called `stream.Read()` directly to
fill each 128-byte record. `DeflateStream` (which backs every `ZipArchiveEntry`
opened for reading) could have legally returned fewer bytes than requested in a single call even when more data was available — this is permitted by the `Stream.Read` contract.

A short read on a body block was treated as a truncated record: a warning was
emitted, the body-block loop broke early, and the stream was left misaligned at the
mid-block position. All subsequent messages were then read at the wrong offset,
failed the `IsPlausibleMessageHeader` plausibility check, and were silently
discarded. All three `stream.Read()` call sites in `ParseMessages` (copyright block, header block, and each body block) have been replaced with `BinaryRecordReader.ReadRecord()`, which already existed in the codebase and already retried internally until the 128-byte buffer was genuinely full or true end-of-stream was reached. The magic literal `128` at each site has been replaced with `BinaryRecordReader.RecordSize`.


## [1.2.0] - 2026-02-18

### Fixed

- **Critical: kludge extraction used a structural heuristic that produced false positives.**
`ExtractKludges` classified any line at the start of a message body as a kludge if it contained a colon with a single-word key — regardless of whether that key was a known kludge identifier. This could potentially cause legitimate body text to be stripped from the message and stored as spurious kludge entries. This fix replaces the heuristic with prefix-based recognition: only lines beginning with `@` (Synchronet `@`-kludges) or whose key is exactly one of the three QWKE-defined header names (`To`, `From`, `Subject`) are extracted. Any other line stops the scan and remains in the body, as do all lines following it.

Malformed kludge lines did not and cannot prevent a message from being parsed or presented — the scanner stops at the first unrecognised line and the message is delivered in full.

- **Minor: QWKE blank-line separator was consumed even when no kludges preceded it.**
A blank line appearing before any kludge had been found is ordinary body formatting and must not be removed. The blank separator is now consumed only when at least one kludge has already been extracted.

### Notes

- CP437 decoding maps byte `0x01` (FidoNet SOH kludge prefix) to U+263A (`☺`). FidoNet kludges cannot be detected by inspecting decoded line content; supporting them would require inspection of the raw byte stream before CP437 decoding. This is documented in `ExtractKludges` for future reference.


## [1.1.0] - 2026-02-10

### Fixed

- **Critical: message count severely under-reported on compressed packets.** `ParseMessages` relied on a single `Stream.Read()` call to fill each 128-byte record. `DeflateStream` (which backs every `ZipArchiveEntry` opened for reading) may legally return fewer bytes than requested in a single call even when more data is available. A short read was incorrectly treated as a truncated block, causing the body-block loop to break early and leave the remaining bytes of that block unconsumed. All subsequent messages were then read at the wrong offset, failed the plausibility check, and were silently discarded. A real-world packet of 895 messages was parsed as 29. A `ReadBlock()` helper now loops until the 128-byte buffer is genuinely full or true end-of-stream is reached.

- **Critical: stream misalignment after a message-content parse exception.** When an exception occurred during message-content parsing (body decoding, `Message` construction, etc.), the single enclosing `try/catch` block incremented the message counter and continued the loop, but the stream position was correct by coincidence only — the body blocks had already been read. The more dangerous case was validation early-exits (`blockCount` exceeds limit): these used `continue` *before* the body-block read loop, leaving all body blocks unconsumed and misaligning every subsequent message. Restructured into explicit phases — header parse, validation, body-block read, content parse — so that body blocks are always consumed before any skip or error path is taken.

- **Documentation: ambiguous `cref` on `Stream.Read` in `ReadBlock` XML comment** resolved by specifying the `Stream.Read(byte[], int, int)` overload explicitly.


## [1.0.0] - 2026-01-19

### Added
- Initial release
- QwkNet library for reading and validating QWK offline mail packets
- High performance packet parsing (<100ms typical)
- Zero external dependencies in core package
- .NET 10 compatibility (Windows, macOS)
- Strongly typed access to QWK control data and messages
- Access to extended message headers (where present)
- Comprehensive message and control block parsing support
- Integrated packet validation and error reporting
- Custom abstractions for message body decoding
- Example tools and code samples included

### Extensions
- Modular architecture supports third-party archive formats and validators
- Example: `QwkNet.Archives.Tar` project demonstrating extension model

### Validation
- Validated with modern QWK packets and historical packets (1991–2023)
- Round-trip (read/write) tested for all public APIs
- Over 910 unit tests included

### Known Limitations
- Some rare control/metadata blocks may be ignored with warning
- No built-in packet repair tools (not planned)
