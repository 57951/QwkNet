using System;
using System.Text;

namespace QwkNet.Encoding;

/// <summary>
/// Provides static methods for encoding and decoding text using the CP437 code page.
/// </summary>
/// <remarks>
/// <para>
/// CP437 (DOS Latin US) is the historical character encoding used by DOS and many BBS systems.
/// It's the de facto standard for QWK message packets, though some systems use other encodings.
/// </para>
/// <para>
/// This class provides explicit control over fallback behaviour when unmappable bytes or
/// characters are encountered. By default, operations use <see cref="DecoderFallbackPolicy.Strict"/>
/// mode to prevent silent data loss.
/// </para>
/// <para>
/// This library preserves byte fidelity by default. Encoding conversion is an explicit,
/// opt-in operation that may be lossy depending on fallback policy.
/// </para>
/// </remarks>
public static class Cp437Encoding
{
  private static readonly System.Text.Encoding _cp437;

  /// <summary>
  /// Initialises the CP437 encoding instance.
  /// </summary>
  static Cp437Encoding()
  {
    // Register the code page provider to ensure CP437 is available
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    _cp437 = System.Text.Encoding.GetEncoding(437);
  }

  /// <summary>
  /// Decodes a byte sequence to a string using CP437 encoding.
  /// </summary>
  /// <param name="bytes">The byte sequence to decode.</param>
  /// <param name="fallbackPolicy">
  /// The fallback policy for unmappable bytes. Default is <see cref="DecoderFallbackPolicy.Strict"/>.
  /// </param>
  /// <returns>
  /// The decoded string.
  /// </returns>
  /// <exception cref="DecoderFallbackException">
  /// Thrown when <paramref name="fallbackPolicy"/> is <see cref="DecoderFallbackPolicy.Strict"/>
  /// and unmappable bytes are encountered.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method is allocation-efficient using <see cref="ReadOnlySpan{T}"/>.
  /// </para>
  /// <para>
  /// In Strict mode, any byte that cannot be decoded will cause an exception. This prevents
  /// silent data corruption but requires the caller to handle encoding issues explicitly.
  /// </para>
  /// </remarks>
  public static string Decode(ReadOnlySpan<byte> bytes, DecoderFallbackPolicy fallbackPolicy = DecoderFallbackPolicy.Strict)
  {
    if (bytes.Length == 0)
    {
      return string.Empty;
    }

    System.Text.Encoding encoding = GetEncodingForFallbackPolicy(fallbackPolicy);
    return encoding.GetString(bytes);
  }

  /// <summary>
  /// Encodes a string to bytes using CP437 encoding.
  /// </summary>
  /// <param name="text">The text to encode.</param>
  /// <param name="fallbackPolicy">
  /// The fallback policy for unmappable characters. Default is <see cref="EncoderFallbackPolicy.Strict"/>.
  /// </param>
  /// <returns>
  /// A byte array containing the encoded text.
  /// </returns>
  /// <exception cref="EncoderFallbackException">
  /// Thrown when <paramref name="fallbackPolicy"/> is <see cref="EncoderFallbackPolicy.Strict"/>
  /// and unmappable characters are encountered.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method allocates a new byte array. For high-performance scenarios, consider
  /// using the overload that writes to a pre-allocated buffer.
  /// </para>
  /// <para>
  /// In Strict mode, any character that cannot be encoded to CP437 will cause an exception.
  /// This prevents silent data corruption but requires the caller to handle encoding issues.
  /// </para>
  /// </remarks>
  public static byte[] Encode(ReadOnlySpan<char> text, EncoderFallbackPolicy fallbackPolicy = EncoderFallbackPolicy.Strict)
  {
    if (text.Length == 0)
    {
      return Array.Empty<byte>();
    }

    System.Text.Encoding encoding = GetEncodingForEncoderFallbackPolicy(fallbackPolicy);
    return encoding.GetBytes(text.ToArray());
  }

  /// <summary>
  /// Encodes a string to bytes using CP437 encoding, writing to a pre-allocated buffer.
  /// </summary>
  /// <param name="text">The text to encode.</param>
  /// <param name="destination">The destination buffer for encoded bytes.</param>
  /// <param name="fallbackPolicy">
  /// The fallback policy for unmappable characters. Default is <see cref="EncoderFallbackPolicy.Strict"/>.
  /// </param>
  /// <returns>
  /// The number of bytes written to <paramref name="destination"/>.
  /// </returns>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="destination"/> is too small to hold the encoded bytes.
  /// </exception>
  /// <exception cref="EncoderFallbackException">
  /// Thrown when <paramref name="fallbackPolicy"/> is <see cref="EncoderFallbackPolicy.Strict"/>
  /// and unmappable characters are encountered.
  /// </exception>
  /// <remarks>
  /// This overload avoids allocation by writing directly to the provided buffer.
  /// Callers must ensure the buffer is large enough (typically same length as input for CP437).
  /// </remarks>
  public static int Encode(ReadOnlySpan<char> text, Span<byte> destination, EncoderFallbackPolicy fallbackPolicy = EncoderFallbackPolicy.Strict)
  {
    if (text.Length == 0)
    {
      return 0;
    }

    System.Text.Encoding encoding = GetEncodingForEncoderFallbackPolicy(fallbackPolicy);
    return encoding.GetBytes(text, destination);
  }

  /// <summary>
  /// Gets the CP437 encoding configured with the specified decoder fallback policy.
  /// </summary>
  /// <param name="policy">The decoder fallback policy.</param>
  /// <returns>
  /// A configured <see cref="System.Text.Encoding"/> instance.
  /// </returns>
  private static System.Text.Encoding GetEncodingForFallbackPolicy(DecoderFallbackPolicy policy)
  {
    return policy switch
    {
      DecoderFallbackPolicy.Strict =>
        System.Text.Encoding.GetEncoding(437, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),

      DecoderFallbackPolicy.ReplacementQuestion =>
        System.Text.Encoding.GetEncoding(437, EncoderFallback.ReplacementFallback, new DecoderReplacementFallback("?")),

      DecoderFallbackPolicy.ReplacementUnicode =>
        System.Text.Encoding.GetEncoding(437, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback),

      DecoderFallbackPolicy.BestEffort =>
        _cp437, // Use default encoding behaviour

      _ => throw new ArgumentException($"Unsupported decoder fallback policy: {policy}", nameof(policy))
    };
  }

  /// <summary>
  /// Gets the CP437 encoding configured with the specified encoder fallback policy.
  /// </summary>
  /// <param name="policy">The encoder fallback policy.</param>
  /// <returns>
  /// A configured <see cref="System.Text.Encoding"/> instance.
  /// </returns>
  private static System.Text.Encoding GetEncodingForEncoderFallbackPolicy(EncoderFallbackPolicy policy)
  {
    return policy switch
    {
      EncoderFallbackPolicy.Strict =>
        System.Text.Encoding.GetEncoding(437, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),

      EncoderFallbackPolicy.ReplacementQuestion =>
        System.Text.Encoding.GetEncoding(437, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback),

      _ => throw new ArgumentException($"Unsupported encoder fallback policy: {policy}", nameof(policy))
    };
  }

  /// <summary>
  /// Gets the standard CP437 encoding instance without fallback modifications.
  /// </summary>
  /// <returns>
  /// The CP437 encoding instance.
  /// </returns>
  /// <remarks>
  /// This property provides direct access to the underlying CP437 encoding for scenarios
  /// where custom fallback configuration is needed.
  /// </remarks>
  public static System.Text.Encoding GetEncoding()
  {
    return _cp437;
  }
}
