using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using QwkNet.Archive.Zip;

namespace QwkNet.Tests.Archive;

/// <summary>
/// Tests for <see cref="ZipArchiveWriter"/>.
/// </summary>
public sealed class ZipArchiveWriterTests
{
  [Fact]
  public void Constructor_Succeeds()
  {
    // Act
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    {
      // Assert
      Assert.NotNull(writer);
    }
  }

  [Fact]
  public void AddFile_WithNullName_ThrowsArgumentNullException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream content = new MemoryStream())
    {
      // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
        () => writer.AddFile(null!, content));
#pragma warning restore CS8625

      Assert.Equal("name", exception.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithNullContent_ThrowsArgumentNullException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    {
      // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
        () => writer.AddFile("test.txt", null!));
#pragma warning restore CS8625

      Assert.Equal("content", exception.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithEmptyName_ThrowsArgumentException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream content = new MemoryStream())
    {
      // Act & Assert
      ArgumentException exception = Assert.Throws<ArgumentException>(
        () => writer.AddFile("", content));

      Assert.Equal("name", exception.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithWhitespaceName_ThrowsArgumentException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream content = new MemoryStream())
    {
      // Act & Assert
      ArgumentException exception = Assert.Throws<ArgumentException>(
        () => writer.AddFile("   ", content));

      Assert.Equal("name", exception.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithValidArguments_Succeeds()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("test")))
    {
      // Act & Assert
      writer.AddFile("test.txt", content);
    }
  }

  [Fact]
  public void AddFile_AfterSave_ThrowsInvalidOperationException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream output = new MemoryStream())
    using (MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("test")))
    {
      writer.Save(output);

      // Act & Assert
      InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
        () => writer.AddFile("test.txt", content));

      Assert.Contains("Save()", exception.Message);
    }
  }

  [Fact]
  public void Save_WithNullOutput_ThrowsArgumentNullException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    {
      // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
        () => writer.Save(null!));
#pragma warning restore CS8625

      Assert.Equal("output", exception.ParamName);
    }
  }

  [Fact]
  public void Save_WithNonWritableStream_ThrowsArgumentException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (Stream readOnlyStream = new ReadOnlyStream())
    {
      // Act & Assert
      ArgumentException exception = Assert.Throws<ArgumentException>(
        () => writer.Save(readOnlyStream));

      Assert.Equal("output", exception.ParamName);
      Assert.Contains("writable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void Save_WithEmptyArchive_CreatesValidZip()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream output = new MemoryStream())
    {
      // Act
      writer.Save(output);

      // Assert
      output.Position = 0;
      using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Read))
      {
        Assert.Empty(archive.Entries);
      }
    }
  }

  [Fact]
  public void Save_WithSingleFile_CreatesValidZip()
  {
    // Arrange
    string fileName = "CONTROL.DAT";
    string content = "Test BBS\nBBS001\n";

    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream contentStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(content)))
    using (MemoryStream output = new MemoryStream())
    {
      writer.AddFile(fileName, contentStream);

      // Act
      writer.Save(output);

      // Assert
      output.Position = 0;
      using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Read))
      {
        Assert.Single(archive.Entries);

        ZipArchiveEntry? entry = archive.GetEntry(fileName);
        Assert.NotNull(entry);

        using (Stream entryStream = entry.Open())
        using (StreamReader reader = new StreamReader(entryStream))
        {
          string actualContent = reader.ReadToEnd();
          Assert.Equal(content, actualContent);
        }
      }
    }
  }

  [Fact]
  public void Save_WithMultipleFiles_CreatesValidZip()
  {
    // Arrange
    string[] fileNames = { "CONTROL.DAT", "MESSAGES.DAT", "DOOR.ID" };
    string[] contents = { "Control", "Messages", "Door" };

    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream output = new MemoryStream())
    {
      for (int i = 0; i < fileNames.Length; i++)
      {
        using (MemoryStream contentStream = new MemoryStream(
          System.Text.Encoding.ASCII.GetBytes(contents[i])))
        {
          writer.AddFile(fileNames[i], contentStream);
        }
      }

      // Act
      writer.Save(output);

      // Assert
      output.Position = 0;
      using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Read))
      {
        Assert.Equal(fileNames.Length, archive.Entries.Count);

        for (int i = 0; i < fileNames.Length; i++)
        {
          ZipArchiveEntry? entry = archive.GetEntry(fileNames[i]);
          Assert.NotNull(entry);

          using (Stream entryStream = entry.Open())
          using (StreamReader reader = new StreamReader(entryStream))
          {
            string actualContent = reader.ReadToEnd();
            Assert.Equal(contents[i], actualContent);
          }
        }
      }
    }
  }

  [Fact]
  public void Save_CalledTwice_ThrowsInvalidOperationException()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream output1 = new MemoryStream())
    using (MemoryStream output2 = new MemoryStream())
    {
      writer.Save(output1);

      // Act & Assert
      InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
        () => writer.Save(output2));

      Assert.Contains("already been called", exception.Message);
    }
  }

  [Fact]
  public void Save_LeavesOutputStreamOpen()
  {
    // Arrange
    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream output = new MemoryStream())
    {
      writer.AddFile("test.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("test")));

      // Act
      writer.Save(output);

      // Assert - stream should still be usable
      Assert.True(output.CanWrite);
      Assert.True(output.CanSeek);

      // Verify we can read the ZIP
      output.Position = 0;
      using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Read, leaveOpen: true))
      {
        Assert.Single(archive.Entries);
      }

      // Stream should still be open
      Assert.True(output.CanWrite);
    }
  }

  [Fact]
  public void AddFile_WithDuplicateName_OverwritesPrevious()
  {
    // Arrange
    string fileName = "test.txt";
    string firstContent = "First";
    string secondContent = "Second (should win)";

    using (ZipArchiveWriter writer = new ZipArchiveWriter())
    using (MemoryStream output = new MemoryStream())
    {
      writer.AddFile(fileName, new MemoryStream(System.Text.Encoding.ASCII.GetBytes(firstContent)));
      writer.AddFile(fileName, new MemoryStream(System.Text.Encoding.ASCII.GetBytes(secondContent)));

      // Act
      writer.Save(output);

      // Assert
      output.Position = 0;
      using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Read))
      {
        ZipArchiveEntry? entry = archive.GetEntry(fileName);
        Assert.NotNull(entry);

        using (Stream entryStream = entry.Open())
        using (StreamReader reader = new StreamReader(entryStream))
        {
          string actualContent = reader.ReadToEnd();
          Assert.Equal(secondContent, actualContent);
        }
      }
    }
  }

  [Fact]
  public void Dispose_AfterSave_DoesNotThrow()
  {
    // Arrange
    ZipArchiveWriter writer = new ZipArchiveWriter();
    using (MemoryStream output = new MemoryStream())
    {
      writer.Save(output);

      // Act & Assert
      writer.Dispose();
      writer.Dispose(); // Multiple dispose should be safe
    }
  }

  [Fact]
  public void AddFile_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    ZipArchiveWriter writer = new ZipArchiveWriter();
    writer.Dispose();

    // Act & Assert
    using (MemoryStream content = new MemoryStream())
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
      Assert.Throws<ObjectDisposedException>(
        () => writer.AddFile("test.txt", content));
#pragma warning restore CS8625
    }
  }

  /// <summary>
  /// Helper stream that is read-only.
  /// </summary>
  private sealed class ReadOnlyStream : Stream
  {
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => 0;
    public override long Position { get; set; }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => 0;
    public override long Seek(long offset, SeekOrigin origin) =>
      throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException();
  }
}