using System;
using System.Text;
using QwkNet.Models.Control;
using QwkNet.Parsing;
using QwkNet.Validation;
using Xunit;

namespace QwkNet.Tests.Parsing;

public sealed class ControlDatParserTests
{
  [Fact]
  public void Parse_ValidMinimalControlDat_Success()
  {
    // Arrange
    string content = string.Join("\r\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "00000,TESTBBS",
      "01-15-92,13:45:00",
      "JOHN DOE",
      "",
      "0",
      "5",
      "1",
      "0",
      "Main Board",
      "1",
      "General"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Strict);

    // Assert
    Assert.Equal("Test BBS", result.BbsName);
    Assert.Equal("Seattle, WA", result.BbsCity);
    Assert.Equal("206-555-1212", result.BbsPhone);
    Assert.Equal("Joe Sysop", result.Sysop);
    Assert.Equal("00000", result.RegistrationNumber);
    Assert.Equal("TESTBBS", result.BbsId);
    Assert.Equal("JOHN DOE", result.UserName);
    Assert.Equal("", result.QmailMenuFile);
    Assert.Equal((ushort)0, result.NetMailConference);
    Assert.Equal(5, result.TotalMessages);
    Assert.Equal(1, result.ConferenceCountMinusOne);
    Assert.Equal(2, result.Conferences.Count);
    Assert.Equal((ushort)0, result.Conferences[0].Number);
    Assert.Equal("Main Board", result.Conferences[0].Name);
    Assert.Equal((ushort)1, result.Conferences[1].Number);
    Assert.Equal("General", result.Conferences[1].Name);
  }

