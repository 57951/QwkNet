using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using QwkNet;
using QwkNet.Diagnostics.Commands;
using QwkNet.Encoding;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;

namespace QwkNet.Diagnostics.Formatting;

/// <summary>
/// Formats messages as JSON for machine-readable output.
/// </summary>
internal sealed class JsonMessageFormatter : IMessageFormatter
{
  private readonly bool _showKludges;
  private readonly bool _showCp437;

  public JsonMessageFormatter(bool showKludges, bool showCp437)
  {
    _showKludges = showKludges;
    _showCp437 = showCp437;
  }

  public string Format(List<MessageView> messages, QwkPacket packet)
  {
    List<object> messageObjects = new List<object>();

    foreach (MessageView msgView in messages)
    {
      messageObjects.Add(FormatMessage(msgView, packet));
    }

    // If only one message, return it directly; otherwise return array
    object result = messageObjects.Count == 1 ? messageObjects[0] : messageObjects;

    JsonSerializerOptions options = new JsonSerializerOptions
    {
      WriteIndented = true,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    return JsonSerializer.Serialize(result, options);
  }

  private object FormatMessage(MessageView msgView, QwkPacket packet)
  {
    Message message = msgView.Message;
    
    // Build header object
    Dictionary<string, object> header = new Dictionary<string, object>
    {
      ["from"] = message.From,
      ["to"] = message.To,
      ["subject"] = message.Subject,
      ["date"] = message.DateTime.HasValue 
        ? message.DateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss")
        : null!,
      ["conference"] = new
      {
        number = message.ConferenceNumber,
        name = GetConferenceName(packet, message.ConferenceNumber)
      },
      ["status"] = new
      {
        isPrivate = message.IsPrivate,
        isRead = message.IsRead,
        isDeleted = message.IsDeleted
      },
      ["messageNumber"] = message.MessageNumber,
      ["referenceNumber"] = message.ReferenceNumber,
      ["blockCount"] = message.RawHeader.BlockCount
    };

    // Build body object
    Dictionary<string, object> body = new Dictionary<string, object>
    {
      ["lines"] = message.Body.Lines.ToList(),
      ["characterCount"] = message.Body.RawText.TrimEnd(' ', '\0').Length,
      ["lineCount"] = message.Body.Lines.Count
    };

    // CP437 analysis
    Cp437Analysis analysis = AnalyseBodyContent(message.Body.RawText);
    Dictionary<string, object> cp437Analysis = new Dictionary<string, object>
    {
      ["lineTerminators"] = analysis.LineTerminatorCount,
      ["boxDrawingChars"] = analysis.BoxDrawingCount,
      ["internationalChars"] = analysis.InternationalCount,
      ["ansiEscapeSequences"] = analysis.AnsiEscapeCount
    };

    // Validation
    Dictionary<string, object> validation = BuildValidationObject(message);

    // Build final message object
    Dictionary<string, object> messageObj = new Dictionary<string, object>
    {
      ["messageNumber"] = msgView.DisplayNumber,
      ["totalMessages"] = msgView.TotalMessages,
      ["header"] = header,
      ["body"] = body,
      ["cp437Analysis"] = cp437Analysis,
      ["validation"] = validation
    };

    // Add kludges if requested and present
    if (_showKludges && message.Kludges.Count > 0)
    {
      Dictionary<string, string> kludges = new Dictionary<string, string>();
      foreach (MessageKludge kludge in message.Kludges)
      {
        kludges[kludge.Key] = kludge.Value;
      }
      messageObj["kludges"] = kludges;
    }

    return messageObj;
  }

  private Dictionary<string, object> BuildValidationObject(Message message)
  {
    int blockCount = message.RawHeader.BlockCount;
    int expectedBodyBlocks = Math.Max(0, blockCount - 1);
    int actualBodyBytes = message.Body.RawText.Length;
    int expectedBodyBytes = expectedBodyBlocks * 128;

    List<object> bodyBlocks = new List<object>();
    int bytesProcessed = 0;

    for (int i = 1; i <= expectedBodyBlocks; i++)
    {
      int blockSize = Math.Min(128, actualBodyBytes - bytesProcessed);
      bytesProcessed += blockSize;

      bodyBlocks.Add(new
      {
        block = i,
        complete = blockSize == 128,
        size = blockSize,
        expected = 128
      });
    }

    return new Dictionary<string, object>
    {
      ["headerComplete"] = true,
      ["bodyBlocks"] = bodyBlocks,
      ["isComplete"] = actualBodyBytes >= expectedBodyBytes
    };
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