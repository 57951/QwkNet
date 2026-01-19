using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QwkNet;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Benchmarking;

/// <summary>
/// Implements performance benchmarking for QWK packet parsing operations.
/// </summary>
/// <remarks>
/// Measures parsing time, memory usage, and throughput across different
/// packet sizes and validation modes to ensure performance targets are met.
/// Target: Packets with &lt;100 messages should parse in &lt;100ms.
/// </remarks>
internal static class BenchmarkCommand
{
  /// <summary>
  /// Executes the benchmark command with the specified arguments.
  /// </summary>
  /// <param name="args">Command-line arguments (benchmark [options] &lt;packet-path&gt;).</param>
  /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
  public static int Execute(string[] args)
  {
    if (args.Length < 2)
    {
      Console.Error.WriteLine("Error: Missing required packet path argument.");
      Console.Error.WriteLine();
      ShowUsage();
      return 1;
    }

    string packetPath = args[1];

    if (!File.Exists(packetPath))
    {
      Console.Error.WriteLine($"Error: Packet file not found: {packetPath}");
      return 1;
    }

    // Parse optional arguments
    bool warmup = true;
    int iterations = 5;
    List<ValidationMode> modes = new List<ValidationMode> 
    { 
      ValidationMode.Strict, 
      ValidationMode.Lenient, 
      ValidationMode.Salvage 
    };

    for (int i = 2; i < args.Length; i++)
    {
      string arg = args[i].ToLowerInvariant();

      if (arg == "--no-warmup")
      {
        warmup = false;
      }
      else if (arg.StartsWith("--iterations="))
      {
        string value = arg.Substring("--iterations=".Length);
        if (int.TryParse(value, out int parsed) && parsed > 0)
        {
          iterations = parsed;
        }
        else
        {
          Console.Error.WriteLine($"Error: Invalid iterations value: {value}");
          return 1;
        }
      }
      else if (arg.StartsWith("--mode="))
      {
        string value = arg.Substring("--mode=".Length);
        if (Enum.TryParse<ValidationMode>(value, true, out ValidationMode mode))
        {
          modes.Clear();
          modes.Add(mode);
        }
        else
        {
          Console.Error.WriteLine($"Error: Invalid validation mode: {value}");
          return 1;
        }
      }
      else
      {
        Console.Error.WriteLine($"Error: Unknown option: {args[i]}");
        ShowUsage();
        return 1;
      }
    }

    try
    {
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
      Console.WriteLine("QWK.NET Performance Benchmark");
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
      Console.WriteLine();
      Console.WriteLine($"Packet:     {Path.GetFileName(packetPath)}");
      Console.WriteLine($"Size:       {new FileInfo(packetPath).Length:N0} bytes");
      Console.WriteLine($"Iterations: {iterations}");
      Console.WriteLine($"Warmup:     {(warmup ? "Yes" : "No")}");
      Console.WriteLine();

      // Warmup run if requested
      if (warmup)
      {
        Console.WriteLine("Performing warmup run...");
        WarmupRun(packetPath);
        Console.WriteLine("Warmup complete.");
        Console.WriteLine();
      }

      // Run benchmarks for each validation mode
      foreach (ValidationMode mode in modes)
      {
        RunBenchmark(packetPath, mode, iterations);
        Console.WriteLine();
      }

      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error: {ex.Message}");
      if (ex.InnerException != null)
      {
        Console.Error.WriteLine($"  Inner: {ex.InnerException.Message}");
      }
      return 1;
    }
  }

  /// <summary>
  /// Performs a warmup run to initialise JIT compilation and caches.
  /// </summary>
  /// <param name="packetPath">Path to the QWK packet file.</param>
  private static void WarmupRun(string packetPath)
  {
    try
    {
      using (QwkPacket packet = QwkPacket.Open(packetPath, ValidationMode.Lenient))
      {
        int messageCount = packet.Messages.Count;
        // Force enumeration to trigger all parsing code paths
        foreach (Message msg in packet.Messages)
        {
          string subject = msg.Subject;
        }
      }
    }
    catch
    {
      // Ignore warmup errors
    }
  }

