using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using QwkNet;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Tests;

/// <summary>
/// Tests for kludge extraction from QWK and QWKE message bodies.
/// </summary>
/// <remarks>
/// <para>
/// <c>ExtractKludges</c> is a private static method on <c>QwkPacket</c> and is
/// exercised here through <c>QwkPacket.Open(Stream)</c> using minimal synthetic
/// in-memory ZIP packets.  Each packet contains one CONTROL.DAT and one
/// MESSAGES.DAT whose body content is tailored to the scenario under test.
/// </para>
/// <para>
/// Kludge conventions under test:
/// </para>
/// <list type="bullet">
/// <item>QWKE extended headers — <c>To:</c>, <c>From:</c>, <c>Subject:</c></item>
/// <item>Synchronet <c>@</c>-kludges — e.g. <c>@MSGID:</c>, <c>@VIA:</c>, <c>@TZ:</c></item>
/// </list>
/// <para>
/// FidoNet SOH kludges (byte <c>0x01</c> prefix) are documented separately.
/// CP437 decoding maps byte <c>0x01</c> to U+263A (☺), not U+0001, so the raw-byte
/// convention cannot be detected after the CP437 pipeline.
/// </para>
/// <para>
/// The primary regression case is Synchronet reply-attribution lines
/// (<c>Re:</c> / <c>By:</c>) being incorrectly classified as kludges by the
/// previous structural heuristic.
/// </para>
/// </remarks>
public sealed class ExtractKludgesTests
{
  // ──────────────────────────────────────────────────────────────────────────
  // QWKE extended headers
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_QwkeToHeader_IsExtractedAsKludge()
  {
    // Arrange – body begins with a QWKE "To:" kludge
    List<string> bodyLines = new List<string>
    {
      "To: A Very Long Recipient Name That Exceeds Twenty Five Chars",
      "Hello, this is the message body.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // Assert – kludge captured, body clean
    Assert.True(message.Kludges.ContainsKey("To"));
    MessageKludge kludge = Assert.Single(message.Kludges.GetByKey("To"));
    Assert.Equal("A Very Long Recipient Name That Exceeds Twenty Five Chars", kludge.Value);

    Assert.DoesNotContain(message.Body.Lines, l => l.StartsWith("To:"));
    Assert.Contains(message.Body.Lines, l => l.Contains("Hello"));
  }

  [Fact]
  public void ExtractKludges_QwkeFromHeader_IsExtractedAsKludge()
  {
    // Arrange
    List<string> bodyLines = new List<string>
    {
      "From: Somebody With A Very Long Name Indeed",
      "Message body line.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.True(message.Kludges.ContainsKey("From"));
    Assert.DoesNotContain(message.Body.Lines, l => l.StartsWith("From:"));
  }

  [Fact]
  public void ExtractKludges_QwkeSubjectHeader_IsExtractedAsKludge()
  {
    // Arrange
    List<string> bodyLines = new List<string>
    {
      "Subject: The American Connection BBS - Now With Extended Subject Support",
      "Message body line.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.True(message.Kludges.ContainsKey("Subject"));
    Assert.DoesNotContain(message.Body.Lines, l => l.StartsWith("Subject:"));
  }

  [Fact]
  public void ExtractKludges_AllThreeQwkeHeaders_AllExtracted()
  {
    // Arrange – all three QWKE extended headers present together
    List<string> bodyLines = new List<string>
    {
      "To: Extended Recipient Name Here",
      "From: Extended Sender Name Here",
      "Subject: Extended Subject Line Here",
      "Body of the message.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.Equal(3, message.Kludges.Count);
    Assert.True(message.Kludges.ContainsKey("To"));
    Assert.True(message.Kludges.ContainsKey("From"));
    Assert.True(message.Kludges.ContainsKey("Subject"));

    // Body must not contain any of the kludge lines
    foreach (string line in message.Body.Lines)
    {
      Assert.False(line.StartsWith("To:") || line.StartsWith("From:") || line.StartsWith("Subject:"),
        $"Kludge line leaked into body: '{line}'");
    }
  }

  [Fact]
  public void ExtractKludges_QwkeHeaderKeyIsCaseInsensitive()
  {
    // Arrange – lowercase variant; spec requires case-insensitive matching
    List<string> bodyLines = new List<string>
    {
      "subject: Lower Case Subject Key",
      "Body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.True(message.Kludges.ContainsKey("subject"));
    Assert.True(message.Kludges.ContainsKey("Subject")); // collection lookup is also case-insensitive
    Assert.DoesNotContain(message.Body.Lines, l => l.StartsWith("subject:"));
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Synchronet @-kludges
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_SynchronetAtKludge_IsExtractedAsKludge()
  {
    // Arrange – single @-kludge as written by Synchronet
    List<string> bodyLines = new List<string>
    {
      "@VIA: VERT",
      "Body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.True(message.Kludges.ContainsKey("@VIA"));
    MessageKludge kludge = Assert.Single(message.Kludges.GetByKey("@VIA"));
    Assert.Equal("VERT", kludge.Value);
    Assert.DoesNotContain(message.Body.Lines, l => l.StartsWith("@VIA:"));
  }

  [Fact]
  public void ExtractKludges_MultipleSynchronetAtKludges_AllExtracted()
  {
    // Arrange – realistic Synchronet QWKE header block
    List<string> bodyLines = new List<string>
    {
      "@VIA: VERT",
      "@MSGID: <699631DD.19004.dove-ads@vertrauen.synchro.net>",
      "@REPLY: <6996025D.133.dove-ads@vertrauen.synchro.net>",
      "@TZ: 41e0",
      "Body text starts here.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.Equal(4, message.Kludges.Count);
    Assert.True(message.Kludges.ContainsKey("@VIA"));
    Assert.True(message.Kludges.ContainsKey("@MSGID"));
    Assert.True(message.Kludges.ContainsKey("@REPLY"));
    Assert.True(message.Kludges.ContainsKey("@TZ"));

    Assert.Equal("41e0", message.Kludges.GetFirstByKey("@TZ")!.Value);

    // Body must be clean
    foreach (string line in message.Body.Lines)
    {
      Assert.False(line.StartsWith("@"), $"@-kludge leaked into body: '{line}'");
    }
  }

  [Fact]
  public void ExtractKludges_SynchronetAtKludge_KeyIncludesAtSign()
  {
    // Arrange – verify the @ is part of the stored key, not stripped
    List<string> bodyLines = new List<string>
    {
      "@MSGID: <abc123.msg@bbs.example.com>",
      "Body.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // Must be retrievable as "@MSGID", not "MSGID"
    Assert.True(message.Kludges.ContainsKey("@MSGID"));
    Assert.False(message.Kludges.ContainsKey("MSGID"));
  }

  [Fact]
  public void ExtractKludges_AtSignAloneBeforeColon_IsNotKludge()
  {
    // Arrange – "@:" with nothing between @ and colon is not a valid @-kludge
    List<string> bodyLines = new List<string>
    {
      "@: this is not a valid kludge",
      "Body.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // Nothing extracted; the malformed line stays in body
    Assert.Empty(message.Kludges);
    Assert.Contains(message.Body.Lines, l => l.Contains("this is not a valid kludge"));
  }

  // ──────────────────────────────────────────────────────────────────────────
  // FidoNet SOH kludges — CP437 encoding note
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_Byte0x01_DecodesAsCp437Glyph_NotSohControl()
  {
    // CP437 maps byte 0x01 to U+263A (☺ WHITE SMILING FACE), not U+0001 (SOH).
    // A line beginning with byte 0x01 in a QWK body therefore arrives at
    // ExtractKludges as a line beginning with '☺', not '\x01'.
    //
    // This test documents that fact and verifies the line is treated as body
    // text (not a kludge), which is the correct behaviour given the current
    // CP437 pipeline.  Supporting FidoNet SOH kludges properly would require
    // inspecting the raw byte stream before CP437 decoding.

    // Arrange — write byte 0x01 directly into the body block.
    // BuildMessagesData writes (byte)ch, so '\x01' → 0x01 in the stream.
    // After CP437 decode it becomes '☺' (U+263A).
    List<string> bodyLines = new List<string>
    {
      "\x01MSGID: 2:280/464 61234567",
      "Body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // No kludge extracted — the '☺'-prefixed line is not recognised as any convention.
    Assert.Empty(message.Kludges);

    // The line survives in the body (as a CP437-decoded string starting with ☺).
    Assert.NotEmpty(message.Body.Lines);
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Mixed conventions
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_QwkeAndAtKludgesTogether_AllExtracted()
  {
    // Arrange – realistic Synchronet QWKE packet: QWKE header followed by @-kludges
    List<string> bodyLines = new List<string>
    {
      "Subject: The American Connection BBS",
      "@VIA: VERT",
      "@MSGID: <699631DD.19004.dove-ads@vertrauen.synchro.net>",
      "@REPLY: <6996025D.133.dove-ads@vertrauen.synchro.net>",
      "@TZ: 41e0",
      "Re: The American Connection BBS",
      "By: Misgretired to All on Wed Feb 18 14:22:00 2026",
      ">  Back online after a hardware crash.",
      "Sprechen sie Deutch?",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // Five kludges total
    Assert.Equal(5, message.Kludges.Count);
    Assert.True(message.Kludges.ContainsKey("Subject"));
    Assert.True(message.Kludges.ContainsKey("@VIA"));
    Assert.True(message.Kludges.ContainsKey("@MSGID"));
    Assert.True(message.Kludges.ContainsKey("@REPLY"));
    Assert.True(message.Kludges.ContainsKey("@TZ"));

    // Re: and By: must remain in the body, not be treated as kludges
    Assert.Contains(message.Body.Lines, l => l.StartsWith("Re:"));
    Assert.Contains(message.Body.Lines, l => l.StartsWith("By:"));

    // Quoted text and body must also survive
    Assert.Contains(message.Body.Lines, l => l.Contains("Back online"));
    Assert.Contains(message.Body.Lines, l => l.Contains("Sprechen"));
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Regression: Re: / By: false-positive (primary bug)
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_ReplyAttributionLines_AreNotKludges()
  {
    // Arrange – no kludges at all; body begins directly with Re:/By: attribution
    List<string> bodyLines = new List<string>
    {
      "Re: Some Subject",
      "By: Original Author to All on Mon Jan 01 12:00:00 2024",
      "> Quoted line from original message.",
      "Reply body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // No kludges extracted
    Assert.Empty(message.Kludges);

    // Every line must remain in the body
    Assert.Contains(message.Body.Lines, l => l.StartsWith("Re:"));
    Assert.Contains(message.Body.Lines, l => l.StartsWith("By:"));
    Assert.Contains(message.Body.Lines, l => l.Contains("Quoted line"));
    Assert.Contains(message.Body.Lines, l => l.Contains("Reply body text."));
  }

  [Fact]
  public void ExtractKludges_ReplyAttributionAfterAtKludges_BodyContainsAttribution()
  {
    // Arrange – @-kludges followed immediately by Re:/By: (no blank line separator)
    // This is the exact structure observed in real Synchronet QWKE packets.
    List<string> bodyLines = new List<string>
    {
      "@VIA: VERT",
      "@MSGID: <abc.123@bbs.test>",
      "Re: Test Subject",
      "By: Original Author",
      "> Quoted.",
      "New text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // Only the two @-kludges extracted
    Assert.Equal(2, message.Kludges.Count);
    Assert.False(message.Kludges.ContainsKey("Re"));
    Assert.False(message.Kludges.ContainsKey("By"));

    // Attribution lines must be in body
    Assert.Contains(message.Body.Lines, l => l.StartsWith("Re:"));
    Assert.Contains(message.Body.Lines, l => l.StartsWith("By:"));
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Blank line terminator behaviour
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_BlankLineSeparator_IsConsumedAndNotInBody()
  {
    // Arrange – QWKE spec mandates a blank line between kludges and body
    List<string> bodyLines = new List<string>
    {
      "Subject: Extended Subject Here",
      "", // blank separator
      "Body text after blank line.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.True(message.Kludges.ContainsKey("Subject"));

    // The blank separator line must be consumed — body must not begin with a blank line.
    // (If more than one blank line existed we would preserve later ones; this packet has only one.)
    Assert.DoesNotContain(message.Body.Lines, l => string.IsNullOrEmpty(l));

    // Body text must survive
    Assert.Contains(message.Body.Lines, l => l.Contains("Body text after blank line."));
  }

  [Fact]
  public void ExtractKludges_BlankLineWithoutKludges_IsPreservedInBody()
  {
    // A blank line before any kludge has been found is ordinary body content.
    // It must not be consumed — only the QWKE separator blank line (which
    // follows at least one kludge) should be removed.

    // Arrange
    List<string> bodyLines = new List<string>
    {
      "",
      "Body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // Nothing extracted as a kludge.
    Assert.Empty(message.Kludges);

    // The leading blank line must survive in the body unchanged.
    Assert.Contains(message.Body.Lines, l => string.IsNullOrEmpty(l));
    Assert.Contains(message.Body.Lines, l => l.Contains("Body text."));
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Edge cases
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void ExtractKludges_EmptyBody_ReturnsNoKludges()
  {
    // Arrange
    List<string> bodyLines = new List<string>();

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.Empty(message.Kludges);
    Assert.Empty(message.Body.Lines);
  }

  [Fact]
  public void ExtractKludges_NonKludgeLineAtTop_BodyFullyPreserved()
  {
    // Arrange – plain body with no kludges; scanner must stop on first line
    List<string> bodyLines = new List<string>
    {
      "Hello there, this is a normal message.",
      "Second line.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.Empty(message.Kludges);
    Assert.Equal(2, message.Body.Lines.Count);
    Assert.Equal("Hello there, this is a normal message.", message.Body.Lines[0]);
    Assert.Equal("Second line.", message.Body.Lines[1]);
  }

  [Fact]
  public void ExtractKludges_UnknownWordWithColon_IsNotKludge()
  {
    // Arrange – "Warning: ..." looks like a kludge structurally but is not a known key
    List<string> bodyLines = new List<string>
    {
      "Warning: this system will be down for maintenance.",
      "Body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    // "Warning" is not a QWKE header key, not @-prefixed, not SOH-prefixed
    Assert.Empty(message.Kludges);
    Assert.Contains(message.Body.Lines, l => l.Contains("Warning:"));
  }

  [Fact]
  public void ExtractKludges_UrlInBody_IsNotKludge()
  {
    // Arrange – URLs contain colons and would confuse the old heuristic
    List<string> bodyLines = new List<string>
    {
      "Visit https://example.com for more information.",
      "Body text.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.Empty(message.Kludges);
    Assert.Contains(message.Body.Lines, l => l.Contains("https://"));
  }

  [Fact]
  public void ExtractKludges_KludgeValueIsEmpty_ExtractedCorrectly()
  {
    // Arrange – @-kludge with empty value
    List<string> bodyLines = new List<string>
    {
      "@FLAGS:",
      "Body.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    Assert.True(message.Kludges.ContainsKey("@FLAGS"));
    Assert.Equal(string.Empty, message.Kludges.GetFirstByKey("@FLAGS")!.Value);
  }

  [Fact]
  public void ExtractKludges_RawLinePreserved()
  {
    // Arrange – verify RawLine is the exact original line
    List<string> bodyLines = new List<string>
    {
      "@MSGID: <exact.raw.line@test.net>",
      "Body.",
    };

    using QwkPacket packet = OpenPacketWithBody(bodyLines);
    Message message = Assert.Single(packet.Messages);

    MessageKludge kludge = Assert.Single(message.Kludges.GetByKey("@MSGID"));
    Assert.Equal("@MSGID: <exact.raw.line@test.net>", kludge.RawLine);
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Synthetic packet builder
  // ──────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Builds a minimal in-memory QWK packet containing a single message whose
  /// body is the given sequence of lines, then opens and returns the parsed
  /// <see cref="QwkPacket"/>.
  /// </summary>
  /// <remarks>
  /// The packet structure is:
  /// <list type="bullet">
  /// <item>CONTROL.DAT — minimal BBS metadata with one conference (0 = Main).</item>
  /// <item>MESSAGES.DAT — copyright block + one message header + body blocks
  /// encoding the supplied lines using 0xE3 as the QWK line terminator.</item>
  /// </list>
  /// The caller is responsible for disposing the returned packet.
  /// </remarks>
  private static QwkPacket OpenPacketWithBody(List<string> bodyLines)
  {
    byte[] messagesData = BuildMessagesData(bodyLines);
    byte[] controlData = BuildControlDat();

    MemoryStream zipStream = new MemoryStream();
    using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
    {
      ZipArchiveEntry controlEntry = zip.CreateEntry("CONTROL.DAT");
      using (Stream entryStream = controlEntry.Open())
      {
        entryStream.Write(controlData, 0, controlData.Length);
      }

      ZipArchiveEntry messagesEntry = zip.CreateEntry("MESSAGES.DAT");
      using (Stream entryStream = messagesEntry.Open())
      {
        entryStream.Write(messagesData, 0, messagesData.Length);
      }
    }

    zipStream.Position = 0;
    return QwkPacket.Open(zipStream, ValidationMode.Lenient);
  }

  /// <summary>
  /// Builds a minimal CONTROL.DAT byte array recognised by <see cref="ControlDatParser"/>.
  /// </summary>
  private static byte[] BuildControlDat()
  {
    // Minimum required lines for ControlDatParser (index 0-10 are mandatory):
    // 0  BBS name
    // 1  City/State
    // 2  Phone number
    // 3  SysOp name
    // 4  Serial/BBS ID
    // 5  Creation date/time
    // 6  User name
    // 7  Blank (unused)
    // 8  Highest message number on BBS
    // 9  Number of conferences - 1
    // 10 First conference number (repeated per conference)
    // Subsequent pairs: conference number, conference name
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("Test BBS");
    sb.AppendLine("Test City, TS");
    sb.AppendLine("555-0100");
    sb.AppendLine("Test SysOp");
    sb.AppendLine("12345,TESTBBS");
    sb.AppendLine("01-01-25,00:00:00");
    sb.AppendLine("TEST USER");
    sb.AppendLine("");
    sb.AppendLine("1");
    sb.AppendLine("0");  // 0 additional conferences (Main only)
    sb.AppendLine("0");  // Conference 0
    sb.AppendLine("Main"); // Conference 0 name

    return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
  }

  /// <summary>
  /// Builds a minimal MESSAGES.DAT byte array with one message whose body
  /// encodes the given lines using 0xE3 QWK terminators.
  /// </summary>
  private static byte[] BuildMessagesData(List<string> bodyLines)
  {
    // Encode body lines into 128-byte blocks with 0xE3 terminators.
    // Use ASCII for plain characters; 0x01 SOH passes through unchanged
    // because we write raw bytes, which is what the parser will see.
    List<byte> bodyBytes = new List<byte>();
    foreach (string line in bodyLines)
    {
      foreach (char ch in line)
      {
        bodyBytes.Add((byte)ch);
      }
      bodyBytes.Add(0xE3); // QWK line terminator
    }

    // Pad body to a whole number of 128-byte blocks.
    int bodyBlockCount = (bodyBytes.Count + 127) / 128;
    if (bodyBlockCount == 0)
    {
      bodyBlockCount = 1;
    }
    while (bodyBytes.Count < bodyBlockCount * 128)
    {
      bodyBytes.Add((byte)' ');
    }

    // Total block count stored in header = header block (1) + body blocks.
    int totalBlocks = 1 + bodyBlockCount;

    // Build the 128-byte message header.
    byte[] header = new byte[128];
    Array.Fill(header, (byte)' ');

    // Offset 0: status byte — ' ' = public, unread
    header[0] = (byte)' ';

    // Offset 1–7: message number, 7 ASCII digits, right-aligned
    byte[] msgNum = System.Text.Encoding.ASCII.GetBytes("      1");
    Array.Copy(msgNum, 0, header, 1, 7);

    // Offset 8–15: date "01-01-25"
    byte[] date = System.Text.Encoding.ASCII.GetBytes("01-01-25");
    Array.Copy(date, 0, header, 8, 8);

    // Offset 16–20: time "00:00"
    byte[] time = System.Text.Encoding.ASCII.GetBytes("00:00");
    Array.Copy(time, 0, header, 16, 5);

    // Offset 21–45: To (25 bytes)
    byte[] to = System.Text.Encoding.ASCII.GetBytes("TEST USER                ");
    Array.Copy(to, 0, header, 21, 25);

    // Offset 46–70: From (25 bytes)
    byte[] from = System.Text.Encoding.ASCII.GetBytes("TEST SENDER              ");
    Array.Copy(from, 0, header, 46, 25);

    // Offset 71–95: Subject (25 bytes)
    byte[] subject = System.Text.Encoding.ASCII.GetBytes("Test Subject             ");
    Array.Copy(subject, 0, header, 71, 25);

    // Offset 96–114: Reference password (blank)
    // Already spaces from Array.Fill.

    // Offset 116–121: block count, 6 ASCII digits, right-aligned
    string blockCountStr = totalBlocks.ToString().PadLeft(6);
    byte[] blockCountBytes = System.Text.Encoding.ASCII.GetBytes(blockCountStr);
    Array.Copy(blockCountBytes, 0, header, 116, 6);

    // Offset 122: alive flag (0xE1)
    header[122] = 0xE1;

    // Offset 123–124: conference number (ushort LE) = 0
    header[123] = 0;
    header[124] = 0;

    // Assemble: 128-byte copyright block + header + body blocks
    List<byte> messagesData = new List<byte>();

    // Copyright/filler block (128 bytes of spaces)
    byte[] copyright = new byte[128];
    Array.Fill(copyright, (byte)' ');
    messagesData.AddRange(copyright);

    messagesData.AddRange(header);
    messagesData.AddRange(bodyBytes);

    return messagesData.ToArray();
  }
}