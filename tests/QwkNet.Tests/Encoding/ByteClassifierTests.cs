using System;
using Xunit;
using QwkNet.Encoding;

namespace QwkNet.Tests.Encoding;

/// <summary>
/// Unit tests for <see cref="ByteClassifier"/>.
/// </summary>
public class ByteClassifierTests
{
  [Fact]
  public void IsExtendedAscii_WithLowByte_ReturnsFalse()
  {
    // Arrange & Act & Assert
    Assert.False(ByteClassifier.IsExtendedAscii(0x00));
    Assert.False(ByteClassifier.IsExtendedAscii(0x20));
    Assert.False(ByteClassifier.IsExtendedAscii(0x7F));
  }

  [Fact]
  public void IsExtendedAscii_WithHighByte_ReturnsTrue()
  {
    // Arrange & Act & Assert
    Assert.True(ByteClassifier.IsExtendedAscii(0x80));
    Assert.True(ByteClassifier.IsExtendedAscii(0xA0));
    Assert.True(ByteClassifier.IsExtendedAscii(0xFF));
  }

  [Fact]
  public void IsExtendedAscii_WithBoundaryValues_ReturnsCorrectly()
  {
    // Arrange & Act & Assert
    Assert.False(ByteClassifier.IsExtendedAscii(0x7F)); // Highest ASCII
    Assert.True(ByteClassifier.IsExtendedAscii(0x80));  // Lowest extended ASCII
  }

  [Fact]
  public void IsBoxDrawing_WithBoxDrawingBytes_ReturnsTrue()
  {
    // Arrange - CP437 box-drawing characters
    byte[] boxDrawingBytes = new byte[]
    {
      0xB0, 0xB1, 0xB2, 0xDB, // Block elements
      0xC4, 0xB3,             // Horizontal and vertical lines
      0xDA, 0xBF, 0xC0, 0xD9, // Corners
      0xC2, 0xC1, 0xB4, 0xC3, 0xC5 // T-junctions and cross
    };

    // Act & Assert
    foreach (byte b in boxDrawingBytes)
    {
      Assert.True(ByteClassifier.IsBoxDrawing(b), $"Byte 0x{b:X2} should be classified as box-drawing");
    }
  }

  [Fact]
  public void IsBoxDrawing_WithNonBoxDrawingBytes_ReturnsFalse()
  {
    // Arrange
    byte[] nonBoxDrawingBytes = new byte[] { 0x00, 0x20, 0x41, 0x7F, 0xA0, 0xE0, 0xFF };

    // Act & Assert
    foreach (byte b in nonBoxDrawingBytes)
    {
      Assert.False(ByteClassifier.IsBoxDrawing(b), $"Byte 0x{b:X2} should not be classified as box-drawing");
    }
  }

  [Fact]
  public void IsLineGraphics_WithLineGraphicsBytes_ReturnsTrue()
  {
    // Arrange - Simple line segments
    byte[] lineGraphicsBytes = new byte[]
    {
      0xC4, 0xB3, 0xBA, 0xCD, // Lines
      0xDA, 0xBF, 0xC0, 0xD9, // Single-line corners
      0xC9, 0xBB, 0xC8, 0xBC, // Double-line corners
      0xC2, 0xC1, 0xB4, 0xC3, 0xC5 // T-junctions
    };

    // Act & Assert
    foreach (byte b in lineGraphicsBytes)
    {
      Assert.True(ByteClassifier.IsLineGraphics(b), $"Byte 0x{b:X2} should be classified as line graphics");
    }
  }

  [Fact]
  public void IsLineGraphics_WithNonLineGraphicsBytes_ReturnsFalse()
  {
    // Arrange
    byte[] nonLineGraphicsBytes = new byte[] { 0x00, 0x20, 0x41, 0x7F, 0xA0, 0xB0, 0xFF };

    // Act & Assert
    foreach (byte b in nonLineGraphicsBytes)
    {
      Assert.False(ByteClassifier.IsLineGraphics(b), $"Byte 0x{b:X2} should not be classified as line graphics");
    }
  }

