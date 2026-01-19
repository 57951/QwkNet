# QWK.NET Example Files

## Summary

This directory contains example files that demonstrate how to use key parts of the QWK.NET API.

Each example provides a focused scenario, such as reading a QWK packet, constructing reply packets, or validating packets. Examples are self-contained and correspond to common usage patterns described in the [API Overview](../docs/API_OVERVIEW.md).

### Main Example Files

- **QwkPacketExamples.cs**  
  Demonstrates how to open and inspect QWK packet files, enumerate messages, and access control data.

- **RepPacketExamples.cs**  
  Shows how to construct REP (reply) packets, build new messages, and export them to file or stream.

- **ValidationExamples.cs**  
  Provides examples of performing packet validation, with code that checks for errors and warnings.

- **RarArchiveExtensionExample.cs**  
  Illustrates how to implement an extension for an archive format.

Each file includes comments explaining how to use the relevant QWK.NET APIs. If you're new to QWK.NET, begin with `QwkPacketExamples.cs` for a basic overview of packet reading, then explore the others for writing and advanced flows.

For detailed API coverage, see [API Overview](../docs/API_OVERVIEW.md) and in-depth guidance linked therein.
