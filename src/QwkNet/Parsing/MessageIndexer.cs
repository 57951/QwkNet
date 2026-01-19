using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using QwkNet.Core;
using QwkNet.Models.Indexing;

namespace QwkNet.Parsing;

/// <summary>
/// Generates QWK index (.NDX) files from MESSAGES.DAT by enumerating message headers.
/// </summary>
/// <remarks>
/// <para>
/// This utility scans MESSAGES.DAT sequentially, identifies message headers for each
/// conference, and generates conference-specific index files containing MSBIN-encoded
/// record offsets.
/// </para>
/// <para>
/// Index generation is primarily used when creating REP packets or rebuilding
/// missing/corrupted indexes from existing QWK packets.
/// </para>
/// </remarks>
public static class MessageIndexer
{
  private const int QwkRecordSize = 128;

  /// <summary>
  /// Generates index files for all conferences in a MESSAGES.DAT file.
  /// </summary>
  /// <param name="messagesDatStream">The stream containing MESSAGES.DAT.</param>
  /// <returns>
  /// A dictionary mapping conference numbers to their respective <see cref="IndexFile"/> instances.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when messagesDatStream is null.</exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the stream is not readable or seekable.
  /// </exception>
  /// <remarks>
  /// The first record in MESSAGES.DAT is always a header record and is skipped.
  /// Message numbering within each conference starts at 1 and increments sequentially.
  /// </remarks>
  public static Dictionary<int, IndexFile> GenerateIndexes(Stream messagesDatStream)
  {
    if (messagesDatStream == null)
    {
      throw new ArgumentNullException(nameof(messagesDatStream));
    }

    if (!messagesDatStream.CanRead)
    {
      throw new InvalidOperationException("Stream must be readable.");
    }

    if (!messagesDatStream.CanSeek)
    {
      throw new InvalidOperationException("Stream must be seekable.");
    }

    // Dictionary mapping conference number to list of index entries
    Dictionary<int, List<IndexEntry>> conferenceEntries = new Dictionary<int, List<IndexEntry>>();

    // Dictionary to track message numbers per conference
    Dictionary<int, int> conferenceMessageNumbers = new Dictionary<int, int>();

    byte[] recordBuffer = ArrayPool<byte>.Shared.Rent(QwkRecordSize);
    try
    {
      // Skip the first record (MESSAGES.DAT header)
      messagesDatStream.Position = QwkRecordSize;

      int recordOffset = 1; // Start at record 1 (after header)

      while (true)
      {
        int bytesRead = messagesDatStream.Read(recordBuffer, 0, QwkRecordSize);
        if (bytesRead == 0)
        {
          break; // End of file
        }

        if (bytesRead < QwkRecordSize)
        {
          // Incomplete record at end of file - ignore it
          break;
        }

        // Parse the message header to get conference number
        ReadOnlySpan<byte> headerSpan = new ReadOnlySpan<byte>(recordBuffer, 0, QwkRecordSize);
        QwkMessageHeader header = QwkMessageHeader.Parse(headerSpan);

        int conferenceNumber = header.ConferenceNumber;

        // Initialise conference tracking if not seen before
        if (!conferenceEntries.ContainsKey(conferenceNumber))
        {
          conferenceEntries[conferenceNumber] = new List<IndexEntry>();
          conferenceMessageNumbers[conferenceNumber] = 0;
        }

        // Increment message number for this conference
        conferenceMessageNumbers[conferenceNumber]++;
        int messageNumber = conferenceMessageNumbers[conferenceNumber];

        // Convert record offset to MSBIN float bytes
        double recordOffsetDouble = (double)recordOffset;
        byte[] msbinBytes = MsbinConverter.FromDouble(recordOffsetDouble);

        // Create index entry
        IndexEntry entry = new IndexEntry(messageNumber, recordOffset, msbinBytes);
        conferenceEntries[conferenceNumber].Add(entry);

        recordOffset++;
      }
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(recordBuffer);
    }

    // Convert lists to IndexFile instances
    Dictionary<int, IndexFile> result = new Dictionary<int, IndexFile>();
    long messagesDatSize = messagesDatStream.Length;

    foreach (KeyValuePair<int, List<IndexEntry>> kvp in conferenceEntries)
    {
      int conferenceNumber = kvp.Key;
      List<IndexEntry> entries = kvp.Value;

      IndexFile indexFile = new IndexFile(
        conferenceNumber,
        entries,
        isValid: true,
        validatedAgainstFileSize: messagesDatSize);

      result[conferenceNumber] = indexFile;
    }

    return result;
  }

  /// <summary>
  /// Generates an index file for a specific conference from MESSAGES.DAT.
  /// </summary>
  /// <param name="messagesDatStream">The stream containing MESSAGES.DAT.</param>
  /// <param name="targetConferenceNumber">The conference number to generate an index for.</param>
  /// <returns>
  /// An <see cref="IndexFile"/> for the specified conference, or an empty index if
  /// no messages exist for that conference.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when messagesDatStream is null.</exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the stream is not readable or seekable.
  /// </exception>
  public static IndexFile GenerateIndex(Stream messagesDatStream, int targetConferenceNumber)
  {
    if (messagesDatStream == null)
    {
      throw new ArgumentNullException(nameof(messagesDatStream));
    }

    Dictionary<int, IndexFile> allIndexes = GenerateIndexes(messagesDatStream);

    if (allIndexes.TryGetValue(targetConferenceNumber, out IndexFile? indexFile))
    {
      return indexFile;
    }

    // Conference not found - return empty index
    return new IndexFile(
      targetConferenceNumber,
      Array.Empty<IndexEntry>(),
      isValid: true,
      validatedAgainstFileSize: messagesDatStream.Length);
  }

  /// <summary>
  /// Writes an index file to a stream in QWK .NDX format.
  /// </summary>
  /// <param name="indexFile">The index file to write.</param>
  /// <param name="outputStream">The stream to write to.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when indexFile or outputStream is null.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the output stream is not writable.
  /// </exception>
  /// <remarks>
  /// The output consists of sequential 5-byte MSBIN floats representing record offsets.
  /// The raw MSBIN bytes are written directly from each <see cref="IndexEntry.RawMsbinBytes"/>.
  /// </remarks>
  public static void WriteIndex(IndexFile indexFile, Stream outputStream)
  {
    if (indexFile == null)
    {
      throw new ArgumentNullException(nameof(indexFile));
    }

    if (outputStream == null)
    {
      throw new ArgumentNullException(nameof(outputStream));
    }

    if (!outputStream.CanWrite)
    {
      throw new InvalidOperationException("Stream must be writable.");
    }

    foreach (IndexEntry entry in indexFile)
    {
      ReadOnlyMemory<byte> msbinBytes = entry.RawMsbinBytes;
      outputStream.Write(msbinBytes.Span);
    }
  }

  /// <summary>
  /// Writes an index file to a file path in QWK .NDX format.
  /// </summary>
  /// <param name="indexFile">The index file to write.</param>
  /// <param name="filePath">The output file path (typically "N.NDX" where N is the conference number).</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when indexFile or filePath is null.
  /// </exception>
  public static void WriteIndexFile(IndexFile indexFile, string filePath)
  {
    if (indexFile == null)
    {
      throw new ArgumentNullException(nameof(indexFile));
    }

    if (filePath == null)
    {
      throw new ArgumentNullException(nameof(filePath));
    }

    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
    {
      WriteIndex(indexFile, fs);
    }
  }
}