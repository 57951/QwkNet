# QwkNet.Archives.Tar

TAR archive format extension for QWK.NET - demonstrates third-party extension capabilities.

## Overview

This package adds TAR archive format support to QWK.NET, demonstrating how third-party developers can extend the library's archive handling without modifying the core codebase. It serves both as a functional extension and as a reference implementation for creating additional format extensions.

## Features

- **Read Support**: Extract files from POSIX TAR (ustar) archives
- **Write Support**: Create TAR archives compatible with GNU tar, BSD tar, and other POSIX tools
- **Automatic Detection**: TAR archives are detected via "ustar" magic signature at offset 257
- **Case-Insensitive Matching**: File names match case-insensitively for QWK packet compatibility
- **Subdirectory Support**: Handles nested directory structures
- **Zero Core Dependencies**: Uses only `System.Formats.Tar` from .NET standard library

## Installation

### From Source

```bash
git clone https://github.com/57951/QwkNet.git
cd QwkNet/src/QwkNet.Archives.Tar
dotnet build
```

### NuGet Package (when published)

```bash
dotnet add package QwkNet.Archives.Tar
```

## Usage

### Basic Registration

Before using TAR archives, register the extension with `ArchiveFactory`:

```csharp
using QwkNet.Archive;
using QwkNet.Archives.Tar;

// Register TAR extension
ArchiveFactory.RegisterExtension(new TarArchiveExtension());
```

### Automatic Detection

Once registered, TAR archives are detected automatically:

```csharp
// Open TAR archive (automatic detection)
using (IArchiveReader reader = ArchiveFactory.OpenArchive("packet.tar"))
{
  IReadOnlyList<string> files = reader.ListFiles();
  
  foreach (string fileName in files)
  {
    using (Stream fileStream = reader.OpenFile(fileName))
    {
      // Process file content
    }
  }
}
```

### Explicit Format Specification

You can also specify TAR format explicitly:

```csharp
using (Stream tarStream = File.OpenRead("packet.tar"))
{
  using (IArchiveReader reader = ArchiveFactory.OpenArchive(
    tarStream,
    ArchiveFormatId.From("tar"),
    leaveOpen: false))
  {
    // Use reader
  }
}
```

### Creating TAR Archives

```csharp
using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")))
{
  // Add files
  writer.AddFile("CONTROL.DAT", controlStream);
  writer.AddFile("MESSAGES.DAT", messagesStream);
  writer.AddFile("PERSONAL.NDX", indexStream);
  
  // Save to file
  using (FileStream output = File.Create("packet.tar"))
  {
    writer.Save(output);
  }
}
```

### Complete QWK Packet Example

```csharp
using QwkNet;
using QwkNet.Archive;
using QwkNet.Archives.Tar;

// Register TAR extension
ArchiveFactory.RegisterExtension(new TarArchiveExtension());

// Read QWK packet from TAR archive
using (QwkPacket packet = QwkPacket.Open("mybbs.tar"))
{
  Console.WriteLine($"BBS: {packet.BbsName}");
  Console.WriteLine($"Messages: {packet.Messages.Count}");
  
  foreach (Message message in packet.Messages)
  {
    Console.WriteLine($"From: {message.From}");
    Console.WriteLine($"Subject: {message.Subject}");
  }
}
```

## Supported TAR Formats

- **POSIX ustar** (most common) ✅
- **GNU tar extensions** (partial) ✅
- **V7 TAR** (read-only, no auto-detection) ✅
- **PAX** (limited support) ⚠️

**Unsupported**: Compressed TAR archives (.tar.gz, .tar.bz2, .tar.xz) must be decompressed before use.

### Handling Compressed TAR

For compressed TAR files, decompress the stream first:

```csharp
using (FileStream compressed = File.OpenRead("packet.tar.gz"))
using (GZipStream decompressed = new GZipStream(compressed, CompressionMode.Decompress))
{
  using (IArchiveReader reader = ArchiveFactory.OpenArchive(
    decompressed,
    ArchiveFormatId.From("tar"),
    leaveOpen: false))
  {
    // Use reader
  }
}
```

## Architecture

### Components

- **TarArchiveExtension**: Implements `IArchiveExtension`, provides registration and factory methods
- **TarArchiveReader**: Implements `IArchiveReader`, wraps `System.Formats.Tar.TarReader`
- **TarArchiveWriter**: Implements `IArchiveWriter`, wraps `System.Formats.Tar.TarWriter`

### Detection Mechanism

TAR archives are detected by the POSIX ustar signature:

- **Magic Bytes**: `0x75 0x73 0x74 0x61 0x72 0x00` ("ustar\0")
- **Offset**: 257 bytes from start
- **Minimum Length**: 263 bytes (offset + signature)

V7 TAR format (pre-POSIX) has no reliable signature and requires explicit format specification.

## Creating Your Own Extension

This package serves as a reference for creating additional archive format extensions. Follow these steps:

### 1. Create Extension Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\QwkNet\QwkNet.csproj" />
    <!-- Add format-specific dependencies here -->
  </ItemGroup>
