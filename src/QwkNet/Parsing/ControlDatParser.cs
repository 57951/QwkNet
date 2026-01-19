using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using QwkNet.Encoding;
using QwkNet.Models.Control;
using QwkNet.Validation;

namespace QwkNet.Parsing;

/// <summary>
/// Parses CONTROL.DAT files from QWK packets.
/// </summary>
/// <remarks>
/// <para>
/// CONTROL.DAT follows a strict line-by-line format as defined in the QWK specification
/// by Patrick Y. Lee and Jeffery Foy. This parser handles all standard fields and preserves
/// unknown or extended fields for round-trip fidelity.
/// </para>
/// <para>
/// The parser supports three validation modes (strict, lenient, salvage) to handle
/// varying levels of real-world packet malformation.
/// </para>
/// <para>
/// <b>Date Format Support:</b>
/// While the QWK specification defines date format as MM-DD-YY,HH:MM:SS, real-world BBS
/// software implementations varied. This parser accepts all observed formats:
/// hyphen or slash delimiters, 2-digit or 4-digit years. See <see cref="ParseDateTime"/>
/// for detailed format documentation.
/// </para>
/// </remarks>
public sealed class ControlDatParser
{
  /// <summary>
  /// Parses a CONTROL.DAT file from raw bytes.
  /// </summary>
  /// <param name="data">The raw CONTROL.DAT file contents.</param>
  /// <param name="mode">The validation mode to use.</param>
  /// <param name="context">Optional validation context to receive issues. If not provided, a new context is created.</param>
  /// <returns>A parsed <see cref="ControlDat"/> instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
  /// <exception cref="QwkFormatException">Thrown in strict mode when format violations occur.</exception>
  public static ControlDat Parse(byte[] data, ValidationMode mode = ValidationMode.Lenient, ValidationContext? context = null)
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    ValidationContext ctx = context ?? new ValidationContext(mode);

    // Use CP437 encoding for proper DOS/BBS character support
    string content = Cp437Encoding.Decode(data);
    string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    // Store all raw lines for round-trip fidelity
    List<string> rawLines = new List<string>(lines);

    // Parse required fields with fallbacks
    string bbsName = GetLine(lines, 0, "BBS Name", ctx) ?? "";
    string bbsCity = GetLine(lines, 1, "BBS City", ctx) ?? "";
    string bbsPhone = GetLine(lines, 2, "BBS Phone", ctx) ?? "";
    string sysop = GetLine(lines, 3, "Sysop", ctx) ?? "";

    // Parse line 5: registration number, BBS ID
    string line5 = GetLine(lines, 4, "Registration/BBS ID", ctx) ?? ",";
    string[] line5Parts = line5.Split(',');
    string registrationNumber = line5Parts.Length > 0 ? line5Parts[0].Trim() : "00000";
    string bbsId = line5Parts.Length > 1 ? line5Parts[1].Trim() : "UNKNOWN";

    // Parse line 6: date/time in MM-DD-YY,HH:MM:SS format
    string line6 = GetLine(lines, 5, "Creation Date/Time", ctx) ?? "";
    DateTimeOffset createdAt = ParseDateTime(line6, ctx);

    string userName = GetLine(lines, 6, "User Name", ctx) ?? "";
    string qmailMenuFile = GetLine(lines, 7, "Qmail Menu File", ctx) ?? "";

    // Parse line 9: NetMail conference (typically 0)
    string line9 = GetLine(lines, 8, "NetMail Conference", ctx) ?? "0";
    ushort netMailConference = ParseUInt16(line9, "NetMail Conference", 0, ctx);

    // Parse line 10: total messages
    string line10 = GetLine(lines, 9, "Total Messages", ctx) ?? "0";
    int totalMessages = ParseInt32(line10, "Total Messages", 0, ctx);

    // Parse line 11: conference count minus one
    string line11 = GetLine(lines, 10, "Conference Count Minus One", ctx) ?? "0";
    int conferenceCountMinusOne = ParseInt32(line11, "Conference Count Minus One", 0, ctx);

    // Parse conferences (pairs of lines: number, name)
    IReadOnlyList<ConferenceInfo> conferences = ParseConferences(lines, 11, conferenceCountMinusOne, ctx);

    // Parse optional files (after conferences)
    int optionalFilesStart = 11 + ((conferenceCountMinusOne + 1) * 2);
    string? welcomeFile = GetOptionalLine(lines, optionalFilesStart);
    string? newsFile = GetOptionalLine(lines, optionalFilesStart + 1);
    string? goodbyeFile = GetOptionalLine(lines, optionalFilesStart + 2);

