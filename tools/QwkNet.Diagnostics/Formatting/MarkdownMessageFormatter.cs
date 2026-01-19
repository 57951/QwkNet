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
/// Formats messages as Markdown documents.
/// </summary>
internal sealed class MarkdownMessageFormatter : IMessageFormatter
{
  private readonly bool _showKludges;
  private readonly bool _showCp437;

  public MarkdownMessageFormatter(bool showKludges, bool showCp437)
  {
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
        output.AppendLine("---");
        output.AppendLine();
      }

      FormatMessage(output, msgView, packet);
    }

    return output.ToString();
  }

  private void FormatMessage(StringBuilder output, MessageView msgView, QwkPacket packet)
  {
    Message message = msgView.Message;

    // Header
    output.AppendLine($"# Message {msgView.DisplayNumber}");
    output.AppendLine();

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

    output.AppendLine($"**From:** {EscapeMarkdown(fromName)}  ");
    output.AppendLine($"**To:** {EscapeMarkdown(toName)}  ");
    output.AppendLine($"**Subject:** {EscapeMarkdown(subject)}  ");
    
    string dateStr = message.DateTime.HasValue 
      ? message.DateTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
      : "Invalid Date";
    output.AppendLine($"**Date:** {dateStr}  ");

    string confName = GetConferenceName(packet, message.ConferenceNumber);
    output.AppendLine($"**Conference:** {message.ConferenceNumber} ({EscapeMarkdown(confName)})  ");

    List<string> statusParts = new List<string>();
    statusParts.Add(message.IsPrivate ? "Private" : "Public");
    statusParts.Add(message.IsRead ? "Read" : "Unread");
    if (message.IsDeleted)
    {
      statusParts.Add("Deleted");
    }
    output.AppendLine($"**Status:** {string.Join(", ", statusParts)}");
    output.AppendLine();

    // QWKE kludges
    if (_showKludges && message.Kludges.Count > 0)
    {
      output.AppendLine("## QWKE Kludges");
      output.AppendLine();
      foreach (MessageKludge kludge in message.Kludges)
      {
        output.AppendLine($"- **{EscapeMarkdown(kludge.Key)}:** {EscapeMarkdown(kludge.Value)}");
      }
      output.AppendLine();
    }

    // Body
    output.AppendLine("## Body");
    output.AppendLine();
    output.AppendLine("```");
    
    string bodyText = message.Body.RawText;
    if (_showCp437)
    {
      output.AppendLine(FormatBodyWithCp437(bodyText));
    }
    else
    {
      output.AppendLine(FormatBodyPlain(bodyText));
    }
    
    output.AppendLine("```");
    output.AppendLine();

    // CP437 Analysis
    output.AppendLine("## CP437 Analysis");
    output.AppendLine();
    Cp437Analysis analysis = AnalyseBodyContent(bodyText);
    output.AppendLine($"- Line terminators (0xE3 / π): {analysis.LineTerminatorCount}");
    output.AppendLine($"- Box-drawing characters: {analysis.BoxDrawingCount}");
    output.AppendLine($"- International characters: {analysis.InternationalCount}");
    output.AppendLine($"- ANSI escape sequences: {analysis.AnsiEscapeCount}");
    output.AppendLine();

    // Validation Notes
    output.AppendLine("## Validation Notes");
    output.AppendLine();
    FormatValidationNotes(output, message);
  }

  private string FormatBodyPlain(string bodyText)
  {
    // CP437 byte 0xE3 decodes to π (U+03C0) in Unicode
    const char qwkTerminator = '\u03C0';  // π character (CP437 0xE3)
    string displayed = bodyText.Replace(qwkTerminator.ToString(), "⟨E3⟩");
    displayed = displayed.TrimEnd(' ', '\0');
    return displayed;
  }

  private string FormatBodyWithCp437(string bodyText)
  {
    StringBuilder result = new StringBuilder();
    // CP437 byte 0xE3 decodes to π (U+03C0) in Unicode
    const char qwkTerminator = '\u03C0';  // π character (CP437 0xE3)
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
      else
      {
        result.Append(c);
      }
    }

    string displayed = result.ToString().TrimEnd(' ', '\0');
    return displayed;
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

    output.AppendLine("- ✓ Header complete (128 bytes)");

    int bytesProcessed = 0;
    for (int i = 1; i <= expectedBodyBlocks; i++)
    {
      int blockSize = Math.Min(128, actualBodyBytes - bytesProcessed);
      bytesProcessed += blockSize;

      if (blockSize == 128)
      {
        output.AppendLine($"- ✓ Body block {i} complete (128 bytes)");
      }
      else if (blockSize > 0)
      {
        output.AppendLine($"- ✗ Body block {i} incomplete ({blockSize} bytes, expected 128)");
      }
      else
      {
        output.AppendLine($"- ✗ Body block {i} missing (0 bytes, expected 128)");
      }
    }
  }

  private string GetConferenceName(QwkPacket packet, ushort conferenceNumber)
  {
    ConferenceInfo? conference = packet.Conferences.FirstOrDefault(c => c.Number == conferenceNumber);
    return conference?.Name ?? "Unknown";
  }

  private string EscapeMarkdown(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    // Escape special Markdown characters
    return text
      .Replace("\\", "\\\\")
      .Replace("*", "\\*")
      .Replace("_", "\\_")
      .Replace("[", "\\[")
      .Replace("]", "\\]")
      .Replace("`", "\\`");
  }

  private sealed class Cp437Analysis
  {
    public int LineTerminatorCount { get; set; }
    public int BoxDrawingCount { get; set; }
    public int InternationalCount { get; set; }
    public int AnsiEscapeCount { get; set; }
  }
}