  [Fact]
  public void Parse_WithOptionalFiles_ParsesCorrectly()
  {
    // Arrange
    string content = string.Join("\r\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "20052,TESTBBS",
      "01-01-91,23:59:59",
      "JOHN DOE",
      "MENU",
      "0",
      "10",
      "0",
      "0",
      "Main Board",
      "HELLO",
      "NEWS",
      "GOODBYE"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("MENU", result.QmailMenuFile);
    Assert.Equal("HELLO", result.WelcomeFile);
    Assert.Equal("NEWS", result.NewsFile);
    Assert.Equal("GOODBYE", result.GoodbyeFile);
  }

  [Fact]
  public void Parse_DateWithTwoDigitYear_InterpretsCorrectly()
  {
    // Arrange - Y2K heuristic: 00-49 = 2000-2049, 50-99 = 1950-1999
    string content = CreateMinimalControlDat("12-25-25,14:30:00"); // Should be 2025
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(2025, result.CreatedAt.Year);
    Assert.Equal(12, result.CreatedAt.Month);
    Assert.Equal(25, result.CreatedAt.Day);
    Assert.Equal(14, result.CreatedAt.Hour);
    Assert.Equal(30, result.CreatedAt.Minute);
  }

  [Fact]
  public void Parse_DateWithTwoDigitYear1990s_InterpretsCorrectly()
  {
    // Arrange
    string content = CreateMinimalControlDat("01-01-92,00:00:00"); // Should be 1992
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(1992, result.CreatedAt.Year);
  }

  [Fact]
  public void Parse_Conference0_AllowedPerSpecification()
  {
    // Arrange - Conference 0 is explicitly permitted by spec
    string content = CreateMinimalControlDat("01-15-92,13:45:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Strict);

    // Assert
    Assert.Single(result.Conferences);
    Assert.Equal((ushort)0, result.Conferences[0].Number);
  }

  [Fact]
  public void Parse_MultipleConferences_ParsesCorrectly()
  {
    // Arrange
    string content = string.Join("\r\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "00000,TESTBBS",
      "01-15-92,13:45:00",
      "JOHN DOE",
      "",
      "0",
      "15",
      "4", // 5 conferences total
      "0",
      "Main Board",
      "1",
      "General",
      "2",
      "Programming",
      "3",
      "Hardware",
      "4",
      "Software"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(5, result.Conferences.Count);
    Assert.Equal("Programming", result.Conferences[2].Name);
    Assert.Equal((ushort)3, result.Conferences[3].Number);
  }

  [Fact]
  public void Parse_MalformedDate_LenientMode_UsesMinValue()
  {
    // Arrange
    string content = CreateMinimalControlDat("INVALID-DATE");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal(DateTimeOffset.MinValue, result.CreatedAt);
    Assert.True(context.HasErrors);
  }

  [Fact]
  public void Parse_MalformedDate_StrictMode_ThrowsException()
  {
    // Arrange
    string content = CreateMinimalControlDat("INVALID-DATE");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act & Assert
    Assert.Throws<QwkFormatException>(() =>
      ControlDatParser.Parse(data, ValidationMode.Strict));
  }

  [Fact]
  public void Parse_MissingRequiredField_StrictMode_ThrowsException()
  {
    // Arrange - Only 3 lines instead of minimum required
    string content = string.Join("\r\n", "Test BBS", "Seattle, WA", "206-555-1212");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act & Assert
    Assert.Throws<QwkFormatException>(() =>
      ControlDatParser.Parse(data, ValidationMode.Strict));
  }

  [Fact]
  public void Parse_MissingRequiredField_LenientMode_UsesDefaults()
  {
    // Arrange
    string content = string.Join("\r\n", "Test BBS", "Seattle, WA", "206-555-1212");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test BBS", result.BbsName);
    Assert.True(context.HasErrors);
  }

  [Fact]
  public void Parse_PreservesRawLines_ForRoundTrip()
  {
    // Arrange
    string content = CreateMinimalControlDat("01-15-92,13:45:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.NotEmpty(result.RawLines);
    Assert.Contains("Test BBS", result.RawLines);
  }

  [Fact]
  public void Parse_InvalidConferenceNumber_LenientMode_UsesZero()
  {
    // Arrange
    string content = string.Join("\r\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "00000,TESTBBS",
      "01-15-92,13:45:00",
      "JOHN DOE",
      "",
      "0",
      "5",
      "0",
      "INVALID",
      "Main Board"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Single(result.Conferences);
    Assert.Equal((ushort)0, result.Conferences[0].Number);
    Assert.True(context.HasWarnings);
  }

  [Fact]
  public void Parse_NullData_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      ControlDatParser.Parse((byte[])null!, ValidationMode.Lenient));
  }

  [Fact]
  public void Parse_EmptyData_StrictMode_ThrowsException()
  {
    // Arrange
    byte[] data = Array.Empty<byte>();

    // Act & Assert
    Assert.Throws<QwkFormatException>(() =>
      ControlDatParser.Parse(data, ValidationMode.Strict));
  }

  [Fact]
  public void Parse_DateWithoutSeconds_ParsesCorrectly()
  {
    // Arrange - Some implementations may omit seconds
    string content = CreateMinimalControlDat("01-15-92,13:45");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal(1992, result.CreatedAt.Year);
    Assert.Equal(13, result.CreatedAt.Hour);
    Assert.Equal(45, result.CreatedAt.Minute);
    Assert.Equal(0, result.CreatedAt.Second);
  }

  [Fact]
  public void Parse_BbsIdWithWhitespace_Trims()
  {
    // Arrange
    string content = string.Join("\r\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "00000,  TESTBBS  ", // Whitespace around BBS ID
      "01-15-92,13:45:00",
      "JOHN DOE",
      "",
      "0",
      "5",
      "0",
      "0",
      "Main Board"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("TESTBBS", result.BbsId);
  }

  [Fact]
  public void Parse_LineFeedOnlyLineEndings_ParsesCorrectly()
  {
    // Arrange - Unix-style line endings
    string content = string.Join("\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "00000,TESTBBS",
      "01-15-92,13:45:00",
      "JOHN DOE",
      "",
      "0",
      "5",
      "0",
      "0",
      "Main Board"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("Test BBS", result.BbsName);
    Assert.Equal("TESTBBS", result.BbsId);
  }

  [Fact]
  public void Parse_DateWithSlashDelimiterTwoDigitYear_ParsesCorrectly()
  {
    // Arrange - Slash-delimited date with 2-digit year (MM/DD/YY format)
    string content = CreateMinimalControlDat("12/31/93,14:30:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(1993, result.CreatedAt.Year);
    Assert.Equal(12, result.CreatedAt.Month);
    Assert.Equal(31, result.CreatedAt.Day);
    Assert.Equal(14, result.CreatedAt.Hour);
    Assert.Equal(30, result.CreatedAt.Minute);
  }

  [Fact]
  public void Parse_DateWithSlashDelimiterFourDigitYear_ParsesCorrectly()
  {
    // Arrange - Slash-delimited date with 4-digit year (mvt2.qwk format)
    string content = CreateMinimalControlDat("08/01/1994,00:24:32");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(1994, result.CreatedAt.Year);
    Assert.Equal(8, result.CreatedAt.Month);
    Assert.Equal(1, result.CreatedAt.Day);
    Assert.Equal(0, result.CreatedAt.Hour);
    Assert.Equal(24, result.CreatedAt.Minute);
    Assert.Equal(32, result.CreatedAt.Second);
  }

  [Fact]
  public void Parse_DateWithHyphenDelimiterFourDigitYear_ParsesCorrectly()
  {
    // Arrange - Hyphen-delimited date with 4-digit year
    string content = CreateMinimalControlDat("12-31-1993,23:59:59");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(1993, result.CreatedAt.Year);
    Assert.Equal(12, result.CreatedAt.Month);
    Assert.Equal(31, result.CreatedAt.Day);
    Assert.Equal(23, result.CreatedAt.Hour);
    Assert.Equal(59, result.CreatedAt.Minute);
    Assert.Equal(59, result.CreatedAt.Second);
  }

  [Fact]
  public void Parse_DateWithYear1980_ParsesCorrectly()
  {
    // Arrange - Earliest valid year (BBS era began ~1980)
    string content = CreateMinimalControlDat("01-01-1980,00:00:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(1980, result.CreatedAt.Year);
    Assert.Equal(1, result.CreatedAt.Month);
    Assert.Equal(1, result.CreatedAt.Day);
  }

  [Fact]
  public void Parse_DateWithYear2099_ParsesCorrectly()
  {
    // Arrange - Latest valid year (future margin)
    string content = CreateMinimalControlDat("12-31-2099,23:59:59");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal(2099, result.CreatedAt.Year);
    Assert.Equal(12, result.CreatedAt.Month);
    Assert.Equal(31, result.CreatedAt.Day);
  }

  [Fact]
  public void Parse_DateWithYear1979_RecordsError()
  {
    // Arrange - Year before BBS era (pre-1980)
    string content = CreateMinimalControlDat("12-31-1979,23:59:59");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal(DateTimeOffset.MinValue, result.CreatedAt);
    Assert.True(context.HasErrors);
    Assert.Contains(context.Issues, i => i.Message.Contains("Year out of valid range"));
  }

  [Fact]
  public void Parse_DateWithYear2100_RecordsError()
  {
    // Arrange - Year after valid range (post-2099)
    string content = CreateMinimalControlDat("01-01-2100,00:00:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal(DateTimeOffset.MinValue, result.CreatedAt);
    Assert.True(context.HasErrors);
    Assert.Contains(context.Issues, i => i.Message.Contains("Year out of valid range"));
  }

  [Fact]
  public void Parse_DateWithInvalidDelimiter_RecordsError()
  {
    // Arrange - Invalid delimiter (dot instead of hyphen/slash)
    string content = CreateMinimalControlDat("12.31.1993,14:30:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal(DateTimeOffset.MinValue, result.CreatedAt);
    Assert.True(context.HasErrors);
    Assert.Contains(context.Issues, i => i.Message.Contains("Invalid date format"));
  }

  [Fact]
  public void Parse_DateWithMixedDelimiters_RecordsError()
  {
    // Arrange - Inconsistent delimiters (hyphen and slash mixed)
    string content = CreateMinimalControlDat("12-31/1993,14:30:00");
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    ControlDat result = ControlDatParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal(DateTimeOffset.MinValue, result.CreatedAt);
    Assert.True(context.HasErrors);
  }

  private static string CreateMinimalControlDat(string dateTime)
  {
    return string.Join("\r\n",
      "Test BBS",
      "Seattle, WA",
      "206-555-1212",
      "Joe Sysop",
      "00000,TESTBBS",
      dateTime,
      "JOHN DOE",
      "",
      "0",
      "5",
      "0",
      "0",
      "Main Board"
    );
  }
}