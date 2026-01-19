# QWK.NET Architecture

This document describes the architectural boundaries, extension mechanisms, and dependency rules for the QWK.NET library.

## Overview

QWK.NET is structured as a core library with extension points for archive formats. The architecture prioritises:

- **Zero dependencies** in the core package
- **Explicit extension registration** (no auto-discovery)
- **Thread safety** for read-only operations
- **Byte-faithful preservation** of QWK packet formats

## Module Boundaries

### Core Library (`QwkNet`)

The core library (`src/QwkNet/`) contains all QWK packet parsing, validation, and generation logic. It is dependency-free except for .NET standard library types.

**Responsibilities:**
- QWK packet parsing (`QwkPacket`, `RepPacket`)
- Message handling (`Message`, `MessageCollection`, `MessageBody`)
- Control data parsing (`ControlDat`, `ConferenceCollection`)
- Index file parsing (`IndexFile`, `IndexEntry`)
- QWKE extension support (`ToReaderExt`, `ToDoorExt`, `QwkeLongHeaders`)
- Validation (`ValidationReport`, `PacketValidator`)
- Encoding support (`Cp437Encoding`, `LineEndingProcessor`)
- Archive abstraction (`IArchiveReader`, `IArchiveWriter`, `ArchiveFactory`)

**Built-in Archive Support:**
- ZIP format only (via `System.IO.Compression`)
- No external archive libraries

**Module Structure:**
```
QwkNet/
├── Archive/              # Archive abstraction layer
│   ├── Extensions/       # Extension interfaces
│   └── Zip/              # Built-in ZIP implementation
├── Core/                 # Binary record parsing utilities
├── Encoding/             # CP437, line endings, text analysis
├── Models/               # Domain models
│   ├── Control/          # CONTROL.DAT structures
│   ├── Indexing/         # NDX file structures
│   ├── Messages/         # Message structures
│   └── Qwke/             # QWKE extension structures
├── Parsing/              # Format parsers
│   └── Qwke/             # QWKE parsers
└── Validation/           # Validation logic
```

### Extension Packages (`QwkNet.Archives.*`)

Extension packages add support for additional archive formats beyond ZIP. They are separate NuGet packages that depend on the core `QwkNet` package.

**Example:** `QwkNet.Archives.Tar` provides TAR archive support.

**Responsibilities:**
- Implement `IArchiveExtension` interface
- Provide `IArchiveReader` and `IArchiveWriter` implementations
- Register format signatures for automatic detection
- Document usage and limitations

**Extension Package Structure:**
```
QwkNet.Archives.Tar/
├── TarArchiveExtension.cs    # Implements IArchiveExtension
├── TarArchiveReader.cs       # Implements IArchiveReader
├── TarArchiveWriter.cs       # Implements IArchiveWriter
└── README.md                 # Usage documentation
```

### Tools (`QwkNet.Diagnostics`, `QwkNet.Benchmarking`)

Command-line tools that consume the core library. They are separate projects with their own entry points.

**Responsibilities:**
- Diagnostic analysis of QWK packets
- Performance benchmarking
- Format validation and reporting
- Example usage demonstrations

**Dependencies:**
- Tools depend on `QwkNet` core library
- Tools may have their own dependencies (e.g., CLI frameworks)

## Extension Rules

### Extension Interface

All archive format extensions must implement `IArchiveExtension`:

```csharp
public interface IArchiveExtension
{
  ArchiveFormatId FormatId { get; }
  IReadOnlyList<ArchiveSignature> Signatures { get; }
  bool SupportsReading { get; }
  bool SupportsWriting { get; }
  IArchiveReader CreateReader(Stream stream, bool leaveOpen);
  IArchiveWriter CreateWriter();
}
```

### Extension Registration

**RULE:** Extensions must be explicitly registered. There is no auto-discovery or assembly scanning.

```csharp
using QwkNet.Archive;
using QwkNet.Archives.Tar;

// Explicit registration required
ArchiveFactory.RegisterExtension(new TarArchiveExtension());
```

**Rationale:**
- Avoids reflection overhead
- Prevents security concerns from assembly scanning
- Ensures deterministic behaviour

### Extension Detection

Extensions are detected via magic byte signatures:

1. **Signature Matching:** Extensions provide `ArchiveSignature` instances with magic bytes, offset, and minimum length
2. **Detection Order:** Registered extensions are tested first (in registration order), then built-in ZIP
3. **Precedence:** Longer signatures take precedence over shorter ones
4. **Explicit Format:** Users can bypass detection by specifying `ArchiveFormatId` directly

### Extension Thread Safety

**RULE:** Extension implementations must be stateless and thread-safe.

- A single `IArchiveExtension` instance may be used concurrently across threads
- `CreateReader` and `CreateWriter` methods may be called from multiple threads
- Extension instances should not maintain mutable state

### Extension Licensing

**RULE:** Extension packages must use permissive licences compatible with MIT.

**Acceptable:**
- MIT
- BSD (2-clause or 3-clause)
- Apache 2.0
- ISC

**Unacceptable:**
- GPL (any version)
- Commercial licences requiring payment
- Licences with attribution requirements in UI

### Extension Versioning

**RULE:** Extension packages must follow semantic versioning (SemVer).

- Major version increments for breaking changes
- Minor version increments for new features (backward compatible)
- Patch version increments for bug fixes
- Document version compatibility with core `QwkNet` package versions

### Extension Documentation

**RULE:** Extension packages must provide useful documentation.

**Required:**
- README.md with usage examples
- Installation instructions
- Format-specific limitations or quirks
- Compatibility notes with core library versions

**Recommended:**
- Code examples demonstrating registration and usage
- Troubleshooting guide for common issues
- Performance considerations if applicable

