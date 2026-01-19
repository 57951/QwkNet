# QWK.NET

QWK.NET is a preservation-grade C# library for reading, writing, and validating QWK, REP, and QWKE offline message packets used by bulletin board systems (BBS) from the 1980s and 1990s. The library provides byte-accurate parsing and generation whilst maintaining compatibility with historical BBS software implementations, handling real-world format variations gracefully without sacrificing fidelity to the original specifications.

## Who This Library Is For

QWK.NET serves developers and archivists working with historical BBS data:

- **BBS software developers** building or maintaining mail door software
- **Offline mail reader authors** creating tools for reading QWK packets
- **Digital archivists and retrocomputing projects** preserving historical BBS content
- **Tooling developers** building exporters, converters, or analysers for QWK data

## Key Capabilities

- **Complete format support** - QWK packets, REP reply packets, and QWKE extensions (TOREADER.EXT, TODOOR.EXT, long headers)
- **Byte-accurate preservation** - CP437 encoding, MSBIN floats, 128-byte records, and QWK-specific line terminators maintained exactly
- **Real-world tolerance** - Handles format variations (date formats, missing fields, non-standard extensions) common in historical packets
- **Multiple validation modes** - Strict, Lenient (default), and Salvage modes for different use cases
- **Archive handling** - Built-in ZIP support with extensible architecture for other formats
- **Zero dependencies** - Core library has no external dependencies
- **.NET 10 compatible** - Modern C# implementation targeting .NET 10

## Get Started

**[Getting Started Guide](getting-started.md)** - Step-by-step introduction to using QWK.NET

**[GitHub Repository](https://github.com/57951/QwkNet)** - Source code, issues, and contributions

## Documentation

- **[API Overview](api-overview.md)** - High-level API map and typical workflows
- **[Architecture](architecture.md)** - Module boundaries and extension rules
- **[Reading Packets](guides/reading-packets.md)** - Reading QWK and QWKE packets
- **[Glossary](glossary.md)** -  A short glossary of terms you might encounter


## Licence

MIT License - see [LICENSE](https://github.com/57951/QwkNet/blob/main/LICENSE) for details.
