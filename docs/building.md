---
layout: default  # ← Uses the THEME's default layout
title: QWK.NET - Building QWK.NET
---

# Building QWK.NET

## Prerequisites

- .NET 10 SDK (released 2025)
- VS Code or any text editor
- Command-line access (bash or zsh on macOS 26, PowerShell on Windows 11)

## Building the Library

### From Command Line

```bash
# Navigate to project root
cd QwkNet

# Restore dependencies
dotnet restore

# Build in Debug configuration
dotnet build

# Build in Release configuration
dotnet build -c Release

# Create NuGet package
dotnet pack -c Release
```

### From VS Code

1. Open the `QwkNet` folder in VS Code
2. Install the C# Dev Kit extension if not already installed
3. Press `Ctrl+Shift+B` (or `Cmd+Shift+B` on macOS) to build
4. Select "build" from the task menu

## Running Tests

```bash
# Run all tests
dotnet test
```

## Project Structure

```
QwkNet/
├── src/
│   └── QwkNet/              # Core library
│       ├── Core/            # Binary parsing engine
│       ├── Models/          # Public data models
│       ├── Validation/      # Validation logic
│       ├── Archives/        # Archive format handling
│       ├── Encoding/        # Text encoding utilities
│       └── Extensions/      # QWKE extensions
├── tests/
│   └── QwkNet.Tests/        # Unit and integration tests
├── tools/
│   ├── QwkNet.Benchmarking/ # Benchmarking Tool
│   └── QwkNet.Diagnostics/  # Diagnostics Tool
├── docs/                    # Documentation
├── examples/                # Usage examples
├── QwkNet.sln               # Solution file
├── README.md                # Project overview
└── LICENSE                  # MIT licence

```

## Compilation Targets

The library must compile successfully on:
- Windows 11 (x64)
- macOS 26 (Tahoe) (arm64)

## Troubleshooting

### Build Errors

If you encounter build errors, try:

```bash
# Clean build artifacts
dotnet clean

# Restore dependencies
dotnet restore

# Rebuild
dotnet build
```

### Test Failures

If tests fail unexpectedly:

```bash
# Run tests with maximum verbosity
dotnet test --verbosity diagnostic
```
