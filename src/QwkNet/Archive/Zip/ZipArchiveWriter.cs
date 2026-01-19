using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace QwkNet.Archive.Zip;

/// <summary>
/// Provides write access for creating ZIP archives using System.IO.Compression.
/// </summary>
/// <remarks>
/// <para>
/// This implementation buffers files in memory until <see cref="Save"/> is called,
/// then writes them to the output stream as a complete ZIP archive.
/// </para>
/// <para>
/// After <see cref="Save"/> is called, no further files may be added.
/// </para>
/// </remarks>
public sealed class ZipArchiveWriter : IArchiveWriter
{
  private readonly Dictionary<string, byte[]> _files;
  private bool _saved;
  private bool _disposed;

  /// <summary>
  /// Initialises a new instance of the <see cref="ZipArchiveWriter"/> class.
  /// </summary>
  public ZipArchiveWriter()
  {
    _files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    _saved = false;
    _disposed = false;
  }

  /// <inheritdoc />
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

    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("File name cannot be empty or whitespace.", nameof(name));
    }

    ObjectDisposedException.ThrowIf(_disposed, this);

    if (_saved)
    {
      throw new InvalidOperationException(
        "Cannot add files after Save() has been called.");
    }

    // Read content into memory
    using (MemoryStream buffer = new MemoryStream())
    {
      content.CopyTo(buffer);
      _files[name] = buffer.ToArray();
    }
  }

  /// <inheritdoc />
  public void Save(Stream output)
  {
    if (output == null)
    {
      throw new ArgumentNullException(nameof(output));
    }

    if (!output.CanWrite)
    {
      throw new ArgumentException("Stream must be writable.", nameof(output));
    }

    ObjectDisposedException.ThrowIf(_disposed, this);

    if (_saved)
    {
      throw new InvalidOperationException(
        "Save() has already been called.");
    }

    // Create ZIP archive
    using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (KeyValuePair<string, byte[]> file in _files)
      {
        ZipArchiveEntry entry = archive.CreateEntry(
          file.Key,
          CompressionLevel.Optimal);

        using (Stream entryStream = entry.Open())
        {
          entryStream.Write(file.Value, 0, file.Value.Length);
        }
      }
    }

    // Flush the output stream
    output.Flush();

    _saved = true;
  }

  /// <summary>
  /// Releases the resources used by the <see cref="ZipArchiveWriter"/>.
  /// </summary>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _files?.Clear();
    _disposed = true;
  }
}