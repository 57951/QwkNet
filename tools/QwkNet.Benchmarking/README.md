# QWK.NET Benchmarking Tool

A command-line tool for measuring QWK.NET library performance, memory usage, and throughput.

## Purpose

The benchmarking tool validates that QWK.NET meets its performance targets and helps identify optimisation opportunities. It measures parsing time, memory allocation, and throughput across different validation modes.

**Primary Target:** Packets with <100 messages should parse in <100ms.

## What It Measures

### Timing Breakdown

The tool measures parsing time across four phases:

- **Archive Open:** ZIP decompression and entry enumeration
- **CONTROL.DAT Parse:** BBS metadata and conference list parsing
- **Message Parsing:** Binary record parsing and CP437 decoding
- **Index Generation:** Optional files access (WELCOME, NEWS, etc.)

Each phase is timed separately to identify bottlenecks.

### Memory Profiling

Memory usage is tracked using .NET's GC APIs:

- **Heap Allocated:** Total managed heap allocations during parsing
- **Peak Memory:** Maximum GC heap size reached
- **Thread Allocated:** Allocations on the current thread
- **Per Message:** Average memory allocated per message

The tool also breaks down allocation by component (Archive Reader, CONTROL.DAT, Message Collection, Optional Files) to identify memory hotspots.

### Performance Metrics

- **Per Message Time:** Average parsing time per message
- **Throughput:** Messages parsed per second

### Validation Mode Comparison

By default, the tool tests all three validation modes (Strict, Lenient, Salvage) to compare performance overhead. Validation overhead should be <20% for Strict mode vs. Lenient.

## Running Benchmarks

### Basic Usage

```bash
dotnet run --project tools/QwkNet.Benchmarking -- benchmark DEMO1.QWK
```

Or after building:

```bash
./bin/Debug/net10.0/QwkNet.Benchmarking benchmark DEMO1.QWK
```

### Command-Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--no-warmup` | Skip warmup run | Warmup enabled |
| `--iterations=N` | Number of iterations (1-100) | 5 |
| `--mode=MODE` | Test only specified mode (Strict\|Lenient\|Salvage) | All modes |

### Examples

```bash
# Basic benchmark with default settings
QwkNet.Benchmarking benchmark DEMO1.QWK

# More iterations for better accuracy
QwkNet.Benchmarking benchmark DEMO1.QWK --iterations=10

# Test only Strict mode
QwkNet.Benchmarking benchmark DEMO1.QWK --mode=Strict

# Measure cold-start performance (no warmup)
QwkNet.Benchmarking benchmark DEMO1.QWK --no-warmup
```

## Interpreting Results

### Output Format

The tool outputs results in structured tables:

1. **Packet Information:** Filename, size, iteration count, warmup status
2. **Per-Mode Results:** Results for each validation mode tested
   - Message count
   - Timing breakdown by phase
   - Performance metrics (per-message time, throughput)
   - Memory usage (heap, peak, thread, per-message)
   - Component allocation breakdown
3. **Performance Target Verification:** ✓ or ⚠ indicator showing whether targets are met

### Example Output

```
═══════════════════════════════════════════════════════════════════════════════
QWK.NET Performance Benchmark
═══════════════════════════════════════════════════════════════════════════════

Packet:     DEMO1.QWK
Size:       329,840 bytes
Iterations: 5
Warmup:     Yes

───────────────────────────────────────────────────────────────────────────────
Validation Mode: Lenient
───────────────────────────────────────────────────────────────────────────────

Messages: 31

Timing Breakdown (averaged over 5 iterations):
┌─────────────────────┬────────────┐
│ Phase               │ Time (ms)  │
├─────────────────────┼────────────┤
│ Archive Open        │       0.65 │
│ CONTROL.DAT Parse   │       0.00 │
│ Message Parsing     │       0.01 │
│ Index Generation    │       0.00 │
├─────────────────────┼────────────┤
│ TOTAL               │       0.89 │
│   Min/Max           │ 0.84/0.98  │
└─────────────────────┴────────────┘

Performance Metrics:
┌──────────────────────┬─────────────┐
│ Metric               │ Value       │
├──────────────────────┼─────────────┤
│ Per Message          │    0.000 ms │
│ Throughput           │ 34786 msg/s │
└──────────────────────┴─────────────┘

Memory Usage:
┌──────────────────────┬─────────────┐
│ Metric               │ Value       │
├──────────────────────┼─────────────┤
│ Heap Allocated       │     0.60 MB │
│ Peak Memory          │     1.10 MB │
│ Thread Allocated     │     0.59 MB │
│ Per Message          │     0.98 KB │
└──────────────────────┴─────────────┘

Memory Allocation by Component:
┌──────────────────────┬─────────────┬──────────┐
│ Component            │ Allocated   │ Percent  │
├──────────────────────┼─────────────┼──────────┤
│ Archive Reader       │     0.56 MB │    94.9% │
│ CONTROL.DAT          │     0.00 MB │     0.0% │
│ Message Collection   │     0.03 MB │     5.0% │
│ Optional Files       │     0.00 MB │     0.1% │
└──────────────────────┴─────────────┴──────────┘

✓  Performance target met: 0.89ms < 100ms for 31 messages
```

### Good Performance Indicators

