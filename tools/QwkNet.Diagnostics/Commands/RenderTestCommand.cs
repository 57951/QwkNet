using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QwkNet;
using QwkNet.Encoding;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;

namespace QwkNet.Diagnostics.Commands;

/// <summary>
/// Tests box-drawing and ANSI art rendering in the console.
/// </summary>
/// <remarks>
/// <para>
/// This command verifies that CP437 box-drawing characters and extended ASCII
/// display correctly in the current terminal environment. It tests both the
/// library's CP437 encoding and the terminal's ability to render these characters.
/// </para>
/// <para>
/// The test generates a reference card showing:
/// - Box-drawing characters (single and double lines)
/// - Block graphics (shading characters)
/// - Extended ASCII characters (accented letters, symbols)
/// - Sample ANSI art patterns
/// </para>
/// <para>
/// Results can be compared against expected output to verify rendering correctness.
/// Platform-specific rendering issues are documented in the output.
/// </para>
/// </remarks>
internal static class RenderTestCommand
{
  /// <summary>
  /// Executes the render test command.
  /// </summary>
  /// <param name="args">Command-line arguments.</param>
  /// <returns>Exit code (0 for success, non-zero for failure).</returns>
  public static int Execute(string[] args)
  {
    try
    {
      // Parse options
      bool testPacket = false;
      bool showReference = true;
      bool showDiagnostics = true;
      string? packetPath = null;

      for (int i = 1; i < args.Length; i++)
      {
        string arg = args[i];

        if (arg == "--packet" && i + 1 < args.Length)
        {
          packetPath = args[i + 1];
          testPacket = true;
          i++;
        }
        else if (arg == "--no-reference")
        {
          showReference = false;
        }
        else if (arg == "--no-diagnostics")
        {
          showDiagnostics = false;
        }
        else if (arg == "--help" || arg == "-h")
        {
          ShowHelp();
          return 0;
        }
      }

      // Ensure console uses UTF-8 encoding
      ConfigureConsoleEncoding();

      if (showDiagnostics)
      {
        ShowEnvironmentDiagnostics();
      }

      if (showReference)
      {
        ShowReferenceCard();
      }

      if (testPacket && packetPath != null)
      {
        return TestPacketRendering(packetPath);
      }

      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error: {ex.Message}");
      return 1;
    }
  }

  /// <summary>
  /// Configures console encoding to UTF-8 for proper CP437 character display.
  /// </summary>
  private static void ConfigureConsoleEncoding()
  {
    try
    {
      // Set console to UTF-8 for proper Unicode character display
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Console.InputEncoding = System.Text.Encoding.UTF8;
    }
    catch
    {
      // Ignore encoding errors on platforms where this isn't supported
    }
  }

  /// <summary>
  /// Shows environment diagnostics for troubleshooting rendering issues.
  /// </summary>
  private static void ShowEnvironmentDiagnostics()
  {
    Console.WriteLine("=== Console Rendering Diagnostics ===");
    Console.WriteLine();
    
    Console.WriteLine("Environment Information:");
    Console.WriteLine($"  Operating System: {Environment.OSVersion}");
    Console.WriteLine($"  .NET Version: {Environment.Version}");
    Console.WriteLine($"  Console Encoding: {Console.OutputEncoding.EncodingName}");
    Console.WriteLine($"  Console Code Page: {Console.OutputEncoding.CodePage}");
    Console.WriteLine();

    // Test basic UTF-8 capability
    Console.WriteLine("UTF-8 Capability Test:");
    Console.WriteLine("  Euro sign: \u20AC");
    Console.WriteLine("  Bullet: \u2022");
    Console.WriteLine("  Check mark: \u2713");
    Console.WriteLine();

    // Platform-specific notes
    Console.WriteLine("Platform-Specific Notes:");
    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
    {
      Console.WriteLine("  Windows detected.");
      Console.WriteLine("  - Use 'chcp 65001' to set UTF-8 code page");
      Console.WriteLine("  - Windows Terminal recommended for best rendering");
      Console.WriteLine("  - Legacy console may show boxes for some characters");
    }
    else if (Environment.OSVersion.Platform == PlatformID.Unix)
    {
      Console.WriteLine("  Unix/Linux detected.");
      Console.WriteLine("  - Ensure LANG=en_GB.UTF-8 or similar");
      Console.WriteLine("  - Most modern terminals support UTF-8 by default");
      Console.WriteLine("  - Use 'locale' command to verify settings");
    }
    else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
    {
      Console.WriteLine("  macOS detected.");
      Console.WriteLine("  - Terminal.app supports UTF-8 by default");
      Console.WriteLine("  - iTerm2 recommended for advanced features");
      Console.WriteLine("  - Check Terminal > Preferences > Profiles > Advanced");
    }
    
    Console.WriteLine();
    Console.WriteLine("".PadRight(50, '='));
    Console.WriteLine();
  }

