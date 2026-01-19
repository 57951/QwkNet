using System;
using System.IO;
using System.Text;
using QwkNet.Encoding;
using Xunit;

namespace QwkNet.Tests.Rendering;

/// <summary>
/// Tests for CP437 box-drawing character rendering and round-trip fidelity.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that:
/// - CP437 box-drawing characters encode/decode correctly
/// - Characters preserve byte values through round-trip
/// - Console encoding setup works correctly
/// - Platform-specific rendering issues are documented
/// </para>
/// <para>
/// Note: These tests verify encoding correctness, not actual visual rendering
/// in the console (which depends on terminal capabilities). Use the rendertest
/// command in QwkNet.Diagnostics for visual verification.
/// </para>
/// </remarks>
public class BoxDrawingRenderingTests
{
  /// <summary>
  /// Tests that single-line box-drawing characters round-trip correctly.
  /// </summary>
  [Fact]
  public void SingleLineBoxDrawing_RoundTrips_Correctly()
  {
    // Arrange - CP437 single-line box drawing bytes
    byte[] originalBytes = new byte[]
    {
      0xC4, // horizontal line
      0xB3, // vertical line
      0xDA, // top-left corner
      0xBF, // top-right corner
      0xC0, // bottom-left corner
      0xD9, // bottom-right corner
      0xC2, // T-down
      0xC1, // T-up
      0xB4, // T-left
      0xC3, // T-right
      0xC5  // cross
    };

    // Act - Decode to string and encode back
    string decoded = Cp437Encoding.Decode(originalBytes, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert - Bytes should match exactly
    Assert.Equal(originalBytes.Length, reencoded.Length);
    Assert.Equal(originalBytes, reencoded);
    
    // Assert - String should contain Unicode box-drawing characters
    Assert.NotEmpty(decoded);
    Assert.DoesNotContain('?', decoded);
  }

  /// <summary>
  /// Tests that double-line box-drawing characters round-trip correctly.
  /// </summary>
  [Fact]
  public void DoubleLineBoxDrawing_RoundTrips_Correctly()
  {
    // Arrange - CP437 double-line box drawing bytes
    byte[] originalBytes = new byte[]
    {
      0xCD, // horizontal line (double)
      0xBA, // vertical line (double)
      0xC9, // top-left corner (double)
      0xBB, // top-right corner (double)
      0xC8, // bottom-left corner (double)
      0xBC, // bottom-right corner (double)
      0xCB, // T-down (double)
      0xCA, // T-up (double)
      0xB9, // T-left (double)
      0xCC, // T-right (double)
      0xCE  // cross (double)
    };

    // Act
    string decoded = Cp437Encoding.Decode(originalBytes, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert
    Assert.Equal(originalBytes.Length, reencoded.Length);
    Assert.Equal(originalBytes, reencoded);
    Assert.NotEmpty(decoded);
    Assert.DoesNotContain('?', decoded);
  }

  /// <summary>
  /// Tests that block graphics characters round-trip correctly.
  /// </summary>
  [Fact]
  public void BlockGraphics_RoundTrip_Correctly()
  {
    // Arrange - CP437 block graphics bytes
    byte[] originalBytes = new byte[]
    {
      0xB0, // light shade
      0xB1, // medium shade
      0xB2, // dark shade
      0xDB, // full block
      0xDC, // lower half block
      0xDD, // left half block
      0xDE, // right half block
      0xDF  // upper half block
    };

    // Act
    string decoded = Cp437Encoding.Decode(originalBytes, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert
    Assert.Equal(originalBytes.Length, reencoded.Length);
    Assert.Equal(originalBytes, reencoded);
    Assert.NotEmpty(decoded);
  }

  /// <summary>
  /// Tests that a complete box draws and round-trips correctly.
  /// </summary>
  [Fact]
  public void CompleteBox_RoundTrips_Correctly()
  {
    // Arrange - A simple box pattern with newlines
    byte[] boxPattern = new byte[]
    {
      0xDA, 0xC4, 0xC4, 0xC4, 0xBF, 0x0A, // top line + newline
      0xB3, 0x20, 0x20, 0x20, 0xB3, 0x0A, // middle + newline
      0xC0, 0xC4, 0xC4, 0xC4, 0xD9        // bottom line
    };

    // Act
    string decoded = Cp437Encoding.Decode(boxPattern, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert - Exact byte match
    Assert.Equal(boxPattern, reencoded);
    Assert.Contains('\n', decoded); // Has newlines
    Assert.NotEmpty(decoded);
  }

  /// <summary>
  /// Tests that ANSI art patterns with mixed characters round-trip correctly.
  /// </summary>
  [Fact]
  public void AnsiArtPattern_RoundTrips_Correctly()
  {
    // Arrange - Mixed box-drawing and block graphics
    byte[] ansiPattern = new byte[]
    {
      0xDA, 0xC4, 0xC4, 0xBF, 0x0A,           // box top
      0xB3, 0xDB, 0xDB, 0xB3, 0x0A,           // full blocks
      0xB3, 0xB2, 0xB2, 0xB3, 0x0A,           // dark shade
      0xB3, 0xB0, 0xB0, 0xB3, 0x0A,           // light shade
      0xC0, 0xC4, 0xC4, 0xD9                  // box bottom
    };

    // Act
    string decoded = Cp437Encoding.Decode(ansiPattern, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert
    Assert.Equal(ansiPattern, reencoded);
    Assert.NotEmpty(decoded);
  }

  /// <summary>
  /// Tests that ByteClassifier correctly identifies box-drawing characters.
  /// </summary>
  [Fact]
  public void ByteClassifier_IdentifiesBoxDrawing_Correctly()
  {
    // Arrange
    byte[] boxBytes = new byte[] { 0xC4, 0xB3, 0xDA, 0xBF, 0xC0, 0xD9 };
    byte[] normalBytes = new byte[] { 0x41, 0x42, 0x43 }; // ABC

    // Act & Assert - Box-drawing bytes are identified
    foreach (byte b in boxBytes)
    {
      Assert.True(ByteClassifier.IsBoxDrawing(b), $"Byte 0x{b:X2} should be box-drawing");
    }

    // Act & Assert - Normal ASCII is not box-drawing
    foreach (byte b in normalBytes)
    {
      Assert.False(ByteClassifier.IsBoxDrawing(b), $"Byte 0x{b:X2} should not be box-drawing");
    }
  }

  /// <summary>
  /// Tests that ByteClassifier correctly identifies line graphics.
  /// </summary>
  [Fact]
  public void ByteClassifier_IdentifiesLineGraphics_Correctly()
  {
    // Arrange
    byte[] lineBytes = new byte[] { 0xC4, 0xB3, 0xC9, 0xBB, 0xC8, 0xBC };
    
    // Act & Assert
    foreach (byte b in lineBytes)
    {
      Assert.True(ByteClassifier.IsLineGraphics(b), $"Byte 0x{b:X2} should be line graphics");
    }
  }

  /// <summary>
  /// Tests that TextAnalysis correctly detects box-drawing content.
  /// </summary>
  [Fact]
  public void TextAnalysis_DetectsBoxDrawing_Correctly()
  {
    // Arrange - Text with box-drawing
    byte[] withBoxes = new byte[] { 0x41, 0xC4, 0xB3, 0x42 }; // A + box chars + B
    byte[] withoutBoxes = new byte[] { 0x41, 0x42, 0x43 };    // ABC

    // Act
    TextAnalysis withAnalysis = TextAnalysis.Analyse(withBoxes, includeHistogram: false);
    TextAnalysis withoutAnalysis = TextAnalysis.Analyse(withoutBoxes, includeHistogram: false);

    // Assert
    Assert.True(withAnalysis.HasBoxDrawingBytes);
    Assert.Equal(2, withAnalysis.BoxDrawingByteCount);
    
    Assert.False(withoutAnalysis.HasBoxDrawingBytes);
    Assert.Equal(0, withoutAnalysis.BoxDrawingByteCount);
  }

  /// <summary>
  /// Tests that BBS-style banner text renders correctly.
  /// </summary>
  [Fact]
  public void BbsBanner_RoundTrips_Correctly()
  {
    // Arrange - Typical BBS banner style (double-line)
    byte[] bannerBytes = new byte[]
    {
      0xC9, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xBB, 0x0A, // top
      0xBA, 0x20, 0x42, 0x42, 0x53, 0x20, 0x20, 0x20, 0x20, 0xBA, 0x0A, // " BBS    "
      0xC8, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xBC        // bottom
    };

    // Act
    string decoded = Cp437Encoding.Decode(bannerBytes, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert - Exact round-trip
    Assert.Equal(bannerBytes, reencoded);
    Assert.Contains("BBS", decoded);
  }

  /// <summary>
  /// Tests that international characters mixed with box-drawing work correctly.
  /// </summary>
  [Fact]
  public void InternationalWithBoxDrawing_RoundTrips_Correctly()
  {
    // Arrange - French name in a box (0x82 = e-acute in CP437)
    byte[] mixedBytes = new byte[]
    {
      0xDA, 0xC4, 0xC4, 0xC4, 0xC4, 0xBF, 0x0A, // box top
      0xB3, 0x4A, 0x6F, 0x73, 0x82, 0xB3, 0x0A, // "Jos" + e-acute + vertical
      0xC0, 0xC4, 0xC4, 0xC4, 0xC4, 0xD9        // box bottom
    };

    // Act
    string decoded = Cp437Encoding.Decode(mixedBytes, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert
    Assert.Equal(mixedBytes, reencoded);
    Assert.Contains("Jos", decoded);
  }

  /// <summary>
  /// Tests that console encoding can be set to UTF-8.
  /// </summary>
  [Fact]
  public void ConsoleEncoding_CanBeSetToUtf8()
  {
    // Act - Try to set console encoding
    System.Text.Encoding originalOutput = Console.OutputEncoding;
    System.Text.Encoding originalInput = Console.InputEncoding;
    
    try
    {
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Console.InputEncoding = System.Text.Encoding.UTF8;

      // Assert
      Assert.Equal(65001, Console.OutputEncoding.CodePage); // UTF-8 code page
      Assert.Equal(65001, Console.InputEncoding.CodePage);
    }
    finally
    {
      // Restore original encoding
      Console.OutputEncoding = originalOutput;
      Console.InputEncoding = originalInput;
    }
  }

  /// <summary>
  /// Tests that all CP437 box-drawing bytes can be classified.
  /// </summary>
  [Fact]
  public void AllBoxDrawingBytes_AreClassified_Correctly()
  {
    // Arrange - All CP437 box-drawing range bytes
    byte[] boxDrawingRange = new byte[]
    {
      // Single-line
      0xB3, 0xB4, 0xB9, 0xBA, 0xBB, 0xBC, 0xBF,
      0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC9,
      0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xD9, 0xDA,
      // Block graphics
      0xB0, 0xB1, 0xB2, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF
    };

    // Act & Assert - All should be identified as box-drawing
    foreach (byte b in boxDrawingRange)
    {
      Assert.True(
        ByteClassifier.IsBoxDrawing(b),
        $"Byte 0x{b:X2} should be classified as box-drawing");
    }
  }

  /// <summary>
  /// Tests that extended ASCII range is correctly identified.
  /// </summary>
  [Fact]
  public void ExtendedAsciiRange_IsIdentified_Correctly()
  {
    // Arrange
    byte normalAscii = 0x41;      // 'A'
    byte extendedAscii = 0x82;    // e-acute in CP437

    // Act & Assert
    Assert.False(ByteClassifier.IsExtendedAscii(normalAscii));
    Assert.True(ByteClassifier.IsExtendedAscii(extendedAscii));
  }

  /// <summary>
  /// Tests that ANSI escape sequences are detected correctly.
  /// </summary>
  [Fact]
  public void AnsiEscapeSequences_AreDetected_Correctly()
  {
    // Arrange - Text with ANSI colour code (ESC[31m = red)
    byte[] withAnsi = new byte[] 
    { 
      0x1B, 0x5B, 0x33, 0x31, 0x6D, // ESC[31m
      0x48, 0x65, 0x6C, 0x6C, 0x6F  // Hello
    };
    
    byte[] withoutAnsi = new byte[] 
    { 
      0x48, 0x65, 0x6C, 0x6C, 0x6F  // Hello
    };

    // Act
    TextAnalysis withAnalysis = TextAnalysis.Analyse(withAnsi, includeHistogram: false);
    TextAnalysis withoutAnalysis = TextAnalysis.Analyse(withoutAnsi, includeHistogram: false);

    // Assert
    Assert.True(withAnalysis.HasAnsiEscapes);
    Assert.False(withoutAnalysis.HasAnsiEscapes);
  }

  /// <summary>
  /// Tests that complex ANSI art with multiple character types works correctly.
  /// </summary>
  [Fact]
  public void ComplexAnsiArt_RoundTrips_Correctly()
  {
    // Arrange - Mix of box-drawing, blocks, and international chars
    byte[] complexArt = new byte[]
    {
      // Line 1: Double box with shading
      0xC9, 0xCD, 0xCD, 0xBB, 0x20, 0xDB, 0xB2, 0xB0, 0x0A,
      // Line 2: Vertical with text including e-acute (0x82)
      0xBA, 0x43, 0x61, 0x66, 0x82, 0xBA, 0x20, 0x20, 0x20, 0x0A, // "Caf" + e-acute
      // Line 3: Bottom with blocks
      0xC8, 0xCD, 0xCD, 0xBC, 0x20, 0xDC, 0xDE, 0xDF
    };

    // Act
    string decoded = Cp437Encoding.Decode(complexArt, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert - Perfect round-trip
    Assert.Equal(complexArt, reencoded);
    Assert.Contains("Caf", decoded); // Contains the cafe text
  }

  /// <summary>
  /// Tests that rendering doesn't corrupt QWK line terminators.
  /// </summary>
  [Fact]
  public void BoxDrawingWithQwkTerminators_PreservesBytes_Correctly()
  {
    // Arrange - Box with QWK line terminators (0xE3)
    byte[] qwkBox = new byte[]
    {
      0xDA, 0xC4, 0xBF, 0xE3,      // top + QWK terminator
      0xB3, 0x20, 0xB3, 0xE3,      // middle + QWK terminator
      0xC0, 0xC4, 0xD9, 0xE3       // bottom + QWK terminator
    };

    // Act
    string decoded = Cp437Encoding.Decode(qwkBox, DecoderFallbackPolicy.Strict);
    byte[] reencoded = Cp437Encoding.Encode(decoded, EncoderFallbackPolicy.Strict);

    // Assert - Byte-perfect preservation
    Assert.Equal(qwkBox, reencoded);
  }
}