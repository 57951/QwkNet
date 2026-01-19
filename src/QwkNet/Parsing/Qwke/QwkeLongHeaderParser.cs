using System;
using QwkNet.Models.Messages;
using QwkNet.Models.Qwke;

namespace QwkNet.Parsing.Qwke;

/// <summary>
/// Parses QWKE long headers from message kludges.
/// </summary>
/// <remarks>
/// <para>
/// QWKE allows TO, FROM, and SUBJECT fields to exceed the 25-character QWK limit
/// by placing kludge lines at the beginning of the message body in the format:
/// "To: extended recipient name"
/// "From: extended sender name"
/// "Subject: extended subject line"
/// </para>
/// <para>
/// These kludges may be terminated by either 0xE3 (QWK newline) or ASCII CR (13).
/// </para>
/// </remarks>
public static class QwkeLongHeaderParser
{
  /// <summary>
  /// Extracts QWKE long headers from a message's kludge collection.
  /// </summary>
  /// <param name="kludges">The message kludge collection to search.</param>
  /// <returns>
  /// A <see cref="QwkeLongHeaders"/> instance containing any extended header fields found.
  /// If no long headers are present, returns an empty instance.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="kludges"/> is <c>null</c>.
  /// </exception>
  public static QwkeLongHeaders Parse(MessageKludgeCollection kludges)
  {
    if (kludges == null)
    {
      throw new ArgumentNullException(nameof(kludges));
    }

    // Search for extended header kludges (case-insensitive)
    MessageKludge? toKludge = kludges.GetFirstByKey("To");
    MessageKludge? fromKludge = kludges.GetFirstByKey("From");
    MessageKludge? subjectKludge = kludges.GetFirstByKey("Subject");

    string? extendedTo = toKludge?.Value;
    string? extendedFrom = fromKludge?.Value;
    string? extendedSubject = subjectKludge?.Value;

    return new QwkeLongHeaders(extendedTo, extendedFrom, extendedSubject);
  }

  /// <summary>
  /// Extracts QWKE long headers from a message.
  /// </summary>
  /// <param name="message">The message to extract long headers from.</param>
  /// <returns>
  /// A <see cref="QwkeLongHeaders"/> instance containing any extended header fields found.
  /// If no long headers are present, returns an empty instance.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="message"/> is <c>null</c>.
  /// </exception>
  public static QwkeLongHeaders Parse(Message message)
  {
    if (message == null)
    {
      throw new ArgumentNullException(nameof(message));
    }

    return Parse(message.Kludges);
  }
}
