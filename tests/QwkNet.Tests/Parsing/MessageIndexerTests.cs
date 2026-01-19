using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QwkNet.Core;
using QwkNet.Models.Indexing;
using QwkNet.Parsing;
using Xunit;

namespace QwkNet.Tests.Parsing;

public sealed class MessageIndexerTests
{
  private const int QwkRecordSize = 128;

  [Fact]
  public void GenerateIndexes_WithEmptyMessagesFile_ReturnsEmptyDictionary()
  {
    // Arrange
    using (MemoryStream stream = CreateMessagesStream(Array.Empty<(int conferenceNumber, string from)>()))
    {
      // Act
      Dictionary<int, IndexFile> result = MessageIndexer.GenerateIndexes(stream);

      // Assert
      Assert.Empty(result);
    }
  }

  [Fact]
  public void GenerateIndexes_WithSingleConference_GeneratesSingleIndex()
  {
    // Arrange
    (int, string)[] messages = new[]
    {
      (1, "Alice"),
      (1, "Bob"),
      (1, "Charlie")
    };

    using (MemoryStream stream = CreateMessagesStream(messages))
    {
      // Act
      Dictionary<int, IndexFile> result = MessageIndexer.GenerateIndexes(stream);

      // Assert
      Assert.Single(result);
      Assert.True(result.ContainsKey(1));

      IndexFile indexFile = result[1];
      Assert.Equal(1, indexFile.ConferenceNumber);
      Assert.Equal(3, indexFile.Count);
      Assert.True(indexFile.IsValid);

      // Verify message numbers are sequential
      Assert.Equal(1, indexFile[0].MessageNumber);
      Assert.Equal(2, indexFile[1].MessageNumber);
      Assert.Equal(3, indexFile[2].MessageNumber);

      // Verify record offsets
      Assert.Equal(1, indexFile[0].RecordOffset); // First message after header
      Assert.Equal(2, indexFile[1].RecordOffset);
      Assert.Equal(3, indexFile[2].RecordOffset);
    }
  }

  [Fact]
  public void GenerateIndexes_WithMultipleConferences_GeneratesMultipleIndexes()
  {
    // Arrange
    (int, string)[] messages = new[]
    {
      (0, "System"),
      (1, "Alice"),
      (1, "Bob"),
      (2, "Charlie"),
      (2, "David"),
      (2, "Eve")
    };

    using (MemoryStream stream = CreateMessagesStream(messages))
    {
      // Act
      Dictionary<int, IndexFile> result = MessageIndexer.GenerateIndexes(stream);

      // Assert
      Assert.Equal(3, result.Count);

      // Conference 0
      Assert.True(result.ContainsKey(0));
      Assert.Equal(1, (int)result[0].Count);
      Assert.Equal(1, result[0][0].MessageNumber);

      // Conference 1
      Assert.True(result.ContainsKey(1));
      Assert.Equal(2, result[1].Count);
      Assert.Equal(1, result[1][0].MessageNumber);
      Assert.Equal(2, result[1][1].MessageNumber);

      // Conference 2
      Assert.True(result.ContainsKey(2));
      Assert.Equal(3, result[2].Count);
      Assert.Equal(1, result[2][0].MessageNumber);
      Assert.Equal(2, result[2][1].MessageNumber);
      Assert.Equal(3, result[2][2].MessageNumber);
    }
  }

  [Fact]
  public void GenerateIndexes_WithInterleavedConferences_GroupsCorrectly()
  {
    // Arrange - Messages from different conferences interleaved
    (int, string)[] messages = new[]
    {
      (1, "Alice"),
      (2, "Bob"),
      (1, "Charlie"),
      (2, "David"),
      (1, "Eve")
    };

    using (MemoryStream stream = CreateMessagesStream(messages))
    {
      // Act
      Dictionary<int, IndexFile> result = MessageIndexer.GenerateIndexes(stream);

      // Assert
      Assert.Equal(2, result.Count);

      // Conference 1: Alice (record 1), Charlie (record 3), Eve (record 5)
      IndexFile conf1 = result[1];
      Assert.Equal(3, conf1.Count);
      Assert.Equal(1, conf1[0].RecordOffset);
      Assert.Equal(3, conf1[1].RecordOffset);
      Assert.Equal(5, conf1[2].RecordOffset);

      // Conference 2: Bob (record 2), David (record 4)
      IndexFile conf2 = result[2];
      Assert.Equal(2, conf2.Count);
      Assert.Equal(2, conf2[0].RecordOffset);
      Assert.Equal(4, conf2[1].RecordOffset);
    }
  }

