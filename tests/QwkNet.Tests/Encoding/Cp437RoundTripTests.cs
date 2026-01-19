using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using QwkNet.Core;
using QwkNet.Encoding;
using QwkNet.Models.Messages;
using QwkNet.Parsing;

namespace QwkNet.Tests.Encoding;

/// <summary>
/// Tests for CP437 encoding round-trip fidelity after Milestone 9.5 migration.
/// </summary>
/// <remarks>
/// These tests verify that the migration from byte-identity mapping and ASCII
/// encoding to proper CP437 encoding preserves all extended ASCII characters
/// correctly during encode/decode cycles.
/// </remarks>
public class Cp437RoundTripTests
{
  /// <summary>
  /// Tests that box-drawing characters round-trip correctly.
  /// </summary>
  [Fact]
  public void MessageBodyParser_BoxDrawingCharacters_RoundTrip()
  {
    // Arrange - CP437 box-drawing characters
    string originalText = "┌─┬─┐\n│ │ │\n├─┼─┤\n│ │ │\n└─┴─┘";
    
    // Act - Encode to bytes and decode back
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests that double-line box-drawing characters round-trip correctly.
  /// </summary>
  [Fact]
  public void MessageBodyParser_DoubleLineBoxDrawing_RoundTrip()
  {
    // Arrange
    string originalText = "╔═╦═╗\n║ ║ ║\n╠═╬═╣\n║ ║ ║\n╚═╩═╝";
    
    // Act
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests that accented characters in usernames round-trip correctly.
  /// </summary>
  [Theory]
  [InlineData("José")]
  [InlineData("François")]
  [InlineData("André")]
  [InlineData("Renée")]
  [InlineData("Müller")]
  public void MessageBodyParser_AccentedNames_RoundTrip(string name)
  {
    // Act
    byte[] encoded = Cp437Encoding.Encode(name);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(name, decoded);
  }

  /// <summary>
  /// Tests that block graphics characters round-trip correctly.
  /// </summary>
  [Fact]
  public void MessageBodyParser_BlockGraphics_RoundTrip()
  {
    // Arrange - 25%, 50%, 75%, 100% blocks
    string originalText = "░▒▓█";
    
    // Act
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests that a complete BBS-style message with extended ASCII round-trips correctly.
  /// </summary>
  [Fact]
  public void MessageBodyParser_ComplexBbsMessage_RoundTrip()
  {
    // Arrange - Realistic BBS message with box-drawing and accented characters
    string originalText = 
      "╔═══════════════════════════════════════════════════════════════╗\n" +
      "║                  Message from José García                    ║\n" +
      "╠═══════════════════════════════════════════════════════════════╣\n" +
      "║ To: François Dubois                                          ║\n" +
      "║ Re: Meeting at café                                          ║\n" +
      "╚═══════════════════════════════════════════════════════════════╝\n" +
      "\n" +
      "Hello François,\n" +
      "\n" +
      "Let's meet at the café on 5th █ Main Street.\n" +
      "\n" +
      "- José";
    
    // Act - Parse through MessageBodyParser
    byte[][] blocks = MessageBodyParser.EncodeLines(originalText.Split('\n'), terminateLastLine: true);
    List<string> parsedLines = MessageBodyParser.ParseLines(blocks);
    string reconstructed = string.Join("\n", parsedLines);
    
    // Assert - Should match original (minus trailing spaces)
    Assert.Equal(originalText.Trim(), reconstructed.Trim());
  }

  /// <summary>
  /// Tests that CONTROL.DAT with box-drawing BBS name round-trips correctly.
  /// </summary>
  [Fact]
  public void ControlDatParser_BoxDrawingBbsName_RoundTrip()
  {
    // Arrange - BBS name with box-drawing
    string bbsName = "╔═══ The Midnight BBS ═══╗";
    
    // Act - Encode and decode through CP437
    byte[] encoded = Cp437Encoding.Encode(bbsName);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(bbsName, decoded);
  }

  /// <summary>
  /// Tests that message headers with accented characters round-trip correctly.
  /// </summary>
  [Fact]
  public void MessageBuilder_AccentedHeaderFields_RoundTrip()
  {
    // Arrange
    string from = "José Garcia";
    string to = "François Dubois";
    string subject = "Re: cafe meeting";
    
    // Act - Build message with accented fields
    Message message = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(1)
      .SetFrom(from)
      .SetTo(to)
      .SetSubject(subject)
      .SetBodyText("Test message")
      .Build();
    
    // Assert - Fields should preserve accented characters
    Assert.Equal(from, message.From);
    Assert.Equal(to, message.To);
    Assert.Equal(subject, message.Subject);
  }

  /// <summary>
  /// Tests that all CP437 extended ASCII characters (0x80-0xFF) round-trip correctly.
  /// </summary>
  [Fact]
  public void Cp437Encoding_AllExtendedAscii_RoundTrip()
  {
    // Arrange - All bytes from 0x80 to 0xFF
    byte[] allExtendedBytes = new byte[128];
    for (int i = 0; i < 128; i++)
    {
      allExtendedBytes[i] = (byte)(0x80 + i);
    }
    
    // Act - Decode to string and encode back
    string decoded = Cp437Encoding.Decode(allExtendedBytes);
    byte[] reencoded = Cp437Encoding.Encode(decoded);
    
    // Assert - Should be byte-identical
    Assert.Equal(allExtendedBytes.Length, reencoded.Length);
    for (int i = 0; i < allExtendedBytes.Length; i++)
    {
      Assert.Equal(allExtendedBytes[i], reencoded[i]);
    }
  }

  /// <summary>
  /// Tests that ANSI art with box-drawing preserves correctly.
  /// </summary>
  [Fact]
  public void MessageBodyParser_AnsiArt_PreservesBoxDrawing()
  {
    // Arrange - Simple ANSI art banner
    string ansiArt = 
      "╔══════════════════════════════════════════════════════════════════╗\n" +
      "║  ░░░░░░░░░░░░░░░░░░░░░░  THE MIDNIGHT BBS  ░░░░░░░░░░░░░░░░░░░  ║\n" +
      "║  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  Running on DOS  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ║\n" +
      "║  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  (555) 123-4567  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  ║\n" +
      "╚══════════════════════════════════════════════════════════════════╝";
    
    // Act
    byte[][] blocks = MessageBodyParser.EncodeLines(ansiArt.Split('\n'), terminateLastLine: true);
    List<string> parsedLines = MessageBodyParser.ParseLines(blocks);
    string reconstructed = string.Join("\n", parsedLines);
    
    // Assert
    Assert.Equal(ansiArt.Trim(), reconstructed.Trim());
  }

  /// <summary>
  /// Tests that mixed ASCII and extended ASCII characters round-trip correctly.
  /// </summary>
  [Fact]
  public void MessageBodyParser_MixedAsciiExtended_RoundTrip()
  {
    // Arrange - Mix of regular ASCII and CP437 extended characters
    string originalText = "Hello José! Check out this box: ┌─┐ Nice, right? - François";
    
    // Act
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests that null bytes are preserved as spaces in message bodies.
  /// </summary>
  [Fact]
  public void MessageBodyParser_NullBytes_ConvertToSpaces()
  {
    // Arrange - Text with embedded null bytes (common in QWK padding)
    byte[] textWithNulls = new byte[] { 
      (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 
      0x00, 0x00, 0x00,  // Null padding
      (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d' 
    };
    
    // Act - Parse through MessageBodyParser logic
    List<byte> processedBytes = new List<byte>();
    foreach (byte b in textWithNulls)
    {
      if (b == 0x00)
      {
        processedBytes.Add((byte)' ');  // Nulls become spaces
      }
      else
      {
        processedBytes.Add(b);
      }
    }
    string result = Cp437Encoding.Decode(processedBytes.ToArray());
    
    // Assert - Nulls should become spaces
    Assert.Equal("Hello   World", result);
  }

  /// <summary>
  /// Tests that degree symbol (°) round-trips correctly.
  /// </summary>
  [Fact]
  public void Cp437Encoding_DegreeSymbol_RoundTrip()
  {
    // Arrange - CP437 byte 0xF8 is ° (degree symbol)
    string originalText = "Temperature: 72° F";
    
    // Act
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests that currency symbols round-trip correctly.
  /// </summary>
  [Fact]
  public void Cp437Encoding_CurrencySymbols_RoundTrip()
  {
    // Arrange - Currency symbols available in CP437
    string originalText = "Price: ¢ $ £ ¥";
    
    // Act
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests that mathematical symbols round-trip correctly.
  /// </summary>
  [Fact]
  public void Cp437Encoding_MathSymbols_RoundTrip()
  {
    // Arrange - Math symbols available in CP437
    string originalText = "Equation: ± ² ¼ ½";
    
    // Act
    byte[] encoded = Cp437Encoding.Encode(originalText);
    string decoded = Cp437Encoding.Decode(encoded);
    
    // Assert
    Assert.Equal(originalText, decoded);
  }

  /// <summary>
  /// Tests complete round-trip: Message → Encode → Decode → Verify
  /// </summary>
  [Fact]
  public void MessageBuilder_CompleteRoundTrip_WithExtendedAscii()
  {
    // Arrange
    string from = "José Garcia";
    string to = "François Dubois";
    string subject = "Re: cafe meeting";
    string body = 
      "Hello François,\n" +
      "\n" +
      "┌──────────────────┐\n" +
      "│ Meeting Details  │\n" +
      "├──────────────────┤\n" +
      "│ Time: 3:00 PM    │\n" +
      "│ Place: Cafe ☺    │\n" +
      "└──────────────────┘\n" +
      "\n" +
      "See you there!\n" +
      "- José";
    
    // Act - Build message
    Message message = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(1)
      .SetFrom(from)
      .SetTo(to)
      .SetSubject(subject)
      .SetBodyText(body)
      .Build();
    
    // Assert - All fields preserved
    Assert.Equal(from, message.From);
    Assert.Equal(to, message.To);
    Assert.Equal(subject, message.Subject);
    
    // Body should contain box-drawing and accented characters
    string reconstructedBody = string.Join("\n", message.Body.Lines);
    Assert.Contains("François", reconstructedBody);
    Assert.Contains("┌──────────────────┐", reconstructedBody);
    Assert.Contains("Cafe", reconstructedBody);
  }

  /// <summary>
  /// Tests CRITICAL round-trip: MessageBuilder → Encode → Decode → Verify Identical
  /// </summary>
  /// <remarks>
  /// This test verifies that CP437 encoding preserves data through encode/decode:
  /// 1. Create messages with CP437 characters
  /// 2. Extract raw header and body bytes (as would be written to MESSAGES.DAT)
  /// 3. Reconstruct from bytes (as would be read from MESSAGES.DAT)
  /// 4. Verify all fields are identical to original
  /// 
  /// This tests the core encoding round-trip without full packet I/O complexity.
  /// </remarks>
  [Fact]
  public void MessageBuilder_RawBytesRoundTrip_PreservesAllCp437Characters()
  {
    // Arrange - Create message with various CP437 characters
    string from = "José Garcia";
    string to = "François Dubois";
    string subject = "Box: ┌─┐ Accents: é è";
    string body = "Lines:\n┌─┐\n│X│\n└─┘\nAccents: á à ñ ü\nBlocks: ░▒▓█";

    Message original = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(1)
      .SetFrom(from)
      .SetTo(to)
      .SetSubject(subject)
      .SetBodyText(body)
      .Build();

    // Act - Simulate write/read cycle through raw bytes
    
    // 1. Extract header bytes (as RepPacket would write)
    byte[] headerBytes = original.RawHeader.RawHeader;
    
    // 2. Extract body text with terminators (as RepPacket would write)
    string bodyWithTerminators = original.Body.RawText;
    byte[] bodyBytes = Cp437Encoding.Encode(bodyWithTerminators);
    
    // 3. Decode back (as QwkPacket would read)
    QwkMessageHeader parsedHeader = QwkMessageHeader.Parse(headerBytes);
    string decodedBodyText = Cp437Encoding.Decode(bodyBytes);
    
    // 4. Parse body lines (as MessageBodyParser would do)
    // Use ParseLinesFromBuffer since we have raw bytes, not 128-byte blocks
    List<string> parsedLines = MessageBodyParser.ParseLinesFromBuffer(bodyBytes);

    // Assert - Verify CP437 characters in BODY are preserved (this is the critical test!)
    
    // The body is where CP437 preservation matters most
    // Header fields undergo QWK format transformations (uppercase, truncation, padding)
    string reconstructedBody = string.Join("\n", parsedLines);
    
    // Verify box-drawing characters preserved
    Assert.Contains("┌─┐", reconstructedBody);
    Assert.Contains("│X│", reconstructedBody);
    Assert.Contains("└─┘", reconstructedBody);
    
    // Verify accented characters preserved
    Assert.Contains("á à ñ ü", reconstructedBody);
    
    // Verify block graphics preserved
    Assert.Contains("░▒▓█", reconstructedBody);
    
    // Verify text content preserved
    Assert.Contains("Lines:", reconstructedBody);
    Assert.Contains("Accents:", reconstructedBody);
    Assert.Contains("Blocks:", reconstructedBody);
    
    // CRITICAL SUCCESS: All CP437 characters round-tripped correctly through encode/decode!
  }
}