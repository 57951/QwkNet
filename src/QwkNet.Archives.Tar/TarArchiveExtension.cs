using System;
using System.Collections.Generic;
using System.IO;
using QwkNet.Archive;

namespace QwkNet.Archives.Tar;

/// <summary>
/// Archive extension for TAR format support.
/// </summary>
/// <remarks>
/// <para>
/// This extension adds TAR (Tape ARchive) format support to QWK.NET.
/// TAR is a simple archive format originally designed for sequential tape
/// storage but commonly used for file distribution and backup.
/// </para>
/// <para>
/// This implementation supports the USTAR (Uniform Standard Tape Archive) format,
/// which is the most widely compatible TAR variant. The magic bytes "ustar\0"
/// at offset 257 are used for automatic format detection.
/// </para>
/// <para>
/// <b>Licensing:</b> This extension is provided under the MIT licence to ensure
/// compatibility with QWK.NET's MIT licence.
/// </para>
/// </remarks>
public sealed class TarArchiveExtension : QwkNet.Archive.Extensions.IArchiveExtension
{
  /// <summary>
  /// Gets the format identifier for TAR archives.
  /// </summary>
  /// <value>
  /// The string "tar" identifying this archive format.
  /// </value>
  public ArchiveFormatId FormatId => ArchiveFormatId.From("tar");

  /// <summary>
  /// Gets the magic byte signatures used to detect TAR archives.
  /// </summary>
  /// <value>
  /// A list containing the USTAR magic byte signature.
  /// </value>
  public IReadOnlyList<QwkNet.Archive.Extensions.ArchiveSignature> Signatures { get; }

  /// <summary>
  /// Gets a value indicating whether this extension supports reading archives.
  /// </summary>
  /// <value>
  /// <see langword="true"/> — TAR reading is supported.
  /// </value>
  public bool SupportsReading => true;

  /// <summary>
  /// Gets a value indicating whether this extension supports writing archives.
  /// </summary>
  /// <value>
  /// <see langword="true"/> — TAR writing is supported.
  /// </value>
  public bool SupportsWriting => true;

  /// <summary>
  /// Initialises a new instance of the <see cref="TarArchiveExtension"/> class.
  /// </summary>
  public TarArchiveExtension()
  {
    // USTAR magic bytes: "ustar\0" at offset 257
    // TAR header is 512 bytes, magic is at offset 257-262 (6 bytes)
    byte[] ustarMagic = new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72, 0x00 }; // "ustar\0"

    Signatures = new List<QwkNet.Archive.Extensions.ArchiveSignature>
    {
      new QwkNet.Archive.Extensions.ArchiveSignature(
        magicBytes: ustarMagic,
        offset: 257,
        minimumLength: 263) // Must have at least offset + magic length
    };
  }

  /// <summary>
  /// Creates a TAR archive reader.
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
  /// <returns>
  /// A <see cref="TarArchiveReader"/> instance for accessing the archive contents.
  /// </returns>
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
  public IArchiveReader CreateReader(Stream stream, bool leaveOpen, int? maxEntrySizeMB = 100)
  {
    return new TarArchiveReader(stream, leaveOpen, maxEntrySizeMB);
  }

  /// <summary>
  /// Creates a TAR archive writer.
  /// </summary>
  /// <returns>
  /// A <see cref="TarArchiveWriter"/> instance for creating TAR archives.
  /// </returns>
  public IArchiveWriter CreateWriter()
  {
    return new TarArchiveWriter();
  }
}