using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using QwkNet;
using QwkNet.Diagnostics.Commands;
using QwkNet.Diagnostics.Formatting;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Tests.Diagnostics.Commands;

/// <summary>
/// Tests for the ViewCommand argument parsing and message collection logic.
/// </summary>
public sealed class ViewCommandTests
{
  [Fact]
  public void ParseOptions_WithSingleMessage_ParsesCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "5"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(5, options.MessageNumber);
    Assert.Empty(options.MessageNumbers);
    Assert.Null(options.RangeStart);
    Assert.Null(options.RangeEnd);
    Assert.Null(options.ConferenceNumber);
    Assert.False(options.ViewAll);
  }

  [Fact]
  public void ParseOptions_WithMultipleMessages_ParsesCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--messages", "1,3,5,7"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Null(options.MessageNumber);
    Assert.Equal(4, options.MessageNumbers.Count);
    Assert.Contains(1, options.MessageNumbers);
    Assert.Contains(3, options.MessageNumbers);
    Assert.Contains(5, options.MessageNumbers);
    Assert.Contains(7, options.MessageNumbers);
  }

  [Fact]
  public void ParseOptions_WithRangeOption_ParsesCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--range", "10-20"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(10, options.RangeStart);
    Assert.Equal(20, options.RangeEnd);
  }

  [Fact]
  public void ParseOptions_WithConferenceOption_ParsesCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--conference", "42"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal((ushort)42, options.ConferenceNumber);
  }

  [Fact]
  public void ParseOptions_WithAllOption_SetsViewAllFlag()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--all"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.True(options.ViewAll);
  }

  [Fact]
  public void ParseOptions_WithTextFormat_SetsFormatCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--format", "text"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(ViewFormat.Text, options.Format);
  }

  [Fact]
  public void ParseOptions_WithJsonFormat_SetsFormatCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--format", "json"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(ViewFormat.Json, options.Format);
  }

  [Fact]
  public void ParseOptions_WithMarkdownFormat_SetsFormatCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--format", "markdown"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(ViewFormat.Markdown, options.Format);
  }

  [Fact]
  public void ParseOptions_WithMdFormat_SetsMarkdownFormat()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--format", "md"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(ViewFormat.Markdown, options.Format);
  }

  [Fact]
  public void ParseOptions_WithDefaultFormat_SetsTextFormat()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(ViewFormat.Text, options.Format);
  }

  [Fact]
  public void ParseOptions_WithShowRawFlag_SetsShowRawCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--show-raw"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.True(options.ShowRaw);
  }

  [Fact]
  public void ParseOptions_WithShowKludgesFlag_SetsShowKludgesCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--show-kludges"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.True(options.ShowKludges);
  }

  [Fact]
  public void ParseOptions_WithShowCp437Flag_SetsShowCp437Correctly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--show-cp437"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.True(options.ShowCp437);
  }

  [Fact]
  public void ParseOptions_WithOutputPath_SetsOutputPathCorrectly()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--output", "output.txt"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal("output.txt", options.OutputPath);
  }

  [Fact]
  public void ParseOptions_WithInvalidMessageNumber_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "invalid"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("Invalid message number", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithInvalidRangeFormat_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--range", "10"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("Invalid range format", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithInvalidRangeNumbers_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--range", "abc-def"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("Invalid range", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithInvalidConferenceNumber_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--conference", "invalid"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("Invalid conference number", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithInvalidFormat_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--format", "invalid"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("Invalid format", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithMissingMessageValue_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("--message requires a value", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithMissingFormatValue_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--message", "1", "--format"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("--format requires a value", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithUnknownOption_SetsErrorMessage()
  {
    // Arrange
    string[] args = ["view", "packet.qwk", "--unknown"];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.True(options.HasError);
    Assert.Contains("Unknown option", options.ErrorMessage);
  }

  [Fact]
  public void ParseOptions_WithMultipleFlagsAndOptions_ParsesAllCorrectly()
  {
    // Arrange
    string[] args = [
      "view", "packet.qwk",
      "--message", "1",
      "--format", "json",
      "--show-raw",
      "--show-kludges",
      "--show-cp437",
      "--output", "result.json"
    ];

    // Act
    ViewOptions options = InvokeParseOptions(args);

    // Assert
    Assert.NotNull(options);
    Assert.False(options.HasError);
    Assert.Equal(1, options.MessageNumber);
    Assert.Equal(ViewFormat.Json, options.Format);
    Assert.True(options.ShowRaw);
    Assert.True(options.ShowKludges);
    Assert.True(options.ShowCp437);
    Assert.Equal("result.json", options.OutputPath);
  }

  [Fact]
  public void CollectMessages_WithSingleMessage_ReturnsCorrectMessage()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(5);
    ViewOptions options = new ViewOptions { MessageNumber = 3 };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Single(messages);
    Assert.Equal(3, messages[0].DisplayNumber);
    Assert.Equal(5, messages[0].TotalMessages);
  }

  [Fact]
  public void CollectMessages_WithMultipleMessages_ReturnsCorrectMessages()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(10);
    ViewOptions options = new ViewOptions();
    options.MessageNumbers.Add(2);
    options.MessageNumbers.Add(5);
    options.MessageNumbers.Add(8);

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Equal(3, messages.Count);
    Assert.Equal(2, messages[0].DisplayNumber);
    Assert.Equal(5, messages[1].DisplayNumber);
    Assert.Equal(8, messages[2].DisplayNumber);
  }

  [Fact]
  public void CollectMessages_WithRange_ReturnsCorrectRange()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(20);
    ViewOptions options = new ViewOptions
    {
      RangeStart = 5,
      RangeEnd = 10
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Equal(6, messages.Count); // 5-10 inclusive
    Assert.Equal(5, messages[0].DisplayNumber);
    Assert.Equal(10, messages[5].DisplayNumber);
  }

  [Fact]
  public void CollectMessages_WithRangeBeyondPacketSize_ClampsToPacketSize()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(10);
    ViewOptions options = new ViewOptions
    {
      RangeStart = 8,
      RangeEnd = 100
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Equal(3, messages.Count); // Only messages 8-10
    Assert.Equal(8, messages[0].DisplayNumber);
    Assert.Equal(10, messages[2].DisplayNumber);
  }

  [Fact]
  public void CollectMessages_WithConferenceNumber_ReturnsOnlyConferenceMessages()
  {
    // Arrange
    QwkPacket packet = CreateMockPacketWithConferences();
    ViewOptions options = new ViewOptions
    {
      ConferenceNumber = 1
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.All(messages, m => Assert.Equal((ushort)1, m.Message.ConferenceNumber));
  }

  [Fact]
  public void CollectMessages_WithViewAll_ReturnsAllMessages()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(15);
    ViewOptions options = new ViewOptions
    {
      ViewAll = true
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Equal(15, messages.Count);
    Assert.Equal(1, messages[0].DisplayNumber);
    Assert.Equal(15, messages[14].DisplayNumber);
  }

  [Fact]
  public void CollectMessages_WithInvalidMessageNumber_ReturnsEmptyList()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(5);
    ViewOptions options = new ViewOptions
    {
      MessageNumber = 100
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Empty(messages);
  }

  [Fact]
  public void CollectMessages_WithZeroMessageNumber_ReturnsEmptyList()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(5);
    ViewOptions options = new ViewOptions
    {
      MessageNumber = 0
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Empty(messages);
  }

  [Fact]
  public void CollectMessages_WithNegativeMessageNumber_ReturnsEmptyList()
  {
    // Arrange
    QwkPacket packet = CreateMockPacket(5);
    ViewOptions options = new ViewOptions
    {
      MessageNumber = -1
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Empty(messages);
  }

  [Fact]
  public void CollectMessages_WithNonExistentConference_ReturnsEmptyList()
  {
    // Arrange
    QwkPacket packet = CreateMockPacketWithConferences();
    ViewOptions options = new ViewOptions
    {
      ConferenceNumber = 999
    };

    // Act
    List<MessageView> messages = InvokeCollectMessages(packet, options);

    // Assert
    Assert.NotNull(messages);
    Assert.Empty(messages);
  }

  // Helper methods to invoke private static methods via reflection
  private ViewOptions InvokeParseOptions(string[] args)
  {
    System.Reflection.MethodInfo? method = typeof(ViewCommand)
      .GetMethod("ParseOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    
    Assert.NotNull(method);
    
    object? result = method.Invoke(null, new object[] { args });
    Assert.NotNull(result);
    
    return (ViewOptions)result;
  }

  private List<MessageView> InvokeCollectMessages(QwkPacket packet, ViewOptions options)
  {
    System.Reflection.MethodInfo? method = typeof(ViewCommand)
      .GetMethod("CollectMessages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    
    Assert.NotNull(method);
    
    object? result = method.Invoke(null, new object[] { packet, options });
    Assert.NotNull(result);
    
    return (List<MessageView>)result;
  }

  // Mock packet creation helpers
  private QwkPacket CreateMockPacket(int messageCount)
  {
    List<Message> messages = new List<Message>();
    
    for (int i = 1; i <= messageCount; i++)
    {
      Message message = new MessageBuilder()
        .SetMessageNumber(i)
        .SetConferenceNumber(0)
        .SetFrom($"User{i}")
        .SetTo("All")
        .SetSubject($"Test Message {i}")
        .SetDateTime(new DateTime(2025, 1, i % 28 + 1))
        .SetBodyText($"This is test message number {i}.")
        .Build();
      
      messages.Add(message);
    }

    return CreatePacketFromMessages(messages);
  }

  private QwkPacket CreateMockPacketWithConferences()
  {
    List<Message> messages = new List<Message>();
    
    // Add messages to conference 0
    for (int i = 0; i < 3; i++)
    {
      Message message = new MessageBuilder()
        .SetMessageNumber(i + 1)
        .SetConferenceNumber(0)
        .SetFrom($"User{i}")
        .SetTo("All")
        .SetSubject("General")
        .SetDateTime(DateTime.Now)
        .SetBodyText("Message in conference 0")
        .Build();
      
      messages.Add(message);
    }

    // Add messages to conference 1
    for (int i = 0; i < 3; i++)
    {
      Message message = new MessageBuilder()
        .SetMessageNumber(i + 4)
        .SetConferenceNumber(1)
        .SetFrom($"User{i}")
        .SetTo("All")
        .SetSubject("Support")
        .SetDateTime(DateTime.Now)
        .SetBodyText("Message in conference 1")
        .Build();
      
      messages.Add(message);
    }

    // Add messages to conference 2
    for (int i = 0; i < 2; i++)
    {
      Message message = new MessageBuilder()
        .SetMessageNumber(i + 7)
        .SetConferenceNumber(2)
        .SetFrom($"User{i}")
        .SetTo("All")
        .SetSubject("Development")
        .SetDateTime(DateTime.Now)
        .SetBodyText("Message in conference 2")
        .Build();
      
      messages.Add(message);
    }

    return CreatePacketFromMessages(messages);
  }

  private QwkPacket CreatePacketFromMessages(List<Message> messages)
  {
    // Create a minimal valid QWK packet in memory
    byte[] controlDat = CreateMinimalControlDat();
    byte[] messagesDat = CreateMessagesDat(messages);

    using MemoryStream zipStream = new MemoryStream();
    using (System.IO.Compression.ZipArchive archive = new System.IO.Compression.ZipArchive(
      zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
    {
      // Add CONTROL.DAT
      System.IO.Compression.ZipArchiveEntry controlEntry = archive.CreateEntry("CONTROL.DAT");
      using (Stream entryStream = controlEntry.Open())
      {
        entryStream.Write(controlDat, 0, controlDat.Length);
      }

      // Add MESSAGES.DAT
      System.IO.Compression.ZipArchiveEntry messagesEntry = archive.CreateEntry("MESSAGES.DAT");
      using (Stream entryStream = messagesEntry.Open())
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
    sb.AppendLine("SysOp Name");
    sb.AppendLine("12345,TEST");
    sb.AppendLine("01-01-2025,00:00:00");
    sb.AppendLine("TEST USER");
    sb.AppendLine("");
    sb.AppendLine("0");
    sb.AppendLine("0");
    sb.AppendLine("General");
    
    return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
  }

  private byte[] CreateMessagesDat(List<Message> messages)
  {
    using MemoryStream ms = new MemoryStream();
    
    // Write copyright block (first 128 bytes)
    byte[] copyrightBlock = new byte[128];
    string copyright = "Produced by TEST BBS";
    byte[] copyrightBytes = System.Text.Encoding.ASCII.GetBytes(copyright);
    Array.Copy(copyrightBytes, copyrightBlock, Math.Min(copyrightBytes.Length, 128));
    // Pad rest with spaces
    for (int i = copyrightBytes.Length; i < 128; i++)
    {
      copyrightBlock[i] = 0x20;
    }
    ms.Write(copyrightBlock, 0, 128);
    
    foreach (Message message in messages)
    {
      // QwkMessageHeader doesn't have ToBytes(), need to recreate header
      byte[] headerBytes = CreateHeaderBytes(message);

      ms.Write(headerBytes, 0, headerBytes.Length);
      
      // Write body blocks
      string bodyText = message.Body.RawText;
      byte[] bodyBytes = System.Text.Encoding.ASCII.GetBytes(bodyText);
      
      // Calculate blocks from actual body length, not from header (which is always 1)
      int bodyLength = bodyBytes.Length;
      int bodyBlocks = (bodyLength + 127) / 128; // Round up
      int totalBodyBytes = bodyBlocks * 128;
      
      byte[] paddedBody = new byte[totalBodyBytes];
      int bytesToCopy = Math.Min(bodyBytes.Length, totalBodyBytes);
      Array.Copy(bodyBytes, 0, paddedBody, 0, bytesToCopy);
      
      // Pad with spaces
      for (int i = bytesToCopy; i < totalBodyBytes; i++)
      {
        paddedBody[i] = 0x20; // Space
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
    
    // Date (offset 8-15): MM-DD-YY format
    // Use message date if available, otherwise use a default date
    DateTime messageDate = message.DateTime ?? new DateTime(2025, 1, 15);
    string dateStr = messageDate.ToString("MM-dd-yy");
    System.Text.Encoding.ASCII.GetBytes(dateStr).CopyTo(header, 8);
    
    // Time (offset 16-20): HH:MM format
    string timeStr = messageDate.ToString("HH:mm");
    System.Text.Encoding.ASCII.GetBytes(timeStr).CopyTo(header, 16);
    
    // To field (offset 21-45)
    byte[] toBytes = System.Text.Encoding.ASCII.GetBytes(message.To.PadRight(25).Substring(0, 25));
    toBytes.CopyTo(header, 21);
    
    // From field (offset 46-70)
    byte[] fromBytes = System.Text.Encoding.ASCII.GetBytes(message.From.PadRight(25).Substring(0, 25));
    fromBytes.CopyTo(header, 46);
    
    // Subject field (offset 71-95)
    byte[] subjectBytes = System.Text.Encoding.ASCII.GetBytes(message.Subject.PadRight(25).Substring(0, 25));
    subjectBytes.CopyTo(header, 71);
    
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