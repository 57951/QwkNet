using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using QwkNet.Encoding;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Commands;

/// <summary>
/// Performs round-trip testing of QWK → REP → QWK packet conversion.
/// </summary>
/// <remarks>
/// <para>
/// This command validates that messages can survive the full round-trip cycle:
/// reading from a QWK packet, writing to a REP packet, and reading back from
/// the REP packet.
/// </para>
/// <para>
/// The test verifies message count preservation, field preservation (From, To,
/// Subject), and body text preservation within CP437 encoding limits.
/// </para>
/// </remarks>
internal static class RoundTripCommand
{
  /// <summary>
  /// Executes the round-trip test command.
  /// </summary>
  /// <param name="args">Command-line arguments.</param>
  /// <returns>Exit code: 0 for success, 1 for failure.</returns>
  public static int Execute(string[] args)
  {
    if (args.Length < 2)
    {
      Console.Error.WriteLine("Error: Missing packet file path.");
      Console.Error.WriteLine("Usage: QwkNet.Diagnostics roundtrip <packet.qwk> [options]");
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
    bool verbose = false;
    bool showDifferences = true;

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

        case "--verbose":
          verbose = true;
          break;

        case "--no-diff":
          showDifferences = false;
          break;

        default:
          Console.Error.WriteLine($"Error: Unknown option: {args[i]}");
          return 1;
      }
    }

    // Execute round-trip test
    RoundTripResult result = PerformRoundTrip(filePath, mode, verbose);

    // Display results
    DisplayResults(result, verbose, showDifferences);

