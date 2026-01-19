using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QwkNet.Archive;
using QwkNet.Archive.Zip;
using QwkNet.Core;
using QwkNet.Encoding;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;
using QwkNet.Parsing;
using QwkNet.Validation;

namespace QwkNet;

/// <summary>
/// Represents a QWK offline mail packet.
/// </summary>
/// <remarks>
/// <para>
/// QWK packets contain messages, conference information, and metadata from
/// bulletin board systems. This class provides read access to packet contents
/// whilst preserving byte-accurate fidelity to the original format.
/// </para>
/// <para>
/// Messages are eagerly loaded during Open() for simplicity (typical usage: 1-16 MB).
/// Optional files (WELCOME, NEWS, GOODBYE) are lazy-loaded on first access.
/// </para>
/// </remarks>
public sealed class QwkPacket : IDisposable
{
  private readonly IArchiveReader _archive;
  private bool _disposed;

  /// <summary>
  /// Gets the control data (CONTROL.DAT) for this packet.
  /// </summary>
  public ControlDat Control { get; }

  /// <summary>
  /// Gets the collection of messages in this packet.
  /// </summary>
  public MessageCollection Messages { get; }

  /// <summary>
  /// Gets the collection of conferences in this packet.
  /// </summary>
  public ConferenceCollection Conferences { get; }

  /// <summary>
  /// Gets the collection of optional files in this packet.
  /// </summary>
  public OptionalFileCollection OptionalFiles { get; }

  /// <summary>
  /// Gets the DOOR.ID metadata (null if not present).
  /// </summary>
  public DoorId? DoorId { get; }

  /// <summary>
  /// Gets the validation report from packet parsing.
  /// </summary>
  public ValidationReport ValidationReport { get; }

  /// <summary>
  /// The maximum number of messages in a packet.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The theoretical maximum number of messages in a QWK packet is primarily dictated
  /// by the 7-character ASCII limit for message numbers within its internal header
  /// format.
  /// </para>
  /// <para>
  /// <b>Theoretical Maximums:</b>
  /// </para>
  /// <list>
  /// <item>
  /// 9,999,999 Message Limit: Each message in the MESSAGES.DAT file starts with a
  /// 128-byte header record. This header uses a 7-byte ASCII field (positions 2
  /// through 8) to store the message number. This field is fixed at 7 characters,
  /// so the highest representable decimal number is 9,999,999.
  /// </item>
  /// <item>
  /// Storage Limitations: While the message number field allows for nearly 10 million
  /// messages, other architectural factors create practical ceilings.
  /// <list>
  /// <item>
  /// Record Count Field: The header also contains a 6-byte ASCII field that tracks the
  /// number of 128-byte blocks used by the message. This limits a single message to
  /// 999,999 blocks (approx. 128 MB).
  /// </item>
  /// <item>
  /// Filesystem Limits: Since QWK packets are typically ZIP archives, the total size
  /// of MESSAGES.DAT is technically limited by the 2GB or 4GB caps of older file systems
  /// (FAT16/FAT32), which would likely be reached long before 9.9 million messages could
  /// be stored. 
  /// </item>
  /// </list>
  /// </item>
  /// </list>
  /// </remarks>
  private const int MAX_MESSAGE_COUNT = 9_999_999;

  private QwkPacket(
    IArchiveReader archive,
    ControlDat control,
    MessageCollection messages,
    ConferenceCollection conferences,
    OptionalFileCollection optionalFiles,
    DoorId? doorId,
    ValidationReport validationReport)
  {
    _archive = archive ?? throw new ArgumentNullException(nameof(archive));
    Control = control ?? throw new ArgumentNullException(nameof(control));
    Messages = messages ?? throw new ArgumentNullException(nameof(messages));
    Conferences = conferences ?? throw new ArgumentNullException(nameof(conferences));
    OptionalFiles = optionalFiles ?? throw new ArgumentNullException(nameof(optionalFiles));
    DoorId = doorId;
    ValidationReport = validationReport ?? throw new ArgumentNullException(nameof(validationReport));
  }

