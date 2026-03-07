using System;

namespace QwkNet.Models.Messages;

/// <summary>
/// Represents the status flags for a QWK message.
/// </summary>
/// <remarks>
/// Status flags indicate message visibility, read status, and protection level.
/// These correspond to the single-byte status field at offset 0 (0-indexed) in
/// the 128-byte QWK message header.
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
  /// Indicated by <c>*</c> (unread) or <c>+</c> (read) in QWK header byte 0.
  /// </remarks>
  Private = 1 << 0,

  /// <summary>
  /// Message has been read by the recipient.
  /// </summary>
  /// <remarks>
  /// Indicated by <c>-</c> (public, read) or <c>+</c> (private, read) in QWK header byte 0.
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
  /// Indicated by <c>~</c> (unread) or <c>`</c> (read) in QWK header byte 0.
  /// </remarks>
  CommentToSysop = 1 << 3,

  /// <summary>
  /// Message is password protected by sender.
  /// </summary>
  /// <remarks>
  /// Indicated by <c>%</c> (unread) or <c>^</c> (read) in QWK header byte 0.
  /// </remarks>
  SenderPasswordProtected = 1 << 4,

  /// <summary>
  /// Message is password protected by group password.
  /// </summary>
  /// <remarks>
  /// Indicated by <c>!</c> (unread) or <c>#</c> (read) in QWK header byte 0.
  /// </remarks>
  GroupPasswordProtected = 1 << 5,

  /// <summary>
  /// Message is addressed to ALL and protected by group password.
  /// </summary>
  /// <remarks>
  /// Indicated by <c>$</c> in QWK header byte 0.
  /// </remarks>
  GroupPasswordProtectedToAll = 1 << 6,

  /// <summary>
  /// Message has a network tag-line appended.
  /// </summary>
  /// <remarks>
  /// Indicated by bit 7 of the status byte, set by some legacy BBS software
  /// (e.g. PCBoard) as an exported or received overlay flag.
  /// </remarks>
  HasNetworkTagLine = 1 << 7
}