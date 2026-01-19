using System;
using System.Collections.Generic;
using System.Linq;
using QwkNet.Archive;
using QwkNet.Models.Control;
using QwkNet.Models.Indexing;
using QwkNet.Models.Messages;

namespace QwkNet.Validation;

/// <summary>
/// Provides comprehensive validation checks for QWK packets.
/// </summary>
/// <remarks>
/// <para>
/// This class performs structural, semantic, and integrity validation
/// of QWK packet components according to the QWK specification.
/// </para>
/// <para>
/// Validation can be performed in Strict, Lenient, or Salvage mode,
/// controlling how errors are handled and reported.
/// </para>
/// </remarks>
public static class PacketValidator
{
  /// <summary>
  /// Validates the archive integrity.
  /// </summary>
  /// <param name="archive">The archive reader to validate.</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="archive"/> or <paramref name="context"/> is null.</exception>
  public static void ValidateArchiveIntegrity(IArchiveReader archive, ValidationContext context)
  {
    if (archive == null)
    {
      throw new ArgumentNullException(nameof(archive));
    }
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    // Check if archive can enumerate files
    try
    {
      IReadOnlyList<string> files = archive.ListFiles();
      if (files.Count == 0)
      {
        context.AddError("Archive contains no files.", "Archive");
      }
      else
      {
        context.AddInfo($"Archive contains {files.Count} file(s).", "Archive");
      }
    }
    catch (Exception ex)
    {
      context.AddError($"Failed to enumerate archive contents: {ex.Message}", "Archive");
    }
  }

  /// <summary>
  /// Validates that required files are present in the archive.
  /// </summary>
  /// <param name="archive">The archive reader to validate.</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="archive"/> or <paramref name="context"/> is null.</exception>
  public static void ValidateRequiredFiles(IArchiveReader archive, ValidationContext context)
  {
    if (archive == null)
    {
      throw new ArgumentNullException(nameof(archive));
    }
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    // CONTROL.DAT is required
    if (!archive.FileExists("CONTROL.DAT"))
    {
      context.AddError("Required file CONTROL.DAT not found.", "Archive");
    }

    // MESSAGES.DAT is required
    if (!archive.FileExists("MESSAGES.DAT"))
    {
      context.AddError("Required file MESSAGES.DAT not found.", "Archive");
    }

    // Optional files (informational only)
    if (archive.FileExists("DOOR.ID"))
    {
      context.AddInfo("Optional file DOOR.ID present.", "Archive");
    }

    if (archive.FileExists("WELCOME"))
    {
      context.AddInfo("Optional file WELCOME present.", "Archive");
    }

    if (archive.FileExists("NEWS"))
    {
      context.AddInfo("Optional file NEWS present.", "Archive");
    }

    if (archive.FileExists("GOODBYE"))
    {
      context.AddInfo("Optional file GOODBYE present.", "Archive");
    }

    // Check for QWKE extension files
    if (archive.FileExists("TOREADER.EXT"))
    {
      context.AddInfo("QWKE extension file TOREADER.EXT present.", "Archive");
    }

    if (archive.FileExists("TODOOR.EXT"))
    {
      context.AddInfo("QWKE extension file TODOOR.EXT present.", "Archive");
    }
  }

