namespace QwkNet.Validation;

/// <summary>
/// Specifies the severity level of a validation issue.
/// </summary>
public enum ValidationSeverity
{
  /// <summary>
  /// Informational message, no action required.
  /// </summary>
  Info,

  /// <summary>
  /// Non-critical issue that does not prevent processing.
  /// </summary>
  Warning,

  /// <summary>
  /// Critical issue that may cause incorrect behaviour.
  /// </summary>
  Error
}