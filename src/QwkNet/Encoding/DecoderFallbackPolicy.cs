namespace QwkNet.Encoding;

/// <summary>
/// Specifies how unmappable bytes should be handled when decoding QWK text to strings.
/// </summary>
/// <remarks>
/// <para>
/// QWK packets typically use CP437 encoding, but real-world packets may contain bytes
/// that don't map cleanly to Unicode or the specified encoding. This enum controls
/// how such bytes are handled.
/// </para>
/// <para>
/// Default behaviour is <see cref="Strict"/>, which throws an exception on unmappable bytes.
/// This prevents silent data corruption whilst ensuring callers are aware of encoding issues.
/// </para>
/// </remarks>
public enum DecoderFallbackPolicy
{
  /// <summary>
  /// Throw an exception when encountering unmappable bytes.
  /// </summary>
  /// <remarks>
  /// This is the strictest mode and the library default. It ensures no data is silently
  /// lost or corrupted during decoding. Callers must handle the exception and decide
  /// how to proceed (e.g., retry with a different encoding or fallback policy).
  /// </remarks>
  Strict = 0,

  /// <summary>
  /// Replace unmappable bytes with a question mark ('?') character.
  /// </summary>
  /// <remarks>
  /// This mode replaces any byte that cannot be decoded with ASCII '?' (0x3F).
  /// This is useful for displaying text to users where occasional replacement
  /// characters are acceptable. The replacement is visible and indicates data loss.
  /// </remarks>
  ReplacementQuestion = 1,

  /// <summary>
  /// Replace unmappable bytes with the Unicode replacement character (U+FFFD).
  /// </summary>
  /// <remarks>
  /// This mode replaces any byte that cannot be decoded with U+FFFD (�).
  /// This is the Unicode standard replacement character and is more semantically
  /// correct than '?' for encoding errors. Use this for Unicode-aware text processing.
  /// </remarks>
  ReplacementUnicode = 2,

  /// <summary>
  /// Best-effort decoding: attempt to decode within the specified encoding without cascading.
  /// </summary>
  /// <remarks>
  /// This mode uses the encoding's own best-fit behaviour to handle unmappable bytes.
  /// It does not cascade through multiple encodings (e.g., CP437 → Latin-1 → UTF-8),
  /// as that would introduce non-deterministic behaviour. The specific replacement
  /// strategy depends on the encoding being used.
  /// </remarks>
  BestEffort = 3
}
