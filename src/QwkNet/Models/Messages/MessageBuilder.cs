using System;
using System.Collections.Generic;
using System.Text;
using QwkNet.Core;
using QwkNet.Encoding;

namespace QwkNet.Models.Messages;

/// <summary>
/// Provides a fluent interface for constructing <see cref="Message"/> instances.
/// </summary>
/// <remarks>
/// This builder handles the complexity of creating QWK message headers and ensures
/// consistency between parsed fields and the raw 128-byte header.
/// </remarks>
public sealed class MessageBuilder
{
  private int _messageNumber;
  private ushort _conferenceNumber;
  private string _from = string.Empty;
  private string _to = string.Empty;
  private string _subject = string.Empty;
  private DateTime? _dateTime;
  private int _referenceNumber;
  private string _password = string.Empty;
  private MessageBody? _body;
  private MessageStatus _status = MessageStatus.None;
  private readonly List<MessageKludge> _kludges = new List<MessageKludge>();

  /// <summary>
  /// Sets the message number.
  /// </summary>
  /// <param name="messageNumber">The message number (0-9999999).</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="messageNumber"/> is out of valid range.
  /// </exception>
  public MessageBuilder SetMessageNumber(int messageNumber)
  {
    if (messageNumber < 0 || messageNumber > 9999999)
    {
      throw new ArgumentOutOfRangeException(
        nameof(messageNumber),
        "Message number must be between 0 and 9999999.");
    }

    _messageNumber = messageNumber;
    return this;
  }

  /// <summary>
  /// Sets the conference number.
  /// </summary>
  /// <param name="conferenceNumber">The conference number (0-65535).</param>
  /// <returns>This builder instance for method chaining.</returns>
  public MessageBuilder SetConferenceNumber(ushort conferenceNumber)
  {
    _conferenceNumber = conferenceNumber;
    return this;
  }

  /// <summary>
  /// Sets the sender's name.
  /// </summary>
  /// <param name="from">The sender's name.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="from"/> is <c>null</c>.
  /// </exception>
  public MessageBuilder SetFrom(string from)
  {
    _from = from ?? throw new ArgumentNullException(nameof(from));
    return this;
  }

  /// <summary>
  /// Sets the recipient's name.
  /// </summary>
  /// <param name="to">The recipient's name.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="to"/> is <c>null</c>.
  /// </exception>
  public MessageBuilder SetTo(string to)
  {
    _to = to ?? throw new ArgumentNullException(nameof(to));
    return this;
  }

  /// <summary>
  /// Sets the message subject.
  /// </summary>
  /// <param name="subject">The subject line.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="subject"/> is <c>null</c>.
  /// </exception>
  public MessageBuilder SetSubject(string subject)
  {
    _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    return this;
  }

  /// <summary>
  /// Sets the message date and time.
  /// </summary>
  /// <param name="dateTime">The message timestamp.</param>
  /// <returns>This builder instance for method chaining.</returns>
  public MessageBuilder SetDateTime(DateTime? dateTime)
  {
    _dateTime = dateTime;
    return this;
  }

  /// <summary>
  /// Sets the reference message number (for replies).
  /// </summary>
  /// <param name="referenceNumber">The reference number (0-99999999).</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="referenceNumber"/> is out of valid range.
  /// </exception>
  public MessageBuilder SetReferenceNumber(int referenceNumber)
  {
    if (referenceNumber < 0 || referenceNumber > 99999999)
    {
      throw new ArgumentOutOfRangeException(
        nameof(referenceNumber),
        "Reference number must be between 0 and 99999999.");
    }

    _referenceNumber = referenceNumber;
    return this;
  }

  /// <summary>
  /// Sets the message password.
  /// </summary>
  /// <param name="password">The password (rarely used).</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="password"/> is <c>null</c>.
  /// </exception>
  public MessageBuilder SetPassword(string password)
  {
    _password = password ?? throw new ArgumentNullException(nameof(password));
    return this;
  }

  /// <summary>
  /// Sets the message body from a <see cref="MessageBody"/> instance.
  /// </summary>
  /// <param name="body">The message body.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="body"/> is <c>null</c>.
  /// </exception>
  public MessageBuilder SetBody(MessageBody body)
  {
    _body = body ?? throw new ArgumentNullException(nameof(body));
    return this;
  }