  [Fact]
  public void GenerateIndexes_PreservesFileSize()
  {
    // Arrange
    (int, string)[] messages = new[]
    {
      (1, "Alice"),
      (1, "Bob")
    };

    using (MemoryStream stream = CreateMessagesStream(messages))
    {
      long expectedSize = stream.Length;

      // Act
      Dictionary<int, IndexFile> result = MessageIndexer.GenerateIndexes(stream);

      // Assert
      IndexFile indexFile = result[1];
      Assert.Equal(expectedSize, indexFile.ValidatedAgainstFileSize);
    }
  }

  [Fact]
  public void GenerateIndexes_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => MessageIndexer.GenerateIndexes(null!));

    Assert.Equal("messagesDatStream", ex.ParamName);
  }

  [Fact]
  public void GenerateIndexes_WithNonReadableStream_ThrowsInvalidOperationException()
  {
    // Arrange
    using (NonReadableStream stream = new NonReadableStream())
    {
      // Act & Assert
      InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
        () => MessageIndexer.GenerateIndexes(stream));

      Assert.Contains("must be readable", ex.Message);
    }
  }

  [Fact]
  public void GenerateIndexes_WithNonSeekableStream_ThrowsInvalidOperationException()
  {
    // Arrange
    using (NonSeekableStream stream = new NonSeekableStream(new byte[256]))
    {
      // Act & Assert
      InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
        () => MessageIndexer.GenerateIndexes(stream));

      Assert.Contains("must be seekable", ex.Message);
    }
  }

  [Fact]
  public void GenerateIndex_ForSpecificConference_ReturnsCorrectIndex()
  {
    // Arrange
    (int, string)[] messages = new[]
    {
      (1, "Alice"),
      (2, "Bob"),
      (1, "Charlie"),
      (2, "David")
    };

    using (MemoryStream stream = CreateMessagesStream(messages))
    {
      // Act
      IndexFile result = MessageIndexer.GenerateIndex(stream, 1);

      // Assert
      Assert.Equal(1, result.ConferenceNumber);
      Assert.Equal(2, result.Count);
      Assert.Equal(1, result[0].MessageNumber);
      Assert.Equal(2, result[1].MessageNumber);
    }
  }

  [Fact]
  public void GenerateIndex_ForNonExistentConference_ReturnsEmptyIndex()
  {
    // Arrange
    (int, string)[] messages = new[]
    {
      (1, "Alice"),
      (1, "Bob")
    };

    using (MemoryStream stream = CreateMessagesStream(messages))
    {
      // Act
      IndexFile result = MessageIndexer.GenerateIndex(stream, 999);

      // Assert
      Assert.Equal(999, result.ConferenceNumber);
      Assert.Empty(result);
      Assert.True(result.IsEmpty);
      Assert.True(result.IsValid);
    }
  }

  [Fact]
  public void GenerateIndex_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => MessageIndexer.GenerateIndex(null!, 1));

    Assert.Equal("messagesDatStream", ex.ParamName);
  }

  [Fact]
  public void WriteIndex_ToStream_WritesCorrectData()
  {
    // Arrange
    List<IndexEntry> entries = new List<IndexEntry>
    {
      new IndexEntry(1, 10, MsbinConverter.FromDouble(10.0)),
      new IndexEntry(2, 20, MsbinConverter.FromDouble(20.0)),
      new IndexEntry(3, 30, MsbinConverter.FromDouble(30.0))
    };
    IndexFile indexFile = new IndexFile(5, entries);

    using (MemoryStream outputStream = new MemoryStream())
    {
      // Act
      MessageIndexer.WriteIndex(indexFile, outputStream);

      // Assert
      byte[] written = outputStream.ToArray();
      Assert.Equal(12, written.Length); // 3 entries × 4 bytes

      // Verify each entry
      double offset1 = MsbinConverter.ToDouble(written.AsSpan(0, 4));
      double offset2 = MsbinConverter.ToDouble(written.AsSpan(4, 4));
      double offset3 = MsbinConverter.ToDouble(written.AsSpan(8, 4));

      Assert.Equal(10.0, offset1, precision: 2);
      Assert.Equal(20.0, offset2, precision: 2);
      Assert.Equal(30.0, offset3, precision: 2);
    }
  }

  [Fact]
  public void WriteIndex_WithEmptyIndex_WritesNothing()
  {
    // Arrange
    IndexFile indexFile = new IndexFile(1, Array.Empty<IndexEntry>());

    using (MemoryStream outputStream = new MemoryStream())
    {
      // Act
      MessageIndexer.WriteIndex(indexFile, outputStream);

      // Assert
      Assert.Empty(outputStream.ToArray());
    }
  }

  [Fact]
  public void WriteIndex_WithNullIndexFile_ThrowsArgumentNullException()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      // Act & Assert
      ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
        () => MessageIndexer.WriteIndex(null!, stream));

      Assert.Equal("indexFile", ex.ParamName);
    }
  }

  [Fact]
  public void WriteIndex_WithNullStream_ThrowsArgumentNullException()
  {
    // Arrange
    IndexFile indexFile = new IndexFile(1, Array.Empty<IndexEntry>());

    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => MessageIndexer.WriteIndex(indexFile, null!));

    Assert.Equal("outputStream", ex.ParamName);
  }

  [Fact]
  public void WriteIndex_WithNonWritableStream_ThrowsInvalidOperationException()
  {
    // Arrange
    IndexFile indexFile = new IndexFile(1, Array.Empty<IndexEntry>());
    using (NonWritableStream stream = new NonWritableStream())
    {
      // Act & Assert
      InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
        () => MessageIndexer.WriteIndex(indexFile, stream));

      Assert.Contains("must be writable", ex.Message);
    }
  }

  [Fact]
  public void WriteIndexFile_CreatesFile()
  {
    // Arrange
    List<IndexEntry> entries = new List<IndexEntry>
    {
      new IndexEntry(1, 5, MsbinConverter.FromDouble(5.0))
    };
    IndexFile indexFile = new IndexFile(42, entries);

    string tempFile = Path.GetTempFileName();
    try
    {
      // Act
      MessageIndexer.WriteIndexFile(indexFile, tempFile);

      // Assert
      Assert.True(File.Exists(tempFile));
      byte[] written = File.ReadAllBytes(tempFile);
      Assert.Equal(4, written.Length);

      double offset = MsbinConverter.ToDouble(written);
      Assert.Equal(5.0, offset, precision: 2);
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Fact]
  public void WriteIndexFile_WithNullIndexFile_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => MessageIndexer.WriteIndexFile(null!, "test.ndx"));

    Assert.Equal("indexFile", ex.ParamName);
  }

  [Fact]
  public void WriteIndexFile_WithNullPath_ThrowsArgumentNullException()
  {
    // Arrange
    IndexFile indexFile = new IndexFile(1, Array.Empty<IndexEntry>());

    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => MessageIndexer.WriteIndexFile(indexFile, null!));

    Assert.Equal("filePath", ex.ParamName);
  }

  [Fact]
  public void RoundTrip_GenerateAndParse_ProducesIdenticalResults()
  {
    // Arrange
    (int, string)[] messages = new[]
    {
      (5, "Alice"),
      (5, "Bob"),
      (5, "Charlie")
    };

    using (MemoryStream messagesStream = CreateMessagesStream(messages))
    {
      // Act - Generate index
      IndexFile generated = MessageIndexer.GenerateIndex(messagesStream, 5);

      // Write to stream
      using (MemoryStream indexStream = new MemoryStream())
      {
        MessageIndexer.WriteIndex(generated, indexStream);

        // Parse back
        IndexFile parsed = IndexFileParser.Parse(
          indexStream.ToArray(),
          5,
          messagesDatFileSize: messagesStream.Length);

        // Assert
        Assert.Equal(generated.ConferenceNumber, parsed.ConferenceNumber);
        Assert.Equal(generated.Count, parsed.Count);

        for (int i = 0; i < generated.Count; i++)
        {
          Assert.Equal(generated[i].MessageNumber, parsed[i].MessageNumber);
          Assert.Equal(generated[i].RecordOffset, parsed[i].RecordOffset);
        }
      }
    }
  }

  // Helper methods

  private static MemoryStream CreateMessagesStream((int conferenceNumber, string from)[] messages)
  {
    MemoryStream stream = new MemoryStream();

    // Write MESSAGES.DAT header record
    byte[] headerRecord = new byte[QwkRecordSize];
    stream.Write(headerRecord, 0, QwkRecordSize);

    // Write each message header
    foreach ((int conferenceNumber, string from) in messages)
    {
      byte[] messageRecord = CreateMessageRecord(conferenceNumber, from);
      stream.Write(messageRecord, 0, QwkRecordSize);
    }

    stream.Position = 0;
    return stream;
  }

  private static byte[] CreateMessageRecord(int conferenceNumber, string from)
  {
    byte[] record = new byte[QwkRecordSize];

    // Status byte (position 0)
    record[0] = (byte)' '; // Public, unread

    // Message number (positions 1-7) - ASCII, space-padded
    byte[] msgNum = System.Text.Encoding.ASCII.GetBytes("1".PadLeft(7));
    Array.Copy(msgNum, 0, record, 1, 7);

    // Date (positions 8-15) - MM-DD-YY format
    byte[] date = System.Text.Encoding.ASCII.GetBytes("01-01-25");
    Array.Copy(date, 0, record, 8, 8);

    // Time (positions 16-20) - HH:MM format
    byte[] time = System.Text.Encoding.ASCII.GetBytes("12:00");
    Array.Copy(time, 0, record, 16, 5);

    // To field (positions 21-45) - 25 characters, space-padded, uppercase
    byte[] toBytes = System.Text.Encoding.ASCII.GetBytes("ALL".PadRight(25));
    Array.Copy(toBytes, 0, record, 21, 25);

    // From field (positions 46-70) - 25 characters, space-padded, uppercase
    byte[] fromBytes = System.Text.Encoding.ASCII.GetBytes(from.ToUpper().PadRight(25).Substring(0, 25));
    Array.Copy(fromBytes, 0, record, 46, 25);

    // Subject (positions 71-95) - 25 characters, mixed case
    byte[] subject = System.Text.Encoding.ASCII.GetBytes("Test Message".PadRight(25).Substring(0, 25));
    Array.Copy(subject, 0, record, 71, 25);

    // Password (positions 96-107) - 12 characters, rarely used
    // Leave as spaces

    // Reference number (positions 108-115) - 8 characters, ASCII
    byte[] refNum = System.Text.Encoding.ASCII.GetBytes("0".PadLeft(8));
    Array.Copy(refNum, 0, record, 108, 8);

    // Number of 128-byte blocks (positions 116-121) - 6 characters, ASCII
    byte[] blocks = System.Text.Encoding.ASCII.GetBytes("1".PadLeft(6));
    Array.Copy(blocks, 0, record, 116, 6);

    // Active flag (position 122) - 0xE1 or 0xE2
    record[122] = 0xE1;

    // Conference number (positions 123-124) - 2 bytes, little-endian (UNSIGNED)
    record[123] = (byte)(conferenceNumber & 0xFF);
    record[124] = (byte)((conferenceNumber >> 8) & 0xFF);

    // Logical record number (positions 125-126) - 2 bytes
    record[125] = 0x01;
    record[126] = 0x00;

    // Tag line indicator (position 127) - space or asterisk
    record[127] = (byte)' ';

    return record;
  }

  // Helper classes for stream testing

  private sealed class NonReadableStream : MemoryStream
  {
    public override bool CanRead => false;
  }

  private sealed class NonSeekableStream : MemoryStream
  {
    public NonSeekableStream(byte[] buffer) : base(buffer) { }

    public override bool CanSeek => false;

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException("Stream is not seekable.");
    }
  }

  private sealed class NonWritableStream : MemoryStream
  {
    public override bool CanWrite => false;
  }
}