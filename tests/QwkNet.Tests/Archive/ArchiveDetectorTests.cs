using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;
using QwkNet.Archive;

namespace QwkNet.Tests.Archive;

/// <summary>
/// Tests for <see cref="ArchiveDetector"/>.
/// </summary>
public sealed class ArchiveDetectorTests
{
  [Fact]
  public void DetectFormat_FromPath_WithNullPath_ThrowsArgumentNullException()
  {
    // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => ArchiveDetector.DetectFormat((string)null!));
#pragma warning restore CS8625

    Assert.Equal("path", exception.ParamName);
  }

  [Fact]
  public void DetectFormat_FromPath_WithNonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

    // Act & Assert
    FileNotFoundException exception = Assert.Throws<FileNotFoundException>(
      () => ArchiveDetector.DetectFormat(nonExistentPath));

    Assert.Equal(nonExistentPath, exception.FileName);
  }

  [Fact]
  public void DetectFormat_FromPath_WithZipFile_ReturnsZip()
  {
    // Arrange
    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

    try
    {
      CreateTestZipFile(tempPath);

      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(tempPath);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
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
  public void DetectFormat_FromStream_WithNullStream_ThrowsArgumentNullException()
  {
    // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => ArchiveDetector.DetectFormat((Stream)null!));
#pragma warning restore CS8625

    Assert.Equal("stream", exception.ParamName);
  }

  [Fact]
  public void DetectFormat_FromStream_WithNonReadableStream_ThrowsArgumentException()
  {
    // Arrange
    using (NonReadableStream stream = new NonReadableStream())
    {
      // Act & Assert
      ArgumentException exception = Assert.Throws<ArgumentException>(
        () => ArchiveDetector.DetectFormat(stream));

      Assert.Equal("stream", exception.ParamName);
      Assert.Contains("readable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void DetectFormat_FromStream_WithNonSeekableStream_ThrowsArgumentException()
  {
    // Arrange
    using (NonSeekableStream stream = new NonSeekableStream())
    {
      // Act & Assert
      ArgumentException exception = Assert.Throws<ArgumentException>(
        () => ArchiveDetector.DetectFormat(stream));

      Assert.Equal("stream", exception.ParamName);
      Assert.Contains("seekable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
  }

  [Fact]
  public void DetectFormat_WithZipSignature_ReturnsZip()
  {
    // Arrange - Standard ZIP signature (PK\x03\x04)
    byte[] zipData = { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00 };
    using (MemoryStream stream = new MemoryStream(zipData))
    {
      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
      Assert.Equal(0, stream.Position); // Position should be restored
    }
  }

  [Fact]
  public void DetectFormat_WithEmptyZipSignature_ReturnsZip()
  {
    // Arrange - Empty ZIP signature (PK\x05\x06)
    byte[] zipData = { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00 };
    using (MemoryStream stream = new MemoryStream(zipData))
    {
      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
    }
  }

  [Fact]
  public void DetectFormat_WithSpannedZipSignature_ReturnsZip()
  {
    // Arrange - Spanned ZIP signature (PK\x07\x08)
    byte[] zipData = { 0x50, 0x4B, 0x07, 0x08, 0x00, 0x00, 0x00, 0x00 };
    using (MemoryStream stream = new MemoryStream(zipData))
    {
      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
    }
  }

  [Fact]
  public void DetectFormat_WithActualZipArchive_ReturnsZip()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      // Create a real ZIP archive
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

      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
      Assert.Equal(0, stream.Position); // Position should be restored
    }
  }

  [Fact]
  public void DetectFormat_WithTextFile_ReturnsUnknown()
  {
    // Arrange
    byte[] textData = System.Text.Encoding.ASCII.GetBytes("This is not an archive");
    using (MemoryStream stream = new MemoryStream(textData))
    {
      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Unknown, format);
    }
  }

  [Fact]
  public void DetectFormat_WithTooFewBytes_ReturnsUnknown()
  {
    // Arrange - Only 2 bytes (need at least 4 for detection)
    byte[] shortData = { 0x50, 0x4B };
    using (MemoryStream stream = new MemoryStream(shortData))
    {
      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Unknown, format);
    }
  }

  [Fact]
  public void DetectFormat_WithEmptyStream_ReturnsUnknown()
  {
    // Arrange
    using (MemoryStream stream = new MemoryStream())
    {
      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Unknown, format);
    }
  }

  [Fact]
  public void DetectFormat_PreservesStreamPosition()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZipStream())
    {
      long originalPosition = 10;
      stream.Position = originalPosition;

      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
      Assert.Equal(originalPosition, stream.Position);
    }
  }

  [Fact]
  public void DetectFormat_WithStreamAtEnd_ReturnsZip()
  {
    // Arrange
    using (MemoryStream stream = CreateTestZipStream())
    {
      stream.Position = stream.Length;

      // Act
      ArchiveFormat format = ArchiveDetector.DetectFormat(stream);

      // Assert
      Assert.Equal(ArchiveFormat.Zip, format);
      Assert.Equal(stream.Length, stream.Position); // Position preserved
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
        byte[] data = System.Text.Encoding.ASCII.GetBytes("test");
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
        byte[] data = System.Text.Encoding.ASCII.GetBytes("test");
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
    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => 0;
    public override long Position { get; set; }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => 0;
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException();
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