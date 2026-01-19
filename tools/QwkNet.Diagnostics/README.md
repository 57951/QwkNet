# QWK.NET Diagnostics Tool

A command-line tool for inspecting, analysing, and validating QWK and REP offline mail packets. The diagnostics tool provides comprehensive packet analysis, message viewing, and round-trip testing capabilities.

## What the Tool Does

The diagnostics tool performs:

- **Packet inspection** - Examines packet structure, metadata, and contents
- **Analysis** - Validates packet format, reports issues, and provides statistics
- **Message formatting** - Displays messages in human-readable formats with CP437 character highlighting
- **Round-trip testing** - Validates QWK → REP → QWK conversion fidelity

## Building and Running

### Build from Repository Root

```bash
# Build the diagnostics tool
dotnet build tools/QwkNet.Diagnostics

# Build in Release configuration
dotnet build -c Release tools/QwkNet.Diagnostics
```

### Run from Repository Root

```bash
# Run with a command
dotnet run --project tools/QwkNet.Diagnostics -- <command> [options]

# Show help
dotnet run --project tools/QwkNet.Diagnostics -- help
```

## Commands

### `analyse` - Single Packet Analysis

Analyses a single QWK or REP packet and reports structure, metadata, messages, and validation issues.

**Usage:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- analyse <packet.qwk> [options]
```

**Options:**
- `--mode <strict|lenient|salvage>` - Validation mode (default: lenient)
- `--output <text|json|markdown>` - Output format (default: text)
- `--verbose` - Show detailed output
- `--benchmark` - Include performance benchmarks
- `--memory` - Include memory profiling
- `--roundtrip` - Perform round-trip validation (read → write → read → compare)
- `--inventory` - Show complete archive file inventory

**Examples:**
```bash
# Basic analysis
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK

# Verbose analysis with JSON output
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK --verbose --output json

# Strict validation mode
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK --mode strict

# Analysis with round-trip validation
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK --roundtrip --verbose
```

### `batch` - Batch Analysis

Analyses all QWK and REP packets in a directory.

**Usage:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- batch <directory> [options]
```

**Options:**
- `--mode <strict|lenient|salvage>` - Validation mode (default: lenient)
- `--output <json|markdown>` - Output format (default: markdown)
- `--summary` - Show summary statistics only

**Examples:**
```bash
# Analyse all packets in directory
dotnet run --project tools/QwkNet.Diagnostics -- batch ./test-packets

# JSON output for automation
dotnet run --project tools/QwkNet.Diagnostics -- batch ./test-packets --output json

# Summary only
dotnet run --project tools/QwkNet.Diagnostics -- batch ./test-packets --summary
```

### `view` - Message Viewing

Extracts and displays messages from a QWK packet in human-readable formats.

**Usage:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- view <packet.qwk> [options]
```

**Options:**
- `--message <N>` - View specific message number (1-based)
- `--messages <N1,N2,N3>` - View multiple messages (comma-separated)
- `--range <N1-N2>` - View range of messages (inclusive)
- `--conference <N>` - View all messages in conference
- `--all` - View all messages in packet
- `--output <file>` - Save output to file instead of stdout
- `--format <text|json|markdown>` - Output format (default: text)
- `--show-raw` - Include raw hex bytes for body
- `--show-kludges` - Show QWKE kludge lines separately
- `--show-cp437` - Highlight CP437 special characters

**Examples:**
```bash
# View single message
dotnet run --project tools/QwkNet.Diagnostics -- view DEMO1.QWK --message 5

# View multiple messages
dotnet run --project tools/QwkNet.Diagnostics -- view DEMO1.QWK --messages 1,5,10

# View message range
dotnet run --project tools/QwkNet.Diagnostics -- view DEMO1.QWK --range 1-10

# View all messages in conference
dotnet run --project tools/QwkNet.Diagnostics -- view DEMO1.QWK --conference 0

# View all messages with CP437 highlighting
dotnet run --project tools/QwkNet.Diagnostics -- view DEMO1.QWK --all --show-cp437
```

### `roundtrip` - Round-Trip Testing

Tests QWK → REP → QWK conversion cycle to validate packet generation fidelity.

**Usage:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- roundtrip <packet.qwk> [options]
```

**Options:**
- `--mode <strict|lenient|salvage>` - Validation mode (default: lenient)
- `--verbose` - Show detailed timing and statistics
- `--no-diff` - Hide detailed message differences

**Examples:**
```bash
# Basic round-trip test
dotnet run --project tools/QwkNet.Diagnostics -- roundtrip DEMO1.QWK

# Verbose round-trip test
dotnet run --project tools/QwkNet.Diagnostics -- roundtrip DEMO1.QWK --verbose

# Round-trip test without detailed differences
dotnet run --project tools/QwkNet.Diagnostics -- roundtrip DEMO1.QWK --no-diff
```

### Help and Version

```bash
# Show help
dotnet run --project tools/QwkNet.Diagnostics -- help

# Show version
dotnet run --project tools/QwkNet.Diagnostics -- version
```

## Output Formats

### Text Format

Human-readable console output with formatted tables and status indicators. Use for interactive analysis and quick inspection.

**When to use:**
- Interactive packet inspection
- Quick validation checks
- Terminal-based workflows

**Example:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK --output text
```

### JSON Format

Machine-readable structured data suitable for automation and integration with other tools.

**When to use:**
- CI/CD pipelines
- Automated analysis scripts
- Integration with external tools
- Programmatic processing

**Example:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK --output json > results.json
```

### Markdown Format

GitHub-flavoured Markdown with tables and formatted code blocks. Use for documentation and reports.

**When to use:**
- Documentation generation
- Report creation
- Sharing analysis results
- GitHub issues and pull requests

**Example:**
```bash
dotnet run --project tools/QwkNet.Diagnostics -- analyse DEMO1.QWK --output markdown > report.md
```

## Related Documentation

- **[QWK.NET Library](../src/QwkNet/README.md)** - Core library usage and API overview
- **[Validation Modes](../../wiki/VALIDATION_MODES.md)** - Detailed explanation of strict, lenient, and salvage validation modes
- **[Building QWK.NET](../../docs/BUILDING.md)** - Build instructions for the entire project

## Exit Codes

- `0` - Success (all operations completed successfully)
- `1` - Error (parse failure, validation errors in strict mode, or command-line errors)
- `2` - Invalid command-line arguments

Run the tool with `--help` to see available commands and options for each command.
