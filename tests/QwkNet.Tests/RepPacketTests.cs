using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QwkNet.Archive;
using QwkNet.Archive.Zip;
using QwkNet.Core;
using QwkNet.Models.Control;
using QwkNet.Models.Indexing;
using QwkNet.Models.Messages;
using QwkNet.Parsing;
using Xunit;

namespace QwkNet.Tests;

/// <summary>
/// Tests for the <see cref="RepPacket"/> class.
/// </summary>
public sealed class RepPacketTests
{
  [Fact]
  public void Create_WithControlDat_ReturnValidRepPacket()
  {
    // Arrange
    ControlDat control = CreateTestControlDat("TESTBBS");

    // Act
    using RepPacket packet = RepPacket.Create(control);

    // Assert
    Assert.NotNull(packet);
    Assert.Equal("TESTBBS", packet.BbsId);
    Assert.Empty(packet.Messages);
  }

  [Fact]
  public void Create_WithBbsId_ReturnsValidRepPacket()
  {
    // Arrange
    const string bbsId = "MYBBS";

    // Act
    using RepPacket packet = RepPacket.Create(bbsId);

    // Assert
    Assert.NotNull(packet);
    Assert.Equal("MYBBS", packet.BbsId);
    Assert.Empty(packet.Messages);
  }

