using System;
using Xunit;
using QwkNet.Core;

namespace QwkNet.Tests.Core;

/// <summary>
/// Tests for <see cref="QwkMessageHeader"/>.
/// </summary>
public sealed class QwkMessageHeaderTests
{
  [Fact]
  public void Parse_WithValidHeader_ParsesAllFields()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      statusByte: 0x01, // Private
      messageNumber: "1234567",
      date: "12-25-24",
      time: "14:30",
      to: "JOHN DOE",
      from: "JANE SMITH",
      subject: "Test Subject",
      password: "",
      referenceNumber: "1234",
      blockCount: 3,
      aliveFlag: 0xE1,
      conferenceNumber: 5,
      logicalMessageNumber: 100,
      networkTagIndicator: 0x20);

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Equal(0x01, header.StatusByte);
    Assert.True(header.IsPrivate);
    Assert.False(header.IsRead);
    Assert.Equal("1234567", header.MessageNumber);
    Assert.Equal("12-25-24", header.Date);
    Assert.Equal("14:30", header.Time);
    Assert.Equal("JOHN DOE", header.To);
    Assert.Equal("JANE SMITH", header.From);
    Assert.Equal("Test Subject", header.Subject);
    Assert.Equal("", header.Password);
    Assert.Equal("1234", header.ReferenceNumber);
    Assert.Equal(3, header.BlockCount);
    Assert.Equal(0xE1, header.AliveFlag);
    Assert.False(header.IsKilled);
    Assert.Equal((ushort)5, header.ConferenceNumber);
    Assert.Equal((ushort)100, header.LogicalMessageNumber);
    Assert.Equal(0x20, header.NetworkTagIndicator);
    Assert.False(header.HasNetworkTag);
  }

  [Fact]
  public void Parse_WithReadFlag_SetsIsReadTrue()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      statusByte: 0x04); // Read flag

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.True(header.IsRead);
    Assert.False(header.IsPrivate);
  }

  [Fact]
  public void Parse_WithKilledFlag_SetsIsKilledTrue()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      aliveFlag: 0xE2);

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.True(header.IsKilled);
  }

  [Fact]
  public void Parse_WithNetworkTag_SetsHasNetworkTagTrue()
  {
    // Arrange
    byte[] headerBytes1 = CreateTestHeader(
      networkTagIndicator: 0x2A);
    byte[] headerBytes2 = CreateTestHeader(
      networkTagIndicator: 0xFF);

    // Act
    QwkMessageHeader header1 = QwkMessageHeader.Parse(headerBytes1);
    QwkMessageHeader header2 = QwkMessageHeader.Parse(headerBytes2);

    // Assert
    Assert.True(header1.HasNetworkTag);
    Assert.True(header2.HasNetworkTag);
  }

  [Fact]
  public void Parse_WithTrailingSpaces_TrimsFieldsCorrectly()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      to: "JOHN          ",
      from: "JANE     ",
      subject: "TEST   ");

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Equal("JOHN", header.To);
    Assert.Equal("JANE", header.From);
    Assert.Equal("TEST", header.Subject);
  }

  [Fact]
  public void Parse_WithEmptyFields_ReturnsEmptyStrings()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      to: "",
      from: "",
      subject: "",
      password: "",
      referenceNumber: "");

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Equal("", header.To);
    Assert.Equal("", header.From);
    Assert.Equal("", header.Subject);
    Assert.Equal("", header.Password);
    Assert.Equal("", header.ReferenceNumber);
  }

  [Fact]
  public void Parse_WithInvalidBlockCount_ReturnsZero()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader();
    // Set block count field to non-numeric
    byte[] invalidCount = System.Text.Encoding.ASCII.GetBytes("ABCDEF");
    Array.Copy(invalidCount, 0, headerBytes, 116, invalidCount.Length);

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Equal(0, header.BlockCount);
  }

  [Fact]
  public void Parse_WithInvalidLength_ThrowsArgumentException()
  {
    // Arrange
    byte[] invalidHeader = new byte[64]; // Too short

    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() =>
      QwkMessageHeader.Parse(invalidHeader));
    
    Assert.Contains("128", ex.Message);
  }

  [Fact]
  public void TryGetDateTime_WithValidDate_ParsesSuccessfully()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      date: "12-25-24",
      time: "14:30");

    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Act
    bool success = header.TryGetDateTime(out DateTime dateTime);

    // Assert
    Assert.True(success);
    Assert.Equal(12, dateTime.Month);
    Assert.Equal(25, dateTime.Day);
    Assert.Equal(2024, dateTime.Year);
    Assert.Equal(14, dateTime.Hour);
    Assert.Equal(30, dateTime.Minute);
  }

  [Fact]
  public void TryGetDateTime_WithInvalidDate_ReturnsFalse()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      date: "INVALID",
      time: "14:30");

    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Act
    bool success = header.TryGetDateTime(out DateTime dateTime);

    // Assert
    Assert.False(success);
    Assert.Equal(DateTime.MinValue, dateTime);
  }

  [Fact]
  public void TryGetDateTime_WithDateOnlyNoTime_ParsesDate()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(
      date: "12-25-24",
      time: "XX:XX");

    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Act
    bool success = header.TryGetDateTime(out DateTime dateTime);

    // Assert
    Assert.True(success);
    Assert.Equal(12, dateTime.Month);
    Assert.Equal(25, dateTime.Day);
    Assert.Equal(2024, dateTime.Year);
    Assert.Equal(0, dateTime.Hour); // No time component
  }

  [Fact]
  public void RawHeader_ContainsOriginalBytes()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader();
    headerBytes[0] = 0x42; // Marker byte

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Equal(128, header.RawHeader.Length);
    Assert.Equal(0x42, header.RawHeader[0]);
  }

  [Fact]
  public void Parse_WithHighAsciiCharacters_PreservesCharacters()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader();
    // Add high-ASCII characters to subject field (offset 71)
    headerBytes[71] = 0xAE; // ®
    headerBytes[72] = 0xA9; // ©
    headerBytes[73] = 0xB1; // ±

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Contains((char)0xAE, header.Subject);
    Assert.Contains((char)0xA9, header.Subject);
    Assert.Contains((char)0xB1, header.Subject);
  }

  [Fact]
  public void Parse_WithZeroBlockCount_ReturnsZero()
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(blockCount: 0);

    // Act
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Assert
    Assert.Equal(0, header.BlockCount);
  }

  [Theory]
  [InlineData("12-25-24", true)]
  [InlineData("01-01-25", true)]
  [InlineData("99-99-99", false)]
  [InlineData("", false)]
  [InlineData("INVALID", false)]
  public void TryGetDateTime_WithVariousDates_ReturnsExpectedResult(
    string dateString,
    bool expectedSuccess)
  {
    // Arrange
    byte[] headerBytes = CreateTestHeader(date: dateString, time: "12:00");
    QwkMessageHeader header = QwkMessageHeader.Parse(headerBytes);

    // Act
    bool success = header.TryGetDateTime(out DateTime _);

    // Assert
    Assert.Equal(expectedSuccess, success);
  }

  private static byte[] CreateTestHeader(
    byte statusByte = 0x00,
    string messageNumber = "1",
    string date = "01-01-25",
    string time = "00:00",
    string to = "ALL",
    string from = "SYSOP",
    string subject = "TEST",
    string password = "",
    string referenceNumber = "0",
    int blockCount = 1,
    byte aliveFlag = 0xE1,
    ushort conferenceNumber = 0,
    ushort logicalMessageNumber = 1,
    byte networkTagIndicator = 0x20)
  {
    byte[] header = new byte[128];
    
    // Fill with spaces (0x20)
    Array.Fill(header, (byte)' ');

    // Set fields
    header[0] = statusByte;
    WriteAsciiField(header, 1, 7, messageNumber);
    WriteAsciiField(header, 8, 8, date);
    WriteAsciiField(header, 16, 5, time);
    WriteAsciiField(header, 21, 25, to);
    WriteAsciiField(header, 46, 25, from);
    WriteAsciiField(header, 71, 25, subject);
    WriteAsciiField(header, 96, 12, password);
    WriteAsciiField(header, 108, 8, referenceNumber);
    WriteAsciiField(header, 116, 6, blockCount.ToString());
    header[122] = aliveFlag;
    
    // Write little-endian shorts
    byte[] confBytes = BitConverter.GetBytes(conferenceNumber);
    Array.Copy(confBytes, 0, header, 123, 2);
    
    byte[] logicalBytes = BitConverter.GetBytes(logicalMessageNumber);
    Array.Copy(logicalBytes, 0, header, 125, 2);
    
    header[127] = networkTagIndicator;

    return header;
  }

  private static void WriteAsciiField(
    byte[] buffer,
    int offset,
    int length,
    string value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return;
    }

    byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
    int copyLength = Math.Min(bytes.Length, length);
    Array.Copy(bytes, 0, buffer, offset, copyLength);
  }
}
