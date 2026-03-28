# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.7.0] - 2026-03-28

### Added

- **`MessageBody.RawBytes`** (`ReadOnlyMemory<byte>`) — exposes the concatenated 128-byte body blocks exactly as read from `MESSAGES.DAT`, before any CP437 decoding. Only populated on bodies produced by `QwkPacket.Open()`; empty on bodies created programmatically via `MessageBody.FromRawText()` or `MessageBuilder.SetBodyText()`.

- **`MessageBody(lines, rawText, rawBytes)` constructor** — a new three-argument overload that accepts the raw body bytes alongside the existing CP437-decoded string fields. The existing two-argument constructor is unchanged and sets `RawBytes = ReadOnlyMemory<byte>.Empty`.

- **`MessageBody.GetText(mode, encoding, fallback)` — raw-bytes path** — when `encoding` is non-null the method now operates on `RawBytes` instead of `RawText`. It splits on byte `0xE3` (or CR/CRLF for QWKE bodies) at the byte level, decodes each line with the specified encoding respecting the `DecoderFallbackPolicy`, and joins the result with the line separator implied by `mode`. Previously the `encoding` parameter was accepted but silently ignored; an `InvalidOperationException` is now thrown when `encoding` is non-null and `RawBytes` is empty. The null-encoding path is unchanged (backward compatible).

- **`MessageBodyParser.ParseLines(ReadOnlySpan<byte>, Encoding, DecoderFallbackPolicy)` overload** — new public method used by the raw-bytes path of `GetText`. Can be called directly when byte-level access is needed without a full `MessageBody` object.

### Changed

- **`MessageBody.GetText` XML documentation** — the docs now clearly separate the two execution paths: (1) `encoding == null` → legacy CP437-decoded `RawText`-based path, identical to pre-v1.7.0 behaviour; (2) `encoding != null` → raw-bytes path requiring `RawBytes` to be non-empty.

### Migration guide

No breaking changes. To decode message bodies in an encoding other than CP437 (e.g. UTF-8):

```csharp
using QwkNet;
using System.Text;

using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");
foreach (Message message in packet.Messages)
{
    // Decode body as UTF-8 (works correctly if the BBS stored UTF-8 bodies)
    string utf8Body = message.Body.GetText(
        QwkNet.Encoding.LineEndingMode.Preserve,
        Encoding.UTF8,
        QwkNet.Encoding.DecoderFallbackPolicy.ReplacementUnicode);
}
```

---

## [1.6.0] - 2026-03-15

### Added

- **`QwkPacket.UnknownFiles`** (`IReadOnlyList<string>`) — exposes the names of every file in the packet archive that the library does not recognise as a standard QWK or QWKE entry (`MESSAGES.DAT`, `CONTROL.DAT`, `DOOR.ID`, `TOREADER.EXT`, `TODOOR.EXT`, `WELCOME`, `NEWS`, `GOODBYE`). The list is populated during `Open()` and is empty when all files are known. Comparison is case-insensitive.

- **`QwkPacket.OpenFile(string name)`** (`Stream?`) — opens a raw, caller-owned byte stream for any file in the archive by name (case-insensitive). Returns `null` if no file with that name exists. Works for any archive entry, not only those in `UnknownFiles`. Throws `ArgumentNullException` when `name` is `null`.

- **Ctrl-A kludge recognition** — `ExtractKludges` now recognises the FidoNet SOH prefix convention. Body lines whose first character is U+0001 (SOH, byte `0x01`) or U+263A (the CP437 visual glyph for that byte) are extracted as kludges. The prefix character is stripped from the stored key, so `kludge.Key == "MSGID"` matches regardless of which prefix form was used. This completes the three-convention kludge model.

- **`DoorCapability` — 12 new enum members**: `ResetAll`, `Yours`, `Mail`, `DeleteMail`, `Attach`, `Own`, `FileRequest`, `Index`, `TimeZone`, `Via`, `MessageId`, `Control`. These cover the remaining standard `CONTROLTYPE` values defined in the QWK DOOR.ID specification that were previously mapped to `Unknown`.

- **`DoorId.ControlTypes`** (`IReadOnlyList<string>`) — raw `CONTROLTYPE` line values in document order, preserving original casing and any non-standard values not covered by the `DoorCapability` enum. Useful for round-trip fidelity and for inspecting capabilities mapped to `Unknown`.

- **Diagnostics tool — capabilities display**: the text analyser's `BBS INFORMATION` section now includes a `Capabilities:` line listing all `DoorCapability` names when the packet's DOOR.ID advertises at least one `CONTROLTYPE`. The JSON output includes a `doorCapabilities` array in the same circumstance. The Markdown output includes a `**Door Capabilities:**` bullet after the Door ID line.

- **Diagnostics tool — additional files display**: a new `ADDITIONAL FILES` section (text), `## Additional Files` section (Markdown), and `unknownFiles` array (JSON) report any archive files not belonging to the standard QWK/QWKE file set. The section is omitted (or the array is empty) when no such files are present.

### Changed