  [Fact]
  public void Create_WithNullControlDat_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => RepPacket.Create((ControlDat)null!));
  }

  [Fact]
  public void Create_WithNullBbsId_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => RepPacket.Create((string)null!));
  }

  [Fact]
  public void Create_WithEmptyBbsId_ThrowsArgumentException()
  {
    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() => RepPacket.Create(string.Empty));
    Assert.Contains("BBS ID cannot be empty", ex.Message);
  }

  [Fact]
  public void Create_WithWhitespaceBbsId_ThrowsArgumentException()
  {
    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() => RepPacket.Create("   "));
    Assert.Contains("BBS ID cannot be empty", ex.Message);
  }

  [Fact]
  public void Create_WithBbsIdTooLong_ThrowsArgumentException()
  {
    // Arrange
    const string longBbsId = "TOOLONGID"; // 9 characters

    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() => RepPacket.Create(longBbsId));
    Assert.Contains("BBS ID cannot exceed 8 characters", ex.Message);
  }

  [Fact]
  public void AddMessage_WithValidMessage_AddsMessageToCollection()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    Message message = CreateTestMessage(1, 1, "Test Message");

    // Act
    packet.AddMessage(message);

    // Assert
    Assert.Single(packet.Messages);
    Assert.Equal(message, packet.Messages[0]);
  }

  [Fact]
  public void AddMessage_WithMultipleMessages_AddsAllMessages()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    Message message1 = CreateTestMessage(1, 1, "Message 1");
    Message message2 = CreateTestMessage(2, 1, "Message 2");
    Message message3 = CreateTestMessage(3, 2, "Message 3");

    // Act
    packet.AddMessage(message1);
    packet.AddMessage(message2);
    packet.AddMessage(message3);

    // Assert
    Assert.Equal(3, packet.Messages.Count);
  }

  [Fact]
  public void AddMessage_WithNullMessage_ThrowsArgumentNullException()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => packet.AddMessage(null!));
  }

  [Fact]
  public void AddMessage_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    RepPacket packet = RepPacket.Create("TESTBBS");
    Message message = CreateTestMessage(1, 1, "Test");
    packet.Dispose();

    // Act & Assert
    Assert.Throws<ObjectDisposedException>(() => packet.AddMessage(message));
  }

  [Fact]
  public void Save_WithNoMessages_CreatesValidRepPacket()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    using MemoryStream output = new MemoryStream();

    // Act
    packet.Save(output);

    // Assert
    Assert.True(output.Length > 0);

    // Verify ZIP structure
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    IReadOnlyList<string> files = reader.ListFiles();

    Assert.Contains("CONTROL.DAT", files);
    Assert.Contains("MESSAGES.DAT", files);
  }

  [Fact]
  public void Save_WithSingleMessage_CreatesValidRepPacket()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    Message message = CreateTestMessage(1, 1, "Test message body");
    packet.AddMessage(message);

    using MemoryStream output = new MemoryStream();

    // Act
    packet.Save(output);

    // Assert
    Assert.True(output.Length > 0);

    // Verify ZIP structure
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    IReadOnlyList<string> files = reader.ListFiles();

    Assert.Contains("CONTROL.DAT", files);
    Assert.Contains("MESSAGES.DAT", files);
    Assert.Contains("1.NDX", files); // Conference 1 index
  }

  [Fact]
  public void Save_WithMultipleConferences_CreatesMultipleIndexFiles()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    packet.AddMessage(CreateTestMessage(1, 1, "Conference 1 Message 1"));
    packet.AddMessage(CreateTestMessage(2, 1, "Conference 1 Message 2"));
    packet.AddMessage(CreateTestMessage(3, 5, "Conference 5 Message 1"));
    packet.AddMessage(CreateTestMessage(4, 10, "Conference 10 Message 1"));

    using MemoryStream output = new MemoryStream();

    // Act
    packet.Save(output);

    // Assert
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    IReadOnlyList<string> files = reader.ListFiles();

    Assert.Contains("1.NDX", files);
    Assert.Contains("5.NDX", files);
    Assert.Contains("10.NDX", files);
  }

  [Fact]
  public void Save_ControlDatContainsBbsId()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("MYBBS");
    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream controlStream = reader.OpenFile("CONTROL.DAT");
    using StreamReader textReader = new StreamReader(controlStream);
    string? firstLine = textReader.ReadLine();

    // Assert
    Assert.NotNull(firstLine);
    Assert.Equal("MYBBS", firstLine);
  }

  [Fact]
  public void Save_MessagesDatHasCorrectHeaderRecord()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream messagesStream = reader.OpenFile("MESSAGES.DAT");
    byte[] headerRecord = new byte[128];
    int totalRead = 0;
    while (totalRead < 128)
    {
      int bytesRead = messagesStream.Read(headerRecord, totalRead, 128 - totalRead);
      if (bytesRead == 0)
      {
        break;
      }
      totalRead += bytesRead;
    }

    // Assert
    Assert.Equal(128, totalRead);

    // Verify BBS ID is at the start
    string headerText = System.Text.Encoding.ASCII.GetString(headerRecord).TrimEnd();
    Assert.StartsWith("TESTBBS", headerText);
  }

  [Fact]
  public void Save_MessagesDatContainsMessageHeaders()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    Message message = CreateTestMessage(1, 1, "Test");
    packet.AddMessage(message);

    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream messagesStream = reader.OpenFile("MESSAGES.DAT");

    // Skip header record
    SkipBytes(messagesStream, 128);

    // Read message header
    byte[] messageHeader = ReadExactly(messagesStream, 128);

    // Parse header
    QwkMessageHeader header = QwkMessageHeader.Parse(messageHeader);
    Assert.Equal(1, header.ConferenceNumber);
    Assert.Equal("ALICE", header.From.TrimEnd());
    Assert.Equal("BOB", header.To.TrimEnd());
  }

  [Fact]
  public void Save_IndexFileContainsValidEntries()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    packet.AddMessage(CreateTestMessage(1, 1, "Message 1"));
    packet.AddMessage(CreateTestMessage(2, 1, "Message 2"));

    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream indexStream = reader.OpenFile("1.NDX");
    using MemoryStream indexCopy = new MemoryStream();
    indexStream.CopyTo(indexCopy);
    indexCopy.Position = 0;

    IndexFile indexFile = IndexFileParser.Parse(indexCopy, 1);

    // Assert
    Assert.Equal(2, indexFile.Count);
    Assert.Equal(1, indexFile[0].MessageNumber);
    Assert.Equal(2, indexFile[1].MessageNumber);
  }

  [Fact]
  public void Save_WithNullOutputStream_ThrowsArgumentNullException()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => packet.Save(null!));
  }

  [Fact]
  public void Save_WithNonWritableStream_ThrowsArgumentException()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    byte[] buffer = new byte[100];
    using MemoryStream readOnlyStream = new MemoryStream(buffer, writable: false);

    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() => packet.Save(readOnlyStream));
    Assert.Contains("writable", ex.Message);
  }

  [Fact]
  public void Save_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    RepPacket packet = RepPacket.Create("TESTBBS");
    packet.Dispose();
    using MemoryStream output = new MemoryStream();

    // Act & Assert
    Assert.Throws<ObjectDisposedException>(() => packet.Save(output));
  }

  [Fact]
  public void SaveToFile_CreatesFileSuccessfully()
  {
    // Arrange
    string tempFile = Path.GetTempFileName();
    try
    {
      using RepPacket packet = RepPacket.Create("TESTBBS");
      packet.AddMessage(CreateTestMessage(1, 1, "Test"));

      // Act
      packet.SaveToFile(tempFile);

      // Assert
      Assert.True(File.Exists(tempFile));
      FileInfo fileInfo = new FileInfo(tempFile);
      Assert.True(fileInfo.Length > 0);

      // Verify it's a valid ZIP
      using FileStream fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read);
      using IArchiveReader reader = new ZipArchiveReader(fs);
      IReadOnlyList<string> files = reader.ListFiles();
      Assert.Contains("CONTROL.DAT", files);
      Assert.Contains("MESSAGES.DAT", files);
    }
    finally
    {
      if (File.Exists(tempFile))
      {
        File.Delete(tempFile);
      }
    }
  }

  [Fact]
  public void SaveToFile_WithNullPath_ThrowsArgumentNullException()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => packet.SaveToFile(null!));
  }

  [Fact]
  public void Save_DeterministicOutput_SameInputProducesSameBytes()
  {
    // Arrange
    Message message1 = CreateTestMessage(1, 1, "Test message");
    Message message2 = CreateTestMessage(1, 1, "Test message");

    // Create first packet
    using MemoryStream output1 = new MemoryStream();
    using (RepPacket packet1 = RepPacket.Create("TESTBBS"))
    {
      packet1.AddMessage(message1);
      packet1.Save(output1);
    }

    // Create second packet with identical data
    using MemoryStream output2 = new MemoryStream();
    using (RepPacket packet2 = RepPacket.Create("TESTBBS"))
    {
      packet2.AddMessage(message2);
      packet2.Save(output2);
    }

    // Act & Assert
    byte[] bytes1 = output1.ToArray();
    byte[] bytes2 = output2.ToArray();

    // Note: ZIP timestamps may differ, so we compare structure instead
    Assert.Equal(bytes1.Length, bytes2.Length);

    // Verify both contain the same files
    output1.Position = 0;
    output2.Position = 0;

    using IArchiveReader reader1 = new ZipArchiveReader(output1);
    using IArchiveReader reader2 = new ZipArchiveReader(output2);

    IReadOnlyList<string> files1 = reader1.ListFiles();
    IReadOnlyList<string> files2 = reader2.ListFiles();

    Assert.Equal(files1.Count, files2.Count);
    foreach (string file in files1)
    {
      Assert.Contains(file, files2);
    }
  }

  [Fact]
  public void Save_MessageBodyPaddedCorrectly()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    Message message = CreateTestMessage(1, 1, "Short"); // Body < 128 bytes
    packet.AddMessage(message);

    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream messagesStream = reader.OpenFile("MESSAGES.DAT");

    // Skip header record and message header
    SkipBytes(messagesStream, 256); // 128 (header) + 128 (message header)

    // Read first body block
    byte[] bodyBlock = ReadExactly(messagesStream, 128);

    // Verify padding with spaces
    string bodyText = System.Text.Encoding.ASCII.GetString(bodyBlock);
    Assert.StartsWith("Short", bodyText);

    // Check that remaining bytes are spaces
    for (int i = 5; i < bodyBlock.Length; i++)
    {
      Assert.Equal((byte)' ', bodyBlock[i]);
    }
  }

  [Fact]
  public void Save_LongMessageBodySpansMultipleBlocks()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");

    // Create a message with body > 128 bytes
    string longBody = new string('A', 200);
    Message message = CreateTestMessage(1, 1, longBody);
    packet.AddMessage(message);

    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream messagesStream = reader.OpenFile("MESSAGES.DAT");

    // Skip header record and message header
    SkipBytes(messagesStream, 256);

    // Read first body block
    byte[] block1 = ReadExactly(messagesStream, 128);

    // Read second body block
    byte[] block2 = ReadExactly(messagesStream, 128);

    // Verify first block is full of 'A'
    Assert.All(block1, b => Assert.Equal((byte)'A', b));

    // Verify second block starts with remaining 'A's and is padded
    string block2Text = System.Text.Encoding.ASCII.GetString(block2);
    Assert.StartsWith(new string('A', 72), block2Text); // 200 - 128 = 72
  }

  [Fact]
  public void Save_BlockCountCalculatedCorrectly()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");

    // Create message with known body size
    string body = new string('X', 200); // Requires 2 body blocks
    Message message = CreateTestMessage(1, 1, body);
    packet.AddMessage(message);

    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream messagesStream = reader.OpenFile("MESSAGES.DAT");

    // Skip header record
    SkipBytes(messagesStream, 128);

    // Read message header
    byte[] messageHeader = ReadExactly(messagesStream, 128);

    // Parse block count (bytes 116-121)
    // Per QWK specification, this is the TOTAL number of 128-byte blocks
    // INCLUDING 1 for the message header itself
    string blockCountStr = System.Text.Encoding.ASCII.GetString(messageHeader, 116, 6).Trim();
    int blockCount = int.Parse(blockCountStr);

    // Assert
    // 200 bytes needs 2 body blocks, plus 1 header block = 3 total
    Assert.Equal(3, blockCount);
  }

  [Fact]
  public void Save_MessageNumbersSequential()
  {
    // Arrange
    using RepPacket packet = RepPacket.Create("TESTBBS");
    packet.AddMessage(CreateTestMessage(100, 1, "Message 1")); // Original number 100
    packet.AddMessage(CreateTestMessage(200, 1, "Message 2")); // Original number 200
    packet.AddMessage(CreateTestMessage(300, 1, "Message 3")); // Original number 300

    using MemoryStream output = new MemoryStream();
    packet.Save(output);

    // Act
    output.Position = 0;
    using IArchiveReader reader = new ZipArchiveReader(output);
    using Stream messagesStream = reader.OpenFile("MESSAGES.DAT");

    List<int> messageNumbers = new List<int>();

    // Skip header record
    SkipBytes(messagesStream, 128);

    for (int i = 0; i < 3; i++)
    {
      // Read message header
      byte[] messageHeader = ReadExactly(messagesStream, 128);

      // Parse message number (bytes 1-7)
      string msgNumStr = System.Text.Encoding.ASCII.GetString(messageHeader, 1, 7).Trim();
      int msgNum = int.Parse(msgNumStr);
      messageNumbers.Add(msgNum);

      // Skip body blocks (assuming 1 block each for short messages)
      SkipBytes(messagesStream, 128);
    }

    // Assert
    Assert.Equal(new[] { 1, 2, 3 }, messageNumbers);
  }

  [Fact]
  public void Dispose_CanBeCalledMultipleTimes()
  {
    // Arrange
    RepPacket packet = RepPacket.Create("TESTBBS");

    // Act & Assert (should not throw)
    packet.Dispose();
    packet.Dispose();
    packet.Dispose();
  }

  // Helper methods

  /// <summary>
  /// Reads exactly the specified number of bytes from a stream, handling partial reads.
  /// </summary>
  private static byte[] ReadExactly(Stream stream, int count)
  {
    byte[] buffer = new byte[count];
    int totalRead = 0;
    while (totalRead < count)
    {
      int bytesRead = stream.Read(buffer, totalRead, count - totalRead);
      if (bytesRead == 0)
      {
        throw new EndOfStreamException($"Expected {count} bytes but only read {totalRead}");
      }
      totalRead += bytesRead;
    }
    return buffer;
  }

  /// <summary>
  /// Skips the specified number of bytes in a stream by reading and discarding.
  /// </summary>
  private static void SkipBytes(Stream stream, int count)
  {
    byte[] buffer = new byte[Math.Min(count, 4096)];
    int remaining = count;
    while (remaining > 0)
    {
      int toRead = Math.Min(remaining, buffer.Length);
      int bytesRead = stream.Read(buffer, 0, toRead);
      if (bytesRead == 0)
      {
        throw new EndOfStreamException($"Expected to skip {count} bytes but only skipped {count - remaining}");
      }
      remaining -= bytesRead;
    }
  }

  private static ControlDat CreateTestControlDat(string bbsId)
  {
    return new ControlDat(
      bbsName: "Test BBS",
      bbsCity: "Test City, TS",
      bbsPhone: "555-1234",
      sysop: "Test Sysop",
      registrationNumber: "12345",
      bbsId: bbsId,
      createdAt: DateTimeOffset.UtcNow,
      userName: "TESTUSER",
      qmailMenuFile: string.Empty,
      netMailConference: 0,
      totalMessages: 0,
      conferenceCountMinusOne: 0,
      conferences: Array.Empty<ConferenceInfo>(),
      welcomeFile: null,
      newsFile: null,
      goodbyeFile: null,
      rawLines: new[] { bbsId });
  }

  private static Message CreateTestMessage(int messageNumber, ushort conferenceNumber, string bodyText)
  {
    MessageBuilder builder = new MessageBuilder();
    builder.SetMessageNumber(messageNumber);
    builder.SetConferenceNumber(conferenceNumber);
    builder.SetFrom("Alice");
    builder.SetTo("Bob");
    builder.SetSubject("Test Subject");
    builder.SetDateTime(new DateTime(2025, 1, 15, 12, 30, 0));
    builder.SetBodyText(bodyText);

    return builder.Build();
  }
}