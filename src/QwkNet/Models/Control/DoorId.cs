using System.Collections.Generic;

namespace QwkNet.Models.Control;

/// <summary>
/// Represents a DOOR.ID file from a QWK packet.
/// </summary>
/// <remarks>
/// <para>
/// DOOR.ID was introduced by Greg Hewgill (Tomcat!/SLMR) to provide offline mail
/// readers with information about how to format control messages for a specific
/// mail door. It identifies the door software, version, BBS type, and supported
/// capabilities.
/// </para>
/// <para>
/// According to the QWK specification (section 4.3.1), DOOR.ID contains key-value
/// pairs that define control message addressing and supported features. Lines may
/// appear in any order and not all fields are required.
/// </para>
/// <para>
/// This model preserves all raw entries to support door-specific extensions beyond
/// the standard specification. The <see cref="ControlTypes"/> list retains the raw
/// value of every CONTROLTYPE line in document order, including any non-standard
/// values that map to <see cref="DoorCapability.Unknown"/>.
/// </para>
/// </remarks>
/// <param name="DoorName">The name of the mail door software.</param>
/// <param name="Version">The version string of the mail door software.</param>
/// <param name="SystemType">The BBS system type, or <see langword="null"/> if not specified.</param>
/// <param name="ControlName">The control name used for addressing control messages, or <see langword="null"/> if not specified.</param>
/// <param name="Capabilities">The set of capabilities advertised by the door.</param>
/// <param name="RawEntries">
/// All raw key-value entries from the DOOR.ID file. For keys that appear more than once
/// (such as CONTROLTYPE), only the first occurrence is stored here.
/// </param>
/// <param name="ControlTypes">
/// The raw string value of every CONTROLTYPE line, in order of appearance.
/// Preserves original casing and includes any non-standard values not covered by
/// <see cref="DoorCapability"/>.
/// </param>
public sealed record DoorId(
  string DoorName,
  string Version,
  string? SystemType,
  string? ControlName,
  IReadOnlySet<DoorCapability> Capabilities,
  IReadOnlyDictionary<string, string> RawEntries,
  IReadOnlyList<string> ControlTypes)
{
  /// <summary>
  /// Returns a string representation of this DOOR.ID file.
  /// </summary>
  /// <returns>
  /// A string in the format "DoorName Version (SystemType)".
  /// </returns>
  public override string ToString()
  {
    if (SystemType != null)
    {
      return $"{DoorName} {Version} ({SystemType})";
    }
    return $"{DoorName} {Version}";
  }
}