using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using QwkNet.Archive;

namespace QwkNet.Archives.Tar;

/// <summary>
/// Provides read access to TAR archive contents.
/// </summary>
/// <remarks>
/// <para>
/// This reader wraps the <see cref="System.Formats.Tar"/> namespace to provide
/// TAR archive support for QWK.NET. It supports standard POSIX TAR format
/// (ustar) and is compatible with archives created by GNU tar, BSD tar, and
/// other POSIX-compliant implementations.
/// </para>
/// <para>
/// TAR archives are uncompressed by default. For compressed TAR archives
/// (.tar.gz, .tar.bz2), decompress the stream before passing it to this reader.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Do not use the same
/// instance across multiple threads.
/// </para>
/// </remarks>
public sealed class TarArchiveReader : IArchiveReader
{
  private readonly Stream _stream;
  private readonly bool _leaveOpen;
  private readonly Dictionary<string, TarEntryMetadata> _entries;
  private readonly long _maxEntrySizeBytes;
  private bool _disposed;

  /// <summary>
  /// Initialises a new instance of the <see cref="TarArchiveReader"/> class.
  /// </summary>
  /// <param name="stream">
  /// The stream containing the TAR archive. Must be readable and seekable.
  /// </param>
  /// <param name="leaveOpen">
  /// <see langword="true"/> to leave the stream open after the reader is disposed;
  /// otherwise, <see langword="false"/>.
  /// </param>
  /// <param name="maxEntrySizeMB">
  /// Optional maximum size in megabytes for individual archive entries. Entries exceeding
  /// this limit will cause <see cref="InvalidDataException"/> to be thrown during index
  /// construction. Default is 100MB. Pass <see langword="null"/> to use the default.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid TAR archive, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  public TarArchiveReader(Stream stream, bool leaveOpen, int? maxEntrySizeMB = 100)
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

    _stream = stream;
    _leaveOpen = leaveOpen;
    _maxEntrySizeBytes = (maxEntrySizeMB ?? 100) * 1024L * 1024L;
    _entries = new Dictionary<string, TarEntryMetadata>(StringComparer.OrdinalIgnoreCase);

    // Build entry index
    BuildEntryIndex();
  }

  /// <summary>
  /// Lists all file names contained in the archive.
  /// </summary>
  /// <returns>
  /// A read-only list of file names as they appear in the archive.
  /// File names preserve their original casing.
  /// </returns>
  /// <remarks>
  /// Only regular files are included. Directories, symbolic links, and other
  /// special entry types are excluded from the listing.
  /// </remarks>
  public IReadOnlyList<string> ListFiles()
  {
    ThrowIfDisposed();
    return _entries.Keys.ToList();
  }

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
  /// <exception cref="ObjectDisposedException">
  /// Thrown when this reader has been disposed.
  /// </exception>
  /// <remarks>
  /// The returned stream is a copy of the file data in memory. Multiple
  /// concurrent streams can be opened safely.
  /// </remarks>
  public Stream OpenFile(string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    ThrowIfDisposed();

    if (!_entries.TryGetValue(name, out TarEntryMetadata? entry))
    {
      throw new FileNotFoundException(
        $"File '{name}' not found in TAR archive.",
        name);
    }

    // Return a new MemoryStream with a copy of the data
    // This ensures multiple concurrent reads are safe
    byte[] data = entry.Data.ToArray();
    return new MemoryStream(data);
  }

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
  /// <exception cref="ObjectDisposedException">
  /// Thrown when this reader has been disposed.
  /// </exception>
  public bool FileExists(string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    ThrowIfDisposed();

    return _entries.ContainsKey(name);
  }

  /// <summary>
  /// Releases all resources used by this reader.
  /// </summary>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    // Dispose all buffered data streams
    foreach (TarEntryMetadata entry in _entries.Values)
    {
      entry.Data?.Dispose();
    }
    _entries.Clear();

    if (!_leaveOpen)
    {
      _stream?.Dispose();
    }

    _disposed = true;
  }

  /// <summary>
  /// Builds an index of all regular file entries in the TAR archive.
  /// </summary>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid TAR archive, or when an archive
  /// entry exceeds the maximum size limit configured in the constructor.
  /// </exception>
  private void BuildEntryIndex()
  {
    _stream.Position = 0;

    try
    {
      using (TarReader reader = new TarReader(_stream, leaveOpen: true))
      {
        System.Formats.Tar.TarEntry? entry;
        while ((entry = reader.GetNextEntry()) != null)
        {
          // Only index regular files, skip directories and special entries
          if (entry.EntryType == TarEntryType.RegularFile ||
              entry.EntryType == TarEntryType.V7RegularFile)
          {
            string name = entry.Name;
            long length = entry.Length;

            // Validate entry size before allocation to prevent decompression bomb attacks
            if (length > _maxEntrySizeBytes)
            {
              double entrySizeMB = length / (1024.0 * 1024.0);
              double maxSizeMB = _maxEntrySizeBytes / (1024.0 * 1024.0);
              throw new InvalidDataException(
                $"Archive entry '{name}' exceeds maximum size ({maxSizeMB:F0}MB). " +
                $"Entry size: {entrySizeMB:F2}MB.");
            }

            // The entry's DataStream property provides access to the file data
            // We need to copy it to memory because TarEntry is only valid during iteration
            MemoryStream dataBuffer = new MemoryStream((int)length);
            if (entry.DataStream != null && length > 0)
            {
              entry.DataStream.CopyTo(dataBuffer);
              dataBuffer.Position = 0;
            }

            // Store entry metadata with buffered data
            _entries[name] = new TarEntryMetadata(name, length, dataBuffer);
          }
        }
      }
    }
    catch (Exception ex) when (ex is not InvalidDataException)
    {
      throw new InvalidDataException(
        "Stream does not contain a valid TAR archive.",
        ex);
    }

    // Reset stream for subsequent operations
    _stream.Position = 0;
  }

  /// <summary>
  /// Throws <see cref="ObjectDisposedException"/> if this reader has been disposed.
  /// </summary>
  private void ThrowIfDisposed()
  {
    if (_disposed)
    {
      throw new ObjectDisposedException(GetType().Name);
    }
  }

  /// <summary>
  /// Represents metadata for a TAR archive entry.
  /// </summary>
  private sealed class TarEntryMetadata
  {
    public string Name { get; }
    public long Length { get; }
    public MemoryStream Data { get; }

    public TarEntryMetadata(string name, long length, MemoryStream data)
    {
      Name = name;
      Length = length;
      Data = data;
    }
  }
}