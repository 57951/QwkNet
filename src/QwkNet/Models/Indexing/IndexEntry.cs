using System;

namespace QwkNet.Models.Indexing;

/// <summary>
/// Represents a single entry in a QWK index (.NDX) file.
/// </summary>
/// <remarks>
/// Each index entry consists of a 5-byte MSBIN floating-point number that encodes
/// the record offset of a message header within MESSAGES.DAT. Index files are
/// conference-specific and named numerically (e.g., 0.NDX, 1.NDX, 123.NDX).
/// </remarks>
public readonly struct IndexEntry : IEquatable<IndexEntry>
{
  /// <summary>
  /// Gets the message number (1-based) that this index entry refers to.
  /// </summary>
  /// <value>The sequential message number within the conference, starting at 1.</value>
  public int MessageNumber { get; }

  /// <summary>
  /// Gets the record offset (0-based) within MESSAGES.DAT where this message header begins.
  /// </summary>
  /// <value>The byte offset divided by 128 (the QWK record size).</value>
  /// <remarks>
  /// This value is calculated from the MSBIN float stored in the .NDX file.
  /// A value of 0 indicates the first record after the MESSAGES.DAT header.
  /// </remarks>
  public int RecordOffset { get; }

  /// <summary>
  /// Gets the raw MSBIN bytes that were read from the .NDX file.
  /// </summary>
  /// <value>A 4-byte array containing the original MSBIN floating-point representation.</value>
  /// <remarks>
  /// Preserved for byte-accurate round-trip fidelity and debugging purposes.
  /// </remarks>
  public ReadOnlyMemory<byte> RawMsbinBytes { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="IndexEntry"/> struct.
  /// </summary>
  /// <param name="messageNumber">The message number (must be 1 or greater).</param>
  /// <param name="recordOffset">The record offset (must be 0 or greater).</param>
  /// <param name="rawMsbinBytes">The raw 4-byte MSBIN representation (must be exactly 4 bytes).</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when messageNumber is less than 1, recordOffset is negative,
  /// or rawMsbinBytes is not exactly 4 bytes.
  /// </exception>
  public IndexEntry(int messageNumber, int recordOffset, ReadOnlyMemory<byte> rawMsbinBytes)
  {
    if (messageNumber < 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(messageNumber),
        messageNumber,
        "Message number must be 1 or greater.");
    }

    if (recordOffset < 0)
    {
      throw new ArgumentOutOfRangeException(
        nameof(recordOffset),
        recordOffset,
        "Record offset must be 0 or greater.");
    }

    if (rawMsbinBytes.Length != 4)
    {
      throw new ArgumentOutOfRangeException(
        nameof(rawMsbinBytes),
        rawMsbinBytes.Length,
        "MSBIN bytes must be exactly 4 bytes.");
    }

    MessageNumber = messageNumber;
    RecordOffset = recordOffset;
    RawMsbinBytes = rawMsbinBytes;
  }

  /// <summary>
  /// Calculates the byte offset within MESSAGES.DAT for this message.
  /// </summary>
  /// <returns>The absolute byte position (record offset × 128).</returns>
  /// <remarks>
  /// QWK records are always 128 bytes. This method converts the record offset
  /// to the actual file position for seeking operations.
  /// </remarks>
  public long GetByteOffset()
  {
    return (long)RecordOffset * 128;
  }

  /// <summary>
  /// Returns a string representation of this index entry.
  /// </summary>
  /// <returns>A string in the format "Message #N at record offset O (byte B)".</returns>
  public override string ToString()
  {
    return $"Message #{MessageNumber} at record offset {RecordOffset} (byte {GetByteOffset()})";
  }

  /// <summary>
  /// Determines whether this instance is equal to another <see cref="IndexEntry"/>.
  /// </summary>
  /// <param name="other">The other index entry to compare.</param>
  /// <returns>True if message number and record offset match; otherwise false.</returns>
  public bool Equals(IndexEntry other)
  {
    return MessageNumber == other.MessageNumber && RecordOffset == other.RecordOffset;
  }

  /// <summary>
  /// Determines whether this instance is equal to another object.
  /// </summary>
  /// <param name="obj">The object to compare.</param>
  /// <returns>True if obj is an <see cref="IndexEntry"/> and equals this instance.</returns>
  public override bool Equals(object? obj)
  {
    return obj is IndexEntry other && Equals(other);
  }

  /// <summary>
  /// Returns a hash code for this instance.
  /// </summary>
  /// <returns>A hash code combining message number and record offset.</returns>
  public override int GetHashCode()
  {
    return HashCode.Combine(MessageNumber, RecordOffset);
  }

  /// <summary>
  /// Determines whether two <see cref="IndexEntry"/> instances are equal.
  /// </summary>
  public static bool operator ==(IndexEntry left, IndexEntry right)
  {
    return left.Equals(right);
  }

  /// <summary>
  /// Determines whether two <see cref="IndexEntry"/> instances are not equal.
  /// </summary>
  public static bool operator !=(IndexEntry left, IndexEntry right)
  {
    return !left.Equals(right);
  }
}