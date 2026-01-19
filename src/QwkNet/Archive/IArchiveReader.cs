using System;
using System.Collections.Generic;
using System.IO;

namespace QwkNet.Archive;

/// <summary>
/// Provides read access to archive contents.
/// </summary>
/// <remarks>
/// <para>
/// Implementers wrap archive formats (ZIP, RAR, 7z, etc.) to provide
/// uniform access to files within compressed QWK packets.
/// </para>
/// <para>
/// Streams returned by <see cref="OpenFile"/> are owned by the caller
/// and must be disposed when no longer needed.
/// </para>
/// </remarks>
public interface IArchiveReader : IDisposable
{
  /// <summary>
  /// Lists all file names contained in the archive.
  /// </summary>
  /// <returns>
  /// A read-only list of file names as they appear in the archive.
  /// File names preserve their original casing.
  /// </returns>
  /// <remarks>
  /// File names are returned in the order they appear in the archive.
  /// Duplicate file names (if the archive format allows them) are included.
  /// </remarks>
  IReadOnlyList<string> ListFiles();

  /// <summary>
  /// Opens a file from the archive for reading.
  /// </summary>
  /// <param name="name">
  /// The name of the file to open. Matching is case-insensitive.
  /// </param>
  /// <returns>
  /// A stream containing the file contents. The caller must dispose this stream.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="name"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="FileNotFoundException">
  /// Thrown when the specified file does not exist in the archive.
  /// </exception>
  /// <remarks>
  /// <para>
  /// File name matching is case-insensitive to accommodate real-world QWK packets
  /// where file names may appear as MESSAGES.DAT, messages.dat, or Messages.Dat.
  /// </para>
  /// <para>
  /// The returned stream is positioned at the beginning of the file contents.
  /// Multiple concurrent streams may be opened if the underlying archive format
  /// supports it, but callers should not assume this behaviour.
  /// </para>
  /// </remarks>
  Stream OpenFile(string name);

  /// <summary>
  /// Determines whether a file with the specified name exists in the archive.
  /// </summary>
  /// <param name="name">
  /// The name of the file to check. Matching is case-insensitive.
  /// </param>
  /// <returns>
  /// <see langword="true"/> if the file exists; otherwise, <see langword="false"/>.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="name"/> is <see langword="null"/>.
  /// </exception>
  bool FileExists(string name);
}