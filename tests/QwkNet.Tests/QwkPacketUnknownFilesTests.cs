using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using QwkNet.Validation;
using Xunit;

namespace QwkNet.Tests;

/// <summary>
/// Tests for the <see cref="QwkPacket.UnknownFiles"/> and <see cref="QwkPacket.OpenFile"/>
/// members that expose non-standard archive entries to callers.
/// </summary>
public sealed class QwkPacketUnknownFilesTests
{
  // ──────────────────────────────────────────────────────────────────────────
  // UnknownFiles
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void UnknownFiles_StandardPacketWithNoExtraFiles_IsEmpty()
  {
    // Arrange - packet contains only CONTROL.DAT and MESSAGES.DAT
    using QwkPacket packet = BuildPacket(extraFiles: []);

    // Assert
    Assert.Empty(packet.UnknownFiles);
  }

  [Fact]
  public void UnknownFiles_PacketWithNonStandardFile_ContainsThatFile()
  {
    // Arrange
    Dictionary<string, byte[]> extras = new Dictionary<string, byte[]>
    {
      { "HEADERS.DAT", System.Text.Encoding.ASCII.GetBytes("some data") }
    };

    using QwkPacket packet = BuildPacket(extraFiles: extras);

    // Assert
    Assert.Single(packet.UnknownFiles);
    Assert.Contains("HEADERS.DAT", packet.UnknownFiles);
  }

  [Fact]
  public void UnknownFiles_PacketWithMultipleNonStandardFiles_ContainsAllOfThem()
  {
    // Arrange
    Dictionary<string, byte[]> extras = new Dictionary<string, byte[]>
    {
      { "HEADERS.DAT", new byte[] { 0x01, 0x02 } },
      { "USERINFO.DAT", new byte[] { 0x03, 0x04 } },
      { "VENDOR.EXT", new byte[] { 0x05 } }
    };

    using QwkPacket packet = BuildPacket(extraFiles: extras);

    // Assert
    Assert.Equal(3, packet.UnknownFiles.Count);
    Assert.Contains("HEADERS.DAT", packet.UnknownFiles);
    Assert.Contains("USERINFO.DAT", packet.UnknownFiles);
    Assert.Contains("VENDOR.EXT", packet.UnknownFiles);
  }

  [Fact]
  public void UnknownFiles_ExcludesKnownFilesCaseInsensitively_ControlDatLowerCase()
  {
    // Arrange - CONTROL.DAT stored with lowercase name; must still be treated as known
    Dictionary<string, byte[]> extras = new Dictionary<string, byte[]>
    {
      { "UNKNOWN.DAT", new byte[] { 0xFF } }
    };

    // Build a packet where the standard files are included with exact casing;
    // the unknown file should be the only entry in UnknownFiles.
    using QwkPacket packet = BuildPacket(extraFiles: extras);

    Assert.DoesNotContain("CONTROL.DAT", packet.UnknownFiles);
    Assert.DoesNotContain("control.dat", packet.UnknownFiles);
    Assert.DoesNotContain("MESSAGES.DAT", packet.UnknownFiles);
    Assert.Contains("UNKNOWN.DAT", packet.UnknownFiles);
  }

  [Fact]
  public void UnknownFiles_ExcludesAllEightKnownFiles()
  {
    // Arrange - add all known files with their standard names plus one unknown
    Dictionary<string, byte[]> extras = new Dictionary<string, byte[]>
    {
      { "DOOR.ID",      System.Text.Encoding.ASCII.GetBytes("DOOR = Test\r\nVERSION = 1.0\r\n") },
      { "TOREADER.EXT", System.Text.Encoding.ASCII.GetBytes("ADD 1\r\n") },
      { "TODOOR.EXT",   System.Text.Encoding.ASCII.GetBytes("ADD 1\r\n") },
      { "WELCOME",      System.Text.Encoding.ASCII.GetBytes("Welcome!") },
      { "NEWS",         System.Text.Encoding.ASCII.GetBytes("News!") },
      { "GOODBYE",      System.Text.Encoding.ASCII.GetBytes("Goodbye!") },
      { "HEADERS.DAT",  new byte[] { 0x01 } }
    };

    using QwkPacket packet = BuildPacket(extraFiles: extras);

    // Only HEADERS.DAT is unknown
    Assert.Single(packet.UnknownFiles);
    Assert.Equal("HEADERS.DAT", packet.UnknownFiles[0]);
  }

  // ──────────────────────────────────────────────────────────────────────────
  // OpenFile
  // ──────────────────────────────────────────────────────────────────────────

  [Fact]
  public void OpenFile_KnownFile_ReturnsReadableStream()
  {
    // Arrange
    using QwkPacket packet = BuildPacket(extraFiles: []);

    // Act
    using Stream? stream = packet.OpenFile("CONTROL.DAT");

    // Assert
    Assert.NotNull(stream);
    Assert.True(stream.CanRead);
    // DeflateStream does not support .Length; verify content by reading.
    byte[] buf = new byte[1];
    int bytesRead = stream.Read(buf, 0, 1);
    Assert.True(bytesRead > 0, "Expected stream to contain data");
  }

