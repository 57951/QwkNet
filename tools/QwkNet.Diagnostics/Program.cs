using System;
using System.Diagnostics;
using System.IO;
using QwkNet.Diagnostics.Commands;

namespace QwkNet.Diagnostics;

/// <summary>
/// QWK.NET Diagnostics Tool - Analyses QWK/REP packets for testing and validation.
/// </summary>
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
        case "analyse":
        case "analyze":
          return AnalyseCommand.Execute(args);

        case "batch":
          return BatchCommand.Execute(args);

        case "view":
          return ViewCommand.Execute(args);

        case "roundtrip":
          return RoundTripCommand.Execute(args);

        case "rendertest":
          return RenderTestCommand.Execute(args);

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
          Console.Error.WriteLine("Run 'QwkNet.Diagnostics help' for usage information.");
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
    Console.WriteLine("QWK.NET Diagnostics Tool");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  QwkNet.Diagnostics analyse <packet.qwk> [options]");
    Console.WriteLine("  QwkNet.Diagnostics batch <directory> [options]");
    Console.WriteLine("  QwkNet.Diagnostics view <packet.qwk> [options]");
    Console.WriteLine("  QwkNet.Diagnostics roundtrip <packet.qwk> [options]");
    Console.WriteLine("  QwkNet.Diagnostics rendertest [options]");
    Console.WriteLine();
    Console.WriteLine("COMMANDS:");
    Console.WriteLine("  analyse    Analyse a single QWK/REP packet");
    Console.WriteLine("  batch      Analyse all QWK/REP packets in a directory");
    Console.WriteLine("  view       View messages from a QWK packet");
    Console.WriteLine("  roundtrip  Test QWK -> REP -> QWK conversion cycle");
    Console.WriteLine("  rendertest Test box-drawing character rendering");
    Console.WriteLine("  help       Show this help message");
    Console.WriteLine("  version    Show version information");
    Console.WriteLine();
    Console.WriteLine("ANALYSE OPTIONS:");
    Console.WriteLine("  --mode <strict|lenient|salvage>  Validation mode (default: lenient)");
    Console.WriteLine("  --output <json|markdown|text>    Output format (default: text)");
    Console.WriteLine("  --verbose                        Show detailed output");
    Console.WriteLine("  --benchmark                      Include performance benchmarks");
    Console.WriteLine("  --memory                         Include memory profiling");
    Console.WriteLine("  --roundtrip                      Perform round-trip validation (readÃ¢â€ â€™writeÃ¢â€ â€™readÃ¢â€ â€™compare)");
    Console.WriteLine("  --inventory                      Show complete archive file inventory");
    Console.WriteLine();
    Console.WriteLine("BATCH OPTIONS:");
    Console.WriteLine("  --mode <strict|lenient|salvage>  Validation mode (default: lenient)");
    Console.WriteLine("  --output <json|markdown>         Output format (default: markdown)");
    Console.WriteLine("  --summary                        Show summary statistics only");
    Console.WriteLine();
    Console.WriteLine("VIEW OPTIONS:");
    Console.WriteLine("  --message <N>                    View specific message number (1-based)");
    Console.WriteLine("  --messages <N1,N2,N3>            View multiple messages (comma-separated)");
    Console.WriteLine("  --range <N1-N2>                  View range of messages (inclusive)");
    Console.WriteLine("  --conference <N>                 View all messages in conference");
    Console.WriteLine("  --all                            View all messages in packet");
    Console.WriteLine("  --output <file>                  Save output to file instead of stdout");
    Console.WriteLine("  --format <text|json|markdown>    Output format (default: text)");
    Console.WriteLine("  --show-raw                       Include raw hex bytes for body");
    Console.WriteLine("  --show-kludges                   Show QWKE kludge lines separately");
    Console.WriteLine("  --show-cp437                     Highlight CP437 special characters");
    Console.WriteLine();
    Console.WriteLine("ROUNDTRIP OPTIONS:");
    Console.WriteLine("  --mode <strict|lenient|salvage>  Validation mode (default: lenient)");
    Console.WriteLine("  --verbose                        Show detailed timing and statistics");
    Console.WriteLine("  --no-diff                        Hide detailed message differences");
    Console.WriteLine();
    Console.WriteLine("RENDERTEST OPTIONS:");
    Console.WriteLine("  --packet <file>                  Test rendering from a QWK packet");
    Console.WriteLine("  --no-reference                   Skip reference card display");
    Console.WriteLine("  --no-diagnostics                 Skip environment diagnostics");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  QwkNet.Diagnostics analyse MYBBS.QWK");
    Console.WriteLine("  QwkNet.Diagnostics analyse MYBBS.QWK --mode strict --output json");
    Console.WriteLine("  QwkNet.Diagnostics analyse MYBBS.QWK --benchmark --memory");
    Console.WriteLine("  QwkNet.Diagnostics analyse MYBBS.QWK --roundtrip --verbose");
    Console.WriteLine("  QwkNet.Diagnostics analyse MYBBS.QWK --inventory --roundtrip");
    Console.WriteLine("  QwkNet.Diagnostics batch ./test-packets --output markdown");
    Console.WriteLine("  QwkNet.Diagnostics view MYBBS.QWK --message 31");
    Console.WriteLine("  QwkNet.Diagnostics view MYBBS.QWK --messages 1,5,10 --show-kludges");
    Console.WriteLine("  QwkNet.Diagnostics view MYBBS.QWK --range 1-10 --format markdown");
    Console.WriteLine("  QwkNet.Diagnostics view MYBBS.QWK --conference 0 --output msgs.txt");
    Console.WriteLine("  QwkNet.Diagnostics roundtrip DEMO1.QWK");
    Console.WriteLine("  QwkNet.Diagnostics roundtrip MYBBS.QWK --verbose");
    Console.WriteLine("  QwkNet.Diagnostics roundtrip MYBBS.QWK --mode strict --no-diff");
    Console.WriteLine("  QwkNet.Diagnostics rendertest");
    Console.WriteLine("  QwkNet.Diagnostics rendertest --packet starol.qwk");
    Console.WriteLine("  QwkNet.Diagnostics rendertest --no-diagnostics");
  }

  private static void ShowVersion()
  {
    Console.WriteLine("QWK.NET Diagnostics Tool v1.0.0");
    Console.WriteLine("QWK.NET Library v1.0.0");
    Console.WriteLine(".NET 10.0");
  }
}