- **`@`-kludge keys no longer include the `@` prefix character.** Previously the `@` sigil was retained as part of the key (e.g. `kludge.Key == "@MSGID"`). The `@` is now treated as a syntax marker and stripped, so the stored key is the bare identifier (e.g. `kludge.Key == "MSGID"`). This aligns `@`-kludge behaviour with Ctrl-A kludge behaviour — in both cases, `kludge.Key == "MSGID"` finds the entry regardless of which prefix form was used. Callers that compared against `"@MSGID"` (or similar) must remove the leading `@` from their comparison strings.

---



## [1.5.0] - 2026-03-07

### Fixed

- **Critical: REP packets were silently dropped by BBS servers due to incorrect message payload filename.**
  The message payload inside a REP archive must be named `BBSID.MSG` (e.g., `DMINE.MSG`). The library was previously writing it as the generic `MESSAGES.DAT`, which BBS servers do not recognise as a valid reply upload and discard without error.

### Changed

- `RepPacket.Save()` now writes the message payload as `BBSID.MSG` (e.g., `DMINE.MSG`, `AMIGACTY.MSG`) instead of `MESSAGES.DAT`. Callers naming the outer archive should also use `BBSID.REP` — `rep.BbsId` exposes the normalised identifier for this purpose.
- `RepPacket.BbsId` is now always uppercase. The BBS identifier is normalised to uppercase on construction (e.g., `"dmine"` is stored and returned as `"DMINE"`), ensuring the generated archive entry name is always correct.
- `RepPacket.Create()` now validates that the BBS identifier contains only ASCII letters and digits (`A–Z`, `0–9`) after normalisation. Spaces, punctuation, and path separators throw `ArgumentException` immediately at construction time rather than producing an unusable archive at save time.

---

## [1.4.0] - 2026-03-07

### Fixed

- `RepPacket`: The ASCII message number field (offsets 1–7) in generated REP packets now correctly contains the conference number, per the QWK specification. Previously this field was populated with a sequential counter, which could cause mail doors to silently reject or misroute uploaded replies.

---

## [1.3.2] - 2026-03-07

### Added

- Additional status properties added to `REP` packet structure.

### Improved

- Status byte parsing for both standard and `REP` packet formats.

---

## [1.3.1] - 2026-03-07

### Added

- Enhanced `Message` model with additional status properties.

### Improved

- Improved status byte parsing logic for `QWK` packets.

---

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

---

## [1.2.0] - 2026-02-18

### Fixed

- **Critical: kludge extraction used a structural heuristic that produced false positives.**
`ExtractKludges` classified any line at the start of a message body as a kludge if it contained a colon with a single-word key — regardless of whether that key was a known kludge identifier. This could potentially cause legitimate body text to be stripped from the message and stored as spurious kludge entries. This fix replaces the heuristic with prefix-based recognition: only lines beginning with `@` (Synchronet `@`-kludges) or whose key is exactly one of the three QWKE-defined header names (`To`, `From`, `Subject`) are extracted. Any other line stops the scan and remains in the body, as do all lines following it.

Malformed kludge lines did not and cannot prevent a message from being parsed or presented — the scanner stops at the first unrecognised line and the message is delivered in full.

- **Minor: QWKE blank-line separator was consumed even when no kludges preceded it.**
A blank line appearing before any kludge had been found is ordinary body formatting and must not be removed. The blank separator is now consumed only when at least one kludge has already been extracted.

### Notes

- CP437 decoding maps byte `0x01` (FidoNet SOH kludge prefix) to U+263A (`☺`). FidoNet kludges cannot be detected by inspecting decoded line content; supporting them would require inspection of the raw byte stream before CP437 decoding. This is documented in `ExtractKludges` for future reference.

---

## [1.1.0] - 2026-02-10

### Fixed

- **Critical: message count severely under-reported on compressed packets.** `ParseMessages` relied on a single `Stream.Read()` call to fill each 128-byte record. `DeflateStream` (which backs every `ZipArchiveEntry` opened for reading) may legally return fewer bytes than requested in a single call even when more data is available. A short read was incorrectly treated as a truncated block, causing the body-block loop to break early and leave the remaining bytes of that block unconsumed. All subsequent messages were then read at the wrong offset, failed the plausibility check, and were silently discarded. A real-world packet of 895 messages was parsed as 29. A `ReadBlock()` helper now loops until the 128-byte buffer is genuinely full or true end-of-stream is reached.

- **Critical: stream misalignment after a message-content parse exception.** When an exception occurred during message-content parsing (body decoding, `Message` construction, etc.), the single enclosing `try/catch` block incremented the message counter and continued the loop, but the stream position was correct by coincidence only — the body blocks had already been read. The more dangerous case was validation early-exits (`blockCount` exceeds limit): these used `continue` *before* the body-block read loop, leaving all body blocks unconsumed and misaligning every subsequent message. Restructured into explicit phases — header parse, validation, body-block read, content parse — so that body blocks are always consumed before any skip or error path is taken.

- **Documentation: ambiguous `cref` on `Stream.Read` in `ReadBlock` XML comment** resolved by specifying the `Stream.Read(byte[], int, int)` overload explicitly.

---

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
