namespace QwkNet.Encoding;

/// <summary>
/// Specifies how unmappable characters should be handled when encoding strings to QWK text bytes.
/// </summary>
/// <remarks>
/// <para>
/// When encoding Unicode strings to QWK format (typically CP437), some characters may not
/// have a valid representation in the target encoding. This enum controls how such characters
/// are handled during encoding.
/// </para>
/// <para>
/// Default behaviour is <see cref="Strict"/>, which throws an exception on unmappable characters.
/// This prevents silent data corruption and ensures callers are aware of encoding limitations.
/// </para>
/// </remarks>
public enum EncoderFallbackPolicy
{
  /// <summary>
  /// Throw an exception when encountering unmappable characters.
  /// </summary>
  /// <remarks>
  /// This is the strictest mode and the library default. It ensures no data is silently
  /// lost or corrupted during encoding. Callers must handle the exception and decide
  /// how to proceed (e.g., use a different encoding or fallback policy).
  /// </remarks>
  Strict = 0,

  /// <summary>
  /// Replace unmappable characters with a question mark ('?') byte (0x3F).
  /// </summary>
  /// <remarks>
  /// This mode replaces any character that cannot be encoded with ASCII '?' (0x3F).
  /// This is useful when lossy encoding is acceptable and replacement characters provide
  /// a visible indication of data loss. This is the most common fallback for legacy systems.
  /// </remarks>
  ReplacementQuestion = 1
}