- **Parse time:** <100ms for packets with <100 messages
- **Per-message memory:** <1 KB per message
- **Archive overhead:** >90% of allocation (ZIP decompression dominates)
- **Message parsing:** <10% of total allocation
- **Throughput:** >10,000 messages/second for typical packets

### Performance Issues

If you see:

- **Parse time >100ms** for small packets: Investigate parsing bottlenecks
- **Per-message memory >10 KB:** Potential memory leak or inefficiency
- **Message parsing >50% of allocation:** Parsing overhead too high, optimisation needed
- **Archive overhead <50%:** Unexpected parsing overhead, investigate

### Timing Breakdown Analysis

**Typical Distribution:**
- Archive Open: 70-90% (ZIP decompression dominates)
- CONTROL.DAT Parse: <1%
- Message Parsing: 5-20%
- Index Generation: <5%

**Anomalies to investigate:**
- Message Parsing >50%: CP437 decoding overhead or allocation issues
- Index Generation >10%: I/O bottlenecks or large optional files
- Archive Open <50%: Small packets or very fast storage

## Avoiding Misleading Benchmarks

### Warmup Runs

**Always use warmup** (default) for realistic measurements. Warmup ensures:
- JIT compilation is complete
- CPU caches are populated
- File system caches are warm
- Memory allocators are initialised

**Skip warmup only when:**
- Testing cold-start performance
- Measuring JIT compilation overhead
- Simulating first-run behaviour

Without warmup, results will be inconsistent and artificially slow.

### I/O vs. CPU Bound Operations

The benchmarking tool measures **total elapsed time**, which includes both I/O and CPU work. Be aware:

- **First run:** File system cache is cold, I/O dominates
- **Subsequent runs:** File system cache is warm, CPU work dominates
- **SSD vs. HDD:** Different I/O characteristics affect results
- **Network storage:** Adds latency and variability

For consistent CPU-bound measurements:
- Use warmup runs
- Run multiple iterations and average
- Test on local storage (not network drives)
- Ensure no other processes are accessing the file

### Packet Size Considerations

Benchmark results vary significantly with packet size:

- **Small packets (<100 messages):** I/O overhead dominates, less representative of parsing performance
- **Medium packets (100-1000 messages):** Good balance for measuring parsing throughput
- **Large packets (>1000 messages):** Memory allocation becomes more significant

The performance target (<100ms for <100 messages) applies specifically to small packets. For larger packets, focus on per-message metrics and throughput rather than absolute time.

### Iteration Count

More iterations provide more accurate averages but take longer:

- **5 iterations (default):** Good balance for most cases
- **10-20 iterations:** Better accuracy for performance-critical measurements
- **100 iterations:** Overkill for most purposes, use only for detailed analysis

The tool reports min/max values to show variance. High variance suggests:
- System load affecting results
- Thermal throttling (laptop CPUs)
- Background processes interfering
- File system cache inconsistencies

### Memory Measurement Variability

Memory profiling triggers GC collections and may show variable results:

- GC timing is non-deterministic
- Background GC collections affect measurements
- Multiple runs may show different values

For accurate memory measurements:
- Run multiple iterations and average
- Run multiple times and compare averages
- Focus on relative differences (e.g., Strict vs. Lenient) rather than absolute values

## Performance Targets

The tool automatically verifies these targets:

### Primary Target
- **Small Packets (<100 messages):** Should parse in <100ms

### Secondary Targets
- **Per-message overhead:** <1 KB memory per message
- **Throughput:** >10,000 messages/second for typical packets
- **Linear scaling:** Parse time scales linearly with message count
- **Memory efficiency:** Heap allocation <1 MB for typical packets

### Validation Overhead
- **Strict mode:** <20% overhead vs. Lenient
- **Salvage mode:** <10% overhead vs. Lenient
- **Index generation:** <5% of total parse time

## Building

From the `tools/QwkNet.Benchmarking` directory:

```bash
dotnet build
```

## Related Tools and Documentation

- **[Diagnostics Tool](../QwkNet.Diagnostics/README.md)** - Comprehensive packet analysis, validation, and inspection
- **[Core Library Documentation](../../src/QwkNet/README.md)** - Library usage, key concepts, and API overview
- **[Building Documentation](../../docs/BUILDING.md)** - Build instructions and project structure
- **[Architecture Documentation](../../docs/ARCHITECTURE.md)** - Module boundaries and design decisions

## Troubleshooting

### "File not found" Error

Ensure the packet file path is correct and the file exists:

```bash
ls -lh DEMO1.QWK
QwkNet.Benchmarking benchmark DEMO1.QWK
```

### Inconsistent Results

If results vary significantly between runs:

- **Increase iterations:** `--iterations=20`
- **Check system load:** Close other applications
- **Check thermal throttling:** Laptop CPUs may throttle under load
- **Use local storage:** Avoid network drives
- **Run multiple times:** Average results manually

### Warmup Not Effective

If warmup doesn't stabilise results:

- **Use larger packets:** More stable measurements
- **Check for background processes:** Other apps may affect timing
- **Verify file system cache:** First run after reboot will be slower

## Contributing

Contributions welcome! Please maintain:
- British English in documentation
- 2-space indentation
- No `var` keyword
- Comprehensive XML documentation
- No external dependencies

## Licence

MIT License - see main project LICENSE file.
