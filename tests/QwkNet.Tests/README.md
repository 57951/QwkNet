# QwkNet.Tests

Unit and integration tests for the QWK.NET library.

## Running Tests

### From Repository Root

Run all tests (excluding optional tests):

```bash
dotnet test
```

Run tests with detailed output:

```bash
dotnet test --verbosity normal
```

Run a specific test class:

```bash
dotnet test --filter QwkPacketTests
```

Run optional tests only:

```bash
dotnet test --filter "Category=Optional" /p:RunSettingsFilePath=""
```

Run all tests including optional:

```bash
dotnet test /p:RunSettingsFilePath=""
```

## Test Structure

Tests are organised by feature area:

- **Archive/** - Archive format handling (ZIP, detection, factory)
- **Core/** - Core parsing engine and binary record handling
- **Diagnostics/** - Diagnostics tool functionality
- **Encoding/** - Text encoding, CP437, ANSI handling, line endings
- **Models/** - Data model tests (messages, control, indexing, QWKE)
- **Parsing/** - Parser tests (CONTROL.DAT, index files, QWKE extensions)
- **Rendering/** - Box drawing and text rendering
- **Validation/** - Packet validation and reporting

## Naming Conventions

Test methods follow the pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `Constructor_WithValidParameters_CreatesInstance`
- `Open_WithNullPath_ThrowsArgumentNullException`
- `ParseMessages_StarolQwk_ReturnsCorrectMessageCount`

Test classes are named after the class or feature being tested, suffixed with `Tests`:
- `QwkPacketTests.cs`
- `MessageTests.cs`
- `ControlDatParserTests.cs`

## Optional vs Required Tests

### Required Tests

Most tests are required and run by default. These validate core functionality and must pass for the library to be considered correct.

### Optional Tests

Optional tests are marked with `[Trait("Category", "Optional")]` and are excluded from default test runs via `test.runsettings`. These tests typically:

- Require external test data files (e.g., `/tmp/DEMO1.QWK`) that may not be available in all environments
- Validate behaviour with real-world packets that cannot be committed to the repository
- Test edge cases that depend on specific packet characteristics

Optional tests are skipped in CI environments but can be run locally when test data is available. They provide additional validation for contributors working with real QWK packets.

## Test Data

Some tests reference external QWK packet files:

- **TestData/** - Some tests look for files like `TestData/starol.qwk` (may be gitignored)
- **External paths** - Optional tests may reference files like `/tmp/DEMO1.QWK` or `/mnt/user-data/uploads/DEMO1.QWK`

Tests that require external files gracefully skip when files are not found, ensuring the test suite remains runnable without test data.

## Round-Trip Testing

Round-trip tests validate QWK → REP → QWK conversion fidelity:

- `RoundTripPacketTests.cs` - Tests with real QWK packets
- `RoundTripTests.cs` - Synthetic message tests (currently skipped due to encoding complexity)

These tests ensure that packets can be read, converted to REP format, and read back without data loss.

## Expectations for Contributors

When adding new tests:

1. **Follow naming conventions** - Use `MethodName_Scenario_ExpectedResult` pattern
2. **Organise by feature** - Place tests in the appropriate subdirectory matching the code being tested
3. **Use xUnit attributes** - `[Fact]` for single-case tests, `[Theory]` with `[InlineData]` for parameterised tests
4. **Mark optional tests** - Use `[Trait("Category", "Optional")]` if the test requires external data or specific environment setup
5. **Handle missing data gracefully** - Tests that require external files should check for file existence and skip if not found
6. **Write descriptive assertions** - Include clear failure messages in `Assert` calls
7. **Test both success and failure paths** - Include tests for error conditions and edge cases

Test coverage should remain high, with tests validating both happy paths and error conditions for all public APIs.
