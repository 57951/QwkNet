using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QwkNet;
using QwkNet.Diagnostics.Commands;
using QwkNet.Encoding;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;

namespace QwkNet.Diagnostics.Formatting;

/// <summary>
/// Formats messages as human-readable text with detailed analysis.
/// </summary>
internal sealed class TextMessageFormatter : IMessageFormatter
{
  private readonly bool _showRaw;
  private readonly bool _showKludges;
  private readonly bool _showCp437;

  public TextMessageFormatter(bool showRaw, bool showKludges, bool showCp437)
  {
    _showRaw = showRaw;
    _showKludges = showKludges;
    _showCp437 = showCp437;
  }

  public string Format(List<MessageView> messages, QwkPacket packet)
  {
    StringBuilder output = new StringBuilder();

    foreach (MessageView msgView in messages)
    {
      if (output.Length > 0)
      {
        output.AppendLine();
        output.AppendLine();
      }

      FormatMessage(output, msgView, packet);
    }

    return output.ToString();
  }

  private void FormatMessage(StringBuilder output, MessageView msgView, QwkPacket packet)
  {
    Message message = msgView.Message;

    // Header section
    output.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
    output.AppendLine($"MESSAGE {msgView.DisplayNumber} / {msgView.TotalMessages}");
    output.AppendLine("═══════════════════════════════════════════════════════════════════════════════");

    // Metadata
    string fromName = message.From;
    string toName = message.To;
    string subject = message.Subject;

    // Check for QWKE extended headers
    if (message.Kludges.Count > 0)
    {
      if (message.Kludges.GetFirstByKey("From") != null)
      {
        fromName = $"{fromName} [EXTENDED]";
      }
      if (message.Kludges.GetFirstByKey("To") != null)
      {
        toName = $"{toName} [EXTENDED]";
      }
      if (message.Kludges.GetFirstByKey("Subject") != null)
      {
        subject = $"{subject} [EXTENDED]";
      }
    }

    output.AppendLine($"From:         {fromName}");
    output.AppendLine($"To:           {toName}");
    output.AppendLine($"Subject:      {subject}");
    
    string dateStr = message.DateTime.HasValue 
      ? message.DateTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
      : "Invalid Date";
    output.AppendLine($"Date:         {dateStr}");

    // Get conference name
    string confName = GetConferenceName(packet, message.ConferenceNumber);
    output.AppendLine($"Conference:   {message.ConferenceNumber} ({confName})");

    // Status
    List<string> statusParts = new List<string>();
    statusParts.Add(message.IsPrivate ? "Private" : "Public");
    statusParts.Add(message.IsRead ? "Read" : "Unread");
    if (message.IsDeleted)
    {
      statusParts.Add("Deleted");
    }
    output.AppendLine($"Status:       {string.Join(", ", statusParts)}");

    // Blocks
    int blockCount = message.RawHeader.BlockCount;
    int expectedBodyBlocks = Math.Max(0, blockCount - 1);
    int expectedTotalBytes = blockCount * 128;
    output.AppendLine($"Blocks:       {blockCount} (Header + {expectedBodyBlocks} body blocks = {expectedTotalBytes} bytes)");

    output.AppendLine($"Message #:    {message.MessageNumber}");
    output.AppendLine($"Reference #:  {message.ReferenceNumber}");

    // QWKE kludges section
    if (_showKludges && message.Kludges.Count > 0)
    {
      output.AppendLine();
      output.AppendLine($"QWKE KLUDGES ({message.Kludges.Count} lines):");
      output.AppendLine("───────────────────────────────────────────────────────────────────────────────");
      
      foreach (MessageKludge kludge in message.Kludges)
      {
        // Format multi-line kludges with proper indentation
        string[] lines = kludge.Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 1)
        {
          output.AppendLine($"{kludge.Key}: {lines[0]}");
        }
        else
        {
          output.AppendLine($"{kludge.Key}: {lines[0]}");
          for (int i = 1; i < lines.Length; i++)
          {
            output.AppendLine($"         {lines[i]}");
          }
        }
      }
    }

    // Body section
    output.AppendLine();
    string bodyText = message.Body.RawText;
    int charCount = bodyText.TrimEnd(' ', '\0').Length;
    int lineCount = message.Body.Lines.Count;
    output.AppendLine($"BODY ({charCount} characters, {lineCount} lines):");
    output.AppendLine("───────────────────────────────────────────────────────────────────────────────");

    // Display body with optional CP437 highlighting
    if (_showCp437)
    {
      output.AppendLine(FormatBodyWithCp437(bodyText));
    }
    else
    {
      output.AppendLine(FormatBodyPlain(bodyText));
    }

    // CP437 analysis
    output.AppendLine();
    Cp437Analysis analysis = AnalyseBodyContent(bodyText);
    output.AppendLine("CP437 ANALYSIS:");
    output.AppendLine($"  Line terminators (0xE3 / π): {analysis.LineTerminatorCount}");
    output.AppendLine($"  Box-drawing characters: {analysis.BoxDrawingCount}");
    output.AppendLine($"  International characters: {analysis.InternationalCount}");
    output.AppendLine($"  ANSI escape sequences: {analysis.AnsiEscapeCount}");

    // Raw hex display
    if (_showRaw)
    {
      output.AppendLine();
      output.AppendLine("RAW HEX:");
      output.AppendLine("───────────────────────────────────────────────────────────────────────────────");
      output.AppendLine(FormatHexDump(bodyText));
    }

    // Validation notes
    output.AppendLine();
    output.AppendLine("VALIDATION NOTES:");
    FormatValidationNotes(output, message);

