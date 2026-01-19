# QWK.NET

QWK.NET is a modern C# library for reading, writing, and validating QWK, REP, and QWKE offline message packets used by bulletin board systems (BBS) in the 1980s and 1990s. The library provides byte-accurate parsing and generation whilst maintaining compatibility with historical BBS software implementations.

## Key Features

- Full QWK, REP, and QWKE format support
- Byte-accurate parsing and generation
- CP437 (DOS) encoding support
- Built-in ZIP (1991+) archive handling
- Multiple validation modes (Strict, Lenient, Salvage)
- Zero external dependencies in core package
- .NET 10 compatible
- High performance parsing (<1ms for typical packets)

## Installation

Coming soon. QWK.NET will be available via NuGet once packaging is complete.

## Quick Start

```csharp
using QwkNet;

// Open a QWK packet
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

// Access control information
Console.WriteLine($"BBS Name: {packet.Control.BbsName}");
Console.WriteLine($"BBS ID: {packet.Control.BbsId}");

// Enumerate messages
foreach (Message message in packet.Messages)
{
  Console.WriteLine($"From: {message.From}");
  Console.WriteLine($"To: {message.To}");
  Console.WriteLine($"Subject: {message.Subject}");
  Console.WriteLine($"Body: {message.Body.GetDecodedText()}");
}

// Validate packet
ValidationReport report = packet.ValidationReport;
if (!report.IsValid)
{
  Console.WriteLine($"Validation issues: {report.Errors.Count} errors, {report.Warnings.Count} warnings");
}
```

## Project Structure

The repository is organised into the library, tests, tools, examples, and documentation.

## Licence

MIT License - see [LICENSE](LICENSE) for details.
