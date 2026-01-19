using System;
using Xunit;
using QwkNet.Models.Messages;

namespace QwkNet.Tests.Models.Messages;

public sealed class MessageKludgeTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesInstance()
  {
    // Arrange
    string key = "To";
    string value = "John Doe <john@example.com>";
    string rawLine = "To: John Doe <john@example.com>\u00E3";

    // Act
    MessageKludge kludge = new MessageKludge(key, value, rawLine);

    // Assert
    Assert.Equal("To", kludge.Key);
    Assert.Equal("John Doe <john@example.com>", kludge.Value);
    Assert.Equal("To: John Doe <john@example.com>\u00E3", kludge.RawLine);
  }

  [Fact]
  public void Constructor_WithNullKey_ThrowsArgumentNullException()
  {
    // Arrange
    string? key = null;
    string value = "test";
    string rawLine = "test";

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new MessageKludge(key!, value, rawLine));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithEmptyKey_ThrowsArgumentException()
  {
    // Arrange
    string key = "";
    string value = "test";
    string rawLine = "test";

    // Act & Assert
    ArgumentException exception = Assert.Throws<ArgumentException>(
      () => new MessageKludge(key, value, rawLine));

    Assert.Equal("key", exception.ParamName);
    Assert.Contains("cannot be empty", exception.Message);
  }

  [Fact]
  public void Constructor_WithWhitespaceKey_ThrowsArgumentException()
  {
    // Arrange
    string key = "   ";
    string value = "test";
    string rawLine = "test";

    // Act & Assert
    ArgumentException exception = Assert.Throws<ArgumentException>(
      () => new MessageKludge(key, value, rawLine));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullValue_CreatesEmptyValue()
  {
    // Arrange
    string key = "Test";
    string? value = null;
    string rawLine = "Test:\u00E3";

    // Act
    MessageKludge kludge = new MessageKludge(key, value!, rawLine);

    // Assert
    Assert.Equal("Test", kludge.Key);
    Assert.Equal(string.Empty, kludge.Value);
    Assert.Equal("Test:\u00E3", kludge.RawLine);
  }

  [Fact]
  public void Constructor_WithNullRawLine_ThrowsArgumentNullException()
  {
    // Arrange
    string key = "Test";
    string value = "value";
    string? rawLine = null;

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new MessageKludge(key, value, rawLine!));

    Assert.Equal("rawLine", exception.ParamName);
  }

  [Fact]
  public void ToString_ReturnsFormattedString()
  {
    // Arrange
    MessageKludge kludge = new MessageKludge("Subject", "Extended Subject Line", "Subject: Extended Subject Line\u00E3");

    // Act
    string result = kludge.ToString();

    // Assert
    Assert.Equal("Subject: Extended Subject Line", result);
  }

  [Fact]
  public void ToString_WithEmptyValue_ReturnsKeyWithColon()
  {
    // Arrange
    MessageKludge kludge = new MessageKludge("Flag", "", "Flag:\u00E3");

    // Act
    string result = kludge.ToString();

    // Assert
    Assert.Equal("Flag: ", result);
  }

  [Fact]
  public void RawLine_PreservesOriginalBytes()
  {
    // Arrange
    const char qwkTerminator = (char)0xE3;
    string rawLine = $"From: Jane Smith{qwkTerminator}";

    // Act
    MessageKludge kludge = new MessageKludge("From", "Jane Smith", rawLine);

    // Assert
    Assert.Contains("\u00E3", kludge.RawLine);
    Assert.Equal(rawLine, kludge.RawLine);
  }

  [Fact]
  public void Constructor_WithLongValue_PreservesFullValue()
  {
    // Arrange
    string longValue = new string('X', 100);
    string rawLine = $"Data: {longValue}\u00E3";

    // Act
    MessageKludge kludge = new MessageKludge("Data", longValue, rawLine);

    // Assert
    Assert.Equal(100, kludge.Value.Length);
    Assert.Equal(longValue, kludge.Value);
  }
}
