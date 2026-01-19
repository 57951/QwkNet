using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using QwkNet.Archive;
using QwkNet.Archive.Zip;

namespace QwkNet.Tests.Archive;

/// <summary>
/// Tests for <see cref="ArchiveFactory"/>.
/// </summary>
public sealed class ArchiveFactoryTests
{
  [Fact]
  public void OpenArchive_FromPath_WithNullPath_ThrowsArgumentNullException()
  {
    // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => ArchiveFactory.OpenArchive((string)null!));
#pragma warning restore CS8625

    Assert.Equal("path", exception.ParamName);
  }

  [Fact]
  public void OpenArchive_FromPath_WithNonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

    // Act & Assert
    FileNotFoundException exception = Assert.Throws<FileNotFoundException>(
      () => ArchiveFactory.OpenArchive(nonExistentPath));

    Assert.Equal(nonExistentPath, exception.FileName);
  }

  [Fact]
  public void OpenArchive_FromPath_WithZipFile_ReturnsZipArchiveReader()
  {
    // Arrange
    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

    try
    {
      CreateTestZipFile(tempPath);

      // Act
      using (IArchiveReader reader = ArchiveFactory.OpenArchive(tempPath))
      {
        // Assert
        Assert.IsType<ZipArchiveReader>(reader);
        Assert.NotEmpty(reader.ListFiles());
      }
    }
    finally
    {
      if (File.Exists(tempPath))
      {
        File.Delete(tempPath);
      }
    }
  }

  [Fact]
  public void OpenArchive_FromPath_WithUnsupportedFormat_ThrowsNotSupportedException()
  {
    // Arrange
    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

    try
    {
      File.WriteAllText(tempPath, "Not an archive");

      // Act & Assert
      NotSupportedException exception = Assert.Throws<NotSupportedException>(
        () => ArchiveFactory.OpenArchive(tempPath));

      Assert.Contains("format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      if (File.Exists(tempPath))
      {
        File.Delete(tempPath);
      }
    }
  }

  [Fact]
  public void OpenArchive_FromStream_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => ArchiveFactory.OpenArchive((Stream)null!, leaveOpen: false));
