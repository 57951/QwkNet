using System;
using System.Collections.Generic;
using System.Text;

namespace QwkNet.Encoding;

/// <summary>
/// Provides static methods for processing QWK line endings and normalising text.
/// </summary>
/// <remarks>
/// <para>
/// QWK format uses 0xE3 (π character in CP437) as the line separator in message bodies.
/// This class handles conversion between QWK format and standard line endings whilst
/// preserving the original content where possible.
/// </para>
/// <para>
/// Default behaviour is preservation-first: only explicit normalisation requests modify
/// the line ending format. This prevents silent data changes whilst enabling practical
/// text processing.
/// </para>
/// </remarks>
public static class LineEndingProcessor
{
  /// <summary>
  /// The QWK line terminator character for ASCII/Latin-1 byte identity (0xE3).
  /// </summary>
  /// <remarks>
  /// This is character U+00E3 (ã) which has byte value 0xE3 in ASCII/Latin-1.
  /// For proper CP437 encoding, use <see cref="QwkLineTerminatorCp437"/> instead.
  /// </remarks>
  public const char QwkLineTerminator = (char)0xE3;

  /// <summary>
  /// The QWK line terminator character for CP437 encoding (π).
  /// </summary>
  /// <remarks>
  /// This is character U+03C0 (π) which encodes to byte 0xE3 in CP437.
  /// This is the correct character to use when working with proper CP437 encoding.
  /// </remarks>
  public const char QwkLineTerminatorCp437 = '\u03C0';

