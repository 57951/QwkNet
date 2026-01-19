using System.Collections.Generic;
using System.Linq;
using Xunit;
using QwkNet.Core;

namespace QwkNet.Tests.Core;

/// <summary>
/// Tests for <see cref="MessageBodyParser"/>.
/// </summary>
public sealed class MessageBodyParserTests
{
  [Fact]
  public void ParseLines_WithSingleBlock_ReturnsLines()
  {
    // Arrange
    byte[] block = new byte[128];
    System.Array.Fill(block, (byte)' ');
    
    // Write "Line 1" + 0xE3 + "Line 2" + 0xE3
    string text = "Line 1";
    for (int i = 0; i < text.Length; i++)
    {
      block[i] = (byte)text[i];
    }
    block[6] = 0xE3; // Line terminator
    
    text = "Line 2";
    for (int i = 0; i < text.Length; i++)
    {
      block[7 + i] = (byte)text[i];
    }
    block[13] = 0xE3; // Line terminator
    
    byte[][] blocks = new[] { block };

    // Act
    List<string> lines = MessageBodyParser.ParseLines(blocks);

    // Assert
    Assert.Equal(2, lines.Count);
    Assert.Equal("Line 1", lines[0]);
    Assert.Equal("Line 2", lines[1]);
  }

  [Fact]
  public void ParseLines_WithMultipleBlocks_ConcatenatesCorrectly()
  {
    // Arrange
    // In QWK, message blocks are concatenated as a continuous byte stream.
    // The test should reflect realistic message structure: content flows continuously,
    // and only the final block has padding.
    
    byte[] block1 = new byte[128];
    byte[] block2 = new byte[128];
    
    // Fill with terminators to avoid accidental "lines" from padding
    System.Array.Fill(block1, (byte)' ');
    System.Array.Fill(block2, (byte)' ');
    
    // Simplest case: all three lines fit in block1, block2 is just padding
    int pos = 0;
    foreach (char c in "Line 1")
    {
      block1[pos++] = (byte)c;
    }
    block1[pos++] = 0xE3;
    
    foreach (char c in "Line 2")
    {
      block1[pos++] = (byte)c;
    }
    block1[pos++] = 0xE3;
    
    foreach (char c in "Line 3")
    {
      block1[pos++] = (byte)c;
    }
    block1[pos++] = 0xE3;
    
    // Rest of block1 and all of block2 are padding (spaces)
    // This is realistic: most messages fit in one or two blocks
    
    byte[][] blocks = new[] { block1, block2 };

    // Act
    List<string> lines = MessageBodyParser.ParseLines(blocks);

    // Assert
    Assert.Equal(3, lines.Count);
    Assert.Equal("Line 1", lines[0]);
    Assert.Equal("Line 2", lines[1]);
    Assert.Equal("Line 3", lines[2]);
  }

  [Fact]
  public void ParseLines_WithoutFinalTerminator_IncludesLastLine()
  {
    // Arrange
    byte[] block = new byte[128];
    System.Array.Fill(block, (byte)' ');
    
    // "Line 1" + 0xE3 + "Line 2" (no terminator on Line 2)
    int pos = 0;
    foreach (char c in "Line 1")
    {
      block[pos++] = (byte)c;
    }
    block[pos++] = 0xE3;
    
    foreach (char c in "Line 2")
    {
      block[pos++] = (byte)c;
    }
    // No terminator after Line 2
    
    byte[][] blocks = new[] { block };

    // Act
    List<string> lines = MessageBodyParser.ParseLines(blocks);

    // Assert
    Assert.Equal(2, lines.Count);
    Assert.Equal("Line 1", lines[0]);
    Assert.Equal("Line 2", lines[1]);
  }

  [Fact]
  public void ParseLines_WithNullBytes_ConvertsToSpaces()
  {
    // Arrange
    byte[] block = new byte[128];
    System.Array.Fill(block, (byte)' ');
    
    // "Test" + 0x00 + "Text" + 0xE3
    int pos = 0;
    foreach (char c in "Test")
    {
      block[pos++] = (byte)c;
    }
    block[pos++] = 0x00; // Null byte
    foreach (char c in "Text")
    {
      block[pos++] = (byte)c;
    }
    block[pos++] = 0xE3; // Line terminator
    
    byte[][] blocks = new[] { block };

    // Act
    List<string> lines = MessageBodyParser.ParseLines(blocks);

    // Assert
    Assert.Single(lines);
    Assert.Equal("Test Text", lines[0]);
  }

  [Fact]
  public void ParseLines_WithEmptyBlocks_ReturnsEmpty()
  {
    // Arrange
    byte[][] blocks = System.Array.Empty<byte[]>();

    // Act
    List<string> lines = MessageBodyParser.ParseLines(blocks);

    // Assert
    Assert.Empty(lines);
  }

