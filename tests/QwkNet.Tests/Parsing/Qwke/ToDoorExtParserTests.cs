using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using QwkNet.Models.Qwke;
using QwkNet.Parsing.Qwke;

namespace QwkNet.Tests.Parsing.Qwke;

public sealed class ToDoorExtParserTests
{
  [Fact]
  public void Parse_EmptyStream_ReturnsEmptyList()
  {
    // Arrange
    byte[] data = Array.Empty<byte>();
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Empty(commands);
  }

  [Fact]
  public void Parse_SingleCommand_ParsesCorrectly()
  {
    // Arrange
    string content = "ATTACH document.pdf";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("ATTACH", commands[0].CommandType);
    Assert.Equal("document.pdf", commands[0].Parameters);
  }

  [Fact]
  public void Parse_MultipleCommands_ParsesAll()
  {
    // Arrange
    string content = "ADD 1\r\nADD 2\r\nDROP 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
    Assert.Equal("ADD", commands[0].CommandType);
    Assert.Equal("1", commands[0].Parameters);
    Assert.Equal("ADD", commands[1].CommandType);
    Assert.Equal("2", commands[1].Parameters);
    Assert.Equal("DROP", commands[2].CommandType);
    Assert.Equal("3", commands[2].Parameters);
  }

  [Fact]
  public void Parse_CommandWithoutParameters_ParsesCommandTypeOnly()
  {
    // Arrange
    string content = "DROP";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("DROP", commands[0].CommandType);
    Assert.Equal(string.Empty, commands[0].Parameters);
  }

  [Fact]
  public void Parse_EmptyLines_SkipsEmptyLines()
  {
    // Arrange
    string content = "ADD 1\r\n\r\nADD 2\r\n\r\n\r\nDROP 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_WhitespaceOnlyLines_SkipsWhitespaceLines()
  {
    // Arrange
    string content = "ADD 1\r\n   \r\nADD 2\r\n\t\t\r\nDROP 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_LeadingWhitespace_TrimsWhitespace()
  {
    // Arrange
    string content = "  REQUEST file.zip";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("REQUEST", commands[0].CommandType);
    Assert.Equal("file.zip", commands[0].Parameters);
  }

  [Fact]
  public void Parse_TrailingWhitespace_TrimsWhitespace()
  {
    // Arrange
    string content = "REQUEST file.zip  ";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("REQUEST", commands[0].CommandType);
    Assert.Equal("file.zip", commands[0].Parameters);
  }

  [Fact]
  public void Parse_FilePathWithSpaces_PreservesSpaces()
  {
    // Arrange
    string content = "ATTACH /path/to/my document.pdf";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("ATTACH", commands[0].CommandType);
    Assert.Equal("/path/to/my document.pdf", commands[0].Parameters);
  }

  [Fact]
  public void Parse_TabSeparatedCommand_SplitsOnTab()
  {
    // Arrange
    string content = "REQUEST\tfile1.zip file2.zip";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("REQUEST", commands[0].CommandType);
    Assert.Equal("file1.zip file2.zip", commands[0].Parameters);
  }

  [Fact]
  public void Parse_MixedCaseCommand_PreservesCase()
  {
    // Arrange
    string content = "MyCustomCommand param1";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("MyCustomCommand", commands[0].CommandType);
  }

  [Fact]
  public void Parse_UnixLineEndings_ParsesCorrectly()
  {
    // Arrange
    string content = "ADD 1\nADD 2\nDROP 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_WindowsLineEndings_ParsesCorrectly()
  {
    // Arrange
    string content = "ADD 1\r\nADD 2\r\nDROP 3";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
  }

  [Fact]
  public void Parse_ByteArray_CallsStreamOverload()
  {
    // Arrange
    string content = "REQUEST file.zip";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(data);

    // Assert
    Assert.Single(commands);
    Assert.Equal("REQUEST", commands[0].CommandType);
    Assert.Equal("file.zip", commands[0].Parameters);
  }

  [Fact]
  public void Parse_NullStream_ThrowsArgumentNullException()
  {
    // Arrange
    Stream? stream = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => ToDoorExtParser.Parse(stream!));
  }

  [Fact]
  public void Parse_NullByteArray_ThrowsArgumentNullException()
  {
    // Arrange
    byte[]? data = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => ToDoorExtParser.Parse(data!));
  }

  [Fact]
  public void Parse_SpecialCharactersInParameters_Preserved()
  {
    // Arrange
    string content = "CONTROL !@#$%^&*()";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("!@#$%^&*()", commands[0].Parameters);
  }

  [Fact]
  public void Parse_RealWorldExample_ParsesCorrectly()
  {
    // Arrange
    string content = "ADD 1\r\nADD 5\r\nDROP 3\r\nATTACH document.pdf\r\nREQUEST file.zip";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(5, commands.Count);
    Assert.Equal("ADD", commands[0].CommandType);
    Assert.Equal("1", commands[0].Parameters);
    Assert.Equal("ADD", commands[1].CommandType);
    Assert.Equal("5", commands[1].Parameters);
    Assert.Equal("DROP", commands[2].CommandType);
    Assert.Equal("3", commands[2].Parameters);
    Assert.Equal("ATTACH", commands[3].CommandType);
    Assert.Equal("document.pdf", commands[3].Parameters);
    Assert.Equal("REQUEST", commands[4].CommandType);
    Assert.Equal("file.zip", commands[4].Parameters);
  }

  [Fact]
  public void Parse_PreservesRawLine()
  {
    // Arrange
    string content = "ATTACH document.pdf";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Single(commands);
    Assert.Equal("ATTACH document.pdf", commands[0].RawLine);
  }

  [Fact]
  public void Parse_StreamLeavesStreamOpen()
  {
    // Arrange
    string content = "ADD 42";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert - stream should still be usable
    Assert.True(stream.CanRead);
    stream.Dispose();
  }

  [Fact]
  public void Parse_MultipleFileAttachments_ParsesSeparately()
  {
    // Arrange
    string content = "ATTACH file1.pdf\r\nATTACH file2.zip\r\nATTACH file3.doc";
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    using MemoryStream stream = new MemoryStream(data);

    // Act
    IReadOnlyList<ToDoorCommand> commands = ToDoorExtParser.Parse(stream);

    // Assert
    Assert.Equal(3, commands.Count);
    Assert.All(commands, cmd => Assert.Equal("ATTACH", cmd.CommandType));
    Assert.Equal("file1.pdf", commands[0].Parameters);
    Assert.Equal("file2.zip", commands[1].Parameters);
    Assert.Equal("file3.doc", commands[2].Parameters);
  }
}
