using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QwkNet.Models.Control;

namespace QwkNet;

/// <summary>
/// Represents a collection of conferences in a QWK packet.
/// </summary>
public sealed class ConferenceCollection : IReadOnlyList<ConferenceInfo>
{
  private readonly List<ConferenceInfo> _conferences;

  /// <summary>
  /// Gets the number of conferences.
  /// </summary>
  public int Count => _conferences.Count;

  /// <summary>
  /// Gets the conference at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index.</param>
  /// <returns>The conference at the specified index.</returns>
  public ConferenceInfo this[int index] => _conferences[index];

  /// <summary>
  /// Initialises a new instance of the <see cref="ConferenceCollection"/> class.
  /// </summary>
  /// <param name="conferences">The list of conferences.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="conferences"/> is null.</exception>
  public ConferenceCollection(IEnumerable<ConferenceInfo> conferences)
  {
    if (conferences == null)
    {
      throw new ArgumentNullException(nameof(conferences));
    }

    _conferences = conferences.ToList();
  }

  /// <summary>
  /// Finds a conference by number.
  /// </summary>
  /// <param name="conferenceNumber">The conference number.</param>
  /// <returns>The conference information, or null if not found.</returns>
  public ConferenceInfo? FindByNumber(ushort conferenceNumber)
  {
    return _conferences.FirstOrDefault(c => c.Number == conferenceNumber);
  }

  /// <summary>
  /// Gets whether a conference exists.
  /// </summary>
  /// <param name="conferenceNumber">The conference number.</param>
  /// <returns>True if the conference exists; otherwise, false.</returns>
  public bool Contains(ushort conferenceNumber)
  {
    return _conferences.Any(c => c.Number == conferenceNumber);
  }

  /// <inheritdoc/>
  public IEnumerator<ConferenceInfo> GetEnumerator() => _conferences.GetEnumerator();

  /// <inheritdoc/>
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}