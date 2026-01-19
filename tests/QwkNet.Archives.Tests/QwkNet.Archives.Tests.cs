using System;
using System.IO;
using System.Linq;
using System.Text;
using QwkNet.Archive;
using QwkNet.Archive.Extensions;
using QwkNet.Archives.Tar;
using Xunit;

namespace QwkNet.Archives.Tests;

/// <summary>
/// Integration tests proving TAR archives work correctly with QWK.NET library.
/// </summary>
/// <remarks>
/// These tests demonstrate that third-party archive extensions can be used
/// seamlessly with QWK.NET without modifying the core library.
/// The TAR extension is registered by the module initializer (TarTestInitializer).
/// </remarks>
public sealed class TarIntegrationTests
{

  [Fact]
  public void QwkPacket_CanBeCreatedInTarArchive()
  {
    // Arrange - create a mock QWK packet structure in TAR
    MemoryStream tarStream = new MemoryStream();

    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")))
    {
      // Add typical QWK packet files
      writer.AddFile("CONTROL.DAT", CreateMockControlDat());
      writer.AddFile("MESSAGES.DAT", CreateMockMessagesDat());
      writer.AddFile("PERSONAL.NDX", new MemoryStream(new byte[0]));

      writer.Save(tarStream);
    }

    // Act - verify we can read it back
    tarStream.Position = 0;
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(tarStream, leaveOpen: false))
    {
      // Assert
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Equal(3, files.Count);
      Assert.Contains("CONTROL.DAT", files);
      Assert.Contains("MESSAGES.DAT", files);
      Assert.Contains("PERSONAL.NDX", files);

      // Verify CONTROL.DAT content
      using (Stream controlStream = reader.OpenFile("CONTROL.DAT"))
      {
        byte[] content = new byte[controlStream.Length];
        controlStream.ReadExactly(content, 0, content.Length);
        string text = System.Text.Encoding.ASCII.GetString(content);
        Assert.StartsWith("MYBBS", text);
      }
    }
  }

  [Fact]
  public void AutoDetection_RecognisesTarArchive()
  {
    // Arrange - create TAR archive with QWK structure
    MemoryStream tarStream = CreateMockQwkTarArchive();

    // Act - use automatic detection (no explicit format)
    tarStream.Position = 0;
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(tarStream, leaveOpen: false))
    {
      // Assert - should be detected as TAR
      Assert.IsType<TarArchiveReader>(reader);

      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Contains("CONTROL.DAT", files);
    }
  }

  [Fact]
  public void TarArchive_PreservesCaseInFileNames()
  {
    // Arrange
    MemoryStream tarStream = new MemoryStream();

    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")))
    {
      // Add files with mixed case
      writer.AddFile("CONTROL.DAT", CreateMockControlDat());
      writer.AddFile("messages.dat", CreateMockMessagesDat());
      writer.AddFile("Personal.Ndx", new MemoryStream(new byte[0]));

      writer.Save(tarStream);
    }

    // Act
    tarStream.Position = 0;
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(tarStream, leaveOpen: false))
    {
      // Assert - case is preserved in listing
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Contains("CONTROL.DAT", files);
      Assert.Contains("messages.dat", files);
      Assert.Contains("Personal.Ndx", files);

      // But lookup is case-insensitive
      Assert.True(reader.FileExists("control.dat"));
      Assert.True(reader.FileExists("MESSAGES.DAT"));
      Assert.True(reader.FileExists("personal.ndx"));
    }
  }

  [Fact]
  public void TarArchive_SupportsSubdirectories()
  {
    // Arrange
    MemoryStream tarStream = new MemoryStream();

    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")))
    {
      // Add files in subdirectory (unusual for QWK, but valid)
      writer.AddFile("CONTROL.DAT", CreateMockControlDat());
      writer.AddFile("optional/WELCOME.TXT", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("Welcome!")));
      writer.AddFile("optional/NEWS.TXT", new MemoryStream(System.Text.Encoding.ASCII.GetBytes("News")));

      writer.Save(tarStream);
    }

    // Act
    tarStream.Position = 0;
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(tarStream, leaveOpen: false))
    {
      // Assert
      System.Collections.Generic.IReadOnlyList<string> files = reader.ListFiles();
      Assert.Equal(3, files.Count);
      Assert.Contains("optional/WELCOME.TXT", files);

      using (Stream welcomeStream = reader.OpenFile("optional/WELCOME.TXT"))
      {
        byte[] content = new byte[welcomeStream.Length];
        welcomeStream.ReadExactly(content, 0, content.Length);
        string text = System.Text.Encoding.ASCII.GetString(content);
        Assert.Equal("Welcome!", text);
      }
    }
  }

  [Fact]
  public void TarExtension_CanOverrideZipIfRegisteredFirst()
  {
    // This test verifies that extension detection order works correctly.
    // Note: In practice, TAR and ZIP have different signatures, so this
    // test just verifies the registry mechanism works as documented.

    // Arrange
    MemoryStream tarStream = CreateMockQwkTarArchive();

    // Act - detect automatically
    tarStream.Position = 0;
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(tarStream, leaveOpen: false))
    {
      // Assert - TAR should be detected (not ZIP)
      Assert.IsType<TarArchiveReader>(reader);
    }
  }

  [Fact]
  public void RoundTrip_WriteAndReadTarQwkPacket()
  {
    // Arrange - create complete mock QWK packet in TAR
    MemoryStream tarStream = new MemoryStream();
    string expectedBbsName = "TEST BBS";
    string expectedLocation = "London, UK";

    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")))
    {
      MemoryStream controlDat = new MemoryStream();
      string controlContent =
        $"{expectedBbsName}\r\n" +
        $"{expectedLocation}\r\n" +
        "555-1234\r\n" +
        "Sysop Name\r\n" +
        "0,MYBBS\r\n" +
        "01-01-2025,12:00:00\r\n" +
        "ALICE\r\n";

      byte[] controlBytes = System.Text.Encoding.ASCII.GetBytes(controlContent);
      controlDat.Write(controlBytes, 0, controlBytes.Length);
      controlDat.Position = 0;

      writer.AddFile("CONTROL.DAT", controlDat);
      writer.AddFile("MESSAGES.DAT", CreateMockMessagesDat());

      writer.Save(tarStream);
    }

    // Act - read it back
    tarStream.Position = 0;
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(
      tarStream,
      ArchiveFormatId.From("tar"),
      leaveOpen: false))
    {
      // Assert - verify CONTROL.DAT
      using (Stream controlStream = reader.OpenFile("CONTROL.DAT"))
      {
        byte[] content = new byte[controlStream.Length];
        controlStream.ReadExactly(content, 0, content.Length);
        string text = System.Text.Encoding.ASCII.GetString(content);

        Assert.Contains(expectedBbsName, text);
        Assert.Contains(expectedLocation, text);
      }

      // Verify MESSAGES.DAT exists
      Assert.True(reader.FileExists("MESSAGES.DAT"));
    }
  }

  /// <summary>
  /// Creates a mock CONTROL.DAT file content.
  /// </summary>
  private static MemoryStream CreateMockControlDat()
  {
    string content =
      "MYBBS\r\n" +
      "Somewhere, Earth\r\n" +
      "555-1234\r\n" +
      "Sysop\r\n" +
      "0,MYBBS\r\n" +
      "01-01-2025,12:00:00\r\n" +
      "USER\r\n";

    byte[] bytes = System.Text.Encoding.ASCII.GetBytes(content);
    MemoryStream stream = new MemoryStream();
    stream.Write(bytes, 0, bytes.Length);
    stream.Position = 0;
    return stream;
  }

  /// <summary>
  /// Creates a mock MESSAGES.DAT file (128-byte header only, no body).
  /// </summary>
  private static MemoryStream CreateMockMessagesDat()
  {
    // Create minimal 128-byte message header
    byte[] header = new byte[128];

    // Status byte (public, unread)
    header[0] = (byte)' ';

    // Message number "1"
    System.Text.Encoding.ASCII.GetBytes("1       ").CopyTo(header, 1);

    // Minimal valid header (rest is spaces)
    for (int i = 9; i < 128; i++)
    {
      header[i] = (byte)' ';
    }

    MemoryStream stream = new MemoryStream();
    stream.Write(header, 0, header.Length);
    stream.Position = 0;
    return stream;
  }

  /// <summary>
  /// Creates a complete mock QWK packet in TAR format.
  /// </summary>
  private static MemoryStream CreateMockQwkTarArchive()
  {
    MemoryStream output = new MemoryStream();

    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      writer.AddFile("CONTROL.DAT", CreateMockControlDat());
      writer.AddFile("MESSAGES.DAT", CreateMockMessagesDat());
      writer.Save(output);
    }

    output.Position = 0;
    return output;
  }
}