using System;
using System.Collections.Generic;
using Xunit;
using QwkNet;
using QwkNet.Diagnostics.Commands;
using QwkNet.Diagnostics.Formatting;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Tests.Diagnostics.Formatting;

/// <summary>
/// Tests for the MarkdownMessageFormatter markdown output formatting.
/// </summary>
public sealed class MarkdownMessageFormatterTests
{
  [Fact]
  public void Format_WithSingleMessage_ReturnsValidMarkdown()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result);
    Assert.Contains("# Message 1", result);
    Assert.Contains("**From:**", result);
    Assert.Contains("**To:**", result);
    Assert.Contains("**Subject:**", result);
  }

  [Fact]
  public void Format_WithMultipleMessages_SeparatesWithHorizontalRules()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message msg1 = CreateTestMessage("User1", "All", "First");
    Message msg2 = CreateTestMessage("User2", "All", "Second");
    QwkPacket packet = CreatePacketWithMessages([msg1, msg2]);
    List<MessageView> messages = [
      new MessageView(msg1, 1, 2),
      new MessageView(msg2, 2, 2)
    ];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("# Message 1", result);
    Assert.Contains("# Message 2", result);
    Assert.Contains("---", result); // Horizontal rule separator
  }

  [Fact]
  public void Format_DisplaysMetadataWithBoldFormatting()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("**From:** Alice", result);
    Assert.Contains("**To:** Bob", result);
    Assert.Contains("**Subject:** Test Message", result);
    Assert.Contains("**Date:**", result);
    Assert.Contains("**Conference:**", result);
    Assert.Contains("**Status:**", result);
  }

  [Fact]
  public void Format_WithShowKludgesEnabled_DisplaysKludgesSection()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(
      showKludges: true,
      showCp437: false);
    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("## QWKE Kludges", result);
    Assert.Contains("- **From:**", result);
    Assert.Contains("alice@example.com", result);
  }

  [Fact]
  public void Format_WithShowKludgesDisabled_DoesNotDisplayKludgesSection()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(
      showKludges: false,
      showCp437: false);
    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.DoesNotContain("## QWKE Kludges", result);
  }

  [Fact]
  public void Format_IncludesBodyInCodeBlock()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("## Body", result);
    Assert.Contains("```", result);
  }

  [Fact]
  public void Format_IncludesCp437AnalysisSection()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("## CP437 Analysis", result);
    Assert.Contains("- Line terminators", result);
    Assert.Contains("- Box-drawing characters:", result);
    Assert.Contains("- International characters:", result);
    Assert.Contains("- ANSI escape sequences:", result);
  }

  [Fact]
  public void Format_IncludesValidationNotesSection()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("## Validation Notes", result);
    Assert.Contains("✓", result);
  }

  [Fact]
  public void EscapeMarkdown_EscapesAsterisks()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage("Alice*Test", "Bob", "Subject");
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Alice\\*Test", result);
  }

  [Fact]
  public void EscapeMarkdown_EscapesUnderscores()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage("Alice_Test", "Bob", "Subject");
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Alice\\_Test", result);
  }

  [Fact]
  public void EscapeMarkdown_EscapesBrackets()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage("Alice[Test]", "Bob", "Subject");
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Alice\\[Test\\]", result);
  }

  [Fact]
  public void EscapeMarkdown_EscapesBackticks()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage("Alice`Test", "Bob", "Subject");
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Alice\\`Test", result);
  }

  [Fact]
  public void EscapeMarkdown_EscapesBackslashes()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage("Alice\\Test", "Bob", "Subject");
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Alice\\\\Test", result);
  }

  [Fact]
  public void Format_WithShowCp437Enabled_DisplaysCp437Markers()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(
      showKludges: false,
      showCp437: true);
    Message message = CreateMessageWithCp437Characters();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("⟨E3:π⟩", result);
  }

  [Fact]
  public void Format_WithShowCp437Disabled_DisplaysSimpleMarkers()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(
      showKludges: false,
      showCp437: false);
    Message message = CreateMessageWithCp437Characters();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("⟨E3⟩", result);
  }

  [Fact]
  public void Format_WithAnsiEscapes_ShowsEscapeMarkers()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, true);
    Message message = CreateMessageWithAnsiEscapes();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("⟨ESC⟩", result);
  }

  [Fact]
  public void Format_WithPrivateMessage_ShowsPrivateStatus()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreatePrivateMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("**Status:** Private", result);
  }

  [Fact]
  public void Format_WithPublicMessage_ShowsPublicStatus()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("**Status:** Public", result);
  }

  [Fact]
  public void Format_WithReadMessage_ShowsReadStatus()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateReadMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Read", result);
  }

  [Fact]
  public void Format_WithDeletedMessage_ShowsDeletedStatus()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateDeletedMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Deleted", result);
  }

  [Fact]
  public void Format_WithInvalidDateTime_ShowsInvalidDateMessage()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateMessageWithInvalidDate();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("**Date:** Invalid Date", result);
  }

  [Fact]
  public void Format_WithValidDateTime_DisplaysFormattedDate()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("**Date:** 2025-01-15", result);
  }

  [Fact]
  public void Format_WithQwkeExtendedHeaders_ShowsExtendedMarkers()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("EXTENDED", result);
  }

  [Fact]
  public void Format_TrimsTrailingSpacesFromBody()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    // Body should not have excessive trailing spaces
    Assert.DoesNotContain("     \n```", result);
  }

  [Fact]
  public void Format_KludgesDisplayedAsList()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(true, false);
    Message message = CreateMessageWithMultipleKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    // Kludges should be list items
    string[] lines = result.Split('\n');
    int kludgeCount = 0;
    foreach (string line in lines)
    {
      if (line.TrimStart().StartsWith("- **"))
      {
        kludgeCount++;
      }
    }
    Assert.True(kludgeCount > 0);
  }

  [Fact]
  public void Format_ValidationNotesDisplayedAsList()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("- ✓", result);
  }

  [Fact]
  public void Format_EmptyBody_DisplaysEmptyCodeBlock()
  {
    // Arrange
    MarkdownMessageFormatter formatter = new MarkdownMessageFormatter(false, false);
    Message message = CreateMessageWithEmptyBody();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("## Body", result);
    Assert.Contains("```", result);
  }

  // Helper methods
  private Message CreateTestMessage(
    string from = "Alice",
    string to = "Bob",
    string subject = "Test Message")
  {
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom(from)
      .SetTo(to)
      .SetSubject(subject)
      .SetDateTime(new DateTime(2025, 1, 15, 10, 30, 0))
      .SetBodyText("This is a test message body.")
      .Build();
  }

  private Message CreateMessageWithKludges()
  {
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Test body");

    builder.AddKludge("From", "alice@example.com");
    builder.AddKludge("To", "bob@example.com");
    builder.AddKludge("Subject", "Extended Subject");

    return builder.Build();
  }

  private Message CreateMessageWithMultipleKludges()
  {
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Test")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Test");

    builder.AddKludge("From", "alice@example.com");
    builder.AddKludge("To", "bob@example.com");
    builder.AddKludge("Subject", "Extended");
    builder.AddKludge("MSGID", "1:234/567 12345678");

    return builder.Build();
  }

  private Message CreateMessageWithCp437Characters()
  {
    // Create message with line breaks - MessageBuilder will convert to 0xE3 (π) terminators
    string bodyWithE3 = "Line 1\nLine 2";
    
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Test")
      .SetTo("User")
      .SetSubject("CP437 Test")
      .SetDateTime(DateTime.Now)
      .SetBodyText(bodyWithE3)
      .Build();
  }

  private Message CreateMessageWithAnsiEscapes()
  {
    string bodyWithAnsi = "Normal\x1B[1mBold\x1B[0m";
    
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Test")
      .SetTo("User")
      .SetSubject("ANSI Test")
      .SetDateTime(DateTime.Now)
      .SetBodyText(bodyWithAnsi)
      .Build();
  }

  private Message CreatePrivateMessage()
  {
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Private")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Private message")
      .SetStatus(MessageStatus.Private)
      .Build();
  }

  private Message CreateReadMessage()
  {
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Read")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Read message")
      .SetStatus(MessageStatus.Read)
      .Build();
  }

  private Message CreateDeletedMessage()
  {
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Deleted")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Deleted message")
      .SetStatus(MessageStatus.Deleted)
      .Build();
  }

  private Message CreateMessageWithInvalidDate()
  {
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Test")
      .SetTo("User")
      .SetSubject("Invalid Date")
      .SetBodyText("Test")
      .Build();
  }

  private Message CreateMessageWithEmptyBody()
  {
    return new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Test")
      .SetTo("User")
      .SetSubject("Empty")
      .SetDateTime(DateTime.Now)
      .SetBodyText("")
      .Build();
  }

  private QwkPacket CreatePacketWithMessages(Message[] messages)
  {
    byte[] controlDat = CreateMinimalControlDat();
    byte[] messagesDat = CreateMessagesDat(messages);

    using System.IO.MemoryStream zipStream = new System.IO.MemoryStream();
    using (System.IO.Compression.ZipArchive archive = new System.IO.Compression.ZipArchive(
      zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
    {
      System.IO.Compression.ZipArchiveEntry controlEntry = archive.CreateEntry("CONTROL.DAT");
      using (System.IO.Stream entryStream = controlEntry.Open())
      {
        entryStream.Write(controlDat, 0, controlDat.Length);
      }

      System.IO.Compression.ZipArchiveEntry messagesEntry = archive.CreateEntry("MESSAGES.DAT");
      using (System.IO.Stream entryStream = messagesEntry.Open())
      {
        entryStream.Write(messagesDat, 0, messagesDat.Length);
      }
    }

    zipStream.Position = 0;
    return QwkPacket.Open(zipStream, ValidationMode.Lenient);
  }

  private byte[] CreateMinimalControlDat()
  {
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("TEST BBS");
    sb.AppendLine("City, ST");
    sb.AppendLine("555-1234");
    sb.AppendLine("SysOp");
    sb.AppendLine("12345,TEST");
    sb.AppendLine("01-01-2025,00:00:00");
    sb.AppendLine("USER");
    sb.AppendLine("");
    sb.AppendLine("0");
    sb.AppendLine("0");
    sb.AppendLine("General");
    
    return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
  }

  private byte[] CreateMessagesDat(Message[] messages)
  {
    using System.IO.MemoryStream ms = new System.IO.MemoryStream();
    
    foreach (Message message in messages)
    {
      // QwkMessageHeader doesn't have ToBytes(), need to recreate header
      byte[] headerBytes = CreateHeaderBytes(message);
      ms.Write(headerBytes, 0, headerBytes.Length);
      
      string bodyText = message.Body.RawText;
      byte[] bodyBytes = System.Text.Encoding.ASCII.GetBytes(bodyText);
      
      int bodyLength = bodyBytes.Length;
      int bodyBlocks = (bodyLength + 127) / 128;
      int totalBodyBytes = bodyBlocks * 128;
      
      byte[] paddedBody = new byte[totalBodyBytes];
      int bytesToCopy = Math.Min(bodyBytes.Length, totalBodyBytes);
      Array.Copy(bodyBytes, 0, paddedBody, 0, bytesToCopy);
      
      for (int i = bytesToCopy; i < totalBodyBytes; i++)
      {
        paddedBody[i] = 0x20;
      }
      
      ms.Write(paddedBody, 0, paddedBody.Length);
    }
    
    return ms.ToArray();
  }


  private byte[] CreateHeaderBytes(Message message)
  {
    // Re-create header from message properties
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(message.MessageNumber)
      .SetConferenceNumber(message.ConferenceNumber)
      .SetFrom(message.From)
      .SetTo(message.To)
      .SetSubject(message.Subject)
      .SetBodyText(message.Body.RawText);

    if (message.DateTime.HasValue)
    {
      builder.SetDateTime(message.DateTime.Value);
    }

    // Build and extract header bytes via reflection or use a simpler approach
    // Actually, we can just write the header ourselves for tests
    return CreateMinimalHeader(message);
  }

  private byte[] CreateMinimalHeader(Message message)
  {
    byte[] header = new byte[128];
    
    // Fill with spaces
    for (int i = 0; i < 128; i++)
    {
      header[i] = 0x20;
    }
    
    // Status byte
    header[0] = (byte)' ';
    
    // Message number (offset 1-7)
    string msgNum = message.MessageNumber.ToString().PadLeft(7);
    System.Text.Encoding.ASCII.GetBytes(msgNum).CopyTo(header, 1);
    
    // Block count (offset 116-121) - calculate from body
    int bodyBlocks = (message.Body.RawText.Length + 127) / 128;
    int totalBlocks = bodyBlocks + 1; // +1 for header
    string blockCount = totalBlocks.ToString().PadLeft(6);
    System.Text.Encoding.ASCII.GetBytes(blockCount).CopyTo(header, 116);
    
    // Active flag (offset 122)
    header[122] = 0xE1;
    
    // Conference number (offset 123-124)
    header[123] = (byte)(message.ConferenceNumber & 0xFF);
    header[124] = (byte)((message.ConferenceNumber >> 8) & 0xFF);
    
    return header;
  }

  private byte[] CreateUpdatedHeader(Message message)
  {
    byte[] header = new byte[128];
    Array.Copy(message.RawHeader.RawHeader, header, 128);
    
    // Calculate correct block count from body length
    int bodyLength = message.Body.RawText.Length;
    int bodyBlocks = (bodyLength + 127) / 128;
    int totalBlocks = bodyBlocks + 1;
    
    // Update block count field (offset 116)
    string blockCountStr = totalBlocks.ToString().PadLeft(6);
    byte[] blockCountBytes = System.Text.Encoding.ASCII.GetBytes(blockCountStr);
    Array.Copy(blockCountBytes, 0, header, 116, Math.Min(blockCountBytes.Length, 6));
    
    return header;
  }
}