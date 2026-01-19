using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using QwkNet.Models.Qwke;
using QwkNet.Parsing.Qwke;

namespace QwkNet.Tests.Parsing.Qwke;

public sealed class ToReaderExtParserTests
{
  [Fact]
  public void Parse_EmptyStream_ReturnsEmptyList()
  {
    // Arrange
    byte[] data = Array.Empty<byte>();
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Empty(commands);
  }

  [Fact]
  public void Parse_SingleCommand_ParsesCorrectly()
  {
    // Arrange
    string content = "AREA 42";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("AREA", commands[0].CommandType);
    Assert.Equal("42", commands[0].Parameters);
  }

  [Fact]
  public void Parse_MultipleCommands_ParsesAll()
  {
    // Arrange
    string content = "AREA 1\r\nAREA 2\r\nAREA 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
    Assert.Equal("AREA", commands[0].CommandType);
    Assert.Equal("1", commands[0].Parameters);
    Assert.Equal("AREA", commands[1].CommandType);
    Assert.Equal("2", commands[1].Parameters);
    Assert.Equal("AREA", commands[2].CommandType);
    Assert.Equal("3", commands[2].Parameters);
  }

  [Fact]
  public void Parse_CommandWithoutParameters_ParsesCommandTypeOnly()
  {
    // Arrange
    string content = "RESET";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("RESET", commands[0].CommandType);
    Assert.Equal(string.Empty, commands[0].Parameters);
  }

  [Fact]
  public void Parse_EmptyLines_SkipsEmptyLines()
  {
    // Arrange
    string content = "AREA 1\r\n\r\nAREA 2\r\n\r\n\r\nAREA 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_WhitespaceOnlyLines_SkipsWhitespaceLines()
  {
    // Arrange
    string content = "AREA 1\r\n   \r\nAREA 2\r\n\t\t\r\nAREA 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_LeadingWhitespace_TrimsWhitespace()
  {
    // Arrange
    string content = "  AREA 42";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("AREA", commands[0].CommandType);
    Assert.Equal("42", commands[0].Parameters);
  }

  [Fact]
  public void Parse_TrailingWhitespace_TrimsWhitespace()
  {
    // Arrange
    string content = "AREA 42  ";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("AREA", commands[0].CommandType);
    Assert.Equal("42", commands[0].Parameters);
  }

  [Fact]
  public void Parse_MultipleParameters_PreservesAllParameters()
  {
    // Arrange
    string content = "KEYWORD urgent important critical";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("KEYWORD", commands[0].CommandType);
    Assert.Equal("urgent important critical", commands[0].Parameters);
  }

  [Fact]
  public void Parse_TabSeparatedCommand_SplitsOnTab()
  {
    // Arrange
    string content = "TWIT\tuser1 user2";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("TWIT", commands[0].CommandType);
    Assert.Equal("user1 user2", commands[0].Parameters);
  }

  [Fact]
  public void Parse_MixedCaseCommand_PreservesCase()
  {
    // Arrange
    string content = "MyCommand param1";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("MyCommand", commands[0].CommandType);
  }

  [Fact]
  public void Parse_UnixLineEndings_ParsesCorrectly()
  {
    // Arrange
    string content = "AREA 1\nAREA 2\nAREA 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_WindowsLineEndings_ParsesCorrectly()
  {
    // Arrange
    string content = "AREA 1\r\nAREA 2\r\nAREA 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_ByteArray_CallsStreamOverload()
  {
    // Arrange
    string content = "AREA 42";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(data);

    // Assert
    Assert.Single(commands);
    Assert.Equal("AREA", commands[0].CommandType);
    Assert.Equal("42", commands[0].Parameters);
  }

  [Fact]
  public void Parse_NullStream_ThrowsArgumentNullException()
  {
    // Arrange
    Stream? stream = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => ToReaderExtParser.Parse(stream!));
  }

  [Fact]
  public void Parse_NullByteArray_ThrowsArgumentNullException()
  {
    // Arrange
    byte[]? data = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => ToReaderExtParser.Parse(data!));
  }

  [Fact]
  public void Parse_SpecialCharactersInParameters_Preserved()
  {
    // Arrange
    string content = "FILTER !@#$%^&*()";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("!@#$%^&*()", commands[0].Parameters);
  }

  [Fact]
  public void Parse_RealWorldExample_ParsesCorrectly()
  {
    // Arrange
    string content = "AREA 1\r\nAREA 5\r\nKEYWORD urgent\r\nTWIT spammer1 spammer2\r\nRESET";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Equal(5, commands.Count);
    Assert.Equal("AREA", commands[0].CommandType);
    Assert.Equal("1", commands[0].Parameters);
    Assert.Equal("AREA", commands[1].CommandType);
    Assert.Equal("5", commands[1].Parameters);
    Assert.Equal("KEYWORD", commands[2].CommandType);
    Assert.Equal("urgent", commands[2].Parameters);
    Assert.Equal("TWIT", commands[3].CommandType);
    Assert.Equal("spammer1 spammer2", commands[3].Parameters);
    Assert.Equal("RESET", commands[4].CommandType);
    Assert.Equal(string.Empty, commands[4].Parameters);
  }

  [Fact]
  public void Parse_PreservesRawLine()
  {
    // Arrange
    string content = "AREA 42";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("AREA 42", commands[0].RawLine);
  }

  [Fact]
  public void Parse_StreamLeavesStreamOpen()
  {
    // Arrange
    string content = "AREA 42";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToReaderCommand> commands = ToReaderExtParser.Parse(stream);

    // Assert - stream should still be usable
    Assert.True(stream.CanRead);
    stream.Dispose();
  }
}
