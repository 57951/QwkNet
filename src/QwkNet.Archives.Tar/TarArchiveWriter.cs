using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using QwkNet.Archive;

namespace QwkNet.Archives.Tar;

/// <summary>
/// Provides write access for creating TAR archives.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="TarWriter"/> from the .NET Standard Library
/// to create TAR archives in USTAR format for maximum compatibility.
/// </para>
/// <para>
/// Files are buffered in memory until <see cref="Save"/> is called, at which point
/// the complete archive is written to the output stream.
/// </para>
/// </remarks>
public sealed class TarArchiveWriter : IArchiveWriter
{
  private readonly List<PendingFile> _files;
  private bool _saved;
  private bool _disposed;

  /// <summary>
  /// Initialises a new instance of the <see cref="TarArchiveWriter"/> class.
  /// </summary>
  public TarArchiveWriter()
  {
    _files = new List<PendingFile>();
  }

  /// <summary>
  /// Adds a file to the archive.
  /// </summary>
  /// <param name="name">
  /// The name of the file within the archive. Must not be <see langword="null"/> or empty.
  /// </param>
  /// <param name="content">
  /// A stream containing the file contents. The stream is read from its current
  /// position to the end.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="name"/> or <paramref name="content"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="name"/> is empty or contains null characters.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <see cref="Save"/> has already been called.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// Thrown when the writer has been disposed.
  /// </exception>
  public void AddFile(string name, Stream content)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    if (content == null)
    {
      throw new ArgumentNullException(nameof(content));
    }

    ThrowIfDisposed();

    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException(
        "File name must not be empty or whitespace.",
        nameof(name));
    }

    if (name.Contains('\0'))
    {
      throw new ArgumentException(
        "File name must not contain null characters.",
        nameof(name));
    }

    if (_saved)
    {
      throw new InvalidOperationException(
        "Cannot add files after archive has already been saved.");
    }

    // Normalise backslashes to forward slashes for TAR compatibility
    string normalisedName = name.Replace('\\', '/');

    // Read content into memory buffer
    MemoryStream buffer = new MemoryStream();
    content.CopyTo(buffer);
    buffer.Position = 0;

    _files.Add(new PendingFile
    {
      Name = normalisedName,
      Content = buffer
    });
  }

  /// <summary>
  /// Finalises the archive and writes it to the output stream.
  /// </summary>
  /// <param name="output">
  /// The stream to write the completed archive to. The stream must be writable.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="output"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="output"/> is not writable.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <see cref="Save"/> has already been called.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// Thrown when the writer has been disposed.
  /// </exception>
  public void Save(Stream output)
  {
    if (output == null)
    {
      throw new ArgumentNullException(nameof(output));
    }

    ThrowIfDisposed();

    if (!output.CanWrite)
    {
      throw new ArgumentException(
        "Output stream must be writable.",
        nameof(output));
    }

    if (_saved)
    {
      throw new InvalidOperationException(
        "Archive has already been saved.");
    }

    // Write TAR archive using USTAR format
    using (TarWriter tarWriter = new TarWriter(output, TarEntryFormat.Ustar, leaveOpen: true))
    {
      foreach (PendingFile file in _files)
      {
        // Create USTAR entry
        UstarTarEntry entry = new UstarTarEntry(
          entryType: TarEntryType.RegularFile,
          entryName: file.Name);

        entry.DataStream = file.Content;
        entry.Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite |
                     UnixFileMode.GroupRead | UnixFileMode.OtherRead; // 0644

        tarWriter.WriteEntry(entry);
      }
    }

    _saved = true;

    // Dispose file buffers
    foreach (PendingFile file in _files)
    {
      file.Content?.Dispose();
    }
  }

  /// <summary>
  /// Releases resources used by the <see cref="TarArchiveWriter"/>.
  /// </summary>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    // Dispose any remaining file buffers
    foreach (PendingFile file in _files)
    {
      file.Content?.Dispose();
    }

    _files.Clear();
    _disposed = true;
  }

  /// <summary>
  /// Throws <see cref="ObjectDisposedException"/> if this instance has been disposed.
  /// </summary>
  private void ThrowIfDisposed()
  {
    if (_disposed)
    {
      throw new ObjectDisposedException(GetType().FullName);
    }
  }

  /// <summary>
  /// Represents a file pending addition to the archive.
  /// </summary>
  private sealed class PendingFile
  {
    public string Name { get; set; } = string.Empty;
    public MemoryStream? Content { get; set; }
  }
}