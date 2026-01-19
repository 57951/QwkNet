using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QwkNet.Archive.Extensions;
using QwkNet.Archive.Zip;

namespace QwkNet.Archive;

/// <summary>
/// Factory for creating archive readers and writers.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides convenient methods for opening archives with automatic
/// format detection or explicit format specification.
/// </para>
/// <para>
/// Only ZIP format is built-in. Other formats require separate extension packages
/// that implement <see cref="IArchiveExtension"/> and must be explicitly registered
/// using <see cref="RegisterExtension"/>.
/// </para>
/// <para>
/// <b>Extension Detection:</b> When using automatic detection, registered extensions
/// are tested first in registration order, followed by built-in ZIP detection.
/// Extensions with longer magic byte signatures take precedence over shorter ones.
/// </para>
/// </remarks>
public static class ArchiveFactory
{
  private static readonly object _registryLock = new object();
  private static readonly List<IArchiveExtension> _extensions = new List<IArchiveExtension>();

  /// <summary>
  /// Registers an archive format extension.
  /// </summary>
  /// <param name="extension">
  /// The extension to register. Must not be <see langword="null"/>.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="extension"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when an extension with the same <see cref="IArchiveExtension.FormatId"/>
  /// is already registered.
  /// </exception>
  /// <remarks>
  /// <para>
  /// Extensions are tested during automatic detection in the order they were registered.
  /// Registration order is preserved for deterministic resolution.
  /// </para>
  /// <para>
  /// This method is thread-safe and may be called concurrently.
  /// </para>
  /// </remarks>
  public static void RegisterExtension(IArchiveExtension extension)
  {
    if (extension == null)
    {
      throw new ArgumentNullException(nameof(extension));
    }

    lock (_registryLock)
    {
      // Check for duplicate FormatId
      if (_extensions.Any(e => e.FormatId == extension.FormatId))
      {
        throw new InvalidOperationException(
          $"An extension with format identifier '{extension.FormatId}' is already registered.");
      }

      _extensions.Add(extension);
    }
  }

  /// <summary>
  /// Unregisters an archive format extension.
  /// </summary>
  /// <param name="formatId">
  /// The format identifier of the extension to unregister.
  /// </param>
  /// <returns>
  /// <see langword="true"/> if the extension was found and removed;
  /// otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// This method is thread-safe and may be called concurrently.
  /// </remarks>
  public static bool UnregisterExtension(ArchiveFormatId formatId)
  {
    lock (_registryLock)
    {
      int index = _extensions.FindIndex(e => e.FormatId == formatId);
      if (index >= 0)
      {
        _extensions.RemoveAt(index);
        return true;
      }
      return false;
    }
  }

  /// <summary>
  /// Lists all registered archive format extensions.
  /// </summary>
  /// <returns>
  /// A read-only list of <see cref="ArchiveFormatId"/> values for all registered extensions,
  /// in registration order.
  /// </returns>
  /// <remarks>
  /// The built-in ZIP format is not included in this list as it does not require registration.
  /// </remarks>
  public static IReadOnlyList<ArchiveFormatId> ListRegisteredExtensions()
  {
    lock (_registryLock)
    {
      return _extensions.Select(e => e.FormatId).ToList();
    }
  }

  /// <summary>
  /// Opens an archive from a file path with automatic format detection.
  /// </summary>
  /// <param name="path">The path to the archive file.</param>
  /// <param name="maxEntrySizeMB">
  /// Optional maximum size in megabytes for individual archive entries. Entries exceeding
  /// this limit will cause <see cref="InvalidDataException"/> to be thrown when opened.
  /// Default is 100MB. Pass <see langword="null"/> to use the default.
  /// </param>
  /// <returns>
  /// An <see cref="IArchiveReader"/> for accessing the archive contents.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="path"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="FileNotFoundException">
  /// Thrown when the specified file does not exist.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Thrown when the archive format is not supported.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the file is not a valid archive of the detected format, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  /// <remarks>
  /// The returned reader owns the underlying file stream and will close it
  /// when disposed.
  /// </remarks>
  public static IArchiveReader OpenArchive(string path, int? maxEntrySizeMB = 100)
  {
    if (path == null)
    {
      throw new ArgumentNullException(nameof(path));
    }

    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Archive file not found.", path);
    }

    // Open file stream for detection and reading
    FileStream stream = File.OpenRead(path);

