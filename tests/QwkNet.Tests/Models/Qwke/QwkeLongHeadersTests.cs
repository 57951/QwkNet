using System;
using Xunit;
using QwkNet.Models.Qwke;

namespace QwkNet.Tests.Models.Qwke;

public sealed class QwkeLongHeadersTests
{
  [Fact]
  public void Constructor_AllFields_SetsProperties()
  {
    // Arrange
    string extendedTo = "Very Long Recipient Name That Exceeds Twenty Five Characters";
    string extendedFrom = "Very Long Sender Name That Exceeds Twenty Five Characters";
    string extendedSubject = "Very Long Subject Line That Exceeds Twenty Five Characters";

    // Act
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedTo, extendedFrom, extendedSubject);

    // Assert
    Assert.Equal(extendedTo, headers.ExtendedTo);
    Assert.Equal(extendedFrom, headers.ExtendedFrom);
    Assert.Equal(extendedSubject, headers.ExtendedSubject);
  }

  [Fact]
  public void Constructor_NoParameters_AllFieldsNull()
  {
    // Act
    QwkeLongHeaders headers = new QwkeLongHeaders();

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
  }

  [Fact]
  public void Constructor_OnlyExtendedTo_OthersNull()
  {
    // Arrange
    string extendedTo = "Long Recipient Name";

    // Act
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedTo: extendedTo);

    // Assert
    Assert.Equal(extendedTo, headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
  }

  [Fact]
  public void Constructor_OnlyExtendedFrom_OthersNull()
  {
    // Arrange
    string extendedFrom = "Long Sender Name";

    // Act
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedFrom: extendedFrom);

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Equal(extendedFrom, headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
  }

  [Fact]
  public void Constructor_OnlyExtendedSubject_OthersNull()
  {
    // Arrange
    string extendedSubject = "Long Subject Line";

    // Act
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedSubject: extendedSubject);

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Equal(extendedSubject, headers.ExtendedSubject);
  }

  [Fact]
  public void HasLongHeaders_AllFieldsNull_ReturnsFalse()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders();

    // Act
    bool result = headers.HasLongHeaders;

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void HasLongHeaders_OnlyExtendedToSet_ReturnsTrue()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedTo: "Recipient");

    // Act
    bool result = headers.HasLongHeaders;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void HasLongHeaders_OnlyExtendedFromSet_ReturnsTrue()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedFrom: "Sender");

    // Act
    bool result = headers.HasLongHeaders;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void HasLongHeaders_OnlyExtendedSubjectSet_ReturnsTrue()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedSubject: "Subject");

    // Act
    bool result = headers.HasLongHeaders;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void HasLongHeaders_AllFieldsSet_ReturnsTrue()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders("To", "From", "Subject");

    // Act
    bool result = headers.HasLongHeaders;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void HasLongHeaders_EmptyStrings_ReturnsFalse()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(string.Empty, string.Empty, string.Empty);

    // Act
    bool result = headers.HasLongHeaders;

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void Empty_ReturnsInstanceWithNoHeaders()
  {
    // Act
    QwkeLongHeaders headers = QwkeLongHeaders.Empty();

    // Assert
    Assert.Null(headers.ExtendedTo);
    Assert.Null(headers.ExtendedFrom);
    Assert.Null(headers.ExtendedSubject);
    Assert.False(headers.HasLongHeaders);
  }

  [Fact]
  public void ToString_NoHeaders_ReturnsNoLongHeaders()
  {
    // Arrange
    QwkeLongHeaders headers = QwkeLongHeaders.Empty();

    // Act
    string result = headers.ToString();

    // Assert
    Assert.Equal("No long headers", result);
  }

  [Fact]
  public void ToString_OneHeader_Returns1LongHeaders()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedTo: "Recipient");

    // Act
    string result = headers.ToString();

    // Assert
    Assert.Equal("1 long header(s)", result);
  }

  [Fact]
  public void ToString_TwoHeaders_Returns2LongHeaders()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedTo: "Recipient", extendedFrom: "Sender");

    // Act
    string result = headers.ToString();

    // Assert
    Assert.Equal("2 long header(s)", result);
  }

  [Fact]
  public void ToString_ThreeHeaders_Returns3LongHeaders()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders("Recipient", "Sender", "Subject");

    // Act
    string result = headers.ToString();

    // Assert
    Assert.Equal("3 long header(s)", result);
  }

  [Fact]
  public void Constructor_WhitespaceOnlyStrings_PreservedAsIs()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders("   ", "   ", "   ");

    // Act
    bool hasHeaders = headers.HasLongHeaders;

    // Assert
    // Whitespace is preserved as-is (preservation-grade)
    Assert.True(hasHeaders);
    Assert.Equal("   ", headers.ExtendedTo);
    Assert.Equal("   ", headers.ExtendedFrom);
    Assert.Equal("   ", headers.ExtendedSubject);
  }

  [Fact]
  public void Constructor_MixOfNullAndValues_OnlyCountsNonNull()
  {
    // Arrange
    QwkeLongHeaders headers = new QwkeLongHeaders(extendedTo: "Recipient", extendedFrom: null, extendedSubject: "Subject");

    // Act
    string result = headers.ToString();

    // Assert
    Assert.Equal("2 long header(s)", result);
  }
}