    return new ControlDat(
      bbsName,
      bbsCity,
      bbsPhone,
      sysop,
      registrationNumber,
      bbsId,
      createdAt,
      userName,
      qmailMenuFile,
      netMailConference,
      totalMessages,
      conferenceCountMinusOne,
      conferences,
      welcomeFile,
      newsFile,
      goodbyeFile,
      rawLines
    );
  }

  /// <summary>
  /// Parses a CONTROL.DAT file from a stream.
  /// </summary>
  /// <param name="stream">The stream containing CONTROL.DAT data.</param>
  /// <param name="mode">The validation mode to use.</param>
  /// <param name="context">Optional validation context to receive issues.</param>
  /// <returns>A parsed <see cref="ControlDat"/> instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
  public static ControlDat Parse(Stream stream, ValidationMode mode = ValidationMode.Lenient, ValidationContext? context = null)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    using (MemoryStream ms = new MemoryStream())
    {
      stream.CopyTo(ms);
      return Parse(ms.ToArray(), mode, context);
    }
  }

  private static string? GetLine(string[] lines, int index, string fieldName, ValidationContext context)
  {
    if (index >= lines.Length)
    {
      context.AddError($"Missing required field: {fieldName}", $"CONTROL.DAT line {index + 1}");
      return null;
    }

    string line = lines[index];
    if (string.IsNullOrWhiteSpace(line))
    {
      context.AddWarning($"Empty or whitespace-only field: {fieldName}", $"CONTROL.DAT line {index + 1}");
    }

    return line;
  }

  private static string? GetOptionalLine(string[] lines, int index)
  {
    if (index >= lines.Length)
    {
      return null;
    }

    string line = lines[index].Trim();
    return string.IsNullOrEmpty(line) ? null : line;
  }

  /// <summary>
  /// Parses a date/time string from CONTROL.DAT line 6.
  /// </summary>
  /// <param name="value">The date/time string to parse.</param>
  /// <param name="context">The validation context for recording errors.</param>
  /// <returns>The parsed date/time, or <see cref="DateTimeOffset.MinValue"/> on failure.</returns>
  /// <remarks>
  /// <para>
  /// The QWK specification defines the format as MM-DD-YY,HH:MM:SS (e.g., "01-01-91,23:59:59").
  /// However, real-world BBS software implementations varied, producing several format variations:
  /// </para>
  /// <list type="bullet">
  /// <item><description>Hyphen-delimited, 2-digit year: MM-DD-YY (spec-compliant)</description></item>
  /// <item><description>Slash-delimited, 2-digit year: MM/DD/YY (common variant)</description></item>
  /// <item><description>Hyphen-delimited, 4-digit year: MM-DD-YYYY (rare but observed)</description></item>
  /// <item><description>Slash-delimited, 4-digit year: MM/DD/YYYY (e.g., mvt2.qwk packet)</description></item>
  /// </list>
  /// <para>
  /// This parser accepts all four observed formats to ensure compatibility with historical QWK packets
  /// created by diverse BBS software implementations during the 1980s-1990s BBS era.
  /// </para>
  /// <para>
  /// <b>Two-digit year handling (Y2K heuristic):</b>
  /// Values 00-49 map to 2000-2049, values 50-99 map to 1950-1999. This matches the original
  /// QWK specification behaviour and ensures correct interpretation of historical packets.
  /// </para>
  /// <para>
  /// <b>Four-digit year validation:</b>
  /// When a 4-digit year is provided, it must fall within the range 1980-2099. Years before 1980
  /// predate the BBS era and are rejected as invalid. Years after 2099 are considered unrealistic
  /// for QWK packet creation and are also rejected.
  /// </para>
  /// <para>
  /// <b>Delimiter detection:</b>
  /// The parser automatically detects whether hyphens or slashes are used as date separators.
  /// Mixed delimiters (e.g., "12-31/1993") are rejected as malformed. Other delimiters (dots, spaces)
  /// are not supported and will result in parsing errors.
  /// </para>
  /// </remarks>
  private static DateTimeOffset ParseDateTime(string value, ValidationContext context)
  {
    // Step 1: Split on comma (date/time separator)
    string[] parts = value.Split(',');
    if (parts.Length != 2)
    {
      context.AddError($"Invalid date/time format: '{value}' (expected MM-DD-YY,HH:MM:SS or MM/DD/YYYY,HH:MM:SS)", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    string datePart = parts[0].Trim();
    string timePart = parts[1].Trim();

    // Step 2: Detect delimiter (hyphen or slash)
    // We check for the presence of each delimiter type in the date string.
    // Real-world QWK packets use either hyphens (per spec) or slashes (common variant).
    char delimiter;
    if (datePart.Contains('-'))
    {
      delimiter = '-';
    }
    else if (datePart.Contains('/'))
    {
      delimiter = '/';
    }
    else
    {
      // No recognised delimiter found - this is a malformed date
      context.AddError($"Invalid date format: '{datePart}' (expected MM-DD-YY or MM/DD/YYYY)", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    // Step 3: Split date components using the detected delimiter
    string[] dateComponents = datePart.Split(delimiter);
    if (dateComponents.Length != 3)
    {
      context.AddError($"Invalid date format: '{datePart}' (expected 3 components: MM{delimiter}DD{delimiter}YY)", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    // Step 4: Parse and validate date components
    if (!int.TryParse(dateComponents[0], out int month) ||
        !int.TryParse(dateComponents[1], out int day) ||
        !int.TryParse(dateComponents[2], out int year))
    {
      context.AddError($"Invalid date components in: '{datePart}'", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    // Step 5: Year normalisation
    // Handle both 2-digit years (per QWK spec) and 4-digit years (real-world variant)
    if (year < 100)
    {
      // Y2K handling for two-digit years (per QWK specification)
      // Heuristic: 00-49 = 2000-2049, 50-99 = 1950-1999
      // This matches the original specification behaviour and ensures correct
      // interpretation of historical packets from the 1990s-2000s transition period.
      year += (year < 50) ? 2000 : 1900;
    }

    // Validate 4-digit year range (historical BBS era: 1980-2099)
    // The BBS era began around 1980, so earlier years are anachronistic.
    // Years beyond 2099 are unrealistic for QWK packet creation and likely indicate
    // malformed data or incorrect parsing.
    if (year < 1980 || year > 2099)
    {
      context.AddError($"Year out of valid range: {year} (must be 1980-2099)", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    // Step 6: Parse time components (unchanged from original implementation)
    // Parse time: HH:MM:SS (24-hour format)
    string[] timeComponents = timePart.Split(':');
    if (timeComponents.Length < 2 || timeComponents.Length > 3)
    {
      context.AddError($"Invalid time format: '{timePart}' (expected HH:MM or HH:MM:SS)", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    if (!int.TryParse(timeComponents[0], out int hour) ||
        !int.TryParse(timeComponents[1], out int minute))
    {
      context.AddError($"Invalid time components in: '{timePart}'", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    int second = 0;
    if (timeComponents.Length == 3)
    {
      if (!int.TryParse(timeComponents[2], out second))
      {
        context.AddWarning($"Invalid seconds component: '{timeComponents[2]}', assuming 0", "CONTROL.DAT line 6");
        second = 0;
      }
    }

    // Validate ranges
    if (month < 1 || month > 12)
    {
      context.AddError($"Month out of range: {month}", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    if (day < 1 || day > DateTime.DaysInMonth(year, month))
    {
      context.AddError($"Day out of range: {day} for month {month}/{year}", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    if (hour < 0 || hour > 23)
    {
      context.AddError($"Hour out of range: {hour}", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    if (minute < 0 || minute > 59)
    {
      context.AddError($"Minute out of range: {minute}", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }

    if (second < 0 || second > 59)
    {
      context.AddWarning($"Second out of range: {second}, clamping to 59", "CONTROL.DAT line 6");
      second = 59;
    }

    try
    {
      DateTime dt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
      return new DateTimeOffset(dt);
    }
    catch (ArgumentOutOfRangeException ex)
    {
      context.AddError($"Invalid date/time: {ex.Message}", "CONTROL.DAT line 6");
      return DateTimeOffset.MinValue;
    }
  }

  private static ushort ParseUInt16(string value, string fieldName, ushort defaultValue, ValidationContext context)
  {
    if (ushort.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort result))
    {
      return result;
    }

    context.AddWarning($"Invalid {fieldName} value: '{value}', using default {defaultValue}", $"{fieldName} field");
    return defaultValue;
  }

  private static int ParseInt32(string value, string fieldName, int defaultValue, ValidationContext context)
  {
    if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
    {
      return result;
    }

    context.AddWarning($"Invalid {fieldName} value: '{value}', using default {defaultValue}", $"{fieldName} field");
    return defaultValue;
  }

  private static IReadOnlyList<ConferenceInfo> ParseConferences(string[] lines, int startIndex, int count, ValidationContext context)
  {
    List<ConferenceInfo> conferences = new List<ConferenceInfo>();
    int conferenceCount = count + 1; // spec says "minus one"

    for (int i = 0; i < conferenceCount; i++)
    {
      int numberIndex = startIndex + (i * 2);
      int nameIndex = numberIndex + 1;

      if (numberIndex >= lines.Length)
      {
        context.AddError($"Missing conference number at expected index {numberIndex}", $"CONTROL.DAT line {numberIndex + 1}");
        break;
      }

      if (nameIndex >= lines.Length)
      {
        context.AddError($"Missing conference name at expected index {nameIndex}", $"CONTROL.DAT line {nameIndex + 1}");
        break;
      }

      string numberLine = lines[numberIndex].Trim();
      string nameLine = lines[nameIndex];

      ushort conferenceNumber = ParseUInt16(numberLine, $"Conference {i} number", 0, context);
      string conferenceName = nameLine; // Preserve exact name including any trailing whitespace

      conferences.Add(new ConferenceInfo(conferenceNumber, conferenceName));
    }

    return conferences;
  }
}