</Project>
```

#### For Repository Developers vs Third-Party Developers
This extension uses `ProjectReference` because it lives in the QwkNet repository:
```xml
<!-- Development within QwkNet repository -->
<ProjectReference Include="..\QwkNet\QwkNet.csproj" />
```

Third-party developers would use PackageReference instead:
```xml
<!-- External developers consuming published package -->
<PackageReference Include="QwkNet" Version="1.0.0" />
```

When this extension is published to NuGet, the ProjectReference is automatically converted to a PackageReference dependency by the build tooling.

### 2. Implement IArchiveReader

```csharp
public sealed class MyArchiveReader : IArchiveReader
{
  public IReadOnlyList<string> ListFiles() { /* ... */ }
  public Stream OpenFile(string name) { /* ... */ }
  public bool FileExists(string name) { /* ... */ }
  public void Dispose() { /* ... */ }
}
```

### 3. Implement IArchiveWriter

```csharp
public sealed class MyArchiveWriter : IArchiveWriter
{
  public void AddFile(string name, Stream content) { /* ... */ }
  public void Save(Stream output) { /* ... */ }
  public void Dispose() { /* ... */ }
}
```

### 4. Implement IArchiveExtension

```csharp
public sealed class MyArchiveExtension : IArchiveExtension
{
  public ArchiveFormatId FormatId => ArchiveFormatId.From("myformat");
  
  public IReadOnlyList<ArchiveSignature> Signatures { get; }
  
  public bool SupportsReading => true;
  public bool SupportsWriting => true;
  
  public MyArchiveExtension()
  {
    Signatures = new[]
    {
      new ArchiveSignature(
        magicBytes: new byte[] { /* format signature */ },
        offset: 0,
        minimumLength: 4)
    };
  }
  
  public IArchiveReader CreateReader(Stream stream, bool leaveOpen)
    => new MyArchiveReader(stream, leaveOpen);
  
  public IArchiveWriter CreateWriter()
    => new MyArchiveWriter();
}
```

### 5. Document and Test

- Create comprehensive unit tests (see `TarArchiveReaderTests.cs`, `TarArchiveWriterTests.cs`)
- Add integration tests (see `TarIntegrationTests.cs`)
- Write clear documentation with usage examples
- Test with real-world files

## Design Principles

### Explicit Registration

Extensions are **never** auto-discovered. This prevents:

- Assembly scanning overhead
- Reflection security concerns
- Unpredictable behaviour
- Dependency conflicts

Users explicitly register extensions they want:

```csharp
ArchiveFactory.RegisterExtension(new TarArchiveExtension());
```

### Thread Safety

Extension implementations must be stateless and thread-safe. The factory may call `CreateReader` or `CreateWriter` from multiple threads concurrently.

### No Core Modifications

Extensions work without modifying QwkNet core library. The core has zero knowledge of TAR or any other third-party format.

### Licensing

Extensions must use permissive licences (MIT, BSD, ISC) compatible with QwkNet's MIT licence.

## Testing

Run tests:

```bash
cd tests/QwkNet.Archives.Tar.Tests
dotnet test
```

Test coverage:

- **TarArchiveExtensionTests**: 18 tests covering registration and integration
- **TarArchiveReaderTests**: 17 tests covering file reading and error handling
- **TarArchiveWriterTests**: 17 tests covering archive creation
- **TarIntegrationTests**: 7 tests proving QWK packet compatibility

**Total**: 59 comprehensive tests

## Limitations

1. **Compression**: TAR archives must be uncompressed (or decompressed externally)
2. **V7 Detection**: Old V7 TAR format cannot be auto-detected
3. **Memory Buffering**: Files are buffered in memory during write operations
4. **Regular Files Only**: Symbolic links, devices, and special files are ignored

## Troubleshooting

### "TAR format not supported"

Ensure the extension is registered:

```csharp
ArchiveFactory.RegisterExtension(new TarArchiveExtension());
```

### "Stream does not contain valid TAR archive"

Check:

1. File is actually TAR format (not compressed)
2. File is not truncated or corrupted
3. For V7 TAR, use explicit format specification

### Compressed TAR Not Working

Decompress first:

```csharp
using (GZipStream gz = new GZipStream(File.OpenRead("file.tar.gz"), CompressionMode.Decompress))
{
  using (IArchiveReader reader = ArchiveFactory.OpenArchive(gz, ...))
  {
    // Use reader
  }
}
```

## Contributing

Contributions welcome! Please:

1. Follow QwkNet coding standards (British English, 2-space indents, no `var`)
2. Add comprehensive tests
3. Update documentation
4. Ensure all tests pass

## Licence

MIT Licence - see [LICENSE](../../LICENSE) for details.

## See Also

- [QwkNet Core Library](../../README.md)
- [QWK Format Specification](http://fileformats.archiveteam.org/wiki/QWK)
- [TAR Format (POSIX)](https://www.gnu.org/software/tar/manual/html_node/Standard.html)
- [System.Formats.Tar Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.formats.tar)

## Acknowledgements

This extension demonstrates QwkNet's extensibility architecture and serves as a reference for third-party developers creating additional archive format support.