    // Conclusion
    output.AppendLine();
    output.AppendLine("CONCLUSION:");
    FormatConclusion(output, message);
  }

  private string FormatBodyPlain(string bodyText)
  {
    // CP437 byte 0xE3 decodes to π (U+03C0) in Unicode
    const char qwkTerminator = '\u03C0';
    string displayed = bodyText.Replace(qwkTerminator.ToString(), "⟨E3⟩");
    
    // Trim trailing padding
    displayed = displayed.TrimEnd(' ', '\0');
    
    return displayed;
  }

  private string FormatBodyWithCp437(string bodyText)
  {
    StringBuilder result = new StringBuilder();
    // CP437 byte 0xE3 decodes to π (U+03C0) in Unicode
    const char qwkTerminator = '\u03C0';
    const char ansiEscape = (char)0x1B;

    foreach (char c in bodyText)
    {
      if (c == qwkTerminator)
      {
        result.Append("⟨E3:π⟩");
      }
      else if (c == ansiEscape)
      {
        result.Append("⟨ESC⟩");
      }
      else if (c >= 0x80 && c <= 0xFF)
      {
        // High-bit character - display as-is (it will show as CP437 equivalent)
        result.Append(c);
      }
      else
      {
        result.Append(c);
      }
    }

    // Trim trailing padding
    string displayed = result.ToString().TrimEnd(' ', '\0');
    return displayed;
  }

  private string FormatHexDump(string bodyText)
  {
    StringBuilder hex = new StringBuilder();
    System.Text.Encoding cp437 = Cp437Encoding.GetEncoding();
    byte[] bytes = cp437.GetBytes(bodyText);

    for (int i = 0; i < bytes.Length; i += 16)
    {
      // Offset
      hex.Append($"{i:X4}  ");

      // Hex bytes
      for (int j = 0; j < 16; j++)
      {
        if (i + j < bytes.Length)
        {
          hex.Append($"{bytes[i + j]:X2} ");
        }
        else
        {
          hex.Append("   ");
        }

        if (j == 7)
        {
          hex.Append(" ");
        }
      }

      hex.Append(" ");

      // ASCII representation
      for (int j = 0; j < 16 && i + j < bytes.Length; j++)
      {
        byte b = bytes[i + j];
        char c = (b >= 32 && b < 127) ? (char)b : '.';
        hex.Append(c);
      }

      hex.AppendLine();
    }

    return hex.ToString();
  }

  private Cp437Analysis AnalyseBodyContent(string bodyText)
  {
    Cp437Analysis analysis = new Cp437Analysis();
    // CP437 byte 0xE3 decodes to π (U+03C0) in Unicode
    // See ON_BYTE_0XE3.md for detailed explanation
    const char qwkTerminator = '\u03C0';  // π character (CP437 0xE3)
    const char ansiEscape = (char)0x1B;

    for (int i = 0; i < bodyText.Length; i++)
    {
      char c = bodyText[i];

      if (c == qwkTerminator)
      {
        analysis.LineTerminatorCount++;
      }
      else if (c == ansiEscape && i + 1 < bodyText.Length && bodyText[i + 1] == '[')
      {
        analysis.AnsiEscapeCount++;
      }
      else if (c >= 0x80 && c <= 0xFF)
      {
        // Check if it's a box-drawing character (CP437 range)
        if (ByteClassifier.IsBoxDrawing((byte)c))
        {
          analysis.BoxDrawingCount++;
        }
        else
        {
          analysis.InternationalCount++;
        }
      }
    }

    return analysis;
  }

  private void FormatValidationNotes(StringBuilder output, Message message)
  {
    int blockCount = message.RawHeader.BlockCount;
    int expectedBodyBlocks = Math.Max(0, blockCount - 1);
    int actualBodyBytes = message.Body.RawText.Length;

    output.AppendLine("  ✓ Header complete (128 bytes)");

    // Analyse body blocks
    int bytesProcessed = 0;
    for (int i = 1; i <= expectedBodyBlocks; i++)
    {
      int blockStart = bytesProcessed;
      int blockSize = Math.Min(128, actualBodyBytes - bytesProcessed);
      bytesProcessed += blockSize;

      if (blockSize == 128)
      {
        output.AppendLine($"  ✓ Body block {i} complete (128 bytes)");
      }
      else if (blockSize > 0)
      {
        output.AppendLine($"  ✗ Body block {i} incomplete ({blockSize} bytes, expected 128)");
      }
      else
      {
        output.AppendLine($"  ✗ Body block {i} missing (0 bytes, expected 128)");
      }
    }
  }

  private void FormatConclusion(StringBuilder output, Message message)
  {
    int blockCount = message.RawHeader.BlockCount;
    int expectedBodyBytes = Math.Max(0, blockCount - 1) * 128;
    int actualBodyBytes = message.Body.RawText.Length;

    if (actualBodyBytes >= expectedBodyBytes)
    {
      output.AppendLine("This message is complete and properly formatted.");
    }
    else
    {
      int missingBytes = expectedBodyBytes - actualBodyBytes;
      output.AppendLine($"This message is truncated. {missingBytes} bytes are missing from the expected");
      output.AppendLine($"{expectedBodyBytes} bytes. This is likely a source data issue where the QWK packet");
      output.AppendLine("was created before the message was fully written to MESSAGES.DAT.");
    }
  }

  private string GetConferenceName(QwkPacket packet, ushort conferenceNumber)
  {
    ConferenceInfo? conference = packet.Conferences.FirstOrDefault(c => c.Number == conferenceNumber);
    return conference?.Name ?? "Unknown";
  }

  private sealed class Cp437Analysis
  {
    public int LineTerminatorCount { get; set; }
    public int BoxDrawingCount { get; set; }
    public int InternationalCount { get; set; }
    public int AnsiEscapeCount { get; set; }
  }
}