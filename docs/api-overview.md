# QWK.NET API Overview

This document provides a high-level map of the QWK.NET library API, entry points, and typical workflows.

## Entry Points

The library provides two main entry points for working with QWK packets:

### QwkPacket

The primary entry point for reading QWK packets. Use `QwkPacket.Open()` to load packets from various sources:

```csharp
using QwkNet;

// Open from stream
using var stream = File.OpenRead("DEMO1.QWK");
using QwkPacket packet = QwkPacket.Open(stream);

// Open from file path
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

// Archive entry limit defaults to max(100MB, 16MB * 10) = 160MB
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK", ValidationMode.Lenient, maxMessageSizeMB: 16);

// Configure entry size limit for archive access
using IArchiveReader reader = ArchiveFactory.OpenArchive("DEMO1.QWK", maxEntrySizeMB: 200);


// Set both message and entry limits explicitly
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK",
    ValidationMode.Lenient,
    maxMessageSizeMB: 1,
    maxEntrySizeMB: 50);

// Open from memory
byte[] data = File.ReadAllBytes("DEMO1.QWK");
using QwkPacket packet = QwkPacket.Open(data);
```

#### QwkPacket.Open() Sizes

| maxMessageSizeMB | maxEntrySizeMB (not specified) | Effective Entry Limit |
|------------------|--------------------------------|----------------------|
| 16 (default)     | null                           | max(100, 16×10) = 160MB |
| 1                | null                           | max(100, 1×10) = 100MB |
| 5                | null                           | max(100, 5×10) = 100MB |
| 20               | null                           | max(100, 20×10) = 200MB |
| Any              | 50                             | 50MB (explicit) |

#### ArchiveFactory.OpenArchive()

| maxEntrySizeMB | Effective Entry Limit |
|----------------|----------------------|
| null (default) | 100MB |
| 50             | 50MB (explicit) |

## Security Benefits

1. **Configurable Limits**: Applications can set appropriate limits for their security
2. **Consistent Security**: Entry limits automatically scale with message limits
3. **Prevents DoS**: Malicious archives with oversized entries are rejected before memory allocation
4. **Simplicity**: Open()'ing a QWK packet works with sensible defaults


### RepPacket

The entry point for creating reply packets (REP format):

```csharp
using QwkNet;

RepPacket rep = RepPacket.Create(packet.Control);
rep.AddMessage(message);
using var output = File.Create("REPLY.REP");
rep.Save(output);
```

## Typical Workflows

### Reading a QWK Packet

The standard workflow follows: **Open → Inspect → Validate → Access optional files**.

```csharp
using QwkNet;

// 1. Open packet
// If a packet is larger than 16MB in size, use 
// QwkPacket.Open(path, mode, maxMessageSizeMB)
using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

// 2. Inspect control data
Console.WriteLine($"BBS: {packet.Control.BbsName}");
Console.WriteLine($"Created: {packet.Control.CreatedAt}");

// 3. Enumerate messages
foreach (Message message in packet.Messages)
{
    Console.WriteLine($"{message.From} → {message.To}: {message.Subject}");
}

// 4. Validate packet integrity
ValidationReport report = packet.Validate();
if (!report.IsValid)
{
    foreach (var error in report.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
    foreach (var warning in report.Warnings)
    {
        Console.WriteLine($"Warning: {warning.Message}");
    }
}

// 5. Access DOOR.ID metadata (if present)
if (packet.DoorId != null)
{
    Console.WriteLine($"Door: {packet.DoorId.DoorName} {packet.DoorId.Version}");
}

// 6. Access optional files (lazy-loaded)
if (packet.OptionalFiles.HasFile("WELCOME"))
{
    string welcome = packet.OptionalFiles.GetText("WELCOME");
    Console.WriteLine(welcome);
}
```

### Creating a Reply Packet

Build a REP packet from messages:

```csharp
using QwkNet;

// Create reply packet using control data from original
RepPacket rep = RepPacket.Create(originalPacket.Control);

// Build and add messages
var builder = new MessageBuilder();
builder.SetFrom("User Name");
builder.SetTo("Sysop");
builder.SetSubject("Re: Test");
builder.SetBody("This is a reply message.");
Message reply = builder.Build();

rep.AddMessage(reply);

// Save to file
using var output = File.Create("REPLY.REP");
rep.Save(output);
```

### Working with Messages

Access message content and metadata:

