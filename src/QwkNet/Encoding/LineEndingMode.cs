namespace QwkNet.Encoding;

/// <summary>
/// Specifies how line endings should be handled when processing QWK message text.
/// </summary>
/// <remarks>
/// <para>
/// QWK format uses 0xE3 (π character) as the line separator in message bodies.
/// This enum controls how these separators are processed when reading message text.
/// </para>
/// <para>
/// Default behaviour is <see cref="Preserve"/>, which maintains the semantic content
/// without normalising line endings. This preserves byte fidelity whilst allowing
/// practical text processing.
/// </para>
/// </remarks>
public enum LineEndingMode
{
  /// <summary>
  /// Preserve the original QWK line endings (0xE3) semantically without normalisation.
  /// </summary>
  /// <remarks>
  /// In this mode, 0xE3 characters are interpreted as line breaks for the Lines property,
  /// but mixed line endings (CR/LF) from malformed packets are preserved as literal characters.
  /// This is the default and most faithful mode for preservation-grade processing.
  /// </remarks>
  Preserve = 0,

  /// <summary>
  /// Normalise all line endings to LF (\n) format.
  /// </summary>
  /// <remarks>
  /// Converts QWK 0xE3 separators to \n, and also normalises any CR/LF or CR sequences
  /// found in the message text. Useful for Unix-style text processing.
  /// </remarks>
  NormaliseToLf = 1,

  /// <summary>
  /// Normalise all line endings to CRLF (\r\n) format.
  /// </summary>
  /// <remarks>
  /// Converts QWK 0xE3 separators to \r\n, and also normalises any LF or CR sequences
  /// found in the message text. Useful for Windows-style text processing or RFC compliance.
  /// </remarks>
  NormaliseToCrLf = 2,

  /// <summary>
  /// Strict QWK mode: only 0xE3 characters are treated as line breaks.
  /// </summary>
  /// <remarks>
  /// In this mode, CR and LF characters are preserved as literal characters in the text.
  /// This mode is useful for forensic analysis or when maximum fidelity to the original
  /// byte stream is required. Only 0xE3 is recognised as a line separator.
  /// </remarks>
  StrictQwk = 3
}

/// <summary>
/// Provides extension methods for <see cref="LineEndingMode"/> to support American English spellings.
/// </summary>
public static class LineEndingModeExtensions
{
  /// <summary>
  /// American spelling alias for NormaliseToLf.
  /// </summary>
  public const LineEndingMode NormalizeToLf = LineEndingMode.NormaliseToLf;

  /// <summary>
  /// American spelling alias for NormaliseToCrLf.
  /// </summary>
  public const LineEndingMode NormalizeToCrLf = LineEndingMode.NormaliseToCrLf;
}
