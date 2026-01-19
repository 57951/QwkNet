# Getting Started with QWK.NET

This guide provides a minimal first-success path for using QWK.NET to read and process QWK packets. For detailed API reference and advanced usage, see the links at the end of this page.

## Prerequisites

- **.NET 10 SDK** - QWK.NET targets .NET 10 (`net10.0`). Ensure you have the .NET 10 SDK installed.
- **A QWK packet file** - You'll need a QWK packet (`.QWK` file) to work with. These are ZIP archives containing `CONTROL.DAT`, `MESSAGES.DAT`, and optional index files.

To verify your .NET version:
```bash
dotnet --version
```

This should show version 10.x.x or later.

## Installation

QWK.NET will be available via NuGet once packaging is complete. The core library package will be `QwkNet`. Installation instructions will be provided when the package is published.

For now, you can build from source. See [building.md](building.md) for instructions.

## Minimal Workflow

The typical workflow follows: **Open → Inspect → Validate → Access optional files**.

Here's a minimal example that opens a QWK packet and displays basic information:

```csharp
using QwkNet;

// Open a QWK packet from a file path
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

// Inspect control data (BBS information)
Console.WriteLine($"BBS Name: {packet.Control.BbsName}");
Console.WriteLine($"BBS ID: {packet.Control.BbsId}");
Console.WriteLine($"Created: {packet.Control.CreatedAt}");

// Enumerate messages
foreach (Message message in packet.Messages)
{
    Console.WriteLine($"{message.From} → {message.To}: {message.Subject}");
}

// Check validation status
ValidationReport report = packet.ValidationReport;
if (!report.IsValid)
{
    Console.WriteLine($"Found {report.Errors.Count} errors, {report.Warnings.Count} warnings");
}
```

### Opening Packets from Different Sources

QWK.NET supports opening packets from file paths, streams, or memory:

```csharp
using QwkNet;

// From file path (most common)
using QwkPacket packet1 = QwkPacket.Open("DEMO1.QWK");

// From stream
using var stream = File.OpenRead("DEMO1.QWK");
using QwkPacket packet2 = QwkPacket.Open(stream);

// From byte array
byte[] data = File.ReadAllBytes("DEMO1.QWK");
using QwkPacket packet3 = QwkPacket.Open(data);
```

### Validation Modes

By default, QWK.NET uses **Lenient** mode, which logs warnings but continues parsing. This is recommended for most scenarios as it handles real-world format variations gracefully.

```csharp
using QwkNet;
using QwkNet.Validation;

// Lenient mode (default) - recommended for most scenarios
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK", ValidationMode.Lenient);

// Strict mode - throws exceptions on any specification violation
using QwkPacket strictPacket = QwkPacket.Open("DEMO1.QWK", ValidationMode.Strict);

// Salvage mode - best-effort recovery from damaged packets
using QwkPacket salvagePacket = QwkPacket.Open("damaged.qwk", ValidationMode.Salvage);
```

### Accessing Optional Files

QWK packets may contain optional files like `WELCOME`, `NEWS`, or `GOODBYE`:

```csharp
using QwkNet;

using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

// Check if an optional file exists
if (packet.OptionalFiles.HasFile("WELCOME"))
{
    // Read as text
    string welcome = packet.OptionalFiles.GetText("WELCOME");
    Console.WriteLine(welcome);
}
```

## Next Steps

Now that you've completed the minimal workflow, explore these resources:

- **[API Overview](api-overview.md)** - Complete API reference with detailed workflows
- **[Reading Packets](guides/reading-packets.md)** - Comprehensive guide to reading QWK and REP packets
- **[Writing Packets](guides/writing-packets.md)** - Guide to creating REP reply packets
- **[Validation](guides/validation.md)** - Detailed explanation of validation modes and when to use them
- **[Archive Format Extensions](guides/extensions.md)** - Extending QWK.NET's archive format support

## Common Tasks

### Reading Message Bodies

```csharp
foreach (Message message in packet.Messages)
{
    MessageBody body = message.Body;
    
    // Access decoded text lines
    foreach (string line in body.Lines)
    {
        Console.WriteLine(line);
    }
}
```

### Checking Message Status

```csharp
foreach (Message message in packet.Messages)
{
    if (message.IsPrivate)
    {
        Console.WriteLine("Private message");
    }
    
    if (message.IsRead)
    {
        Console.WriteLine("Already read");
    }
}
```

### Accessing QWKE Extensions

```csharp
foreach (Message message in packet.Messages)
{
    // Check for QWKE kludges (extended headers)
    if (message.Kludges.Count > 0)
    {
        foreach (var kludge in message.Kludges)
        {
            Console.WriteLine($"Kludge: {kludge.Key} = {kludge.Value}");
        }
    }
}
```

## Further Reading

- **[Architecture](architecture.md)** - Module boundaries and extension points
- **[Compatibility](compatibility.md)** - Platform requirements and .NET version details