  /// <summary>
  /// Sets the message body from plain text.
  /// </summary>
  /// <param name="text">The message text.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="text"/> is <c>null</c>.
  /// </exception>
  /// <remarks>
  /// This method splits the text on standard line endings and creates a <see cref="MessageBody"/>.
  /// </remarks>
  public MessageBuilder SetBodyText(string text)
  {
    if (text == null)
    {
      throw new ArgumentNullException(nameof(text));
    }

    // Split on common line endings
    string[] lines = text.Split(
      new[] { "\r\n", "\r", "\n" },
      StringSplitOptions.None);

    // Build raw text with QWK terminators
    // Use π (U+03C0) which encodes to 0xE3 in CP437
    // NOT ã (U+00E3) which is NOT in CP437
    const char qwkTerminator = '\u03C0'; // π (Greek pi) = byte 0xE3 in CP437
    StringBuilder rawBuilder = new StringBuilder();

    for (int i = 0; i < lines.Length; i++)
    {
      rawBuilder.Append(lines[i]);
      if (i < lines.Length - 1)
      {
        rawBuilder.Append(qwkTerminator);
      }
    }

    _body = new MessageBody(lines, rawBuilder.ToString());
    return this;
  }

  /// <summary>
  /// Sets the message status flags.
  /// </summary>
  /// <param name="status">The status flags.</param>
  /// <returns>This builder instance for method chaining.</returns>
  public MessageBuilder SetStatus(MessageStatus status)
  {
    _status = status;
    return this;
  }

  /// <summary>
  /// Adds a kludge line to the message.
  /// </summary>
  /// <param name="key">The kludge key.</param>
  /// <param name="value">The kludge value.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="key"/> is <c>null</c>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="key"/> is empty or whitespace.
  /// </exception>
  /// <remarks>
  /// The raw line is synthesised as "Key: Value" with a 0xE3 terminator.
  /// </remarks>
  public MessageBuilder AddKludge(string key, string value)
  {
    if (key == null)
    {
      throw new ArgumentNullException(nameof(key));
    }

    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentException("Kludge key cannot be empty or whitespace.", nameof(key));
    }

    const char qwkTerminator = '\u03C0';
    string rawLine = $"{key}: {value ?? string.Empty}{qwkTerminator}";

    MessageKludge kludge = new MessageKludge(key, value ?? string.Empty, rawLine);
    _kludges.Add(kludge);

