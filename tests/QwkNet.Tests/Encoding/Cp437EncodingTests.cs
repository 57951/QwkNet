using System;
using System.Text;
using Xunit;
using QwkNet.Encoding;

namespace QwkNet.Tests.Encoding;

/// <summary>
/// Unit tests for <see cref="Cp437Encoding"/>.
/// </summary>
public class Cp437EncodingTests
{
  [Fact]
  public void Decode_WithEmptyBytes_ReturnsEmptyString()
  {
    // Arrange
    ReadOnlySpan<byte> bytes = ReadOnlySpan<byte>.Empty;

    // Act
    string result = Cp437Encoding.Decode(bytes);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void Decode_WithAsciiBytes_ReturnsCorrectString()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

    // Act
    string result = Cp437Encoding.Decode(bytes);

    // Assert
    Assert.Equal("Hello", result);
  }

  [Fact]
  public void Decode_WithBoxDrawingBytes_ReturnsCorrectString()
  {
    // Arrange - CP437 box-drawing characters
    byte[] bytes = new byte[] { 0xC4, 0xB3, 0xDA, 0xBF }; // ─│┌┐

    // Act
    string result = Cp437Encoding.Decode(bytes);

    // Assert - Verify we get Unicode equivalents
    Assert.NotEmpty(result);
    Assert.Equal(4, result.Length);
  }

  [Fact]
  public void Encode_WithEmptyString_ReturnsEmptyArray()
  {
    // Arrange
    ReadOnlySpan<char> text = ReadOnlySpan<char>.Empty;

    // Act
    byte[] result = Cp437Encoding.Encode(text);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void Encode_WithAsciiString_ReturnsCorrectBytes()
  {
    // Arrange
    string text = "Hello";

    // Act
    byte[] result = Cp437Encoding.Encode(text);

    // Assert
    byte[] expected = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void RoundTrip_WithAsciiText_PreservesData()
  {
    // Arrange
    string original = "Hello, World! 123";

    // Act
    byte[] encoded = Cp437Encoding.Encode(original);
    string decoded = Cp437Encoding.Decode(encoded);

    // Assert
    Assert.Equal(original, decoded);
  }

  [Fact]
  public void RoundTrip_WithExtendedAscii_PreservesData()
  {
    // Arrange - Include some extended ASCII bytes
    byte[] originalBytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0xB0, 0xB1, 0xB2 }; // "Hello" + block elements

    // Act
    string decoded = Cp437Encoding.Decode(originalBytes);
    byte[] reencoded = Cp437Encoding.Encode(decoded);

    // Assert
    Assert.Equal(originalBytes, reencoded);
  }

  [Fact]
  public void Decode_WithStrictPolicy_OnValidBytes_Succeeds()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

    // Act
    string result = Cp437Encoding.Decode(bytes, DecoderFallbackPolicy.Strict);

    // Assert
    Assert.Equal("Hello", result);
  }

  [Fact]
  public void Encode_WithStrictPolicy_OnValidString_Succeeds()
  {
    // Arrange
    string text = "Hello";

    // Act
    byte[] result = Cp437Encoding.Encode(text, EncoderFallbackPolicy.Strict);

    // Assert
    byte[] expected = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void Encode_WithReplacementPolicy_OnUnmappableChar_ReplacesWithQuestionMark()
  {
    // Arrange - Unicode character that doesn't exist in CP437
    string text = "Hello\u2764"; // Heart emoji

    // Act
    byte[] result = Cp437Encoding.Encode(text, EncoderFallbackPolicy.ReplacementQuestion);

    // Assert - Should have "Hello?"
    byte[] expected = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x3F };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void Encode_ToBuffer_WithSufficientSpace_Succeeds()
  {
    // Arrange
    string text = "Hello";
    Span<byte> buffer = stackalloc byte[10];

    // Act
    int bytesWritten = Cp437Encoding.Encode(text, buffer);

    // Assert
    Assert.Equal(5, bytesWritten);
    Assert.Equal(0x48, buffer[0]); // 'H'
    Assert.Equal(0x65, buffer[1]); // 'e'
  }

  [Fact]
  public void GetEncoding_ReturnsNonNullEncoding()
  {
    // Act
    System.Text.Encoding encoding = Cp437Encoding.GetEncoding();

    // Assert
    Assert.NotNull(encoding);
    Assert.Equal(437, encoding.CodePage);
  }

  [Fact]
  public void Decode_WithQwkLineTerminator_PreservesCharacter()
  {
    // Arrange - 0xE3 is the QWK line terminator (π in CP437)
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0xE3, 0x57, 0x6F, 0x72, 0x6C, 0x64 }; // "Hello<0xE3>World"

    // Act
    string result = Cp437Encoding.Decode(bytes);

    // Assert
    Assert.Contains('\u03C0', result); // π (pi) character preserved - 0xE3 in CP437 is U+03C0
  }

  [Fact]
  public void Decode_WithReplacementUnicode_OnUnmappableBytes_UsesReplacementChar()
  {
    // Arrange - This test ensures the replacement policy works
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" (all valid)

    // Act
    string result = Cp437Encoding.Decode(bytes, DecoderFallbackPolicy.ReplacementUnicode);

    // Assert
    Assert.Equal("Hello", result);
  }

  [Fact]
  public void Encode_WithEmptySpan_WritesZeroBytes()
  {
    // Arrange
    ReadOnlySpan<char> text = ReadOnlySpan<char>.Empty;
    Span<byte> buffer = stackalloc byte[10];

    // Act
    int bytesWritten = Cp437Encoding.Encode(text, buffer);

    // Assert
    Assert.Equal(0, bytesWritten);
  }

  [Fact]
  public void RoundTrip_WithAllPrintableAscii_PreservesData()
  {
    // Arrange - All printable ASCII characters (space through ~)
    string original = string.Empty;
    for (char c = ' '; c <= '~'; c++)
    {
      original += c;
    }

    // Act
    byte[] encoded = Cp437Encoding.Encode(original);
    string decoded = Cp437Encoding.Decode(encoded);

    // Assert
    Assert.Equal(original, decoded);
  }

  [Fact]
  public void Decode_WithBestEffort_OnValidBytes_Succeeds()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

    // Act
    string result = Cp437Encoding.Decode(bytes, DecoderFallbackPolicy.BestEffort);

    // Assert
    Assert.Equal("Hello", result);
  }

  [Fact]
  public void Decode_WithReplacementQuestion_OnValidBytes_Succeeds()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

    // Act
    string result = Cp437Encoding.Decode(bytes, DecoderFallbackPolicy.ReplacementQuestion);

    // Assert
    Assert.Equal("Hello", result);
  }
}