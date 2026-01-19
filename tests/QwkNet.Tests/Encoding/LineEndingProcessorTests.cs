using System;
using Xunit;
using QwkNet.Encoding;

namespace QwkNet.Tests.Encoding;

/// <summary>
/// Unit tests for <see cref="LineEndingProcessor"/>.
/// </summary>
public class LineEndingProcessorTests
{
  [Fact]
  public void QwkLineTerminator_HasCorrectValue()
  {
    // Assert
    Assert.Equal((char)0xE3, LineEndingProcessor.QwkLineTerminator);
  }

  [Fact]
  public void ConvertToQwkFormat_WithCrLfLineEndings_ConvertsToQwkTerminator()
  {
    // Arrange
    string text = "Line 1\r\nLine 2\r\nLine 3";

    // Act
    string result = LineEndingProcessor.ConvertToQwkFormat(text);

    // Assert
    Assert.Equal("Line 1\u00E3Line 2\u00E3Line 3", result);
  }

  [Fact]
  public void ConvertToQwkFormat_WithLfLineEndings_ConvertsToQwkTerminator()
  {
    // Arrange
    string text = "Line 1\nLine 2\nLine 3";

    // Act
    string result = LineEndingProcessor.ConvertToQwkFormat(text);

    // Assert
    Assert.Equal("Line 1\u00E3Line 2\u00E3Line 3", result);
  }

  [Fact]
  public void ConvertToQwkFormat_WithCrLineEndings_ConvertsToQwkTerminator()
  {
    // Arrange
    string text = "Line 1\rLine 2\rLine 3";

    // Act
    string result = LineEndingProcessor.ConvertToQwkFormat(text);

    // Assert
    Assert.Equal("Line 1\u00E3Line 2\u00E3Line 3", result);
  }

  [Fact]
  public void ConvertToQwkFormat_WithMixedLineEndings_ConvertsAllToQwkTerminator()
  {
    // Arrange - Mixed CRLF, LF, and CR
    string text = "Line 1\r\nLine 2\nLine 3\rLine 4";

    // Act
    string result = LineEndingProcessor.ConvertToQwkFormat(text);

    // Assert
    Assert.Equal("Line 1\u00E3Line 2\u00E3Line 3\u00E3Line 4", result);
  }