  /// <summary>
  /// Validates the structure of a CONTROL.DAT file.
  /// </summary>
  /// <param name="control">The parsed CONTROL.DAT data.</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="control"/> or <paramref name="context"/> is null.</exception>
  public static void ValidateControlDatStructure(ControlDat control, ValidationContext context)
  {
    if (control == null)
    {
      throw new ArgumentNullException(nameof(control));
    }
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    // Validate BBS name
    if (string.IsNullOrWhiteSpace(control.BbsName))
    {
      context.AddWarning("BBS name is empty or whitespace.", "CONTROL.DAT:BbsName");
    }

    // Validate BBS ID
    if (string.IsNullOrWhiteSpace(control.BbsId))
    {
      context.AddWarning("BBS ID is empty or whitespace.", "CONTROL.DAT:BbsId");
    }

    // Validate phone number (property is BbsPhone, not PhoneNumber)
    if (string.IsNullOrWhiteSpace(control.BbsPhone))
    {
      context.AddWarning("BBS phone number is empty or whitespace.", "CONTROL.DAT:BbsPhone");
    }

    // Validate sysop name (property is Sysop, not SysopName)
    if (string.IsNullOrWhiteSpace(control.Sysop))
    {
      context.AddWarning("Sysop name is empty or whitespace.", "CONTROL.DAT:Sysop");
    }

    // Validate registration number (may be "0" or "00000" for unregistered)
    if (string.IsNullOrWhiteSpace(control.RegistrationNumber))
    {
      context.AddWarning("Registration number is empty or whitespace.", "CONTROL.DAT:RegistrationNumber");
    }

    // Validate conferences
    if (control.Conferences.Count == 0)
    {
      context.AddWarning("No conferences defined in CONTROL.DAT.", "CONTROL.DAT:Conferences");
    }
    else
    {
      context.AddInfo($"CONTROL.DAT defines {control.Conferences.Count} conference(s).", "CONTROL.DAT:Conferences");

      // Validate individual conference names
      for (int i = 0; i < control.Conferences.Count; i++)
      {
        ConferenceInfo conf = control.Conferences[i];
        if (string.IsNullOrWhiteSpace(conf.Name))
        {
          context.AddWarning($"Conference {conf.Number} has empty or whitespace name.", $"CONTROL.DAT:Conference[{i}]");
        }
      }
    }
  }

  /// <summary>
  /// Validates message header fields for plausibility.
  /// </summary>
  /// <param name="message">The message to validate.</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="context"/> is null.</exception>
  public static void ValidateMessageHeader(Message message, ValidationContext context)
  {
    if (message == null)
    {
      throw new ArgumentNullException(nameof(message));
    }
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    string location = $"Message {message.MessageNumber}";

    // Validate To field (should not be empty for non-deleted messages)
    if (string.IsNullOrWhiteSpace(message.To) && !message.IsDeleted)
    {
      context.AddWarning("Message 'To' field is empty.", $"{location}:To");
    }

    // Validate From field
    if (string.IsNullOrWhiteSpace(message.From))
    {
      context.AddWarning("Message 'From' field is empty.", $"{location}:From");
    }

    // Validate Subject (can be empty but unusual)
    if (string.IsNullOrWhiteSpace(message.Subject))
    {
      context.AddInfo("Message subject is empty.", $"{location}:Subject");
    }

    // Message has DateTime? property, not separate Date and Time strings
    if (message.DateTime == null)
    {
      context.AddWarning("Message date/time is null or invalid.", $"{location}:DateTime");
    }

    // Validate raw header date/time format strings per QWK specification
    ValidateDateField(message.RawHeader.Date, context, $"{location}:Date");
    ValidateTimeField(message.RawHeader.Time, context, $"{location}:Time");

    // Validate conference number (should be >= 0)
    if (message.ConferenceNumber < 0)
    {
      context.AddError($"Invalid conference number: {message.ConferenceNumber}.", $"{location}:Conference");
    }

    // Validate message number (should be > 0)
    if (message.MessageNumber <= 0)
    {
      context.AddWarning($"Invalid message number: {message.MessageNumber}.", $"{location}:MessageNumber");
    }

    // Reference number is an int, validate range
    if (message.ReferenceNumber < 0)
    {
      context.AddWarning($"Negative reference number: {message.ReferenceNumber}.", $"{location}:Reference");
    }
  }

