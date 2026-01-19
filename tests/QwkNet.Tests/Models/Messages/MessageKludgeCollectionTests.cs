using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QwkNet.Models.Messages;

namespace QwkNet.Tests.Models.Messages;

public sealed class MessageKludgeCollectionTests
{
  [Fact]
  public void Constructor_WithNullKludges_ThrowsArgumentNullException()
  {
    // Arrange
    IEnumerable<MessageKludge>? kludges = null;

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new MessageKludgeCollection(kludges!));

    Assert.Equal("kludges", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithEmptyCollection_CreatesEmptyCollection()
  {
    // Arrange & Act
    MessageKludgeCollection collection = new MessageKludgeCollection(Enumerable.Empty<MessageKludge>());

    // Assert
    Assert.Empty(collection);
  }

  [Fact]
  public void DefaultConstructor_CreatesEmptyCollection()
  {
    // Arrange & Act
    MessageKludgeCollection collection = new MessageKludgeCollection();

    // Assert
    Assert.Empty(collection);
  }

  [Fact]
  public void Constructor_WithKludges_StoresAllKludges()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("To", "Extended Name", "To: Extended Name\u00E3"),
      new MessageKludge("From", "Extended Sender", "From: Extended Sender\u00E3"),
      new MessageKludge("Subject", "Extended Subject", "Subject: Extended Subject\u00E3")
    };

    // Act
    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Assert
    Assert.Equal(3, collection.Count);
  }

  [Fact]
  public void GetByKey_WithMatchingKey_ReturnsAllMatches()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("To", "Person1", "To: Person1\u00E3"),
      new MessageKludge("CC", "Person2", "CC: Person2\u00E3"),
      new MessageKludge("To", "Person3", "To: Person3\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    IReadOnlyList<MessageKludge> matches = collection.GetByKey("To");

    // Assert
    Assert.Equal(2, matches.Count);
    Assert.Equal("Person1", matches[0].Value);
    Assert.Equal("Person3", matches[1].Value);
  }

  [Fact]
  public void GetByKey_IsCaseInsensitive()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("Subject", "Test", "Subject: Test\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    IReadOnlyList<MessageKludge> matches1 = collection.GetByKey("SUBJECT");
    IReadOnlyList<MessageKludge> matches2 = collection.GetByKey("subject");
    IReadOnlyList<MessageKludge> matches3 = collection.GetByKey("Subject");

    // Assert
    Assert.Single(matches1);
    Assert.Single(matches2);
    Assert.Single(matches3);
  }

  [Fact]
  public void GetByKey_WithNoMatches_ReturnsEmptyCollection()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("To", "Someone", "To: Someone\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    IReadOnlyList<MessageKludge> matches = collection.GetByKey("From");

    // Assert
    Assert.Empty(matches);
  }

  [Fact]
  public void GetByKey_WithNullKey_ThrowsArgumentNullException()
  {
    // Arrange
    MessageKludgeCollection collection = new MessageKludgeCollection();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => collection.GetByKey(null!));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void GetFirstByKey_WithMatchingKey_ReturnsFirstMatch()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("To", "First", "To: First\u00E3"),
      new MessageKludge("To", "Second", "To: Second\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    MessageKludge? match = collection.GetFirstByKey("To");

    // Assert
    Assert.NotNull(match);
    Assert.Equal("First", match.Value);
  }

  [Fact]
  public void GetFirstByKey_WithNoMatch_ReturnsNull()
  {
    // Arrange
    MessageKludgeCollection collection = new MessageKludgeCollection();

    // Act
    MessageKludge? match = collection.GetFirstByKey("Missing");

    // Assert
    Assert.Null(match);
  }

  [Fact]
  public void GetFirstByKey_IsCaseInsensitive()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("From", "Test", "From: Test\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    MessageKludge? match = collection.GetFirstByKey("from");

    // Assert
    Assert.NotNull(match);
    Assert.Equal("Test", match.Value);
  }

  [Fact]
  public void GetFirstByKey_WithNullKey_ThrowsArgumentNullException()
  {
    // Arrange
    MessageKludgeCollection collection = new MessageKludgeCollection();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => collection.GetFirstByKey(null!));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void ContainsKey_WithMatchingKey_ReturnsTrue()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("Subject", "Test", "Subject: Test\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    bool result = collection.ContainsKey("Subject");

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ContainsKey_WithNoMatch_ReturnsFalse()
  {
    // Arrange
    MessageKludgeCollection collection = new MessageKludgeCollection();

    // Act
    bool result = collection.ContainsKey("Missing");

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ContainsKey_IsCaseInsensitive()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("ReplyTo", "test@example.com", "ReplyTo: test@example.com\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    bool result1 = collection.ContainsKey("REPLYTO");
    bool result2 = collection.ContainsKey("replyto");
    bool result3 = collection.ContainsKey("ReplyTo");

    // Assert
    Assert.True(result1);
    Assert.True(result2);
    Assert.True(result3);
  }

  [Fact]
  public void ContainsKey_WithNullKey_ThrowsArgumentNullException()
  {
    // Arrange
    MessageKludgeCollection collection = new MessageKludgeCollection();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => collection.ContainsKey(null!));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void GetEnumerator_IteratesInInsertionOrder()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("First", "1", "First: 1\u00E3"),
      new MessageKludge("Second", "2", "Second: 2\u00E3"),
      new MessageKludge("Third", "3", "Third: 3\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    List<string> keys = collection.Select(k => k.Key).ToList();

    // Assert
    Assert.Equal(3, keys.Count);
    Assert.Equal("First", keys[0]);
    Assert.Equal("Second", keys[1]);
    Assert.Equal("Third", keys[2]);
  }

  [Fact]
  public void Collection_AllowsDuplicateKeys()
  {
    // Arrange
    List<MessageKludge> kludges = new List<MessageKludge>
    {
      new MessageKludge("CC", "person1@example.com", "CC: person1@example.com\u00E3"),
      new MessageKludge("CC", "person2@example.com", "CC: person2@example.com\u00E3"),
      new MessageKludge("CC", "person3@example.com", "CC: person3@example.com\u00E3")
    };

    MessageKludgeCollection collection = new MessageKludgeCollection(kludges);

    // Act
    IReadOnlyList<MessageKludge> matches = collection.GetByKey("CC");

    // Assert
    Assert.Equal(3, matches.Count);
    Assert.Equal(3, collection.Count);
  }
}