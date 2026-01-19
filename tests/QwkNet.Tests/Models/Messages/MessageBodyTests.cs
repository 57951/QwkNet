using System;
using System.Collections.Generic;
using Xunit;
using QwkNet.Models.Messages;

namespace QwkNet.Tests.Models.Messages;

public sealed class MessageBodyTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesInstance()
  {
    // Arrange
    List<string> lines = new List<string> { "Line 1", "Line 2", "Line 3" };
    string rawText = "Line 1\u03C0Line 2\u03C0Line 3\u03C0";

    // Act
    MessageBody body = new MessageBody(lines, rawText);

    // Assert
    Assert.Equal(3, body.Lines.Count);
    Assert.Equal("Line 1", body.Lines[0]);
    Assert.Equal("Line 2", body.Lines[1]);
    Assert.Equal("Line 3", body.Lines[2]);
    Assert.Equal(rawText, body.RawText);
  }

  [Fact]
  public void Constructor_WithNullLines_ThrowsArgumentNullException()
  {
    // Arrange
    IReadOnlyList<string>? lines = null;
    string rawText = "test";

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new MessageBody(lines!, rawText));

    Assert.Equal("lines", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullRawText_ThrowsArgumentNullException()
  {
    // Arrange
    List<string> lines = new List<string> { "test" };
    string? rawText = null;

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => new MessageBody(lines, rawText!));

    Assert.Equal("rawText", exception.ParamName);
  }

  [Fact]
  public void FromRawText_WithValidText_ParsesLines()
  {
    // Arrange
    string rawText = "First line\u03C0Second line\u03C0Third line\u03C0";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Equal(3, body.Lines.Count);
    Assert.Equal("First line", body.Lines[0]);
    Assert.Equal("Second line", body.Lines[1]);
    Assert.Equal("Third line", body.Lines[2]);
    Assert.Equal(rawText, body.RawText);
  }

  [Fact]
  public void FromRawText_WithTrailingPadding_RemovesPadding()
  {
    // Arrange
    string rawText = "Line 1\u03C0Line 2\u03C0     ";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Equal(2, body.Lines.Count);
    Assert.Equal("Line 1", body.Lines[0]);
    Assert.Equal("Line 2", body.Lines[1]);
  }

  [Fact]
  public void FromRawText_WithNullPadding_RemovesPadding()
  {
    // Arrange
    string rawText = "Line 1\u03C0Line 2\u03C0\0\0\0\0\0";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Equal(2, body.Lines.Count);
    Assert.Equal("Line 1", body.Lines[0]);
    Assert.Equal("Line 2", body.Lines[1]);
  }

  [Fact]
  public void FromRawText_WithMixedPadding_RemovesPadding()
  {
    // Arrange
    string rawText = "Test\u03C0  \0 \0  ";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Single(body.Lines);
    Assert.Equal("Test", body.Lines[0]);
  }

  [Fact]
  public void FromRawText_WithEmptyLines_PreservesEmptyLines()
  {
    // Arrange
    string rawText = "Line 1\u03C0\u03C0Line 3\u03C0";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Equal(3, body.Lines.Count);
    Assert.Equal("Line 1", body.Lines[0]);
    Assert.Equal(string.Empty, body.Lines[1]);
    Assert.Equal("Line 3", body.Lines[2]);
  }

  [Fact]
  public void FromRawText_WithNoTerminators_ReturnsSingleLine()
  {
    // Arrange
    string rawText = "Single line without terminators";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Single(body.Lines);
    Assert.Equal("Single line without terminators", body.Lines[0]);
  }

  [Fact]
  public void FromRawText_WithOnlyPadding_ReturnsEmptyLines()
  {
    // Arrange
    string rawText = "     ";

    // Act
    MessageBody body = MessageBody.FromRawText(rawText);

    // Assert
    Assert.Empty(body.Lines);
  }

  [Fact]
  public void FromRawText_WithNullText_ThrowsArgumentNullException()
  {
    // Arrange
    string? rawText = null;

    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
      () => MessageBody.FromRawText(rawText!));

    Assert.Equal("rawText", exception.ParamName);
  }

  [Fact]
  public void GetDecodedText_JoinsLinesWithNewLine()
  {
    // Arrange
    List<string> lines = new List<string> { "Line 1", "Line 2", "Line 3" };
    string rawText = "Line 1\u03C0Line 2\u03C0Line 3\u03C0";
    MessageBody body = new MessageBody(lines, rawText);

    // Act
    string decoded = body.GetDecodedText();

    // Assert
    string expected = string.Join(Environment.NewLine, lines);
    Assert.Equal(expected, decoded);
  }

  [Fact]
  public void GetDecodedText_WithEmptyLines_PreservesEmptyLines()
  {
    // Arrange
    List<string> lines = new List<string> { "Line 1", "", "Line 3" };
    string rawText = "Line 1\u03C0\u03C0Line 3\u03C0";
    MessageBody body = new MessageBody(lines, rawText);

    // Act
    string decoded = body.GetDecodedText();

    // Assert
    Assert.Contains(Environment.NewLine + Environment.NewLine, decoded);
  }

  [Fact]
  public void GetEncodedText_EncodesWithQwkTerminators()
  {
    // Arrange
    List<string> lines = new List<string> { "Line 1", "Line 2", "Line 3" };
    string rawText = "original";
    MessageBody body = new MessageBody(lines, rawText);

    // Act
    string encoded = body.GetEncodedText();

    // Assert
    Assert.Equal("Line 1\u03C0Line 2\u03C0Line 3", encoded);
  }

  [Fact]
  public void GetEncodedText_WithSingleLine_NoTerminator()
  {
    // Arrange
    List<string> lines = new List<string> { "Single line" };
    string rawText = "Single line";
    MessageBody body = new MessageBody(lines, rawText);

    // Act
    string encoded = body.GetEncodedText();

    // Assert
    Assert.Equal("Single line", encoded);
    Assert.DoesNotContain("\u03C0", encoded);
  }

  [Fact]
  public void GetEncodedText_WithEmptyLines_PreservesEmptyLines()
  {
    // Arrange
    List<string> lines = new List<string> { "Line 1", "", "Line 3" };
    string rawText = "original";
    MessageBody body = new MessageBody(lines, rawText);

    // Act
    string encoded = body.GetEncodedText();

    // Assert
    Assert.Equal("Line 1\u03C0\u03C0Line 3", encoded);
  }

  [Fact]
  public void ToString_ReturnsLineSummary()
  {
    // Arrange
    List<string> lines = new List<string> { "A", "B", "C" };
    MessageBody body = new MessageBody(lines, "A\u03C0B\u03C0C\u03C0");

    // Act
    string result = body.ToString();

    // Assert
    Assert.Contains("3 line(s)", result);
  }

  [Fact]
  public void RoundTrip_FromRawTextAndGetEncodedText_PreservesContent()
  {
    // Arrange
    string original = "Line 1\u03C0Line 2\u03C0Line 3";

    // Act
    MessageBody body = MessageBody.FromRawText(original);
    string encoded = body.GetEncodedText();

    // Assert
    Assert.Equal(original, encoded);
  }
}