using System;
using Xunit;
using QwkNet.Models.Messages;

namespace QwkNet.Tests.Models.Messages;

public sealed class MessageStatusTests
{
  [Fact]
  public void None_ShouldHaveValueZero()
  {
    // Arrange & Act
    MessageStatus status = MessageStatus.None;

    // Assert
    Assert.Equal(0, (int)status);
  }

  [Fact]
  public void Private_ShouldHaveCorrectValue()
  {
    // Arrange & Act
    MessageStatus status = MessageStatus.Private;

    // Assert
    Assert.Equal(1, (int)status);
  }

  [Fact]
  public void Read_ShouldHaveCorrectValue()
  {
    // Arrange & Act
    MessageStatus status = MessageStatus.Read;

    // Assert
    Assert.Equal(2, (int)status);
  }

  [Fact]
  public void PrivateAndRead_CanBeCombined()
  {
    // Arrange & Act
    MessageStatus status = MessageStatus.Private | MessageStatus.Read;

    // Assert
    Assert.True(status.HasFlag(MessageStatus.Private));
    Assert.True(status.HasFlag(MessageStatus.Read));
    Assert.False(status.HasFlag(MessageStatus.Deleted));
  }

  [Fact]
  public void AllFlags_CanBeCombined()
  {
    // Arrange & Act
    MessageStatus status = MessageStatus.Private
      | MessageStatus.Read
      | MessageStatus.Deleted
      | MessageStatus.CommentToSysop
      | MessageStatus.SenderPasswordProtected
      | MessageStatus.GroupPasswordProtected
      | MessageStatus.GroupPasswordProtectedToAll
      | MessageStatus.HasNetworkTagLine;

    // Assert
    Assert.True(status.HasFlag(MessageStatus.Private));
    Assert.True(status.HasFlag(MessageStatus.Read));
    Assert.True(status.HasFlag(MessageStatus.Deleted));
    Assert.True(status.HasFlag(MessageStatus.CommentToSysop));
    Assert.True(status.HasFlag(MessageStatus.SenderPasswordProtected));
    Assert.True(status.HasFlag(MessageStatus.GroupPasswordProtected));
    Assert.True(status.HasFlag(MessageStatus.GroupPasswordProtectedToAll));
    Assert.True(status.HasFlag(MessageStatus.HasNetworkTagLine));
  }

  [Fact]
  public void HasFlag_ReturnsFalseForUnsetFlags()
  {
    // Arrange
    MessageStatus status = MessageStatus.Private | MessageStatus.Read;

    // Act & Assert
    Assert.False(status.HasFlag(MessageStatus.Deleted));
    Assert.False(status.HasFlag(MessageStatus.CommentToSysop));
  }
}