  /// <summary>
  /// Shows a reference card of CP437 box-drawing and extended ASCII characters.
  /// </summary>
  private static void ShowReferenceCard()
  {
    Console.WriteLine("=== CP437 Box-Drawing Reference Card ===");
    Console.WriteLine();

    // Single-line box drawing
    Console.WriteLine("Single-Line Box Drawing (CP437 0xB3-0xDA range):");
    byte[] singleLineBytes = new byte[] 
    { 
      0xC4, 0xB3, 0xDA, 0xBF, 0xC0, 0xD9, 
      0xC2, 0xC1, 0xB4, 0xC3, 0xC5 
    };
    string singleLine = Cp437Encoding.Decode(singleLineBytes, DecoderFallbackPolicy.ReplacementUnicode);
    Console.WriteLine("  Horizontal, Vertical");
    Console.WriteLine("  Corners: top-left top-right bottom-left bottom-right");
    Console.WriteLine("  T-junctions: down up left right cross");
    Console.WriteLine($"  Actual: {singleLine}");
    Console.WriteLine();

    // Double-line box drawing
    Console.WriteLine("Double-Line Box Drawing (CP437 0xC9-0xCE range):");
    byte[] doubleLineBytes = new byte[] 
    { 
      0xCD, 0xBA, 0xC9, 0xBB, 0xC8, 0xBC, 
      0xCB, 0xCA, 0xB9, 0xCC, 0xCE 
    };
    string doubleLine = Cp437Encoding.Decode(doubleLineBytes, DecoderFallbackPolicy.ReplacementUnicode);
    Console.WriteLine("  Horizontal, Vertical");
    Console.WriteLine("  Corners: top-left top-right bottom-left bottom-right");
    Console.WriteLine("  T-junctions: down up left right cross");
    Console.WriteLine($"  Actual: {doubleLine}");
    Console.WriteLine();

    // Block graphics
    Console.WriteLine("Block Graphics (CP437 0xB0-0xDB range):");
    byte[] blockBytes = new byte[] { 0xB0, 0xB1, 0xB2, 0xDB };
    string blocks = Cp437Encoding.Decode(blockBytes, DecoderFallbackPolicy.ReplacementUnicode);
    Console.WriteLine("  Shading: light medium dark full");
    Console.WriteLine($"  Actual: {blocks}");
    Console.WriteLine();

    // Sample box
    Console.WriteLine("Sample Single-Line Box:");
    DrawSampleBox(false);
    Console.WriteLine();

    Console.WriteLine("Sample Double-Line Box:");
    DrawSampleBox(true);
    Console.WriteLine();

    // Extended ASCII - International characters
    Console.WriteLine("International Characters (CP437 0x80-0x9F range):");
    byte[] internationalBytes = new byte[] 
    { 
      0x82, 0x8E, 0x83, 0x84, 0x85, 0x86, 0x87, 
      0x81, 0x94, 0x88, 0x89, 0x8A, 0x8B 
    };
    string international = Cp437Encoding.Decode(internationalBytes, DecoderFallbackPolicy.ReplacementUnicode);
    Console.WriteLine("  French: e-acute E-acute a-circumflex a-umlaut a-grave c-cedilla C-cedilla");
    Console.WriteLine("  German: u-umlaut o-umlaut e-umlaut i-umlaut");
    Console.WriteLine($"  Actual: {international}");
    Console.WriteLine();

    // ANSI art sample
    Console.WriteLine("Sample ANSI-style Art Pattern:");
    DrawAnsiArtSample();
    Console.WriteLine();

    Console.WriteLine("".PadRight(50, '='));
    Console.WriteLine();
  }

