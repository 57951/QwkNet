using System;
using Xunit;
using QwkNet.Models.Messages;

namespace QwkNet.Tests.Models.Messages;

public sealed class MessageBuilderTests
{
  [Fact]
  public void Build_WithRequiredFields_CreatesMessage()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Hello")
      .SetBodyText("Test message")
      .Build();

    // Assert
    Assert.NotNull(message);
    Assert.Equal("Alice", message.From);
    Assert.Equal("Bob", message.To);
    Assert.Equal("Hello", message.Subject);
    Assert.Single(message.Body.Lines);
  }

  [Fact]
  public void Build_WithoutFrom_ThrowsInvalidOperationException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder()
      .SetTo("Bob")
      .SetSubject("Hello")
      .SetBodyText("Test");

    // Act & Assert
    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
      () => builder.Build());

    Assert.Contains("From", exception.Message);
  }

  [Fact]
  public void Build_WithoutTo_ThrowsInvalidOperationException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder()
      .SetFrom("Alice")
      .SetSubject("Hello")
      .SetBodyText("Test");

    // Act & Assert
    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
      () => builder.Build());

    Assert.Contains("To", exception.Message);
  }

  [Fact]
  public void Build_WithoutSubject_ThrowsInvalidOperationException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder()
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetBodyText("Test");

    // Act & Assert
    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
      () => builder.Build());

    Assert.Contains("Subject", exception.Message);
  }

  [Fact]
  public void Build_WithoutBody_ThrowsInvalidOperationException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder()
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Hello");

    // Act & Assert
    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
      () => builder.Build());

    Assert.Contains("Body", exception.Message);
  }

  [Fact]
  public void SetMessageNumber_WithValidValue_SetsMessageNumber()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetMessageNumber(42)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    Assert.Equal(42, message.MessageNumber);
  }

  [Fact]
  public void SetMessageNumber_WithNegativeValue_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => builder.SetMessageNumber(-1));

    Assert.Equal("messageNumber", exception.ParamName);
  }

  [Fact]
  public void SetMessageNumber_WithValueTooLarge_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => builder.SetMessageNumber(10000000));

    Assert.Equal("messageNumber", exception.ParamName);
  }

  [Fact]
  public void SetConferenceNumber_SetsConferenceNumber()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetConferenceNumber(5)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    Assert.Equal((ushort)5, message.ConferenceNumber);
  }

  [Fact]
  public void SetFrom_WithNullValue_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.SetFrom(null!));

    Assert.Equal("from", exception.ParamName);
  }

  [Fact]
  public void SetTo_WithNullValue_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.SetTo(null!));

    Assert.Equal("to", exception.ParamName);
  }

  [Fact]
  public void SetSubject_WithNullValue_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.SetSubject(null!));

    Assert.Equal("subject", exception.ParamName);
  }

  [Fact]
  public void SetDateTime_SetsDateTime()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();
    DateTime testDate = new DateTime(2025, 1, 7, 14, 30, 0);

    // Act
    Message message = builder
      .SetDateTime(testDate)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    Assert.Equal(testDate, message.DateTime);
  }

  [Fact]
  public void SetReferenceNumber_WithValidValue_SetsReferenceNumber()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetReferenceNumber(123)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    Assert.Equal(123, message.ReferenceNumber);
  }

  [Fact]
  public void SetReferenceNumber_WithNegativeValue_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => builder.SetReferenceNumber(-1));

    Assert.Equal("referenceNumber", exception.ParamName);
  }

  [Fact]
  public void SetReferenceNumber_WithValueTooLarge_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
      () => builder.SetReferenceNumber(100000000));

    Assert.Equal("referenceNumber", exception.ParamName);
  }

  [Fact]
  public void SetPassword_WithNullValue_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.SetPassword(null!));

    Assert.Equal("password", exception.ParamName);
  }

  [Fact]
  public void SetPassword_SetsPassword()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetPassword("secret")
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    Assert.Equal("secret", message.Password);
  }

  [Fact]
  public void SetBodyText_WithMultipleLines_CreatesMultiLineBody()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();
    string bodyText = "Line 1\r\nLine 2\r\nLine 3";

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText(bodyText)
      .Build();

    // Assert
    Assert.Equal(3, message.Body.Lines.Count);
    Assert.Equal("Line 1", message.Body.Lines[0]);
    Assert.Equal("Line 2", message.Body.Lines[1]);
    Assert.Equal("Line 3", message.Body.Lines[2]);
  }

  [Fact]
  public void SetBodyText_WithNullValue_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.SetBodyText(null!));

    Assert.Equal("text", exception.ParamName);
  }

  [Fact]
  public void SetBody_WithMessageBody_SetsBody()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();
    MessageBody body = MessageBody.FromRawText("Custom body\u00E3");

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBody(body)
      .Build();

    // Assert
    Assert.Same(body, message.Body);
  }

  [Fact]
  public void SetBody_WithNullValue_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.SetBody(null!));

    Assert.Equal("body", exception.ParamName);
  }

  [Fact]
  public void SetStatus_SetsStatusFlags()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();
    MessageStatus status = MessageStatus.Private | MessageStatus.Read;

    // Act
    Message message = builder
      .SetStatus(status)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    Assert.Equal(status, message.Status);
    Assert.True(message.IsPrivate);
    Assert.True(message.IsRead);
  }

  [Fact]
  public void AddKludge_WithKeyValue_AddsKludge()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .AddKludge("MSGID", "1:234/567 12345678")
      .Build();

    // Assert
    Assert.Single(message.Kludges);
    Assert.True(message.Kludges.ContainsKey("MSGID"));
  }

  [Fact]
  public void AddKludge_WithMultipleKludges_AddsAllKludges()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .AddKludge("To", "Long Name <email@example.com>")
      .AddKludge("From", "Long Sender <sender@example.com>")
      .AddKludge("Subject", "This is a very long subject that exceeds 25 characters")
      .Build();

    // Assert
    Assert.Equal(3, message.Kludges.Count);
  }

  [Fact]
  public void AddKludge_WithNullKey_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.AddKludge(null!, "value"));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void AddKludge_WithEmptyKey_ThrowsArgumentException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentException exception = Assert.Throws<ArgumentException>(
      () => builder.AddKludge("", "value"));

    Assert.Equal("key", exception.ParamName);
  }

  [Fact]
  public void AddKludge_WithKludgeInstance_AddsKludge()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();
    MessageKludge kludge = new MessageKludge("Custom", "Value", "Custom: Value\u00E3");

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .AddKludge(kludge)
      .Build();

    // Assert
    Assert.Single(message.Kludges);
    Assert.Same(kludge, message.Kludges.GetFirstByKey("Custom"));
  }

  [Fact]
  public void AddKludge_WithNullKludgeInstance_ThrowsArgumentNullException()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => builder.AddKludge(null!));

    Assert.Equal("kludge", exception.ParamName);
  }

  [Fact]
  public void Build_GeneratesRawHeader()
  {
    // Arrange
    MessageBuilder builder = new MessageBuilder();

    // Act
    Message message = builder
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetBodyText("Body")
      .Build();

    // Assert
    // QwkMessageHeader is a struct, so it's always "not null"
    Assert.Equal(128, message.RawHeader.RawHeader.Length);
  }

  [Fact]
  public void FluentInterface_AllowsMethodChaining()
  {
    // Arrange & Act
    Message message = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(2)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Hello")
      .SetDateTime(new DateTime(2025, 1, 7))
      .SetReferenceNumber(3)
      .SetPassword("secret")
      .SetBodyText("Test message")
      .SetStatus(MessageStatus.Private)
      .AddKludge("Test", "Value")
      .Build();

    // Assert
    Assert.NotNull(message);
    Assert.Equal(1, message.MessageNumber);
    Assert.Equal((ushort)2, message.ConferenceNumber);
    Assert.Equal("Alice", message.From);
    Assert.Equal("Bob", message.To);
  }
}