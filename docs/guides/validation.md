# Validation in QWK.NET

This guide provides a high-level overview of validation in QWK.NET. It explains why validation exists, when to use each validation mode, and what validation warnings mean in practice.

## Why Validation Exists

Historical QWK packets from real-world BBS software often deviate from the specification. Common issues include:

- Missing or incomplete required fields
- Format variations between different BBS implementations
- Data corruption from storage media or incomplete transfers
- Non-standard vendor-specific extensions

Without validation, the library would either fail completely on any deviation (too strict for real-world packets) or silently accept invalid data (too permissive for quality assurance).

Validation provides a balanced approach: it detects and reports issues whilst allowing processing to continue when appropriate. This makes QWK.NET suitable for both strict quality assurance scenarios and practical archival work with historical packets.

## Validation Modes

QWK.NET provides three validation modes, each suited to different scenarios:

### Lenient (Default - Recommended)

**When to use:** Most scenarios where you want to process packets with minor issues.

Lenient mode logs warnings for issues but continues parsing. It applies sensible default values for missing or invalid fields, allowing you to access packet data even when some fields are incomplete. This mode never throws exceptions during parsing, making it suitable for general-purpose applications.

**Typical use cases:**
- General-purpose packet reading applications
- Offline mail readers
- Archive browsing tools
- Most production scenarios

### Strict

**When to use:** Scenarios requiring guaranteed specification compliance.

Strict mode throws `QwkFormatException` immediately upon encountering any structural error or missing required field. Processing stops at the first error, and no default values are applied. Use this mode when invalid packets indicate a serious problem that must halt processing.

**Typical use cases:**
- Production systems requiring strict compliance
- Quality assurance pipelines
- Packet generation tools verifying correct output
- Automated systems where any deviation indicates a problem

### Salvage

**When to use:** Processing damaged, corrupted, or highly suspect packets.

Salvage mode uses aggressive recovery strategies to extract maximum data from problematic packets. It makes best-effort assumptions to recover from truncated records, corrupted index files, and severely malformed headers. Like Lenient mode, it never throws exceptions and accumulates all issues in the validation report.

**Typical use cases:**
- Digital archiving projects processing historical packets
- Forensic analysis of corrupted packets
- Recovery tools for damaged archives
- Research projects where partial data is valuable

## What Warnings Mean in Practice

When using Lenient or Salvage mode, validation issues are recorded in a `ValidationReport`. Understanding the different types of issues helps you assess packet quality:

- **Errors** - Structural problems that prevented correct parsing of some data. The library applied defaults or workarounds, but the data may be incomplete or incorrect.

- **Warnings** - Format deviations or missing optional data. The packet was processed successfully, but some fields may use default values or the data may not match the specification exactly.

- **Infos** - Informational messages about packet characteristics, such as format variations or optional features detected.

In practice, warnings are common with historical packets and usually don't prevent successful processing. Errors indicate more serious issues, but Lenient and Salvage modes still allow you to access whatever data could be recovered.

You can inspect the validation report after opening a packet:

```csharp
using QwkNet;

using QwkPacket packet = QwkPacket.Open("packet.qwk", ValidationMode.Lenient);
ValidationReport report = packet.Validate();

if (!report.IsValid)
{
    // Review issues to assess packet quality
    foreach (var error in report.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
    foreach (var warning in report.Warnings)
    {
        Console.WriteLine($"Warning: {warning.Message}");
    }
}
```

## Choosing a Mode

**Start with Lenient** - It's the default and recommended for most scenarios. It handles real-world format variations gracefully whilst still reporting issues.

**Use Strict** - Only when you need guaranteed specification compliance and cannot accept partial or default data.

**Use Salvage** - When processing known damaged or corrupted packets where maximum data recovery is more important than correctness.

## Further Reading

- [Validation Modes](validation.md) - Detailed explanation of each mode, behavioural differences, and examples
- [API Overview](../api-overview.md) - Complete API reference including validation APIs
