using System;
using QwkNet.Models.Indexing;
using Xunit;

namespace QwkNet.Tests.Models.Indexing;

public sealed class IndexEntryTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesInstance()
  {
    // Arrange
    int messageNumber = 42;
    int recordOffset = 100;
    byte[] msbinBytes = new byte[] { 0x84, 0x00, 0x00, 0x64 }; // 100.0 in MSBIN (4 bytes)

    // Act
    IndexEntry entry = new IndexEntry(messageNumber, recordOffset, msbinBytes);

    // Assert
    Assert.Equal(42, entry.MessageNumber);
    Assert.Equal(100, entry.RecordOffset);
    Assert.Equal(4, entry.RawMsbinBytes.Length);
  }

  [Fact]
  public void Constructor_WithMessageNumberZero_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexEntry(0, 10, msbinBytes));

    Assert.Contains("Message number must be 1 or greater", ex.Message);
    Assert.Equal("messageNumber", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithNegativeMessageNumber_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexEntry(-5, 10, msbinBytes));

    Assert.Contains("Message number must be 1 or greater", ex.Message);
  }

  [Fact]
  public void Constructor_WithNegativeRecordOffset_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act & Assert
    ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexEntry(1, -10, msbinBytes));

    Assert.Contains("Record offset must be 0 or greater", ex.Message);
    Assert.Equal("recordOffset", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithInvalidMsbinBytesLength_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    byte[] shortBytes = new byte[] { 0x00, 0x00 }; // Only 2 bytes
    byte[] longBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }; // 5 bytes

    // Act & Assert (short)
    ArgumentOutOfRangeException ex1 = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexEntry(1, 10, shortBytes));
    Assert.Contains("MSBIN bytes must be exactly 4 bytes", ex1.Message);

    // Act & Assert (long)
    ArgumentOutOfRangeException ex2 = Assert.Throws<ArgumentOutOfRangeException>(
      () => new IndexEntry(1, 10, longBytes));
    Assert.Contains("MSBIN bytes must be exactly 4 bytes", ex2.Message);
  }

  [Fact]
  public void Constructor_WithRecordOffsetZero_IsValid()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act
    IndexEntry entry = new IndexEntry(1, 0, msbinBytes);

    // Assert
    Assert.Equal(1, entry.MessageNumber);
    Assert.Equal(0, entry.RecordOffset);
  }

  [Fact]
  public void GetByteOffset_CalculatesCorrectValue()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry = new IndexEntry(1, 100, msbinBytes);

    // Act
    long byteOffset = entry.GetByteOffset();

    // Assert
    Assert.Equal(12800L, byteOffset); // 100 * 128
  }

  [Fact]
  public void GetByteOffset_WithZeroRecordOffset_ReturnsZero()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
    IndexEntry entry = new IndexEntry(1, 0, msbinBytes);

    // Act
    long byteOffset = entry.GetByteOffset();

    // Assert
    Assert.Equal(0L, byteOffset);
  }

  [Fact]
  public void GetByteOffset_WithLargeRecordOffset_CalculatesCorrectly()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
    IndexEntry entry = new IndexEntry(1, 10000, msbinBytes);

    // Act
    long byteOffset = entry.GetByteOffset();

    // Assert
    Assert.Equal(1280000L, byteOffset); // 10000 * 128
  }

  [Fact]
  public void ToString_ReturnsExpectedFormat()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry = new IndexEntry(42, 100, msbinBytes);

    // Act
    string result = entry.ToString();

    // Assert
    Assert.Equal("Message #42 at record offset 100 (byte 12800)", result);
  }

  [Fact]
  public void Equals_WithSameValues_ReturnsTrue()
  {
    // Arrange
    byte[] msbinBytes1 = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    byte[] msbinBytes2 = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry1 = new IndexEntry(42, 100, msbinBytes1);
    IndexEntry entry2 = new IndexEntry(42, 100, msbinBytes2);

    // Act
    bool equal = entry1.Equals(entry2);

    // Assert
    Assert.True(equal);
  }

  [Fact]
  public void Equals_WithDifferentMessageNumber_ReturnsFalse()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry1 = new IndexEntry(42, 100, msbinBytes);
    IndexEntry entry2 = new IndexEntry(43, 100, msbinBytes);

    // Act
    bool equal = entry1.Equals(entry2);

    // Assert
    Assert.False(equal);
  }

  [Fact]
  public void Equals_WithDifferentRecordOffset_ReturnsFalse()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry1 = new IndexEntry(42, 100, msbinBytes);
    IndexEntry entry2 = new IndexEntry(42, 101, msbinBytes);

    // Act
    bool equal = entry1.Equals(entry2);

    // Assert
    Assert.False(equal);
  }

  [Fact]
  public void EqualityOperator_WithSameValues_ReturnsTrue()
  {
    // Arrange
    byte[] msbinBytes1 = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    byte[] msbinBytes2 = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry1 = new IndexEntry(42, 100, msbinBytes1);
    IndexEntry entry2 = new IndexEntry(42, 100, msbinBytes2);

    // Act & Assert
    Assert.True(entry1 == entry2);
    Assert.False(entry1 != entry2);
  }

  [Fact]
  public void GetHashCode_WithSameValues_ReturnsSameHash()
  {
    // Arrange
    byte[] msbinBytes1 = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    byte[] msbinBytes2 = new byte[] { 0x84, 0x00, 0x00, 0x64 };
    IndexEntry entry1 = new IndexEntry(42, 100, msbinBytes1);
    IndexEntry entry2 = new IndexEntry(42, 100, msbinBytes2);

    // Act
    int hash1 = entry1.GetHashCode();
    int hash2 = entry2.GetHashCode();

    // Assert
    Assert.Equal(hash1, hash2);
  }

  [Fact]
  public void RawMsbinBytes_PreservesOriginalData()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
    IndexEntry entry = new IndexEntry(1, 100, msbinBytes);

    // Act
    ReadOnlyMemory<byte> preserved = entry.RawMsbinBytes;

    // Assert
    Assert.Equal(4, preserved.Length);
    Assert.Equal(0x12, preserved.Span[0]);
    Assert.Equal(0x34, preserved.Span[1]);
    Assert.Equal(0x56, preserved.Span[2]);
    Assert.Equal(0x78, preserved.Span[3]);
  }
}