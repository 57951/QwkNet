using System;
using System.Collections.Generic;
using System.IO;

namespace QwkNet.Archive.Extensions;

/// <summary>
/// Defines the contract for archive format extensions.
/// </summary>
/// <remarks>
/// <para>
/// Extension developers implement this interface to add support for archive
/// formats beyond the built-in ZIP support (e.g., RAR, 7z, TAR).
/// </para>
/// <para>
/// Extensions must be explicitly registered with <see cref="ArchiveFactory"/>
/// using <see cref="ArchiveFactory.RegisterExtension"/>. There is no automatic
/// discovery or assembly scanning.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be stateless and thread-safe,
/// as a single extension instance may be used concurrently across multiple threads.
/// </para>
/// <para>
/// <b>Licensing:</b> Extension packages must use permissive licences (MIT, BSD, ISC)
/// to ensure compatibility with QWK.NET's MIT licence.
/// </para>
/// </remarks>
public interface IArchiveExtension
{
  /// <summary>
  /// Gets the unique format identifier for this extension.
  /// </summary>
  /// <value>
  /// An <see cref="ArchiveFormatId"/> that uniquely identifies this archive format.
  /// This identifier is used for explicit format specification and must be unique
  /// across all registered extensions.
  /// </value>
  ArchiveFormatId FormatId { get; }

  /// <summary>
  /// Gets the magic byte signatures used to detect this archive format.
  /// </summary>
  /// <value>
  /// A read-only list of <see cref="ArchiveSignature"/> instances. May be empty
  /// if the format cannot be reliably detected by signature alone (in which case
  /// only explicit format specification is supported).
  /// </value>
  /// <remarks>
  /// When multiple signatures are provided, detection succeeds if any signature
  /// matches. Signatures are tested in the order they appear in this list.
  /// </remarks>
  IReadOnlyList<ArchiveSignature> Signatures { get; }

  /// <summary>
  /// Gets a value indicating whether this extension supports reading archives.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if <see cref="CreateReader"/> can be called;
  /// otherwise, <see langword="false"/>.
  /// </value>
  bool SupportsReading { get; }

  /// <summary>
  /// Gets a value indicating whether this extension supports writing archives.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if <see cref="CreateWriter"/> can be called;
  /// otherwise, <see langword="false"/>.
  /// </value>
  bool SupportsWriting { get; }

  /// <summary>
  /// Creates an archive reader for this format.
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
  /// An <see cref="IArchiveReader"/> instance for accessing the archive contents.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="stream"/> is not readable or not seekable.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Thrown when <see cref="SupportsReading"/> is <see langword="false"/>.
  /// </exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when the stream does not contain a valid archive of this format, or when an archive
  /// entry exceeds the maximum size limit specified by <paramref name="maxEntrySizeMB"/>.
  /// </exception>
  /// <remarks>
  /// The stream is positioned at the start before being passed to the reader
  /// implementation. Implementations should validate the stream contains their
  /// expected format and throw <see cref="InvalidDataException"/> if not.
  /// </remarks>
  IArchiveReader CreateReader(Stream stream, bool leaveOpen, int? maxEntrySizeMB = 100);

  /// <summary>
  /// Creates an archive writer for this format.
  /// </summary>
  /// <returns>
  /// An <see cref="IArchiveWriter"/> instance for creating archives.
  /// </returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when <see cref="SupportsWriting"/> is <see langword="false"/>.
  /// </exception>
  /// <remarks>
  /// Writers are constructed independently of any output stream. Files are added
  /// with <see cref="IArchiveWriter.AddFile"/>, then the archive is finalised
  /// with <see cref="IArchiveWriter.Save"/>.
  /// </remarks>
  IArchiveWriter CreateWriter();
}