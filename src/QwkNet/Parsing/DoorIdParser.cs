using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QwkNet.Encoding;
using QwkNet.Models.Control;
using QwkNet.Validation;

namespace QwkNet.Parsing;

/// <summary>
/// Parses DOOR.ID files from QWK packets.
/// </summary>
/// <remarks>
/// <para>
/// DOOR.ID was introduced by Greg Hewgill to provide offline mail readers with
/// information about mail door capabilities and control message formatting.
/// </para>
/// <para>
/// Per the QWK specification (section 4.3.1), DOOR.ID contains key-value pairs
/// separated by " = ". Lines may appear in any order and not all fields are required.
/// </para>
/// </remarks>
public sealed class DoorIdParser
{
  /// <summary>
  /// Parses a DOOR.ID file from raw bytes.
  /// </summary>
  /// <param name="data">The raw DOOR.ID file contents.</param>
  /// <param name="mode">The validation mode to use.</param>
  /// <param name="context">Optional validation context to receive issues.</param>
  /// <returns>A parsed <see cref="DoorId"/> instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
  /// <exception cref="QwkFormatException">Thrown in strict mode when required fields are missing.</exception>
  public static DoorId Parse(byte[] data, ValidationMode mode = ValidationMode.Lenient, ValidationContext? context = null)
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    ValidationContext ctx = context ?? new ValidationContext(mode);

    // Use CP437 encoding for proper DOS/BBS character support
    string content = Cp437Encoding.Decode(data);
    string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    // Parse key-value pairs (case-insensitive keys)
    Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    HashSet<DoorCapability> capabilities = new HashSet<DoorCapability>();

    foreach (string line in lines)
    {
      string trimmedLine = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmedLine))
      {
        continue;
      }

      // Check for standalone capability keywords (no "=" sign)
      if (trimmedLine.Equals("RECEIPT", StringComparison.OrdinalIgnoreCase))
      {
        capabilities.Add(DoorCapability.Receipt);
        entries["RECEIPT"] = "";
        continue;
      }

      // Split on " = " (with spaces, per specification)
      int separatorIndex = trimmedLine.IndexOf(" = ", StringComparison.Ordinal);
      if (separatorIndex < 0)
      {
        // Try without spaces for lenient parsing
        separatorIndex = trimmedLine.IndexOf("=", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
          ctx.AddWarning($"Invalid DOOR.ID line format (no '=' found): '{trimmedLine}'", "DOOR.ID");
          continue;
        }
      }

      string key = trimmedLine.Substring(0, separatorIndex).Trim();
      string value = trimmedLine.Substring(separatorIndex + 1).TrimStart('=').Trim();

      // Handle multiple entries with same key (e.g., multiple CONTROLTYPE lines)
      if (entries.ContainsKey(key))
      {
        // For CONTROLTYPE, accumulate capabilities
        if (key.Equals("CONTROLTYPE", StringComparison.OrdinalIgnoreCase))
        {
          capabilities.Add(ParseControlType(value));
        }
        // For other keys, keep first value but warn
        else
        {
          ctx.AddWarning($"Duplicate DOOR.ID key '{key}', keeping first value", "DOOR.ID");
        }
      }
      else
      {
        entries[key] = value;

        // Parse known capabilities
        if (key.Equals("CONTROLTYPE", StringComparison.OrdinalIgnoreCase))
        {
          capabilities.Add(ParseControlType(value));
        }
        else if (key.Equals("MIXEDCASE", StringComparison.OrdinalIgnoreCase) &&
                 value.Equals("YES", StringComparison.OrdinalIgnoreCase))
        {
          capabilities.Add(DoorCapability.MixedCase);
        }
        else if (key.Equals("FIDOTAG", StringComparison.OrdinalIgnoreCase) &&
                 value.Equals("YES", StringComparison.OrdinalIgnoreCase))
        {
          capabilities.Add(DoorCapability.FidoTag);
        }
      }
    }

    // Extract required fields
    if (!entries.TryGetValue("DOOR", out string? doorName))
    {
      ctx.AddError("Missing required DOOR field", "DOOR.ID");
      doorName = "Unknown";
    }

    if (!entries.TryGetValue("VERSION", out string? version))
    {
      ctx.AddError("Missing required VERSION field", "DOOR.ID");
      version = "0.0";
    }

    entries.TryGetValue("SYSTEM", out string? systemType);
    entries.TryGetValue("CONTROLNAME", out string? controlName);

    return new DoorId(
      doorName,
      version,
      systemType,
      controlName,
      capabilities,
      entries
    );
  }

  /// <summary>
  /// Parses a DOOR.ID file from a stream.
  /// </summary>
  /// <param name="stream">The stream containing DOOR.ID data.</param>
  /// <param name="mode">The validation mode to use.</param>
  /// <param name="context">Optional validation context to receive issues.</param>
  /// <returns>A parsed <see cref="DoorId"/> instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
  public static DoorId Parse(Stream stream, ValidationMode mode = ValidationMode.Lenient, ValidationContext? context = null)
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

  private static DoorCapability ParseControlType(string value)
  {
    string upperValue = value.ToUpperInvariant();
    return upperValue switch
    {
      "ADD" => DoorCapability.Add,
      "DROP" => DoorCapability.Drop,
      "REQUEST" => DoorCapability.Request,
      "RESET" => DoorCapability.Reset,
      _ => DoorCapability.Unknown
    };
  }
}