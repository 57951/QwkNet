using System;

namespace QwkNet.Archive.Extensions;

/// <summary>
/// Represents a magic byte signature used to identify archive formats.
/// </summary>
/// <remarks>
/// <para>
/// Archive signatures consist of a specific byte sequence (magic bytes) that
/// typically appears at a fixed offset within the file. Extensions use these
/// signatures to detect whether a stream contains their supported format.
/// </para>
/// <para>
/// Detection logic should account for the minimum length requirement to avoid
/// false positives from truncated files.
/// </para>
/// </remarks>
public sealed class ArchiveSignature
{
  /// <summary>
  /// Gets the magic byte sequence that identifies this archive format.
  /// </summary>
  /// <value>
  /// A byte array containing the signature pattern. Never <see langword="null"/> or empty.
  /// </value>
  public byte[] MagicBytes { get; }

  /// <summary>
  /// Gets the offset within the stream where the magic bytes should appear.
  /// </summary>
  /// <value>
  /// The zero-based byte offset. Most signatures appear at offset 0 (file start).
  /// </value>
  public int Offset { get; }

  /// <summary>
  /// Gets the minimum stream length required for signature matching.
  /// </summary>
  /// <value>
  /// The minimum number of bytes that must be available in the stream for
  /// reliable detection. This is typically <see cref="Offset"/> plus the
  /// length of <see cref="MagicBytes"/>.
  /// </value>
  public int MinimumLength { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="ArchiveSignature"/> class.
  /// </summary>
  /// <param name="magicBytes">
  /// The magic byte sequence. Must not be <see langword="null"/> or empty.
  /// </param>
  /// <param name="offset">
  /// The offset within the stream where the magic bytes should appear.
  /// Must be zero or greater.
  /// </param>
  /// <param name="minimumLength">
  /// The minimum stream length required for detection. Must be at least
  /// <paramref name="offset"/> plus the length of <paramref name="magicBytes"/>.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="magicBytes"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="magicBytes"/> is empty, or when
  /// <paramref name="offset"/> is negative, or when <paramref name="minimumLength"/>
  /// is less than the required minimum.
  /// </exception>
  public ArchiveSignature(byte[] magicBytes, int offset, int minimumLength)
  {
    if (magicBytes == null)
    {
      throw new ArgumentNullException(nameof(magicBytes));
    }

    if (magicBytes.Length == 0)
    {
      throw new ArgumentException(
        "Magic bytes must not be empty.",
        nameof(magicBytes));
    }

    if (offset < 0)
    {
      throw new ArgumentException(
        "Offset must be zero or greater.",
        nameof(offset));
    }

    int requiredMinimum = offset + magicBytes.Length;
    if (minimumLength < requiredMinimum)
    {
      throw new ArgumentException(
        $"Minimum length must be at least {requiredMinimum} " +
        $"(offset {offset} + magic bytes length {magicBytes.Length}).",
        nameof(minimumLength));
    }

    // Create defensive copy to prevent external modification
    MagicBytes = new byte[magicBytes.Length];
    Array.Copy(magicBytes, MagicBytes, magicBytes.Length);

    Offset = offset;
    MinimumLength = minimumLength;
  }
}