  [Fact]
  public void IsControlCharacter_WithControlBytes_ReturnsTrue()
  {
    // Arrange & Act & Assert
    Assert.True(ByteClassifier.IsControlCharacter(0x00)); // NUL
    Assert.True(ByteClassifier.IsControlCharacter(0x09)); // TAB
    Assert.True(ByteClassifier.IsControlCharacter(0x0A)); // LF
    Assert.True(ByteClassifier.IsControlCharacter(0x0D)); // CR
    Assert.True(ByteClassifier.IsControlCharacter(0x1B)); // ESC
    Assert.True(ByteClassifier.IsControlCharacter(0x1F)); // Highest control char
    Assert.True(ByteClassifier.IsControlCharacter(0x7F)); // DEL
  }

  [Fact]
  public void IsControlCharacter_WithPrintableBytes_ReturnsFalse()
  {
    // Arrange & Act & Assert
    Assert.False(ByteClassifier.IsControlCharacter(0x20)); // Space
    Assert.False(ByteClassifier.IsControlCharacter(0x41)); // 'A'
    Assert.False(ByteClassifier.IsControlCharacter(0x7E)); // '~'
    Assert.False(ByteClassifier.IsControlCharacter(0x80)); // Extended ASCII
    Assert.False(ByteClassifier.IsControlCharacter(0xE3)); // QWK line terminator
  }

  [Fact]
  public void IsControlCharacter_WithQwkLineTerminator_ReturnsFalse()
  {
    // Arrange - 0xE3 is used as QWK line separator but is not a control character
    byte qwkTerminator = 0xE3;

    // Act
    bool result = ByteClassifier.IsControlCharacter(qwkTerminator);

    // Assert
    Assert.False(result, "0xE3 (QWK line terminator) should not be classified as a control character");
  }

  [Fact]
  public void IsPrintableAscii_WithPrintableBytes_ReturnsTrue()
  {
    // Arrange & Act & Assert
    Assert.True(ByteClassifier.IsPrintableAscii(0x20)); // Space
    Assert.True(ByteClassifier.IsPrintableAscii(0x41)); // 'A'
    Assert.True(ByteClassifier.IsPrintableAscii(0x7E)); // '~' (highest printable)
  }

  [Fact]
  public void IsPrintableAscii_WithNonPrintableBytes_ReturnsFalse()
  {
    // Arrange & Act & Assert
    Assert.False(ByteClassifier.IsPrintableAscii(0x00)); // NUL
    Assert.False(ByteClassifier.IsPrintableAscii(0x1F)); // Control char
    Assert.False(ByteClassifier.IsPrintableAscii(0x7F)); // DEL
    Assert.False(ByteClassifier.IsPrintableAscii(0x80)); // Extended ASCII
    Assert.False(ByteClassifier.IsPrintableAscii(0xFF)); // Extended ASCII
  }

  [Fact]
  public void IsPrintableAscii_WithBoundaryValues_ReturnsCorrectly()
  {
    // Arrange & Act & Assert
    Assert.False(ByteClassifier.IsPrintableAscii(0x1F)); // Just before space
    Assert.True(ByteClassifier.IsPrintableAscii(0x20));  // Space (lowest printable)
    Assert.True(ByteClassifier.IsPrintableAscii(0x7E));  // Tilde (highest printable)
    Assert.False(ByteClassifier.IsPrintableAscii(0x7F)); // DEL (just after)
  }

  [Fact]
  public void IsLineGraphics_IsSubsetOfBoxDrawing()
  {
    // Arrange - All line graphics should also be box-drawing
    byte[] lineGraphicsBytes = new byte[]
    {
      0xC4, 0xB3, 0xBA, 0xCD,
      0xDA, 0xBF, 0xC0, 0xD9,
      0xC9, 0xBB, 0xC8, 0xBC,
      0xC2, 0xC1, 0xB4, 0xC3, 0xC5
    };

    // Act & Assert
    foreach (byte b in lineGraphicsBytes)
    {
      Assert.True(ByteClassifier.IsBoxDrawing(b),
        $"Line graphics byte 0x{b:X2} should also be classified as box-drawing");
    }
  }

  [Fact]
  public void BoxDrawing_IncludesBlockElements_NotInLineGraphics()
  {
    // Arrange - Block elements are box-drawing but not line graphics
    byte[] blockElements = new byte[] { 0xB0, 0xB1, 0xB2, 0xDB };

    // Act & Assert
    foreach (byte b in blockElements)
    {
      Assert.True(ByteClassifier.IsBoxDrawing(b),
        $"Block element 0x{b:X2} should be classified as box-drawing");
      Assert.False(ByteClassifier.IsLineGraphics(b),
        $"Block element 0x{b:X2} should not be classified as line graphics");
    }
  }
}
