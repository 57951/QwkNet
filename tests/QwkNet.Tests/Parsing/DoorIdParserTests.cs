using System;
using System.Linq;
using System.Text;
using QwkNet.Models.Control;
using QwkNet.Parsing;
using QwkNet.Validation;
using Xunit;

namespace QwkNet.Tests.Parsing;

public sealed class DoorIdParserTests
{
  [Fact]
  public void Parse_ValidMinimalDoorId_Success()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TomCat!",
      "VERSION = 2.9"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Strict);

    // Assert
    Assert.Equal("TomCat!", result.DoorName);
    Assert.Equal("2.9", result.Version);
    Assert.Null(result.SystemType);
    Assert.Null(result.ControlName);
  }

  [Fact]
  public void Parse_FullDoorId_ParsesAllFields()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TomCat!",
      "VERSION = 2.9",
      "SYSTEM = Wildcat! 2.x",
      "CONTROLNAME = TOMCAT",
      "CONTROLTYPE = ADD",
      "CONTROLTYPE = DROP"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("TomCat!", result.DoorName);
    Assert.Equal("2.9", result.Version);
    Assert.Equal("Wildcat! 2.x", result.SystemType);
    Assert.Equal("TOMCAT", result.ControlName);
    Assert.Contains(DoorCapability.Add, result.Capabilities);
    Assert.Contains(DoorCapability.Drop, result.Capabilities);
  }

  [Fact]
  public void Parse_WithReceipt_AddsReceiptCapability()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = Qmail",
      "VERSION = 4.0",
      "RECEIPT"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(DoorCapability.Receipt, result.Capabilities);
  }

  [Fact]
  public void Parse_WithMixedCaseYes_AddsMixedCaseCapability()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = Qmail",
      "VERSION = 4.0",
      "MIXEDCASE = YES"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(DoorCapability.MixedCase, result.Capabilities);
  }

  [Fact]
  public void Parse_WithMixedCaseNo_DoesNotAddMixedCaseCapability()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = OldDoor",
      "VERSION = 1.0",
      "MIXEDCASE = NO"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.DoesNotContain(DoorCapability.MixedCase, result.Capabilities);
  }

  [Fact]
  public void Parse_WithFidoTagYes_AddsFidoTagCapability()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = FidoDoor",
      "VERSION = 1.5",
      "FIDOTAG = YES"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(DoorCapability.FidoTag, result.Capabilities);
  }

  [Fact]
  public void Parse_MultipleControlTypes_AddsAllCapabilities()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = FullFeatured",
      "VERSION = 3.0",
      "CONTROLTYPE = ADD",
      "CONTROLTYPE = DROP",
      "CONTROLTYPE = REQUEST",
      "CONTROLTYPE = RESET"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(DoorCapability.Add, result.Capabilities);
    Assert.Contains(DoorCapability.Drop, result.Capabilities);
    Assert.Contains(DoorCapability.Request, result.Capabilities);
    Assert.Contains(DoorCapability.Reset, result.Capabilities);
  }

  [Fact]
  public void Parse_UnknownControlType_AddsUnknownCapability()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = CustomDoor",
      "VERSION = 1.0",
      "CONTROLTYPE = CUSTOMCOMMAND"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(DoorCapability.Unknown, result.Capabilities);
  }

  [Fact]
  public void Parse_PreservesRawEntries_ForRoundTrip()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      "CUSTOMFIELD = CustomValue"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.True(result.RawEntries.ContainsKey("CUSTOMFIELD"));
    Assert.Equal("CustomValue", result.RawEntries["CUSTOMFIELD"]);
  }

  [Fact]
  public void Parse_CaseInsensitiveKeys_ParsesCorrectly()
  {
    // Arrange
    string content = string.Join("\r\n",
      "door = TestDoor",
      "version = 1.0",
      "CONTROLNAME = TESTDOOR"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("TestDoor", result.DoorName);
    Assert.Equal("1.0", result.Version);
    Assert.Equal("TESTDOOR", result.ControlName);
  }

  [Fact]
  public void Parse_EqualsWithoutSpaces_LenientMode_ParsesCorrectly()
  {
    // Arrange - Some implementations may not use spaces around =
    string content = string.Join("\r\n",
      "DOOR=TestDoor",
      "VERSION=1.0"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal("TestDoor", result.DoorName);
    Assert.Equal("1.0", result.Version);
  }

  [Fact]
  public void Parse_InvalidLineFormat_LenientMode_SkipsLine()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      "INVALID LINE WITHOUT EQUALS",
      "SYSTEM = Test"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal("TestDoor", result.DoorName);
    Assert.Equal("Test", result.SystemType);
    Assert.True(context.HasWarnings);
  }

  [Fact]
  public void Parse_MissingDoorField_StrictMode_ThrowsException()
  {
    // Arrange
    string content = string.Join("\r\n",
      "VERSION = 1.0",
      "SYSTEM = Test"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act & Assert
    Assert.Throws<QwkFormatException>(() =>
      DoorIdParser.Parse(data, ValidationMode.Strict));
  }

  [Fact]
  public void Parse_MissingVersionField_StrictMode_ThrowsException()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "SYSTEM = Test"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act & Assert
    Assert.Throws<QwkFormatException>(() =>
      DoorIdParser.Parse(data, ValidationMode.Strict));
  }

  [Fact]
  public void Parse_MissingRequiredField_LenientMode_UsesDefault()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "SYSTEM = Test"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal("TestDoor", result.DoorName);
    Assert.Equal("0.0", result.Version); // Default version
    Assert.True(context.HasErrors);
  }

  [Fact]
  public void Parse_EmptyLines_SkipsCorrectly()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "",
      "VERSION = 1.0",
      "",
      "SYSTEM = Test"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("TestDoor", result.DoorName);
    Assert.Equal("1.0", result.Version);
    Assert.Equal("Test", result.SystemType);
  }

  [Fact]
  public void Parse_DuplicateNonControlTypeKey_KeepsFirstValue()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = FirstDoor",
      "VERSION = 1.0",
      "DOOR = SecondDoor"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal("FirstDoor", result.DoorName);
    Assert.True(context.HasWarnings);
  }

  [Fact]
  public void Parse_NullData_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      DoorIdParser.Parse((byte[])null!, ValidationMode.Lenient));
  }

  [Fact]
  public void Parse_EmptyData_LenientMode_UsesDefaults()
  {
    // Arrange
    byte[] data = Array.Empty<byte>();
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient, context);

    // Assert
    Assert.Equal("Unknown", result.DoorName);
    Assert.Equal("0.0", result.Version);
    Assert.True(context.HasErrors);
  }

  [Fact]
  public void Parse_LineFeedOnlyLineEndings_ParsesCorrectly()
  {
    // Arrange - Unix-style line endings
    string content = string.Join("\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      "SYSTEM = Test"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Equal("TestDoor", result.DoorName);
    Assert.Equal("1.0", result.Version);
    Assert.Equal("Test", result.SystemType);
  }

  [Fact]
  public void ToString_WithSystemType_FormatsCorrectly()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TomCat!",
      "VERSION = 2.9",
      "SYSTEM = Wildcat! 2.x"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Act
    string text = result.ToString();

    // Assert
    Assert.Equal("TomCat! 2.9 (Wildcat! 2.x)", text);
  }

  [Fact]
  public void ToString_WithoutSystemType_FormatsCorrectly()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = SimpleDoor",
      "VERSION = 1.0"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Act
    string text = result.ToString();

    // Assert
    Assert.Equal("SimpleDoor 1.0", text);
  }
}