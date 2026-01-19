using System;

namespace QwkNet.Models.Messages;

/// <summary>
/// Represents a single kludge line in a QWK or QWKE message.
/// </summary>
/// <remarks>
/// <para>
/// Kludge lines are metadata fields that appear at the beginning of message text.
/// In QWKE format, kludges allow extended header fields (To:, From:, Subject:) that
/// exceed the 25-character limit of the original QWK header.
/// </para>
/// <para>
/// This class stores kludges as raw key-value pairs without interpretation,
/// preserving byte fidelity for unknown or custom kludge types.
/// </para>
/// </remarks>
public sealed class MessageKludge
{
  /// <summary>
  /// Gets the kludge key (e.g., "To", "From", "Subject").
  /// </summary>
  /// <value>
  /// The kludge identifier, typically without the trailing colon.
  /// </value>
  public string Key { get; }

  /// <summary>
  /// Gets the kludge value.
  /// </summary>
  /// <value>
  /// The content following the key, with leading/trailing whitespace preserved as written.
  /// </value>
  public string Value { get; }

  /// <summary>
  /// Gets the raw kludge line as it appeared in the message body.
  /// </summary>
  /// <value>
  /// The original line including key, separator, value, and line terminator (0xE3 or CR).
  /// </value>
  public string RawLine { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="MessageKludge"/> class.
  /// </summary>
  /// <param name="key">The kludge key.</param>
  /// <param name="value">The kludge value.</param>
  /// <param name="rawLine">The raw kludge line as it appeared in the message.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="key"/> or <paramref name="rawLine"/> is <c>null</c>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="key"/> is empty or whitespace-only.
  /// </exception>
  public MessageKludge(string key, string value, string rawLine)
  {
    if (key == null)
    {
      throw new ArgumentNullException(nameof(key));
    }

    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentException("Kludge key cannot be empty or whitespace.", nameof(key));
    }

    if (rawLine == null)
    {
      throw new ArgumentNullException(nameof(rawLine));
    }

    Key = key;
    Value = value ?? string.Empty;
    RawLine = rawLine;
  }

  /// <summary>
  /// Returns a string representation of this kludge.
  /// </summary>
  /// <returns>
  /// A string in the format "Key: Value".
  /// </returns>
  public override string ToString()
  {
    return $"{Key}: {Value}";
  }
}
