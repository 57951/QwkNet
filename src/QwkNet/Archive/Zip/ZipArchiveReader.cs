using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace QwkNet.Archive.Zip;

/// <summary>
/// Provides read access to ZIP archive contents using System.IO.Compression.
/// </summary>
/// <remarks>
/// <para>
/// This implementation wraps <see cref="ZipArchive"/> to provide QWK packet access.
/// File name matching is case-insensitive to accommodate real-world variations.
/// </para>
/// <para>
/// Streams returned by <see cref="OpenFile"/> must be disposed by the caller.
/// The underlying <see cref="ZipArchive"/> remains open until this reader is disposed.
/// </para>
/// </remarks>
public sealed class ZipArchiveReader : IArchiveReader
{
  private readonly ZipArchive _archive;
  private readonly bool _leaveOpen;
  private readonly long _maxEntrySizeBytes;
  private bool _disposed;

  /// <summary>
  /// Initialises a new instance of the <see cref="ZipArchiveReader"/> class.
  /// </summary>
  /// <param name="stream">
  /// The stream containing the ZIP archive. Must be readable and seekable.
  /// </param>
  /// <param name="leaveOpen">
  /// <see langword="true"/> to leave the stream open after the reader is disposed;
  /// otherwise, <see langword="false"/>.
  /// </param>
  /// <param name="maxEntrySizeMB">
  /// Optional maximum size in megabytes for individual archive entries. Entries exceeding
  /// this limit will cause <see cref="InvalidDataException"/> to be thrown when opened.
  /// Default is 100MB. Pass <see langword="null"/> to use the default.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid ZIP archive, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  public ZipArchiveReader(Stream stream, bool leaveOpen = false, int? maxEntrySizeMB = 100)
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

    _leaveOpen = leaveOpen;
    _maxEntrySizeBytes = (maxEntrySizeMB ?? 100) * 1024L * 1024L;

    try
    {
      _archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen);
    }
    catch (InvalidDataException)
    {
      // Re-throw with consistent exception for invalid ZIP files
      throw;
    }
  }

  /// <inheritdoc />
  public IReadOnlyList<string> ListFiles()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    List<string> fileNames = new List<string>(_archive.Entries.Count);

    foreach (ZipArchiveEntry entry in _archive.Entries)
    {
      // Exclude directory entries (entries ending with /)
      if (!entry.FullName.EndsWith('/'))
      {
        fileNames.Add(entry.FullName);
      }
    }

    return fileNames.AsReadOnly();
  }

  /// <inheritdoc />
  /// <exception cref="InvalidDataException">
  /// Thrown when the archive entry exceeds the maximum size limit configured in the constructor.
  /// </exception>
  public Stream OpenFile(string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    ObjectDisposedException.ThrowIf(_disposed, this);

    // Find entry case-insensitively
    ZipArchiveEntry? entry = FindEntry(name);

    if (entry == null)
    {
      throw new FileNotFoundException(
        $"File '{name}' not found in archive.",
        name);
    }

    // Validate entry size before opening to prevent decompression bomb attacks
    if (entry.Length > _maxEntrySizeBytes)
    {
      double entrySizeMB = entry.Length / (1024.0 * 1024.0);
      double maxSizeMB = _maxEntrySizeBytes / (1024.0 * 1024.0);
      throw new InvalidDataException(
        $"Archive entry '{name}' exceeds maximum size ({maxSizeMB:F0}MB). " +
        $"Entry size: {entrySizeMB:F2}MB.");
    }

    // Open and return stream - caller owns it
    return entry.Open();
  }

  /// <inheritdoc />
  public bool FileExists(string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    ObjectDisposedException.ThrowIf(_disposed, this);

    return FindEntry(name) != null;
  }

  /// <summary>
  /// Finds a ZIP entry by name using case-insensitive comparison.
  /// </summary>
  /// <param name="name">The file name to search for.</param>
  /// <returns>
  /// The matching <see cref="ZipArchiveEntry"/>, or <see langword="null"/> if not found.
  /// </returns>
  private ZipArchiveEntry? FindEntry(string name)
  {
    // Direct match first (fast path)
    ZipArchiveEntry? entry = _archive.GetEntry(name);
    if (entry != null)
    {
      return entry;
    }

    // Case-insensitive fallback
    return _archive.Entries.FirstOrDefault(e =>
      string.Equals(e.FullName, name, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Releases the resources used by the <see cref="ZipArchiveReader"/>.
  /// </summary>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _archive?.Dispose();
    _disposed = true;
  }
}