using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QwkNet.Models.Messages;

namespace QwkNet;

/// <summary>
/// Represents a collection of messages in a QWK packet.
/// </summary>
/// <remarks>
/// Messages are eagerly loaded during packet opening for simplicity and performance.
/// Typical packets contain 100-1000 messages (~1-10 MB memory usage).
/// </remarks>
public sealed class MessageCollection : IReadOnlyList<Message>
{
  private readonly List<Message> _messages;

  /// <summary>
  /// Gets the number of messages in the collection.
  /// </summary>
  public int Count => _messages.Count;

  /// <summary>
  /// Gets the message at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index.</param>
  /// <returns>The message at the specified index.</returns>
  public Message this[int index] => _messages[index];

  /// <summary>
  /// Initialises a new instance of the <see cref="MessageCollection"/> class.
  /// </summary>
  /// <param name="messages">The list of messages.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="messages"/> is null.</exception>
  public MessageCollection(IEnumerable<Message> messages)
  {
    if (messages == null)
    {
      throw new ArgumentNullException(nameof(messages));
    }

    _messages = messages.ToList();
  }

  /// <summary>
  /// Gets messages for a specific conference.
  /// </summary>
  /// <param name="conferenceNumber">The conference number.</param>
  /// <returns>A list of messages in the specified conference.</returns>
  public IReadOnlyList<Message> GetByConference(ushort conferenceNumber)
  {
    return _messages.Where(m => m.ConferenceNumber == conferenceNumber).ToList();
  }

  /// <summary>
  /// Gets private messages only.
  /// </summary>
  /// <returns>A list of private messages.</returns>
  public IReadOnlyList<Message> GetPrivateMessages()
  {
    return _messages.Where(m => m.IsPrivate).ToList();
  }

  /// <summary>
  /// Gets unread messages only.
  /// </summary>
  /// <returns>A list of unread messages.</returns>
  public IReadOnlyList<Message> GetUnreadMessages()
  {
    return _messages.Where(m => !m.IsRead).ToList();
  }

  /// <inheritdoc/>
  public IEnumerator<Message> GetEnumerator() => _messages.GetEnumerator();

  /// <inheritdoc/>
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}