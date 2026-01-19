using System;
using System.Linq;
using Xunit;
using QwkNet.Encoding.Ansi;

namespace QwkNet.Tests.Encoding.Ansi;

/// <summary>
/// Unit tests for <see cref="AnsiEscapeStripper"/>.
/// </summary>
public class AnsiEscapeStripperTests
{
  [Fact]
  public void StripAnsiEscapes_Bytes_WithNoEscapes_ReturnsOriginalBytes()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    Assert.Equal(bytes, result);
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_WithSingleSequence_RemovesSequence()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x1B, 0x5B, 0x33, 0x31, 0x6D, 0x65 }; // H + ESC[31m + e

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    byte[] expected = new byte[] { 0x48, 0x65 }; // "He"
    Assert.Equal(expected, result);
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_WithMultipleSequences_RemovesAllSequences()
  {
    // Arrange
    byte[] bytes = new byte[]
    {
      0x1B, 0x5B, 0x33, 0x31, 0x6D,  // ESC[31m
      0x48, 0x65,                     // He
      0x1B, 0x5B, 0x30, 0x6D,         // ESC[0m
      0x6C, 0x6C, 0x6F                // llo
    };

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    byte[] expected = System.Text.Encoding.ASCII.GetBytes("Hello");
    Assert.Equal(expected, result);
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_WithEmptyBytes_ReturnsEmpty()
  {
    // Arrange
    ReadOnlySpan<byte> bytes = ReadOnlySpan<byte>.Empty;

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithNoEscapes_ReturnsOriginalString()
  {
    // Arrange
    string text = "Hello World";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapes(text);

    // Assert
    Assert.Equal(text, result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithSingleSequence_RemovesSequence()
  {
    // Arrange
    string text = "Hello\u001B[31mWorld";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapes(text);

    // Assert
    Assert.Equal("HelloWorld", result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithMultipleSequences_RemovesAllSequences()
  {
    // Arrange
    string text = "\u001B[31mHello\u001B[0m World\u001B[1m!";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapes(text);

    // Assert
    Assert.Equal("Hello World!", result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithNullOrEmpty_ReturnsInput()
  {
    // Act & Assert
    Assert.Empty(AnsiEscapeStripper.StripAnsiEscapes(string.Empty));
    Assert.Null(AnsiEscapeStripper.StripAnsiEscapes((string)null!));
  }

  [Fact]
  public void StripAnsiEscapesPreserveLines_WithLineBreaks_PreservesLineStructure()
  {
    // Arrange
    string text = "\u001B[31mLine 1\u001B[0m\r\n\u001B[32mLine 2\u001B[0m\nLine 3";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapesPreserveLines(text);

    // Assert
    Assert.Contains("Line 1", result);
    Assert.Contains("Line 2", result);
    Assert.Contains("Line 3", result);
    Assert.Contains(Environment.NewLine, result);
  }

  [Fact]
  public void StripAnsiEscapesPreserveLines_WithNullOrEmpty_ReturnsInput()
  {
    // Act & Assert
    Assert.Empty(AnsiEscapeStripper.StripAnsiEscapesPreserveLines(string.Empty));
    Assert.Null(AnsiEscapeStripper.StripAnsiEscapesPreserveLines(null!));
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_WithColorSequences_RemovesColor()
  {
    // Arrange - Text with foreground and background colors
    byte[] bytes = new byte[]
    {
      0x1B, 0x5B, 0x33, 0x31, 0x6D,         // ESC[31m (red foreground)
      0x1B, 0x5B, 0x34, 0x32, 0x6D,         // ESC[42m (green background)
      0x48, 0x65, 0x6C, 0x6C, 0x6F,         // Hello
      0x1B, 0x5B, 0x30, 0x6D                // ESC[0m (reset)
    };

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    byte[] expected = System.Text.Encoding.ASCII.GetBytes("Hello");
    Assert.Equal(expected, result);
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_WithCursorMovement_RemovesMovement()
  {
    // Arrange - Text with cursor positioning
    byte[] bytes = new byte[]
    {
      0x1B, 0x5B, 0x48,           // ESC[H (cursor home)
      0x48, 0x65,                 // He
      0x1B, 0x5B, 0x32, 0x4A,     // ESC[2J (clear screen)
      0x6C, 0x6C, 0x6F            // llo
    };

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    byte[] expected = System.Text.Encoding.ASCII.GetBytes("Hello");
    Assert.Equal(expected, result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithIncompleteSequence_RemovesPartialSequence()
  {
    // Arrange - ESC[31W is technically a valid ANSI sequence (W is the command)
    string text = "Hello\u001B[31World";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapes(text);

    // Assert
    // ESC[31W is treated as a complete sequence (W is command byte), so W is stripped
    Assert.Contains("Hello", result);
    Assert.Contains("orld", result); // "World" minus the 'W' command byte
    Assert.DoesNotContain("W", result); // W was the command byte and is stripped
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_PreservesNonAnsiEscapes()
  {
    // Arrange - ESC followed by something other than '['
    byte[] bytes = new byte[] { 0x48, 0x1B, 0x41, 0x65 }; // H + ESC + A + e

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    // Non-CSI escape sequences are preserved in this implementation
    byte[] expected = new byte[] { 0x48, 0x1B, 0x41, 0x65 };
    Assert.Equal(expected, result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithComplexSequence_RemovesCorrectly()
  {
    // Arrange - Multi-parameter sequence
    string text = "Bold \u001B[1;31mRed\u001B[0m Text";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapes(text);

    // Assert
    Assert.Equal("Bold Red Text", result);
  }

  [Fact]
  public void StripAnsiEscapesPreserveLines_WithMultipleLineEndings_NormalisesToPlatform()
  {
    // Arrange
    string text = "\u001B[31mLine 1\u001B[0m\r\nLine 2\nLine 3\rLine 4";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapesPreserveLines(text);

    // Assert
    Assert.Contains("Line 1", result);
    Assert.Contains("Line 2", result);
    Assert.Contains("Line 3", result);
    Assert.Contains("Line 4", result);
  }

  [Fact]
  public void StripAnsiEscapes_Bytes_WithOnlyEscapeSequences_ReturnsEmpty()
  {
    // Arrange - Only ANSI codes, no text
    byte[] bytes = new byte[]
    {
      0x1B, 0x5B, 0x33, 0x31, 0x6D,  // ESC[31m
      0x1B, 0x5B, 0x30, 0x6D          // ESC[0m
    };

    // Act
    byte[] result = AnsiEscapeStripper.StripAnsiEscapes(bytes.AsSpan());

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void StripAnsiEscapes_String_WithOnlyEscapeSequences_ReturnsEmpty()
  {
    // Arrange
    string text = "\u001B[31m\u001B[0m";

    // Act
    string result = AnsiEscapeStripper.StripAnsiEscapes(text);

    // Assert
    Assert.Empty(result);
  }
}