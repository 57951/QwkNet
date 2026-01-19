# Compatibility

This document describes the .NET framework requirements, supported platforms, and compatibility considerations for QWK.NET.

## Target Framework

QWK.NET targets **.NET 10** (`net10.0`).

- **Minimum SDK:** .NET 10 SDK (released 2025)
- **Runtime:** .NET 10 runtime or later
- **Language Version:** Latest C# language features

All projects in the QWK.NET solution target .NET 10, including:
- Core library (`QwkNet`)
- Extension packages (e.g., `QwkNet.Archives.Tar`)
- Test projects
- Tooling projects (Diagnostics, Benchmarking)

## Supported Platforms

QWK.NET is tested and supported on the following platforms:

### macOS
- **Version:** macOS 26+ (Tahoe) or later
- **Architectures:** arm64 (Apple Silicon) and x64 (Intel)

### Windows
- **Version:** Windows 11 or later
- **Architecture:** x64

QWK.NET is untested yet may support the following platforms:

### Linux
- **Architectures:** x64 and arm64
- **Distributions:** Any distribution with .NET 10 runtime support

For build instructions, see [building.md](building.md).

## Dependencies

QWK.NET maintains minimal dependencies to ensure broad compatibility:

### Core Dependencies
- **System.IO.Compression:** Built into .NET runtime (ZIP archive support)
- **System.Text.Encoding.CodePages:** Package reference for CP437 encoding support

### Extension Dependencies
- Extension packages may add additional dependencies (e.g., `System.Formats.Tar` in `QwkNet.Archives.Tar`)

## Version Compatibility

### .NET Version Policy

QWK.NET targets .NET 10 as its minimum supported version. This ensures:
- Access to latest .NET performance improvements
- Modern C# language features
- Long-term support alignment

### Backward Compatibility

The library does not support earlier .NET versions (e.g., .NET 8, .NET 9). If you require support for earlier versions, please file an issue on the project repository.

## Platform-Specific Considerations

### macOS
- No special configuration required
- Works with both Apple Silicon and Intel processors

### Windows
- Requires Windows 11 or later
- No special configuration required

### Linux
- Requires .NET 10 runtime installation
- Compatible with major distributions (Ubuntu, Debian, Fedora, etc.)

## Known Limitations

- **macOS versions:** Only macOS 26+ (Tahoe) is officially supported and tested
- **Windows versions:** Only Windows 11 is officially supported and tested
- **Earlier .NET versions:** Not supported; upgrade to .NET 10

## Reporting Compatibility Issues

If you encounter compatibility issues:

1. Verify you are using .NET 10 SDK and runtime
2. Check that your platform version meets the minimum requirements
3. File an issue on the project repository with:
   - Platform and version details
   - .NET SDK version (`dotnet --version`)
   - Error messages or logs
   - Steps to reproduce
