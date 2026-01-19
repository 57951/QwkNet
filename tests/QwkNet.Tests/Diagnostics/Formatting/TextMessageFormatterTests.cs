using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QwkNet;
using QwkNet.Diagnostics.Commands;
using QwkNet.Diagnostics.Formatting;
using QwkNet.Models.Messages;
using QwkNet.Validation;
using QwkNet.Core;

namespace QwkNet.Tests.Diagnostics.Formatting;

/// <summary>
/// Tests for the TextMessageFormatter text output formatting.
/// </summary>
public sealed class TextMessageFormatterTests
{
  [Fact]
  public void Format_WithSingleMessage_ReturnsFormattedText()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: false,
      showCp437: false);

    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result);
    Assert.Contains("MESSAGE 1 / 1", result);
    Assert.Contains("From:         Alice", result);
    Assert.Contains("To:           Bob", result);
    Assert.Contains("Subject:      Test Message", result);
  }

  [Fact]
  public void Format_WithMultipleMessages_SeparatesWithBlankLines()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
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
    Assert.Contains("MESSAGE 1 / 2", result);
    Assert.Contains("MESSAGE 2 / 2", result);
    
    // Check separation between messages (two newlines)
    string[] lines = result.Split('\n');
    int firstMessageEnd = Array.FindIndex(lines, l => l.Contains("MESSAGE 1 / 2"));
    int secondMessageStart = Array.FindIndex(lines, l => l.Contains("MESSAGE 2 / 2"));
    Assert.True(secondMessageStart > firstMessageEnd);
  }

  [Fact]
  public void Format_WithShowRawEnabled_IncludesHexDump()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: true,
      showKludges: false,
      showCp437: false);

    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("RAW HEX:", result);
    Assert.Contains("0000  ", result); // Hex dump offset
  }

  [Fact]
  public void Format_WithShowRawDisabled_DoesNotIncludeHexDump()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: false,
      showCp437: false);

    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.DoesNotContain("RAW HEX:", result);
  }

  [Fact]
  public void Format_WithShowKludgesEnabled_DisplaysKludges()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: true,
      showCp437: false);

    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("QWKE KLUDGES", result);
    Assert.Contains("From: alice@example.com", result);
  }

  [Fact]
  public void Format_WithShowKludgesDisabled_DoesNotDisplayKludges()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: false,
      showCp437: false);

    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.DoesNotContain("QWKE KLUDGES", result);
  }

  [Fact]
  public void Format_WithNoKludges_DoesNotDisplayKludgesSection()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: true,
      showCp437: false);

    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.DoesNotContain("QWKE KLUDGES", result);
  }

  [Fact]
  public void Format_WithShowCp437Enabled_DisplaysCp437Markers()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: false,
      showCp437: true);

    Message message = CreateMessageWithCp437Characters();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    // Should display E3 byte marker
    Assert.Contains("⟨E3:π⟩", result);
  }

  [Fact]
  public void Format_WithShowCp437Disabled_DoesNotDisplayCp437Markers()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: false,
      showCp437: false);

    Message message = CreateMessageWithCp437Characters();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    // Should display simpler E3 marker
    Assert.Contains("⟨E3⟩", result);
  }

  [Fact]
  public void Format_IncludesCp437Analysis()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateMessageWithCp437Characters();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("CP437 ANALYSIS:", result);
    Assert.Contains("Line terminators", result);
    Assert.Contains("Box-drawing characters:", result);
    Assert.Contains("International characters:", result);
    Assert.Contains("ANSI escape sequences:", result);
  }

  [Fact]
  public void Format_IncludesValidationNotes()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("VALIDATION NOTES:", result);
    Assert.Contains("Header complete", result);
  }

  [Fact]
  public void Format_WithCompleteMessage_ShowsCompleteConclusion()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("CONCLUSION:", result);
    Assert.Contains("complete and properly formatted", result);
  }


  [Fact]
  public void Format_DisplaysMessageMetadata()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Conference:", result);
    Assert.Contains("Status:", result);
    Assert.Contains("Blocks:", result);
    Assert.Contains("Message #:", result);
    Assert.Contains("Reference #:", result);
  }

  [Fact]
  public void Format_WithPrivateMessage_ShowsPrivateStatus()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreatePrivateMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Status:", result);
    Assert.Contains("Private", result);
  }

  [Fact]
  public void Format_WithPublicMessage_ShowsPublicStatus()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Status:", result);
    Assert.Contains("Public", result);
  }

  [Fact]
  public void Format_WithReadMessage_ShowsReadStatus()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
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
  public void Format_WithUnreadMessage_ShowsUnreadStatus()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Unread", result);
  }

  [Fact]
  public void Format_WithDeletedMessage_ShowsDeletedStatus()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
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
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateMessageWithInvalidDate();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Invalid Date", result);
  }

  [Fact]
  public void Format_WithValidDateTime_DisplaysFormattedDate()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("Date:", result);
    Assert.Contains("2025-01-15", result);
  }

  [Fact]
  public void Format_WithQwkeExtendedHeaders_ShowsExtendedMarkers()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(
      showRaw: false,
      showKludges: false,
      showCp437: false);

    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("[EXTENDED]", result);
  }

  [Fact]
  public void Format_WithEmptyBody_DisplaysEmptyContent()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateMessageWithEmptyBody();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("BODY (0 characters", result);
  }

  [Fact]
  public void Format_DisplaysCharacterAndLineCount()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("characters,", result);
    Assert.Contains("lines):", result);
  }

  [Fact]
  public void Format_WithAnsiEscapeSequences_CountsThemInAnalysis()
  {
    // Arrange
    TextMessageFormatter formatter = new TextMessageFormatter(false, false, false);
    Message message = CreateMessageWithAnsiEscapes();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.Contains("ANSI escape sequences:", result);
    // Should show at least 1 ANSI sequence
    Assert.DoesNotContain("ANSI escape sequences: 0", result);
  }

  // Helper methods to create test messages
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
    builder.AddKludge("Subject", "Extended Subject Line");

    return builder.Build();
  }

  private Message CreateMessageWithCp437Characters()
  {
    // Create message with line breaks - MessageBuilder will convert to 0xE3 (π) terminators
    string bodyWithE3 = "Line 1\nLine 2\nLine 3";
    
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
    // ESC [ sequences
    string bodyWithAnsi = "Normal text\x1B[1mBold text\x1B[0m";
    
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
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Private")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Private message")
      .SetStatus(MessageStatus.Private);

    return builder.Build();
  }

  private Message CreateReadMessage()
  {
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Read")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Read message")
      .SetStatus(MessageStatus.Read);

    return builder.Build();
  }

  private Message CreateDeletedMessage()
  {
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Alice")
      .SetTo("Bob")
      .SetSubject("Deleted")
      .SetDateTime(DateTime.Now)
      .SetBodyText("Deleted message")
      .SetStatus(MessageStatus.Deleted);

    return builder.Build();
  }

  private Message CreateMessageWithInvalidDate()
  {
    MessageBuilder builder = new MessageBuilder()
      .SetMessageNumber(1)
      .SetConferenceNumber(0)
      .SetFrom("Test")
      .SetTo("User")
      .SetSubject("Invalid Date")
      .SetBodyText("Test");

    // Don't set date - will result in null DateTime
    return builder.Build();
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
      byte[] headerBytes = message.RawHeader.RawHeader;
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