# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-01-19

### Added
- Initial release
- QwkNet library for reading and validating QWK offline mail packets
- High performance packet parsing (<100ms typical)
- Zero external dependencies in core package
- .NET 10 compatibility (Windows, macOS)
- Strongly typed access to QWK control data and messages
- Access to extended message headers (where present)
- Comprehensive message and control block parsing support
- Integrated packet validation and error reporting
- Custom abstractions for message body decoding
- Example tools and code samples included

### Extensions
- Modular architecture supports third-party archive formats and validators
- Example: `QwkNet.Archives.Tar` project demonstrating extension model

### Validation
- Validated with modern QWK packets and historical packets (1991–2023)
- Round-trip (read/write) tested for all public APIs
- Over 910 unit tests included

### Known Limitations
- Some rare control/metadata blocks may be ignored with warning
- No built-in packet repair tools (not planned)
