---
layout: default  # ← Uses the THEME's default layout
title: QWK.NET - Diagnostics Tool
---

# Diagnostics Tool

The QWK.NET Diagnostics Tool is a command-line application for inspecting, analysing, and validating QWK and REP packets. It provides comprehensive packet analysis capabilities without requiring you to write code.

## What the Tool Is For

The diagnostics tool performs packet inspection and analysis tasks that are useful for:

- **Packet validation** - Checking packet structure and format compliance
- **Message viewing** - Displaying messages in human-readable formats with CP437 character highlighting
- **Round-trip testing** - Validating QWK → REP → QWK conversion fidelity
- **Batch analysis** - Processing multiple packets in a directory
- **Format investigation** - Examining packet structure, metadata, and contents

The tool provides formatted output in text, JSON, or Markdown formats, making it suitable for both interactive use and automated workflows.

## Typical Use Cases

**Packet inspection and validation:**
- Quickly checking if a packet is valid and well-formed
- Identifying format issues or validation warnings
- Examining packet metadata and structure

**Message viewing:**
- Displaying messages from packets without writing code
- Viewing specific messages, ranges, or conferences
- Inspecting message content with CP437 character highlighting

**Quality assurance:**
- Round-trip testing to verify packet generation fidelity
- Batch analysis of multiple packets
- Automated validation in CI/CD pipelines

**Format analysis:**
- Investigating packet structure and contents
- Understanding validation issues
- Analysing archive file inventories

## When to Use the Tool vs the Core Library

**Use the diagnostics tool when:**
- You need quick packet inspection without writing code
- You want formatted, human-readable output for analysis
- You're debugging packet issues or investigating format problems
- You need batch processing of multiple packets
- You want to integrate packet validation into shell scripts or automation

**Use the core library when:**
- You're building an application that processes packets programmatically
- You need to integrate QWK packet handling into your own code
- You want to create or modify packets programmatically
- You need custom processing logic beyond what the tool provides

The diagnostics tool is built on top of the core library and uses the same validation and parsing logic. It provides a convenient command-line interface for common tasks, whilst the core library gives you full programmatic control.

## Output Formats

The tool supports three output formats, selectable with `--output <text|json|markdown>`.

### Text Output

Text output is the default format. It displays packet information in clearly labelled sections:

```
═══════════════════════════════════════════════════════════════
QWK PACKET ANALYSIS
═══════════════════════════════════════════════════════════════

FILE INFORMATION:
  Path:          /path/to/DEMO1.QWK
  File Name:     DEMO1.QWK
  File Size:     24.3 KB
  Analysis Time: 2024-01-01 12:00:00 UTC
  Mode:          Lenient

PARSE STATUS: ✓ SUCCESS

BBS INFORMATION:
  BBS Name:      Demo BBS
  City:          Anytown, State
  Phone:         555-555-5555
  Sysop:         Sysop Name
  Packet ID:     DEMO
  Packet Date:   1994-03-15 09:00:00
  Door ID:       MailDoor 1.50
  Door System:   PCBoard 15.1
  Capabilities:  Add, Drop, Reset, Yours, Mail

MESSAGE STATISTICS:
  Total Messages:   23
  Private Messages: 2
  Unread Messages:  23
  Read Messages:    0
  Conferences:      4

OPTIONAL FILES:
  - WELCOME
  - NEWS

ADDITIONAL FILES:
  - CUSTOM.DAT
  - BBSINFO.TXT
```

**BBS INFORMATION — Capabilities line:**
The `Capabilities:` line appears beneath the `Door ID:` and `Door System:` lines when the DOOR.ID file lists one or more `CONTROLTYPE` entries. Each capability is shown as its `DoorCapability` enum name (e.g. `Add`, `Drop`, `Reset`). The line is omitted entirely when no capabilities are present.

**OPTIONAL FILES section:**
Lists the standard QWK optional files present in the packet (`WELCOME`, `NEWS`, `GOODBYE`). Omitted when none are present.

**ADDITIONAL FILES section:**
Lists archive files that are not part of the standard QWK or QWKE file set. These are files not recognised as `MESSAGES.DAT`, `CONTROL.DAT`, `DOOR.ID`, `TOREADER.EXT`, `TODOOR.EXT`, `WELCOME`, `NEWS`, or `GOODBYE`. The section is omitted when no such files are present.

### JSON Output

JSON output is intended for automated processing and scripting. The top-level object contains all analysis fields:

```json
{
  "filePath": "/path/to/DEMO1.QWK",
  "fileName": "DEMO1.QWK",
  "fileSize": 24883,
  "analysisTimestamp": "2024-01-01T12:00:00.000Z",
  "validationMode": "Lenient",
  "parseSuccess": true,
  "bbsName": "Demo BBS",
  "packetId": "DEMO",
  "doorId": "MailDoor 1.50",
  "doorCapabilities": ["Add", "Drop", "Reset", "Yours", "Mail"],
  "messageCount": 23,
  "conferenceCount": 4,
  "conferences": [
    { "number": 0, "name": "Main Board", "messageCount": 10 }
  ],
  "optionalFiles": ["WELCOME", "NEWS"],
  "unknownFiles": ["CUSTOM.DAT", "BBSINFO.TXT"],
  "hasValidationErrors": false,
  "hasValidationWarnings": false,
  "validationErrorCount": 0,
  "validationWarningCount": 0
}
```

**`doorCapabilities` field:**
Present only when the packet has a DOOR.ID file and that file lists at least one `CONTROLTYPE`. Each element is the `DoorCapability` enum name as a string. Omitted (not present) when the door has no advertised capabilities.

**`unknownFiles` field:**
Always present when the packet was parsed successfully. Contains the names of archive files not recognised as standard QWK or QWKE files. The array is empty when no such files exist.

### Markdown Output

Markdown output is suitable for saving reports or including in documentation:

```markdown
# Packet Analysis: DEMO1.QWK

## File Information
- **File:** DEMO1.QWK
- **Size:** 24.3 KB

## BBS Information
- **BBS Name:** Demo BBS
- **Door ID:** MailDoor 1.50
- **Door Capabilities:** Add, Drop, Reset, Yours, Mail

## Messages
- **Total:** 23

## Optional Files
- WELCOME
- NEWS

## Additional Files
- CUSTOM.DAT
- BBSINFO.TXT
```

**Door Capabilities bullet:**
The `**Door Capabilities:**` bullet appears after `**Door ID:**` when the packet has a DOOR.ID file with at least one `CONTROLTYPE`. Capabilities are listed as a comma-separated string of `DoorCapability` enum names.

**Additional Files section:**
The `## Additional Files` section appears after `## Optional Files` when the packet contains non-standard archive files. Omitted when no such files exist.

## Commands

For available commands (`analyse`, `batch`, `view`, `roundtrip`) and their options, see the [tool README](https://github.com/0xe25f/QwkNet/blob/main/tools/QwkNet.Diagnostics/README.md).

## Further Reading

- [Validation Modes](../guides/validation.md) - Understanding validation modes used by the tool
- [API Overview](../api-overview.md) - Core library API including `UnknownFiles` and `OpenFile()`