  /// <summary>
  /// Converts QWK format text to a string with the specified line ending mode.
  /// </summary>
  /// <param name="text">The QWK message text containing 0xE3 separators.</param>
  /// <param name="mode">The line ending processing mode.</param>
  /// <returns>
  /// A string with line endings processed according to <paramref name="mode"/>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method assumes the input is already decoded to a string (e.g., from CP437).
  /// For byte-level processing, use the overload that accepts a byte span.
  /// </para>
  /// <para>
  /// In <see cref="LineEndingMode.Preserve"/> mode, 0xE3 characters are converted to
  /// the platform's native line ending (Environment.NewLine), but other line ending
  /// sequences are left unchanged.
  /// </para>
  /// </remarks>
  public static string ConvertFromQwkFormat(string text, LineEndingMode mode = LineEndingMode.Preserve)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    return mode switch
    {
      LineEndingMode.Preserve => ConvertQwkToNative(text),
      LineEndingMode.NormaliseToLf => NormaliseToLf(ConvertQwkToNative(text)),
      LineEndingMode.NormaliseToCrLf => NormaliseToCrLf(ConvertQwkToNative(text)),
      LineEndingMode.StrictQwk => ConvertQwkToNativeStrict(text),
      _ => throw new ArgumentException($"Unsupported line ending mode: {mode}", nameof(mode))
    };
  }

  /// <summary>
  /// Converts standard text to QWK format with 0xE3 separators.
  /// </summary>
  /// <param name="text">The text with standard line endings.</param>
  /// <param name="forCp437">
  /// If <c>true</c>, uses π (U+03C0) which encodes to 0xE3 in CP437.
  /// If <c>false</c>, uses character 0xE3 (ã) for ASCII/Latin-1 byte identity.
  /// Default is <c>false</c> for backward compatibility.
  /// </param>
  /// <returns>
  /// The text with line endings converted to the appropriate QWK terminator character.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method converts any line ending sequence (CRLF, LF, CR) to the QWK separator character.
  /// This is used when creating REP packets or writing MESSAGES.DAT.
  /// </para>
  /// <para>
  /// The choice of character depends on your encoding:
  /// - For proper CP437 encoding: use <paramref name="forCp437"/> = <c>true</c> (π → 0xE3)
  /// - For ASCII/Latin-1 byte identity: use <paramref name="forCp437"/> = <c>false</c> (char 0xE3)
  /// </para>
  /// <para>
  /// The output does not include trailing padding; that must be added during 128-byte
  /// record formatting.
  /// </para>
  /// </remarks>
  public static string ConvertToQwkFormat(string text, bool forCp437 = false)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    // Choose the correct terminator character based on encoding
    // CP437: byte 0xE3 = π (U+03C0)
    // ASCII/Latin-1: byte 0xE3 = ã (U+00E3) via byte identity
    string terminator = forCp437 ? "\u03C0" : "\u00E3";

    // Replace all line ending variants with QWK terminator
    // Process in order: CRLF first (so we don't double-convert), then CR, then LF
    string result = text.Replace("\r\n", terminator);
    result = result.Replace("\r", terminator);
    result = result.Replace("\n", terminator);

    return result;
  }

  /// <summary>
  /// Converts standard text to QWK format bytes with 0xE3 separators.
  /// </summary>
  /// <param name="text">The text with standard line endings.</param>
  /// <param name="encoding">The encoding to use. If <c>null</c>, CP437 is used.</param>
  /// <returns>
  /// A byte array containing the QWK-formatted text.
  /// </returns>
  /// <remarks>
  /// This method combines line ending conversion and encoding in a single operation.
  /// When using CP437 encoding (default), the proper π (U+03C0) character is used.
  /// </remarks>
  public static byte[] ConvertToQwkFormatBytes(string text, System.Text.Encoding? encoding = null)
  {
    if (string.IsNullOrEmpty(text))
    {
      return Array.Empty<byte>();
    }

    // Determine if we're using CP437 to choose the right character
    System.Text.Encoding enc = encoding ?? Cp437Encoding.GetEncoding();
    bool isCp437 = enc.CodePage == 437;

    string qwkText = ConvertToQwkFormat(text, forCp437: isCp437);
    return enc.GetBytes(qwkText);
  }

  /// <summary>
  /// Normalises all line endings in text to LF (\n) format.
  /// </summary>
  /// <param name="text">The text to normalise.</param>
  /// <returns>
  /// Text with all line endings converted to LF.
  /// </returns>
  /// <remarks>
  /// This method converts CRLF and CR sequences to LF. It's used for Unix-style
  /// text processing.
  /// </remarks>
  public static string NormaliseToLf(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    // Replace CRLF with LF first to avoid double-conversion
    string result = text.Replace("\r\n", "\n");
    // Replace remaining CR with LF
    result = result.Replace("\r", "\n");

    return result;
  }

  /// <summary>
  /// American spelling alias for <see cref="NormaliseToLf"/>.
  /// </summary>
  public static string NormalizeToLf(string text) => NormaliseToLf(text);

  /// <summary>
  /// Normalises all line endings in text to CRLF (\r\n) format.
  /// </summary>
  /// <param name="text">The text to normalise.</param>
  /// <returns>
  /// Text with all line endings converted to CRLF.
  /// </returns>
  /// <remarks>
  /// This method converts LF and CR sequences to CRLF. It's used for Windows-style
  /// text processing or RFC-compliant formats.
  /// </remarks>
  public static string NormaliseToCrLf(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    // First, normalise everything to LF to avoid double-conversion
    string normalized = NormaliseToLf(text);
    // Then convert all LF to CRLF
    return normalized.Replace("\n", "\r\n");
  }

  /// <summary>
  /// American spelling alias for <see cref="NormaliseToCrLf"/>.
  /// </summary>
  public static string NormalizeToCrLf(string text) => NormaliseToCrLf(text);

  /// <summary>
  /// Splits text on QWK line terminators (0xE3) into individual lines.
  /// </summary>
  /// <param name="text">The QWK-formatted text.</param>
  /// <param name="removeEmpty">Whether to remove empty lines. Default is <c>false</c>.</param>
  /// <returns>
  /// An array of lines with 0xE3 terminators removed.
  /// </returns>
  /// <remarks>
  /// This method is used internally for parsing QWK message bodies. It preserves
  /// empty lines by default to maintain message structure.
  /// </remarks>
  public static string[] SplitOnQwkTerminator(string text, bool removeEmpty = false)
  {
    if (string.IsNullOrEmpty(text))
    {
      return Array.Empty<string>();
    }

    StringSplitOptions options = removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
    return text.Split(new[] { QwkLineTerminator }, options);
  }

  /// <summary>
  /// Joins lines with QWK line terminators (0xE3).
  /// </summary>
  /// <param name="lines">The lines to join.</param>
  /// <returns>
  /// A string with lines separated by 0xE3 characters.
  /// </returns>
  /// <remarks>
  /// This method does not add a trailing 0xE3 after the last line. Trailing terminators
  /// should be added during 128-byte record formatting if needed.
  /// </remarks>
  public static string JoinWithQwkTerminator(IEnumerable<string> lines)
  {
    if (lines == null)
    {
      throw new ArgumentNullException(nameof(lines));
    }

    return string.Join(QwkLineTerminator.ToString(), lines);
  }

  /// <summary>
  /// Converts QWK 0xE3 terminators to platform-native line endings.
  /// </summary>
  /// <param name="text">Text containing QWK terminators.</param>
  /// <returns>
  /// Text with QWK terminators replaced by Environment.NewLine.
  /// </returns>
  /// <remarks>
  /// This is used in <see cref="LineEndingMode.Preserve"/> mode to convert QWK format
  /// to native format without normalising other line endings.
  /// </remarks>
  private static string ConvertQwkToNative(string text)
  {
    return text.Replace(QwkLineTerminator.ToString(), Environment.NewLine);
  }

  /// <summary>
  /// Converts QWK 0xE3 terminators to platform-native line endings in strict mode.
  /// </summary>
  /// <param name="text">Text containing QWK terminators.</param>
  /// <returns>
  /// Text with only QWK terminators replaced; CR/LF are left as literal characters.
  /// </returns>
  /// <remarks>
  /// This is used in <see cref="LineEndingMode.StrictQwk"/> mode for forensic analysis
  /// where CR and LF should be preserved as literal content rather than line breaks.
  /// </remarks>
  private static string ConvertQwkToNativeStrict(string text)
  {
    // In strict mode, only convert QWK terminators
    // CR and LF remain as literal characters in the output
    return text.Replace(QwkLineTerminator.ToString(), Environment.NewLine);
  }
}