  /// <summary>
  /// Opens a QWK packet from the specified file path.
  /// </summary>
  /// <param name="path">The path to the QWK packet file.</param>
  /// <param name="mode">The validation mode (default: Lenient).</param>
  /// <param name="maxMessageSizeMB">
  /// Optional maximum size in megabytes for individual messages. Messages exceeding
  /// this limit will cause validation warnings or exceptions depending on the validation mode.
  /// Default is 16MB. Pass <see langword="null"/> to use the default.
  /// </param>
  /// <param name="maxEntrySizeMB">
  /// Optional maximum size in megabytes for individual archive entries. Entries exceeding
  /// this limit will cause <see cref="InvalidDataException"/> to be thrown when opened.
  /// If not specified, defaults to the larger of 100MB or <paramref name="maxMessageSizeMB"/> × 10.
  /// Pass <see langword="null"/> to use the calculated default.
  /// </param>
  /// <returns>A new <see cref="QwkPacket"/> instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
  /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
  /// <exception cref="QwkFormatException">Thrown in Strict mode when format violations occur.</exception>
  /// <exception cref="InvalidDataException">Thrown when an archive entry exceeds the maximum size limit.</exception>
  public static QwkPacket Open(string path, ValidationMode mode = ValidationMode.Lenient, int? maxMessageSizeMB = 16, int? maxEntrySizeMB = null)
  {
    if (path == null)
    {
      throw new ArgumentNullException(nameof(path));
    }

    if (!File.Exists(path))
    {
      throw new FileNotFoundException("QWK packet file not found.", path);
    }

    int effectiveMaxEntrySizeMB = maxEntrySizeMB ?? Math.Max(100, (maxMessageSizeMB ?? 16) * 10);

    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    ZipArchiveReader archive = new ZipArchiveReader(fileStream, leaveOpen: false, maxEntrySizeMB: effectiveMaxEntrySizeMB);

    try
    {
      return OpenFromArchive(archive, mode, maxMessageSizeMB);
    }
    catch
    {
      archive.Dispose();
      throw;
    }
  }

  /// <summary>
  /// Opens a QWK packet from the specified stream.
  /// </summary>
  /// <param name="stream">The stream containing the QWK packet data.</param>
  /// <param name="mode">The validation mode (default: Lenient).</param>
  /// <param name="maxMessageSizeMB">
  /// Optional maximum size in megabytes for individual messages. Messages exceeding
  /// this limit will cause validation warnings or exceptions depending on the validation mode.
  /// Default is 16MB. Pass <see langword="null"/> to use the default.
  /// </param>
  /// <param name="maxEntrySizeMB">
  /// Optional maximum size in megabytes for individual archive entries. Entries exceeding
  /// this limit will cause <see cref="InvalidDataException"/> to be thrown when opened.
  /// If not specified, defaults to the larger of 100MB or <paramref name="maxMessageSizeMB"/> × 10.
  /// Pass <see langword="null"/> to use the calculated default.
  /// </param>
  /// <returns>A new <see cref="QwkPacket"/> instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
  /// <exception cref="QwkFormatException">Thrown in Strict mode when format violations occur.</exception>
  /// <exception cref="InvalidDataException">Thrown when an archive entry exceeds the maximum size limit.</exception>
  public static QwkPacket Open(Stream stream, ValidationMode mode = ValidationMode.Lenient, int? maxMessageSizeMB = 16, int? maxEntrySizeMB = null)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    int effectiveMaxEntrySizeMB = maxEntrySizeMB ?? Math.Max(100, (maxMessageSizeMB ?? 16) * 10);

    ZipArchiveReader archive = new ZipArchiveReader(stream, leaveOpen: true, maxEntrySizeMB: effectiveMaxEntrySizeMB);

