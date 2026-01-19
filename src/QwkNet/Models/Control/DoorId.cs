using System;
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
/// the standard specification.
/// </para>
/// </remarks>
public sealed record DoorId(
  string DoorName,
  string Version,
  string? SystemType,
  string? ControlName,
  IReadOnlySet<DoorCapability> Capabilities,
  IReadOnlyDictionary<string, string> RawEntries)
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