using System;
using Xunit;
using QwkNet.Encoding;

namespace QwkNet.Tests.Diagnostics;

/// <summary>
/// Demonstrates the CP437 0xE3 byte → π (U+03C0) character mapping.
/// This test proves why TextMessageFormatter must check for '\u03C0' not (char)0xE3.
/// </summary>
public sealed class Cp437LineTerminatorMappingTests
{
  [Fact]
  public void Cp437_Byte0xE3_DecodesTo_PiCharacter()
  {
    // Arrange: Create a byte array with 0xE3 (the QWK line terminator byte)
    byte[] bytes = new byte[] { 0xE3 };

    // Act: Decode using CP437 (as QwkPacket does)
    string decoded = Cp437Encoding.Decode(bytes);

    // Assert: Byte 0xE3 becomes π (U+03C0) in Unicode, NOT character 0xE3
    Assert.Single(decoded);
    Assert.Equal('\u03C0', decoded[0]);  // π (Greek letter pi)
    Assert.NotEqual((char)0xE3, decoded[0]);  // NOT ã (a-tilde)

    // Additional verification
    Assert.Equal("π", decoded);
  }

  [Fact]
  public void Cp437_PiCharacter_EncodesTo_Byte0xE3()
  {
    // Arrange: String containing π (U+03C0)
    string text = "π";

    // Act: Encode using CP437
    byte[] bytes = Cp437Encoding.Encode(text);

    // Assert: π (U+03C0) becomes byte 0xE3 (round-trip preservation)
    Assert.Single(bytes);
    Assert.Equal(0xE3, bytes[0]);
  }

  [Fact]
  public void RoundTrip_Byte0xE3_PreservedThroughCP437()
  {
    // Arrange
    byte[] originalBytes = new byte[] { 0x48, 0x69, 0xE3, 0x42, 0x79, 0x65 };  // "Hi⟨E3⟩Bye"

    // Act: Decode and re-encode
    string decoded = Cp437Encoding.Decode(originalBytes);
    byte[] reencoded = Cp437Encoding.Encode(decoded);

    // Assert: Byte 0xE3 is preserved
    Assert.Equal(originalBytes, reencoded);
    
    // Verify the middle character is π (U+03C0)
    Assert.Equal('\u03C0', decoded[2]);
    Assert.NotEqual((char)0xE3, decoded[2]);
  }

  [Fact]
  public void AnalysisLogic_MustCheck_ForPiNotByte0xE3()
  {
    // This simulates what happens in TextMessageFormatter.AnalyseBodyContent()
    
    // Arrange: Message body with 3 line terminators (as in starol.qwk message 30)
    byte[] bodyBytes = new byte[]
    {
      // "-πBA>"
      (byte)'-', 0xE3, (byte)'B', (byte)'A', (byte)'>',
      // "tags?π π"
      (byte)'t', (byte)'a', (byte)'g', (byte)'s', (byte)'?', 0xE3, (byte)' ', 0xE3,
      // "done!π*"
      (byte)'d', (byte)'o', (byte)'n', (byte)'e', (byte)'!', 0xE3, (byte)'*'
    };

    // Act: Decode as QwkPacket does
    string bodyText = Cp437Encoding.Decode(bodyBytes);

    // WRONG way (what the bug was doing):
    int wrongCount = 0;
    foreach (char c in bodyText)
    {
      if (c == (char)0xE3)  // ❌ Checking for ã (U+00E3)
      {
        wrongCount++;
      }
    }

    // CORRECT way (the fix):
    int correctCount = 0;
    foreach (char c in bodyText)
    {
      if (c == '\u03C0')  // ✅ Checking for π (U+03C0)
      {
        correctCount++;
      }
    }

    // Assert
    Assert.Equal(0, wrongCount);  // Wrong check finds nothing
    Assert.Equal(4, correctCount);  // Correct check finds all 4 terminators
  }

  [Fact]
  public void Character0xE3_IsNotSameAs_PiCharacter()
  {
    // Demonstrate the fundamental difference
    
    char wrongChar = (char)0xE3;     // ã (U+00E3) - Latin small letter a with tilde
    char correctChar = '\u03C0';     // π (U+03C0) - Greek letter pi

    // These are NOT the same character
    Assert.NotEqual(wrongChar, correctChar);
    Assert.Equal('ã', wrongChar);
    Assert.Equal('π', correctChar);

    // When byte 0xE3 is decoded with CP437, it becomes π, not ã
    byte[] testByte = new byte[] { 0xE3 };
    string decodedWithCp437 = Cp437Encoding.Decode(testByte);
    
    Assert.Equal(correctChar, decodedWithCp437[0]);  // ✅ Is π
    Assert.NotEqual(wrongChar, decodedWithCp437[0]); // ❌ Is NOT ã
  }
}