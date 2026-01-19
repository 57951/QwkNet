using System;
using System.IO;
using QwkNet.Core;
using QwkNet.Models.Indexing;
using QwkNet.Parsing;
using QwkNet.Validation;
using Xunit;

namespace QwkNet.Tests.Parsing;

public sealed class IndexFileParserTests
{
  [Fact]
  public void Parse_WithEmptyData_ReturnsEmptyIndex()
  {
    // Arrange
    byte[] data = Array.Empty<byte>();

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1);

    // Assert
    Assert.Equal(1, result.ConferenceNumber);
    Assert.Empty(result);
    Assert.True(result.IsEmpty);
    Assert.True(result.IsValid);
  }

  [Fact]
  public void Parse_WithSingleEntry_ParsesCorrectly()
  {
    // Arrange
    double recordOffset = 10.0;
    byte[] msbinBytes = MsbinConverter.FromDouble(recordOffset);
    byte[] data = msbinBytes;

    // Act
    IndexFile result = IndexFileParser.Parse(data, 5);

    // Assert
    Assert.Equal(5, result.ConferenceNumber);
    Assert.Single(result);
    Assert.Equal(1, result[0].MessageNumber);
    Assert.Equal(10, result[0].RecordOffset);
  }

  [Fact]
  public void Parse_WithMultipleEntries_ParsesAllCorrectly()
  {
    // Arrange
    byte[] data = CreateIndexData(new float[] { 1.0f, 5.0f, 10.0f, 100.0f });

    // Act
    IndexFile result = IndexFileParser.Parse(data, 42);

    // Assert
    Assert.Equal(42, result.ConferenceNumber);
    Assert.Equal(4, result.Count);

    Assert.Equal(1, result[0].MessageNumber);
    Assert.Equal(1, result[0].RecordOffset);

    Assert.Equal(2, result[1].MessageNumber);
    Assert.Equal(5, result[1].RecordOffset);

    Assert.Equal(3, result[2].MessageNumber);
    Assert.Equal(10, result[2].RecordOffset);

    Assert.Equal(4, result[3].MessageNumber);
    Assert.Equal(100, result[3].RecordOffset);
  }

  [Fact]
  public void Parse_WithInvalidFileSize_InStrictMode_ThrowsException()
  {
    // Arrange - 6 bytes (not a multiple of 4)
    byte[] data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };

    // Act & Assert
    QwkFormatException ex = Assert.Throws<QwkFormatException>(
      () => IndexFileParser.Parse(data, 1, ValidationMode.Strict));

    Assert.Contains("Index file size", ex.Message);
    Assert.Contains("6 bytes", ex.Message);
  }

  [Fact]
  public void Parse_WithInvalidFileSize_InLenientMode_ParsesCompleteEntries()
  {
    // Arrange - 6 bytes (1 complete entry + 2 extra bytes)
    byte[] completeEntry = MsbinConverter.FromDouble(42.0);
    byte[] data = new byte[6];
    Array.Copy(completeEntry, data, 4);
    data[4] = 0xFF;
    data[5] = 0xFF;

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1, ValidationMode.Lenient);

    // Assert
    Assert.Single(result);
    Assert.Equal(1, result[0].MessageNumber);
    Assert.Equal(42, result[0].RecordOffset);
    Assert.False(result.IsValid); // Marked invalid due to size issue
  }

  [Fact]
  public void Parse_WithNegativeRecordOffset_InLenientMode_SkipsEntry()
  {
    // Arrange - MSBIN representation of negative number
    byte[] data = CreateIndexData(new float[] { 10.0f, -5.0f, 20.0f });

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1, ValidationMode.Lenient);

    // Assert
    // Should skip the negative entry
    Assert.Equal(2, result.Count);
    Assert.Equal(1, result[0].MessageNumber);
    Assert.Equal(10, result[0].RecordOffset);
    Assert.Equal(2, result[1].MessageNumber);
    Assert.Equal(20, result[1].RecordOffset);
  }

  [Fact]
  public void Parse_WithRecordOffsetBeyondFileSize_InStrictMode_ThrowsException()
  {
    // Arrange
    long messagesDatSize = 10 * 128; // 10 records (1280 bytes)
    byte[] data = CreateIndexData(new float[] { 5.0f, 15.0f }); // Second offset is beyond file

    // Act & Assert
    QwkFormatException ex = Assert.Throws<QwkFormatException>(
      () => IndexFileParser.Parse(data, 1, ValidationMode.Strict, messagesDatSize));

    Assert.Contains("points beyond MESSAGES.DAT file", ex.Message);
  }

  [Fact]
  public void Parse_WithRecordOffsetBeyondFileSize_InLenientMode_SkipsInvalidEntry()
  {
    // Arrange
    long messagesDatSize = 10 * 128; // 10 records
    byte[] data = CreateIndexData(new float[] { 5.0f, 15.0f, 8.0f });

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1, ValidationMode.Lenient, messagesDatSize);

    // Assert
    // Should skip the out-of-bounds entry (15.0f)
    Assert.Equal(2, result.Count);
    Assert.Equal(1, result[0].MessageNumber);
    Assert.Equal(5, result[0].RecordOffset);
    Assert.Equal(2, result[1].MessageNumber);
    Assert.Equal(8, result[1].RecordOffset);
    Assert.False(result.IsValid); // Marked invalid due to skipped entry
  }

  [Fact]
  public void Parse_WithValidationAgainstFileSize_PreservesMetadata()
  {
    // Arrange
    long messagesDatSize = 1000 * 128;
    byte[] data = CreateIndexData(new float[] { 1.0f, 2.0f, 3.0f });

    // Act
    IndexFile result = IndexFileParser.Parse(data, 99, ValidationMode.Lenient, messagesDatSize);

    // Assert
    Assert.Equal(99, result.ConferenceNumber);
    Assert.Equal(messagesDatSize, result.ValidatedAgainstFileSize);
    Assert.True(result.IsValid);
  }

  [Fact]
  public void Parse_WithoutFileSize_DoesNotValidateBounds()
  {
    // Arrange
    byte[] data = CreateIndexData(new float[] { 1000.0f, 2000.0f }); // Large offsets

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1, ValidationMode.Strict, messagesDatFileSize: null);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Null(result.ValidatedAgainstFileSize);
  }

  [Fact]
  public void Parse_FromStream_ParsesCorrectly()
  {
    // Arrange
    byte[] data = CreateIndexData(new float[] { 10.0f, 20.0f, 30.0f });
    using (MemoryStream stream = new MemoryStream(data))
    {
      // Act
      IndexFile result = IndexFileParser.Parse(stream, 5);

      // Assert
      Assert.Equal(5, result.ConferenceNumber);
      Assert.Equal(3, result.Count);
    }
  }

  [Fact]
  public void Parse_FromStream_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => IndexFileParser.Parse((Stream)null!, 1));

    Assert.Equal("stream", ex.ParamName);
  }

  [Fact]
  public void Parse_FromNonSeekableStream_Works()
  {
    // Arrange
    byte[] data = CreateIndexData(new float[] { 5.0f, 15.0f });
    using (NonSeekableStream stream = new NonSeekableStream(data))
    {
      // Act
      IndexFile result = IndexFileParser.Parse(stream, 10);

      // Assert
      Assert.Equal(10, result.ConferenceNumber);
      Assert.Equal(2, result.Count);
    }
  }

  [Fact]
  public void ParseFile_WithValidFile_ParsesCorrectly()
  {
    // Arrange
    string tempFile = Path.GetTempFileName();
    try
    {
      byte[] data = CreateIndexData(new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f });
      File.WriteAllBytes(tempFile, data);

      // Act
      IndexFile result = IndexFileParser.ParseFile(tempFile, 7);

      // Assert
      Assert.Equal(7, result.ConferenceNumber);
      Assert.Equal(5, result.Count);
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Fact]
  public void ParseFile_WithNullPath_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => IndexFileParser.ParseFile(null!, 1));

    Assert.Equal("filePath", ex.ParamName);
  }

  [Fact]
  public void ParseFile_WithNonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    string nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".ndx");

    // Act & Assert
    FileNotFoundException ex = Assert.Throws<FileNotFoundException>(
      () => IndexFileParser.ParseFile(nonExistentPath, 1));

    Assert.Contains("Index file not found", ex.Message);
  }

  [Fact]
  public void Parse_PreservesRawMsbinBytes()
  {
    // Arrange
    byte[] originalMsbin = new byte[] { 0x12, 0x34, 0x56, 0x78 };
    byte[] data = originalMsbin;

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1);

    // Assert
    Assert.Single(result);
    ReadOnlyMemory<byte> preserved = result[0].RawMsbinBytes;
    Assert.Equal(4, preserved.Length);
    Assert.Equal(0x12, preserved.Span[0]);
    Assert.Equal(0x34, preserved.Span[1]);
    Assert.Equal(0x56, preserved.Span[2]);
    Assert.Equal(0x78, preserved.Span[3]);
  }

  [Fact]
  public void Parse_WithZeroRecordOffset_IsValid()
  {
    // Arrange
    byte[] data = CreateIndexData(new float[] { 0.0f, 1.0f, 2.0f });

    // Act
    IndexFile result = IndexFileParser.Parse(data, 1);

    // Assert
    Assert.Equal(3, result.Count);
    Assert.Equal(0, result[0].RecordOffset);
    Assert.Equal(1, result[1].RecordOffset);
    Assert.Equal(2, result[2].RecordOffset);
  }

  [Fact]
  public void Parse_WithLargeConferenceNumber_Works()
  {
    // Arrange
    byte[] data = CreateIndexData(new float[] { 10.0f });

    // Act
    IndexFile result = IndexFileParser.Parse(data, 65535);

    // Assert
    Assert.Equal(65535, result.ConferenceNumber);
    Assert.Single(result);
  }

  // Helper methods

  private static byte[] CreateIndexData(float[] recordOffsets)
  {
    byte[] result = new byte[recordOffsets.Length * 4];
    for (int i = 0; i < recordOffsets.Length; i++)
    {
      byte[] msbinBytes = MsbinConverter.FromDouble(recordOffsets[i]);
      Array.Copy(msbinBytes, 0, result, i * 4, 4);
    }
    return result;
  }

  // Helper class for non-seekable stream testing
  private sealed class NonSeekableStream : MemoryStream
  {
    public NonSeekableStream(byte[] buffer) : base(buffer) { }

    public override bool CanSeek => false;

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException("Stream is not seekable.");
    }
  }
}