    return result.Success ? 0 : 1;
  }

  /// <summary>
  /// Performs the complete round-trip test.
  /// </summary>
  /// <param name="filePath">Path to the QWK packet file.</param>
  /// <param name="mode">Validation mode.</param>
  /// <param name="verbose">Whether to display verbose output.</param>
  /// <returns>The round-trip test results.</returns>
  private static RoundTripResult PerformRoundTrip(string filePath, ValidationMode mode, bool verbose)
  {
    RoundTripResult result = new RoundTripResult
    {
      OriginalFilePath = filePath,
      ValidationMode = mode
    };

    Stopwatch totalTimer = Stopwatch.StartNew();

    try
    {
      // Step 1: Open original QWK packet
      if (verbose)
      {
        Console.WriteLine("Step 1: Opening original QWK packet...");
      }

      Stopwatch stepTimer = Stopwatch.StartNew();
      QwkPacket originalPacket;
      
      try
      {
        originalPacket = QwkPacket.Open(filePath, mode);
        result.OriginalPacketLoaded = true;
      }
      catch (Exception ex)
      {
        result.ErrorMessage = $"Failed to open original QWK packet: {ex.Message}";
        result.Success = false;
        return result;
      }

      result.LoadOriginalTimeMs = stepTimer.ElapsedMilliseconds;
      result.OriginalMessageCount = originalPacket.Messages.Count;

      if (verbose)
      {
        Console.WriteLine($"  Loaded {result.OriginalMessageCount} messages in {result.LoadOriginalTimeMs}ms");
      }

      // Step 2: Create REP packet from messages
      if (verbose)
      {
        Console.WriteLine("Step 2: Creating REP packet from messages...");
      }

      stepTimer.Restart();
      RepPacket repPacket = RepPacket.Create(originalPacket.Control);

      foreach (Message msg in originalPacket.Messages)
      {
        repPacket.AddMessage(msg);
      }

      result.RepPacketCreated = true;
      result.CreateRepTimeMs = stepTimer.ElapsedMilliseconds;

      if (verbose)
      {
        Console.WriteLine($"  Created REP packet with {repPacket.Messages.Count} messages in {result.CreateRepTimeMs}ms");
      }

      // Step 3: Write REP packet to memory
      if (verbose)
      {
        Console.WriteLine("Step 3: Writing REP packet to memory stream...");
      }

      stepTimer.Restart();
      MemoryStream repStream = new MemoryStream();
      
      try
      {
        repPacket.Save(repStream);
        result.RepPacketSaved = true;
      }
      catch (Exception ex)
      {
        result.ErrorMessage = $"Failed to save REP packet: {ex.Message}";
        result.Success = false;
        originalPacket.Dispose();
        repPacket.Dispose();
        return result;
      }

      result.WriteRepTimeMs = stepTimer.ElapsedMilliseconds;
      result.RepPacketSizeBytes = repStream.Length;

      if (verbose)
      {
        Console.WriteLine($"  Wrote {result.RepPacketSizeBytes} bytes in {result.WriteRepTimeMs}ms");
      }

      // Step 4: Read REP packet back as QWK
      if (verbose)
      {
        Console.WriteLine("Step 4: Reading REP packet back as QWK...");
      }

      stepTimer.Restart();
      repStream.Position = 0;
      
      QwkPacket roundTripPacket;
      
      try
      {
        roundTripPacket = QwkPacket.Open(repStream, mode);
        result.RoundTripPacketLoaded = true;
      }
      catch (Exception ex)
      {
        result.ErrorMessage = $"Failed to read REP packet back: {ex.Message}";
        result.Success = false;
        originalPacket.Dispose();
        repPacket.Dispose();
        repStream.Dispose();
        return result;
      }

      result.LoadRoundTripTimeMs = stepTimer.ElapsedMilliseconds;
      result.RoundTripMessageCount = roundTripPacket.Messages.Count;

      if (verbose)
      {
        Console.WriteLine($"  Loaded {result.RoundTripMessageCount} messages in {result.LoadRoundTripTimeMs}ms");
      }

      // Step 5: Compare messages
      if (verbose)
      {
        Console.WriteLine("Step 5: Comparing messages...");
      }

      stepTimer.Restart();
      CompareMessages(originalPacket.Messages, roundTripPacket.Messages, result);
      result.CompareTimeMs = stepTimer.ElapsedMilliseconds;

      if (verbose)
      {
        Console.WriteLine($"  Comparison completed in {result.CompareTimeMs}ms");
      }

      // Clean up
      originalPacket.Dispose();
      repPacket.Dispose();
      roundTripPacket.Dispose();
      repStream.Dispose();

      totalTimer.Stop();
      result.TotalTimeMs = totalTimer.ElapsedMilliseconds;
      result.Success = result.MessageCountMatches && 
                       result.MessageDifferences.Count == 0;
    }
    catch (Exception ex)
    {
      result.ErrorMessage = $"Unexpected error during round-trip test: {ex.Message}";
      result.Success = false;
    }

    return result;
  }

  /// <summary>
  /// Compares original and round-trip message collections.
  /// </summary>
  /// <param name="original">Original messages.</param>
  /// <param name="roundTrip">Round-trip messages.</param>
  /// <param name="result">Result object to populate.</param>
  private static void CompareMessages(
    MessageCollection original,
    MessageCollection roundTrip,
    RoundTripResult result)
  {
    // Check message count
    result.MessageCountMatches = original.Count == roundTrip.Count;

    if (!result.MessageCountMatches)
    {
      // Cannot proceed with individual message comparison - no MessageDifference to add
      return;
    }

    // Compare individual messages
    for (int i = 0; i < original.Count; i++)
    {
      Message originalMsg = original[i];
      Message roundTripMsg = roundTrip[i];

      List<string> differences = new List<string>();

      // Compare From field
      if (originalMsg.From != roundTripMsg.From)
      {
        differences.Add($"From: '{originalMsg.From}' → '{roundTripMsg.From}'");
      }

      // Compare To field
      if (originalMsg.To != roundTripMsg.To)
      {
        differences.Add($"To: '{originalMsg.To}' → '{roundTripMsg.To}'");
      }

      // Compare Subject field
      if (originalMsg.Subject != roundTripMsg.Subject)
      {
        differences.Add($"Subject: '{originalMsg.Subject}' → '{roundTripMsg.Subject}'");
      }

      // Compare conference number
      if (originalMsg.ConferenceNumber != roundTripMsg.ConferenceNumber)
      {
        differences.Add(
          $"Conference: {originalMsg.ConferenceNumber} → {roundTripMsg.ConferenceNumber}");
      }

      // Compare message body
      string originalBody = NormaliseBodyText(originalMsg.Body.GetDecodedText());
      string roundTripBody = NormaliseBodyText(roundTripMsg.Body.GetDecodedText());

      if (originalBody != roundTripBody)
      {
        // Calculate Levenshtein distance for similarity
        int distance = CalculateLevenshteinDistance(originalBody, roundTripBody);
        double similarity = 1.0 - ((double)distance / Math.Max(originalBody.Length, roundTripBody.Length));

        differences.Add($"Body text differs (similarity: {similarity:P1}, distance: {distance})");
      }

      // Record differences if any
      if (differences.Count > 0)
      {
        MessageDifference diff = new MessageDifference
        {
          MessageIndex = i,
          OriginalMessage = originalMsg,
          RoundTripMessage = roundTripMsg,
          Differences = differences
        };

        result.MessageDifferences.Add(diff);
      }
    }
  }

  /// <summary>
  /// Normalises message body text for comparison.
  /// </summary>
  /// <param name="text">The text to normalise.</param>
  /// <returns>Normalised text.</returns>
  /// <remarks>
  /// Normalisation handles line ending variations and trailing whitespace
  /// that may differ between QWK and REP packet formats.
  /// </remarks>
  private static string NormaliseBodyText(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return string.Empty;
    }

    // Normalise line endings to \n
    string normalised = text.Replace("\r\n", "\n").Replace("\r", "\n");

    // Trim trailing whitespace from each line
    string[] lines = normalised.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
      lines[i] = lines[i].TrimEnd();
    }

    return string.Join("\n", lines).Trim();
  }

  /// <summary>
  /// Calculates the Levenshtein distance between two strings.
  /// </summary>
  /// <param name="source">First string.</param>
  /// <param name="target">Second string.</param>
  /// <returns>The edit distance.</returns>
  private static int CalculateLevenshteinDistance(string source, string target)
  {
    if (string.IsNullOrEmpty(source))
    {
      return string.IsNullOrEmpty(target) ? 0 : target.Length;
    }

    if (string.IsNullOrEmpty(target))
    {
      return source.Length;
    }

    int sourceLength = source.Length;
    int targetLength = target.Length;

    int[,] distance = new int[sourceLength + 1, targetLength + 1];

    for (int i = 0; i <= sourceLength; i++)
    {
      distance[i, 0] = i;
    }

    for (int j = 0; j <= targetLength; j++)
    {
      distance[0, j] = j;
    }

    for (int i = 1; i <= sourceLength; i++)
    {
      for (int j = 1; j <= targetLength; j++)
      {
        int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

        distance[i, j] = Math.Min(
          Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
          distance[i - 1, j - 1] + cost);
      }
    }

    return distance[sourceLength, targetLength];
  }

  /// <summary>
  /// Displays the round-trip test results.
  /// </summary>
  /// <param name="result">The test results.</param>
  /// <param name="verbose">Whether to show verbose output.</param>
  /// <param name="showDifferences">Whether to show detailed differences.</param>
  private static void DisplayResults(RoundTripResult result, bool verbose, bool showDifferences)
  {
    Console.WriteLine();
    Console.WriteLine("=".PadRight(80, '='));
    Console.WriteLine("QWK.NET Round-Trip Test Results");
    Console.WriteLine("=".PadRight(80, '='));
    Console.WriteLine();

    Console.WriteLine($"File: {result.OriginalFilePath}");
    Console.WriteLine($"Validation Mode: {result.ValidationMode}");
    Console.WriteLine();

    // Test status
    if (result.Success)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("✓ PASSED");
      Console.ResetColor();
    }
    else
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine("✗ FAILED");
      Console.ResetColor();

      if (!string.IsNullOrEmpty(result.ErrorMessage))
      {
        Console.WriteLine();
        Console.WriteLine($"Error: {result.ErrorMessage}");
      }
    }

    Console.WriteLine();

    // Test steps
    Console.WriteLine("Test Steps:");
    Console.WriteLine($"  1. Load original QWK:  {(result.OriginalPacketLoaded ? "✓" : "✗")} ({result.LoadOriginalTimeMs}ms)");
    Console.WriteLine($"  2. Create REP packet:  {(result.RepPacketCreated ? "✓" : "✗")} ({result.CreateRepTimeMs}ms)");
    Console.WriteLine($"  3. Write REP to disk:  {(result.RepPacketSaved ? "✓" : "✗")} ({result.WriteRepTimeMs}ms)");
    Console.WriteLine($"  4. Load REP as QWK:    {(result.RoundTripPacketLoaded ? "✓" : "✗")} ({result.LoadRoundTripTimeMs}ms)");
    Console.WriteLine($"  5. Compare messages:   {(result.MessageCountMatches && result.MessageDifferences.Count == 0 ? "✓" : "✗")} ({result.CompareTimeMs}ms)");
    Console.WriteLine();

    // Message statistics
    Console.WriteLine("Message Statistics:");
    Console.WriteLine($"  Original message count:    {result.OriginalMessageCount}");
    Console.WriteLine($"  Round-trip message count:  {result.RoundTripMessageCount}");
    Console.WriteLine($"  Count matches:             {(result.MessageCountMatches ? "Yes" : "No")}");
    Console.WriteLine($"  Messages with differences: {result.MessageDifferences.Count}");
    Console.WriteLine();

    // Performance metrics
    if (verbose)
    {
      Console.WriteLine("Performance Metrics:");
      Console.WriteLine($"  REP packet size:  {FormatBytes(result.RepPacketSizeBytes)}");
      Console.WriteLine($"  Total time:       {result.TotalTimeMs}ms");
      Console.WriteLine();
    }

    // Message differences
    if (showDifferences && result.MessageDifferences.Count > 0)
    {
      Console.WriteLine("Message Differences:");
      Console.WriteLine();

      int displayCount = Math.Min(result.MessageDifferences.Count, verbose ? int.MaxValue : 10);

      for (int i = 0; i < displayCount; i++)
      {
        MessageDifference diff = result.MessageDifferences[i];

        Console.WriteLine($"Message #{diff.MessageIndex + 1}:");
        
        foreach (string difference in diff.Differences)
        {
          Console.WriteLine($"  - {difference}");
        }

        if (verbose && diff.Differences.Any(d => d.StartsWith("Body text differs")))
        {
          Console.WriteLine();
          Console.WriteLine("  Original body preview:");
          Console.WriteLine($"    {TruncateText(diff.OriginalMessage!.Body.GetDecodedText(), 100)}");
          Console.WriteLine();
          Console.WriteLine("  Round-trip body preview:");
          Console.WriteLine($"    {TruncateText(diff.RoundTripMessage!.Body.GetDecodedText(), 100)}");
        }

        Console.WriteLine();
      }

      if (result.MessageDifferences.Count > displayCount)
      {
        Console.WriteLine($"... and {result.MessageDifferences.Count - displayCount} more differences.");
        Console.WriteLine("Use --verbose to see all differences.");
        Console.WriteLine();
      }
    }

    Console.WriteLine("=".PadRight(80, '='));
  }

  /// <summary>
  /// Formats a byte count into human-readable form.
  /// </summary>
  /// <param name="bytes">Byte count.</param>
  /// <returns>Formatted string.</returns>
  private static string FormatBytes(long bytes)
  {
    if (bytes < 1024)
    {
      return $"{bytes} bytes";
    }
    else if (bytes < 1024 * 1024)
    {
      return $"{bytes / 1024.0:F1} KB";
    }
    else
    {
      return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
  }

  /// <summary>
  /// Truncates text to a maximum length.
  /// </summary>
  /// <param name="text">Text to truncate.</param>
  /// <param name="maxLength">Maximum length.</param>
  /// <returns>Truncated text.</returns>
  private static string TruncateText(string text, int maxLength)
  {
    if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
    {
      return text ?? string.Empty;
    }

    return text.Substring(0, maxLength) + "...";
  }

  /// <summary>
  /// Parses a validation mode from a string.
  /// </summary>
  /// <param name="value">The string value.</param>
  /// <returns>The validation mode.</returns>
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
}

