using System;
using System.IO;
using QwkNet.Archive;
using QwkNet.Archive.Extensions;
using QwkNet.Archives.Tar;
using Xunit;

namespace QwkNet.Archives.Tests;

/// <summary>
/// Tests for <see cref="TarArchiveExtension"/> registration and integration.
/// </summary>
public sealed class TarArchiveExtensionTests : IDisposable
{
  public void Dispose()
  {
    // Clean up after each test - but don't unregister TAR since other tests need it
  }

  [Fact]
  public void FormatId_ReturnsExpectedValue()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act
    ArchiveFormatId formatId = extension.FormatId;

    // Assert
    Assert.Equal("tar", formatId.ToString());
  }

  [Fact]
  public void Signatures_ContainsUstarSignature()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act
    System.Collections.Generic.IReadOnlyList<ArchiveSignature> signatures = extension.Signatures;

    // Assert
    Assert.NotEmpty(signatures);
    Assert.Single(signatures);

    ArchiveSignature signature = signatures[0];
    Assert.Equal(257, signature.Offset);
    Assert.Equal(263, signature.MinimumLength);

    // "ustar\0"
    byte[] expectedMagic = { 0x75, 0x73, 0x74, 0x61, 0x72, 0x00 };
    Assert.Equal(expectedMagic, signature.MagicBytes);
  }

  [Fact]
  public void SupportsReading_ReturnsTrue()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act & Assert
    Assert.True(extension.SupportsReading);
  }

  [Fact]
  public void SupportsWriting_ReturnsTrue()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act & Assert
    Assert.True(extension.SupportsWriting);
  }

  [Fact]
  public void RegisterExtension_Succeeds()
  {
    // Arrange - TAR is already registered by module initializer
    // Just verify it's in the list
    
    // Act
    System.Collections.Generic.IReadOnlyList<ArchiveFormatId> registered =
      ArchiveFactory.ListRegisteredExtensions();

    // Assert - TAR should be registered
    Assert.Contains(ArchiveFormatId.From("tar"), registered);
  }

  [Fact]
  public void RegisterExtension_Twice_ThrowsInvalidOperationException()
  {
    // Arrange - TAR is already registered by module initializer
    // So we test with a fresh instance attempting to register again
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act & Assert - should throw because TAR is already registered
    InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
      () => ArchiveFactory.RegisterExtension(extension));

    Assert.Contains("tar", ex.Message);
    Assert.Contains("already registered", ex.Message);
  }

  [Fact]
  public void UnregisterExtension_AfterRegistration_ReturnsTrue()
  {
    // Arrange - TAR is already registered by module initializer

    // Act - unregister it
    bool result = ArchiveFactory.UnregisterExtension(ArchiveFormatId.From("tar"));

    // Assert
    Assert.True(result);

    System.Collections.Generic.IReadOnlyList<ArchiveFormatId> registered =
      ArchiveFactory.ListRegisteredExtensions();

    Assert.DoesNotContain(ArchiveFormatId.From("tar"), registered);
    
    // Re-register for other tests
    ArchiveFactory.RegisterExtension(new TarArchiveExtension());
  }

  [Fact]
  public void UnregisterExtension_WhenNotRegistered_ReturnsFalse()
  {
    // Arrange - first unregister TAR since it's registered by module initializer
    ArchiveFactory.UnregisterExtension(ArchiveFormatId.From("tar"));
    
    // Act - try to unregister again (when not registered)
    bool result = ArchiveFactory.UnregisterExtension(ArchiveFormatId.From("tar"));

    // Assert
    Assert.False(result);
    
    // Re-register for other tests
    ArchiveFactory.RegisterExtension(new TarArchiveExtension());
  }

  [Fact]
  public void CreateReader_WithValidStream_ReturnsReader()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();
    MemoryStream stream = CreateMinimalTarArchive();

    // Act
    using (IArchiveReader reader = extension.CreateReader(stream, leaveOpen: false))
    {
      // Assert
      Assert.NotNull(reader);
      Assert.IsType<TarArchiveReader>(reader);
    }
  }

  [Fact]
  public void CreateReader_WithNullStream_ThrowsArgumentNullException()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(
      () => extension.CreateReader(null!, leaveOpen: false));
  }

  [Fact]
  public void CreateWriter_ReturnsWriter()
  {
    // Arrange
    TarArchiveExtension extension = new TarArchiveExtension();

    // Act
    using (IArchiveWriter writer = extension.CreateWriter())
    {
      // Assert
      Assert.NotNull(writer);
      Assert.IsType<TarArchiveWriter>(writer);
    }
  }

  [Fact]
  public void ArchiveFactory_OpenArchive_WithRegisteredExtension_DetectsTar()
  {
    // Arrange - TAR is already registered by module initializer
    MemoryStream tarStream = CreateMinimalTarArchive();

    // Act
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(tarStream, leaveOpen: false))
    {
      // Assert
      Assert.NotNull(reader);
      Assert.IsType<TarArchiveReader>(reader);
    }
  }

  [Fact]
  public void ArchiveFactory_OpenArchive_WithExplicitFormatId_OpensTar()
  {
    // Arrange - TAR is already registered by module initializer
    MemoryStream tarStream = CreateMinimalTarArchive();

    // Act
    using (IArchiveReader reader = ArchiveFactory.OpenArchive(
      tarStream,
      ArchiveFormatId.From("tar"),
      leaveOpen: false))
    {
      // Assert
      Assert.NotNull(reader);
      Assert.IsType<TarArchiveReader>(reader);
    }
  }

  [Fact]
  public void ArchiveFactory_CreateWriter_WithRegisteredExtension_CreatesTar()
  {
    // Arrange - TAR is already registered by module initializer

    // Act
    using (IArchiveWriter writer = ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")))
    {
      // Assert
      Assert.NotNull(writer);
      Assert.IsType<TarArchiveWriter>(writer);
    }
  }

  [Fact]
  public void ArchiveFactory_CreateWriter_WithoutRegistration_ThrowsNotSupportedException()
  {
    // Arrange - unregister TAR first
    ArchiveFactory.UnregisterExtension(ArchiveFormatId.From("tar"));

    try
    {
      // Act & Assert - should throw because TAR is not registered
      NotSupportedException ex = Assert.Throws<NotSupportedException>(
        () => ArchiveFactory.CreateWriter(ArchiveFormatId.From("tar")));

      Assert.Contains("tar", ex.Message);
      Assert.Contains("not supported", ex.Message);
    }
    finally
    {
      // Re-register for other tests
      ArchiveFactory.RegisterExtension(new TarArchiveExtension());
    }
  }

  /// <summary>
  /// Creates a minimal valid TAR archive with a single empty file.
  /// </summary>
  private static MemoryStream CreateMinimalTarArchive()
  {
    MemoryStream output = new MemoryStream();

    using (TarArchiveWriter writer = new TarArchiveWriter())
    {
      // Add a minimal file
      MemoryStream content = new MemoryStream(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }); // "Hello"
      writer.AddFile("test.txt", content);
      writer.Save(output);
    }

    output.Position = 0;
    return output;
  }

  /// <summary>
  /// Unregisters TAR extension if present (cleanup helper).
  /// </summary>
  private static void UnregisterTarIfPresent()
  {
    ArchiveFactory.UnregisterExtension(ArchiveFormatId.From("tar"));
  }
}