    try
    {
      return OpenFromArchive(archive, mode, maxMessageSizeMB);
    }
    catch
    {
      archive.Dispose();
      throw;
    }
  }

  /// <summary>
  /// Opens a QWK packet from a memory buffer.
  /// </summary>
  /// <param name="data">The packet data (ZIP format).</param>
  /// <param name="mode">The validation mode (default: Lenient).</param>
  /// <param name="maxMessageSizeMB">
  /// Optional maximum size in megabytes for individual messages. Messages exceeding
  /// this limit will cause validation warnings or exceptions depending on the validation mode.
  /// Default is 16MB. Pass <see langword="null"/> to use the default.
  /// </param>
  /// <param name="maxEntrySizeMB">
  /// Optional maximum size in megabytes for individual archive entries. Entries exceeding
  /// this limit will cause <see cref="InvalidDataException"/> to be thrown when opened.
  /// If not specified, defaults to the larger of 100MB or <paramref name="maxMessageSizeMB"/> × 10.
  /// Pass <see langword="null"/> to use the calculated default.
  /// </param>
  /// <returns>A new <see cref="QwkPacket"/> instance.</returns>
  /// <exception cref="ArgumentException">Thrown when data is empty.</exception>
  /// <exception cref="QwkFormatException">Thrown in Strict mode when format violations occur.</exception>
  /// <exception cref="InvalidDataException">Thrown when an archive entry exceeds the maximum size limit.</exception>
  public static QwkPacket Open(ReadOnlyMemory<byte> data, ValidationMode mode = ValidationMode.Lenient, int? maxMessageSizeMB = 16, int? maxEntrySizeMB = null)
  {
    if (data.Length == 0)
    {
      throw new ArgumentException("Packet data cannot be empty.", nameof(data));
    }

    MemoryStream stream = new MemoryStream(data.ToArray());
    return Open(stream, mode, maxMessageSizeMB, maxEntrySizeMB);
  }

  /// <summary>
  /// Validates the packet with the current validation mode.
  /// </summary>
  /// <returns>The validation report.</returns>
  public ValidationReport Validate()
  {
    // Return the existing report (validation already performed during Open)
    return ValidationReport;
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    if (!_disposed)
    {
      _archive?.Dispose();
      _disposed = true;
    }
  }

  private static QwkPacket OpenFromArchive(IArchiveReader archive, ValidationMode mode, int? maxMessageSizeMB = 16)
  {
    ValidationContext context = new ValidationContext(mode);

    // Parse CONTROL.DAT (required)
    ControlDat control = ParseControlDat(archive, mode, context);

    // Parse DOOR.ID (optional)
    DoorId? doorId = ParseDoorId(archive, mode, context);

    // Parse MESSAGES.DAT (required)
    List<Message> messages = ParseMessages(archive, context, mode, maxMessageSizeMB);

    // Create collections
    MessageCollection messageCollection = new MessageCollection(messages);
    ConferenceCollection conferenceCollection = new ConferenceCollection(control.Conferences);
    OptionalFileCollection optionalFiles = new OptionalFileCollection(archive);

    // Create validation report from context
    ValidationReport report = ValidationReport.FromContext(context);

    // In strict mode, fail if there are errors
    if (mode == ValidationMode.Strict && context.HasErrors)
    {
      throw new QwkFormatException($"Packet validation failed with {context.Issues.Count} error(s).");
    }

    return new QwkPacket(archive, control, messageCollection, conferenceCollection, optionalFiles, doorId, report);
  }

  private static ControlDat ParseControlDat(IArchiveReader archive, ValidationMode mode, ValidationContext context)
  {
    if (!archive.FileExists("CONTROL.DAT"))
    {
      string error = "CONTROL.DAT file not found in packet.";
      context.AddError(error);

      if (mode == ValidationMode.Strict)
      {
        throw new QwkFormatException(error);
      }

      // Return minimal valid ControlDat for lenient/salvage modes
      List<string> emptyRawLines = new List<string>();
      return new ControlDat(
        "Unknown BBS", "Unknown", "555-1212", "Unknown",
        "0", "UNKNOWN", DateTimeOffset.Now, "Unknown", "",
        0, 0, 0, new List<ConferenceInfo>(),
        null, null, null, emptyRawLines);
    }

    try
    {
      using Stream stream = archive.OpenFile("CONTROL.DAT");
      return ControlDatParser.Parse(stream, mode);
    }
    catch (Exception ex)
    {
      context.AddError($"Failed to parse CONTROL.DAT: {ex.Message}");

      if (mode == ValidationMode.Strict)
      {
        throw;
      }

      // Return minimal valid ControlDat
      List<string> emptyRawLines = new List<string>();
      return new ControlDat(
        "Unknown BBS", "Unknown", "555-1212", "Unknown",
        "0", "UNKNOWN", DateTimeOffset.Now, "Unknown", "",
        0, 0, 0, new List<ConferenceInfo>(),
        null, null, null, emptyRawLines);
    }
  }

  private static DoorId? ParseDoorId(IArchiveReader archive, ValidationMode mode, ValidationContext context)
  {
    if (!archive.FileExists("DOOR.ID"))
    {
      return null; // Optional file
    }

    try
    {
      using Stream stream = archive.OpenFile("DOOR.ID");
      using MemoryStream ms = new MemoryStream();
      stream.CopyTo(ms);
      byte[] data = ms.ToArray();
      
      return DoorIdParser.Parse(data, mode, context);
    }
    catch (Exception ex)
    {
      context.AddWarning($"Failed to parse DOOR.ID: {ex.Message}");
      return null;
    }
  }

  /// <summary>
  /// Determines whether a 128-byte block is a plausible QWK message header.
  /// </summary>
  /// <param name="headerBytes">The block to validate.</param>
  /// <returns>
  /// <c>true</c> if the block appears to be a valid message header; otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// This performs heuristic validation to distinguish message headers from body blocks:
  /// <list type="bullet">
  /// <item><description>Status byte in printable ASCII range (0x20-0x7E)</description></item>
  /// <item><description>Date field has delimiters at positions 2 and 5 (MM-DD-YY or MM/DD/YY format)</description></item>
  /// <item><description>Time field has colon at position 2 (HH:MM format)</description></item>
  /// <item><description>Alive flag is 0xE1 (alive) or 0xE2 (killed)</description></item>
  /// </list>
  /// <para>
  /// The validation supports both hyphen and slash date delimiters per the library's support for
  /// multiple real-world QWK date format variants (MM-DD-YY, MM/DD/YY, MM-DD-YYYY, MM/DD/YYYY).
  /// </para>
  /// <para>
  /// False positive rate is approximately 1 in 13 million, making this extremely reliable for
  /// distinguishing genuine headers from body text blocks.
  /// </para>
  /// </remarks>
  private static bool IsPlausibleMessageHeader(ReadOnlySpan<byte> headerBytes)
  {
    // Check 1: Block must be exactly 128 bytes
    if (headerBytes.Length != 128)
    {
      return false;
    }

    // Check 2: Status byte must be in printable ASCII range (0x20-0x7E)
    // Valid status bytes: ' ', '-', '*', '+', '~', '`', '%', '^', '!', '#', '$'
    // Body blocks often start with 0xE3 (line terminator) or control characters
    byte statusByte = headerBytes[0];
    if (statusByte < 0x20 || statusByte > 0x7E)
    {
      return false;
    }

    // Check 3: Date field must have delimiters at positions 2 and 5
    // Date is at bytes 8-15 (8 bytes: "MM-DD-YY" or "MM/DD/YY")
    // Delimiters at absolute positions 10 and 13 (relative positions 2 and 5)
    // Support both hyphen '-' (0x2D) and slash '/' (0x2F) per Milestone 3
    byte delimiter1 = headerBytes[10];
    byte delimiter2 = headerBytes[13];

    // Delimiters must be consistent (both hyphens OR both slashes)
    bool hasHyphens = (delimiter1 == (byte)'-' && delimiter2 == (byte)'-');
    bool hasSlashes = (delimiter1 == (byte)'/' && delimiter2 == (byte)'/');

    if (!hasHyphens && !hasSlashes)
    {
      return false;
    }

    // Check 4: Time field must have colon at position 2
    // Time is at bytes 16-20 (5 bytes: "HH:MM")
    // Colon at absolute position 18 (relative position 2)
    if (headerBytes[18] != (byte)':')
    {
      return false;
    }

    // Check 5: Alive flag must be valid (0xE1 or 0xE2)
    // This is a very strong discriminator - body text at byte 122 is essentially random
    // Probability of exactly 0xE1 or 0xE2 is approximately 0.8% (2/256)
    byte aliveFlag = headerBytes[122];
    if (aliveFlag != 0xE1 && aliveFlag != 0xE2)
    {
      return false;
    }

    // All validation checks passed - this is almost certainly a valid message header
    // False positive probability: approximately 1 in 13 million
    return true;
  }

  private static List<Message> ParseMessages(IArchiveReader archive, ValidationContext context, ValidationMode mode, int? maxMessageSizeMB = 16)
  {
    List<Message> messages = new List<Message>();

    if (!archive.FileExists("MESSAGES.DAT"))
    {
      context.AddWarning("MESSAGES.DAT file not found - packet has no messages.");
      return messages;
    }

    try
    {
      using Stream stream = archive.OpenFile("MESSAGES.DAT");

      // Skip first 128-byte copyright record
      byte[] copyrightBlock = new byte[128];
      int copyrightRead = stream.Read(copyrightBlock, 0, 128);
      if (copyrightRead < 128)
      {
        context.AddWarning("MESSAGES.DAT is too small (missing copyright block).");
        return messages;
      }

      // Read messages until stream exhausted
      int messageNumber = 1;

      while (true)
      {
        // Read 128-byte header
        byte[] headerBytes = new byte[128];
        int headerRead = stream.Read(headerBytes, 0, 128);

        if (headerRead == 0)
        {
          break; // End of stream
        }

        if (headerRead < 128)
        {
          context.AddWarning($"Message {messageNumber}: Incomplete header block ({headerRead} bytes).");
          break;
        }

        // Validate header structure before attempting to parse
        // This prevents body blocks from being misinterpreted as message headers
        if (!IsPlausibleMessageHeader(headerBytes))
        {
          long estimatedOffset = 128 + ((long)(messageNumber - 1) * 128);

          // Determine which delimiter type was found (if any) for diagnostics
          byte delim1 = headerBytes[10];
          byte delim2 = headerBytes[13];
          bool hasDateDelimiters =
            (delim1 == (byte)'-' && delim2 == (byte)'-') ||
            (delim1 == (byte)'/' && delim2 == (byte)'/');

          context.AddWarning(
            $"Block at offset ~{estimatedOffset} does not appear to be a valid message header. " +
            $"This may indicate a malformed packet or incorrect block count in a previous message. " +
            $"Status: 0x{headerBytes[0]:X2}, " +
            $"Date delimiters: {hasDateDelimiters}, " +
            $"Time colon: {headerBytes[18] == (byte)':'}, " +
            $"Alive flag: 0x{headerBytes[122]:X2}");

          // Skip this invalid block and continue to next block
          // In lenient/salvage mode, we attempt to recover by continuing
          continue;
        }

        try
        {
          QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

          // Parse block count (number of 128-byte body blocks, header is block 1)
          int totalBlocks = header.BlockCount;
          int bodyBlockCount = Math.Max(0, totalBlocks - 1);

          // Validate block count against maximum message size to prevent decompression bomb attacks
          if (maxMessageSizeMB.HasValue)
          {
            long maxBytes = maxMessageSizeMB.Value * 1024L * 1024L;
            long maxBlocks = maxBytes / 128L;
            long messageBytes = totalBlocks * 128L;

            if (totalBlocks > maxBlocks)
            {
              double messageSizeMB = messageBytes / (1024.0 * 1024.0);
              string error = $"Message {messageNumber}: Block count {totalBlocks} exceeds maximum ({maxBlocks} blocks, {maxMessageSizeMB}MB). Message size: {messageSizeMB:F2}MB.";

              if (mode == ValidationMode.Strict)
              {
                throw new QwkFormatException(error);
              }

              context.AddWarning(error);
              // Skip this message in lenient/salvage mode
              messageNumber++;
              continue;
            }

            // Also validate against remaining stream bytes if stream is seekable
            if (stream.CanSeek)
            {
              long remainingBytes = stream.Length - stream.Position;
              if (messageBytes > remainingBytes)
              {
                string error = $"Message {messageNumber}: Block count {totalBlocks} exceeds remaining stream bytes ({remainingBytes} bytes).";
                context.AddWarning(error);

                if (mode == ValidationMode.Strict)
                {
                  throw new QwkFormatException(error);
                }

                // Skip this message in lenient/salvage mode
                messageNumber++;
                continue;
              }
            }
          }

          // Read message body blocks
          List<byte[]> bodyBlocks = new List<byte[]>();
          for (int i = 0; i < bodyBlockCount; i++)
          {
            byte[] block = new byte[128];
            int blockRead = stream.Read(block, 0, 128);

            if (blockRead == 0)
            {
              context.AddWarning($"Message {messageNumber}: Missing body block {i + 1}/{bodyBlockCount}.");
              break;
            }

            if (blockRead < 128)
            {
              context.AddWarning($"Message {messageNumber}: Incomplete body block {i + 1}/{bodyBlockCount} ({blockRead} bytes).");
              break;
            }

            bodyBlocks.Add(block);
          }

          // Parse body lines using MessageBodyParser
          List<string> bodyLines = MessageBodyParser.ParseLines(bodyBlocks.ToArray());
          
          // Extract kludges from body lines (QWKE extended headers)
          List<MessageKludge> kludges = ExtractKludges(ref bodyLines);
          
          // Reconstruct raw text from body blocks for MessageBody constructor
          StringBuilder rawTextBuilder = new StringBuilder();
          foreach (byte[] block in bodyBlocks)
          {
            rawTextBuilder.Append(Cp437Encoding.Decode(block));
          }
          string rawText = rawTextBuilder.ToString();
          
          MessageBody body = new MessageBody(bodyLines, rawText);

          // Parse status from status byte
          MessageStatus status = ParseStatus(header.StatusByte);

          // Parse date/time
          DateTime? messageDateTime = null;
          if (header.TryGetDateTime(out DateTime parsedDateTime))
          {
            messageDateTime = parsedDateTime;
          }

          // Create kludge collection
          MessageKludgeCollection kludgeCollection = new MessageKludgeCollection(kludges);

          // Parse reference number (ASCII field to integer)
          int referenceNumber = 0;
          if (!string.IsNullOrWhiteSpace(header.ReferenceNumber))
          {
            int.TryParse(header.ReferenceNumber.Trim(), out referenceNumber);
          }

          Message message = new Message(
            messageNumber++,
            header.ConferenceNumber,
            header.From,
            header.To,
            header.Subject,
            messageDateTime,
            referenceNumber,
            header.Password,
            body,
            status,
            kludgeCollection,
            header);

          messages.Add(message);
        }
        catch (Exception ex)
        {
          context.AddWarning($"Failed to parse message {messageNumber}: {ex.Message}");
          messageNumber++;
        }
      }

      context.AddInfo($"Parsed {messages.Count} message(s) from MESSAGES.DAT.");
    }
    catch (Exception ex)
    {
      context.AddError($"Failed to read MESSAGES.DAT: {ex.Message}");
    }

    if (messages.Count >= MAX_MESSAGE_COUNT)
    {
      context.AddWarning($"Message count ({messages.Count}) exceeds maximum ({MAX_MESSAGE_COUNT}).");
    }

    return messages;
  }

  /// <summary>
  /// Extracts kludge lines from the beginning of the body lines.
  /// </summary>
  /// <remarks>
  /// QWKE extended headers (To:, From:, Subject:) appear as kludge lines at the
  /// beginning of the message body. This method extracts them and removes them
  /// from the body lines list.
  /// </remarks>
  private static List<MessageKludge> ExtractKludges(ref List<string> bodyLines)
  {
    List<MessageKludge> kludges = new List<MessageKludge>();

    if (bodyLines.Count == 0)
    {
      return kludges;
    }

    int kludgeLineCount = 0;

    // Kludges appear at the start of the message
    for (int i = 0; i < bodyLines.Count; i++)
    {
      string line = bodyLines[i];

      // Kludge lines must contain a colon
      int colonIndex = line.IndexOf(':');
      if (colonIndex < 1)
      {
        // Not a kludge line, stop scanning
        break;
      }

      string key = line.Substring(0, colonIndex).Trim();
      string value = colonIndex + 1 < line.Length
        ? line.Substring(colonIndex + 1).TrimStart()
        : string.Empty;

      // Only extract if key looks like a kludge (single word, no spaces)
      if (key.Contains(" ") || string.IsNullOrWhiteSpace(key))
      {
        // Not a kludge, stop scanning
        break;
      }

      // Create kludge with raw line preserved
      MessageKludge kludge = new MessageKludge(key, value, line);
      kludges.Add(kludge);
      kludgeLineCount++;
    }

    // Remove kludge lines from body
    if (kludgeLineCount > 0)
    {
      bodyLines.RemoveRange(0, kludgeLineCount);
    }

    return kludges;
  }

  private static MessageStatus ParseStatus(byte statusByte)
  {
    MessageStatus status = MessageStatus.None;

    // Check individual bits based on QWK specification
    char statusChar = (char)statusByte;

    switch (statusChar)
    {
      case ' ': // Public, unread
        break;
      case '-': // Public, read
        status = MessageStatus.Read;
        break;
      case '*': // Private, unread
        status = MessageStatus.Private;
        break;
      case '+': // Private, read
        status = MessageStatus.Private | MessageStatus.Read;
        break;
      case '~': // Comment to sysop, unread
        status = MessageStatus.CommentToSysop;
        break;
      case '`': // Comment to sysop, read
        status = MessageStatus.CommentToSysop | MessageStatus.Read;
        break;
      case '%': // Sender password protected, unread
        status = MessageStatus.Private | MessageStatus.SenderPasswordProtected;
        break;
      case '^': // Sender password protected, read
        status = MessageStatus.Private | MessageStatus.SenderPasswordProtected | MessageStatus.Read;
        break;
      case '!': // Group password protected, unread
        status = MessageStatus.Private | MessageStatus.GroupPasswordProtected;
        break;
      case '#': // Group password protected, read
        status = MessageStatus.Private | MessageStatus.GroupPasswordProtected | MessageStatus.Read;
        break;
      case '$': // Group password protected to ALL
        status = MessageStatus.GroupPasswordProtected;
        break;
    }

    return status;
  }
}