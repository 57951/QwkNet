using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QwkNet.Diagnostics.Analysis;
using QwkNet.Diagnostics.Output;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Commands;

/// <summary>
/// Analyses all QWK/REP packets in a directory.
/// </summary>
internal static class BatchCommand
{
  public static int Execute(string[] args)
  {
    if (args.Length < 2)
    {
      Console.Error.WriteLine("Error: Missing directory path.");
      Console.Error.WriteLine("Usage: QwkNet.Diagnostics batch <directory> [options]");
      return 1;
    }

    string directoryPath = args[1];

    if (!Directory.Exists(directoryPath))
    {
      Console.Error.WriteLine($"Error: Directory not found: {directoryPath}");
      return 1;
    }

    // Parse options
    ValidationMode mode = ValidationMode.Lenient;
    OutputFormat format = OutputFormat.Markdown;
    bool summaryOnly = false;

    for (int i = 2; i < args.Length; i++)
    {
      string arg = args[i].ToLowerInvariant();

      switch (arg)
      {
        case "--mode":
          if (i + 1 >= args.Length)
          {
            Console.Error.WriteLine("Error: --mode requires a value (strict|lenient|salvage)");
            return 1;
          }
          mode = ParseValidationMode(args[++i]);
          break;

        case "--output":
          if (i + 1 >= args.Length)
          {
            Console.Error.WriteLine("Error: --output requires a value (json|markdown)");
            return 1;
          }
          format = ParseOutputFormat(args[++i]);
          break;

        case "--summary":
          summaryOnly = true;
          break;

        default:
          Console.Error.WriteLine($"Error: Unknown option: {args[i]}");
          return 1;
      }
    }

    // Find all QWK/REP files
    string[] qwkFiles = Directory.GetFiles(directoryPath, "*.qwk", SearchOption.TopDirectoryOnly);
    string[] repFiles = Directory.GetFiles(directoryPath, "*.rep", SearchOption.TopDirectoryOnly);
    string[] allFiles = qwkFiles.Concat(repFiles).OrderBy(f => f).ToArray();

    if (allFiles.Length == 0)
    {
      Console.Error.WriteLine($"Error: No QWK or REP files found in {directoryPath}");
      return 1;
    }

    Console.WriteLine($"Found {allFiles.Length} packet(s) to analyse...");
    Console.WriteLine();

    // Analyse all packets
    PacketAnalyser analyser = new PacketAnalyser();
    List<AnalysisResult> results = new List<AnalysisResult>();

    foreach (string file in allFiles)
    {
      Console.Write($"Analysing {Path.GetFileName(file)}... ");
      AnalysisResult result = analyser.Analyse(file, mode, includeBenchmark: false, includeMemory: false, performRoundtrip: false, includeInventory: false);
      results.Add(result);
      Console.WriteLine(result.ParseSuccess ? "OK" : "FAILED");
    }

    Console.WriteLine();

    // Output results
    IOutputFormatter formatter = format switch
    {
      OutputFormat.Json => new JsonBatchOutputFormatter(),
      OutputFormat.Markdown => new MarkdownBatchOutputFormatter(summaryOnly),
      _ => new MarkdownBatchOutputFormatter(summaryOnly)
    };

    string output = formatter.FormatBatch(results);
    Console.WriteLine(output);

    int failedCount = results.Count(r => !r.ParseSuccess);
    return failedCount > 0 ? 1 : 0;
  }

  private static ValidationMode ParseValidationMode(string value)
  {
    return value.ToLowerInvariant() switch
    {
      "strict" => ValidationMode.Strict,
      "lenient" => ValidationMode.Lenient,
      "salvage" => ValidationMode.Salvage,
      _ => throw new ArgumentException($"Invalid validation mode: {value}. Use strict, lenient, or salvage.")
    };
  }

  private static OutputFormat ParseOutputFormat(string value)
  {
    return value.ToLowerInvariant() switch
    {
      "json" => OutputFormat.Json,
      "markdown" => OutputFormat.Markdown,
      "md" => OutputFormat.Markdown,
      _ => throw new ArgumentException($"Invalid output format: {value}. Use json or markdown.")
    };
  }
}