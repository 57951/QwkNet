using System;

namespace QwkNet.Models.Messages;

/// <summary>
/// Represents the status flags for a QWK message.
/// </summary>
/// <remarks>
/// Status flags indicate message visibility, read status, and protection level.
/// These correspond to the single-byte status field at offset 1 in the QWK header.
/// </remarks>
[Flags]
public enum MessageStatus
{
  /// <summary>
  /// No status flags set.
  /// </summary>
  None = 0,

  /// <summary>
  /// Message is private (visible only to sender and recipient).
  /// </summary>
  /// <remarks>
  /// Indicated by '*' (unread) or '+' (read) in QWK header byte 1.
  /// </remarks>
  Private = 1 << 0,

  /// <summary>
  /// Message has been read by the recipient.
  /// </summary>
  /// <remarks>
  /// Indicated by '-' (public, read) or '+' (private, read) in QWK header.
  /// </remarks>
  Read = 1 << 1,

  /// <summary>
  /// Message is marked for deletion.
  /// </summary>
  /// <remarks>
  /// Not commonly used in standard QWK packets but supported for completeness.
  /// </remarks>
  Deleted = 1 << 2,

  /// <summary>
  /// Message is a comment to the sysop.
  /// </summary>
  /// <remarks>
  /// Indicated by '~' (unread) or '`' (read) in QWK header byte 1.
  /// </remarks>
  CommentToSysop = 1 << 3,

  /// <summary>
  /// Message is password protected by sender.
  /// </summary>
  /// <remarks>
  /// Indicated by '%' (unread) or '^' (read) in QWK header byte 1.
  /// </remarks>
  SenderPasswordProtected = 1 << 4,

  /// <summary>
  /// Message is password protected by group password.
  /// </summary>
  /// <remarks>
  /// Indicated by '!' (unread) or '#' (read) in QWK header byte 1.
  /// </remarks>
  GroupPasswordProtected = 1 << 5,

  /// <summary>
  /// Message is addressed to ALL and protected by group password.
  /// </summary>
  /// <remarks>
  /// Indicated by '$' in QWK header byte 1.
  /// </remarks>
  GroupPasswordProtectedToAll = 1 << 6,

  /// <summary>
  /// Message has a network tag-line appended.
  /// </summary>
  /// <remarks>
  /// Indicated by '*' at offset 128 in QWK header (byte 128, relative offset 1).
  /// </remarks>
  HasNetworkTagLine = 1 << 7
}