    return this;
  }

  /// <summary>
  /// Adds a kludge to the message.
  /// </summary>
  /// <param name="kludge">The kludge to add.</param>
  /// <returns>This builder instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="kludge"/> is <c>null</c>.
  /// </exception>
  public MessageBuilder AddKludge(MessageKludge kludge)
  {
    if (kludge == null)
    {
      throw new ArgumentNullException(nameof(kludge));
    }

    _kludges.Add(kludge);
    return this;
  }

  /// <summary>
  /// Builds the <see cref="Message"/> instance.
  /// </summary>
  /// <returns>
  /// A new immutable <see cref="Message"/> with the configured properties.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when required fields (From, To, Subject, Body) are not set.
  /// </exception>
  /// <remarks>
  /// This method generates the raw 128-byte QWK header based on the configured fields.
  /// </remarks>
  public Message Build()
  {
    // Validate required fields
    if (string.IsNullOrEmpty(_from))
    {
      throw new InvalidOperationException("From field is required.");
    }

    if (string.IsNullOrEmpty(_to))
    {
      throw new InvalidOperationException("To field is required.");
    }

    if (string.IsNullOrEmpty(_subject))
    {
      throw new InvalidOperationException("Subject field is required.");
    }

    if (_body == null)
    {
      throw new InvalidOperationException("Body is required.");
    }

    // Generate the raw header
    QwkMessageHeader rawHeader = CreateRawHeader();

    // Build kludge collection
    MessageKludgeCollection kludgeCollection = new MessageKludgeCollection(_kludges);

    // Create the message
    return new Message(
      messageNumber: _messageNumber,
      conferenceNumber: _conferenceNumber,
      from: _from,
      to: _to,
      subject: _subject,
      dateTime: _dateTime,
      referenceNumber: _referenceNumber,
      password: _password,
      body: _body,
      status: _status,
      kludges: kludgeCollection,
      rawHeader: rawHeader);
  }

  /// <summary>
  /// Creates the raw 128-byte QWK header from the builder's fields.
  /// </summary>
  /// <returns>
  /// A <see cref="QwkMessageHeader"/> representing the message header.
  /// </returns>
  private QwkMessageHeader CreateRawHeader()
  {
    byte[] headerBytes = new byte[128];

    // Fill with spaces by default
    for (int i = 0; i < headerBytes.Length; i++)
    {
      headerBytes[i] = (byte)' ';
    }

    // Offset 1: Status flag (1 byte)
    headerBytes[0] = ConvertStatusToQwkByte(_status);

    // Offset 2-8: Message number (7 bytes, ASCII)
    WriteAsciiField(headerBytes, 1, _messageNumber.ToString(), 7);

    // Offset 9-16: Date (8 bytes, MM-DD-YY)
    if (_dateTime.HasValue)
    {
      string dateStr = _dateTime.Value.ToString("MM-dd-yy");
      WriteAsciiField(headerBytes, 8, dateStr, 8);
    }

    // Offset 17-21: Time (5 bytes, HH:MM)
    if (_dateTime.HasValue)
    {
      string timeStr = _dateTime.Value.ToString("HH:mm");
      WriteAsciiField(headerBytes, 16, timeStr, 5);
    }

    // Offset 22-46: To (25 bytes)
    WriteAsciiField(headerBytes, 21, _to.ToUpperInvariant(), 25);

    // Offset 47-71: From (25 bytes)
    WriteAsciiField(headerBytes, 46, _from.ToUpperInvariant(), 25);

    // Offset 72-96: Subject (25 bytes)
    WriteAsciiField(headerBytes, 71, _subject, 25);

    // Offset 97-108: Password (12 bytes)
    WriteAsciiField(headerBytes, 96, _password, 12);

    // Offset 109-116: Reference number (8 bytes, ASCII)
    WriteAsciiField(headerBytes, 108, _referenceNumber.ToString(), 8);

    // Offset 117-122: Block count (6 bytes, ASCII) - set to "1" as placeholder
    // Actual value will be calculated during packet writing
    WriteAsciiField(headerBytes, 116, "1", 6);

    // Offset 123: Message active flag (1 byte)
    headerBytes[122] = 0xE1; // 'a' = active

    // Offset 124-125: Conference number (2 bytes, little-endian unsigned short)
    headerBytes[123] = (byte)(_conferenceNumber & 0xFF);
    headerBytes[124] = (byte)((_conferenceNumber >> 8) & 0xFF);

    // Offset 126-128: Reserved (3 bytes) - already filled with spaces

    return QwkMessageHeader.Parse(headerBytes);
  }

  /// <summary>
  /// Converts <see cref="MessageStatus"/> flags to the QWK status byte.
  /// </summary>
  /// <param name="status">The status flags.</param>
  /// <returns>
  /// The QWK status byte character.
  /// </returns>
  private static byte ConvertStatusToQwkByte(MessageStatus status)
  {
    bool isPrivate = status.HasFlag(MessageStatus.Private);
    bool isRead = status.HasFlag(MessageStatus.Read);
    bool isCommentToSysop = status.HasFlag(MessageStatus.CommentToSysop);
    bool isSenderPassword = status.HasFlag(MessageStatus.SenderPasswordProtected);
    bool isGroupPassword = status.HasFlag(MessageStatus.GroupPasswordProtected);
    bool isGroupPasswordToAll = status.HasFlag(MessageStatus.GroupPasswordProtectedToAll);

    // Handle password protection first (highest priority)
    if (isGroupPasswordToAll)
    {
      return (byte)'$';
    }

    if (isGroupPassword)
    {
      return (byte)(isRead ? '#' : '!');
    }

    if (isSenderPassword)
    {
      return (byte)(isRead ? '^' : '%');
    }

    if (isCommentToSysop)
    {
      return (byte)(isRead ? '`' : '~');
    }

    if (isPrivate)
    {
      return (byte)(isRead ? '+' : '*');
    }

    // Public message
    return (byte)(isRead ? '-' : ' ');
  }

  /// <summary>
  /// Writes an ASCII string to the header byte array at the specified offset.
  /// </summary>
  /// <param name="headerBytes">The header byte array.</param>
  /// <param name="offset">The starting offset (0-based).</param>
  /// <param name="value">The value to write.</param>
  /// <param name="maxLength">The maximum field length.</param>
  private static void WriteAsciiField(byte[] headerBytes, int offset, string value, int maxLength)
  {
    if (string.IsNullOrEmpty(value))
    {
      return;
    }

    // Truncate if necessary
    string truncated = value.Length > maxLength ? value.Substring(0, maxLength) : value;

    // Write bytes using CP437 encoding (DOS/BBS standard)
    byte[] valueBytes = Cp437Encoding.Encode(truncated);
    Array.Copy(valueBytes, 0, headerBytes, offset, valueBytes.Length);
  }
}