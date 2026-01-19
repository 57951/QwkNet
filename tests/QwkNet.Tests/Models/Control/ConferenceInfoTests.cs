using System;
using QwkNet.Models.Control;
using Xunit;

namespace QwkNet.Tests.Models.Control;

public sealed class ConferenceInfoTests
{
  [Fact]
  public void Constructor_ValidParameters_Success()
  {
    // Arrange & Act
    ConferenceInfo conference = new ConferenceInfo(42, "General Discussion");

    // Assert
    Assert.Equal((ushort)42, conference.Number);
    Assert.Equal("General Discussion", conference.Name);
  }

  [Fact]
  public void Constructor_Conference0_AllowedPerSpecification()
  {
    // Arrange & Act
    ConferenceInfo conference = new ConferenceInfo(0, "Main Board");

    // Assert
    Assert.Equal((ushort)0, conference.Number);
    Assert.Equal("Main Board", conference.Name);
  }

  [Fact]
  public void Constructor_NullName_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      new ConferenceInfo(1, null!));
  }

  [Fact]
  public void Constructor_EmptyName_Allowed()
  {
    // Arrange & Act
    ConferenceInfo conference = new ConferenceInfo(1, "");

    // Assert
    Assert.Equal("", conference.Name);
  }

  [Fact]
  public void ToString_FormatsCorrectly()
  {
    // Arrange
    ConferenceInfo conference = new ConferenceInfo(15, "Programming");

    // Act
    string text = conference.ToString();

    // Assert
    Assert.Equal("Conference 15: Programming", text);
  }

  [Fact]
  public void RecordEquality_SameValues_Equal()
  {
    // Arrange
    ConferenceInfo conf1 = new ConferenceInfo(10, "Test");
    ConferenceInfo conf2 = new ConferenceInfo(10, "Test");

    // Act & Assert
    Assert.Equal(conf1, conf2);
    Assert.True(conf1 == conf2);
  }

  [Fact]
  public void RecordEquality_DifferentNumbers_NotEqual()
  {
    // Arrange
    ConferenceInfo conf1 = new ConferenceInfo(10, "Test");
    ConferenceInfo conf2 = new ConferenceInfo(11, "Test");

    // Act & Assert
    Assert.NotEqual(conf1, conf2);
    Assert.False(conf1 == conf2);
  }

  [Fact]
  public void RecordEquality_DifferentNames_NotEqual()
  {
    // Arrange
    ConferenceInfo conf1 = new ConferenceInfo(10, "Test1");
    ConferenceInfo conf2 = new ConferenceInfo(10, "Test2");

    // Act & Assert
    Assert.NotEqual(conf1, conf2);
    Assert.False(conf1 == conf2);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(255)]
  [InlineData(65535)]
  public void Constructor_ValidConferenceNumbers_Success(ushort number)
  {
    // Act
    ConferenceInfo conference = new ConferenceInfo(number, "Test");

    // Assert
    Assert.Equal(number, conference.Number);
  }
}