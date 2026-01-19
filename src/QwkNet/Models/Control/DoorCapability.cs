namespace QwkNet.Models.Control;

/// <summary>
/// Represents capabilities advertised by a QWK mail door via DOOR.ID.
/// </summary>
/// <remarks>
/// These capabilities inform offline mail readers how to format control messages
/// and what features are supported by the mail door.
/// </remarks>
public enum DoorCapability
{
  /// <summary>
  /// Door supports adding conferences to the user's scan list.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = ADD" in DOOR.ID.
  /// </remarks>
  Add,

  /// <summary>
  /// Door supports dropping conferences from the user's scan list.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = DROP" in DOOR.ID.
  /// </remarks>
  Drop,

  /// <summary>
  /// Door supports file requests.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = REQUEST" in DOOR.ID.
  /// </remarks>
  Request,

  /// <summary>
  /// Door supports return receipt requests.
  /// </summary>
  /// <remarks>
  /// Indicated by "RECEIPT" in DOOR.ID. When enabled, messages with "RRR"
  /// prefix in the subject should generate a return receipt.
  /// </remarks>
  Receipt,

  /// <summary>
  /// Door supports mixed-case names and subjects.
  /// </summary>
  /// <remarks>
  /// Indicated by "MIXEDCASE = YES" in DOOR.ID. Most modern doors support this.
  /// </remarks>
  MixedCase,

  /// <summary>
  /// Door uses FidoNet-compliant tag-lines.
  /// </summary>
  /// <remarks>
  /// Indicated by "FIDOTAG = YES" in DOOR.ID. When enabled, tear-lines and
  /// high-ASCII characters should be avoided.
  /// </remarks>
  FidoTag,

  /// <summary>
  /// Door supports resetting last-read pointers.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = RESET" in DOOR.ID.
  /// </remarks>
  Reset,

  /// <summary>
  /// Unknown or custom capability.
  /// </summary>
  /// <remarks>
  /// Used for door-specific capabilities not covered by the standard set.
  /// The raw entry is preserved in <see cref="DoorId.RawEntries"/>.
  /// </remarks>
  Unknown
}