```csharp
using QwkNet;

// If a packet is larger than 16MB in size, use 
// QwkPacket.Open(path, mode, maxMessageSizeMB)
using QwkPacket packet = QwkPacket.Open("packet.qwk");

foreach (Message message in packet.Messages)
{
    // Access header fields
    Console.WriteLine($"Message #{message.MessageNumber}");
    Console.WriteLine($"Conference: {message.ConferenceNumber}");
    Console.WriteLine($"From: {message.From}");
    Console.WriteLine($"To: {message.To}");
    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine($"Date: {message.DateTime}");
    
    // Access message body
    MessageBody body = message.Body;
    foreach (string line in body.Lines)
    {
        Console.WriteLine(line);
    }
    
    // Get decoded text with standard line endings
    string decodedText = body.GetDecodedText();
    
    // Get encoded text with QWK line terminators (0xE3)
    string encodedText = body.GetEncodedText();
    
    // Check message status flags
    if (message.IsPrivate)
    {
        Console.WriteLine("Private message");
    }
    if (message.IsRead)
    {
        Console.WriteLine("Message has been read");
    }
    if (message.IsDeleted)
    {
        Console.WriteLine("Message is deleted");
    }
    
    // Access QWKE kludges (if present)
    if (message.Kludges.Count > 0)
    {
        foreach (var kludge in message.Kludges)
        {
            Console.WriteLine($"Kludge: {kludge.Key} = {kludge.Value}");
        }
    }
}
```

### Validation Modes

Control how the library handles malformed packets:

**Lenient** (default) - Logs warnings, continues parsing:
```csharp
// If a packet is larger than 16MB in size, use 
// QwkPacket.Open(path, mode, maxMessageSizeMB)
QwkPacket packet = QwkPacket.Open("DEMO1.QWK", ValidationMode.Lenient);
```

**Strict** - Throws exceptions on specification violations:
```csharp
QwkPacket packet = QwkPacket.Open("DEMO1.QWK", ValidationMode.Strict);
```

**Salvage** - Best-effort recovery from damaged packets:
```csharp
QwkPacket packet = QwkPacket.Open("DEMO1.QWK", ValidationMode.Salvage);
```

## API Structure

### Core Types

- **QwkPacket** - Main packet container with control data, messages, conferences, optional files, and DOOR.ID metadata
  - `Control` - ControlDat instance with packet metadata
  - `Messages` - MessageCollection of all messages
  - `Conferences` - ConferenceCollection of conference definitions
  - `OptionalFiles` - OptionalFileCollection for lazy-loaded files
  - `DoorId` - DOOR.ID metadata (nullable if not present)
  - `Validate()` - Returns ValidationReport
- **RepPacket** - Builder for creating reply packets
- **ControlDat** - Packet metadata containing:
  - `BbsName`, `BbsId`, `CreatedAt` - BBS identification and creation time
  - `Conferences` - List of conference definitions
  - `WelcomeFile`, `NewsFile`, `GoodbyeFile` - Optional file names
  - `RawLines` - All original lines for byte fidelity
- **Message** - Individual message with header and body
- **MessageCollection** - Collection of messages with indexing support
- **ConferenceCollection** - Collection of conference definitions
- **OptionalFileCollection** - Lazy-loaded access to optional packet files

### Supporting Types

- **MessageBody** - Message content with encoding support
  - `Lines` - Read-only list of message lines (0xE3 terminators removed)
  - `RawText` - Original message text with line terminators preserved
  - `GetDecodedText()` - Returns message text with standard line endings
  - `GetEncodedText()` - Returns message text with QWK line terminators (0xE3)
- **MessageBuilder** - Fluent API for constructing messages
- **ValidationReport** - Results from packet validation
  - `Errors` - List of error-level validation issues
  - `Warnings` - List of warning-level validation issues
  - `Infos` - List of informational validation messages
  - `IsValid` - True if no errors or warnings
- **IndexFile** - NDX file parser for message indexing
  - Implements `IReadOnlyList<IndexEntry>` for direct enumeration
  - `Entries` - Read-only list of index entries
- **IndexEntry** - Single entry in an index file
  - `MessageNumber` - The message number (1-based)
  - `RecordOffset` - Record offset within MESSAGES.DAT
- **DoorId** - DOOR.ID metadata parser
  - `DoorName`, `Version` - Door identification
  - `Capabilities` - Set of supported door capabilities
  - `RawEntries` - All raw key-value entries for byte fidelity

### QWKE Extensions

- **ToReaderExtParser** - Static parser for TOREADER.EXT files
  - Returns `IReadOnlyList<ToReaderCommand>` from parsed commands
- **ToDoorExtParser** - Static parser for TODOOR.EXT files
  - Returns `IReadOnlyList<ToDoorCommand>` from parsed commands

### Archive Abstractions

- **IArchiveReader** - Interface for reading archive formats
  - `ListFiles()` - Returns list of all file names in the archive
  - `OpenFile(string name)` - Opens a file stream for reading
  - `FileExists(string name)` - Checks if a file exists in the archive
- **IArchiveWriter** - Interface for writing archive formats
  - `AddFile(string name, Stream content)` - Adds a file to the archive
  - `Save(Stream output)` - Finalises and writes the archive
- **IArchiveExtension** - Extension point for custom archive formats

## Extension Points

The library supports extension via archive format adapters. See [Architecture](architecture.md) for extension rules.

```csharp
using QwkNet.Archive;
using QwkNet.Archive.Extensions;

ArchiveFactory.RegisterExtension(new MyArchiveExtension());
```

## Further Reading

- [Architecture](architecture.md) - Module boundaries and extension rules
- [Validation Modes](guides/validation.md) - Detailed validation behaviour
