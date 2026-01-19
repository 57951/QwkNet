using System;

namespace QwkNet.Encoding;

/// <summary>
/// Provides static methods for classifying individual bytes in QWK message content.
/// </summary>
/// <remarks>
/// <para>
/// This class operates on raw bytes rather than decoded characters, as QWK packets
/// are fundamentally byte streams. Classification helps identify the presence of
/// extended ASCII, box-drawing characters, and control codes without making assumptions
/// about the encoding.
/// </para>
/// <para>
/// All methods are byte-based for maximum fidelity to the original file format.
/// </para>
/// </remarks>
public static class ByteClassifier
{
  /// <summary>
  /// Determines whether a byte represents an extended ASCII character (high bit set).
  /// </summary>
  /// <param name="b">The byte to classify.</param>
  /// <returns>
  /// <c>true</c> if the byte value is 128 or greater (0x80-0xFF); otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// Extended ASCII refers to any byte with the high bit set (bit 7 = 1). This includes
  /// CP437 box-drawing characters, accented letters, and other non-ASCII symbols commonly
  /// found in BBS messages.
  /// </remarks>
  public static bool IsExtendedAscii(byte b)
  {
    return b >= 0x80;
  }

  /// <summary>
  /// Determines whether a byte represents a CP437 box-drawing character.
  /// </summary>
  /// <param name="b">The byte to classify.</param>
  /// <returns>
  /// <c>true</c> if the byte is in a CP437 box-drawing range; otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Box-drawing characters in CP437 are primarily in these ranges:
  /// </para>
  /// <list type="bullet">
  ///   <item><description>0xB0-0xDF: Box-drawing and block elements</description></item>
  ///   <item><description>0xC4, 0xB3: Horizontal and vertical lines</description></item>
  ///   <item><description>0xC0, 0xD9, 0xDA, 0xBF: Corner pieces</description></item>
  /// </list>
  /// <para>
  /// This detection is based on CP437 byte values and does not require decoding to Unicode.
  /// Presence of these bytes may suggest CP437-origin content but does not guarantee it,
  /// as other encodings may reuse these byte values for different characters.
  /// </para>
  /// </remarks>
  public static bool IsBoxDrawing(byte b)
  {
    // Primary box-drawing and block element range
    if (b >= 0xB0 && b <= 0xDF)
    {
      return true;
    }

    // Additional line-drawing characters scattered throughout upper range
    // Single-line box drawing: ─ │ ┌ ┐ └ ┘ ├ ┤ ┬ ┴ ┼
    // These are in the 0xC0-0xC5, 0xB3-0xB4, 0xDA-0xBF ranges
    if (b >= 0xC0 && b <= 0xC5)
    {
      return true;
    }

    if (b >= 0xDA && b <= 0xBF)
    {
      return true;
    }

    // Vertical line variations
    if (b == 0xB3 || b == 0xBA)
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Determines whether a byte represents a CP437 line graphics character.
  /// </summary>
  /// <param name="b">The byte to classify.</param>
  /// <returns>
  /// <c>true</c> if the byte is a CP437 line graphics character; otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Line graphics are a subset of box-drawing characters, specifically the simple
  /// horizontal and vertical line segments and corners. This includes:
  /// </para>
  /// <list type="bullet">
  ///   <item><description>0xC4: Horizontal line (─)</description></item>
  ///   <item><description>0xB3: Vertical line (│)</description></item>
  ///   <item><description>0xDA, 0xBF, 0xC0, 0xD9: Corner pieces</description></item>
  ///   <item><description>0xC2, 0xC1, 0xB4, 0xC3: T-junctions</description></item>
  ///   <item><description>0xC5: Cross (+)</description></item>
  /// </list>
  /// <para>
  /// This is a more conservative classification than <see cref="IsBoxDrawing"/>, focusing
  /// on the most common line-drawing characters used in BBS menus and borders.
  /// </para>
  /// </remarks>
  public static bool IsLineGraphics(byte b)
  {
    // Simple line segments
    if (b == 0xC4 || b == 0xB3 || b == 0xBA || b == 0xCD)
    {
      return true;
    }

    // Corners
    if (b == 0xDA || b == 0xBF || b == 0xC0 || b == 0xD9 ||
        b == 0xC9 || b == 0xBB || b == 0xC8 || b == 0xBC)
    {
      return true;
    }

    // T-junctions and cross
    if (b == 0xC2 || b == 0xC1 || b == 0xB4 || b == 0xC3 || b == 0xC5 ||
        b == 0xD1 || b == 0xCF || b == 0xB5 || b == 0xC6 || b == 0xD8)
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Determines whether a byte represents a control character.
  /// </summary>
  /// <param name="b">The byte to classify.</param>
  /// <returns>
  /// <c>true</c> if the byte is a control character (0x00-0x1F or 0x7F); otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Control characters are non-printable characters in the ASCII range 0-31 and 127 (DEL).
  /// This includes common controls like TAB (0x09), LF (0x0A), CR (0x0D), and ESC (0x1B).
  /// </para>
  /// <para>
  /// Note that 0xE3 (π in CP437) is not classified as a control character here, even though
  /// it's used as a line separator in QWK format. It's in the extended ASCII range and is
  /// treated as a printable character outside of QWK-specific parsing.
  /// </para>
  /// </remarks>
  public static bool IsControlCharacter(byte b)
  {
    return b < 0x20 || b == 0x7F;
  }

  /// <summary>
  /// Determines whether a byte represents a printable ASCII character.
  /// </summary>
  /// <param name="b">The byte to classify.</param>
  /// <returns>
  /// <c>true</c> if the byte is in the printable ASCII range (0x20-0x7E); otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// Printable ASCII includes space (0x20) through tilde (0x7E). This excludes control
  /// characters and extended ASCII. This is useful for identifying purely ASCII text content.
  /// </remarks>
  public static bool IsPrintableAscii(byte b)
  {
    return b >= 0x20 && b <= 0x7E;
  }
}