    try
    {
      return OpenArchive(stream, leaveOpen: false, maxEntrySizeMB: maxEntrySizeMB);
    }
    catch
    {
      stream?.Dispose();
      throw;
    }
  }

  /// <summary>
  /// Opens an archive from a stream with automatic format detection.
  /// </summary>
  /// <param name="stream">
  /// The stream containing the archive. Must be readable and seekable.
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
  /// <returns>
  /// An <see cref="IArchiveReader"/> for accessing the archive contents.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Thrown when the archive format is not supported.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid archive of the detected format, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  /// <remarks>
  /// Detection order: registered extensions (by signature), then built-in ZIP detection.
  /// </remarks>
  public static IArchiveReader OpenArchive(Stream stream, bool leaveOpen = false, int? maxEntrySizeMB = 100)
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

    // Try extension signatures first
    IArchiveExtension? matchedExtension = TryDetectExtension(stream);
    if (matchedExtension != null)
    {
      if (!matchedExtension.SupportsReading)
      {
        throw new NotSupportedException(
          $"Archive format '{matchedExtension.FormatId}' does not support reading.");
      }

      return matchedExtension.CreateReader(stream, leaveOpen, maxEntrySizeMB);
    }

    // Fall back to built-in ZIP detection
    ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

    if (format == ArchiveFormat.Unknown)
    {
      throw new NotSupportedException(
        "Unable to determine archive format from stream.");
    }

    return OpenArchive(stream, format, leaveOpen, maxEntrySizeMB);
  }

  /// <summary>
  /// Opens an archive from a stream with explicit format specification.
  /// </summary>
  /// <param name="stream">
  /// The stream containing the archive. Must be readable and seekable.
  /// </param>
  /// <param name="format">
  /// The archive format.
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
  /// <returns>
  /// An <see cref="IArchiveReader"/> for accessing the archive contents.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Thrown when <paramref name="format"/> is not supported.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid archive of the specified format, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  public static IArchiveReader OpenArchive(
    Stream stream,
    ArchiveFormat format,
    bool leaveOpen = false,
    int? maxEntrySizeMB = 100)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    return format switch
    {
      ArchiveFormat.Zip => new ZipArchiveReader(stream, leaveOpen, maxEntrySizeMB),
      ArchiveFormat.Unknown => throw new NotSupportedException(
        "Cannot open archive with unknown format."),
      _ => throw new NotSupportedException(
        $"Archive format '{format}' is not supported. " +
        $"An extension package may be required.")
    };
  }

  /// <summary>
  /// Opens an archive from a stream with explicit format identifier.
  /// </summary>
  /// <param name="stream">
  /// The stream containing the archive. Must be readable and seekable.
  /// </param>
  /// <param name="formatId">
  /// The archive format identifier.
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
  /// <returns>
  /// An <see cref="IArchiveReader"/> for accessing the archive contents.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Thrown when <paramref name="formatId"/> is not supported or the format
  /// does not support reading.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid archive of the specified format, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  public static IArchiveReader OpenArchive(
    Stream stream,
    ArchiveFormatId formatId,
    bool leaveOpen = false,
    int? maxEntrySizeMB = 100)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // Check for built-in ZIP
    if (formatId == ArchiveFormatId.Zip)
    {
      return new ZipArchiveReader(stream, leaveOpen, maxEntrySizeMB);
    }

    // Check registered extensions
    IArchiveExtension? extension = FindExtension(formatId);
    if (extension != null)
    {
      if (!extension.SupportsReading)
      {
        throw new NotSupportedException(
          $"Archive format '{formatId}' does not support reading.");
      }

      return extension.CreateReader(stream, leaveOpen, maxEntrySizeMB);
    }

    throw new NotSupportedException(
      $"Archive format '{formatId}' is not supported. " +
      $"An extension package may be required.");
  }

  /// <summary>
  /// Creates an archive writer for the specified format.
  /// </summary>
  /// <param name="format">The archive format to create.</param>
  /// <returns>
  /// An <see cref="IArchiveWriter"/> for creating an archive.
  /// </returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when <paramref name="format"/> is not supported.
  /// </exception>
  /// <remarks>
  /// Files are added to the writer with <see cref="IArchiveWriter.AddFile"/>,
  /// then the archive is finalised with <see cref="IArchiveWriter.Save"/>.
  /// </remarks>
  public static IArchiveWriter CreateWriter(ArchiveFormat format)
  {
    return format switch
    {
      ArchiveFormat.Zip => new ZipArchiveWriter(),
      ArchiveFormat.Unknown => throw new NotSupportedException(
        "Cannot create archive writer with unknown format."),
      _ => throw new NotSupportedException(
        $"Archive format '{format}' is not supported for writing. " +
        $"An extension package may be required.")
    };
  }

  /// <summary>
  /// Creates an archive writer for the specified format identifier.
  /// </summary>
  /// <param name="formatId">The archive format identifier.</param>
  /// <returns>
  /// An <see cref="IArchiveWriter"/> for creating an archive.
  /// </returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when <paramref name="formatId"/> is not supported or the format
  /// does not support writing.
  /// </exception>
  /// <remarks>
  /// Files are added to the writer with <see cref="IArchiveWriter.AddFile"/>,
  /// then the archive is finalised with <see cref="IArchiveWriter.Save"/>.
  /// </remarks>
  public static IArchiveWriter CreateWriter(ArchiveFormatId formatId)
  {
    // Check for built-in ZIP
    if (formatId == ArchiveFormatId.Zip)
    {
      return new ZipArchiveWriter();
    }

    // Check registered extensions
    IArchiveExtension? extension = FindExtension(formatId);
    if (extension != null)
    {
      if (!extension.SupportsWriting)
      {
        throw new NotSupportedException(
          $"Archive format '{formatId}' does not support writing.");
      }

      return extension.CreateWriter();
    }

    throw new NotSupportedException(
      $"Archive format '{formatId}' is not supported for writing. " +
      $"An extension package may be required.");
  }

  /// <summary>
  /// Attempts to detect an extension by matching signatures against the stream.
  /// </summary>
  /// <param name="stream">The stream to test. Must be seekable.</param>
  /// <returns>
  /// The matched extension, or <see langword="null"/> if no extension matched.
  /// </returns>
  private static IArchiveExtension? TryDetectExtension(Stream stream)
  {
    long originalPosition = stream.Position;

    try
    {
      // Get snapshot of registered extensions
      List<IArchiveExtension> extensions;
      lock (_registryLock)
      {
        extensions = new List<IArchiveExtension>(_extensions);
      }

      // Determine maximum buffer size needed
      int maxBufferSize = 0;
      foreach (IArchiveExtension extension in extensions)
      {
        foreach (ArchiveSignature signature in extension.Signatures)
        {
          int required = signature.Offset + signature.MagicBytes.Length;
          if (required > maxBufferSize)
          {
            maxBufferSize = required;
          }
        }
      }

      if (maxBufferSize == 0)
      {
        return null; // No signatures to test
      }

      // Read header bytes once
      stream.Position = 0;
      byte[] buffer = new byte[maxBufferSize];
      int bytesRead = stream.Read(buffer, 0, buffer.Length);

      // Test extensions in order, preferring longer magic bytes
      IArchiveExtension? bestMatch = null;
      int bestMagicLength = 0;

      foreach (IArchiveExtension extension in extensions)
      {
        foreach (ArchiveSignature signature in extension.Signatures)
        {
          if (MatchesSignature(buffer, bytesRead, signature))
          {
            // Prefer longer signatures (more specific)
            if (signature.MagicBytes.Length > bestMagicLength)
            {
              bestMatch = extension;
              bestMagicLength = signature.MagicBytes.Length;
            }
            else if (signature.MagicBytes.Length == bestMagicLength && bestMatch == null)
            {
              // Equal length, earlier registration wins
              bestMatch = extension;
            }
          }
        }
      }

      return bestMatch;
    }
    finally
    {
      // Restore original position
      stream.Position = originalPosition;
    }
  }

  /// <summary>
  /// Checks if a buffer matches a signature.
  /// </summary>
  /// <param name="buffer">The buffer to check.</param>
  /// <param name="bytesRead">The number of valid bytes in the buffer.</param>
  /// <param name="signature">The signature to match.</param>
  /// <returns>
  /// <see langword="true"/> if the signature matches; otherwise, <see langword="false"/>.
  /// </returns>
  private static bool MatchesSignature(byte[] buffer, int bytesRead, ArchiveSignature signature)
  {
    // Check minimum length
    if (bytesRead < signature.MinimumLength)
    {
      return false;
    }

    // Check offset bounds
    if (signature.Offset + signature.MagicBytes.Length > bytesRead)
    {
      return false;
    }

    // Match magic bytes
    for (int i = 0; i < signature.MagicBytes.Length; i++)
    {
      if (buffer[signature.Offset + i] != signature.MagicBytes[i])
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Finds a registered extension by format identifier.
  /// </summary>
  /// <param name="formatId">The format identifier to search for.</param>
  /// <returns>
  /// The matching extension, or <see langword="null"/> if not found.
  /// </returns>
  private static IArchiveExtension? FindExtension(ArchiveFormatId formatId)
  {
    lock (_registryLock)
    {
      return _extensions.FirstOrDefault(e => e.FormatId == formatId);
    }
  }

}