  /// <summary>
  /// Draws a sample box using CP437 box-drawing characters.
  /// </summary>
  /// <param name="doubleLine">If true, uses double-line characters; otherwise single-line.</param>
  private static void DrawSampleBox(bool doubleLine)
  {
    byte topLeft, topRight, bottomLeft, bottomRight, horizontal, vertical;

    if (doubleLine)
    {
      topLeft = 0xC9;      // Double top-left
      topRight = 0xBB;     // Double top-right
      bottomLeft = 0xC8;   // Double bottom-left
      bottomRight = 0xBC;  // Double bottom-right
      horizontal = 0xCD;   // Double horizontal
      vertical = 0xBA;     // Double vertical
    }
    else
    {
      topLeft = 0xDA;      // Single top-left
      topRight = 0xBF;     // Single top-right
      bottomLeft = 0xC0;   // Single bottom-left
      bottomRight = 0xD9;  // Single bottom-right
      horizontal = 0xC4;   // Single horizontal
      vertical = 0xB3;     // Single vertical
    }

    // Top line
    byte[] topLineBytes = new byte[22];
    topLineBytes[0] = topLeft;
    for (int i = 1; i < 21; i++)
    {
      topLineBytes[i] = horizontal;
    }
    topLineBytes[21] = topRight;
    string topLine = Cp437Encoding.Decode(topLineBytes, DecoderFallbackPolicy.ReplacementUnicode);
    Console.WriteLine($"  {topLine}");

    // Middle lines
    byte[] middleLineBytes = new byte[22];
    middleLineBytes[0] = vertical;
    for (int i = 1; i < 21; i++)
    {
      middleLineBytes[i] = (byte)' ';
    }
    middleLineBytes[21] = vertical;
    string middleLine = Cp437Encoding.Decode(middleLineBytes, DecoderFallbackPolicy.ReplacementUnicode);
    
    for (int i = 0; i < 3; i++)
    {
      Console.WriteLine($"  {middleLine}");
    }

    // Bottom line
    byte[] bottomLineBytes = new byte[22];
    bottomLineBytes[0] = bottomLeft;
    for (int i = 1; i < 21; i++)
    {
      bottomLineBytes[i] = horizontal;
    }
    bottomLineBytes[21] = bottomRight;
    string bottomLine = Cp437Encoding.Decode(bottomLineBytes, DecoderFallbackPolicy.ReplacementUnicode);
    Console.WriteLine($"  {bottomLine}");
  }

  /// <summary>
  /// Draws a sample ANSI art pattern using block graphics.
  /// </summary>
  private static void DrawAnsiArtSample()
  {
    // Create a simple gradient pattern using CP437 block graphics
    byte[] pattern1 = new byte[] { 0xDB, 0xDB, 0xDB, 0xB2, 0xB2, 0xB1, 0xB1, 0xB0, 0xB0, (byte)' ', (byte)' ' };
    byte[] pattern2 = new byte[] { 0xDB, 0xDB, 0xB2, 0xB2, 0xB1, 0xB1, 0xB0, 0xB0, (byte)' ', (byte)' ', (byte)' ' };
    byte[] pattern3 = new byte[] { 0xDB, 0xB2, 0xB2, 0xB1, 0xB1, 0xB0, 0xB0, (byte)' ', (byte)' ', (byte)' ', (byte)' ' };

    string line1 = Cp437Encoding.Decode(pattern1, DecoderFallbackPolicy.ReplacementUnicode);
    string line2 = Cp437Encoding.Decode(pattern2, DecoderFallbackPolicy.ReplacementUnicode);
    string line3 = Cp437Encoding.Decode(pattern3, DecoderFallbackPolicy.ReplacementUnicode);

    Console.WriteLine($"  {line1}");
    Console.WriteLine($"  {line2}");
    Console.WriteLine($"  {line3}");
  }

