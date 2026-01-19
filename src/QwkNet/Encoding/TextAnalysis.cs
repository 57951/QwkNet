using System;
using System.Collections.Generic;

namespace QwkNet.Encoding;

/// <summary>
/// Contains the results of analysing byte content in QWK message text.
/// </summary>
/// <remarks>
/// <para>
/// This structure reports objective properties of byte sequences without inferring encodings
/// or making assumptions about the content. It simply describes what bytes are present.
/// </para>
/// <para>
/// Use <see cref="Analyse(ReadOnlySpan{byte}, bool)"/> to generate an analysis of QWK message content.
/// </para>
/// </remarks>
public readonly struct TextAnalysis
{
  /// <summary>
  /// Gets a value indicating whether the content contains any high-bit bytes (>= 0x80).
  /// </summary>
  /// <value>
  /// <c>true</c> if at least one byte has the high bit set; otherwise, <c>false</c>.
  /// </value>
  public bool ContainsHighBitBytes { get; }

  /// <summary>
  /// Gets the count of high-bit bytes (>= 0x80) in the content.
  /// </summary>
  /// <value>
  /// The number of bytes with values from 0x80 to 0xFF.
  /// </value>
  public int HighBitByteCount { get; }

  /// <summary>
  /// Gets a value indicating whether the content contains CP437 box-drawing bytes.
  /// </summary>
  /// <value>
  /// <c>true</c> if at least one box-drawing byte is present; otherwise, <c>false</c>.
  /// </value>
  /// <remarks>
  /// Presence of CP437 box-drawing byte values may suggest CP437-origin content,
  /// but this library does not infer encodings. Other encodings may use the same
  /// byte values for different characters.
  /// </remarks>
  public bool HasBoxDrawingBytes { get; }

  /// <summary>
  /// Gets the count of CP437 box-drawing bytes in the content.
  /// </summary>
  /// <value>
  /// The number of bytes classified as CP437 box-drawing characters.
  /// </value>
  public int BoxDrawingByteCount { get; }

  /// <summary>
  /// Gets a value indicating whether the content contains ANSI escape sequences.
  /// </summary>
  /// <value>
  /// <c>true</c> if at least one ANSI escape sequence is detected; otherwise, <c>false</c>.
  /// </value>
  /// <remarks>
  /// This is a basic detection of ESC character (0x1B) followed by '['. More sophisticated
  /// ANSI sequence detection is available in the QwkNet.Encoding.Ansi namespace.
  /// </remarks>
  public bool HasAnsiEscapes { get; }

  /// <summary>
  /// Gets the byte histogram if requested, or <c>null</c> if not generated.
  /// </summary>
  /// <value>
  /// A dictionary mapping byte values (0-255) to their occurrence counts, or <c>null</c>
  /// if the histogram was not requested during analysis.
  /// </value>
  /// <remarks>
  /// The histogram is expensive to generate and allocate, so it's only created when
  /// explicitly requested via the <c>includeHistogram</c> parameter in <see cref="Analyse"/>.
  /// </remarks>
  public IReadOnlyDictionary<byte, int>? ByteHistogram { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="TextAnalysis"/> struct.
  /// </summary>
  /// <param name="containsHighBitBytes">Whether high-bit bytes are present.</param>
  /// <param name="highBitByteCount">Count of high-bit bytes.</param>
  /// <param name="hasBoxDrawingBytes">Whether box-drawing bytes are present.</param>
  /// <param name="boxDrawingByteCount">Count of box-drawing bytes.</param>
  /// <param name="hasAnsiEscapes">Whether ANSI escape sequences are present.</param>
  /// <param name="byteHistogram">Optional byte histogram.</param>
  public TextAnalysis(
    bool containsHighBitBytes,
    int highBitByteCount,
    bool hasBoxDrawingBytes,
    int boxDrawingByteCount,
    bool hasAnsiEscapes,
    IReadOnlyDictionary<byte, int>? byteHistogram)
  {
    ContainsHighBitBytes = containsHighBitBytes;
    HighBitByteCount = highBitByteCount;
    HasBoxDrawingBytes = hasBoxDrawingBytes;
    BoxDrawingByteCount = boxDrawingByteCount;
    HasAnsiEscapes = hasAnsiEscapes;
    ByteHistogram = byteHistogram;
  }

  /// <summary>
  /// Analyses a byte sequence and generates a report of its properties.
  /// </summary>
  /// <param name="bytes">The byte sequence to analyse.</param>
  /// <param name="includeHistogram">
  /// Whether to generate a byte histogram. Default is <c>false</c> to avoid allocations.
  /// </param>
  /// <returns>
  /// A <see cref="TextAnalysis"/> containing the analysis results.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method performs a single pass over the byte sequence to collect statistics.
  /// It reports objective properties without making encoding inferences.
  /// </para>
  /// <para>
  /// The histogram is expensive (allocates a dictionary) and should only be requested
  /// when detailed byte distribution analysis is required, such as for validation
  /// or diagnostic purposes.
  /// </para>
  /// </remarks>
  public static TextAnalysis Analyse(ReadOnlySpan<byte> bytes, bool includeHistogram = false)
  {
    int highBitCount = 0;
    int boxDrawingCount = 0;
    bool hasAnsi = false;

    Dictionary<byte, int>? histogram = includeHistogram ? new Dictionary<byte, int>() : null;

    for (int i = 0; i < bytes.Length; i++)
    {
      byte b = bytes[i];

      // Update histogram if requested
      if (histogram != null)
      {
        if (histogram.ContainsKey(b))
        {
          histogram[b]++;
        }
        else
        {
          histogram[b] = 1;
        }
      }

      // Count high-bit bytes
      if (ByteClassifier.IsExtendedAscii(b))
      {
        highBitCount++;
      }

      // Count box-drawing bytes
      if (ByteClassifier.IsBoxDrawing(b))
      {
        boxDrawingCount++;
      }

      // Detect ANSI escape sequences (ESC followed by '[')
      if (!hasAnsi && b == 0x1B && i + 1 < bytes.Length && bytes[i + 1] == (byte)'[')
      {
        hasAnsi = true;
      }
    }

    return new TextAnalysis(
      containsHighBitBytes: highBitCount > 0,
      highBitByteCount: highBitCount,
      hasBoxDrawingBytes: boxDrawingCount > 0,
      boxDrawingByteCount: boxDrawingCount,
      hasAnsiEscapes: hasAnsi,
      byteHistogram: histogram);
  }

  /// <summary>
  /// Returns a string representation of the analysis results.
  /// </summary>
  /// <returns>
  /// A summary string describing the byte content properties.
  /// </returns>
  public override string ToString()
  {
    return $"TextAnalysis: HighBit={HighBitByteCount}, BoxDrawing={BoxDrawingByteCount}, " +
           $"ANSI={HasAnsiEscapes}, Histogram={(ByteHistogram != null ? "Yes" : "No")}";
  }
}
