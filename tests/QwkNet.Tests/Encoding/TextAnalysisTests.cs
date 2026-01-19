using System;
using System.Collections.Generic;
using Xunit;
using QwkNet.Encoding;

namespace QwkNet.Tests.Encoding;

/// <summary>
/// Unit tests for <see cref="TextAnalysis"/>.
/// </summary>
public class TextAnalysisTests
{
  [Fact]
  public void Analyse_WithPureAscii_ReportsNoHighBitBytes()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.False(result.ContainsHighBitBytes);
    Assert.Equal(0, result.HighBitByteCount);
  }

  [Fact]
  public void Analyse_WithExtendedAscii_ReportsHighBitBytes()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x80, 0x90, 0xFF }; // "Hello" + extended

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.ContainsHighBitBytes);
    Assert.Equal(3, result.HighBitByteCount);
  }

  [Fact]
  public void Analyse_WithBoxDrawing_ReportsBoxDrawingBytes()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0xC4, 0xB3, 0xDA }; // "He" + box-drawing

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.HasBoxDrawingBytes);
    Assert.Equal(3, result.BoxDrawingByteCount);
  }

  [Fact]
  public void Analyse_WithNoBoxDrawing_ReportsNoBoxDrawingBytes()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.False(result.HasBoxDrawingBytes);
    Assert.Equal(0, result.BoxDrawingByteCount);
  }

  [Fact]
  public void Analyse_WithAnsiEscapes_ReportsAnsiPresence()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x1B, 0x5B, 0x33, 0x31, 0x6D, 0x65 }; // "H" + ESC[31m + "e"

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.HasAnsiEscapes);
  }

  [Fact]
  public void Analyse_WithoutAnsiEscapes_ReportsNoAnsi()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello World");

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.False(result.HasAnsiEscapes);
  }

  [Fact]
  public void Analyse_WithEscButNotAnsi_ReportsNoAnsi()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x1B, 0x41, 0x65 }; // "H" + ESC + "A" + "e" (not ESC[)

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.False(result.HasAnsiEscapes);
  }

  [Fact]
  public void Analyse_WithoutHistogram_ReturnsNullHistogram()
  {
    // Arrange
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes("Hello");

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes, includeHistogram: false);

    // Assert
    Assert.Null(result.ByteHistogram);
  }

  [Fact]
  public void Analyse_WithHistogram_ReturnsPopulatedHistogram()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes, includeHistogram: true);

    // Assert
    Assert.NotNull(result.ByteHistogram);
    Assert.Equal(4, result.ByteHistogram.Count); // H, e, l, o (l appears twice)
    Assert.Equal(1, result.ByteHistogram[0x48]); // H
    Assert.Equal(1, result.ByteHistogram[0x65]); // e
    Assert.Equal(2, result.ByteHistogram[0x6C]); // l (twice)
    Assert.Equal(1, result.ByteHistogram[0x6F]); // o
  }

  [Fact]
  public void Analyse_WithEmptyBytes_ReturnsZeroCountsAndStats()
  {
    // Arrange
    ReadOnlySpan<byte> bytes = ReadOnlySpan<byte>.Empty;

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.False(result.ContainsHighBitBytes);
    Assert.Equal(0, result.HighBitByteCount);
    Assert.False(result.HasBoxDrawingBytes);
    Assert.Equal(0, result.BoxDrawingByteCount);
    Assert.False(result.HasAnsiEscapes);
  }

  [Fact]
  public void Analyse_WithMixedContent_ReportsAllStats()
  {
    // Arrange - ASCII + extended + box-drawing + ANSI
    byte[] bytes = new byte[]
    {
      0x48, 0x65,       // "He"
      0x1B, 0x5B, 0x6D, // ESC[m
      0xC4, 0xB3,       // Box-drawing
      0xFF              // Extended ASCII
    };

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.ContainsHighBitBytes);
    Assert.Equal(3, result.HighBitByteCount); // 0xC4, 0xB3, 0xFF
    Assert.True(result.HasBoxDrawingBytes);
    Assert.Equal(2, result.BoxDrawingByteCount); // 0xC4, 0xB3
    Assert.True(result.HasAnsiEscapes);
  }

  [Fact]
  public void ToString_ReturnsDescriptiveSummary()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0xC4, 0xFF };
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Act
    string summary = result.ToString();

    // Assert
    Assert.Contains("HighBit", summary);
    Assert.Contains("BoxDrawing", summary);
    Assert.Contains("ANSI", summary);
    Assert.Contains("Histogram", summary);
  }

  [Fact]
  public void Analyse_WithRepeatedBoxDrawing_CountsCorrectly()
  {
    // Arrange
    byte[] bytes = new byte[] { 0xC4, 0xC4, 0xC4, 0xB3, 0xB3 }; // Repeated box-drawing

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.HasBoxDrawingBytes);
    Assert.Equal(5, result.BoxDrawingByteCount);
  }

  [Fact]
  public void Analyse_WithQwkLineTerminator_CountsAsHighBit()
  {
    // Arrange
    byte[] bytes = new byte[] { 0x48, 0x65, 0xE3, 0x6C, 0x6C, 0x6F }; // "He<0xE3>llo"

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.ContainsHighBitBytes);
    Assert.Equal(1, result.HighBitByteCount); // 0xE3
  }

  [Fact]
  public void Analyse_WithMultipleAnsiSequences_ReportsPresence()
  {
    // Arrange - Multiple ANSI sequences
    byte[] bytes = new byte[]
    {
      0x1B, 0x5B, 0x33, 0x31, 0x6D, // ESC[31m
      0x48, 0x65,                   // "He"
      0x1B, 0x5B, 0x30, 0x6D        // ESC[0m
    };

    // Act
    TextAnalysis result = TextAnalysis.Analyse(bytes);

    // Assert
    Assert.True(result.HasAnsiEscapes);
  }

  [Fact]
  public void Constructor_InitialisesAllProperties()
  {
    // Arrange
    Dictionary<byte, int> histogram = new Dictionary<byte, int> { { 0x48, 1 } };

    // Act
    TextAnalysis result = new TextAnalysis(
      containsHighBitBytes: true,
      highBitByteCount: 5,
      hasBoxDrawingBytes: true,
      boxDrawingByteCount: 3,
      hasAnsiEscapes: true,
      byteHistogram: histogram);

    // Assert
    Assert.True(result.ContainsHighBitBytes);
    Assert.Equal(5, result.HighBitByteCount);
    Assert.True(result.HasBoxDrawingBytes);
    Assert.Equal(3, result.BoxDrawingByteCount);
    Assert.True(result.HasAnsiEscapes);
    Assert.NotNull(result.ByteHistogram);
    Assert.Single(result.ByteHistogram);
  }
}