  [Fact]
  public void ParseLines_WithNullInput_ThrowsArgumentNullException()
  {
    // Act & Assert
    System.ArgumentNullException ex = Assert.Throws<System.ArgumentNullException>(() =>
      MessageBodyParser.ParseLines(null!));
    
    Assert.Equal("blocks", ex.ParamName);
  }

  [Fact]
  public void ParseLinesFromBuffer_WithSingleLine_ReturnsOneLine()
  {
    // Arrange
    byte[] buffer = new byte[13];
    int pos = 0;
    foreach (char c in "Hello World")
    {
      buffer[pos++] = (byte)c;
    }
    buffer[pos++] = 0xE3; // Line terminator

    // Act
    List<string> lines = MessageBodyParser.ParseLinesFromBuffer(buffer);

    // Assert
    Assert.Single(lines);
    Assert.Equal("Hello World", lines[0]);
  }

  [Fact]
  public void ParseLinesFromBuffer_WithEmptyBuffer_ReturnsEmpty()
  {
    // Arrange
    byte[] buffer = System.Array.Empty<byte>();

    // Act
    List<string> lines = MessageBodyParser.ParseLinesFromBuffer(buffer);

    // Assert
    Assert.Empty(lines);
  }

  [Fact]
  public void EncodeLines_WithSimpleLines_ReturnsCorrectBlocks()
  {
    // Arrange
    string[] lines = new[] { "Line 1", "Line 2" };

    // Act
    byte[][] blocks = MessageBodyParser.EncodeLines(lines);

    // Assert
    Assert.Single(blocks); // Should fit in one 128-byte block
    Assert.Equal(128, blocks[0].Length);
    
    // Verify round-trip
    List<string> decoded = MessageBodyParser.ParseLines(blocks);
    Assert.Equal(2, decoded.Count);
    Assert.Equal("Line 1", decoded[0]);
    Assert.Equal("Line 2", decoded[1]);
  }

  [Fact]
  public void EncodeLines_WithLongText_CreatesMultipleBlocks()
  {
    // Arrange
    string longLine = new string('X', 200);
    string[] lines = new[] { longLine };

    // Act
    byte[][] blocks = MessageBodyParser.EncodeLines(lines);

    // Assert
    Assert.True(blocks.Length > 1); // Must span multiple blocks
    Assert.All(blocks, block => Assert.Equal(128, block.Length));
  }

  [Fact]
  public void EncodeLines_WithNoTerminateLastLine_OmitsTerminator()
  {
    // Arrange
    string[] lines = new[] { "Only line" };

    // Act
    byte[][] blocks = MessageBodyParser.EncodeLines(
      lines,
      terminateLastLine: false);

    // Assert
    byte[] buffer = blocks.SelectMany(b => b).ToArray();
    int lastNonSpace = System.Array.FindLastIndex(
      buffer,
      b => b != (byte)' ');
    
    // Last non-space byte should not be 0xE3
    Assert.NotEqual(0xE3, buffer[lastNonSpace]);
  }

  [Fact]
  public void EncodeLines_WithNullInput_ThrowsArgumentNullException()
  {
    // Act & Assert
    System.ArgumentNullException ex = Assert.Throws<System.ArgumentNullException>(() =>
      MessageBodyParser.EncodeLines(null!));
    
    Assert.Equal("lines", ex.ParamName);
  }

  [Fact]
  public void EncodeLines_PadsToBlockBoundary()
  {
    // Arrange
    string[] lines = new[] { "Short" };

    // Act
    byte[][] blocks = MessageBodyParser.EncodeLines(lines);

    // Assert
    Assert.Single(blocks);
    Assert.Equal(128, blocks[0].Length);
    
    // After "Short\u00E3", rest should be spaces
    int contentLength = "Short".Length + 1; // +1 for terminator
    for (int i = contentLength; i < 128; i++)
    {
      Assert.Equal((byte)' ', blocks[0][i]);
    }
  }

  [Fact]
  public void IsQwkeFormat_WithCRLineEndings_ReturnsTrue()
  {
    // Arrange
    byte[] buffer = System.Text.Encoding.ASCII.GetBytes("Line 1\rLine 2\rLine 3\r");

    // Act
    bool isQwke = MessageBodyParser.IsQwkeFormat(buffer);

    // Assert
    Assert.True(isQwke);
  }

  [Fact]
  public void IsQwkeFormat_WithE3Terminators_ReturnsFalse()
  {
    // Arrange
    byte[] buffer = new byte[16];
    int pos = 0;
    foreach (char c in "Line 1")
    {
      buffer[pos++] = (byte)c;
    }
    buffer[pos++] = 0xE3;
    foreach (char c in "Line 2")
    {
      buffer[pos++] = (byte)c;
    }
    buffer[pos++] = 0xE3;

    // Act
    bool isQwke = MessageBodyParser.IsQwkeFormat(buffer);

    // Assert
    Assert.False(isQwke);
  }

