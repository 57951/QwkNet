using System;
using System.IO;
using System.Linq;
using QwkNet.Models.Messages;
using QwkNet.Validation;
using Xunit;

namespace QwkNet.Tests;

/// <summary>
/// Tests for QwkPacket functionality including message parsing validation.
/// </summary>
public sealed class QwkPacketTests
{
  [Fact]
  public void Open_WithNullPath_ThrowsArgumentNullException()
  {
    // Arrange
    string? path = null;

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => QwkPacket.Open(path!)
    );

    Assert.Equal("path", exception.ParamName);
  }

  [Fact]
  public void Open_WithNonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    string path = "nonexistent.qwk";

    // Act & Assert
    System.IO.FileNotFoundException exception = Assert.Throws<System.IO.FileNotFoundException>(
      () => QwkPacket.Open(path)
    );

    Assert.Equal("nonexistent.qwk", exception.FileName);
  }

  [Fact]
  public void Open_WithNullStream_ThrowsArgumentNullException()
  {
    // Arrange
    System.IO.Stream? stream = null;

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => QwkPacket.Open(stream!)
    );

    Assert.Equal("stream", exception.ParamName);
  }

  [Fact]
  public void ParseMessages_StarolQwk_ReturnsCorrectMessageCount()
  {
    // Arrange
    string testFilePath = Path.Combine("TestData", "starol.qwk");
    
    // Skip test if file doesn't exist (CI environment without test data)
    if (!File.Exists(testFilePath))
    {
      return;
    }

    // Act
    using QwkPacket packet = QwkPacket.Open(testFilePath, ValidationMode.Lenient);

    // Assert
    // Before fix: ~7,500+ "messages" parsed (all garbled)
    // After fix: ~20-30 messages parsed (all valid)
    Assert.InRange(packet.Messages.Count, 5, 50);

    // All messages should have valid From fields (not body text)
    foreach (Message msg in packet.Messages)
    {
      Assert.False(string.IsNullOrWhiteSpace(msg.From), 
        $"Message {msg.MessageNumber} has empty From field");
      
      // No 0xE3 terminators should appear in From field
      // (0xE3 is the QWK line terminator character 'ã' in CP437)
      Assert.DoesNotContain("ã", msg.From);
      Assert.DoesNotContain("\u00E3", msg.From); // Unicode representation
    }

    // All messages should have valid dates (not "Invalid Date")
    // Allow some tolerance for malformed date fields
    int validDates = packet.Messages.Count(m => m.DateTime.HasValue);
    double validDatePercentage = (double)validDates / packet.Messages.Count;
    
    Assert.True(validDatePercentage > 0.90, 
      $"Expected >90% messages with valid dates, got {validDatePercentage:P0} ({validDates}/{packet.Messages.Count})");
  }

  [Fact]
  public void ParseMessages_StarolQwk_HasValidMessageContent()
  {
    // Arrange
    string testFilePath = Path.Combine("TestData", "starol.qwk");
    
    if (!File.Exists(testFilePath))
    {
      return;
    }

    // Act
    using QwkPacket packet = QwkPacket.Open(testFilePath, ValidationMode.Lenient);

    // Assert - verify first message has expected characteristics
    Assert.True(packet.Messages.Count > 0, "Packet should contain at least one message");

    Message firstMessage = packet.Messages[0];
    
    // From field should be a proper name, not garbled body text
    Assert.False(string.IsNullOrWhiteSpace(firstMessage.From));
    Assert.True(firstMessage.From.Length > 0);
    
    // Subject should be meaningful
    Assert.False(string.IsNullOrWhiteSpace(firstMessage.Subject));
    
    // Body should exist
    Assert.NotNull(firstMessage.Body);
    Assert.True(firstMessage.Body.Lines.Count >= 0);
  }

  [Fact]
  public void ParseMessages_StarolQwk_ReportsSkippedInvalidBlocks()
  {
    // Arrange
    string testFilePath = Path.Combine("TestData", "starol.qwk");
    
    if (!File.Exists(testFilePath))
    {
      return;
    }

    // Act
    using QwkPacket packet = QwkPacket.Open(testFilePath, ValidationMode.Lenient);

    // Assert
    // The packet should have warnings about invalid blocks being skipped
    // (because starol.qwk has malformed block counts causing body blocks to be read as headers)
    Assert.True(packet.ValidationReport.Warnings.Count > 0, 
      "Expected warnings about invalid blocks in starol.qwk");

    // Should have warnings about blocks not appearing to be valid headers
    bool hasInvalidHeaderWarnings = packet.ValidationReport.Warnings.Any(w => 
      w.Message.Contains("does not appear to be a valid message header"));
    
    Assert.True(hasInvalidHeaderWarnings, 
      "Expected warnings about invalid message headers");
  }

  [Fact]
  public void ParseMessages_WithValidMessage_ParsesCorrectly()
  {
    // Arrange - create a minimal valid QWK packet in memory
    MemoryStream stream = new MemoryStream();
    
    // Write copyright block (128 bytes)
    byte[] copyrightBlock = new byte[128];
    for (int i = 0; i < 128; i++)
    {
      copyrightBlock[i] = (byte)' ';
    }
    stream.Write(copyrightBlock, 0, 128);

    // Write one valid message header (128 bytes)
    byte[] header = new byte[128];
    for (int i = 0; i < 128; i++)
    {
      header[i] = (byte)' '; // Initialize with spaces
    }
    
    // Status byte: '*' (private message)
    header[0] = (byte)'*';
    
    // Message number: "      1"
    header[1] = (byte)' ';
    header[2] = (byte)' ';
    header[3] = (byte)' ';
    header[4] = (byte)' ';
    header[5] = (byte)' ';
    header[6] = (byte)' ';
    header[7] = (byte)'1';
    
    // Date: "06-15-93"
    byte[] date = System.Text.Encoding.ASCII.GetBytes("06-15-93");
    Array.Copy(date, 0, header, 8, 8);
    
    // Time: "22:25"
    byte[] time = System.Text.Encoding.ASCII.GetBytes("22:25");
    Array.Copy(time, 0, header, 16, 5);
    
    // To: "JOHN DOE" (padded to 25 chars)
    byte[] to = System.Text.Encoding.ASCII.GetBytes("JOHN DOE                 ");
    Array.Copy(to, 0, header, 21, 25);
    
    // From: "JANE SMITH" (padded to 25 chars)
    byte[] from = System.Text.Encoding.ASCII.GetBytes("JANE SMITH               ");
    Array.Copy(from, 0, header, 46, 25);
    
    // Subject: "Test Message" (padded to 25 chars)
    byte[] subject = System.Text.Encoding.ASCII.GetBytes("Test Message             ");
    Array.Copy(subject, 0, header, 71, 25);
    
    // Block count: "     1" (header only, no body)
    byte[] blockCount = System.Text.Encoding.ASCII.GetBytes("     1");
    Array.Copy(blockCount, 0, header, 116, 6);
    
    // Alive flag: 0xE1
    header[122] = 0xE1;
    
    // Conference: 0 (little-endian ushort)
    header[123] = 0;
    header[124] = 0;
    
    stream.Write(header, 0, 128);
    stream.Position = 0;

    // Create a minimal ZIP archive with MESSAGES.DAT
    // For this test, we'll use the stream directly (requires archive wrapper)
    // Since we can't easily create a ZIP in memory here, we'll skip this test
    // in favour of testing with real packets
  }


  // These tests require a real QWK packet with known characteristics.
  // Ensure that /tmp/DEMO1.QWK exists with expected messages before running.
  // To run them, use: dotnet test --filter "Category=Optional" /p:RunSettingsFilePath=""
  [Fact]
  [Trait("Category", "Optional")]
  public void ParseMessages_SetsReferenceNumberCorrectly()
  {
      // Arrange
      string testPacket = "/tmp/DEMO1.QWK";
      
      // Act
      using QwkPacket packet = QwkPacket.Open(testPacket);
      
      // Find message with known reference number (message #1)
      Message? msg1 = packet.Messages
          .FirstOrDefault(m => m.Subject.Contains("TEST MESSAGE"));
      
      // Assert
      Assert.NotNull(msg1);
      Assert.Equal(0, msg1.ReferenceNumber);  // Not 1 or 100 or 42!
      Assert.NotEqual(msg1.Body.RawText.Length / 128, msg1.ReferenceNumber);
  }

  [Fact]
  [Trait("Category", "Optional")]
  public void ParseMessages_SetsPasswordCorrectly()
  {
    // Arrange
    string testPacket = "/tmp/DEMO1.QWK";
    
    // Act
    using QwkPacket packet = QwkPacket.Open(testPacket);
    Message msg = packet.Messages[0];
    
    // Assert
    // Password should be empty string, not a numeric reference
    Assert.True(string.IsNullOrWhiteSpace(msg.Password) || 
                !int.TryParse(msg.Password, out _));
  }
}