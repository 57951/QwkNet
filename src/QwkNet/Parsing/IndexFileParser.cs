using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using QwkNet.Core;
using QwkNet.Models.Indexing;
using QwkNet.Validation;

namespace QwkNet.Parsing;

/// <summary>
/// Parses QWK index (.NDX) files into structured <see cref="IndexFile"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Index files contain 4-byte MSBIN floating-point values representing record offsets
/// into MESSAGES.DAT. Each conference has its own index file named by conference
/// number (e.g., 0.NDX, 1.NDX, 123.NDX).
/// </para>
/// <para>
/// The parser validates that:
/// - File size is a multiple of 4 bytes
/// - All record offsets are within the MESSAGES.DAT file bounds (if provided)
/// - Message numbers are sequential starting from 1
/// </para>
/// </remarks>
public static class IndexFileParser
{
  private const int MsbinFloatSize = 4;
  private const int QwkRecordSize = 128;

  /// <summary>
  /// Parses an index file from a byte array.
  /// </summary>
  /// <param name="data">The raw .NDX file contents.</param>
  /// <param name="conferenceNumber">The conference number this index represents.</param>
  /// <param name="validationMode">The validation mode to use for error handling.</param>
  /// <param name="messagesDatFileSize">
  /// Optional MESSAGES.DAT file size for validation. If provided, record offsets
  /// will be checked to ensure they don't exceed this size.
  /// </param>
  /// <returns>An <see cref="IndexFile"/> containing the parsed entries.</returns>
  /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
  /// <exception cref="QwkFormatException">
  /// Thrown in strict mode when the file is malformed or validation fails.
  /// </exception>
  public static IndexFile Parse(
    ReadOnlyMemory<byte> data,
    int conferenceNumber,
    ValidationMode validationMode = ValidationMode.Lenient,
    long? messagesDatFileSize = null)
  {
    if (data.IsEmpty)
    {
      return new IndexFile(conferenceNumber, Array.Empty<IndexEntry>(), true, messagesDatFileSize);
    }

    ValidationContext context = new ValidationContext(validationMode);

    // Validate file size is multiple of 4
    if (data.Length % MsbinFloatSize != 0)
    {
      context.AddError(
        $"Index file size ({data.Length} bytes) is not a multiple of {MsbinFloatSize}.",
        "IndexFile");

      if (validationMode == ValidationMode.Strict)
      {
        throw new QwkFormatException(
          $"Invalid index file size: {data.Length} bytes (expected multiple of {MsbinFloatSize}).",
          "IndexFile");
      }
    }

    // Calculate how many complete entries we can read
    int entryCount = data.Length / MsbinFloatSize;
    List<IndexEntry> entries = new List<IndexEntry>(entryCount);

    ReadOnlySpan<byte> span = data.Span;

    for (int i = 0; i < entryCount; i++)
    {
      int offset = i * MsbinFloatSize;
      ReadOnlySpan<byte> msbinBytes = span.Slice(offset, MsbinFloatSize);

      try
      {
        double recordOffsetDouble = MsbinConverter.ToDouble(msbinBytes);
        int recordOffset = (int)recordOffsetDouble;

        // Validate record offset is non-negative
        if (recordOffset < 0)
        {
          context.AddWarning(
            $"Entry {i + 1}: Negative record offset ({recordOffset}). Skipping entry.",
            "IndexEntry");
          continue;
        }

        // Validate record offset doesn't exceed MESSAGES.DAT size
        if (messagesDatFileSize.HasValue)
        {
          long byteOffset = (long)recordOffset * QwkRecordSize;
          if (byteOffset >= messagesDatFileSize.Value)
          {
            context.AddWarning(
              $"Entry {i + 1}: Record offset {recordOffset} (byte {byteOffset}) exceeds MESSAGES.DAT size ({messagesDatFileSize.Value} bytes).",
              "IndexEntry");

            if (validationMode == ValidationMode.Strict)
            {
              throw new QwkFormatException(
                $"Index entry {i + 1} points beyond MESSAGES.DAT file (offset {recordOffset}, file size {messagesDatFileSize.Value}).",
                "IndexEntry");
            }

            // In lenient/salvage mode, skip this entry
            continue;
          }
        }

        // Create entry with sequential message number (1-based)
        // Use entries.Count + 1 to ensure sequential numbering even when skipping invalid entries
        byte[] msbinCopy = msbinBytes.ToArray();
        IndexEntry entry = new IndexEntry(entries.Count + 1, recordOffset, msbinCopy);
        entries.Add(entry);
      }
      catch (Exception ex) when (ex is not QwkFormatException)
      {
        context.AddError(
          $"Entry {i + 1}: Failed to parse MSBIN float: {ex.Message}",
          "IndexEntry");

        if (validationMode == ValidationMode.Strict)
        {
          throw new QwkFormatException(
            $"Failed to parse index entry {i + 1}: {ex.Message}",
            "IndexEntry",
            ex);
        }
      }
    }

    // Determine if index is valid based on validation issues
    // An index is invalid if there are any errors OR warnings (indicating skipped/problematic data)
    bool isValid = !context.HasErrors && !context.HasWarnings;

    return new IndexFile(conferenceNumber, entries, isValid, messagesDatFileSize);
  }

