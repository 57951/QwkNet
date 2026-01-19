using System;
using Xunit;
using QwkNet.Core;
using QwkNet.Models.Messages;

namespace QwkNet.Tests.Models.Messages;

public sealed class MessageTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesInstance()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test body\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act
    Message message = new Message(
      messageNumber: 42,
      conferenceNumber: 1,
      from: "John Doe",
      to: "Jane Smith",
      subject: "Test Subject",
      dateTime: new DateTime(2025, 1, 7, 14, 30, 0),
      referenceNumber: 0,
      password: "",
      body: body,
      status: MessageStatus.None,
      kludges: kludges,
      rawHeader: header);

    // Assert
    Assert.Equal(42, message.MessageNumber);
    Assert.Equal(1, message.ConferenceNumber);
    Assert.Equal("John Doe", message.From);
    Assert.Equal("Jane Smith", message.To);
    Assert.Equal("Test Subject", message.Subject);
    Assert.Equal(new DateTime(2025, 1, 7, 14, 30, 0), message.DateTime);
    Assert.Equal(0, message.ReferenceNumber);
    Assert.Equal("", message.Password);
    Assert.Same(body, message.Body);
    Assert.Equal(MessageStatus.None, message.Status);
    Assert.Same(kludges, message.Kludges);
  }

  [Fact]
  public void Constructor_WithNegativeMessageNumber_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => new Message(-1, 1, "From", "To", "Subject", null, 0, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("messageNumber", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithMessageNumberTooLarge_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => new Message(10000000, 1, "From", "To", "Subject", null, 0, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("messageNumber", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNegativeReferenceNumber_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => new Message(1, 1, "From", "To", "Subject", null, -1, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("referenceNumber", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithReferenceNumberTooLarge_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => new Message(1, 1, "From", "To", "Subject", null, 100000000, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("referenceNumber", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullFrom_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new Message(1, 1, null!, "To", "Subject", null, 0, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("from", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullTo_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new Message(1, 1, "From", null!, "Subject", null, 0, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("to", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullSubject_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new Message(1, 1, "From", "To", null!, null, 0, "", body, MessageStatus.None, kludges, header));

    Assert.Equal("subject", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullBody_ThrowsArgumentNullException()
  {
    // Arrange
    MessageKludgeCollection kludges = new MessageKludgeCollection();
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new Message(1, 1, "From", "To", "Subject", null, 0, "", null!, MessageStatus.None, kludges, header));

    Assert.Equal("body", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullKludges_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBody body = MessageBody.FromRawText("Test\u03C0");
    QwkMessageHeader header = CreateTestHeader();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new Message(1, 1, "From", "To", "Subject", null, 0, "", body, MessageStatus.None, null!, header));

    Assert.Equal("kludges", exception.ParamName);
  }

  [Fact]
  public void IsPrivate_WithPrivateFlag_ReturnsTrue()
  {
    // Arrange
    Message message = CreateTestMessage(status: MessageStatus.Private);

    // Act & Assert
    Assert.True(message.IsPrivate);
  }

  [Fact]
  public void IsPrivate_WithoutPrivateFlag_ReturnsFalse()
  {
    // Arrange
    Message message = CreateTestMessage(status: MessageStatus.None);

    // Act & Assert
    Assert.False(message.IsPrivate);
  }

  [Fact]
  public void IsRead_WithReadFlag_ReturnsTrue()
  {
    // Arrange
    Message message = CreateTestMessage(status: MessageStatus.Read);

    // Act & Assert
    Assert.True(message.IsRead);
  }

  [Fact]
  public void IsRead_WithoutReadFlag_ReturnsFalse()
  {
    // Arrange
    Message message = CreateTestMessage(status: MessageStatus.None);

    // Act & Assert
    Assert.False(message.IsRead);
  }

  [Fact]
  public void IsDeleted_WithDeletedFlag_ReturnsTrue()
  {
    // Arrange
    Message message = CreateTestMessage(status: MessageStatus.Deleted);

    // Act & Assert
    Assert.True(message.IsDeleted);
  }

  [Fact]
  public void IsDeleted_WithoutDeletedFlag_ReturnsFalse()
  {
    // Arrange
    Message message = CreateTestMessage(status: MessageStatus.None);

    // Act & Assert
    Assert.False(message.IsDeleted);
  }

  [Fact]
  public void StatusFlags_CanBeCombined()
  {
    // Arrange
    MessageStatus combined = MessageStatus.Private | MessageStatus.Read;
    Message message = CreateTestMessage(status: combined);

    // Act & Assert
    Assert.True(message.IsPrivate);
    Assert.True(message.IsRead);
    Assert.False(message.IsDeleted);
  }

  [Fact]
  public void ToString_ReturnsFormattedString()
  {
    // Arrange
    Message message = CreateTestMessage(
      messageNumber: 123,
      from: "Alice",
      to: "Bob",
      subject: "Hello");

    // Act
    string result = message.ToString();

    // Assert
    Assert.Contains("123", result);
    Assert.Contains("Alice", result);
    Assert.Contains("Bob", result);
    Assert.Contains("Hello", result);
  }

  [Fact]
  public void RawHeader_PreservesOriginalBytes()
  {
    // Arrange
    QwkMessageHeader header = CreateTestHeader();
    Message message = CreateTestMessage(rawHeader: header);

    // Act
    QwkMessageHeader retrievedHeader = message.RawHeader;

    // Assert
    Assert.Equal(header.RawHeader.Length, retrievedHeader.RawHeader.Length);
  }

  private static Message CreateTestMessage(
    int messageNumber = 1,
    ushort conferenceNumber = 0,
    string from = "Test From",
    string to = "Test To",
    string subject = "Test Subject",
    DateTime? dateTime = null,
    int referenceNumber = 0,
    string password = "",
    MessageBody? body = null,
    MessageStatus status = MessageStatus.None,
    MessageKludgeCollection? kludges = null,
    QwkMessageHeader rawHeader = default)
  {
    body ??= MessageBody.FromRawText("Test body\u03C0");
    kludges ??= new MessageKludgeCollection();
    
    // If rawHeader is default (all zeros), create a proper test header
    if (rawHeader.RawHeader == null || rawHeader.RawHeader.Length == 0)
    {
      rawHeader = CreateTestHeader();
    }

    return new Message(
      messageNumber,
      conferenceNumber,
      from,
      to,
      subject,
      dateTime,
      referenceNumber,
      password,
      body,
      status,
      kludges,
      rawHeader);
  }

  private static QwkMessageHeader CreateTestHeader()
  {
    byte[] headerBytes = new byte[128];
    for (int i = 0; i < headerBytes.Length; i++)
    {
      headerBytes[i] = (byte)' ';
    }

    headerBytes[122] = 0xE1; // Active message flag

    return QwkMessageHeader.Parse(headerBytes);
  }
}