using System.Text;

namespace QwkNet;

/// <summary>
/// Contains configuration options for QWK packet processing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides packet-level defaults for text encoding, line ending handling,
/// and other processing options. Individual operations can override these defaults.
/// </para>
/// <para>
/// Default values are chosen to preserve byte fidelity and prevent silent data loss.
/// Applications requiring different behaviour should explicitly configure these options.
/// </para>
/// </remarks>
public class QwkOptions
{
  /// <summary>
  /// Gets or sets the default text encoding for message content.
  /// </summary>
  /// <value>
  /// The text encoding. Default is CP437 (DOS Latin US), the historical standard for QWK packets.
  /// </value>
  /// <remarks>
  /// <para>
  /// CP437 is the de facto standard encoding for QWK packets from DOS-era BBS systems.
  /// Some systems may use different encodings (e.g., ISO-8859-1), which can be configured here.
  /// </para>
  /// <para>
  /// Individual operations can override this default by passing an explicit encoding parameter.
  /// </para>
  /// </remarks>
  public System.Text.Encoding DefaultTextEncoding { get; set; }

  /// <summary>
  /// Gets or sets the default line ending mode for text processing.
  /// </summary>
  /// <value>
  /// The line ending mode. Default is <see cref="QwkNet.Encoding.LineEndingMode.Preserve"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// Preserve mode maintains semantic content without normalising line endings, providing
  /// the best balance between fidelity and usability.
  /// </para>
  /// <para>
  /// Individual operations can override this default by passing an explicit mode parameter.
  /// </para>
  /// </remarks>
  public QwkNet.Encoding.LineEndingMode DefaultLineEndingMode { get; set; }

  /// <summary>
  /// Gets or sets the default decoder fallback policy for handling unmappable bytes.
  /// </summary>
  /// <value>
  /// The decoder fallback policy. Default is <see cref="QwkNet.Encoding.DecoderFallbackPolicy.Strict"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// Strict mode throws exceptions on unmappable bytes, preventing silent data loss.
  /// This is the safest default but requires applications to handle encoding errors explicitly.
  /// </para>
  /// <para>
  /// For user-facing applications, consider using
  /// <see cref="QwkNet.Encoding.DecoderFallbackPolicy.ReplacementQuestion"/> or
  /// <see cref="QwkNet.Encoding.DecoderFallbackPolicy.ReplacementUnicode"/> instead.
  /// </para>
  /// </remarks>
  public QwkNet.Encoding.DecoderFallbackPolicy DefaultDecoderFallback { get; set; }

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkOptions"/> class with default values.
  /// </summary>
  public QwkOptions()
  {
    // Register the code page provider to ensure CP437 is available
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    // Set defaults
    DefaultTextEncoding = System.Text.Encoding.GetEncoding(437); // CP437
    DefaultLineEndingMode = QwkNet.Encoding.LineEndingMode.Preserve;
    DefaultDecoderFallback = QwkNet.Encoding.DecoderFallbackPolicy.Strict;
  }

  /// <summary>
  /// Creates a new <see cref="QwkOptions"/> instance with lenient defaults for user-facing applications.
  /// </summary>
  /// <returns>
  /// A <see cref="QwkOptions"/> configured for lenient text processing.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Lenient options use:
  /// </para>
  /// <list type="bullet">
  ///   <item><description>CP437 encoding</description></item>
  ///   <item><description>Preserve line ending mode</description></item>
  ///   <item><description>ReplacementQuestion fallback policy</description></item>
  /// </list>
  /// <para>
  /// This configuration is appropriate for applications that display text to users
  /// where occasional replacement characters are acceptable.
  /// </para>
  /// </remarks>
  public static QwkOptions CreateLenient()
  {
    return new QwkOptions
    {
      DefaultDecoderFallback = QwkNet.Encoding.DecoderFallbackPolicy.ReplacementQuestion
    };
  }

  /// <summary>
  /// Creates a new <see cref="QwkOptions"/> instance with strict defaults for archival processing.
  /// </summary>
  /// <returns>
  /// A <see cref="QwkOptions"/> configured for strict byte-accurate processing.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Strict options use:
  /// </para>
  /// <list type="bullet">
  ///   <item><description>CP437 encoding</description></item>
  ///   <item><description>StrictQwk line ending mode</description></item>
  ///   <item><description>Strict fallback policy</description></item>
  /// </list>
  /// <para>
  /// This configuration is appropriate for archival, forensic, or preservation-grade
  /// processing where maximum fidelity is required.
  /// </para>
  /// </remarks>
  public static QwkOptions CreateStrict()
  {
    return new QwkOptions
    {
      DefaultLineEndingMode = QwkNet.Encoding.LineEndingMode.StrictQwk,
      DefaultDecoderFallback = QwkNet.Encoding.DecoderFallbackPolicy.Strict
    };
  }
}
