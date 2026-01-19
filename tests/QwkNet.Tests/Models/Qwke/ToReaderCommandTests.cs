using System;
using Xunit;
using QwkNet.Models.Qwke;

namespace QwkNet.Tests.Models.Qwke;

public sealed class ToReaderCommandTests
{
  [Fact]
  public void Constructor_ValidParameters_CreatesInstance()
  {
    // Arrange
    string commandType = "AREA";
    string parameters = "123";
    string rawLine = "AREA 123";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("AREA", command.CommandType);
    Assert.Equal("123", command.Parameters);
    Assert.Equal("AREA 123", command.RawLine);
  }

  [Fact]
  public void Constructor_EmptyParameters_AcceptsEmptyString()
  {
    // Arrange
    string commandType = "RESET";
    string parameters = string.Empty;
    string rawLine = "RESET";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("RESET", command.CommandType);
    Assert.Equal(string.Empty, command.Parameters);
    Assert.Equal("RESET", command.RawLine);
  }

  [Fact]
  public void Constructor_NullParameters_ReplacesWithEmptyString()
  {
    // Arrange
    string commandType = "RESET";
    string? parameters = null;
    string rawLine = "RESET";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters!, rawLine);

    // Assert
    Assert.Equal("RESET", command.CommandType);
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
    Assert.Throws<ArgumentNullException>(() => new ToReaderCommand(commandType!, parameters, rawLine));
  }

  [Fact]
  public void Constructor_EmptyCommandType_ThrowsArgumentException()
  {
    // Arrange
    string commandType = string.Empty;
    string parameters = "test";
    string rawLine = "test";

    // Act & Assert
    Assert.Throws<ArgumentException>(() => new ToReaderCommand(commandType, parameters, rawLine));
  }

  [Fact]
  public void Constructor_WhitespaceCommandType_ThrowsArgumentException()
  {
    // Arrange
    string commandType = "   ";
    string parameters = "test";
    string rawLine = "test";

    // Act & Assert
    Assert.Throws<ArgumentException>(() => new ToReaderCommand(commandType, parameters, rawLine));
  }

  [Fact]
  public void Constructor_NullRawLine_ThrowsArgumentNullException()
  {
    // Arrange
    string commandType = "AREA";
    string parameters = "test";
    string? rawLine = null;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new ToReaderCommand(commandType, parameters, rawLine!));
  }

  [Fact]
  public void ToString_WithParameters_ReturnsFormattedString()
  {
    // Arrange
    ToReaderCommand command = new ToReaderCommand("KEYWORD", "urgent important", "KEYWORD urgent important");

    // Act
    string result = command.ToString();

    // Assert
    Assert.Equal("KEYWORD urgent important", result);
  }

  [Fact]
  public void ToString_WithoutParameters_ReturnsCommandTypeOnly()
  {
    // Arrange
    ToReaderCommand command = new ToReaderCommand("RESET", string.Empty, "RESET");

    // Act
    string result = command.ToString();

    // Assert
    Assert.Equal("RESET", result);
  }

  [Fact]
  public void Constructor_LongParameters_PreservesFullValue()
  {
    // Arrange
    string commandType = "TWIT";
    string parameters = "user1 user2 user3 user4 user5 user6 user7 user8";
    string rawLine = $"{commandType} {parameters}";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal(parameters, command.Parameters);
    Assert.Equal(rawLine, command.RawLine);
  }

  [Fact]
  public void Constructor_MixedCaseCommandType_PreservesCase()
  {
    // Arrange
    string commandType = "MyCustomCommand";
    string parameters = "param1";
    string rawLine = "MyCustomCommand param1";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("MyCustomCommand", command.CommandType);
  }

  [Fact]
  public void Constructor_SpecialCharactersInParameters_Preserved()
  {
    // Arrange
    string commandType = "FILTER";
    string parameters = "!@#$%^&*()";
    string rawLine = "FILTER !@#$%^&*()";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Equal("!@#$%^&*()", command.Parameters);
  }

  [Fact]
  public void Constructor_TabsInParameters_Preserved()
  {
    // Arrange
    string commandType = "TEST";
    string parameters = "param1\tparam2\tparam3";
    string rawLine = "TEST param1\tparam2\tparam3";

    // Act
    ToReaderCommand command = new ToReaderCommand(commandType, parameters, rawLine);

    // Assert
    Assert.Contains("\t", command.Parameters);
  }
}
