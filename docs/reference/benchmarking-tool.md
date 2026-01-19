# Benchmarking Tool

The QWK.NET Benchmarking Tool is a command-line application for measuring QWK.NET library performance, memory usage, and throughput. It helps validate performance targets and identify optimisation opportunities.

## What the Tool Is For

The benchmarking tool measures parsing performance and resource usage:

- **Performance measurement** - Parsing time across different phases (archive open, control parsing, message parsing, index generation)
- **Memory profiling** - Heap allocations, peak memory usage, and per-message memory overhead
- **Throughput analysis** - Messages parsed per second and per-message timing
- **Validation mode comparison** - Performance differences between Strict, Lenient, and Salvage modes

The tool validates that QWK.NET meets its performance targets, particularly that packets with fewer than 100 messages parse in under 100 milliseconds.

## Typical Use Cases

**Performance validation:**
- Verifying that the library meets performance targets
- Identifying parsing bottlenecks and optimisation opportunities
- Comparing performance across different validation modes
- Measuring memory efficiency and allocation patterns

**Optimisation work:**
- Profiling specific parsing phases to find slow areas
- Analysing memory allocation by component
- Testing performance impact of code changes
- Validating that optimisations improve performance

**Performance regression testing:**
- Ensuring new changes don't degrade performance
- Comparing performance across library versions
- Validating performance targets are maintained

**Performance analysis:**
- Understanding where time is spent during packet parsing
- Identifying memory hotspots
- Analysing throughput characteristics

## When to Use the Tool vs the Core Library

**Use the benchmarking tool when:**
- You need to measure parsing performance and memory usage
- You're investigating performance issues or optimisation opportunities
- You want to validate that performance targets are met
- You need to compare performance across validation modes or library versions
- You're doing performance regression testing

**Use the core library when:**
- You're building an application that processes packets
- You need to integrate QWK packet handling into your own code
- You want to create or modify packets programmatically
- Performance measurement isn't your primary concern

The benchmarking tool uses the core library to perform actual packet parsing, then measures and reports on the performance characteristics. It's designed specifically for performance analysis, whilst the core library is designed for packet processing.

## Further Reading

- [Architecture](../architecture.md) - Design decisions that affect performance
