using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using QwkNet.Archive.Zip;

namespace QwkNet.Tests.Archive;

/// <summary>
/// Tests for <see cref="ZipArchiveReader"/>.
/// </summary>
public sealed class ZipArchiveReaderTests : IDisposable
{
  private readonly MemoryStream _testStream;

  public ZipArchiveReaderTests()
  {
    _testStream = new MemoryStream();
  }

  public void Dispose()
  {
    _testStream?.Dispose();
  }

  [Fact]
  public void Constructor_WithNullStream_ThrowsArgumentNullException()
  {
    // Arrange
#pragma warning disable CS8600, CS8604 // Converting null literal or possible null value to non-nullable type
    Stream nullStream = null!;
#pragma warning restore CS8600, CS8604

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new ZipArchiveReader(nullStream));

    Assert.Equal("stream", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNonReadableStream_ThrowsArgumentException()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      // Force stream to be non-readable by wrapping it
      Stream nonReadableStream = new NonReadableStream(stream);

      // Act & Assert
      ArgumentException exception = Assert.Throws<ArgumentException>(
        () => new ZipArchiveReader(nonReadableStream));

      Assert.Equal("stream", exception.ParamName);
      Assert.Contains("readable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void Constructor_WithNonSeekableStream_ThrowsArgumentException()
  {
    // Arrange
    Stream nonSeekableStream = new NonSeekableStream();

    // Act & Assert
    ArgumentException exception = Assert.Throws<ArgumentException>(
      () => new ZipArchiveReader(nonSeekableStream));

    Assert.Equal("stream", exception.ParamName);
    Assert.Contains("seekable", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Constructor_WithInvalidZipData_ThrowsInvalidDataException()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Not a ZIP file")))
    {
      // Act & Assert
      Assert.Throws<InvalidDataException>(() => new ZipArchiveReader(stream));
    }
  }

  [Fact]
  public void Constructor_WithValidZip_Succeeds()
  {
    // Arrange
    MemoryStream zipStream = CreateTestZip();

    // Act
    using (ZipArchiveReader reader = new ZipArchiveReader(zipStream))
    {
      // Assert
      Assert.NotNull(reader);
    }
  }

  [Fact]
  public void ListFiles_WithEmptyZip_ReturnsEmptyList()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
      {
        // Create empty ZIP
      }

      stream.Position = 0;

      using (ZipArchiveReader reader = new ZipArchiveReader(stream, leaveOpen: true))
      {
        // Act
        IReadOnlyList<string> files = reader.ListFiles();

        // Assert
        Assert.Empty(files);
      }
    }
  }

  [Fact]
  public void ListFiles_WithMultipleFiles_ReturnsAllFiles()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("FILE1.TXT", "FILE2.DAT", "MESSAGES.DAT"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act
      IReadOnlyList<string> files = reader.ListFiles();

      // Assert
      Assert.Equal(3, files.Count);
      Assert.Contains("FILE1.TXT", files);
      Assert.Contains("FILE2.DAT", files);
      Assert.Contains("MESSAGES.DAT", files);
    }
  }

  [Fact]
  public void ListFiles_ExcludesDirectoryEntries()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
      {
        // Add directory entry
        archive.CreateEntry("subdir/");

        // Add file entry
        ZipArchiveEntry fileEntry = archive.CreateEntry("subdir/file.txt");
        using (Stream entryStream = fileEntry.Open())
        {
          byte[] data = System.Text.Encoding.ASCII.GetBytes("test");
          entryStream.Write(data, 0, data.Length);
        }
      }

      stream.Position = 0;

      using (ZipArchiveReader reader = new ZipArchiveReader(stream))
      {
        // Act
        IReadOnlyList<string> files = reader.ListFiles();

        // Assert
        Assert.Single(files);
        Assert.Equal("subdir/file.txt", files[0]);
      }
    }
  }

  [Fact]
  public void OpenFile_WithNullName_ThrowsArgumentNullException()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("test.txt"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
        () => reader.OpenFile(null!));
