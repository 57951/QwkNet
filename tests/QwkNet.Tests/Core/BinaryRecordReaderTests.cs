using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using QwkNet.Core;

namespace QwkNet.Tests.Core;

/// <summary>
/// Tests for <see cref="BinaryRecordReader"/>.
/// </summary>
public sealed class BinaryRecordReaderTests
{
  [Fact]
  public void Constructor_WithNullStream_ThrowsArgumentNullException()
  {
    // Arrange & Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
      new BinaryRecordReader(null!));
    
    Assert.Equal("stream", ex.ParamName);
  }

  [Fact]
  public void ReadRecord_WithValidStream_ReadsExactly128Bytes()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x42);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);
    Span<byte> buffer = stackalloc byte[128];

    // Act
    int bytesRead = reader.ReadRecord(buffer);

    // Assert
    Assert.Equal(128, bytesRead);
    Assert.Equal(0x42, buffer[0]);
    Assert.Equal(0x42, buffer[127]);
  }

  [Fact]
  public void ReadRecord_WithBufferTooSmall_ThrowsArgumentException()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x00);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);
    byte[] buffer = new byte[64]; // Too small

    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() =>
      reader.ReadRecord(buffer));
    
    Assert.Contains("128", ex.Message);
  }

  [Fact]
  public void ReadRecord_AtEndOfStream_ReturnsZero()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x00);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);
    Span<byte> buffer = stackalloc byte[128];

    // Act
    reader.ReadRecord(buffer); // Read first record
    int bytesRead = reader.ReadRecord(buffer); // Try to read beyond end

    // Assert
    Assert.Equal(0, bytesRead);
  }

  [Fact]
  public void ReadRecord_WithPartialRecord_ReturnsPartialCount()
  {
    // Arrange
    byte[] testData = new byte[64]; // Half a record
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);
    Span<byte> buffer = stackalloc byte[128];

    // Act
    int bytesRead = reader.ReadRecord(buffer);

    // Assert
    Assert.Equal(64, bytesRead);
  }

  [Fact]
  public async Task ReadRecordAsync_WithValidStream_ReadsExactly128Bytes()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x99);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    byte[]? record = await reader.ReadRecordAsync();

    // Assert
    Assert.NotNull(record);
    Assert.Equal(128, record.Length);
    Assert.Equal(0x99, record[0]);
    Assert.Equal(0x99, record[127]);
  }

  [Fact]
  public async Task ReadRecordAsync_AtEndOfStream_ReturnsNull()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x00);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    await reader.ReadRecordAsync(); // Read first record
    byte[]? record = await reader.ReadRecordAsync(); // Try to read beyond end

    // Assert
    Assert.Null(record);
  }

  [Fact]
  public void SeekToRecord_WithValidPosition_MovesStreamCorrectly()
  {
    // Arrange
    byte[] testData = new byte[128 * 5]; // 5 records
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    reader.SeekToRecord(3);

    // Assert
    Assert.Equal(3, reader.RecordPosition);
    Assert.Equal(384, stream.Position); // 128 * 3
  }

  [Fact]
  public void SeekToRecord_WithNegativePosition_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x00);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
      reader.SeekToRecord(-1));
    
    Assert.Equal("recordNumber", ex.ParamName);
  }

  [Fact]
  public void SeekToRecord_BeyondStreamLength_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x00);
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
      reader.SeekToRecord(10)); // Only 1 record exists
    
    Assert.Equal("recordNumber", ex.ParamName);
  }

  [Fact]
  public void RecordCount_WithMultipleRecords_ReturnsCorrectCount()
  {
    // Arrange
    byte[] testData = new byte[128 * 7]; // 7 records
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    long count = reader.RecordCount;

    // Assert
    Assert.Equal(7, count);
  }

  [Fact]
  public void RecordPosition_AfterReading_ReturnsCorrectPosition()
  {
    // Arrange
    byte[] testData = new byte[128 * 3];
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);
    Span<byte> buffer = stackalloc byte[128];

    // Act
    reader.ReadRecord(buffer);
    reader.ReadRecord(buffer);

    // Assert
    Assert.Equal(2, reader.RecordPosition);
  }

  [Fact]
  public void ReadAllRecords_WithMultipleRecords_ReturnsAllRecords()
  {
    // Arrange
    byte[] testData = new byte[128 * 4];
    for (int i = 0; i < 4; i++)
    {
      testData[i * 128] = (byte)i; // Mark each record
    }
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    byte[][] records = reader.ReadAllRecords();

    // Assert
    Assert.Equal(4, records.Length);
    Assert.Equal(0, records[0][0]);
    Assert.Equal(1, records[1][0]);
    Assert.Equal(2, records[2][0]);
    Assert.Equal(3, records[3][0]);
  }

  [Fact]
  public void ReadAllRecords_WithPartialLastRecord_ReturnsOnlyCompleteRecords()
  {
    // Arrange
    byte[] testData = new byte[(128 * 2) + 64]; // 2 complete + 1 partial
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    byte[][] records = reader.ReadAllRecords();

    // Assert
    Assert.Equal(2, records.Length); // Only complete records
  }

  [Fact]
  public void ValidateStreamLength_WithValidLength_ReturnsTrue()
  {
    // Arrange
    byte[] testData = new byte[128 * 3];
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    bool isValid = reader.ValidateStreamLength();

    // Assert
    Assert.True(isValid);
  }

  [Fact]
  public void ValidateStreamLength_WithInvalidLength_ReturnsFalse()
  {
    // Arrange
    byte[] testData = new byte[200]; // Not a multiple of 128
    using MemoryStream stream = new MemoryStream(testData);
    using BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    bool isValid = reader.ValidateStreamLength();

    // Assert
    Assert.False(isValid);
  }

  [Fact]
  public void Dispose_WithLeaveOpenFalse_DisposesStream()
  {
    // Arrange
    MemoryStream stream = new MemoryStream(new byte[128]);
    BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);

    // Act
    reader.Dispose();

    // Assert
    Assert.Throws<ObjectDisposedException>(() => stream.Position);
  }

  [Fact]
  public void Dispose_WithLeaveOpenTrue_DoesNotDisposeStream()
  {
    // Arrange
    MemoryStream stream = new MemoryStream(new byte[128]);
    BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: true);

    // Act
    reader.Dispose();

    // Assert
    long position = stream.Position; // Should not throw
    Assert.Equal(0, position);
    stream.Dispose();
  }

  [Fact]
  public void ReadRecord_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    byte[] testData = CreateTestRecord(0x00);
    using MemoryStream stream = new MemoryStream(testData);
    BinaryRecordReader reader = new BinaryRecordReader(stream, leaveOpen: false);
    reader.Dispose();
    byte[] buffer = new byte[128];

    // Act & Assert
    Assert.Throws<ObjectDisposedException>(() => reader.ReadRecord(buffer));
  }

  private static byte[] CreateTestRecord(byte fillByte)
  {
    byte[] record = new byte[128];
    Array.Fill(record, fillByte);
    return record;
  }
}