#pragma warning restore CS8625

    Assert.Equal("stream", exception.ParamName);
  }

  [Fact]
  public void OpenArchive_FromStream_WithZipStream_ReturnsZipArchiveReader()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZipStream())
    {
      // Act
      using (IArchiveReader reader = ArchiveFactory.OpenArchive(stream, leaveOpen: true))
      {
        // Assert
        Assert.IsType<ZipArchiveReader>(reader);
        Assert.NotEmpty(reader.ListFiles());
      }

      // Stream should still be open
      Assert.True(stream.CanRead);
    }
  }

  [Fact]
  public void OpenArchive_FromStream_WithLeaveOpenFalse_ClosesStream()
  {
    // Arrange
    MemoryStream stream = CreateTestZipStream();

    // Act
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(stream, leaveOpen: false))
    {
      Assert.NotEmpty(reader.ListFiles());
    }

    // Assert - stream should be closed after reader disposal
    Assert.Throws<ObjectDisposedException>(() => stream.Position);
  }

  [Fact]
  public void OpenArchive_FromStream_WithUnsupportedFormat_ThrowsNotSupportedException()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Not an archive")))
    {
      // Act & Assert
      NotSupportedException exception = Assert.Throws<NotSupportedException>(
        () => ArchiveFactory.OpenArchive(stream, leaveOpen: true));

      Assert.Contains("format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void OpenArchive_WithExplicitFormat_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => ArchiveFactory.OpenArchive(null!, ArchiveFormat.Zip, leaveOpen: false));
#pragma warning restore CS8625

    Assert.Equal("stream", exception.ParamName);
  }

  [Fact]
  public void OpenArchive_WithExplicitFormat_WithZipFormat_ReturnsZipArchiveReader()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZipStream())
    {
      // Act
      using (IArchiveReader reader = ArchiveFactory.OpenArchive(
        stream,
        ArchiveFormat.Zip,
        leaveOpen: true))
      {
        // Assert
        Assert.IsType<ZipArchiveReader>(reader);
        Assert.NotEmpty(reader.ListFiles());
      }
    }
  }

  [Fact]
  public void OpenArchive_WithExplicitFormat_WithUnknownFormat_ThrowsNotSupportedException()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      // Act & Assert
      NotSupportedException exception = Assert.Throws<NotSupportedException>(
        () => ArchiveFactory.OpenArchive(stream, ArchiveFormat.Unknown, leaveOpen: true));

      Assert.Contains("unknown", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void OpenArchive_WithExplicitFormat_WithInvalidZipData_ThrowsInvalidDataException()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Not a ZIP")))
    {
      // Act & Assert
      Assert.Throws<InvalidDataException>(
        () => ArchiveFactory.OpenArchive(stream, ArchiveFormat.Zip, leaveOpen: true));
    }
  }

  [Fact]
  public void CreateWriter_WithZipFormat_ReturnsZipArchiveWriter()
  {
    // Act
    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormat.Zip))
    {
      // Assert
      Assert.IsType<ZipArchiveWriter>(writer);
    }
  }

  [Fact]
  public void CreateWriter_WithUnknownFormat_ThrowsNotSupportedException()
  {
    // Act & Assert
    NotSupportedException exception = Assert.Throws<NotSupportedException>(
      () => ArchiveFactory.CreateWriter(ArchiveFormat.Unknown));

    Assert.Contains("unknown", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void CreateWriter_ProducesValidArchive()
  {
    // Arrange
    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormat.Zip))
    using (MemoryStream contentStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("test")))
    using (MemoryStream outputStream = new MemoryStream())
    {
      // Act
      writer.AddFile("test.txt", contentStream);
      writer.Save(outputStream);

      // Assert
      outputStream.Position = 0;
      using (IArchiveReader reader = ArchiveFactory.OpenArchive(
        outputStream,
        ArchiveFormat.Zip,
        leaveOpen: true))
      {
        Assert.Single(reader.ListFiles());
        Assert.True(reader.FileExists("test.txt"));

        using (Stream fileStream = reader.OpenFile("test.txt"))
        using (StreamReader streamReader = new StreamReader(fileStream))
        {
          string content = streamReader.ReadToEnd();
          Assert.Equal("test", content);
        }
      }
    }
  }

  [Fact]
  public void RoundTrip_WriteAndRead_PreservesContent()
  {
    // Arrange
    string[] fileNames = { "CONTROL.DAT", "MESSAGES.DAT", "DOOR.ID" };
    string[] contents = { "Control content", "Message content", "Door content" };

    using (MemoryStream archiveStream = new MemoryStream())
    {
      // Write
      using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormat.Zip))
      {
        for (int i = 0; i < fileNames.Length; i++)
        {
          using (MemoryStream contentStream = new MemoryStream(
            System.Text.Encoding.ASCII.GetBytes(contents[i])))
          {
            writer.AddFile(fileNames[i], contentStream);
          }
        }

        writer.Save(archiveStream);
      }

      // Read
      archiveStream.Position = 0;
      using (IArchiveReader reader = ArchiveFactory.OpenArchive(
        archiveStream,
        ArchiveFormat.Zip,
        leaveOpen: true))
      {
        IReadOnlyList<string> files = reader.ListFiles();
        Assert.Equal(fileNames.Length, files.Count);

        for (int i = 0; i < fileNames.Length; i++)
        {
          Assert.True(reader.FileExists(fileNames[i]));

          using (Stream fileStream = reader.OpenFile(fileNames[i]))
          using (StreamReader streamReader = new StreamReader(fileStream))
          {
            string actualContent = streamReader.ReadToEnd();
            Assert.Equal(contents[i], actualContent);
          }
        }
      }
    }
  }

  /// <summary>
  /// Creates a test ZIP file at the specified path.
  /// </summary>
  private static void CreateTestZipFile(string path)
  {
    using (FileStream fileStream = File.Create(path))
    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
    {
      ZipArchiveEntry entry = archive.CreateEntry("test.txt");
      using (Stream entryStream = entry.Open())
      {
        byte[] data = System.Text.Encoding.ASCII.GetBytes("test content");
        entryStream.Write(data, 0, data.Length);
      }
    }
  }

  /// <summary>
  /// Creates a test ZIP archive in memory.
  /// </summary>
  private static MemoryStream CreateTestZipStream()
  {
    MemoryStream stream = new MemoryStream();

    using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
      ZipArchiveEntry entry = archive.CreateEntry("test.txt");
      using (Stream entryStream = entry.Open())
      {
        byte[] data = System.Text.Encoding.ASCII.GetBytes("test content");
        entryStream.Write(data, 0, data.Length);
      }
    }

    stream.Position = 0;
    return stream;
  }
}