#pragma warning restore CS8625

      Assert.Equal("name", exception.ParamName);
    }
  }

  [Fact]
  public void OpenFile_WithNonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("test.txt"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act & Assert
      FileNotFoundException exception = Assert.Throws<FileNotFoundException>(
        () => reader.OpenFile("nonexistent.txt"));

      Assert.Equal("nonexistent.txt", exception.FileName);
    }
  }

  [Fact]
  public void OpenFile_WithExactMatch_ReturnsStream()
  {
    // Arrange
    string fileName = "MESSAGES.DAT";
    string content = "Test message content";

    using (MemoryStream stream = CreateTestZip(fileName, content))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act
      using (Stream fileStream = reader.OpenFile(fileName))
      using (StreamReader streamReader = new StreamReader(fileStream))
      {
        string actualContent = streamReader.ReadToEnd();

        // Assert
        Assert.Equal(content, actualContent);
      }
    }
  }

  [Fact]
  public void OpenFile_WithCaseInsensitiveMatch_ReturnsStream()
  {
    // Arrange
    string fileNameInZip = "MESSAGES.DAT";
    string fileNameToOpen = "messages.dat";
    string content = "Test content";

    using (MemoryStream stream = CreateTestZip(fileNameInZip, content))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act
      using (Stream fileStream = reader.OpenFile(fileNameToOpen))
      using (StreamReader streamReader = new StreamReader(fileStream))
      {
        string actualContent = streamReader.ReadToEnd();

        // Assert
        Assert.Equal(content, actualContent);
      }
    }
  }

  [Fact]
  public void FileExists_WithNullName_ThrowsArgumentNullException()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("test.txt"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
        () => reader.FileExists(null!));
#pragma warning restore CS8625

      Assert.Equal("name", exception.ParamName);
    }
  }

  [Fact]
  public void FileExists_WithExistingFile_ReturnsTrue()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("CONTROL.DAT"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act
      bool exists = reader.FileExists("CONTROL.DAT");

      // Assert
      Assert.True(exists);
    }
  }

  [Fact]
  public void FileExists_WithNonExistentFile_ReturnsFalse()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("CONTROL.DAT"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act
      bool exists = reader.FileExists("NONEXISTENT.DAT");

      // Assert
      Assert.False(exists);
    }
  }

  [Fact]
  public void FileExists_WithCaseInsensitiveMatch_ReturnsTrue()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("CONTROL.DAT"))
    using (ZipArchiveReader reader = new ZipArchiveReader(stream))
    {
      // Act
      bool exists = reader.FileExists("control.dat");

      // Assert
      Assert.True(exists);
    }
  }

  [Fact]
  public void Dispose_CalledMultipleTimes_DoesNotThrow()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("test.txt"))
    {
      ZipArchiveReader reader = new ZipArchiveReader(stream, leaveOpen: true);

      // Act & Assert
      reader.Dispose();
      reader.Dispose(); // Should not throw
    }
  }

  [Fact]
  public void ListFiles_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZip("test.txt"))
    {
      ZipArchiveReader reader = new ZipArchiveReader(stream, leaveOpen: true);
      reader.Dispose();

      // Act & Assert
      Assert.Throws<ObjectDisposedException>(() => reader.ListFiles());
    }
  }

  /// <summary>
  /// Creates a test ZIP archive in memory with the specified file names.
  /// </summary>
  private static MemoryStream CreateTestZip(params string[] fileNames)
  {
    MemoryStream stream = new MemoryStream();

    using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (string fileName in fileNames)
      {
        ZipArchiveEntry entry = archive.CreateEntry(fileName);
        using (Stream entryStream = entry.Open())
        {
          byte[] data = System.Text.Encoding.ASCII.GetBytes($"Content of {fileName}");
          entryStream.Write(data, 0, data.Length);
        }
      }
    }

    stream.Position = 0;
    return stream;
  }

  /// <summary>
  /// Creates a test ZIP archive with a single file containing specific content.
  /// </summary>
  private static MemoryStream CreateTestZip(string fileName, string content)
  {
    MemoryStream stream = new MemoryStream();

    using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
      ZipArchiveEntry entry = archive.CreateEntry(fileName);
      using (Stream entryStream = entry.Open())
      {
        byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
        entryStream.Write(data, 0, data.Length);
      }
    }

    stream.Position = 0;
    return stream;
  }

  /// <summary>
  /// Helper stream that is not readable.
  /// </summary>
  private sealed class NonReadableStream : Stream
  {
    private readonly Stream _inner;

    public NonReadableStream(Stream inner) => _inner = inner;
    public override bool CanRead => false;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position
    {
      get => _inner.Position;
      set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) =>
      _inner.Write(buffer, offset, count);
  }

  /// <summary>
  /// Helper stream that is not seekable.
  /// </summary>
  private sealed class NonSeekableStream : Stream
  {
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => 0;
    public override long Seek(long offset, SeekOrigin origin) =>
      throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException();
  }
}