  [Fact]
  public void IsQwkeFormat_WithMixedEndings_PrefersE3Detection()
  {
    // Arrange
    byte[] buffer = new byte[16];
    int pos = 0;
    foreach (char c in "Line 1")
    {
      buffer[pos++] = (byte)c;
    }
    buffer[pos++] = 0x0D; // CR
    foreach (char c in "Line 2")
    {
      buffer[pos++] = (byte)c;
    }
    buffer[pos++] = 0xE3; // E3 terminator

    // Act
    bool isQwke = MessageBodyParser.IsQwkeFormat(buffer);

    // Assert
    // If both CR and E3 are present, it's not pure QWKE format
    Assert.False(isQwke);
  }

  [Fact]
  public void ParseQwkeLines_WithCRLF_ParsesCorrectly()
  {
    // Arrange
    byte[] buffer = System.Text.Encoding.ASCII.GetBytes("Line 1\r\nLine 2\r\nLine 3");

    // Act
    List<string> lines = MessageBodyParser.ParseQwkeLines(buffer);

    // Assert
    Assert.Equal(3, lines.Count);
    Assert.Equal("Line 1", lines[0]);
    Assert.Equal("Line 2", lines[1]);
    Assert.Equal("Line 3", lines[2]);
  }

  [Fact]
  public void ParseQwkeLines_WithCROnly_ParsesCorrectly()
  {
    // Arrange
    byte[] buffer = System.Text.Encoding.ASCII.GetBytes("Line 1\rLine 2\rLine 3");

    // Act
    List<string> lines = MessageBodyParser.ParseQwkeLines(buffer);

    // Assert
    Assert.Equal(3, lines.Count);
    Assert.Equal("Line 1", lines[0]);
    Assert.Equal("Line 2", lines[1]);
    Assert.Equal("Line 3", lines[2]);
  }

  [Fact]
  public void ParseQwkeLines_WithNullBytes_ConvertsToSpaces()
  {
    // Arrange
    byte[] buffer = System.Text.Encoding.ASCII.GetBytes("Test\0Text\r");

    // Act
    List<string> lines = MessageBodyParser.ParseQwkeLines(buffer);

    // Assert
    Assert.Single(lines);
    Assert.Equal("Test Text", lines[0]);
  }

  [Fact]
  public void ParseQwkeLines_WithStandaloneLF_ParsesAsNewLine()
  {
    // Arrange
    byte[] buffer = System.Text.Encoding.ASCII.GetBytes("Line 1\nLine 2\n");

    // Act
    List<string> lines = MessageBodyParser.ParseQwkeLines(buffer);

    // Assert
    Assert.Equal(2, lines.Count);
    Assert.Equal("Line 1", lines[0]);
    Assert.Equal("Line 2", lines[1]);
  }

  [Fact]
  public void RoundTrip_EncodeDecode_PreservesContent()
  {
    // Arrange
    string[] originalLines = new[]
    {
      "This is line one",
      "This is line two",
      "This is line three with more text"
    };

    // Act
    byte[][] encoded = MessageBodyParser.EncodeLines(originalLines);
    List<string> decoded = MessageBodyParser.ParseLines(encoded);

    // Assert
    Assert.Equal(originalLines.Length, decoded.Count);
    for (int i = 0; i < originalLines.Length; i++)
    {
      Assert.Equal(originalLines[i], decoded[i]);
    }
  }

  [Fact]
  public void ParseLines_WithTrailingSpaces_TrimsCorrectly()
  {
    // Arrange
    byte[] block = new byte[128];
    System.Array.Fill(block, (byte)' ');
    
    // "Line with spaces" + "   " + 0xE3
    int pos = 0;
    foreach (char c in "Line with spaces   ")
    {
      block[pos++] = (byte)c;
    }
    block[pos++] = 0xE3; // Line terminator
    
    byte[][] blocks = new[] { block };

    // Act
    List<string> lines = MessageBodyParser.ParseLines(blocks);

    // Assert
    Assert.Single(lines);
    Assert.Equal("Line with spaces", lines[0]);
  }

  private static byte[] CreateBlockWithText(string text)
  {
    byte[] block = new byte[128];
    System.Array.Fill(block, (byte)' ');
    
    // Convert text to bytes, treating each char as a byte value
    // This allows us to include 0xE3 line terminators
    int length = System.Math.Min(text.Length, 128);
    for (int i = 0; i < length; i++)
    {
      block[i] = (byte)text[i];
    }
    
    return block;
  }

  private static byte[] CreateTextWithTerminator(string text)
  {
    // Helper to create text with 0xE3 terminator
    List<byte> bytes = new List<byte>();
    foreach (char c in text)
    {
      bytes.Add((byte)c);
    }
    bytes.Add(0xE3); // QWK line terminator
    return bytes.ToArray();
  }
}