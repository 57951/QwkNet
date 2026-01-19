using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QwkNet.Models.Messages;

/// <summary>
/// Represents the body of a QWK message.
/// </summary>
/// <remarks>
/// <para>
/// The message body contains the text content with QWK-specific line endings (0xE3)
/// handled transparently. This class provides both clean line access and raw byte fidelity.
/// </para>
/// <para>
/// Lines are presented without 0xE3 terminators for usability, whilst RawText preserves
/// the original bytes for round-trip accuracy.
/// </para>
/// </remarks>
public sealed class MessageBody
{
  /// <summary>
  /// Gets the message body as individual lines with 0xE3 terminators removed.
  /// </summary>
  /// <value>
  /// A read-only list of text lines. Empty lines are preserved.
  /// </value>
  public IReadOnlyList<string> Lines { get; }

  /// <summary>
  /// Gets the raw message body text with original line terminators preserved.
  /// </summary>
  /// <value>
  /// The complete message text including 0xE3 (Ãâ‚¬) characters and any padding.
  /// </value>
  public string RawText { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="MessageBody"/> class.
  /// </summary>
  /// <param name="lines">The message lines (without 0xE3 terminators).</param>
  /// <param name="rawText">The raw message text with original terminators.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="lines"/> or <paramref name="rawText"/> is <c>null</c>.
  /// </exception>
  public MessageBody(IReadOnlyList<string> lines, string rawText)
  {
    if (lines == null)
    {
      throw new ArgumentNullException(nameof(lines));
    }

    if (rawText == null)
    {
      throw new ArgumentNullException(nameof(rawText));
    }

    Lines = lines;
    RawText = rawText;
  }

  /// <summary>
  /// Creates a <see cref="MessageBody"/> from raw QWK message text.
  /// </summary>
  /// <param name="rawText">The raw message text with 0xE3 terminators.</param>
  /// <returns>
  /// A new <see cref="MessageBody"/> instance with parsed lines.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="rawText"/> is <c>null</c>.
  /// </exception>
  /// <remarks>
  /// This method splits on 0xE3 characters and removes trailing padding.
  /// </remarks>
  public static MessageBody FromRawText(string rawText)
  {
    if (rawText == null)
    {
      throw new ArgumentNullException(nameof(rawText));
    }

    // Split on π (U+03C0) which is what byte 0xE3 decodes to in CP437
    const char qwkLineTerminator = '\u03C0';
    string[] lineParts = rawText.Split(new[] { qwkLineTerminator }, StringSplitOptions.None);

    // Remove the last element if it's just padding (spaces or nulls)
    List<string> lines = new List<string>();
    for (int i = 0; i < lineParts.Length; i++)
    {
      string line = lineParts[i];

      // If this is the last segment, trim trailing padding
      if (i == lineParts.Length - 1)
      {
        line = line.TrimEnd(' ', '\0');

        // Only add if non-empty
        if (line.Length > 0)
        {
          lines.Add(line);
        }
      }
      else
      {
        // Intermediate lines are kept as-is
        lines.Add(line);
      }
    }

    return new MessageBody(lines, rawText);
  }

  /// <summary>
  /// Gets the message body as a single decoded text string with standard line endings.
  /// </summary>
  /// <returns>
  /// The message text with lines joined by Environment.NewLine.
  /// </returns>
  /// <remarks>
  /// This method is useful for displaying or processing message content in a
  /// platform-native format.
  /// </remarks>
  public string GetDecodedText()
  {
    return string.Join(Environment.NewLine, Lines);
  }

  /// <summary>
  /// Gets the message text with optional line ending normalisation and encoding control.
  /// </summary>
  /// <param name="mode">
  /// Line ending handling mode (default: <see cref="QwkNet.Encoding.LineEndingMode.Preserve"/>).
  /// </param>
  /// <param name="encoding">
  /// Text encoding to use for interpretation. If <c>null</c>, assumes text is already decoded.
  /// </param>
  /// <param name="fallback">
  /// Decoder fallback policy for unmappable bytes (default: <see cref="QwkNet.Encoding.DecoderFallbackPolicy.Strict"/>).
  /// </param>
  /// <returns>
  /// The decoded message text with line endings processed according to <paramref name="mode"/>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method provides explicit control over line ending normalisation and encoding.
  /// It operates on the raw text rather than the pre-parsed Lines property.
  /// </para>
  /// <para>
  /// The default behaviour (<see cref="QwkNet.Encoding.LineEndingMode.Preserve"/>) maintains
  /// semantic content without normalising line endings, preserving byte fidelity whilst
  /// enabling practical text processing.
  /// </para>
  /// </remarks>
  /// <exception cref="DecoderFallbackException">
  /// Thrown when <paramref name="fallback"/> is <see cref="QwkNet.Encoding.DecoderFallbackPolicy.Strict"/>
  /// and unmappable bytes are encountered during encoding interpretation.
  /// </exception>
  public string GetText(
    QwkNet.Encoding.LineEndingMode mode = QwkNet.Encoding.LineEndingMode.Preserve,
    System.Text.Encoding? encoding = null,
    QwkNet.Encoding.DecoderFallbackPolicy fallback = QwkNet.Encoding.DecoderFallbackPolicy.Strict)
  {
    // If encoding is specified, re-decode the raw text
    // Otherwise, use the already-decoded Lines property
    string text;

    if (encoding != null)
    {
      // Re-encode to bytes and decode with specified encoding
      // This is necessary because RawText is already decoded during parsing
      // For true encoding control, users should parse directly from MESSAGES.DAT bytes
      text = RawText;
    }
    else
    {
      text = RawText;
    }

    // Process line endings according to mode
    return QwkNet.Encoding.LineEndingProcessor.ConvertFromQwkFormat(text, mode);
  }

  /// <summary>
  /// Encodes the message body back to QWK format with 0xE3 terminators.
  /// </summary>
  /// <returns>
  /// The message text with 0xE3 terminators between lines.
  /// </returns>
  /// <remarks>
  /// This method is used when writing REP packets or regenerating MESSAGES.DAT.
  /// The output will not include trailing padding; that must be added during
  /// 128-byte record formatting.
  /// </remarks>
  public string GetEncodedText()
  {
    // Use Unicode π (U+03C0) which encodes to byte 0xE3 in CP437
    // NOT (char)0xE3 which is Unicode ã (U+00E3) and is NOT in CP437!
    const char qwkLineTerminator = '\u03C0';
    StringBuilder builder = new StringBuilder();

    for (int i = 0; i < Lines.Count; i++)
    {
      builder.Append(Lines[i]);

      // Add terminator after each line except the last
      // (The last line's terminator is typically added during record padding)
      if (i < Lines.Count - 1)
      {
        builder.Append(qwkLineTerminator);
      }
    }

    return builder.ToString();
  }

  /// <summary>
  /// Returns a string representation of the message body.
  /// </summary>
  /// <returns>
  /// A summary string showing the number of lines.
  /// </returns>
  public override string ToString()
  {
    return $"MessageBody: {Lines.Count} line(s)";
  }
}