  /// <summary>
  /// Parses an index file from a stream.
  /// </summary>
  /// <param name="stream">The stream containing the .NDX file data.</param>
  /// <param name="conferenceNumber">The conference number this index represents.</param>
  /// <param name="validationMode">The validation mode to use for error handling.</param>
  /// <param name="messagesDatFileSize">
  /// Optional MESSAGES.DAT file size for validation.
  /// </param>
  /// <returns>An <see cref="IndexFile"/> containing the parsed entries.</returns>
  /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
  /// <exception cref="QwkFormatException">
  /// Thrown in strict mode when the file is malformed or validation fails.
  /// </exception>
  public static IndexFile Parse(
    Stream stream,
    int conferenceNumber,
    ValidationMode validationMode = ValidationMode.Lenient,
    long? messagesDatFileSize = null)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // Read entire stream into memory
    // Index files are typically small (5 bytes per message), so this is acceptable
    byte[] buffer;
    if (stream.CanSeek)
    {
      int length = (int)stream.Length;
      buffer = ArrayPool<byte>.Shared.Rent(length);
      try
      {
        int bytesRead = stream.Read(buffer, 0, length);
        return Parse(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), conferenceNumber, validationMode, messagesDatFileSize);
      }
      finally
      {
        ArrayPool<byte>.Shared.Return(buffer);
      }
    }
    else
    {
      using (MemoryStream ms = new MemoryStream())
      {
        stream.CopyTo(ms);
        return Parse(ms.ToArray(), conferenceNumber, validationMode, messagesDatFileSize);
      }
    }
  }

  /// <summary>
  /// Parses an index file from a file path.
  /// </summary>
  /// <param name="filePath">The path to the .NDX file.</param>
  /// <param name="conferenceNumber">The conference number this index represents.</param>
  /// <param name="validationMode">The validation mode to use for error handling.</param>
  /// <param name="messagesDatFileSize">
  /// Optional MESSAGES.DAT file size for validation.
  /// </param>
  /// <returns>An <see cref="IndexFile"/> containing the parsed entries.</returns>
  /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
  /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
  /// <exception cref="QwkFormatException">
  /// Thrown in strict mode when the file is malformed or validation fails.
  /// </exception>
  public static IndexFile ParseFile(
    string filePath,
    int conferenceNumber,
    ValidationMode validationMode = ValidationMode.Lenient,
    long? messagesDatFileSize = null)
  {
    if (filePath == null)
    {
      throw new ArgumentNullException(nameof(filePath));
    }

    if (!File.Exists(filePath))
    {
      throw new FileNotFoundException($"Index file not found: {filePath}", filePath);
    }

    byte[] data = File.ReadAllBytes(filePath);
    return Parse(data, conferenceNumber, validationMode, messagesDatFileSize);
  }
}