  /// <summary>
  /// Validates date field format and plausibility per QWK specification.
  /// </summary>
  /// <param name="dateString">The date string to validate (QWK format: MM-DD-YY).</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <param name="location">The location context for this field.</param>
  /// <remarks>
  /// QWK specification requires date format MM-DD-YY with:
  /// - Month: 01-12
  /// - Day: 01-31 (basic validation, not calendar-aware)
  /// - Year: 00-99 (two-digit year)
  /// </remarks>
  private static void ValidateDateField(string dateString, ValidationContext context, string location)
  {
    if (string.IsNullOrWhiteSpace(dateString))
    {
      context.AddWarning("Date field is empty or whitespace.", location);
      return;
    }

    // QWK date format is MM-DD-YY (8 characters including hyphens)
    if (dateString.Length < 8)
    {
      context.AddWarning($"Date field '{dateString}' is shorter than expected MM-DD-YY format.", location);
      return;
    }

    // Check for hyphens in positions 2 and 5
    if (dateString.Length >= 3 && dateString[2] != '-')
    {
      context.AddWarning($"Date field '{dateString}' missing hyphen at position 2.", location);
    }

    if (dateString.Length >= 6 && dateString[5] != '-')
    {
      context.AddWarning($"Date field '{dateString}' missing hyphen at position 5.", location);
    }

    // Parse month (positions 0-1)
    if (dateString.Length >= 2)
    {
      string monthString = dateString.Substring(0, 2);
      if (int.TryParse(monthString, out int month))
      {
        if (month < 1 || month > 12)
        {
          context.AddWarning($"Date field '{dateString}' has invalid month: {month} (must be 01-12).", location);
        }
      }
      else
      {
        context.AddWarning($"Date field '{dateString}' has non-numeric month: '{monthString}'.", location);
      }
    }

    // Parse day (positions 3-4)
    if (dateString.Length >= 5)
    {
      string dayString = dateString.Substring(3, 2);
      if (int.TryParse(dayString, out int day))
      {
        if (day < 1 || day > 31)
        {
          context.AddWarning($"Date field '{dateString}' has invalid day: {day} (must be 01-31).", location);
        }
      }
      else
      {
        context.AddWarning($"Date field '{dateString}' has non-numeric day: '{dayString}'.", location);
      }
    }

    // Parse year (positions 6-7)
    if (dateString.Length >= 8)
    {
      string yearString = dateString.Substring(6, 2);
      if (!int.TryParse(yearString, out int year))
      {
        context.AddWarning($"Date field '{dateString}' has non-numeric year: '{yearString}'.", location);
      }
      // Year is two-digit 00-99, all values are valid
    }
  }

  /// <summary>
  /// Validates time field format and plausibility per QWK specification.
  /// </summary>
  /// <param name="timeString">The time string to validate (QWK format: HH:MM).</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <param name="location">The location context for this field.</param>
  /// <remarks>
  /// QWK specification requires time format HH:MM with:
  /// - Hour: 00-23 (24-hour format)
  /// - Minute: 00-59
  /// </remarks>
  private static void ValidateTimeField(string timeString, ValidationContext context, string location)
  {
    if (string.IsNullOrWhiteSpace(timeString))
    {
      context.AddWarning("Time field is empty or whitespace.", location);
      return;
    }

    // QWK time format is HH:MM (5 characters including colon)
    if (timeString.Length < 5)
    {
      context.AddWarning($"Time field '{timeString}' is shorter than expected HH:MM format.", location);
      return;
    }

    // Check for colon at position 2
    if (timeString.Length >= 3 && timeString[2] != ':')
    {
      context.AddWarning($"Time field '{timeString}' missing colon at position 2.", location);
    }

    // Parse hour (positions 0-1)
    if (timeString.Length >= 2)
    {
      string hourString = timeString.Substring(0, 2);
      if (int.TryParse(hourString, out int hour))
      {
        if (hour < 0 || hour > 23)
        {
          context.AddWarning($"Time field '{timeString}' has invalid hour: {hour} (must be 00-23).", location);
        }
      }
      else
      {
        context.AddWarning($"Time field '{timeString}' has non-numeric hour: '{hourString}'.", location);
      }
    }

    // Parse minute (positions 3-4)
    if (timeString.Length >= 5)
    {
      string minuteString = timeString.Substring(3, 2);
      if (int.TryParse(minuteString, out int minute))
      {
        if (minute < 0 || minute > 59)
        {
          context.AddWarning($"Time field '{timeString}' has invalid minute: {minute} (must be 00-59).", location);
        }
      }
      else
      {
        context.AddWarning($"Time field '{timeString}' has non-numeric minute: '{minuteString}'.", location);
      }
    }
  }

