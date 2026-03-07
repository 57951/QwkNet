---
layout: default  # ← Uses the THEME's default layout
title: QWK.NET - Writing QWK and REP Packets
---

# Writing QWK and REP Packets

This guide explains how to create REP (reply) packets using QWK.NET. It covers when and why you would write packets, the basic workflow, and important considerations for creating valid packets.

For detailed API reference, see the [API Overview](../api_overview.md).

## What This Guide Is For

This guide is for developers who need to:
- Create REP reply packets to upload back to a BBS
- Generate QWK-compatible packet files programmatically
- Understand the workflow and common considerations when writing packets

If you're reading existing packets, see the [Reading Packets](reading-packets.md) guide instead.

## When and Why to Write Packets

REP packets are reply packets that contain user messages to be uploaded back to the BBS. You would write REP packets when:

- **Building offline mail readers** - Creating reply packets from user responses
- **Implementing BBS mail doors** - Generating reply packets for processing
- **Converting message formats** - Translating from other formats into QWK/REP format
- **Testing and validation** - Creating test packets for validation or round-trip testing

REP packets contain only the messages being sent back to the BBS, along with the necessary control data and index files. They are simpler than full QWK packets, which also include received messages and optional files.

## High-Level Workflow

The process of creating a REP packet follows these steps:

1. **Create packet** - Initialise a `RepPacket` with BBS identifier or control data
2. **Build messages** - Create messages using `MessageBuilder` with required fields
3. **Add messages** - Add messages to the packet in the desired order
4. **Save packet** - Write the packet to a file or stream

The library handles encoding, message ordering, index file generation, and format compliance automatically. You focus on creating the message content, and QWK.NET ensures the output is valid.

## BBS Identifier and Archive Naming

Every REP packet is tied to the BBS it was downloaded from via the **BBS identifier** (BBS ID). The BBS ID drives two naming conventions that BBS servers rely on:

| File | Purpose |
|------|---------|
| `BBSID.REP` | The outer archive file (e.g., `DMINE.REP`) |
| `BBSID.MSG` | The message payload *inside* the archive (e.g., `DMINE.MSG`) |

BBS servers identify the correct reply upload by looking for `BBSID.MSG` inside the archive. A packet containing only a generic `MESSAGES.DAT` is not recognised and will typically be silently dropped.

### Obtaining the BBS ID

The recommended approach is to pass the `ControlDat` from the original QWK packet directly to `RepPacket.Create`. The BBS ID is then taken automatically from the packet's control data:

```csharp
using QwkPacket qwk = QwkPacket.Open("DMINE.QWK");

// BBS ID comes from the original packet — always correct.
using RepPacket rep = RepPacket.Create(qwk.Control);
rep.AddMessage(reply);

// Name the outer archive after the BBS ID as well.
rep.SaveToFile($"{rep.BbsId}.REP"); // → DMINE.REP
```

If you are creating a REP packet without a QWK source, supply the BBS ID explicitly:

```csharp
using RepPacket rep = RepPacket.Create("DMINE");
```

### BBS ID Rules

The BBS ID must satisfy the following requirements:

- **Characters**: ASCII letters (A–Z) and digits (0–9) only.
- **Length**: 1–8 characters.
- **Case**: Automatically normalised to uppercase. Passing `"dmine"` is equivalent to passing `"DMINE"`.

`RepPacket.Create` throws `ArgumentException` immediately if the ID is null, empty, whitespace, too long, or contains disallowed characters (spaces, punctuation, path separators, etc.).

## Common Considerations

### Message Ordering

Messages should be added in the order you want them to appear in the packet. The library maintains this order and generates sequential message numbers automatically. Messages are grouped by conference for index file generation.

### Encoding and Line Endings

QWK packets use CP437 encoding and 0xE3 line terminators. The library handles encoding conversion automatically when you provide text content. You don't need to manually encode text or convert line endings.

### Index File Generation

Index files (`.NDX`) are generated automatically for each conference that contains messages. The library calculates the correct offsets and creates the binary index files. You don't need to manually create or manage index files.

### Field Lengths and Formatting

QWK format has specific field length requirements (e.g., BBS ID is 1-8 characters, message headers are 128 bytes). The library enforces these constraints and handles padding automatically. Ensure your input data fits within the format's constraints.

## Validation and Preservation

### Validation During Writing

The library validates data as you build messages and add them to the packet. Invalid data (e.g., fields that are too long, invalid dates) will cause exceptions. This ensures that packets you create are valid according to the QWK specification.

### Validation After Writing

You can validate a packet after writing it by reading it back and calling `Validate()`. This is useful for:
- Verifying round-trip fidelity (write → read → validate)
- Testing that your code produces valid packets
- Debugging format issues

For information about validation modes and what they check, see [Validation Modes](validation.md).

### Byte Preservation

QWK.NET preserves byte-level fidelity when writing packets. Given the same input data, the library produces identical output bytes across builds. This ensures deterministic packet generation and supports round-trip testing.

## Common Pitfalls

**Adding messages in wrong order:**
- Messages are numbered sequentially based on the order they're added. Ensure messages are added in the desired sequence.

**Field length violations:**
- Fields like BBS ID, message headers, and subject lines have maximum lengths. The library will throw exceptions if limits are exceeded.

**Missing required fields:**
- Messages require certain fields (from, to, subject, conference number). The `MessageBuilder` helps ensure all required fields are set.

**Encoding assumptions:**
- Don't assume UTF-8 or other encodings. The library handles CP437 conversion automatically, but ensure your source text is appropriate for CP437 encoding.

**Index file concerns:**
- You don't need to manually create index files. The library generates them automatically with correct offsets.

**Incorrect outer archive name:**
- Name the `.REP` file after the BBS ID (e.g., `DMINE.REP`). The inner message payload is always `BBSID.MSG` and is generated automatically by `Save()`. Do not attempt to rename or replace this entry manually.

**Using the wrong BBS ID:**
- Always create the `RepPacket` from `qwk.Control` when replying to a downloaded packet. Using a hardcoded or guessed BBS ID risks a mismatch that will cause the BBS server to reject the upload.

## Further Reading

- [API Overview](../api-overview.md) - Complete API reference including `RepPacket` and `MessageBuilder` usage
- [Validation Modes](validation.md) - Understanding validation when reading written packets
