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

**MESSAGES.DAT** - Required file containing all message headers and bodies in a QWK packet.

**.NDX** - Index file extension. Conference-specific binary files providing random access to messages (e.g., `0.NDX`, `1.NDX`).

## Message Terms

**Conference** - A message board or forum area within a BBS. Each conference has a numeric identifier.

**Message Header** - A 128-byte record containing message metadata (sender, recipient, subject, date, etc.).

**Message Body** - The text content of a message, stored in 128-byte blocks with 0xE3 line terminators.

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