  /// <summary>
  /// Validates index file consistency against MESSAGES.DAT.
  /// </summary>
  /// <param name="index">The index file to validate.</param>
  /// <param name="messagesDatSize">The size of MESSAGES.DAT in bytes.</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="index"/> or <paramref name="context"/> is null.</exception>
  public static void ValidateIndexConsistency(IndexFile index, long messagesDatSize, ValidationContext context)
  {
    if (index == null)
    {
      throw new ArgumentNullException(nameof(index));
    }
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    string location = $"Index (Conference {index.ConferenceNumber})";

    if (index.Count == 0)
    {
      context.AddInfo("Index file is empty (no messages for this conference).", location);
      return;
    }

    // Validate that index was validated against file size
    if (!index.ValidatedAgainstFileSize.HasValue)
    {
      context.AddWarning("Index was not validated against MESSAGES.DAT file size.", location);
    }

    // Check for duplicate message numbers
    HashSet<int> seenNumbers = new HashSet<int>();
    for (int i = 0; i < index.Count; i++)
    {
      IndexEntry entry = index[i];

      if (seenNumbers.Contains(entry.MessageNumber))
      {
        context.AddWarning($"Duplicate message number {entry.MessageNumber} found in index.", $"{location}:Entry[{i}]");
      }
      else
      {
        seenNumbers.Add(entry.MessageNumber);
      }

      // Validate record offset is within file bounds
      long byteOffset = entry.GetByteOffset();
      if (byteOffset < 0)
      {
        context.AddError($"Index entry {i} has negative record offset: {entry.RecordOffset}.", $"{location}:Entry[{i}]");
      }
      else if (byteOffset >= messagesDatSize)
      {
        context.AddError($"Index entry {i} points beyond end of MESSAGES.DAT: offset={byteOffset}, fileSize={messagesDatSize}.", $"{location}:Entry[{i}]");
      }
    }

    // Check for sequential message numbers
    bool isSequential = true;
    for (int i = 1; i < index.Count; i++)
    {
      if (index[i].MessageNumber != index[i - 1].MessageNumber + 1)
      {
        isSequential = false;
        break;
      }
    }

    if (!isSequential)
    {
      context.AddInfo("Index message numbers are not strictly sequential.", location);
    }
  }

  /// <summary>
  /// Validates that conference numbers in messages are within valid range.
  /// </summary>
  /// <param name="messages">The messages to validate.</param>
  /// <param name="conferences">The list of valid conferences from CONTROL.DAT.</param>
  /// <param name="context">The validation context for recording issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
  public static void ValidateConferenceNumbers(
    IEnumerable<Message> messages,
    IReadOnlyList<ConferenceInfo> conferences,
    ValidationContext context)
  {
    if (messages == null)
    {
      throw new ArgumentNullException(nameof(messages));
    }
    if (conferences == null)
    {
      throw new ArgumentNullException(nameof(conferences));
    }
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    // Build set of valid conference numbers
    HashSet<int> validConferences = new HashSet<int>();
    foreach (ConferenceInfo conf in conferences)
    {
      validConferences.Add(conf.Number);
    }

    // Conference 0 is always valid (main board)
    validConferences.Add(0);

    // Validate each message's conference number
    foreach (Message message in messages)
    {
      if (!validConferences.Contains(message.ConferenceNumber))
      {
        context.AddWarning(
          $"Message {message.MessageNumber} references undefined conference {message.ConferenceNumber}.",
          $"Message {message.MessageNumber}:Conference");
      }
    }
  }
}