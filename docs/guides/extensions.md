---
layout: default  # ← Uses the THEME's default layout
title: QWK.NET - Archive Format Extensions
---

# Extending QWK.NET

This guide explains how QWK.NET can be extended to support additional functionality without modifying the core library. It covers the conceptual model, design principles, and how to use existing extensions.

For detailed architectural rules and extension interfaces, see [Architecture](../architecture.md).

## What Extensions Are

Extensions allow you to add functionality to QWK.NET without modifying the core library. They work through well-defined interfaces that the core library provides, enabling third-party developers to extend capabilities whilst maintaining clear boundaries.

The extension system follows a plugin model: extensions implement interfaces defined by the core library, register themselves explicitly, and the core library uses them when appropriate. This design ensures the core remains dependency-free and focused on QWK packet handling.

## Archive Format Extensions

The primary extension point in QWK.NET is archive format support. By default, QWK.NET supports ZIP archives (the standard QWK packet format). Archive format extensions add support for additional formats like TAR, RAR, or other archive types.

### Why Archive Extensions Exist

Historical QWK packets may be stored in various archive formats beyond ZIP. Some BBS systems used TAR archives, and archival collections may contain packets in multiple formats. Archive extensions allow QWK.NET to read these packets without requiring format conversion.

### How Archive Extensions Work

Archive extensions implement the `IArchiveExtension` interface, which provides:

- Format identification and magic byte signatures for automatic detection
- Factory methods to create archive readers and writers
- Metadata about read/write support capabilities

Once registered, extensions are automatically used when opening packets. The core library detects the archive format by examining magic bytes, then delegates to the appropriate extension.

### Using an Archive Extension

To use an archive extension, you must register it explicitly before opening packets:

```csharp
using QwkNet.Archive;
using QwkNet.Archives.Tar;

// Register the TAR extension
ArchiveFactory.RegisterExtension(new TarArchiveExtension());

// Now TAR archives are automatically detected
using QwkPacket packet = QwkPacket.Open("packet.tar");
```

## Design Principles

### Explicit Registration

**QWK.NET uses explicit registration only. There is no auto-discovery.**

Extensions must be explicitly registered by calling `ArchiveFactory.RegisterExtension()`. The library never scans assemblies or uses reflection to discover extensions automatically.

**Rationale:**
- Avoids reflection overhead and security concerns
- Ensures deterministic behaviour
- Prevents unexpected dependencies from being loaded
- Makes extension dependencies explicit in application code

This design choice means you control exactly which extensions are active in your application. If you don't register an extension, it won't be used, even if the extension assembly is present.

### No Core Modifications Required

Extensions work entirely through public interfaces. The core library has no knowledge of specific extensions like TAR or RAR. This means:

- Extensions can be developed independently
- Core library updates don't require extension updates
- Multiple extensions can coexist without conflicts
- Extensions are truly additive, not invasive

### Thread Safety

Extension implementations must be stateless and thread-safe. A single extension instance may be used concurrently from multiple threads. The factory methods (`CreateReader`, `CreateWriter`) may be called from any thread.

### Interface-Based Design

Extensions implement well-defined interfaces (`IArchiveExtension`, `IArchiveReader`, `IArchiveWriter`). The core library interacts with extensions only through these interfaces, ensuring compatibility and allowing extensions to evolve independently.

## Creating Extensions

If you want to create your own archive format extension, the TAR extension serves as a complete reference implementation. The process involves:

1. Implementing `IArchiveReader` for reading archives
2. Implementing `IArchiveWriter` for writing archives (if supported)
3. Implementing `IArchiveExtension` to tie everything together
4. Providing magic byte signatures for automatic format detection
5. Registering the extension with `ArchiveFactory`

For detailed implementation guidance, code examples, and architectural rules, see:

- [Architecture](../architecture.md) - Extension rules, interface definitions, and architectural constraints

## Extension Limitations

Extensions are currently limited to archive format support. The extension system is designed specifically for archive formats, and other types of extensions are not supported at this time.

The core library focuses on QWK packet handling and remains dependency-free. Extensions provide the flexibility to support additional archive formats without compromising this design.

## Further Reading

- [Architecture](../architecture.md) - Complete architectural documentation including extension rules, interface definitions, and dependency constraints
- [API Overview](../api-overview.md) - Archive abstraction APIs and extension interfaces
