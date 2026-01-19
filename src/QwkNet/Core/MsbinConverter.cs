using System;

namespace QwkNet.Core;

/// <summary>
/// Converts Microsoft Binary Format (MSBIN) floats to record offsets.
/// </summary>
/// <remarks>
/// <para>
/// QWK index files (.NDX) store message pointers as 4-byte MSBIN floats.
/// This format was used in Microsoft BASIC and QuickBASIC.
/// </para>
/// <para>
/// MSBIN format:
/// - Byte 0: Exponent + 0x81 (biased by 129)
/// - Bytes 1-3: Mantissa (24-bit, big-endian)
/// - Byte 3, bit 7: Sign bit
/// </para>
/// <para>
/// Zero is represented as four zero bytes.
/// </para>
/// </remarks>
public static class MsbinConverter
{
  /// <summary>
  /// Converts a 4-byte MSBIN float to a record offset.
  /// </summary>
  /// <param name="msbinBytes">The 4-byte MSBIN float representation.</param>
  /// <returns>The record offset as an integer.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when msbinBytes is not exactly 4 bytes.
  /// </exception>
  /// <remarks>
  /// The record offset is calculated as floor(float_value / 128.0).
  /// This accounts for QWK's 128-byte record size.
  /// </remarks>
  public static int ToRecordOffset(ReadOnlySpan<byte> msbinBytes)
  {
    if (msbinBytes.Length != 4)
    {
      throw new ArgumentException(
        "MSBIN float must be exactly 4 bytes.",
        nameof(msbinBytes));
    }

    double value = ToDouble(msbinBytes);
    return (int)Math.Floor(value / 128.0);
  }

  /// <summary>
  /// Converts a 4-byte MSBIN float to a double-precision floating point.
  /// </summary>
  /// <param name="msbinBytes">The 4-byte MSBIN float representation.</param>
  /// <returns>The converted double value.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when msbinBytes is not exactly 4 bytes.
  /// </exception>
  public static double ToDouble(ReadOnlySpan<byte> msbinBytes)
  {
    if (msbinBytes.Length != 4)
    {
      throw new ArgumentException(
        "MSBIN float must be exactly 4 bytes.",
        nameof(msbinBytes));
    }

    // Check for zero
    if (msbinBytes[0] == 0 && msbinBytes[1] == 0 &&
        msbinBytes[2] == 0 && msbinBytes[3] == 0)
    {
      return 0.0;
    }

    // Extract components
    byte exponentByte = msbinBytes[0];
    int mantissaHigh = msbinBytes[1];
    int mantissaMid = msbinBytes[2];
    int mantissaLow = msbinBytes[3];

    // Extract sign bit (bit 7 of byte 3)
    bool isNegative = (mantissaLow & 0x80) != 0;

    // Clear sign bit from mantissa
    mantissaLow &= 0x7F;

    // Reconstruct mantissa as 24-bit value
    int mantissa = (mantissaHigh << 16) | (mantissaMid << 8) | mantissaLow;

    // MSBIN stores mantissa with implicit leading 1 bit
    // Convert to floating point: mantissa / 2^24 * 2
    double mantissaValue = (double)mantissa / 0x800000 + 1.0;

    // Decode exponent (biased by 129 = 0x81)
    int exponent = exponentByte - 0x81;

    // Calculate final value
    double value = mantissaValue * Math.Pow(2.0, exponent);

    return isNegative ? -value : value;
  }

  /// <summary>
  /// Converts a record offset to a 4-byte MSBIN float.
  /// </summary>
  /// <param name="recordOffset">The zero-based record offset.</param>
  /// <returns>A 4-byte array containing the MSBIN representation.</returns>
  /// <remarks>
  /// Used when generating .NDX index files for REP packets.
  /// </remarks>
  public static byte[] FromRecordOffset(int recordOffset)
  {
    double value = recordOffset * 128.0;
    return FromDouble(value);
  }

  /// <summary>
  /// Converts a double-precision float to a 4-byte MSBIN float.
  /// </summary>
  /// <param name="value">The double value to convert.</param>
  /// <returns>A 4-byte array containing the MSBIN representation.</returns>
  public static byte[] FromDouble(double value)
  {
    byte[] result = new byte[4];

    // Handle zero
    if (value == 0.0)
    {
      return result; // All zeros
    }

    // Extract sign
    bool isNegative = value < 0.0;
    value = Math.Abs(value);

    // Calculate exponent and mantissa
    int exponent = (int)Math.Floor(Math.Log(value, 2.0));
    double mantissaValue = value / Math.Pow(2.0, exponent);

    // Normalise mantissa to [1.0, 2.0)
    if (mantissaValue >= 2.0)
    {
      mantissaValue /= 2.0;
      exponent++;
    }
    else if (mantissaValue < 1.0)
    {
      mantissaValue *= 2.0;
      exponent--;
    }

    // Convert mantissa to 24-bit integer (removing implicit leading 1)
    int mantissa = (int)Math.Round((mantissaValue - 1.0) * 0x800000);

    // Encode exponent (bias by 129)
    result[0] = (byte)(exponent + 0x81);

    // Encode mantissa (big-endian)
    result[1] = (byte)((mantissa >> 16) & 0xFF);
    result[2] = (byte)((mantissa >> 8) & 0xFF);
    result[3] = (byte)(mantissa & 0x7F);

    // Set sign bit
    if (isNegative)
    {
      result[3] |= 0x80;
    }

    return result;
  }

  /// <summary>
  /// Validates that a byte sequence represents a valid MSBIN float.
  /// </summary>
  /// <param name="msbinBytes">The bytes to validate.</param>
  /// <returns>True if valid, false otherwise.</returns>
  public static bool IsValid(ReadOnlySpan<byte> msbinBytes)
  {
    if (msbinBytes.Length != 4)
    {
      return false;
    }

    // Zero is always valid
    if (msbinBytes[0] == 0 && msbinBytes[1] == 0 &&
        msbinBytes[2] == 0 && msbinBytes[3] == 0)
    {
      return true;
    }

    // Exponent must be non-zero for non-zero values
    if (msbinBytes[0] == 0)
    {
      return false;
    }

    return true;
  }
}
