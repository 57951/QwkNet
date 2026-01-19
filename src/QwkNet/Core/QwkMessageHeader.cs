using System;

namespace QwkNet.Core;

/// <summary>
/// Represents a parsed QWK message header (128-byte record).
/// </summary>
/// <remarks>
/// <para>
/// QWK message headers are fixed at 128 bytes with the following layout:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Offset</term>
/// <term>Length</term>
/// <description>Field</description>
/// </listheader>
/// <item>
/// <term>0</term>
/// <term>1</term>
/// <description>Message status byte</description>
/// </item>
/// <item>
/// <term>1</term>
/// <term>7</term>
/// <description>Message number (space-padded ASCII)</description>
/// </item>
/// <item>
/// <term>8</term>
/// <term>8</term>
/// <description>Date (MM-DD-YY)</description>
/// </item>
/// <item>
/// <term>16</term>
/// <term>5</term>
/// <description>Time (HH:MM)</description>
/// </item>
/// <item>
/// <term>21</term>
/// <term>25</term>
/// <description>TO: field (uppercase, space-padded)</description>
/// </item>
/// <item>
/// <term>46</term>
/// <term>25</term>
/// <description>FROM: field (uppercase, space-padded)</description>
/// </item>
/// <item>
/// <term>71</term>
/// <term>25</term>
/// <description>SUBJECT: field (mixed case, space-padded)</description>
/// </item>
/// <item>
/// <term>96</term>
/// <term>12</term>
/// <description>PASSWORD: field (space-padded)</description>
/// </item>
/// <item>
/// <term>108</term>
/// <term>8</term>
/// <description>REFERENCE: message number (space-padded ASCII)</description>
/// </item>
/// <item>
/// <term>116</term>
/// <term>6</term>
/// <description>Number of 128-byte blocks (ASCII, left-justified) - includes header block</description>
/// </item>
/// <item>
/// <term>122</term>
/// <term>1</term>
/// <description>Alive/killed flag (0xE1 = alive, 0xE2 = killed)</description>
/// </item>
/// <item>
/// <term>123</term>
/// <term>2</term>
/// <description>Conference number (little-endian)</description>
/// </item>
/// <item>
/// <term>125</term>
/// <term>2</term>
/// <description>Logical message number (little-endian)</description>
/// </item>
/// <item>
/// <term>127</term>
/// <term>1</term>
/// <description>Network tag indicator (0x20 = no tag, 0x2A = has tag, 0xFF = has tag)</description>
/// </item>
/// </list>
/// </remarks>
public readonly struct QwkMessageHeader
{
  /// <summary>
  /// Gets the raw message status byte.
  /// </summary>
  public byte StatusByte { get; }

  /// <summary>
  /// Gets whether the message is marked as private.
  /// </summary>
  public bool IsPrivate => (StatusByte & 0x01) != 0;

  /// <summary>
  /// Gets whether the message has been read.
  /// </summary>
  public bool IsRead => (StatusByte & 0x04) != 0;

  /// <summary>
  /// Gets the message number as a string (may be empty or non-numeric).
  /// </summary>
  public string MessageNumber { get; }

  /// <summary>
  /// Gets the date string in MM-DD-YY format.
  /// </summary>
  public string Date { get; }

  /// <summary>
  /// Gets the time string in HH:MM format.
  /// </summary>
  public string Time { get; }

  /// <summary>
  /// Gets the recipient name (TO: field).
  /// </summary>
  public string To { get; }

  /// <summary>
  /// Gets the sender name (FROM: field).
  /// </summary>
  public string From { get; }

  /// <summary>
  /// Gets the message subject.
  /// </summary>
  public string Subject { get; }

  /// <summary>
  /// Gets the password field (rarely used).
  /// </summary>
  public string Password { get; }

  /// <summary>
  /// Gets the reference message number (for replies).
  /// </summary>
  public string ReferenceNumber { get; }

  /// <summary>
  /// Gets the number of 128-byte blocks in the message body.
  /// </summary>
  public int BlockCount { get; }

  /// <summary>
  /// Gets the alive/killed flag byte.
  /// </summary>
  public byte AliveFlag { get; }

  /// <summary>
  /// Gets whether the message is marked as killed/deleted.
  /// </summary>
  public bool IsKilled => AliveFlag == 0xE2;

  /// <summary>
  /// Gets the conference number (zero-based).
  /// </summary>
  public ushort ConferenceNumber { get; }

  /// <summary>
  /// Gets the logical message number within the conference.
  /// </summary>
  public ushort LogicalMessageNumber { get; }

  /// <summary>
  /// Gets the network tag indicator byte.
  /// </summary>
  public byte NetworkTagIndicator { get; }

  /// <summary>
  /// Gets whether the message has a network tag line.
  /// </summary>
  public bool HasNetworkTag => NetworkTagIndicator == 0x2A || NetworkTagIndicator == 0xFF;

  /// <summary>
  /// Gets the raw 128-byte header data.
  /// </summary>
  public byte[] RawHeader { get; }

  /// <summary>
  /// The maximum number of 128-byte blocks in a message.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This prevents integer overflow while still allowing values far beyond realistic
  /// QWK messages. The higher-level validation in QwkPacket.ParseMessages() (which
  /// checks against maxMessageSizeMB) will catch unreasonably large messages.
  /// </para>
  /// <para>
  /// The default maxMessageSizeMB: 16 allows up to 131,072 blocks, which is well
  /// within the 16M limit but far exceeds typical QWK message sizes.
  /// </para>
  /// <para>
  /// <b>Rationale:</b>
  /// The block count field is 6 digits, allowing 0-999,999
  /// </para>
  /// <list>
  /// <item>int.MaxValue = 2,147,483,647</item>
  /// <item>Safe multiplication: 2,147,483,647 / 128 ≈ 16,777,215 blocks</item>
  /// <item>Use 16,000,000 to leave headroom and prevent overflow in totalBlocks * 128L</item>
  /// </list>
  /// </remarks>
  private const int MAX_SAFE_BLOCK_COUNT = 16_000_000; // Prevents overflow: int.MaxValue / 128 ≈ 16.7M

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkMessageHeader"/> struct.
  /// </summary>
  /// <param name="rawHeader">The 128-byte header data.</param>
  /// <exception cref="ArgumentException">
  /// Thrown when rawHeader is not exactly 128 bytes.
  /// </exception>
  private QwkMessageHeader(byte[] rawHeader)
  {
    if (rawHeader.Length != BinaryRecordReader.RecordSize)
    {
      throw new ArgumentException(
        $"Header must be exactly {BinaryRecordReader.RecordSize} bytes.",
        nameof(rawHeader));
    }

    RawHeader = rawHeader;

    // Parse status byte
    StatusByte = rawHeader[0];

    // Parse ASCII fields (trim trailing spaces)
    MessageNumber = ParseAsciiField(rawHeader, 1, 7);
    Date = ParseAsciiField(rawHeader, 8, 8);
    Time = ParseAsciiField(rawHeader, 16, 5);
    To = ParseAsciiField(rawHeader, 21, 25);
    From = ParseAsciiField(rawHeader, 46, 25);
    Subject = ParseAsciiField(rawHeader, 71, 25);
    Password = ParseAsciiField(rawHeader, 96, 12);
    ReferenceNumber = ParseAsciiField(rawHeader, 108, 8);

    // Parse block count (ASCII numeric field)
    string blockCountStr = ParseAsciiField(rawHeader, 116, 6);
    BlockCount = int.TryParse(blockCountStr, out int blocks) && blocks > 0 && blocks <= MAX_SAFE_BLOCK_COUNT ? blocks : 0;
    //BlockCount = int.TryParse(blockCountStr, out int blocks) ? blocks : 0;

    // Parse binary fields
    AliveFlag = rawHeader[122];
    ConferenceNumber = BitConverter.ToUInt16(rawHeader, 123);
    LogicalMessageNumber = BitConverter.ToUInt16(rawHeader, 125);
    NetworkTagIndicator = rawHeader[127];
  }

  /// <summary>
  /// Parses a QWK message header from a 128-byte buffer.
  /// </summary>
  /// <param name="headerBytes">The header bytes.</param>
  /// <returns>A parsed header structure.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when headerBytes is not exactly 128 bytes.
  /// </exception>
  public static QwkMessageHeader Parse(ReadOnlySpan<byte> headerBytes)
  {
    byte[] copy = headerBytes.ToArray();
    return new QwkMessageHeader(copy);
  }

  /// <summary>
  /// Attempts to parse a date/time combination into a DateTime.
  /// </summary>
  /// <param name="dateTime">The parsed DateTime, or DateTime.MinValue on failure.</param>
  /// <returns>True if parsing succeeded, false otherwise.</returns>
  /// <remarks>
  /// QWK dates are notoriously inconsistent. This method attempts common formats.
  /// </remarks>
  public bool TryGetDateTime(out DateTime dateTime)
  {
    dateTime = DateTime.MinValue;

    // Common formats: MM-DD-YY, DD-MM-YY
    string[] dateFormats = new[]
    {
      "MM-dd-yy",
      "dd-MM-yy",
      "MM/dd/yy",
      "dd/MM/yy"
    };

    foreach (string format in dateFormats)
    {
      if (DateTime.TryParseExact(
        Date,
        format,
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None,
        out DateTime parsedDate))
      {
        // Combine with time if possible
        if (TimeSpan.TryParseExact(
          Time,
          "hh\\:mm",
          System.Globalization.CultureInfo.InvariantCulture,
          out TimeSpan parsedTime))
        {
          dateTime = parsedDate.Add(parsedTime);
          return true;
        }

        dateTime = parsedDate;
        return true;
      }
    }

    return false;
  }

  private static string ParseAsciiField(byte[] data, int offset, int length)
  {
    Span<byte> fieldBytes = data.AsSpan(offset, length);
    
    // Convert to string, preserving high ASCII
    char[] chars = new char[length];
    for (int i = 0; i < length; i++)
    {
      chars[i] = (char)fieldBytes[i];
    }

    // Trim trailing spaces and null bytes
    return new string(chars).TrimEnd(' ', '\0');
  }
}