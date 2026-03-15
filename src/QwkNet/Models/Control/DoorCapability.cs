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
  /// Door supports resetting all last-read pointers across all conferences.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = RESETALL" in DOOR.ID.
  /// </remarks>
  ResetAll,

  /// <summary>
  /// Door supports retrieving messages addressed to the current user only.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = YOURS" in DOOR.ID.
  /// </remarks>
  Yours,

  /// <summary>
  /// Door supports retrieving personal mail for the current user.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = MAIL" in DOOR.ID.
  /// </remarks>
  Mail,

  /// <summary>
  /// Door supports deleting personal mail for the current user.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = DELMAIL" in DOOR.ID.
  /// </remarks>
  DeleteMail,

  /// <summary>
  /// Door supports file attachments.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = ATTACH" in DOOR.ID.
  /// </remarks>
  Attach,

  /// <summary>
  /// Door supports marking messages as owned by the current user.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = OWN" in DOOR.ID.
  /// </remarks>
  Own,

  /// <summary>
  /// Door supports FidoNet-style file requests.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = FREQ" in DOOR.ID.
  /// </remarks>
  FileRequest,

  /// <summary>
  /// Door produces NDX (index) files alongside message packets.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = NDX" in DOOR.ID.
  /// </remarks>
  Index,

  /// <summary>
  /// Door supports time-zone information in message headers.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = TZ" in DOOR.ID.
  /// </remarks>
  TimeZone,

  /// <summary>
  /// Door supports VIA (routing path) information in message headers.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = VIA" in DOOR.ID.
  /// </remarks>
  Via,

  /// <summary>
  /// Door supports MSGID (unique message identifier) kludge lines.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = MSGID" in DOOR.ID.
  /// </remarks>
  MessageId,

  /// <summary>
  /// Door supports CONTROL kludge handling for extended message control lines.
  /// </summary>
  /// <remarks>
  /// Typically indicated by "CONTROLTYPE = CONTROL" in DOOR.ID.
  /// </remarks>
  Control,

  /// <summary>
  /// Unknown or custom capability.
  /// </summary>
  /// <remarks>
  /// Used for door-specific capabilities not covered by the standard set.
  /// The raw entry is preserved in <see cref="DoorId.RawEntries"/> and
  /// <see cref="DoorId.ControlTypes"/>.
  /// </remarks>
  Unknown
}