  [Fact]
  public void ConvertToQwkFormat_WithEmptyString_ReturnsEmptyString()
  {
    // Arrange
    string text = string.Empty;

    // Act
    string result = LineEndingProcessor.ConvertToQwkFormat(text);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void ConvertFromQwkFormat_Preserve_ConvertsToNativeLineEndings()
  {
    // Arrange
    string text = "Line 1\u00E3Line 2\u00E3Line 3";

    // Act
    string result = LineEndingProcessor.ConvertFromQwkFormat(text, LineEndingMode.Preserve);

    // Assert
    Assert.Contains("Line 1", result);
    Assert.Contains("Line 2", result);
    Assert.Contains("Line 3", result);
    // Should contain Environment.NewLine
    Assert.Contains(Environment.NewLine, result);
  }

  [Fact]
  public void ConvertFromQwkFormat_NormaliseToLf_ConvertsToLf()
  {
    // Arrange
    string text = "Line 1\u00E3Line 2\u00E3Line 3";

    // Act
    string result = LineEndingProcessor.ConvertFromQwkFormat(text, LineEndingMode.NormaliseToLf);

    // Assert
    Assert.Equal("Line 1\nLine 2\nLine 3", result);
  }

  [Fact]
  public void ConvertFromQwkFormat_NormaliseToCrLf_ConvertsToCrLf()
  {
    // Arrange
    string text = "Line 1\u00E3Line 2\u00E3Line 3";

    // Act
    string result = LineEndingProcessor.ConvertFromQwkFormat(text, LineEndingMode.NormaliseToCrLf);

    // Assert
    Assert.Equal("Line 1\r\nLine 2\r\nLine 3", result);
  }

  [Fact]
  public void ConvertFromQwkFormat_StrictQwk_OnlyConvertsQwkTerminators()
  {
    // Arrange - Text with QWK terminator and literal CR/LF
    string text = "Line 1\u00E3Line 2";

    // Act
    string result = LineEndingProcessor.ConvertFromQwkFormat(text, LineEndingMode.StrictQwk);

    // Assert
    Assert.Contains("Line 1", result);
    Assert.Contains("Line 2", result);
    // Should contain Environment.NewLine for the QWK terminator
  }

  [Fact]
  public void NormaliseToLf_WithCrLf_ConvertsToLf()
  {
    // Arrange
    string text = "Line 1\r\nLine 2\r\nLine 3";

    // Act
    string result = LineEndingProcessor.NormaliseToLf(text);

    // Assert
    Assert.Equal("Line 1\nLine 2\nLine 3", result);
  }

  [Fact]
  public void NormalizeToLf_AmericanSpelling_WorksIdentically()
  {
    // Arrange
    string text = "Line 1\r\nLine 2\r\nLine 3";

    // Act
    string result = LineEndingProcessor.NormalizeToLf(text);

    // Assert
    Assert.Equal("Line 1\nLine 2\nLine 3", result);
  }

  [Fact]
  public void NormaliseToCrLf_WithLf_ConvertsToCrLf()
  {
    // Arrange
    string text = "Line 1\nLine 2\nLine 3";

    // Act
    string result = LineEndingProcessor.NormaliseToCrLf(text);

    // Assert
    Assert.Equal("Line 1\r\nLine 2\r\nLine 3", result);
  }

  [Fact]
  public void NormalizeToCrLf_AmericanSpelling_WorksIdentically()
  {
    // Arrange
    string text = "Line 1\nLine 2\nLine 3";

    // Act
    string result = LineEndingProcessor.NormalizeToCrLf(text);

    // Assert
    Assert.Equal("Line 1\r\nLine 2\r\nLine 3", result);
  }

  [Fact]
  public void SplitOnQwkTerminator_WithMultipleLines_ReturnsLineArray()
  {
    // Arrange
    string text = "Line 1\u00E3Line 2\u00E3Line 3";

    // Act
    string[] result = LineEndingProcessor.SplitOnQwkTerminator(text);

    // Assert
    Assert.Equal(3, result.Length);
    Assert.Equal("Line 1", result[0]);
    Assert.Equal("Line 2", result[1]);
    Assert.Equal("Line 3", result[2]);
  }

  [Fact]
  public void SplitOnQwkTerminator_WithEmptyLines_PreservesEmptyLines()
  {
    // Arrange
    string text = "Line 1\u00E3\u00E3Line 3";

    // Act
    string[] result = LineEndingProcessor.SplitOnQwkTerminator(text);

    // Assert
    Assert.Equal(3, result.Length);
    Assert.Equal("Line 1", result[0]);
    Assert.Equal(string.Empty, result[1]);
    Assert.Equal("Line 3", result[2]);
  }

  [Fact]
  public void SplitOnQwkTerminator_WithRemoveEmpty_RemovesEmptyLines()
  {
    // Arrange
    string text = "Line 1\u00E3\u00E3Line 3";

    // Act
    string[] result = LineEndingProcessor.SplitOnQwkTerminator(text, removeEmpty: true);

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal("Line 1", result[0]);
    Assert.Equal("Line 3", result[1]);
  }

  [Fact]
  public void JoinWithQwkTerminator_WithMultipleLines_JoinsWithTerminator()
  {
    // Arrange
    string[] lines = new string[] { "Line 1", "Line 2", "Line 3" };

    // Act
    string result = LineEndingProcessor.JoinWithQwkTerminator(lines);

    // Assert
    Assert.Equal("Line 1\u00E3Line 2\u00E3Line 3", result);
  }

  [Fact]
  public void JoinWithQwkTerminator_WithNullInput_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      LineEndingProcessor.JoinWithQwkTerminator(null!));
  }

  [Fact]
  public void RoundTrip_ConvertToQwkAndBack_PreservesLineStructure()
  {
    // Arrange
    string original = "Line 1\r\nLine 2\nLine 3";

    // Act
    string qwkFormat = LineEndingProcessor.ConvertToQwkFormat(original);
    string result = LineEndingProcessor.ConvertFromQwkFormat(qwkFormat, LineEndingMode.NormaliseToLf);

    // Assert
    Assert.Equal("Line 1\nLine 2\nLine 3", result);
  }

  [Fact]
  public void ConvertToQwkFormatBytes_WithText_ReturnsEncodedBytes()
  {
    // Arrange
    string text = "Hello\r\nWorld";

    // Act
    byte[] result = LineEndingProcessor.ConvertToQwkFormatBytes(text);

    // Assert
    Assert.NotEmpty(result);
    // Should contain QWK terminator byte (0xE3)
    Assert.Contains((byte)0xE3, result);
  }

  [Fact]
  public void ConvertToQwkFormatBytes_WithEmptyString_ReturnsEmptyArray()
  {
    // Arrange
    string text = string.Empty;

    // Act
    byte[] result = LineEndingProcessor.ConvertToQwkFormatBytes(text);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void ConvertFromQwkFormat_WithEmptyString_ReturnsEmptyString()
  {
    // Arrange
    string text = string.Empty;

    // Act
    string result = LineEndingProcessor.ConvertFromQwkFormat(text);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void NormaliseToLf_WithMixedEndings_NormalisesAll()
  {
    // Arrange
    string text = "Line 1\r\nLine 2\rLine 3\nLine 4";

    // Act
    string result = LineEndingProcessor.NormaliseToLf(text);

    // Assert
    Assert.Equal("Line 1\nLine 2\nLine 3\nLine 4", result);
  }

  [Fact]
  public void NormaliseToCrLf_WithMixedEndings_NormalisesAll()
  {
    // Arrange
    string text = "Line 1\r\nLine 2\rLine 3\nLine 4";

    // Act
    string result = LineEndingProcessor.NormaliseToCrLf(text);

    // Assert
    Assert.Equal("Line 1\r\nLine 2\r\nLine 3\r\nLine 4", result);
  }
}
