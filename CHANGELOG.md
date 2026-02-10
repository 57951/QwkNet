# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
