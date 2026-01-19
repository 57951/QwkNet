using System;
using System.IO;

namespace QwkNet.Archive;

/// <summary>
/// Detects archive formats based on file signatures (magic bytes).
/// </summary>
/// <remarks>
/// <para>
/// This utility examines the first few bytes of a file or stream to determine
/// the archive format. Detection is based on well-known magic byte sequences.
/// </para>
/// <para>
/// Currently supported formats:
/// <list type="bullet">
/// <item><description>ZIP (PK signature)</description></item>
/// </list>
/// </para>
/// </remarks>
public static class ArchiveDetector
{
  // ZIP file signatures
  private static readonly byte[] ZipSignaturePK = { 0x50, 0x4B, 0x03, 0x04 }; // PK\x03\x04
  private static readonly byte[] ZipSignatureEmpty = { 0x50, 0x4B, 0x05, 0x06 }; // PK\x05\x06 (empty archive)
  private static readonly byte[] ZipSignatureSpanned = { 0x50, 0x4B, 0x07, 0x08 }; // PK\x07\x08 (spanned archive)

  /// <summary>
  /// Detects the archive format of a file.
  /// </summary>
  /// <param name="path">The path to the file to examine.</param>
  /// <returns>
  /// An <see cref="ArchiveFormat"/> value indicating the detected format,
  /// or <see cref="ArchiveFormat.Unknown"/> if the format cannot be determined.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="path"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="FileNotFoundException">
  /// Thrown when the specified file does not exist.
  /// </exception>
  /// <exception cref="IOException">
  /// Thrown when an I/O error occurs reading the file.
  /// </exception>
  public static ArchiveFormat DetectFormat(string path)
  {
    if (path == null)
    {
      throw new ArgumentNullException(nameof(path));
    }

    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Archive file not found.", path);
    }

    using (FileStream stream = File.OpenRead(path))
    {
      return DetectFormat(stream);
    }
  }

  /// <summary>
  /// Detects the archive format of a stream.
  /// </summary>
  /// <param name="stream">
  /// The stream to examine. Must be readable and seekable.
  /// The stream position is restored after detection.
  /// </param>
  /// <returns>
  /// An <see cref="ArchiveFormat"/> value indicating the detected format,
  /// or <see cref="ArchiveFormat.Unknown"/> if the format cannot be determined.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="IOException">
  /// Thrown when an I/O error occurs reading the stream.
  /// </exception>
  public static ArchiveFormat DetectFormat(Stream stream)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    if (!stream.CanRead)
    {
      throw new ArgumentException("Stream must be readable.", nameof(stream));
    }

    if (!stream.CanSeek)
    {
      throw new ArgumentException("Stream must be seekable.", nameof(stream));
    }

    // Save original position
    long originalPosition = stream.Position;

    try
    {
      // Seek to beginning to read magic bytes
      stream.Position = 0;

      // Read first 8 bytes for magic number detection
      byte[] buffer = new byte[8];
      int bytesRead = stream.Read(buffer, 0, buffer.Length);

      if (bytesRead < 4)
      {
        // Not enough bytes to determine format
        return ArchiveFormat.Unknown;
      }

      // Check for ZIP signatures
      if (MatchesSignature(buffer, ZipSignaturePK) ||
          MatchesSignature(buffer, ZipSignatureEmpty) ||
          MatchesSignature(buffer, ZipSignatureSpanned))
      {
        return ArchiveFormat.Zip;
      }

      // Future: Add detection for other formats here
      // - RAR: 0x52 0x61 0x72 0x21 0x1A 0x07
      // - 7z:  0x37 0x7A 0xBC 0xAF 0x27 0x1C
      // - etc.

      return ArchiveFormat.Unknown;
    }
    finally
    {
      // Restore original position
      stream.Position = originalPosition;
    }
  }

  /// <summary>
  /// Checks if a buffer starts with a specific signature.
  /// </summary>
  /// <param name="buffer">The buffer to check.</param>
  /// <param name="signature">The signature to match.</param>
  /// <returns>
  /// <see langword="true"/> if the buffer starts with the signature;
  /// otherwise, <see langword="false"/>.
  /// </returns>
  private static bool MatchesSignature(byte[] buffer, byte[] signature)
  {
    if (buffer.Length < signature.Length)
    {
      return false;
    }

    for (int i = 0; i < signature.Length; i++)
    {
      if (buffer[i] != signature[i])
      {
        return false;
      }
    }

    return true;
  }
}

/// <summary>
/// Represents supported archive formats.
/// </summary>
public enum ArchiveFormat
{
  /// <summary>
  /// Unknown or unsupported archive format.
  /// </summary>
  Unknown = 0,

  /// <summary>
  /// ZIP archive format (PKZIP-compatible).
  /// </summary>
  Zip = 1
}