# Reading QWK and REP Packets

This guide provides a practical workflow for reading QWK and REP packets with QWK.NET. It covers the essential steps to open, inspect, and process packet files.

For detailed API reference, see the [API Overview](../api-overview.md).

## What This Guide Is For

This guide is for developers who need to:
- Read QWK packet files (`.QWK`) from disk, streams, or memory
- Read REP reply packets (`.REP`)
- Access packet metadata, messages, and optional files
- Understand the basic workflow without diving into format specifications

If you're creating reply packets, see the [Writing Packets](writing-packets.md) guide instead.

## Simple Workflow

The standard workflow for reading packets follows these steps:

1. **Open** - Load the packet from a file, stream, or byte array
2. **Inspect** - Access control data (BBS name, creation date, etc.)
3. **Enumerate** - Iterate through messages
4. **Validate** - Check packet integrity (optional but recommended)
5. **Access optional files** - Read WELCOME, NEWS, or other optional files if present

## Minimal Example

Here's a minimal example that demonstrates the core workflow:

```csharp
using QwkNet;

// 1. Open packet
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

// 2. Inspect control data
Console.WriteLine($"BBS: {packet.Control.BbsName}");
Console.WriteLine($"Created: {packet.Control.CreatedAt}");

// 3. Enumerate messages
foreach (Message message in packet.Messages)
{
    Console.WriteLine($"{message.From} → {message.To}: {message.Subject}");
}

// 4. Validate (optional)
ValidationReport report = packet.Validate();
if (!report.IsValid)
{
    Console.WriteLine($"Found {report.Errors.Count} errors");
}

// 5. Access optional files (if present)
if (packet.OptionalFiles.HasFile("WELCOME"))
{
    string welcome = packet.OptionalFiles.GetText("WELCOME");
    Console.WriteLine(welcome);
}
```

## Opening Packets

QWK.NET supports opening packets from multiple sources:

- **File path** - Most common for local files
- **Stream** - For network streams or in-memory processing
- **Byte array** - For data already loaded into memory

See the [API Overview](../api-overview.md) for detailed examples of each approach.

## Validation Modes

By default, QWK.NET uses **Lenient** validation mode, which handles real-world format variations gracefully. You can specify a different mode when opening:

- **Lenient** (default) - Logs warnings, continues parsing
- **Strict** - Throws exceptions on specification violations
- **Salvage** - Best-effort recovery from damaged packets

For detailed information about validation modes and when to use each, see [Validation Modes](validation.md).

## Reading Message Content

Messages contain both header metadata and body content. Access the body through the `Body` property:

```csharp
foreach (Message message in packet.Messages)
{
    MessageBody body = message.Body;
    
    // Access lines (0xE3 terminators removed)
    foreach (string line in body.Lines)
    {
        Console.WriteLine(line);
    }
    
    // Or get decoded text with standard line endings
    string text = body.GetDecodedText();
}
```

For complete message handling examples, see the [API Overview](../api-overview.md).

## Troubleshooting

If you encounter issues reading packets:

- Check that the file is a valid QWK packet (ZIP archive with CONTROL.DAT)
- Verify file encoding and line endings match expectations
- Review validation warnings for format variations
- Consider using Salvage mode for damaged packets

## Further Reading

- [API Overview](../api-overview.md) - Complete API reference and detailed workflows
- [Validation Modes](validation.md) - Detailed validation behaviour and examples