### Extension Dependencies

**RULE:** Extensions should minimise external dependencies.

- Prefer .NET standard library types (`System.Formats.Tar`, `System.IO.Compression`)
- Document why each external dependency is required
- Ensure dependencies use compatible licences

### Extension Design Principles

**RULE:** Extensions must not require core package modification.

- Extensions work through public interfaces only
- No changes to core library code should be necessary
- Extensions are additive, not invasive

**RULE:** Extensions should avoid global or static state.

- Prefer instance-based state management
- Thread-safe extension instances (see [Extension Thread Safety](#extension-thread-safety))
- Stateless implementations where possible

### Extension Examples

Common extension types include:

- **Archive Format Adapters:** RAR, 7z, LHA, pre-1991 ZIP variants
- **Encoding Helpers:** ANSI/CP437 rendering utilities, custom encoding converters
- **Export Plugins:** Message export to JSON, mbox, or other formats
- **Validation Extensions:** Custom validation rules or format-specific checks

These examples demonstrate the extension model's flexibility whilst maintaining clear boundaries with the core library.

## Dependency Rules

### Core Library Dependencies

**RULE:** The core `QwkNet` library must remain dependency-free.

**Allowed:**
- .NET standard library types (`System.*`, `System.IO.*`, `System.Collections.Generic.*`)
- `System.IO.Compression` (for built-in ZIP support only)

**Forbidden:**
- Third-party NuGet packages
- External libraries
- Reflection-heavy operations (except where required by .NET)

### Extension Package Dependencies

**RULE:** Extension packages may depend on the core `QwkNet` package and format-specific libraries.

**Allowed:**
- `QwkNet` core package (via `PackageReference` or `ProjectReference`)
- Format-specific libraries (e.g., `System.Formats.Tar` for TAR support)
- Minimal utility libraries if absolutely necessary

**Forbidden:**
- Dependencies that would require the core library to change

### Tool Dependencies

**RULE:** Tools may have their own dependencies but must not expose them to library consumers.

**Allowed:**
- CLI frameworks
- JSON serialisation libraries (for tool output)
- Testing frameworks (for tool tests)

**Forbidden:**
- Dependencies that leak into the core library API
- Dependencies that require core library changes

### Dependency Direction

**RULE:** Dependencies must flow in one direction only.

```
Tools → Core Library
Extensions → Core Library
Core Library → .NET Standard Library only
```

**Forbidden:**
- Core Library → Extensions
- Core Library → Tools
- Extensions → Tools

## Thread Safety

### Read-Only Operations

**RULE:** All read-only operations must be thread-safe.

- `QwkPacket` instances can be read concurrently from multiple threads
- `MessageCollection` enumeration is thread-safe
- `IArchiveReader` implementations must be thread-safe for concurrent reads

### Mutable Operations

**RULE:** Mutable operations are not thread-safe.

- `RepPacket` construction is not thread-safe (single-threaded builder pattern)
- `ArchiveFactory.RegisterExtension` is thread-safe (uses locking)
- Extension registration/unregistration is thread-safe

### Extension Thread Safety

**RULE:** Extension implementations must be stateless.

- `IArchiveExtension` instances should not maintain mutable state
- `IArchiveReader` and `IArchiveWriter` instances may be used from multiple threads if documented

## Byte Fidelity

### Preservation Requirements

**RULE:** The library must preserve byte-level fidelity where possible.

- CP437 encoding preserved (no UTF-8 conversion)
- MSBIN floats maintained in index files
- 128-byte records with space padding preserved
- 0xE3 line terminators maintained
- Round-trip fidelity (QWK → REP → QWK preserves bytes)

### Format Tolerance

**RULE:** Support real-world format variations whilst maintaining fidelity.

- Accept multiple date formats (hyphen/slash, 2-digit/4-digit years)
- Handle malformed packets gracefully (via validation modes)
- Preserve original bytes even when parsing tolerates variations

## Validation Modes

### Mode Behaviour

**RULE:** Validation modes control error handling, not byte preservation.

- **Strict:** Throws exceptions on specification violations
- **Lenient:** Logs warnings, continues parsing (default)
- **Salvage:** Best-effort recovery from damaged packets

**RULE:** Validation mode does not affect byte preservation. All modes preserve original bytes.

## Namespace Organisation

### Core Namespaces

- `QwkNet` - Public API entry points (`QwkPacket`, `RepPacket`)
- `QwkNet.Archive` - Archive abstraction (`ArchiveFactory`, `IArchiveReader`, `IArchiveWriter`)
- `QwkNet.Archive.Extensions` - Extension interfaces (`IArchiveExtension`)
- `QwkNet.Archive.Zip` - Built-in ZIP implementation (internal)

### Extension Namespaces

**RULE:** Extension packages should use namespaces that match their package name.

- `QwkNet.Archives.Tar` - TAR extension package
- `QwkNet.Archives.Rar` - Hypothetical RAR extension package

### Internal Namespaces

**RULE:** Internal implementation details use `Internal` suffix or are marked `internal`.

- Parsing logic is internal
- Binary record readers are internal
- Format-specific implementations are internal

## Summary

The QWK.NET architecture enforces clear boundaries:

1. **Core Library:** Dependency-free, handles all QWK packet logic
2. **Extensions:** Separate packages, explicit registration, interface-based
3. **Tools:** Separate projects, consume core library
4. **Dependencies:** One-way flow (Tools/Extensions → Core → .NET)
5. **Thread Safety:** Read-only operations thread-safe, mutable operations single-threaded
6. **Byte Fidelity:** Preserve original bytes, tolerate format variations

This architecture ensures the library remains maintainable, extensible, and compatible with modern .NET deployment scenarios whilst preserving historical QWK packet formats accurately.
