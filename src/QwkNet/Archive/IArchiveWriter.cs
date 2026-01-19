using System;
using System.IO;

namespace QwkNet.Archive;

/// <summary>
/// Provides write access for creating archives.
/// </summary>
/// <remarks>
/// <para>
/// Implementers wrap archive formats (ZIP, RAR, 7z, etc.) to provide
/// uniform archive creation for QWK reply packets.
/// </para>
/// <para>
/// Archives are constructed by repeatedly calling <see cref="AddFile"/>,
/// then finalised with <see cref="Save"/>. After <see cref="Save"/> is called,
/// no further files may be added.
/// </para>
/// </remarks>
public interface IArchiveWriter : IDisposable
{
  /// <summary>
  /// Adds a file to the archive.
  /// </summary>
  /// <param name="name">
  /// The name of the file within the archive. Must not be <see langword="null"/> or empty.
  /// </param>
  /// <param name="content">
  /// A stream containing the file contents. The stream is read from its current
  /// position to the end. The stream is not disposed by this method.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="name"/> or <paramref name="content"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="name"/> is empty or contains invalid characters
  /// for the target archive format.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <see cref="Save"/> has already been called.
  /// </exception>
  /// <remarks>
  /// <para>
  /// File names should use forward slashes (/) for directory separators where
  /// applicable. Backslashes (\) may be converted to forward slashes by some
  /// archive formats.
  /// </para>
  /// <para>
  /// The content stream is read immediately or buffered. Callers may dispose
  /// the stream after this method returns.
  /// </para>
  /// </remarks>
  void AddFile(string name, Stream content);

  /// <summary>
  /// Finalises the archive and writes it to the output stream.
  /// </summary>
  /// <param name="output">
  /// The stream to write the completed archive to. The stream must be writable.
  /// The stream is not disposed by this method.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="output"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <see cref="Save"/> has already been called.
  /// </exception>
  /// <exception cref="IOException">
  /// Thrown when an I/O error occurs during archive creation.
  /// </exception>
  /// <remarks>
  /// <para>
  /// After this method returns, no further files may be added to the archive.
  /// The archive writer should be disposed after calling <see cref="Save"/>.
  /// </para>
  /// <para>
  /// The output stream is flushed but not closed by this method.
  /// </para>
  /// </remarks>
  void Save(Stream output);
}