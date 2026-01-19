using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QwkNet.Encoding.Ansi;

namespace QwkNet.Tests.Encoding.Ansi;

/// <summary>
/// Unit tests for <see cref="AnsiEscapeDetector"/>.
/// </summary>
public class AnsiEscapeDetectorTests
{
  [Fact]
  public void EscapeByte_HasCorrectValue()
  {
    // Assert
    Assert.Equal(0x1B, AnsiEscapeDetector.EscapeByte);
  }

  [Fact]
  public void ContainsAnsiEscapes_WithNoEscapes_ReturnsFalse()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(bytes);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ContainsAnsiEscapes_WithCsiSequence_ReturnsTrue()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x1B, 0x5B, 0x33, 0x31, 0x6D }; // ESC[31m

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(bytes);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ContainsAnsiEscapes_WithEscButNotCsi_ReturnsFalse()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x1B, 0x41 }; // ESC A (not CSI)

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(bytes);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ContainsAnsiEscapes_WithTextAndEscapes_ReturnsTrue()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello")
      .Concat(new byte[] { 0x1B, 0x5B, 0x31, 0x6D })
      .Concat(System.Text.Encoding.ASCII.GetBytes("World"))
      .ToArray();

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(bytes);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ContainsAnsiEscapes_String_WithNoEscapes_ReturnsFalse()
  {
    // Arrange
    string text = "Hello World";

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(text);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ContainsAnsiEscapes_String_WithCsiSequence_ReturnsTrue()
  {
    // Arrange
    string text = "Hello\u001B[31mWorld";

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(text);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ContainsAnsiEscapes_String_WithNullOrEmpty_ReturnsFalse()
  {
    // Act & Assert
    Assert.False(AnsiEscapeDetector.ContainsAnsiEscapes(string.Empty));
    Assert.False(AnsiEscapeDetector.ContainsAnsiEscapes((string)null!));
  }

  [Fact]
  public void FindEscapeSequences_WithNoEscapes_ReturnsEmptyList()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    IEnumerable<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void FindEscapeSequences_WithSingleSequence_ReturnsOneEntry()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x1B, 0x5B, 0x33, 0x31, 0x6D, 0x65 }; // H + ESC[31m + e

    // Act
    List<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes).ToList();

    // Assert
    Assert.Single(result);
    Assert.Equal(1, result[0].start);    // Starts at index 1
    Assert.Equal(5, result[0].length);   // ESC [ 3 1 m = 5 bytes
  }

  [Fact]
  public void FindEscapeSequences_WithMultipleSequences_ReturnsMultipleEntries()
  {
    // Arrange
    byte[] bytes = new byte[]
    {
      0x1B, 0x5B, 0x33, 0x31, 0x6D,  // ESC[31m
      0x48, 0x65,                     // He
      0x1B, 0x5B, 0x30, 0x6D          // ESC[0m
    };

    // Act
    List<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes).ToList();

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(0, result[0].start);
    Assert.Equal(5, result[0].length);
    Assert.Equal(7, result[1].start);
    Assert.Equal(4, result[1].length);
  }

  [Fact]
  public void FindEscapeSequences_WithIncompleteSequence_ReturnsSequence()
  {
    // Arrange - ESC [ but no command byte
    byte[] bytes = new byte[] { 0x48, 0x1B, 0x5B, 0x33, 0x31 }; // H + ESC[31 (incomplete)

    // Act
    List<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes).ToList();

    // Assert
    Assert.Single(result);
    Assert.Equal(1, result[0].start);
    Assert.Equal(4, result[0].length); // ESC [ 3 1 (no command byte)
  }

  [Fact]
  public void CountEscapeSequences_WithNoEscapes_ReturnsZero()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    int result = AnsiEscapeDetector.CountEscapeSequences(bytes);

    // Assert
    Assert.Equal(0, result);
  }

  [Fact]
  public void CountEscapeSequences_WithMultipleSequences_ReturnsCorrectCount()
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
    int result = AnsiEscapeDetector.CountEscapeSequences(bytes);

    // Assert
    Assert.Equal(2, result);
  }

  [Fact]
  public void FindEscapeSequences_WithCursorMovement_DetectsSequence()
  {
    // Arrange - ESC[H (cursor home)
    byte[] bytes = new byte[] { 0x1B, 0x5B, 0x48 };

    // Act
    List<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes).ToList();

    // Assert
    Assert.Single(result);
    Assert.Equal(0, result[0].start);
    Assert.Equal(3, result[0].length);
  }

  [Fact]
  public void FindEscapeSequences_WithColorReset_DetectsSequence()
  {
    // Arrange - ESC[0m (reset)
    byte[] bytes = new byte[] { 0x1B, 0x5B, 0x30, 0x6D };

    // Act
    List<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes).ToList();

    // Assert
    Assert.Single(result);
    Assert.Equal(0, result[0].start);
    Assert.Equal(4, result[0].length);
  }

  [Fact]
  public void FindEscapeSequences_WithMultiParameter_DetectsSequence()
  {
    // Arrange - ESC[1;31m (bold red)
    byte[] bytes = new byte[] { 0x1B, 0x5B, 0x31, 0x3B, 0x33, 0x31, 0x6D }; // ESC[1;31m

    // Act
    List<(int start, int length)> result = AnsiEscapeDetector.FindEscapeSequences(bytes).ToList();

    // Assert
    Assert.Single(result);
    Assert.Equal(0, result[0].start);
    Assert.Equal(7, result[0].length);
  }

  [Fact]
  public void ContainsAnsiEscapes_WithEscAtEnd_ReturnsFalse()
  {
    // Arrange - ESC at the end with no following byte
    byte[] bytes = new byte[] { 0x48, 0x65, 0x1B }; // "He" + ESC

    // Act
    bool result = AnsiEscapeDetector.ContainsAnsiEscapes(bytes);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void CountEscapeSequences_WithEmptyBytes_ReturnsZero()
  {
    // Arrange
    ReadOnlySpan<byte> bytes = ReadOnlySpan<byte>.Empty;

    // Act
    int result = AnsiEscapeDetector.CountEscapeSequences(bytes);

    // Assert
    Assert.Equal(0, result);
  }
}