/// <summary>
/// Represents the results of a round-trip test.
/// </summary>
internal sealed class RoundTripResult
{
  /// <summary>
  /// Gets or sets the original file path.
  /// </summary>
  public string OriginalFilePath { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the validation mode used.
  /// </summary>
  public ValidationMode ValidationMode { get; set; }

  /// <summary>
  /// Gets or sets whether the test succeeded.
  /// </summary>
  public bool Success { get; set; }

  /// <summary>
  /// Gets or sets the error message if the test failed.
  /// </summary>
  public string? ErrorMessage { get; set; }

  /// <summary>
  /// Gets or sets whether the original packet was loaded successfully.
  /// </summary>
  public bool OriginalPacketLoaded { get; set; }

  /// <summary>
  /// Gets or sets whether the REP packet was created successfully.
  /// </summary>
  public bool RepPacketCreated { get; set; }

  /// <summary>
  /// Gets or sets whether the REP packet was saved successfully.
  /// </summary>
  public bool RepPacketSaved { get; set; }

  /// <summary>
  /// Gets or sets whether the round-trip packet was loaded successfully.
  /// </summary>
  public bool RoundTripPacketLoaded { get; set; }

  /// <summary>
  /// Gets or sets the number of messages in the original packet.
  /// </summary>
  public int OriginalMessageCount { get; set; }

  /// <summary>
  /// Gets or sets the number of messages in the round-trip packet.
  /// </summary>
  public int RoundTripMessageCount { get; set; }

  /// <summary>
  /// Gets or sets whether message counts match.
  /// </summary>
  public bool MessageCountMatches { get; set; }

  /// <summary>
  /// Gets the list of message differences found.
  /// </summary>
  public List<MessageDifference> MessageDifferences { get; } = new List<MessageDifference>();

  /// <summary>
  /// Gets or sets the time taken to load the original packet (milliseconds).
  /// </summary>
  public long LoadOriginalTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the time taken to create the REP packet (milliseconds).
  /// </summary>
  public long CreateRepTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the time taken to write the REP packet (milliseconds).
  /// </summary>
  public long WriteRepTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the time taken to load the round-trip packet (milliseconds).
  /// </summary>
  public long LoadRoundTripTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the time taken to compare messages (milliseconds).
  /// </summary>
  public long CompareTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the total time taken for the test (milliseconds).
  /// </summary>
  public long TotalTimeMs { get; set; }

  /// <summary>
  /// Gets or sets the size of the REP packet in bytes.
  /// </summary>
  public long RepPacketSizeBytes { get; set; }
}

/// <summary>
/// Represents a difference between original and round-trip messages.
/// </summary>
internal sealed class MessageDifference
{
  /// <summary>
  /// Gets or sets the message index (zero-based).
  /// </summary>
  public int MessageIndex { get; set; }

  /// <summary>
  /// Gets or sets the original message.
  /// </summary>
  public Message? OriginalMessage { get; set; }

  /// <summary>
  /// Gets or sets the round-trip message.
  /// </summary>
  public Message? RoundTripMessage { get; set; }

  /// <summary>
  /// Gets the list of specific differences.
  /// </summary>
  public List<string> Differences { get; set; } = new List<string>();
}