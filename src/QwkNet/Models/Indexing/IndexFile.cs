using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QwkNet.Models.Indexing;

/// <summary>
/// Represents a complete QWK index (.NDX) file containing message-to-offset mappings.
/// </summary>
/// <remarks>
/// <para>
/// Index files are conference-specific and named numerically based on the conference
/// number (e.g., 0.NDX for conference 0, 123.NDX for conference 123). Each file
/// contains a series of 4-byte MSBIN floating-point values representing record
/// offsets into MESSAGES.DAT.
/// </para>
/// <para>
/// Index files enable random access to messages without sequential scanning.
/// If an index file is missing or invalid, the library falls back to sequential
/// message enumeration.
/// </para>
/// </remarks>
public sealed class IndexFile : IReadOnlyList<IndexEntry>
{
  private readonly List<IndexEntry> _entries;

  /// <summary>
  /// Gets the conference number that this index file represents.
  /// </summary>
  /// <value>The conference number (0-65535).</value>
  public int ConferenceNumber { get; }

  /// <summary>
  /// Gets the number of index entries in this file.
  /// </summary>
  /// <value>The count of entries (may be zero for empty conferences).</value>
  public int Count => _entries.Count;

  /// <summary>
  /// Gets a value indicating whether this index file is empty.
  /// </summary>
  /// <value>True if there are no entries; otherwise false.</value>
  public bool IsEmpty => _entries.Count == 0;

  /// <summary>
  /// Gets the validation status indicating whether this index passed integrity checks.
  /// </summary>
  /// <value>True if the index is structurally valid; false if validation failed.</value>
  /// <remarks>
  /// An invalid index may still be usable in salvage mode, but should not be
  /// trusted for production use without review.
  /// </remarks>
  public bool IsValid { get; }

  /// <summary>
  /// Gets the expected MESSAGES.DAT file size (in bytes) that this index was validated against.
  /// </summary>
  /// <value>The file size used for validation, or null if not validated.</value>
  /// <remarks>
  /// If this value is set, it indicates that record offsets were checked against
  /// the actual MESSAGES.DAT size to ensure they don't point beyond the file end.
  /// </remarks>
  public long? ValidatedAgainstFileSize { get; }

  /// <summary>
  /// Gets the index entry at the specified position.
  /// </summary>
  /// <param name="index">The zero-based index of the entry to retrieve.</param>
  /// <returns>The index entry at the specified position.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when index is negative or greater than or equal to Count.
  /// </exception>
  public IndexEntry this[int index] => _entries[index];

  /// <summary>
  /// Initialises a new instance of the <see cref="IndexFile"/> class.
  /// </summary>
  /// <param name="conferenceNumber">The conference number (0-65535).</param>
  /// <param name="entries">The collection of index entries.</param>
  /// <param name="isValid">Whether this index passed validation checks.</param>
  /// <param name="validatedAgainstFileSize">The MESSAGES.DAT file size used for validation, or null.</param>
  /// <exception cref="ArgumentNullException">Thrown when entries is null.</exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when conferenceNumber is negative or greater than 65535.
  /// </exception>
  public IndexFile(
    int conferenceNumber,
    IEnumerable<IndexEntry> entries,
    bool isValid = true,
    long? validatedAgainstFileSize = null)
  {
    if (conferenceNumber < 0 || conferenceNumber > 65535)
    {
      throw new ArgumentOutOfRangeException(
        nameof(conferenceNumber),
        conferenceNumber,
        "Conference number must be between 0 and 65535.");
    }

    if (entries == null)
    {
      throw new ArgumentNullException(nameof(entries));
    }

    ConferenceNumber = conferenceNumber;
    _entries = entries.ToList();
    IsValid = isValid;
    ValidatedAgainstFileSize = validatedAgainstFileSize;
  }

  /// <summary>
  /// Attempts to find an index entry for the specified message number.
  /// </summary>
  /// <param name="messageNumber">The message number to search for (1-based).</param>
  /// <returns>The index entry if found; otherwise null.</returns>
  /// <remarks>
  /// This method performs a linear search. For repeated lookups, consider
  /// building a dictionary if performance is critical.
  /// </remarks>
  public IndexEntry? FindByMessageNumber(int messageNumber)
  {
    foreach (IndexEntry entry in _entries)
    {
      if (entry.MessageNumber == messageNumber)
      {
        return entry;
      }
    }

    return null;
  }

  /// <summary>
  /// Gets all index entries as a read-only list.
  /// </summary>
  /// <returns>A read-only view of the entries collection.</returns>
  public IReadOnlyList<IndexEntry> GetEntries()
  {
    return _entries.AsReadOnly();
  }

  /// <summary>
  /// Returns an enumerator that iterates through the index entries.
  /// </summary>
  /// <returns>An enumerator for the entries collection.</returns>
  public IEnumerator<IndexEntry> GetEnumerator()
  {
    return _entries.GetEnumerator();
  }

  /// <summary>
  /// Returns an enumerator that iterates through the index entries.
  /// </summary>
  /// <returns>An enumerator for the entries collection.</returns>
  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  /// <summary>
  /// Returns a string representation of this index file.
  /// </summary>
  /// <returns>A string indicating conference number, entry count, and validation status.</returns>
  public override string ToString()
  {
    string validStatus = IsValid ? "valid" : "invalid";
    string sizeInfo = ValidatedAgainstFileSize.HasValue
      ? $", validated against {ValidatedAgainstFileSize.Value} bytes"
      : "";

    return $"IndexFile for conference {ConferenceNumber}: {Count} entries ({validStatus}{sizeInfo})";
  }
}