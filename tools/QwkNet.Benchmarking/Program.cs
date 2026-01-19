using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QwkNet;

namespace QwkNet.Benchmarking;

/// <summary>
/// QWK.NET Performance Benchmarking Tool.
/// </summary>
/// <remarks>
/// Measures parsing performance, memory usage, and throughput for QWK packets
/// across different packet sizes and validation modes. Target: packets with
/// &lt;100 messages should parse in &lt;100ms.
/// </remarks>
internal static class Program
{
  private static int Main(string[] args)
  {
    try
    {
      if (args.Length == 0)
      {
        ShowUsage();
        return 0;
      }

      string command = args[0].ToLowerInvariant();

      switch (command)
      {
        case "benchmark":
          return BenchmarkCommand.Execute(args);

        case "help":
        case "--help":
        case "-h":
        case "?":
          ShowUsage();
          return 0;

        case "version":
        case "--version":
        case "-v":
          ShowVersion();
          return 0;

        default:
          Console.Error.WriteLine($"Unknown command: {command}");
          Console.Error.WriteLine("Run 'QwkNet.Benchmarking help' for usage information.");
          return 1;
      }
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Fatal error: {ex.Message}");
      if (Debugger.IsAttached)
      {
        Console.Error.WriteLine(ex.StackTrace);
      }
      return 1;
    }
  }

  private static void ShowUsage()
  {
    Console.WriteLine("QWK.NET Performance Benchmarking Tool");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  QwkNet.Benchmarking benchmark <packet.qwk> [options]");
    Console.WriteLine();
    Console.WriteLine("COMMANDS:");
    Console.WriteLine("  benchmark  Run performance benchmarks on a QWK packet");
    Console.WriteLine("  help       Show this help message");
    Console.WriteLine("  version    Show version information");
    Console.WriteLine();
    Console.WriteLine("BENCHMARK OPTIONS:");
    Console.WriteLine("  --no-warmup                      Skip warmup run");
    Console.WriteLine("  --iterations=N                   Number of iterations (default: 5)");
    Console.WriteLine("  --mode=MODE                      Test only specified mode (Strict|Lenient|Salvage)");
    Console.WriteLine("                                   Default: tests all modes");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  QwkNet.Benchmarking benchmark MYBBS.QWK");
    Console.WriteLine("  QwkNet.Benchmarking benchmark MYBBS.QWK --iterations=10");
    Console.WriteLine("  QwkNet.Benchmarking benchmark MYBBS.QWK --mode=Strict --no-warmup");
    Console.WriteLine();
    Console.WriteLine("PERFORMANCE TARGET:");
    Console.WriteLine("  Packets with <100 messages should parse in <100ms");
  }

  private static void ShowVersion()
  {
    Console.WriteLine("QWK.NET Benchmarking Tool v1.0.0");
    Console.WriteLine("QWK.NET Library v1.0.0");
    Console.WriteLine(".NET 10.0");
  }
}