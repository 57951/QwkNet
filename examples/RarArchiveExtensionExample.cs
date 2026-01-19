using System;
using System.Collections.Generic;
using System.IO;
using QwkNet.Archive;
using QwkNet.Archive.Extensions;

namespace QwkNet.Archives.Rar;

/// <summary>
/// Example archive extension for RAR format support.
/// </summary>
/// <remarks>
/// <para>
/// This is a STUB implementation demonstrating how third-party developers
/// can create their own archive extensions for QWK.NET.
/// </para>
/// <para>
/// A real RAR implementation would use SharpCompress or similar library.
/// This stub proves the extension system works without adding dependencies.
/// </para>
/// </remarks>
public sealed class RarArchiveExtension : IArchiveExtension
{
  /// <summary>
  /// Gets the format identifier for RAR archives.
  /// </summary>
  public ArchiveFormatId FormatId => ArchiveFormatId.From("rar");

  /// <summary>
  /// Gets the magic byte signatures used to detect RAR archives.
  /// </summary>
  public IReadOnlyList<ArchiveSignature> Signatures { get; }

  /// <summary>
  /// Gets a value indicating whether this extension supports reading archives.
  /// </summary>
  public bool SupportsReading => true;

  /// <summary>
  /// Gets a value indicating whether this extension supports writing archives.
  /// </summary>
  public bool SupportsWriting => false; // RAR writing requires proprietary SDK

  /// <summary>
  /// Initialises a new instance of the <see cref="RarArchiveExtension"/> class.
  /// </summary>
  public RarArchiveExtension()
  {
    // RAR 4.x magic bytes: "Rar!\x1A\x07\x00" at offset 0
    byte[] rar4Magic = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 };
    
    // RAR 5.x magic bytes: "Rar!\x1A\x07\x01\x00" at offset 0
    byte[] rar5Magic = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 };

    Signatures = new List<ArchiveSignature>
    {
      new ArchiveSignature(rar5Magic, offset: 0, minimumLength: 8),
      new ArchiveSignature(rar4Magic, offset: 0, minimumLength: 7)
    };
  }

  /// <summary>
  /// Creates a RAR archive reader.
  /// </summary>
  public IArchiveReader CreateReader(Stream stream, bool leaveOpen)
  {
    throw new NotImplementedException(
      "RAR reading requires SharpCompress or similar library. " +
      "This is a stub demonstrating the extension system.");
  }

  /// <summary>
  /// Creates a RAR archive writer.
  /// </summary>
  public IArchiveWriter CreateWriter()
  {
    throw new NotSupportedException(
      "RAR writing is not supported (requires WinRAR proprietary SDK).");
  }
}