  [Fact]
  public void OpenFile_UnknownFile_ReturnsReadableStream()
  {
    // Arrange
    byte[] sentinelData = System.Text.Encoding.ASCII.GetBytes("HEADERS.DAT content marker");
    Dictionary<string, byte[]> extras = new Dictionary<string, byte[]>
    {
      { "HEADERS.DAT", sentinelData }
    };

    using QwkPacket packet = BuildPacket(extraFiles: extras);

    // Act
    using Stream? stream = packet.OpenFile("HEADERS.DAT");

    // Assert
    Assert.NotNull(stream);
    Assert.True(stream.CanRead);

    // Read back and verify content
    using MemoryStream ms = new MemoryStream();
    stream.CopyTo(ms);
    Assert.Equal(sentinelData, ms.ToArray());
  }

  [Fact]
  public void OpenFile_CaseInsensitiveName_ReturnsStream()
  {
    // Arrange
    using QwkPacket packet = BuildPacket(extraFiles: []);

    // Act - try various casings of CONTROL.DAT
    using Stream? lowerStream = packet.OpenFile("control.dat");

    // Assert
    Assert.NotNull(lowerStream);
    Assert.True(lowerStream.CanRead);
  }

  [Fact]
  public void OpenFile_NonExistentFile_ReturnsNull()
  {
    // Arrange
    using QwkPacket packet = BuildPacket(extraFiles: []);

    // Act
    Stream? stream = packet.OpenFile("MISSING_FILE.DAT");

    // Assert
    Assert.Null(stream);
  }

  [Fact]
  public void OpenFile_NullName_ThrowsArgumentNullException()
  {
    // Arrange
    using QwkPacket packet = BuildPacket(extraFiles: []);

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => packet.OpenFile(null!));
  }

  [Fact]
  public void OpenFile_DoesNotParseOrInterpretStream_RawBytesIntact()
  {
    // Arrange - write raw bytes that are NOT valid text; verify they pass through unmodified
    byte[] rawBinary = [0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD, 0x80, 0x7F];
    Dictionary<string, byte[]> extras = new Dictionary<string, byte[]>
    {
      { "BINARY.DAT", rawBinary }
    };

    using QwkPacket packet = BuildPacket(extraFiles: extras);

    // Act
    using Stream? stream = packet.OpenFile("BINARY.DAT");
    Assert.NotNull(stream);

    using MemoryStream result = new MemoryStream();
    stream.CopyTo(result);

    // Assert - bytes are returned completely verbatim
    Assert.Equal(rawBinary, result.ToArray());
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Synthetic packet builder
  // ──────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Builds a minimal in-memory QWK packet containing CONTROL.DAT and
  /// MESSAGES.DAT, plus any additional files supplied in <paramref name="extraFiles"/>.
  /// </summary>
  /// <param name="extraFiles">
  /// A dictionary of additional file names and their byte contents to include in the archive.
  /// </param>
  private static QwkPacket BuildPacket(Dictionary<string, byte[]> extraFiles)
  {
    MemoryStream zipStream = new MemoryStream();
    using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
    {
      ZipArchiveEntry controlEntry = zip.CreateEntry("CONTROL.DAT");
      using (Stream entryStream = controlEntry.Open())
      {
        byte[] controlData = BuildControlDat();
        entryStream.Write(controlData, 0, controlData.Length);
      }

      ZipArchiveEntry messagesEntry = zip.CreateEntry("MESSAGES.DAT");
      using (Stream entryStream = messagesEntry.Open())
      {
        byte[] messagesData = BuildMinimalMessagesDat();
        entryStream.Write(messagesData, 0, messagesData.Length);
      }

      foreach (KeyValuePair<string, byte[]> extra in extraFiles)
      {
        ZipArchiveEntry extraEntry = zip.CreateEntry(extra.Key);
        using Stream entryStream = extraEntry.Open();
        entryStream.Write(extra.Value, 0, extra.Value.Length);
      }
    }

    zipStream.Position = 0;
    return QwkPacket.Open(zipStream, ValidationMode.Lenient);
  }

  /// <summary>
  /// Builds a minimal CONTROL.DAT byte array.
  /// </summary>
  private static byte[] BuildControlDat()
  {
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("Test BBS");
    sb.AppendLine("Test City, TS");
    sb.AppendLine("555-0100");
    sb.AppendLine("Test SysOp");
    sb.AppendLine("12345,TESTBBS");
    sb.AppendLine("01-01-25,00:00:00");
    sb.AppendLine("TEST USER");
    sb.AppendLine("");
    sb.AppendLine("1");
    sb.AppendLine("0");
    sb.AppendLine("0");
    sb.AppendLine("Main");
    return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
  }

  /// <summary>
  /// Builds a minimal MESSAGES.DAT byte array (copyright block only, no messages).
  /// </summary>
  private static byte[] BuildMinimalMessagesDat()
  {
    // Just the 128-byte copyright/filler block; no messages.
    byte[] data = new byte[128];
    System.Array.Fill(data, (byte)' ');
    return data;
  }
}
