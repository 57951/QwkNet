using System;
using Xunit;
using QwkNet.Models.Qwke;

namespace QwkNet.Tests.Models.Qwke;

public sealed class ToDoorCommandTests
{
  [Fact]
  public void Constructor_ValidParameters_CreatesInstance()
  {
    // Arrange
    string commandType = "ATTACH";
    string parameters = "document.pdf";
    string rawLine = "ATTACH document.pdf";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("ATTACH", command.CommandType);
    Assert.Equal("document.pdf", command.Parameters);
    Assert.Equal("ATTACH document.pdf", command.RawLine);
  }

  [Fact]
  public void Constructor_EmptyParameters_AcceptsEmptyString()
  {
    // Arrange
    string commandType = "DROP";
    string parameters = string.Empty;
    string rawLine = "DROP";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("DROP", command.CommandType);
    Assert.Equal(string.Empty, command.Parameters);
    Assert.Equal("DROP", command.RawLine);
  }

  [Fact]
  public void Constructor_NullParameters_ReplacesWithEmptyString()
  {
    // Arrange
    string commandType = "ADD";
    string? parameters = null;
    string rawLine = "ADD";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters!, rawLine);

    // Assert
    Assert.Equal("ADD", command.CommandType);
    Assert.Equal(string.Empty, command.Parameters);
  }

  [Fact]
  public void Constructor_NullCommandType_ThrowsArgumentNullException()
  {
    // Arrange
    string? commandType = null;
    string parameters = "test";
    string rawLine = "test";

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new ToDoorCommand(commandType!, parameters, rawLine));
  }

  [Fact]
  public void Constructor_EmptyCommandType_ThrowsArgumentException()
  {
    // Arrange
    string commandType = string.Empty;
    string parameters = "test";
    string rawLine = "test";

    // Act & Assert
    Assert.Throws<ArgumentException>(() => new ToDoorCommand(commandType, parameters, rawLine));
  }

  [Fact]
  public void Constructor_WhitespaceCommandType_ThrowsArgumentException()
  {
    // Arrange
    string commandType = "   ";
    string parameters = "test";
    string rawLine = "test";

    // Act & Assert
    Assert.Throws<ArgumentException>(() => new ToDoorCommand(commandType, parameters, rawLine));
  }

  [Fact]
  public void Constructor_NullRawLine_ThrowsArgumentNullException()
  {
    // Arrange
    string commandType = "REQUEST";
    string parameters = "file.zip";
    string? rawLine = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new ToDoorCommand(commandType, parameters, rawLine!));
  }

  [Fact]
  public void ToString_WithParameters_ReturnsFormattedString()
  {
    // Arrange
    ToDoorCommand command = new ToDoorCommand("REQUEST", "archive.zip", "REQUEST archive.zip");

    // Act
    string result = command.ToString();

    // Assert
    Assert.Equal("REQUEST archive.zip", result);
  }

  [Fact]
  public void ToString_WithoutParameters_ReturnsCommandTypeOnly()
  {
    // Arrange
    ToDoorCommand command = new ToDoorCommand("ADD", string.Empty, "ADD");

    // Act
    string result = command.ToString();

    // Assert
    Assert.Equal("ADD", result);
  }

  [Fact]
  public void Constructor_FilePathParameters_PreservesFullPath()
  {
    // Arrange
    string commandType = "ATTACH";
    string parameters = "/path/to/my/document with spaces.pdf";
    string rawLine = $"{commandType} {parameters}";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal(parameters, command.Parameters);
    Assert.Equal(rawLine, command.RawLine);
  }

  [Fact]
  public void Constructor_MixedCaseCommandType_PreservesCase()
  {
    // Arrange
    string commandType = "MyCustomAction";
    string parameters = "param1";
    string rawLine = "MyCustomAction param1";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("MyCustomAction", command.CommandType);
  }

  [Fact]
  public void Constructor_SpecialCharactersInParameters_Preserved()
  {
    // Arrange
    string commandType = "CONTROL";
    string parameters = "!@#$%^&*()";
    string rawLine = "CONTROL !@#$%^&*()";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("!@#$%^&*()", command.Parameters);
  }

  [Fact]
  public void Constructor_MultipleSpacesInParameters_Preserved()
  {
    // Arrange
    string commandType = "REQUEST";
    string parameters = "file1.txt   file2.txt   file3.txt";
    string rawLine = "REQUEST file1.txt   file2.txt   file3.txt";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Contains("   ", command.Parameters);
  }

  [Fact]
  public void Constructor_ConferenceNumberParameter_ParsesCorrectly()
  {
    // Arrange
    string commandType = "DROP";
    string parameters = "42";
    string rawLine = "DROP 42";

    // Act
    ToDoorCommand command = new ToDoorCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("42", command.Parameters);
  }
}