  /// <summary>
  /// Runs the benchmark for a specific validation mode.
  /// </summary>
  /// <param name="packetPath">Path to the QWK packet file.</param>
  /// <param name="mode">Validation mode to use.</param>
  /// <param name="iterations">Number of iterations to run.</param>
  private static void RunBenchmark(string packetPath, ValidationMode mode, int iterations)
  {
    Console.WriteLine("───────────────────────────────────────────────────────────────────────────────");
    Console.WriteLine($"Validation Mode: {mode}");
    Console.WriteLine("───────────────────────────────────────────────────────────────────────────────");
    Console.WriteLine();

    List<BenchmarkResult> results = new List<BenchmarkResult>();

    for (int i = 0; i < iterations; i++)
    {
      BenchmarkResult result = RunSingleIteration(packetPath, mode);
      results.Add(result);
    }

    // Calculate statistics
    double avgTotalTime = results.Average(r => r.TotalTimeMs);
    double minTotalTime = results.Min(r => r.TotalTimeMs);
    double maxTotalTime = results.Max(r => r.TotalTimeMs);
    double avgArchiveTime = results.Average(r => r.ArchiveOpenTimeMs);
    double avgControlDatTime = results.Average(r => r.ControlDatParseTimeMs);
    double avgMessagesTime = results.Average(r => r.MessagesParseTimeMs);
    double avgIndexTime = results.Average(r => r.IndexGenerateTimeMs);
    double avgPerMessageTime = results.Average(r => r.PerMessageTimeMs);
    double avgThroughput = results.Average(r => r.MessagesPerSecond);
    
    // Memory statistics
    long avgMemoryAllocated = (long)results.Average(r => r.MemoryAllocatedBytes);
    long avgPeakMemory = (long)results.Average(r => r.PeakMemoryBytes);
    long avgThreadAllocated = (long)results.Average(r => r.TotalThreadAllocatedBytes);
    long avgArchiveAlloc = (long)results.Average(r => r.ArchiveAllocatedBytes);
    long avgControlDatAlloc = (long)results.Average(r => r.ControlDatAllocatedBytes);
    long avgMessageCollectionAlloc = (long)results.Average(r => r.MessageCollectionAllocatedBytes);
    long avgOptionalFilesAlloc = (long)results.Average(r => r.OptionalFilesAllocatedBytes);
    long avgBytesPerMessage = (long)results.Average(r => r.BytesPerMessage);
    
    int messageCount = results[0].MessageCount;

    // Display results in table format
    Console.WriteLine($"Messages: {messageCount:N0}");
    Console.WriteLine();
    Console.WriteLine("Timing Breakdown (averaged over {0} iteration{1}):", 
      iterations, iterations == 1 ? "" : "s");
    Console.WriteLine("┌─────────────────────┬────────────┐");
    Console.WriteLine("│ Phase               │ Time (ms)  │");
    Console.WriteLine("├─────────────────────┼────────────┤");
    Console.WriteLine($"│ Archive Open        │ {avgArchiveTime,10:F2} │");
    Console.WriteLine($"│ CONTROL.DAT Parse   │ {avgControlDatTime,10:F2} │");
    Console.WriteLine($"│ Message Parsing     │ {avgMessagesTime,10:F2} │");
    Console.WriteLine($"│ Index Generation    │ {avgIndexTime,10:F2} │");
    Console.WriteLine("├─────────────────────┼────────────┤");
    Console.WriteLine($"│ TOTAL               │ {avgTotalTime,10:F2} │");
    Console.WriteLine($"│   Min/Max           │ {minTotalTime,4:F2}/{maxTotalTime,4:F2} │");
    Console.WriteLine("└─────────────────────┴────────────┘");
    Console.WriteLine();

    Console.WriteLine("Performance Metrics:");
    Console.WriteLine("┌──────────────────────┬─────────────┐");
    Console.WriteLine("│ Metric               │ Value       │");
    Console.WriteLine("├──────────────────────┼─────────────┤");
    Console.WriteLine($"│ Per Message          │ {avgPerMessageTime,8:F3} ms │");
    Console.WriteLine($"│ Throughput           │ {avgThroughput,8:F0} msg/s │");
    Console.WriteLine("└──────────────────────┴─────────────┘");
    Console.WriteLine();

    Console.WriteLine("Memory Usage:");
    Console.WriteLine("┌──────────────────────┬─────────────┐");
    Console.WriteLine("│ Metric               │ Value       │");
    Console.WriteLine("├──────────────────────┼─────────────┤");
    Console.WriteLine($"│ Heap Allocated       │ {avgMemoryAllocated / 1024.0 / 1024.0,8:F2} MB │");
    Console.WriteLine($"│ Peak Memory          │ {avgPeakMemory / 1024.0 / 1024.0,8:F2} MB │");
    Console.WriteLine($"│ Thread Allocated     │ {avgThreadAllocated / 1024.0 / 1024.0,8:F2} MB │");
    Console.WriteLine($"│ Per Message          │ {avgBytesPerMessage / 1024.0,8:F2} KB │");
    Console.WriteLine("└──────────────────────┴─────────────┘");
    Console.WriteLine();

    Console.WriteLine("Memory Allocation by Component:");
    Console.WriteLine("┌──────────────────────┬─────────────┬──────────┐");
    Console.WriteLine("│ Component            │ Allocated   │ Percent  │");
    Console.WriteLine("├──────────────────────┼─────────────┼──────────┤");
    
    double archivePct = avgThreadAllocated > 0 ? (avgArchiveAlloc * 100.0 / avgThreadAllocated) : 0;
    double controlPct = avgThreadAllocated > 0 ? (avgControlDatAlloc * 100.0 / avgThreadAllocated) : 0;
    double messagePct = avgThreadAllocated > 0 ? (avgMessageCollectionAlloc * 100.0 / avgThreadAllocated) : 0;
    double optionalPct = avgThreadAllocated > 0 ? (avgOptionalFilesAlloc * 100.0 / avgThreadAllocated) : 0;
    
    Console.WriteLine($"│ Archive Reader       │ {avgArchiveAlloc / 1024.0 / 1024.0,8:F2} MB │ {archivePct,6:F1}% │");
    Console.WriteLine($"│ CONTROL.DAT          │ {avgControlDatAlloc / 1024.0 / 1024.0,8:F2} MB │ {controlPct,6:F1}% │");
    Console.WriteLine($"│ Message Collection   │ {avgMessageCollectionAlloc / 1024.0 / 1024.0,8:F2} MB │ {messagePct,6:F1}% │");
    Console.WriteLine($"│ Optional Files       │ {avgOptionalFilesAlloc / 1024.0 / 1024.0,8:F2} MB │ {optionalPct,6:F1}% │");
    Console.WriteLine("└──────────────────────┴─────────────┴──────────┘");
    Console.WriteLine();

    // Check performance targets
    if (messageCount < 100)
    {
      if (avgTotalTime >= 100.0)
      {
        Console.WriteLine("⚠  WARNING: Performance target not met!");
        Console.WriteLine($"   Expected: <100ms for packets with <100 messages");
        Console.WriteLine($"   Actual:   {avgTotalTime:F2}ms for {messageCount} messages");
      }
      else
      {
        Console.WriteLine($"✓  Performance target met: {avgTotalTime:F2}ms < 100ms for {messageCount} messages");
      }
    }
    else
    {
      Console.WriteLine($"   Packet has {messageCount} messages (performance target applies to <100 messages)");
      Console.WriteLine($"   Actual parse time: {avgTotalTime:F2}ms ({avgPerMessageTime:F3}ms per message)");
    }
  }

