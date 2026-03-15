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

  // -------------------------------------------------------------------------
  // New capability round-trip tests (Part A / B)
  // -------------------------------------------------------------------------

  [Theory]
  [InlineData("RESETALL", DoorCapability.ResetAll)]
  [InlineData("YOURS", DoorCapability.Yours)]
  [InlineData("MAIL", DoorCapability.Mail)]
  [InlineData("DELMAIL", DoorCapability.DeleteMail)]
  [InlineData("ATTACH", DoorCapability.Attach)]
  [InlineData("OWN", DoorCapability.Own)]
  [InlineData("FREQ", DoorCapability.FileRequest)]
  [InlineData("NDX", DoorCapability.Index)]
  [InlineData("TZ", DoorCapability.TimeZone)]
  [InlineData("VIA", DoorCapability.Via)]
  [InlineData("MSGID", DoorCapability.MessageId)]
  [InlineData("CONTROL", DoorCapability.Control)]
  public void Parse_NewControlTypeValues_MapsToCorrectCapability(string controlTypeValue, DoorCapability expectedCapability)
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      $"CONTROLTYPE = {controlTypeValue}"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(expectedCapability, result.Capabilities);
  }

  [Theory]
  [InlineData("resetall", DoorCapability.ResetAll)]
  [InlineData("yours", DoorCapability.Yours)]
  [InlineData("mail", DoorCapability.Mail)]
  [InlineData("delmail", DoorCapability.DeleteMail)]
  [InlineData("attach", DoorCapability.Attach)]
  [InlineData("own", DoorCapability.Own)]
  [InlineData("freq", DoorCapability.FileRequest)]
  [InlineData("ndx", DoorCapability.Index)]
  [InlineData("tz", DoorCapability.TimeZone)]
  [InlineData("via", DoorCapability.Via)]
  [InlineData("msgid", DoorCapability.MessageId)]
  [InlineData("control", DoorCapability.Control)]
  public void Parse_NewControlTypeValues_CaseInsensitive_MapsToCorrectCapability(string controlTypeValue, DoorCapability expectedCapability)
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      $"CONTROLTYPE = {controlTypeValue}"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Contains(expectedCapability, result.Capabilities);
  }

  // -------------------------------------------------------------------------
  // ControlTypes list tests (Part C)
  // -------------------------------------------------------------------------

  [Fact]
  public void Parse_MultipleControlTypes_ControlTypesListPreservesOrderAndCasing()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      "CONTROLTYPE = ADD",
      "CONTROLTYPE = drop",
      "CONTROLTYPE = ResetAll",
      "CONTROLTYPE = YOURS"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert - order preserved
    Assert.Equal(4, result.ControlTypes.Count);
    Assert.Equal("ADD", result.ControlTypes[0]);
    Assert.Equal("drop", result.ControlTypes[1]);
    Assert.Equal("ResetAll", result.ControlTypes[2]);
    Assert.Equal("YOURS", result.ControlTypes[3]);
  }

  [Fact]
  public void Parse_NoControlTypes_ControlTypesListIsEmpty()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert
    Assert.Empty(result.ControlTypes);
  }

  [Fact]
  public void Parse_UnknownControlType_AppearsInControlTypesListButMapsToUnknown()
  {
    // Arrange
    string content = string.Join("\r\n",
      "DOOR = TestDoor",
      "VERSION = 1.0",
      "CONTROLTYPE = FILES",
      "CONTROLTYPE = CTRL-A"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert - raw values are preserved
    Assert.Equal(2, result.ControlTypes.Count);
    Assert.Equal("FILES", result.ControlTypes[0]);
    Assert.Equal("CTRL-A", result.ControlTypes[1]);

    // Both map to Unknown in the capability set
    Assert.Contains(DoorCapability.Unknown, result.Capabilities);
  }

  // -------------------------------------------------------------------------
  // Synchronet 3.21 real-world fixture test (Part D)
  // -------------------------------------------------------------------------

  [Fact]
  public void Parse_Synchronet321RealWorldDoorId_ParsesCorrectly()
  {
    // Arrange - exact content as found in a Synchronet 3.21a QWK packet
    string content = string.Join("\r\n",
      "DOOR = Synchronet",
      "VERSION = 3.21a",
      "SYSTEM = Synchronet BBS for Win32  Version 3.21a",
      "CONTROLNAME = SBBS",
      "CONTROLTYPE = ADD",
      "CONTROLTYPE = DROP",
      "CONTROLTYPE = YOURS",
      "CONTROLTYPE = RESET",
      "CONTROLTYPE = RESETALL",
      "CONTROLTYPE = FILES",
      "CONTROLTYPE = ATTACH",
      "CONTROLTYPE = OWN",
      "CONTROLTYPE = MAIL",
      "CONTROLTYPE = DELMAIL",
      "CONTROLTYPE = CTRL-A",
      "CONTROLTYPE = FREQ",
      "CONTROLTYPE = NDX",
      "CONTROLTYPE = TZ",
      "CONTROLTYPE = VIA",
      "CONTROLTYPE = MSGID",
      "CONTROLTYPE = CONTROL",
      "MIXEDCASE = YES"
    );
    byte[] data = System.Text.Encoding.ASCII.GetBytes(content);

    // Act
    DoorId result = DoorIdParser.Parse(data, ValidationMode.Lenient);

    // Assert - standard fields
    Assert.Equal("Synchronet", result.DoorName);
    Assert.Equal("3.21a", result.Version);
    Assert.Equal("Synchronet BBS for Win32  Version 3.21a", result.SystemType);
    Assert.Equal("SBBS", result.ControlName);

    // Assert - known capabilities
    Assert.Contains(DoorCapability.Add, result.Capabilities);
    Assert.Contains(DoorCapability.Drop, result.Capabilities);
    Assert.Contains(DoorCapability.Yours, result.Capabilities);
    Assert.Contains(DoorCapability.Reset, result.Capabilities);
    Assert.Contains(DoorCapability.ResetAll, result.Capabilities);
    Assert.Contains(DoorCapability.Attach, result.Capabilities);
    Assert.Contains(DoorCapability.Own, result.Capabilities);
    Assert.Contains(DoorCapability.Mail, result.Capabilities);
    Assert.Contains(DoorCapability.DeleteMail, result.Capabilities);
    Assert.Contains(DoorCapability.FileRequest, result.Capabilities);
    Assert.Contains(DoorCapability.Index, result.Capabilities);
    Assert.Contains(DoorCapability.TimeZone, result.Capabilities);
    Assert.Contains(DoorCapability.Via, result.Capabilities);
    Assert.Contains(DoorCapability.MessageId, result.Capabilities);
    Assert.Contains(DoorCapability.Control, result.Capabilities);
    Assert.Contains(DoorCapability.MixedCase, result.Capabilities);

    // FILES and CTRL-A are not in the enum so they map to Unknown
    Assert.Contains(DoorCapability.Unknown, result.Capabilities);

    // Assert - ControlTypes list preserves order and casing
    string[] expectedControlTypes =
    [
      "ADD", "DROP", "YOURS", "RESET", "RESETALL",
      "FILES", "ATTACH", "OWN", "MAIL", "DELMAIL",
      "CTRL-A", "FREQ", "NDX", "TZ", "VIA", "MSGID", "CONTROL"
    ];
    Assert.Equal(expectedControlTypes.Length, result.ControlTypes.Count);
    for (int i = 0; i < expectedControlTypes.Length; i++)
    {
      Assert.Equal(expectedControlTypes[i], result.ControlTypes[i]);
    }
  }
}