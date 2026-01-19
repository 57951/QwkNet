using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QwkNet.Models.Messages;

/// <summary>
/// Represents a collection of kludge lines in a message.
/// </summary>
/// <remarks>
/// This collection provides indexed access to kludges by key whilst preserving
/// insertion order and allowing duplicate keys (as some kludges may appear multiple times).
/// </remarks>
public sealed class MessageKludgeCollection : IReadOnlyCollection<MessageKludge>
{
  private readonly List<MessageKludge> _kludges;

  /// <summary>
  /// Gets the number of kludges in the collection.
  /// </summary>
  public int Count => _kludges.Count;

  /// <summary>
  /// Initialises a new instance of the <see cref="MessageKludgeCollection"/> class.
  /// </summary>
  /// <param name="kludges">The kludges to include in the collection.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="kludges"/> is <c>null</c>.
  /// </exception>
  public MessageKludgeCollection(IEnumerable<MessageKludge> kludges)
  {
    if (kludges == null)
    {
      throw new ArgumentNullException(nameof(kludges));
    }

    _kludges = new List<MessageKludge>(kludges);
  }

  /// <summary>
  /// Initialises a new empty instance of the <see cref="MessageKludgeCollection"/> class.
  /// </summary>
  public MessageKludgeCollection()
    : this(Enumerable.Empty<MessageKludge>())
  {
  }

  /// <summary>
  /// Gets all kludges with the specified key.
  /// </summary>
  /// <param name="key">The kludge key to search for (case-insensitive).</param>
  /// <returns>
  /// A collection of all kludges matching the specified key, or an empty collection if none found.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="key"/> is <c>null</c>.
  /// </exception>
  public IReadOnlyList<MessageKludge> GetByKey(string key)
  {
    if (key == null)
    {
      throw new ArgumentNullException(nameof(key));
    }

    return _kludges
      .Where(k => string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase))
      .ToList();
  }

  /// <summary>
  /// Gets the first kludge with the specified key, or <c>null</c> if not found.
  /// </summary>
  /// <param name="key">The kludge key to search for (case-insensitive).</param>
  /// <returns>
  /// The first matching kludge, or <c>null</c> if no match is found.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="key"/> is <c>null</c>.
  /// </exception>
  public MessageKludge? GetFirstByKey(string key)
  {
    if (key == null)
    {
      throw new ArgumentNullException(nameof(key));
    }

    return _kludges
      .FirstOrDefault(k => string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Determines whether the collection contains a kludge with the specified key.
  /// </summary>
  /// <param name="key">The kludge key to search for (case-insensitive).</param>
  /// <returns>
  /// <c>true</c> if a kludge with the specified key exists; otherwise, <c>false</c>.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="key"/> is <c>null</c>.
  /// </exception>
  public bool ContainsKey(string key)
  {
    if (key == null)
    {
      throw new ArgumentNullException(nameof(key));
    }

    return _kludges.Any(k => string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Returns an enumerator that iterates through the kludge collection.
  /// </summary>
  /// <returns>
  /// An enumerator for the collection.
  /// </returns>
  public IEnumerator<MessageKludge> GetEnumerator()
  {
    return _kludges.GetEnumerator();
  }

  /// <summary>
  /// Returns an enumerator that iterates through the kludge collection.
  /// </summary>
  /// <returns>
  /// An enumerator for the collection.
  /// </returns>
  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
