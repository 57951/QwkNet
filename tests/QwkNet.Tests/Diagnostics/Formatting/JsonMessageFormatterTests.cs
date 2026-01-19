using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using QwkNet;
using QwkNet.Diagnostics.Commands;
using QwkNet.Diagnostics.Formatting;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Tests.Diagnostics.Formatting;

/// <summary>
/// Tests for the JsonMessageFormatter JSON output formatting.
/// </summary>
public sealed class JsonMessageFormatterTests
{
  [Fact]
  public void Format_WithSingleMessage_ReturnsValidJsonObject()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result);
    
    // Validate JSON structure
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    
    Assert.Equal(JsonValueKind.Object, root.ValueKind);
    Assert.True(root.TryGetProperty("messageNumber", out _));
    Assert.True(root.TryGetProperty("header", out _));
    Assert.True(root.TryGetProperty("body", out _));
  }

  [Fact]
  public void Format_WithMultipleMessages_ReturnsJsonArray()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
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
    
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    
    Assert.Equal(JsonValueKind.Array, root.ValueKind);
    Assert.Equal(2, root.GetArrayLength());
  }

  [Fact]
  public void Format_UsesCamelCasePropertyNames()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    
    Assert.True(root.TryGetProperty("messageNumber", out _));
    Assert.True(root.TryGetProperty("totalMessages", out _));
    Assert.True(root.TryGetProperty("cp437Analysis", out _));
  }

  [Fact]
  public void Format_IncludesHeaderSection()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement header = doc.RootElement.GetProperty("header");
    
    Assert.Equal("Alice", header.GetProperty("from").GetString());
    Assert.Equal("Bob", header.GetProperty("to").GetString());
    Assert.Equal("Test Message", header.GetProperty("subject").GetString());
    Assert.True(header.TryGetProperty("date", out _));
    Assert.True(header.TryGetProperty("conference", out _));
    Assert.True(header.TryGetProperty("status", out _));
    Assert.True(header.TryGetProperty("messageNumber", out _));
    Assert.True(header.TryGetProperty("referenceNumber", out _));
    Assert.True(header.TryGetProperty("blockCount", out _));
  }

  [Fact]
  public void Format_IncludesBodySection()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement body = doc.RootElement.GetProperty("body");
    
    Assert.True(body.TryGetProperty("lines", out JsonElement lines));
    Assert.Equal(JsonValueKind.Array, lines.ValueKind);
    Assert.True(body.TryGetProperty("characterCount", out _));
    Assert.True(body.TryGetProperty("lineCount", out _));
  }

  [Fact]
  public void Format_IncludesCp437AnalysisSection()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement analysis = doc.RootElement.GetProperty("cp437Analysis");
    
    Assert.True(analysis.TryGetProperty("lineTerminators", out _));
    Assert.True(analysis.TryGetProperty("boxDrawingChars", out _));
    Assert.True(analysis.TryGetProperty("internationalChars", out _));
    Assert.True(analysis.TryGetProperty("ansiEscapeSequences", out _));
  }

  [Fact]
  public void Format_IncludesValidationSection()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement validation = doc.RootElement.GetProperty("validation");
    
    Assert.True(validation.TryGetProperty("headerComplete", out _));
    Assert.True(validation.TryGetProperty("bodyBlocks", out _));
    Assert.True(validation.TryGetProperty("isComplete", out _));
  }

  [Fact]
  public void Format_WithShowKludgesEnabled_IncludesKludgesSection()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(
      showKludges: true,
      showCp437: false);
    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    
    Assert.True(root.TryGetProperty("kludges", out JsonElement kludges));
    Assert.Equal(JsonValueKind.Object, kludges.ValueKind);
    Assert.True(kludges.TryGetProperty("From", out JsonElement fromKludge));
    Assert.Equal("alice@example.com", fromKludge.GetString());
  }

  [Fact]
  public void Format_WithShowKludgesDisabled_DoesNotIncludeKludges()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(
      showKludges: false,
      showCp437: false);
    Message message = CreateMessageWithKludges();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    
    Assert.False(root.TryGetProperty("kludges", out _));
  }

  [Fact]
  public void Format_WithNoKludges_DoesNotIncludeKludgesSection()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(
      showKludges: true,
      showCp437: false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement root = doc.RootElement;
    
    Assert.False(root.TryGetProperty("kludges", out _));
  }

  [Fact]
  public void Format_ConferenceIncludesNumberAndName()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement conference = doc.RootElement
      .GetProperty("header")
      .GetProperty("conference");
    
    Assert.True(conference.TryGetProperty("number", out _));
    Assert.True(conference.TryGetProperty("name", out _));
  }

  [Fact]
  public void Format_StatusIncludesAllFlags()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement status = doc.RootElement
      .GetProperty("header")
      .GetProperty("status");
    
    Assert.True(status.TryGetProperty("isPrivate", out _));
    Assert.True(status.TryGetProperty("isRead", out _));
    Assert.True(status.TryGetProperty("isDeleted", out _));
  }

  [Fact]
  public void Format_WithPrivateMessage_SetsIsPrivateToTrue()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreatePrivateMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    bool isPrivate = doc.RootElement
      .GetProperty("header")
      .GetProperty("status")
      .GetProperty("isPrivate")
      .GetBoolean();
    
    Assert.True(isPrivate);
  }

  [Fact]
  public void Format_WithPublicMessage_SetsIsPrivateToFalse()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    bool isPrivate = doc.RootElement
      .GetProperty("header")
      .GetProperty("status")
      .GetProperty("isPrivate")
      .GetBoolean();
    
    Assert.False(isPrivate);
  }

  [Fact]
  public void Format_WithReadMessage_SetsIsReadToTrue()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateReadMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    bool isRead = doc.RootElement
      .GetProperty("header")
      .GetProperty("status")
      .GetProperty("isRead")
      .GetBoolean();
    
    Assert.True(isRead);
  }

  [Fact]
  public void Format_WithDeletedMessage_SetsIsDeletedToTrue()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateDeletedMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    bool isDeleted = doc.RootElement
      .GetProperty("header")
      .GetProperty("status")
      .GetProperty("isDeleted")
      .GetBoolean();
    
    Assert.True(isDeleted);
  }

  [Fact]
  public void Format_WithInvalidDateTime_SetsDateToNull()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateMessageWithInvalidDate();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement date = doc.RootElement
      .GetProperty("header")
      .GetProperty("date");
    
    Assert.Equal(JsonValueKind.Null, date.ValueKind);
  }

  [Fact]
  public void Format_WithValidDateTime_FormatsDateAsIso8601()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    string? dateStr = doc.RootElement
      .GetProperty("header")
      .GetProperty("date")
      .GetString();
    
    Assert.NotNull(dateStr);
    Assert.Contains("2025-01-15T10:30:00", dateStr);
  }

  [Fact]
  public void Format_BodyLinesIsArray()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement lines = doc.RootElement
      .GetProperty("body")
      .GetProperty("lines");
    
    Assert.Equal(JsonValueKind.Array, lines.ValueKind);
  }

  [Fact]
  public void Format_ValidationBodyBlocksIsArray()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement bodyBlocks = doc.RootElement
      .GetProperty("validation")
      .GetProperty("bodyBlocks");
    
    Assert.Equal(JsonValueKind.Array, bodyBlocks.ValueKind);
  }

  [Fact]
  public void Format_ProducesWellFormedIndentedJson()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    Assert.NotNull(result);
    
    // Check for indentation
    Assert.Contains("  \"messageNumber\":", result);
    
    // Validate it's parseable
    JsonDocument doc = JsonDocument.Parse(result);
    Assert.NotNull(doc);
  }

  [Fact]
  public void Format_MessageNumberAndTotalMessagesAreCorrect()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message msg1 = CreateTestMessage();
    Message msg2 = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([msg1, msg2]);
    List<MessageView> messages = [
      new MessageView(msg1, 3, 10),
      new MessageView(msg2, 7, 10)
    ];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement array = doc.RootElement;
    
    int msg1Number = array[0].GetProperty("messageNumber").GetInt32();
    int msg1Total = array[0].GetProperty("totalMessages").GetInt32();
    int msg2Number = array[1].GetProperty("messageNumber").GetInt32();
    int msg2Total = array[1].GetProperty("totalMessages").GetInt32();
    
    Assert.Equal(3, msg1Number);
    Assert.Equal(10, msg1Total);
    Assert.Equal(7, msg2Number);
    Assert.Equal(10, msg2Total);
  }

  [Fact]
  public void Format_CharacterCountExcludesTrailingSpaces()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateTestMessage();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    int charCount = doc.RootElement
      .GetProperty("body")
      .GetProperty("characterCount")
      .GetInt32();
    
    // Should not count trailing spaces
    Assert.True(charCount > 0);
    Assert.Equal(28, charCount); // "This is a test message body."
  }

  [Fact]
  public void Format_WithCp437Characters_CountsThemInAnalysis()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateMessageWithCp437Characters();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement analysis = doc.RootElement.GetProperty("cp437Analysis");
    
    int lineTerminators = analysis.GetProperty("lineTerminators").GetInt32();
    Assert.True(lineTerminators > 0);
  }

  [Fact]
  public void Format_WithAnsiEscapes_CountsThemInAnalysis()
  {
    // Arrange
    JsonMessageFormatter formatter = new JsonMessageFormatter(false, false);
    Message message = CreateMessageWithAnsiEscapes();
    QwkPacket packet = CreatePacketWithMessages([message]);
    List<MessageView> messages = [new MessageView(message, 1, 1)];

    // Act
    string result = formatter.Format(messages, packet);

    // Assert
    JsonDocument doc = JsonDocument.Parse(result);
    JsonElement analysis = doc.RootElement.GetProperty("cp437Analysis");
    
    int ansiCount = analysis.GetProperty("ansiEscapeSequences").GetInt32();
    Assert.True(ansiCount > 0);
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