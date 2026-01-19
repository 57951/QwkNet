using System;
using System.IO;
using System.Linq;
using System.Text;
using QwkNet.Archive;
using QwkNet.Archives.Tar;
using Xunit;

namespace QwkNet.Archives.Tests;

/// <summary>
/// Tests for <see cref="TarArchiveReader"/>.
/// </summary>
public sealed class TarArchiveReaderTests
{
  [Fact]
  public void Constructor_WithValidStream_Succeeds()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();

    // Act
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Assert
      Assert.NotNull(reader);
    }
  }

  [Fact]
  public void Constructor_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
      () => new TarArchiveReader(null!, leaveOpen: false));

    Assert.Equal("stream", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithNonReadableStream_ThrowsArgumentException()
  {
    // Arrange
    MemoryStream stream = new MemoryStream();
    stream.Dispose(); // Make stream non-readable

    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(
      () => new TarArchiveReader(stream, leaveOpen: false));

    Assert.Contains("readable", ex.Message);
    Assert.Equal("stream", ex.ParamName);
  }

  [Fact]
  public void Constructor_WithInvalidTarData_ThrowsInvalidDataException()
  {
    // Arrange - create stream with random bytes (not valid TAR)
    MemoryStream stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });

    // Act & Assert
    InvalidDataException ex = Assert.Throws<InvalidDataException>(
      () => new TarArchiveReader(stream, leaveOpen: false));

    Assert.Contains("valid TAR", ex.Message);
  }

  [Fact]
  public void ListFiles_ReturnsAllRegularFiles()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();

      // Assert
      Assert.Equal(3, files.Count);
      Assert.Contains("file1.txt", files);
      Assert.Contains("file2.dat", files);
      Assert.Contains("subdir/file3.txt", files);
    }
  }

  [Fact]
  public void FileExists_WithExistingFile_ReturnsTrue()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act & Assert
      Assert.True(reader.FileExists("file1.txt"));
      Assert.True(reader.FileExists("FILE1.TXT")); // Case-insensitive
      Assert.True(reader.FileExists("subdir/file3.txt"));
    }
  }

  [Fact]
  public void FileExists_WithNonExistingFile_ReturnsFalse()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act & Assert
      Assert.False(reader.FileExists("nonexistent.txt"));
    }
  }

  [Fact]
  public void FileExists_WithNullName_ThrowsArgumentNullException()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act & Assert
      ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
        () => reader.FileExists(null!));

      Assert.Equal("name", ex.ParamName);
    }
  }

  [Fact]
  public void OpenFile_WithExistingFile_ReturnsCorrectContent()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act
      using (Stream fileStream = reader.OpenFile("file1.txt"))
      {
        byte[] content = new byte[fileStream.Length];
        int bytesRead = fileStream.Read(content, 0, content.Length);

        // Assert
        Assert.Equal(content.Length, bytesRead);
        string text = System.Text.Encoding.ASCII.GetString(content);
        Assert.Equal("Content of file1", text);
      }
    }
  }

  [Fact]
  public void OpenFile_CaseInsensitive_ReturnsCorrectFile()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act
      using (Stream fileStream = reader.OpenFile("FILE1.TXT"))
      {
        byte[] content = new byte[fileStream.Length];
        fileStream.ReadExactly(content, 0, content.Length);

        // Assert
        string text = System.Text.Encoding.ASCII.GetString(content);
        Assert.Equal("Content of file1", text);
      }
    }
  }

  [Fact]
  public void OpenFile_WithNonExistingFile_ThrowsFileNotFoundException()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act & Assert
      FileNotFoundException ex = Assert.Throws<FileNotFoundException>(
        () => reader.OpenFile("nonexistent.txt"));

      Assert.Contains("nonexistent.txt", ex.Message);
    }
  }

  [Fact]
  public void OpenFile_WithNullName_ThrowsArgumentNullException()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act & Assert
      ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
        () => reader.OpenFile(null!));

      Assert.Equal("name", ex.ParamName);
    }
  }

  [Fact]
  public void OpenFile_MultipleTimes_ReturnsIndependentStreams()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act - open same file twice
      using (Stream stream1 = reader.OpenFile("file1.txt"))
      using (Stream stream2 = reader.OpenFile("file1.txt"))
      {
        // Assert - both streams are independent
        Assert.NotSame(stream1, stream2);

        byte[] content1 = new byte[stream1.Length];
        byte[] content2 = new byte[stream2.Length];

        stream1.ReadExactly(content1, 0, content1.Length);
        stream2.ReadExactly(content2, 0, content2.Length);

        Assert.Equal(content1, content2);
      }
    }
  }

  [Fact]
  public void Dispose_WithLeaveOpenFalse_DisposesStream()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false);

    // Act
    reader.Dispose();

    // Assert
    Assert.Throws<ObjectDisposedException>(() => stream.Position);
  }

  [Fact]
  public void Dispose_WithLeaveOpenTrue_LeavesStreamOpen()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: true);

    // Act
    reader.Dispose();

    // Assert - stream should still be usable
    long position = stream.Position;
    Assert.True(position >= 0);

    stream.Dispose();
  }

  [Fact]
  public void ListFiles_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false);
    reader.Dispose();

    // Act & Assert
    Assert.Throws<ObjectDisposedException>(() => reader.ListFiles());
  }

  [Fact]
  public void OpenFile_WithSubdirectoryPath_ReturnsCorrectContent()
  {
    // Arrange
    MemoryStream stream = CreateSimpleTarArchive();
    using (TarArchiveReader reader = new TarArchiveReader(stream, leaveOpen: false))
    {
      // Act
      using (Stream fileStream = reader.OpenFile("subdir/file3.txt"))
      {
        byte[] content = new byte[fileStream.Length];
        fileStream.ReadExactly(content, 0, content.Length);

        // Assert
        string text = System.Text.Encoding.ASCII.GetString(content);
        Assert.Equal("Content of file3", text);
      }
    }
  }

  /// <summary>
  /// Creates a simple TAR archive with three files for testing.
  /// </summary>
  private static MemoryStream CreateSimpleTarArchive()
  {
    MemoryStream output = new MemoryStream();

    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Add three test files
      writer.AddFile("file1.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Content of file1")));
      writer.AddFile("file2.dat", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Content of file2")));
      writer.AddFile("subdir/file3.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Content of file3")));

      writer.Save(output);
    }

    output.Position = 0;
    return output;
  }
}