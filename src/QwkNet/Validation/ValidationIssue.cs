using System;

namespace QwkNet.Validation;

/// <summary>
/// Represents a single validation issue encountered during parsing or validation.
/// </summary>
/// <param name="Severity">The severity level of this issue.</param>
/// <param name="Message">A human-readable description of the issue.</param>
/// <param name="Location">Optional location context (e.g., "CONTROL.DAT line 6", "Message header offset 0x1A0").</param>
public sealed record ValidationIssue(
  ValidationSeverity Severity,
  string Message,
  string? Location = null)
{
  /// <summary>
  /// Gets the severity level of this issue.
  /// </summary>
  public ValidationSeverity Severity { get; init; } = Severity;

  /// <summary>
  /// Gets the human-readable description of the issue.
  /// </summary>
  public string Message { get; init; } = Message ?? throw new ArgumentNullException(nameof(Message));

  /// <summary>
  /// Gets the optional location context for this issue.
  /// </summary>
  public string? Location { get; init; } = Location;

  /// <summary>
  /// Returns a string representation of this validation issue.
  /// </summary>
  /// <returns>
  /// A string in the format "[Severity] Message (at Location)" or "[Severity] Message" if no location.
  /// </returns>
  public override string ToString()
  {
    if (Location != null)
    {
      return $"[{Severity}] {Message} (at {Location})";
    }
    return $"[{Severity}] {Message}";
  }
}