  /// <summary>
  /// Runs a single benchmark iteration and returns the results.
  /// </summary>
  /// <param name="packetPath">Path to the QWK packet file.</param>
  /// <param name="mode">Validation mode to use.</param>
  /// <returns>Benchmark results for this iteration.</returns>
  private static BenchmarkResult RunSingleIteration(string packetPath, ValidationMode mode)
  {
    BenchmarkResult result = new BenchmarkResult();
    Stopwatch totalTimer = Stopwatch.StartNew();
    Stopwatch phaseTimer = new Stopwatch();

    // Measure baseline memory - force full GC to get accurate baseline
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    long memoryBefore = GC.GetTotalMemory(false);
    long threadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();

    try
    {
      // Phase 1: Archive Opening
      phaseTimer.Restart();
      long archiveAllocBefore = GC.GetAllocatedBytesForCurrentThread();
      QwkPacket? packet = QwkPacket.Open(packetPath, mode);
      long archiveAllocAfter = GC.GetAllocatedBytesForCurrentThread();
      phaseTimer.Stop();
      result.ArchiveOpenTimeMs = phaseTimer.Elapsed.TotalMilliseconds;
      result.ArchiveAllocatedBytes = archiveAllocAfter - archiveAllocBefore;

      using (packet)
      {
        // Phase 2: CONTROL.DAT Parsing
        phaseTimer.Restart();
        long controlAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        ControlDat control = packet.Control;
        string bbsName = control.BbsName;
        long controlAllocAfter = GC.GetAllocatedBytesForCurrentThread();
        phaseTimer.Stop();
        result.ControlDatParseTimeMs = phaseTimer.Elapsed.TotalMilliseconds;
        result.ControlDatAllocatedBytes = controlAllocAfter - controlAllocBefore;

        // Phase 3: Message Collection & Parsing
        phaseTimer.Restart();
        long messageAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        int messageCount = 0;
        long peakMemoryDuringParsing = GC.GetTotalMemory(false);

        foreach (Message msg in packet.Messages)
        {
          // Access key properties to ensure full parsing occurs
          string subject = msg.Subject;
          string from = msg.From;
          string to = msg.To;
          string body = msg.Body.GetDecodedText();
          messageCount++;

          // Sample peak memory every 10 messages to reduce overhead
          if (messageCount % 10 == 0)
          {
            long currentMemory = GC.GetTotalMemory(false);
            if (currentMemory > peakMemoryDuringParsing)
            {
              peakMemoryDuringParsing = currentMemory;
            }
          }
        }

        long messageAllocAfter = GC.GetAllocatedBytesForCurrentThread();
        phaseTimer.Stop();
        result.MessagesParseTimeMs = phaseTimer.Elapsed.TotalMilliseconds;
        result.MessageCount = messageCount;
        result.MessageCollectionAllocatedBytes = messageAllocAfter - messageAllocBefore;

        // Capture final peak memory
        long finalMemory = GC.GetTotalMemory(false);
        result.PeakMemoryBytes = Math.Max(peakMemoryDuringParsing, finalMemory);

        // Phase 4: Optional Files Access
        phaseTimer.Restart();
        long optionalFilesAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        // Access the standard optional files to measure allocation overhead
        string? welcome = packet.OptionalFiles.GetWelcomeText();
        string? news = packet.OptionalFiles.GetNewsText();
        string? goodbye = packet.OptionalFiles.GetGoodbyeText();
        long optionalFilesAllocAfter = GC.GetAllocatedBytesForCurrentThread();
        phaseTimer.Stop();
        result.IndexGenerateTimeMs = phaseTimer.Elapsed.TotalMilliseconds;
        result.OptionalFilesAllocatedBytes = optionalFilesAllocAfter - optionalFilesAllocBefore;
      }

      totalTimer.Stop();
      result.TotalTimeMs = totalTimer.Elapsed.TotalMilliseconds;

      // Measure total thread allocations
      long threadAllocatedAfter = GC.GetAllocatedBytesForCurrentThread();
      result.TotalThreadAllocatedBytes = threadAllocatedAfter - threadAllocatedBefore;

      // Measure heap memory allocated (difference in total memory)
      long memoryAfter = GC.GetTotalMemory(false);
      result.MemoryAllocatedBytes = Math.Max(0, memoryAfter - memoryBefore);

      // Calculate derived metrics
      if (result.MessageCount > 0)
      {
        result.PerMessageTimeMs = result.MessagesParseTimeMs / result.MessageCount;
        result.MessagesPerSecond = (result.MessageCount * 1000.0) / result.TotalTimeMs;
        result.BytesPerMessage = result.MessageCollectionAllocatedBytes / result.MessageCount;
      }

      return result;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error during benchmark iteration: {ex.Message}");
      throw;
    }
  }