  /// <summary>
  /// Tests box-drawing rendering from an actual QWK packet.
  /// </summary>
  /// <param name="packetPath">Path to the QWK packet file.</param>
  /// <returns>Exit code.</returns>
  private static int TestPacketRendering(string packetPath)
  {
    try
    {
      Console.WriteLine($"=== Testing Packet Rendering: {Path.GetFileName(packetPath)} ===");
      Console.WriteLine();

      if (!File.Exists(packetPath))
      {
        Console.Error.WriteLine($"Error: Packet file not found: {packetPath}");
        return 1;
      }

      // Open and analyse packet
      QwkPacket packet = QwkPacket.Open(packetPath);

      // Test BBS name rendering
      Console.WriteLine("BBS Name:");
      Console.WriteLine($"  {packet.Control.BbsName}");
      
      byte[] bbsNameBytes = Cp437Encoding.Encode(packet.Control.BbsName, EncoderFallbackPolicy.ReplacementQuestion);
      TextAnalysis bbsAnalysis = TextAnalysis.Analyse(bbsNameBytes, includeHistogram: false);
      
      if (bbsAnalysis.HasBoxDrawingBytes)
      {
        Console.WriteLine($"  [OK] Contains box-drawing characters ({bbsAnalysis.BoxDrawingByteCount} characters)");
      }
      else
      {
        Console.WriteLine("  [--] No box-drawing characters detected");
      }
      Console.WriteLine();

      // Test conference names
      Console.WriteLine("Conference Names with Extended ASCII:");
      int conferenceCount = 0;
      foreach (ConferenceInfo conference in packet.Control.Conferences)
      {
        byte[] confNameBytes = Cp437Encoding.Encode(conference.Name, EncoderFallbackPolicy.ReplacementQuestion);
        TextAnalysis confAnalysis = TextAnalysis.Analyse(confNameBytes, includeHistogram: false);

        if (confAnalysis.ContainsHighBitBytes || confAnalysis.HasBoxDrawingBytes)
        {
          Console.WriteLine($"  [{conference.Number}] {conference.Name}");
          
          if (confAnalysis.HasBoxDrawingBytes)
          {
            Console.WriteLine($"      [OK] Box-drawing: {confAnalysis.BoxDrawingByteCount} characters");
          }
          if (confAnalysis.ContainsHighBitBytes)
          {
            Console.WriteLine($"      [OK] Extended ASCII: {confAnalysis.HighBitByteCount} characters");
          }

          conferenceCount++;
          if (conferenceCount >= 5)
          {
            Console.WriteLine("  ... (showing first 5 conferences with extended ASCII)");
            break;
          }
        }
      }

      if (conferenceCount == 0)
      {
        Console.WriteLine("  (No conferences with extended ASCII found)");
      }
      Console.WriteLine();

      // Test message rendering
      Console.WriteLine("Messages with Box-Drawing or ANSI Art:");
      int messageCount = 0;
      foreach (Message message in packet.Messages)
      {
        byte[] bodyBytes = Cp437Encoding.Encode(message.Body.RawText, EncoderFallbackPolicy.ReplacementQuestion);
        TextAnalysis bodyAnalysis = TextAnalysis.Analyse(bodyBytes, includeHistogram: false);

        if (bodyAnalysis.HasBoxDrawingBytes || bodyAnalysis.HasAnsiEscapes)
        {
          Console.WriteLine($"  Message #{message.MessageNumber}: {message.Subject}");
          Console.WriteLine($"    From: {message.From} To: {message.To}");

          if (bodyAnalysis.HasBoxDrawingBytes)
          {
            Console.WriteLine($"    [OK] Box-drawing: {bodyAnalysis.BoxDrawingByteCount} characters");
          }
          if (bodyAnalysis.HasAnsiEscapes)
          {
            Console.WriteLine("    [OK] ANSI escape sequences detected");
          }

          // Show first few lines
          Console.WriteLine("    Preview:");
          IReadOnlyList<string> lines = message.Body.Lines;
          for (int i = 0; i < Math.Min(3, lines.Count); i++)
          {
            Console.WriteLine($"      {lines[i]}");
          }

          messageCount++;
          if (messageCount >= 3)
          {
            Console.WriteLine("  ... (showing first 3 messages with graphics)");
            break;
          }

          Console.WriteLine();
        }
      }

      if (messageCount == 0)
      {
        Console.WriteLine("  (No messages with box-drawing or ANSI art found)");
      }
      Console.WriteLine();

      Console.WriteLine("".PadRight(50, '='));
      Console.WriteLine();

      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error testing packet: {ex.Message}");
      return 1;
    }
  }

  /// <summary>
  /// Shows command help.
  /// </summary>
  private static void ShowHelp()
  {
    Console.WriteLine("QWK.NET Box-Drawing Render Test");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  QwkNet.Diagnostics rendertest [options]");
    Console.WriteLine();
    Console.WriteLine("OPTIONS:");
    Console.WriteLine("  --packet <file>       Test rendering from a QWK packet");
    Console.WriteLine("  --no-reference        Skip reference card display");
    Console.WriteLine("  --no-diagnostics      Skip environment diagnostics");
    Console.WriteLine("  --help, -h            Show this help message");
    Console.WriteLine();
    Console.WriteLine("DESCRIPTION:");
    Console.WriteLine("  Tests console rendering of CP437 box-drawing characters and");
    Console.WriteLine("  extended ASCII. Displays a reference card and optionally tests");
    Console.WriteLine("  rendering from an actual QWK packet.");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  QwkNet.Diagnostics rendertest");
    Console.WriteLine("  QwkNet.Diagnostics rendertest --packet starol.qwk");
    Console.WriteLine("  QwkNet.Diagnostics rendertest --no-diagnostics");
  }
}