using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using QwkNet.Encoding;
using QwkNet.Models.Messages;
using TextEncoding = System.Text.Encoding;

namespace QwkNet.Tests.Models.Messages;

/// <summary>
/// Tests for <see cref="MessageBody.GetText"/> covering both the null-encoding (legacy)
/// path and the raw-bytes path introduced in v1.7.0.
/// </summary>
public sealed class MessageBodyGetTextTests
{
  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  /// <summary>
  /// Builds a MessageBody that has live RawBytes: classic QWK format using CP437.
  /// Each line is terminated with 0xE3; the buffer is padded to a 128-byte boundary.
  /// </summary>
  private static MessageBody BuildCp437Body(params string[] lines)
  {
    System.Text.Encoding cp437 = Cp437Encoding.GetEncoding();
    List<byte> bytes = new List<byte>();

    foreach (string line in lines)
    {
      bytes.AddRange(cp437.GetBytes(line));
      bytes.Add(0xE3); // QWK line terminator
    }

    // Pad to 128-byte boundary
    int rem = bytes.Count % 128;
    if (rem != 0)
    {
      for (int i = 0; i < 128 - rem; i++)
      {
        bytes.Add(0x20); // space padding
      }
    }

    byte[] rawBytes = bytes.ToArray();

    // Build the RawText the same way QwkPacket does (CP437-decode the blocks)
    string rawText = cp437.GetString(rawBytes);

    // Build Lines (same as MessageBodyParser: split on 0xE3 and trim trailing padding)
    List<string> parsedLines = new List<string>(lines);

    return new MessageBody(parsedLines, rawText, rawBytes);
  }

  /// <summary>
  /// Builds a MessageBody whose RawBytes contain UTF-8-encoded text with 0xE3 line breaks.
  /// RawText is set to a sentinel (callers should not rely on it for the raw-bytes path).
  /// </summary>
  private static MessageBody BuildUtf8Body(params string[] lines)
  {
    System.Text.Encoding utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    List<byte> bytes = new List<byte>();

    foreach (string line in lines)
    {
      bytes.AddRange(utf8.GetBytes(line));
      bytes.Add(0xE3); // QWK line terminator — still byte 0xE3 regardless of encoding
    }

    // Pad to 128-byte boundary
    int rem = bytes.Count % 128;
    if (rem != 0)
    {
      for (int i = 0; i < 128 - rem; i++)
      {
        bytes.Add(0x20);
      }
    }

    byte[] rawBytes = bytes.ToArray();

    // Lines is intentionally set to empty: we are testing the raw-bytes path
    return new MessageBody(Array.Empty<string>(), string.Empty, rawBytes);
  }

  // -------------------------------------------------------------------------
  // Null-encoding (legacy) path — tests 1–3
  // -------------------------------------------------------------------------

  [Fact]
  public void GetText_NullEncoding_Preserve_ReturnsSameAsConvertFromQwkFormat()
  {
    // The null-encoding / Preserve path delegates entirely to
    // LineEndingProcessor.ConvertFromQwkFormat(RawText, Preserve).
    // For CP437-decoded bodies (real QWK), ConvertFromQwkFormat is effectively
    // a no-op on the π characters (it looks for ã/U+00E3, not π/U+03C0),
    // so the result is equal to RawText.
    List<string> lines = new List<string> { "Hello", "World" };
    string rawText = "Hello\u03C0World\u03C0"; // π = CP437 decode of byte 0xE3
    MessageBody body = new MessageBody(lines, rawText);

    string result = body.GetText(LineEndingMode.Preserve, encoding: null);

    // ConvertFromQwkFormat looks for ã (U+00E3), which is not in this CP437-decoded text,
    // so the replacement is a no-op and the result equals RawText.
    Assert.Equal(rawText, result);
  }

  [Fact]
  public void GetText_NullEncoding_NormaliseToLf_ConvertsCrLfToLf()
  {
    // When RawText contains CRLF sequences (e.g. QWKE-style inline newlines),
    // NormaliseToLf should convert them to LF.
    List<string> lines = new List<string> { "Line A", "Line B" };
    string rawText = "Line A\r\nLine B";
    MessageBody body = new MessageBody(lines, rawText);

    string result = body.GetText(LineEndingMode.NormaliseToLf, encoding: null);

    Assert.Equal("Line A\nLine B", result);
    Assert.DoesNotContain("\r\n", result);
  }

  [Fact]
  public void GetText_NullEncoding_NormaliseToCrLf_ConvertsLfToCrLf()
  {
    // When RawText contains bare LF sequences, NormaliseToCrLf should convert them to CRLF.
    List<string> lines = new List<string> { "Line A", "Line B" };
    string rawText = "Line A\nLine B";
    MessageBody body = new MessageBody(lines, rawText);

    string result = body.GetText(LineEndingMode.NormaliseToCrLf, encoding: null);

    Assert.Equal("Line A\r\nLine B", result);
  }

  // -------------------------------------------------------------------------
  // Raw-bytes path — CP437 tests (4a, 4b)
  // -------------------------------------------------------------------------

  [Fact]
  public void GetText_WithCp437Encoding_Preserve_MatchesGetDecodedText()
  {
    // CP437 body with a high-byte character: 0x84 = ä in CP437
    System.Text.Encoding cp437 = Cp437Encoding.GetEncoding();
    string lineWithHighByte = "Caf\u00e9"; // é is 0x82 in CP437

    // Build raw bytes manually so we control the encoding
    List<byte> bytes = new List<byte>();
    bytes.AddRange(cp437.GetBytes(lineWithHighByte));
    bytes.Add(0xE3);
    bytes.AddRange(cp437.GetBytes("plain line"));
    bytes.Add(0xE3);
    // Pad to 128 bytes
    while (bytes.Count < 128) bytes.Add(0x20);

    byte[] rawBytes = bytes.ToArray();
    string rawText = cp437.GetString(rawBytes);
    List<string> parsedLines = new List<string> { lineWithHighByte, "plain line" };
    MessageBody body = new MessageBody(parsedLines, rawText, rawBytes);

    string viaGetText = body.GetText(LineEndingMode.Preserve, cp437);

    // Should equal GetDecodedText() because both use the same CP437 encoding
    Assert.Equal(body.GetDecodedText(), viaGetText);
  }

