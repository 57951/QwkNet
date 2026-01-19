using System;
using System.Collections.Generic;
using System.Linq;
using QwkNet.Models.Indexing;
using Xunit;

namespace QwkNet.Tests.Models.Indexing;

public sealed class IndexFileTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesInstance()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(5);

    // Act
    IndexFile indexFile = new IndexFile(123, entries);

    // Assert
    Assert.Equal(123, indexFile.ConferenceNumber);
    Assert.Equal(5, indexFile.Count);
    Assert.True(indexFile.IsValid);
    Assert.Null(indexFile.ValidatedAgainstFileSize);
    Assert.False(indexFile.IsEmpty);
  }

  [Fact]
  public void Constructor_WithEmptyEntries_CreatesEmptyIndex()
  {
    // Arrange
    List<IndexEntry> entries = new List<IndexEntry>();

    // Act
    IndexFile indexFile = new IndexFile(0, entries);

    // Assert
    Assert.Empty(indexFile);
    Assert.True(indexFile.IsEmpty);
  }

  [Fact]
  public void Constructor_WithValidationMetadata_PreservesIt()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(3);

    // Act
    IndexFile indexFile = new IndexFile(1, entries, isValid: false, validatedAgainstFileSize: 102400);

    // Assert
    Assert.False(indexFile.IsValid);
    Assert.Equal(102400L, indexFile.ValidatedAgainstFileSize);
  }

  [Fact]
  public void Constructor_WithNullEntries_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => new IndexFile(0, null!));

    Assert.Equal("entries", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithNegativeConferenceNumber_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    List<IndexEntry> entries = new List<IndexEntry>();

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexFile(-1, entries));

    Assert.Contains("Conference number must be between 0 and 65535", ex.Message);
    Assert.Equal("conferenceNumber", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithConferenceNumberTooLarge_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    List<IndexEntry> entries = new List<IndexEntry>();

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexFile(65536, entries));

    Assert.Contains("Conference number must be between 0 and 65535", ex.Message);
  }

  [Fact]
  public void Constructor_WithConferenceNumber65535_IsValid()
  {
    // Arrange
    List<IndexEntry> entries = new List<IndexEntry>();

    // Act
    IndexFile indexFile = new IndexFile(65535, entries);

    // Assert
    Assert.Equal(65535, indexFile.ConferenceNumber);
  }

  [Fact]
  public void Indexer_ReturnsCorrectEntry()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(5);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act
    IndexEntry entry = indexFile[2];

    // Assert
    Assert.Equal(3, entry.MessageNumber); // 0-based index, 1-based message number
    Assert.Equal(20, entry.RecordOffset);
  }

  [Fact]
  public void Indexer_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(3);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => indexFile[5]);
    Assert.Throws<ArgumentOutOfRangeException>(() => indexFile[-1]);
  }

  [Fact]
  public void FindByMessageNumber_WithExistingMessage_ReturnsEntry()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(5);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act
    IndexEntry? result = indexFile.FindByMessageNumber(3);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(3, result.Value.MessageNumber);
    Assert.Equal(20, result.Value.RecordOffset);
  }

  [Fact]
  public void FindByMessageNumber_WithNonExistingMessage_ReturnsNull()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(5);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act
    IndexEntry? result = indexFile.FindByMessageNumber(99);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void FindByMessageNumber_InEmptyIndex_ReturnsNull()
  {
    // Arrange
    IndexFile indexFile = new IndexFile(1, Array.Empty<IndexEntry>());

    // Act
    IndexEntry? result = indexFile.FindByMessageNumber(1);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void GetEntries_ReturnsReadOnlyList()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(3);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act
    IReadOnlyList<IndexEntry> result = indexFile.GetEntries();

    // Assert
    Assert.Equal(3, result.Count);
    Assert.Equal(entries[0], result[0]);
    Assert.Equal(entries[1], result[1]);
    Assert.Equal(entries[2], result[2]);
  }

  [Fact]
  public void GetEnumerator_IteratesAllEntries()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(5);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act
    List<IndexEntry> enumerated = new List<IndexEntry>();
    foreach (IndexEntry entry in indexFile)
    {
      enumerated.Add(entry);
    }

    // Assert
    Assert.Equal(5, enumerated.Count);
    Assert.Equal(entries[0], enumerated[0]);
    Assert.Equal(entries[4], enumerated[4]);
  }

  [Fact]
  public void GetEnumerator_WithLinq_Works()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(10);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act
    List<IndexEntry> filtered = indexFile
      .Where(e => e.MessageNumber > 5)
      .ToList();

    // Assert
    Assert.Equal(5, filtered.Count);
    Assert.All(filtered, e => Assert.True(e.MessageNumber > 5));
  }

  [Fact]
  public void ToString_WithValidIndex_ReturnsExpectedFormat()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(42);
    IndexFile indexFile = new IndexFile(123, entries, isValid: true, validatedAgainstFileSize: 102400);

    // Act
    string result = indexFile.ToString();

    // Assert
    Assert.Equal("IndexFile for conference 123: 42 entries (valid, validated against 102400 bytes)", result);
  }

  [Fact]
  public void ToString_WithInvalidIndex_ReturnsExpectedFormat()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(5);
    IndexFile indexFile = new IndexFile(0, entries, isValid: false);

    // Act
    string result = indexFile.ToString();

    // Assert
    Assert.Equal("IndexFile for conference 0: 5 entries (invalid)", result);
  }

  [Fact]
  public void ToString_WithEmptyIndex_ReturnsExpectedFormat()
  {
    // Arrange
    IndexFile indexFile = new IndexFile(99, Array.Empty<IndexEntry>());

    // Act
    string result = indexFile.ToString();

    // Assert
    Assert.Equal("IndexFile for conference 99: 0 entries (valid)", result);
  }

  [Fact]
  public void Count_ReflectsActualEntryCount()
  {
    // Arrange & Act
    IndexFile empty = new IndexFile(1, Array.Empty<IndexEntry>());
    IndexFile small = new IndexFile(1, CreateTestEntries(3));
    IndexFile large = new IndexFile(1, CreateTestEntries(1000));

    // Assert
    Assert.Empty(empty);
    Assert.Equal(3, small.Count);
    Assert.Equal(1000, large.Count);
  }

  [Fact]
  public void IsEmpty_ReflectsEmptyState()
  {
    // Arrange & Act
    IndexFile empty = new IndexFile(1, Array.Empty<IndexEntry>());
    IndexFile notEmpty = new IndexFile(1, CreateTestEntries(1));

    // Assert
    Assert.True(empty.IsEmpty);
    Assert.False(notEmpty.IsEmpty);
  }

  [Fact]
  public void Constructor_CopiesEntriesDefensively()
  {
    // Arrange
    List<IndexEntry> entries = CreateTestEntries(3);
    IndexFile indexFile = new IndexFile(1, entries);

    // Act - Modify original list
    entries.Clear();

    // Assert - IndexFile still has original entries
    Assert.Equal(3, indexFile.Count);
  }

  // Helper method to create test entries
  private static List<IndexEntry> CreateTestEntries(int count)
  {
    List<IndexEntry> entries = new List<IndexEntry>();
    for (int i = 0; i < count; i++)
    {
      byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
      entries.Add(new IndexEntry(i + 1, i * 10, msbinBytes));
    }
    return entries;
  }
}