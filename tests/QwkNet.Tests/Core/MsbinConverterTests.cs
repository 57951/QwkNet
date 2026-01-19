using System;
using Xunit;
using QwkNet.Core;

namespace QwkNet.Tests.Core;

/// <summary>
/// Tests for <see cref="MsbinConverter"/>.
/// </summary>
public sealed class MsbinConverterTests
{
  [Fact]
  public void ToDouble_WithZeroBytes_ReturnsZero()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act
    double result = MsbinConverter.ToDouble(msbinBytes);

    // Assert
    Assert.Equal(0.0, result);
  }

  [Fact]
  public void ToDouble_WithKnownValue128_ReturnsCorrectValue()
  {
    // Arrange
    // MSBIN representation of 128.0
    // Exponent: 128 = 2^7, so exponent byte = 7 + 129 = 136 (0x88)
    // Mantissa: 1.0 normalized, so mantissa bits = 0
    byte[] msbinBytes = new byte[] { 0x88, 0x00, 0x00, 0x00 };

    // Act
    double result = MsbinConverter.ToDouble(msbinBytes);

    // Assert
    Assert.Equal(128.0, result, precision: 5);
  }

  [Fact]
  public void ToDouble_WithKnownValue256_ReturnsCorrectValue()
  {
    // Arrange
    // MSBIN representation of 256.0
    // Exponent: 256 = 2^8, so exponent byte = 8 + 129 = 137 (0x89)
    byte[] msbinBytes = new byte[] { 0x89, 0x00, 0x00, 0x00 };

    // Act
    double result = MsbinConverter.ToDouble(msbinBytes);

    // Assert
    Assert.Equal(256.0, result, precision: 5);
  }

  [Fact]
  public void ToDouble_WithNegativeValue_ReturnsNegative()
  {
    // Arrange
    // MSBIN representation of -128.0
    // Same as 128.0 but with sign bit set (bit 7 of byte 3)
    byte[] msbinBytes = new byte[] { 0x88, 0x00, 0x00, 0x80 };

    // Act
    double result = MsbinConverter.ToDouble(msbinBytes);

    // Assert
    Assert.Equal(-128.0, result, precision: 5);
  }

  [Fact]
  public void ToDouble_WithInvalidLength_ThrowsArgumentException()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00 }; // Only 3 bytes

    // Act & Assert
    ArgumentException ex = Assert.Throws<ArgumentException>(() =>
      MsbinConverter.ToDouble(msbinBytes));
    
    Assert.Contains("4 bytes", ex.Message);
  }

  [Fact]
  public void ToRecordOffset_WithValue128_ReturnsOne()
  {
    // Arrange
    // 128.0 / 128.0 = 1.0, floor = 1
    byte[] msbinBytes = new byte[] { 0x88, 0x00, 0x00, 0x00 };

    // Act
    int offset = MsbinConverter.ToRecordOffset(msbinBytes);

    // Assert
    Assert.Equal(1, offset);
  }

  [Fact]
  public void ToRecordOffset_WithValue256_ReturnsTwo()
  {
    // Arrange
    // 256.0 / 128.0 = 2.0, floor = 2
    byte[] msbinBytes = new byte[] { 0x89, 0x00, 0x00, 0x00 };

    // Act
    int offset = MsbinConverter.ToRecordOffset(msbinBytes);

    // Assert
    Assert.Equal(2, offset);
  }

  [Fact]
  public void ToRecordOffset_WithValueZero_ReturnsZero()
  {
    // Arrange
    byte[] msbinBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act
    int offset = MsbinConverter.ToRecordOffset(msbinBytes);

    // Assert
    Assert.Equal(0, offset);
  }

  [Fact]
  public void FromDouble_WithZero_ReturnsZeroBytes()
  {
    // Arrange & Act
    byte[] result = MsbinConverter.FromDouble(0.0);

    // Assert
    Assert.Equal(4, result.Length);
    Assert.Equal(0x00, result[0]);
    Assert.Equal(0x00, result[1]);
    Assert.Equal(0x00, result[2]);
    Assert.Equal(0x00, result[3]);
  }

  [Fact]
  public void FromDouble_WithValue128_ReturnsCorrectBytes()
  {
    // Arrange & Act
    byte[] result = MsbinConverter.FromDouble(128.0);

    // Assert
    Assert.Equal(4, result.Length);
    Assert.Equal(0x88, result[0]); // Exponent byte
    
    // Verify round-trip
    double roundTrip = MsbinConverter.ToDouble(result);
    Assert.Equal(128.0, roundTrip, precision: 5);
  }

  [Fact]
  public void FromDouble_WithNegativeValue_SetSignBit()
  {
    // Arrange & Act
    byte[] result = MsbinConverter.FromDouble(-128.0);

    // Assert
    Assert.Equal(4, result.Length);
    Assert.True((result[3] & 0x80) != 0); // Sign bit should be set
    
    // Verify round-trip
    double roundTrip = MsbinConverter.ToDouble(result);
    Assert.Equal(-128.0, roundTrip, precision: 5);
  }

  [Fact]
  public void FromRecordOffset_WithOffsetOne_ReturnsCorrectBytes()
  {
    // Arrange & Act
    byte[] result = MsbinConverter.FromRecordOffset(1);

    // Assert
    // Should represent 128.0 (1 * 128)
    double value = MsbinConverter.ToDouble(result);
    Assert.Equal(128.0, value, precision: 5);
  }

  [Fact]
  public void FromRecordOffset_WithOffsetZero_ReturnsZeroBytes()
  {
    // Arrange & Act
    byte[] result = MsbinConverter.FromRecordOffset(0);

    // Assert
    Assert.All(result, b => Assert.Equal(0x00, b));
  }

  [Fact]
  public void RoundTrip_DoubleToMsbinToDouble_PreservesValue()
  {
    // Arrange
    double[] testValues = new double[]
    {
      0.0,
      1.0,
      128.0,
      256.0,
      512.0,
      1024.0,
      -128.0,
      -256.0
    };

    foreach (double original in testValues)
    {
      // Act
      byte[] msbin = MsbinConverter.FromDouble(original);
      double result = MsbinConverter.ToDouble(msbin);

      // Assert
      Assert.Equal(original, result, precision: 5);
    }
  }

  [Fact]
  public void RoundTrip_RecordOffsetToMsbinToOffset_PreservesValue()
  {
    // Arrange
    int[] testOffsets = new int[] { 0, 1, 2, 10, 100, 1000 };

    foreach (int original in testOffsets)
    {
      // Act
      byte[] msbin = MsbinConverter.FromRecordOffset(original);
      int result = MsbinConverter.ToRecordOffset(msbin);

      // Assert
      Assert.Equal(original, result);
    }
  }

  [Fact]
  public void IsValid_WithValidMsbin_ReturnsTrue()
  {
    // Arrange
    byte[] validBytes = new byte[] { 0x88, 0x00, 0x00, 0x00 };

    // Act
    bool isValid = MsbinConverter.IsValid(validBytes);

    // Assert
    Assert.True(isValid);
  }

  [Fact]
  public void IsValid_WithZeroBytes_ReturnsTrue()
  {
    // Arrange
    byte[] zeroBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    // Act
    bool isValid = MsbinConverter.IsValid(zeroBytes);

    // Assert
    Assert.True(isValid);
  }

  [Fact]
  public void IsValid_WithInvalidLength_ReturnsFalse()
  {
    // Arrange
    byte[] invalidBytes = new byte[] { 0x00, 0x00, 0x00 };

    // Act
    bool isValid = MsbinConverter.IsValid(invalidBytes);

    // Assert
    Assert.False(isValid);
  }

  [Fact]
  public void IsValid_WithZeroExponentNonZeroMantissa_ReturnsFalse()
  {
    // Arrange
    // Invalid: exponent is zero but mantissa is non-zero
    byte[] invalidBytes = new byte[] { 0x00, 0x01, 0x00, 0x00 };

    // Act
    bool isValid = MsbinConverter.IsValid(invalidBytes);

    // Assert
    Assert.False(isValid);
  }

  [Theory]
  [InlineData(1.5, 0)]
  [InlineData(127.9, 0)]
  [InlineData(128.0, 1)]
  [InlineData(128.1, 1)]
  [InlineData(255.9, 1)]
  [InlineData(256.0, 2)]
  public void ToRecordOffset_WithVariousValues_ReturnsFlooredDivision(
    double value,
    int expectedOffset)
  {
    // Arrange
    byte[] msbin = MsbinConverter.FromDouble(value);

    // Act
    int offset = MsbinConverter.ToRecordOffset(msbin);

    // Assert
    Assert.Equal(expectedOffset, offset);
  }

  [Fact]
  public void ToDouble_WithLargeValue_HandlesCorrectly()
  {
    // Arrange
    double large = 65536.0; // 2^16

    // Act
    byte[] msbin = MsbinConverter.FromDouble(large);
    double result = MsbinConverter.ToDouble(msbin);

    // Assert
    Assert.Equal(large, result, precision: 5);
  }

  [Fact]
  public void ToDouble_WithSmallFractionalValue_HandlesCorrectly()
  {
    // Arrange
    double small = 0.5;

    // Act
    byte[] msbin = MsbinConverter.FromDouble(small);
    double result = MsbinConverter.ToDouble(msbin);

    // Assert
    Assert.Equal(small, result, precision: 5);
  }
}
