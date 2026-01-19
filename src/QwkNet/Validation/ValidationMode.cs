namespace QwkNet.Validation;

/// <summary>
/// Specifies the strictness level for parsing and validation operations.
/// </summary>
/// <remarks>
/// <para>
/// Real-world QWK packets may contain malformed data, missing fields, or
/// non-standard extensions. The validation mode determines how the parser
/// responds to such issues.
/// </para>
/// <para>
/// Per project rule #6, validation must support strict, lenient, and salvage modes.
/// </para>
/// </remarks>
public enum ValidationMode
{
  /// <summary>
  /// Fail on any structural error or missing required field.
  /// </summary>
  /// <remarks>
  /// Use this mode when packet correctness is critical and any deviation
  /// from the specification should halt processing.
  /// </remarks>
  Strict,

  /// <summary>
  /// Log warnings for issues but continue parsing.
  /// </summary>
  /// <remarks>
  /// Use this mode for production scenarios where slightly malformed packets
  /// should still be processed. Missing fields receive default values and
  /// warnings are recorded.
  /// </remarks>
  Lenient,

  /// <summary>
  /// Attempt best-effort recovery from errors.
  /// </summary>
  /// <remarks>
  /// Use this mode when processing archival or highly suspect packets.
  /// The parser makes aggressive assumptions to extract as much data as
  /// possible, even from severely malformed input.
  /// </remarks>
  Salvage
}