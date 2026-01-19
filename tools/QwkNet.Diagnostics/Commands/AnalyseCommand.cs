using System;
using System.IO;
using QwkNet.Diagnostics.Analysis;
using QwkNet.Diagnostics.Output;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Commands;

/// <summary>
/// Analyses a single QWK/REP packet and outputs diagnostics.
/// </summary>
internal static class AnalyseCommand
{
  public static int Execute(string[] args)
  {
    if (args.Length < 2)
    {
      Console.Error.WriteLine("Error: Missing packet file path.");
      Console.Error.WriteLine("Usage: QwkNet.Diagnostics analyse <packet.qwk> [options]");
      return 1;
    }

    string filePath = args[1];

    if (!File.Exists(filePath))
    {
      Console.Error.WriteLine($"Error: File not found: {filePath}");
      return 1;
    }

    // Parse options
    ValidationMode mode = ValidationMode.Lenient;
    OutputFormat format = OutputFormat.Text;
    bool verbose = false;
    bool benchmark = false;
    bool memory = false;
    bool roundtrip = false;
    bool inventory = false;

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
            Console.Error.WriteLine("Error: --output requires a value (json|markdown|text)");
            return 1;
          }
          format = ParseOutputFormat(args[++i]);
          break;

        case "--verbose":
          verbose = true;
          break;

        case "--benchmark":
          benchmark = true;
          break;

        case "--memory":
          memory = true;
          break;

        case "--roundtrip":
          roundtrip = true;
          break;

        case "--inventory":
          inventory = true;
          break;

        default:
          Console.Error.WriteLine($"Error: Unknown option: {args[i]}");
          return 1;
      }
    }

    // Analyse packet
    PacketAnalyser analyser = new PacketAnalyser();
    AnalysisResult result = analyser.Analyse(filePath, mode, benchmark, memory, roundtrip, inventory);

    // Output results
    IOutputFormatter formatter = format switch
    {
      OutputFormat.Json => new JsonOutputFormatter(),
      OutputFormat.Markdown => new MarkdownOutputFormatter(),
      OutputFormat.Text => new TextOutputFormatter(verbose),
      _ => new TextOutputFormatter(verbose)
    };

    string output = formatter.Format(result);
    Console.WriteLine(output);

    return result.ParseSuccess ? 0 : 1;
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
      "text" => OutputFormat.Text,
      "txt" => OutputFormat.Text,
      _ => throw new ArgumentException($"Invalid output format: {value}. Use json, markdown, or text.")
    };
  }
}

internal enum OutputFormat
{
  Text,
  Json,
  Markdown
}