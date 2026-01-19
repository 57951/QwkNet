using System;
using System.Collections.Generic;
using Xunit;
using QwkNet.Models.Messages;
using QwkNet.Models.Qwke;
using QwkNet.Parsing.Qwke;
using QwkNet.Core;

namespace QwkNet.Tests.Parsing.Qwke;

public sealed class QwkeLongHeaderParserTests
{
  [Fact]
  public void Parse_EmptyKludgeCollection_ReturnsEmptyHeaders()
  {
    // Arrange
    MessageKludgeCollection kludges = new MessageKludgeCollection(new List<MessageKludge>());

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
    Assert.False(headers.HasLongHeaders);
  }

  [Fact]
  public void Parse_ToKludgeOnly_ExtractsExtendedTo()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "Very Long Recipient Name That Exceeds Twenty Five Characters", "To: Very Long Recipient Name That Exceeds Twenty Five Characters\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("Very Long Recipient Name That Exceeds Twenty Five Characters", headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
    Assert.True(headers.HasLongHeaders);
  }

  [Fact]
  public void Parse_FromKludgeOnly_ExtractsExtendedFrom()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("From", "Very Long Sender Name That Exceeds Twenty Five Characters", "From: Very Long Sender Name That Exceeds Twenty Five Characters\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Equal("Very Long Sender Name That Exceeds Twenty Five Characters", headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
    Assert.True(headers.HasLongHeaders);
  }

  [Fact]
  public void Parse_SubjectKludgeOnly_ExtractsExtendedSubject()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("Subject", "Very Long Subject Line That Exceeds Twenty Five Characters", "Subject: Very Long Subject Line That Exceeds Twenty Five Characters\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Equal("Very Long Subject Line That Exceeds Twenty Five Characters", headers.ExtendedSubject);
    Assert.True(headers.HasLongHeaders);
  }

  [Fact]
  public void Parse_AllThreeKludges_ExtractsAll()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "Long Recipient Name", "To: Long Recipient Name\u00E3"),
      new MessageKludge("From", "Long Sender Name", "From: Long Sender Name\u00E3"),
      new MessageKludge("Subject", "Long Subject Line", "Subject: Long Subject Line\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("Long Recipient Name", headers.ExtendedTo);
    Assert.Equal("Long Sender Name", headers.ExtendedFrom);
    Assert.Equal("Long Subject Line", headers.ExtendedSubject);
    Assert.True(headers.HasLongHeaders);
  }

  [Fact]
  public void Parse_CaseInsensitiveKeys_FindsKludges()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("to", "Recipient", "to: Recipient\u00E3"),
      new MessageKludge("FROM", "Sender", "FROM: Sender\u00E3"),
      new MessageKludge("SuBjEcT", "Subject Line", "SuBjEcT: Subject Line\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("Recipient", headers.ExtendedTo);
    Assert.Equal("Sender", headers.ExtendedFrom);
    Assert.Equal("Subject Line", headers.ExtendedSubject);
  }

  [Fact]
  public void Parse_DuplicateToKludges_UsesFirst()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "First Recipient", "To: First Recipient\u00E3"),
      new MessageKludge("To", "Second Recipient", "To: Second Recipient\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("First Recipient", headers.ExtendedTo);
  }

  [Fact]
  public void Parse_NonHeaderKludges_Ignored()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("MSGID", "1:234/567 12345678", "MSGID: 1:234/567 12345678\u00E3"),
      new MessageKludge("To", "Recipient", "To: Recipient\u00E3"),
      new MessageKludge("REPLY", "1:234/567 87654321", "REPLY: 1:234/567 87654321\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("Recipient", headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
  }

  [Fact]
  public void Parse_NullKludgeCollection_ThrowsArgumentNullException()
  {
    // Arrange
    MessageKludgeCollection? kludges = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => QwkeLongHeaderParser.Parse(kludges!));
  }

  [Fact]
  public void Parse_MessageOverload_ExtractsFromMessage()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "Long Recipient", "To: Long Recipient\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    QwkMessageHeader header = new QwkMessageHeader();
    MessageBody body = MessageBody.FromRawText("Test message");
    Message message = new Message(
      messageNumber: 1,
      conferenceNumber: 1,
      from: "Sender",
      to: "Recipient",
      subject: "Test",
      dateTime: DateTime.Now,
      referenceNumber: 0,
      password: string.Empty,
      body: body,
      status: MessageStatus.None,
      kludges: kludges,
      rawHeader: header);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(message);

    // Assert
    Assert.Equal("Long Recipient", headers.ExtendedTo);
  }

  [Fact]
  public void Parse_NullMessage_ThrowsArgumentNullException()
  {
    // Arrange
    Message? message = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => QwkeLongHeaderParser.Parse(message!));
  }

  [Fact]
  public void Parse_EmptyValueKludges_ReturnsEmptyStrings()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", string.Empty, "To:\u00E3"),
      new MessageKludge("From", string.Empty, "From:\u00E3"),
      new MessageKludge("Subject", string.Empty, "Subject:\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal(string.Empty, headers.ExtendedTo);
    Assert.Equal(string.Empty, headers.ExtendedFrom);
    Assert.Equal(string.Empty, headers.ExtendedSubject);
    Assert.False(headers.HasLongHeaders);
  }

  [Fact]
  public void Parse_WhitespaceValueKludges_PreservesWhitespace()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "   ", "To:   \u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("   ", headers.ExtendedTo);
  }

  [Fact]
  public void Parse_SpecialCharactersInValues_Preserved()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "user@domain.com <Real Name>", "To: user@domain.com <Real Name>\u00E3"),
      new MessageKludge("Subject", "Re: [URGENT] Bug #12345 - Critical!", "Subject: Re: [URGENT] Bug #12345 - Critical!\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("user@domain.com <Real Name>", headers.ExtendedTo);
    Assert.Equal("Re: [URGENT] Bug #12345 - Critical!", headers.ExtendedSubject);
  }

  [Fact]
  public void Parse_Utf8Characters_Preserved()
  {
    // Arrange
    List<MessageKludge> kludgeList = new List<MessageKludge>
    {
      new MessageKludge("To", "Renée Müller", "To: Renée Müller\u00E3"),
      new MessageKludge("Subject", "Café ☕ Discussion", "Subject: Café ☕ Discussion\u00E3")
    };
    MessageKludgeCollection kludges = new MessageKludgeCollection(kludgeList);

    // Act
    QwkeLongHeaders headers = QwkeLongHeaderParser.Parse(kludges);

    // Assert
    Assert.Equal("Renée Müller", headers.ExtendedTo);
    Assert.Equal("Café ☕ Discussion", headers.ExtendedSubject);
  }
}
