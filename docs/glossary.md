---
layout: default  # ← Uses the THEME's default layout
title: QWK.NET - Glossary
---

# Glossary

Short definitions of common terms used in QWK.NET documentation.

## Format Terms

**BBS** - Bulletin Board System. A computer system that provided message boards, file downloads, and online services in the 1980s and 1990s.

**QWK** - Quick BBS Mail Packet format. The original offline mail packet format for BBS systems.

**REP** - Reply packet format. A QWK-compatible format for offline readers to send replies back to BBS systems.

**QWKE** - QWK Extended. An extension to the QWK format that adds long headers, file attachments, and reader configuration.

## File Names

**CONTROL.DAT** - Required metadata file in QWK packets containing BBS information, user details, and conference listings.

**DOOR.ID** - Optional metadata file in QWK packets advertising the mail door's name, version, system type, and supported capabilities. Parsed into a `DoorId` record; absent when the door software does not generate it.

**MESSAGES.DAT** - Required file containing all message headers and bodies in a QWK packet.

**.NDX** - Index file extension. Conference-specific binary files providing random access to messages (e.g., `0.NDX`, `1.NDX`).

## Message Terms

**Conference** - A message board or forum area within a BBS. Each conference has a numeric identifier.

**Message Header** - A 128-byte record containing message metadata (sender, recipient, subject, date, etc.).

**Message Body** - The text content of a message, stored in 128-byte blocks with 0xE3 line terminators.

**Kludge** - A machine-readable control line embedded at the top of a QWK message body, before the human-readable content. Three conventions are recognised: QWKE extended headers (`To:`, `From:`, `Subject:`), @-kludges (lines beginning `@KEY:`), and Ctrl-A kludges (lines beginning with U+0001 or its CP437 glyph). The prefix character is stripped from the stored key in all cases. See [Reading Packets](guides/reading-packets.md) for details.

**Ctrl-A kludge** - A kludge line whose first character is U+0001 (ASCII SOH, Ctrl-A) or U+263A (the CP437 visual glyph for byte 0x01 — a white smiling face). Widely used in FidoNet-style messaging to carry routing and identification data such as `MSGID` and `REPLY`.

**@-kludge** - A kludge line beginning with `@KEY:`, e.g. `@MSGID:` or `@TZ:`. The `@` is a syntax marker and is not stored in the key.

**Unknown files** - Archive files bundled in a QWK packet that are not part of the standard QWK or QWKE file set (`MESSAGES.DAT`, `CONTROL.DAT`, `DOOR.ID`, `TOREADER.EXT`, `TODOOR.EXT`, `WELCOME`, `NEWS`, `GOODBYE`). Accessible via `QwkPacket.UnknownFiles` and `QwkPacket.OpenFile()`.

## Encoding Terms

**CP437** - Code Page 437, also known as DOS Latin US or OEM-US. The character encoding used by DOS-era BBS systems.

**0xE3** - The byte value used as line terminator in QWK message bodies. Represents π (pi) in CP437 encoding.

## Validation Terms

**Validation Mode** - The strictness level for parsing and validation: Strict, Lenient, or Salvage.

**Strict Mode** - Validation mode that throws exceptions immediately on any specification violation.

**Lenient Mode** - Validation mode that logs warnings but continues parsing with default values. Default and recommended for most scenarios.

**Salvage Mode** - Validation mode that attempts best-effort recovery from damaged or corrupted packets.

**Validation Report** - A collection of validation issues (errors, warnings, informational messages) found during packet parsing.

## Archive Terms

**Archive** - A container file (typically ZIP) holding QWK packet files. QWK packets are distributed as archives.

**Archive Extension** - A separate package that adds support for archive formats beyond ZIP (e.g., TAR, RAR).

## Door Terms

**DoorCapability** - An enumeration of capabilities a QWK mail door may advertise in its DOOR.ID file. Capabilities include conference management operations (`Add`, `Drop`, `Reset`, `ResetAll`), message retrieval options (`Yours`, `Mail`, `DeleteMail`), and protocol features (`Attach`, `FileRequest`, `Index`, `TimeZone`, `Via`, `MessageId`, `Control`). Unrecognised values map to `Unknown`. See the [API Overview](api-overview.md) for the full list.

**CONTROLTYPE** - A key in DOOR.ID that advertises a single door capability, e.g. `CONTROLTYPE = ADD`. Multiple `CONTROLTYPE` lines may appear in one DOOR.ID file; each maps to a `DoorCapability` value.

