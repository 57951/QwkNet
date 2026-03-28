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
  /// The complete message text including π (U+03C0 / byte 0xE3 in CP437) characters and any padding.
  /// This is the CP437-decoded string representation. For byte-level access, see <see cref="RawBytes"/>.
  /// </value>
  public string RawText { get; }

  /// <summary>
  /// Gets the raw, undecoded body bytes as read from the QWK archive.
  /// </summary>
  /// <value>
  /// The concatenated 128-byte body blocks for this message, exactly as stored in MESSAGES.DAT.
  /// This is <see cref="ReadOnlyMemory{T}.Empty"/> for bodies created via
  /// <see cref="FromRawText"/>, or the two-argument constructor — only bodies
  /// produced by <see cref="QwkPacket.Open(string, QwkNet.Validation.ValidationMode)"/> carry live byte data.
  /// </value>
  /// <remarks>
  /// Use <see cref="GetText(QwkNet.Encoding.LineEndingMode, System.Text.Encoding, QwkNet.Encoding.DecoderFallbackPolicy)"/>
  /// with a non-null encoding to decode these bytes with an encoding other than CP437.
  /// </remarks>
  public ReadOnlyMemory<byte> RawBytes { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="MessageBody"/> class.
  /// </summary>
  /// <param name="lines">The message lines (without 0xE3 terminators).</param>
  /// <param name="rawText">The raw message text with original terminators.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="lines"/> or <paramref name="rawText"/> is <c>null</c>.
  /// </exception>
  /// <remarks>
  /// Bodies created via this constructor have <see cref="RawBytes"/> = <see cref="ReadOnlyMemory{T}.Empty"/>.
  /// To obtain a body with live byte data, open a packet via <see cref="QwkPacket.Open(string, QwkNet.Validation.ValidationMode)"/>.
  /// </remarks>
  public MessageBody(IReadOnlyList<string> lines, string rawText)
    : this(lines, rawText, ReadOnlyMemory<byte>.Empty)
  {
  }

  /// <summary>
  /// Initialises a new instance of the <see cref="MessageBody"/> class with raw body bytes.
  /// </summary>
  /// <param name="lines">The message lines (without 0xE3 terminators), decoded from CP437.</param>
  /// <param name="rawText">The raw message text with original terminators, decoded from CP437.</param>
  /// <param name="rawBytes">
  /// The concatenated raw body blocks as read from MESSAGES.DAT before any decoding.
  /// Pass <see cref="ReadOnlyMemory{T}.Empty"/> when byte-level access is not required.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="lines"/> or <paramref name="rawText"/> is <c>null</c>.
  /// </exception>
  public MessageBody(IReadOnlyList<string> lines, string rawText, ReadOnlyMemory<byte> rawBytes)
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
    RawBytes = rawBytes;
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
  /// <para>
  /// The text encoding used to decode the raw body bytes. When <c>null</c> (default), the
  /// method uses the <strong>legacy path</strong>: it operates on the CP437-decoded
  /// <see cref="RawText"/> string and applies <see cref="QwkNet.Encoding.LineEndingProcessor.ConvertFromQwkFormat"/>.
  /// This preserves backward compatibility with all existing callers.
  /// </para>
  /// <para>
  /// When non-null, the method uses the <strong>raw-bytes path</strong>: it decodes
  /// <see cref="RawBytes"/> using this encoding, splitting on byte <c>0xE3</c> (or CR/CRLF
  /// for QWKE bodies). <see cref="RawBytes"/> must be non-empty; if it is empty (e.g. the body
  /// was created programmatically via <see cref="FromRawText"/> or <see cref="MessageBuilder.SetBodyText"/>),
  /// an <see cref="InvalidOperationException"/> is thrown. Open packets via
  /// <see cref="QwkPacket.Open(string, QwkNet.Validation.ValidationMode)"/> to obtain bodies with live byte data.
  /// </para>
  /// </param>
  /// <param name="fallback">
  /// Decoder fallback policy for unmappable bytes (default: <see cref="QwkNet.Encoding.DecoderFallbackPolicy.Strict"/>).
  /// Only used when <paramref name="encoding"/> is non-null.
  /// </param>
  /// <returns>
  /// <para>
  /// When <paramref name="encoding"/> is <c>null</c>: the CP437-decoded text with QWK π (0xE3)
  /// terminators converted to standard line endings per <paramref name="mode"/>.
  /// Equivalent to <see cref="GetDecodedText"/> when <paramref name="mode"/> is
  /// <see cref="QwkNet.Encoding.LineEndingMode.Preserve"/>.
  /// </para>
  /// <para>
  /// When <paramref name="encoding"/> is non-null: each body line decoded from
  /// <see cref="RawBytes"/> using <paramref name="encoding"/>, joined by the
  /// line separator implied by <paramref name="mode"/> (<see cref="Environment.NewLine"/> for
  /// <see cref="QwkNet.Encoding.LineEndingMode.Preserve"/> and
  /// <see cref="QwkNet.Encoding.LineEndingMode.StrictQwk"/>,
  /// <c>"\n"</c> for <see cref="QwkNet.Encoding.LineEndingMode.NormaliseToLf"/>,
  /// <c>"\r\n"</c> for <see cref="QwkNet.Encoding.LineEndingMode.NormaliseToCrLf"/>).
  /// </para>
  /// </returns>
  /// <exception cref="DecoderFallbackException">
  /// Thrown when <paramref name="encoding"/> is non-null,
  /// <paramref name="fallback"/> is <see cref="QwkNet.Encoding.DecoderFallbackPolicy.Strict"/>,
  /// and <see cref="RawBytes"/> contains bytes that are invalid for the specified encoding.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <paramref name="encoding"/> is non-null but <see cref="RawBytes"/> is empty
  /// (i.e. the body was not loaded from an archive). Open the packet via
  /// <see cref="QwkPacket.Open(string, QwkNet.Validation.ValidationMode)"/> to obtain bodies with live byte data.
  /// </exception>
  public string GetText(
    QwkNet.Encoding.LineEndingMode mode = QwkNet.Encoding.LineEndingMode.Preserve,
    System.Text.Encoding? encoding = null,
    QwkNet.Encoding.DecoderFallbackPolicy fallback = QwkNet.Encoding.DecoderFallbackPolicy.Strict)
  {
    if (encoding == null)
    {
      // Legacy path: operate on the already-CP437-decoded RawText string.
      // This preserves backward compatibility for all existing callers.
      return QwkNet.Encoding.LineEndingProcessor.ConvertFromQwkFormat(RawText, mode);
    }

    // Raw-bytes path: decode from the original archive bytes so that byte-accurate
    // line splitting and the full encoding round-trip are both correct.
    if (RawBytes.IsEmpty)
    {
      throw new InvalidOperationException(
        "RawBytes is empty. GetText() with a non-null encoding requires raw body bytes, " +
        "which are only available on bodies loaded via QwkPacket.Open(). " +
        "Bodies created with MessageBody.FromRawText() or MessageBuilder.SetBodyText() " +
        "do not carry raw byte data.");
    }

    // Use the parser to split on 0xE3 (or CR/CRLF for QWKE) and decode each line
    // with the requested encoding and fallback policy.
    List<string> decodedLines = QwkNet.Core.MessageBodyParser.ParseLines(
      RawBytes.Span, encoding, fallback);

    // Choose the join separator based on the requested line-ending mode.
    string separator = mode switch
    {
      QwkNet.Encoding.LineEndingMode.NormaliseToLf => "\n",
      QwkNet.Encoding.LineEndingMode.NormaliseToCrLf => "\r\n",
      _ => Environment.NewLine
    };

    return string.Join(separator, decodedLines);
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