  [Fact]
  public void GetText_WithCp437Encoding_Preserve_LinesJoinedByEnvironmentNewLine()
  {
    MessageBody body = BuildCp437Body("First", "Second", "Third");

    string result = body.GetText(LineEndingMode.Preserve, Cp437Encoding.GetEncoding());

    string expected = string.Join(Environment.NewLine, new[] { "First", "Second", "Third" });
    Assert.Equal(expected, result);
  }

  // -------------------------------------------------------------------------
  // Raw-bytes path — UTF-8 tests (5–7)
  // -------------------------------------------------------------------------

  [Fact]
  public void GetText_WithUtf8Encoding_DecodesMultiByteCharacters()
  {
    // 🌍 is U+1F30D, encoded in UTF-8 as F0 9F 8C 8D (4 bytes)
    MessageBody body = BuildUtf8Body("Hello \U0001F30D", "World");

    string result = body.GetText(LineEndingMode.Preserve, TextEncoding.UTF8);

    string[] resultLines = result.Split(Environment.NewLine);
    Assert.Equal(2, resultLines.Length);
    Assert.Equal("Hello \U0001F30D", resultLines[0]);
    Assert.Equal("World", resultLines[1]);
  }

  [Fact]
  public void GetText_WithUtf8StrictFallback_ThrowsOnInvalidBytes()
  {
    // Build a body containing byte 0xFF, which is invalid UTF-8
    byte[] rawBytes = new byte[128];
    rawBytes[0] = (byte)'H';
    rawBytes[1] = 0xFF; // invalid UTF-8
    rawBytes[2] = 0xE3; // line terminator
    // rest is 0x00 (padding)

    MessageBody body = new MessageBody(Array.Empty<string>(), string.Empty, rawBytes);

    Assert.Throws<DecoderFallbackException>(
      () => body.GetText(LineEndingMode.Preserve, TextEncoding.UTF8, DecoderFallbackPolicy.Strict));
  }

  [Fact]
  public void GetText_WithUtf8ReplacementQuestion_ReplacesInvalidBytes()
  {
    // Build a body containing known-invalid UTF-8 byte 0xFF
    byte[] rawBytes = new byte[128];
    rawBytes[0] = (byte)'A';
    rawBytes[1] = 0xFF; // invalid UTF-8
    rawBytes[2] = 0xE3; // line terminator
    for (int i = 3; i < 128; i++) rawBytes[i] = 0x20;

    MessageBody body = new MessageBody(Array.Empty<string>(), string.Empty, rawBytes);

    string result = body.GetText(
      LineEndingMode.Preserve, TextEncoding.UTF8, DecoderFallbackPolicy.ReplacementQuestion);

    // The invalid byte should be replaced with '?'
    Assert.Contains("?", result);
    // The 'A' byte should still decode correctly
    Assert.Contains("A", result);
  }

  // -------------------------------------------------------------------------
  // Raw-bytes path — edge cases (8–10)
  // -------------------------------------------------------------------------

  [Fact]
  public void GetText_WithNonNullEncoding_WhenRawBytesEmpty_ThrowsInvalidOperationException()
  {
    // Bodies created via FromRawText or the 2-arg constructor have RawBytes = Empty
    MessageBody body = MessageBody.FromRawText("Test line\u03C0");

    InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
      () => body.GetText(LineEndingMode.Preserve, TextEncoding.UTF8));

    Assert.Contains("RawBytes is empty", ex.Message);
    Assert.Contains("QwkPacket.Open()", ex.Message);
  }

  [Fact]
  public void GetText_WithQwkeFormatBytes_SplitsOnCr()
  {
    // QWKE format: lines end with CR (0x0D) instead of 0xE3
    System.Text.Encoding latin1 = System.Text.Encoding.GetEncoding("iso-8859-1");
    List<byte> bytes = new List<byte>();
    bytes.AddRange(latin1.GetBytes("Line One"));
    bytes.Add(0x0D); // CR terminator (QWKE)
    bytes.AddRange(latin1.GetBytes("Line Two"));
    bytes.Add(0x0D);
    while (bytes.Count < 128) bytes.Add(0x20);

    byte[] rawBytes = bytes.ToArray();
    MessageBody body = new MessageBody(Array.Empty<string>(), string.Empty, rawBytes);

    string result = body.GetText(LineEndingMode.Preserve, latin1);

    string[] resultLines = result.Split(Environment.NewLine);
    Assert.Equal(2, resultLines.Length);
    Assert.Equal("Line One", resultLines[0]);
    Assert.Equal("Line Two", resultLines[1]);
  }

  [Fact]
  public void GetText_DifferentLineEndingModes_ProduceCorrectSeparators()
  {
    MessageBody body = BuildUtf8Body("Alpha", "Beta", "Gamma");

    string lf   = body.GetText(LineEndingMode.NormaliseToLf,   TextEncoding.UTF8);
    string crlf = body.GetText(LineEndingMode.NormaliseToCrLf, TextEncoding.UTF8);

    Assert.Equal("Alpha\nBeta\nGamma", lf);
    Assert.Equal("Alpha\r\nBeta\r\nGamma", crlf);
  }
}