  /// <summary>
  /// Displays usage information for the benchmark command.
  /// </summary>
  private static void ShowUsage()
  {
    Console.WriteLine("Usage: benchmark <packet-path> [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --no-warmup           Skip warmup run");
    Console.WriteLine("  --iterations=N        Number of iterations (default: 5)");
    Console.WriteLine("  --mode=MODE           Test only specified mode (Strict|Lenient|Salvage)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  benchmark packet.qwk");
    Console.WriteLine("  benchmark packet.qwk --iterations=10");
    Console.WriteLine("  benchmark packet.qwk --mode=Strict --no-warmup");
  }

  /// <summary>
  /// Contains the results of a single benchmark iteration.
  /// </summary>
  private class BenchmarkResult
  {
    public double TotalTimeMs { get; set; }
    public double ArchiveOpenTimeMs { get; set; }
    public double ControlDatParseTimeMs { get; set; }
    public double MessagesParseTimeMs { get; set; }
    public double IndexGenerateTimeMs { get; set; }
    public int MessageCount { get; set; }
    public double PerMessageTimeMs { get; set; }
    public double MessagesPerSecond { get; set; }
    
    // Memory profiling properties
    public long MemoryAllocatedBytes { get; set; }
    public long PeakMemoryBytes { get; set; }
    public long TotalThreadAllocatedBytes { get; set; }
    public long ArchiveAllocatedBytes { get; set; }
    public long ControlDatAllocatedBytes { get; set; }
    public long MessageCollectionAllocatedBytes { get; set; }
    public long OptionalFilesAllocatedBytes { get; set; }
    public long BytesPerMessage { get; set; }
  }
}