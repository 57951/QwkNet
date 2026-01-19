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

## Further Reading

- [Validation Modes](../guides/validation.md) - Understanding validation modes used by the tool
