using System;

namespace QwkNet.Models.Qwke;

/// <summary>
/// Represents extended header fields from QWKE kludge lines that exceed the 25-character QWK limit.
/// </summary>
/// <remarks>
/// <para>
/// In QWKE format, when TO, FROM, or SUBJECT fields exceed 25 characters, they are truncated
/// in the message header and repeated in full as kludge lines at the beginning of the message body.
/// </para>
/// <para>
/// QWKE-aware readers use these extended values instead of the truncated header fields.
/// Non-QWKE readers simply display the kludge lines as part of the message text.
/// </para>
/// </remarks>
public sealed class QwkeLongHeaders
{
  /// <summary>
  /// Gets the extended TO field, or <c>null</c> if not present.
  /// </summary>
  /// <value>
  /// The full recipient name or address, exceeding the 25-character QWK limit.
  /// </value>
  public string? ExtendedTo { get; }

  /// <summary>
  /// Gets the extended FROM field, or <c>null</c> if not present.
  /// </summary>
  /// <value>
  /// The full sender name or address, exceeding the 25-character QWK limit.
  /// </value>
  public string? ExtendedFrom { get; }

  /// <summary>
  /// Gets the extended SUBJECT field, or <c>null</c> if not present.
  /// </summary>
  /// <value>
  /// The full subject line, exceeding the 25-character QWK limit.
  /// </value>
  public string? ExtendedSubject { get; }

  /// <summary>
  /// Gets a value indicating whether any long headers are present.
  /// </summary>
  /// <value>
  /// <c>true</c> if at least one extended header field is present; otherwise, <c>false</c>.
  /// </value>
  public bool HasLongHeaders =>
    !string.IsNullOrEmpty(ExtendedTo) ||
    !string.IsNullOrEmpty(ExtendedFrom) ||
    !string.IsNullOrEmpty(ExtendedSubject);

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkeLongHeaders"/> class.
  /// </summary>
  /// <param name="extendedTo">The extended TO field.</param>
  /// <param name="extendedFrom">The extended FROM field.</param>
  /// <param name="extendedSubject">The extended SUBJECT field.</param>
  public QwkeLongHeaders(
    string? extendedTo = null,
    string? extendedFrom = null,
    string? extendedSubject = null)
  {
    ExtendedTo = extendedTo;
    ExtendedFrom = extendedFrom;
    ExtendedSubject = extendedSubject;
  }

  /// <summary>
  /// Creates an empty <see cref="QwkeLongHeaders"/> instance with no extended fields.
  /// </summary>
  /// <returns>
  /// A new instance with all fields set to <c>null</c>.
  /// </returns>
  public static QwkeLongHeaders Empty()
  {
    return new QwkeLongHeaders();
  }

  /// <summary>
  /// Returns a string representation of the long headers.
  /// </summary>
  /// <returns>
  /// A summary of which extended fields are present.
  /// </returns>
  public override string ToString()
  {
    if (!HasLongHeaders)
    {
      return "No long headers";
    }

    int count = 0;
    if (!string.IsNullOrEmpty(ExtendedTo)) count++;
    if (!string.IsNullOrEmpty(ExtendedFrom)) count++;
    if (!string.IsNullOrEmpty(ExtendedSubject)) count++;

    return $"{count} long header(s)";
  }
}
