using System;
using QwkNet.Core;

namespace QwkNet.Models.Messages;

/// <summary>
/// Represents a QWK message with header fields and body content.
/// </summary>
/// <remarks>
/// <para>
/// This class provides an immutable model of a QWK message, preserving byte fidelity
/// through the RawHeader property whilst exposing parsed fields for usability.
/// </para>
/// <para>
/// Instances are constructed via <see cref="MessageBuilder"/> to ensure consistency
/// between parsed fields and the raw header.
/// </para>
/// </remarks>
public sealed class Message
{
  /// <summary>
  /// Gets the message number as it appears in the packet.
  /// </summary>
  /// <value>
  /// The message number (1-9999999). In REP packets, this field contains the conference number.
  /// </value>
  public int MessageNumber { get; }

  /// <summary>
  /// Gets the conference number this message belongs to.
  /// </summary>
  /// <value>
  /// The conference number (0-65535).
  /// </value>
  public ushort ConferenceNumber { get; }

  /// <summary>
  /// Gets the name of the message sender.
  /// </summary>
  /// <value>
  /// The sender's name (uppercase in original QWK, up to 25 characters).
  /// </value>
  public string From { get; }

  /// <summary>
  /// Gets the name of the message recipient.
  /// </summary>
  /// <value>
  /// The recipient's name (uppercase in original QWK, up to 25 characters).
  /// </value>
  public string To { get; }

  /// <summary>
  /// Gets the message subject.
  /// </summary>
  /// <value>
  /// The subject line (mixed case, up to 25 characters in original QWK).
  /// </value>
  public string Subject { get; }

  /// <summary>
  /// Gets the date and time the message was written.
  /// </summary>
  /// <value>
  /// The message timestamp. May be <c>null</c> if the date/time is invalid or unparseable.
  /// </value>
  public DateTime? DateTime { get; }

  /// <summary>
  /// Gets the message reference number (the message this is a reply to).
  /// </summary>
  /// <value>
  /// The reference message number, or 0 if this is not a reply.
  /// </value>
  public int ReferenceNumber { get; }

  /// <summary>
  /// Gets the message password (rarely used).
  /// </summary>
  /// <value>
  /// The password field content, typically empty or whitespace.
  /// </value>
  public string Password { get; }

  /// <summary>
  /// Gets the message body.
  /// </summary>
  /// <value>
  /// The message text content with lines and raw text.
  /// </value>
  public MessageBody Body { get; }

  /// <summary>
  /// Gets the message status flags.
  /// </summary>
  /// <value>
  /// A combination of <see cref="MessageStatus"/> flags indicating visibility,
  /// read status, and protection level.
  /// </value>
  public MessageStatus Status { get; }

  /// <summary>
  /// Gets a value indicating whether this message is private.
  /// </summary>
  /// <value>
  /// <c>true</c> if the message has the <see cref="MessageStatus.Private"/> flag; otherwise, <c>false</c>.
  /// </value>
  public bool IsPrivate => Status.HasFlag(MessageStatus.Private);

  /// <summary>
  /// Gets a value indicating whether this message has been read.
  /// </summary>
  /// <value>
  /// <c>true</c> if the message has the <see cref="MessageStatus.Read"/> flag; otherwise, <c>false</c>.
  /// </value>
  public bool IsRead => Status.HasFlag(MessageStatus.Read);

  /// <summary>
  /// Gets a value indicating whether this message is marked for deletion.
  /// </summary>
  /// <value>
  /// <c>true</c> if the message has the <see cref="MessageStatus.Deleted"/> flag; otherwise, <c>false</c>.
  /// </value>
  public bool IsDeleted => Status.HasFlag(MessageStatus.Deleted);

  /// <summary>
  /// Gets the collection of kludge lines in this message.
  /// </summary>
  /// <value>
  /// The kludge lines, or an empty collection if none are present.
  /// </value>
  public MessageKludgeCollection Kludges { get; }

  /// <summary>
  /// Gets the raw 128-byte QWK message header.
  /// </summary>
  /// <value>
  /// The original header bytes for byte-perfect round-trip accuracy.
  /// </value>
  public QwkMessageHeader RawHeader { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="Message"/> class.
  /// </summary>
  /// <param name="messageNumber">The message number.</param>
  /// <param name="conferenceNumber">The conference number.</param>
  /// <param name="from">The sender's name.</param>
  /// <param name="to">The recipient's name.</param>
  /// <param name="subject">The subject line.</param>
  /// <param name="dateTime">The message timestamp.</param>
  /// <param name="referenceNumber">The reference message number.</param>
  /// <param name="password">The message password.</param>
  /// <param name="body">The message body.</param>
  /// <param name="status">The message status flags.</param>
  /// <param name="kludges">The kludge lines.</param>
  /// <param name="rawHeader">The raw 128-byte header.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when any required parameter is <c>null</c>.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when message or reference numbers are out of valid range.
  /// </exception>
  public Message(
    int messageNumber,
    ushort conferenceNumber,
    string from,
    string to,
    string subject,
    DateTime? dateTime,
    int referenceNumber,
    string password,
    MessageBody body,
    MessageStatus status,
    MessageKludgeCollection kludges,
    QwkMessageHeader rawHeader)
  {
    if (messageNumber < 0 || messageNumber > 9999999)
    {
      throw new ArgumentOutOfRangeException(
        nameof(messageNumber),
        "Message number must be between 0 and 9999999.");
    }

    if (referenceNumber < 0 || referenceNumber > 99999999)
    {
      throw new ArgumentOutOfRangeException(
        nameof(referenceNumber),
        "Reference number must be between 0 and 99999999.");
    }

    MessageNumber = messageNumber;
    ConferenceNumber = conferenceNumber;
    From = from ?? throw new ArgumentNullException(nameof(from));
    To = to ?? throw new ArgumentNullException(nameof(to));
    Subject = subject ?? throw new ArgumentNullException(nameof(subject));
    DateTime = dateTime;
    ReferenceNumber = referenceNumber;
    Password = password ?? throw new ArgumentNullException(nameof(password));
    Body = body ?? throw new ArgumentNullException(nameof(body));
    Status = status;
    Kludges = kludges ?? throw new ArgumentNullException(nameof(kludges));
    RawHeader = rawHeader;
  }

  /// <summary>
  /// Returns a string representation of this message.
  /// </summary>
  /// <returns>
  /// A string showing the message number, sender, recipient, and subject.
  /// </returns>
  public override string ToString()
  {
    return $"Message #{MessageNumber}: From '{From}' To '{To}' Subject '{Subject}'";
  }
}
