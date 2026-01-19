using System;
using System.IO;
using System.Text;
using QwkNet.Archive;
using QwkNet.Archives.Tar;
using Xunit;

namespace QwkNet.Archives.Tests;

/// <summary>
/// Tests for <see cref="TarArchiveWriter"/>.
/// </summary>
public sealed class TarArchiveWriterTests
{
  [Fact]
  public void Constructor_Succeeds()
  {
    // Act
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Assert
      Assert.NotNull(writer);
    }
  }

  [Fact]
  public void AddFile_WithValidParameters_Succeeds()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test content"));

      // Act
      writer.AddFile("test.txt", content);

      // Assert - should not throw
    }
  }

  [Fact]
  public void AddFile_WithNullName_ThrowsArgumentNullException()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test"));

      // Act & Assert
      ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
        () => writer.AddFile(null!, content));

      Assert.Equal("name", ex.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithNullContent_ThrowsArgumentNullException()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Act & Assert
      ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
        () => writer.AddFile("test.txt", null!));

      Assert.Equal("content", ex.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithEmptyName_ThrowsArgumentException()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test"));

      // Act & Assert
      ArgumentException ex = Assert.Throws<ArgumentException>(
        () => writer.AddFile(string.Empty, content));

      Assert.Contains("empty", ex.Message);
      Assert.Equal("name", ex.ParamName);
    }
  }

  [Fact]
  public void AddFile_WithNullCharacterInName_ThrowsArgumentException()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test"));

      // Act & Assert
      ArgumentException ex = Assert.Throws<ArgumentException>(
        () => writer.AddFile("test\0.txt", content));

      Assert.Contains("null characters", ex.Message);
      Assert.Equal("name", ex.ParamName);
    }
  }

  [Fact]
  public void AddFile_AfterSave_ThrowsInvalidOperationException()
  {
    // Arrange
    MemoryStream output = new MemoryStream();
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      writer.Save(output);

      MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test"));

      // Act & Assert
      InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
        () => writer.AddFile("test.txt", content));

      Assert.Contains("already been saved", ex.Message);
    }
  }

  [Fact]
  public void Save_WithValidOutput_CreatesValidTarArchive()
  {
    // Arrange
    MemoryStream output = new MemoryStream();
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      writer.AddFile("test.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Hello TAR")));

      // Act
      writer.Save(output);
    }

    // Assert - verify we can read it back
    output.Position = 0;
    using (TarArchiveReader reader = new TarArchiveReader(output, leaveOpen: false))
    {
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Single(files);
      Assert.Equal("test.txt", files[0]);

      using (Stream fileStream = reader.OpenFile("test.txt"))
      {
        byte[] content = new byte[fileStream.Length];
        fileStream.ReadExactly(content, 0, content.Length);
        string text = System.Text.Encoding.ASCII.GetString(content);
        Assert.Equal("Hello TAR", text);
      }
    }
  }

  [Fact]
  public void Save_WithMultipleFiles_CreatesValidArchive()
  {
    // Arrange
    MemoryStream output = new MemoryStream();
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      writer.AddFile("file1.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Content 1")));
      writer.AddFile("file2.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Content 2")));
      writer.AddFile("subdir/file3.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Content 3")));

      // Act
      writer.Save(output);
    }

    // Assert
    output.Position = 0;
    using (TarArchiveReader reader = new TarArchiveReader(output, leaveOpen: false))
    {
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Equal(3, files.Count);
      Assert.Contains("file1.txt", files);
      Assert.Contains("file2.txt", files);
      Assert.Contains("subdir/file3.txt", files);
    }
  }

  [Fact]
  public void Save_WithNullOutput_ThrowsArgumentNullException()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Act & Assert
      ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
        () => writer.Save(null!));

      Assert.Equal("output", ex.ParamName);
    }
  }

  [Fact]
  public void Save_WithNonWritableStream_ThrowsArgumentException()
  {
    // Arrange
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      MemoryStream output = new MemoryStream(new byte[100], writable: false);

      // Act & Assert
      ArgumentException ex = Assert.Throws<ArgumentException>(
        () => writer.Save(output));

      Assert.Contains("writable", ex.Message);
      Assert.Equal("output", ex.ParamName);
    }
  }

  [Fact]
  public void Save_CalledTwice_ThrowsInvalidOperationException()
  {
    // Arrange
    MemoryStream output1 = new MemoryStream();
    MemoryStream output2 = new MemoryStream();

    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      writer.Save(output1);

      // Act & Assert
      InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
        () => writer.Save(output2));

      Assert.Contains("already been saved", ex.Message);
    }
  }

  [Fact]
  public void Save_WithEmptyArchive_CreatesValidEmptyTar()
  {
    // Arrange
    MemoryStream output = new MemoryStream();
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Act - save without adding any files
      writer.Save(output);
    }

    // Assert - verify it's a valid (empty) TAR archive
    output.Position = 0;
    using (TarArchiveReader reader = new TarArchiveReader(output, leaveOpen: false))
    {
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Empty(files);
    }
  }

  [Fact]
  public void Save_NormalisesBackslashesToForwardSlashes()
  {
    // Arrange
    MemoryStream output = new MemoryStream();
    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Add file with backslashes (Windows-style path)
      writer.AddFile("subdir\\file.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test")));

      // Act
      writer.Save(output);
    }

    // Assert - verify path was normalised to forward slashes
    output.Position = 0;
    using (TarArchiveReader reader = new TarArchiveReader(output, leaveOpen: false))
    {
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Single(files);
      Assert.Equal("subdir/file.txt", files[0]);
    }
  }

  [Fact]
  public void Dispose_AfterSave_Succeeds()
  {
    // Arrange
    MemoryStream output = new MemoryStream();
    TarArchiveWriter writer = new TarArchiveWriter();
    writer.Save(output);

    // Act
    writer.Dispose();

    // Assert - should not throw
  }

  [Fact]
  public void Dispose_WithoutSave_Succeeds()
  {
    // Arrange
    TarArchiveWriter writer = new TarArchiveWriter();
    writer.AddFile("test.txt", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test")));

    // Act
    writer.Dispose();

    // Assert - should not throw
  }

  [Fact]
  public void Dispose_CalledTwice_Succeeds()
  {
    // Arrange
    TarArchiveWriter writer = new TarArchiveWriter();

    // Act
    writer.Dispose();
    writer.Dispose();

    // Assert - should not throw
  }

  [Fact]
  public void AddFile_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    TarArchiveWriter writer = new TarArchiveWriter();
    writer.Dispose();

    MemoryStream content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Test"));

    // Act & Assert
    Assert.Throws<ObjectDisposedException>(
      () => writer.AddFile("test.txt", content));
  }

  [Fact]
  public void Save_AfterDispose_ThrowsObjectDisposedException()
  {
    // Arrange
    TarArchiveWriter writer = new TarArchiveWriter();
    writer.Dispose();

    MemoryStream output = new MemoryStream();

    // Act & Assert
    Assert.Throws<ObjectDisposedException>(
      () => writer.Save(output));
  }
}