using System;
using System.Collections.Generic;

namespace QwkNet.Encoding.Ansi;

/// <summary>
/// Provides static methods for detecting ANSI escape sequences in byte content.
/// </summary>
/// <remarks>
/// <para>
/// ANSI escape sequences are commonly found in BBS bulletin files, screen captures,
/// and occasionally in message bodies. This class provides detection capabilities
/// without performing terminal emulation or rendering.
/// </para>
/// <para>
/// ANSI handling is optional and not part of the QWK specification. These utilities
/// are provided for convenience when working with BBS content that includes ANSI codes.
/// </para>
/// </remarks>
public static class AnsiEscapeDetector
{
  /// <summary>
  /// The ESC character (0x1B) that begins ANSI escape sequences.
  /// </summary>
  public const byte EscapeByte = 0x1B;

  /// <summary>
  /// Determines whether a byte sequence contains ANSI escape sequences.
  /// </summary>
  /// <param name="bytes">The byte sequence to check.</param>
  /// <returns>
  /// <c>true</c> if at least one ANSI escape sequence is detected; otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method performs a simple check for ESC (0x1B) followed by '[' which indicates
  /// a Control Sequence Introducer (CSI), the most common ANSI escape sequence type.
  /// </para>
  /// <para>
  /// This is a conservative detection that focuses on the most common ANSI sequences
  /// used in BBS systems. Other escape sequences (e.g., ESC followed by other characters)
  /// are not detected by this method.
  /// </para>
  /// </remarks>
  public static bool ContainsAnsiEscapes(ReadOnlySpan<byte> bytes)
  {
    for (int i = 0; i < bytes.Length - 1; i++)
    {
      if (bytes[i] == EscapeByte && bytes[i + 1] == (byte)'[')
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Determines whether a string contains ANSI escape sequences.
  /// </summary>
  /// <param name="text">The text to check.</param>
  /// <returns>
  /// <c>true</c> if at least one ANSI escape sequence is detected; otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// This overload operates on decoded text rather than raw bytes. It's less accurate
  /// than the byte-based version because encoding conversions may have already altered
  /// or removed escape sequences.
  /// </remarks>
  public static bool ContainsAnsiEscapes(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return false;
    }

    for (int i = 0; i < text.Length - 1; i++)
    {
      if (text[i] == (char)EscapeByte && text[i + 1] == '[')
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Finds all ANSI escape sequences in a byte sequence.
  /// </summary>
  /// <param name="bytes">The byte sequence to search.</param>
  /// <returns>
  /// An enumerable of tuples containing the start index and length of each detected sequence.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method attempts to identify complete ANSI CSI sequences by looking for
  /// ESC '[' followed by parameter bytes and a final command byte.
  /// </para>
  /// <para>
  /// CSI sequences typically follow the pattern: ESC '[' [parameters] [command]
  /// where parameters are digits, semicolons, or spaces, and the command is a letter.
  /// </para>
  /// <para>
  /// Truncated or malformed sequences may not be fully detected. This method provides
  /// best-effort identification without throwing exceptions on invalid sequences.
  /// </para>
  /// </remarks>
  public static IEnumerable<(int start, int length)> FindEscapeSequences(ReadOnlySpan<byte> bytes)
  {
    List<(int start, int length)> sequences = new List<(int start, int length)>();

    for (int i = 0; i < bytes.Length - 1; i++)
    {
      if (bytes[i] == EscapeByte && bytes[i + 1] == (byte)'[')
      {
        // Found CSI sequence start
        int start = i;
        int pos = i + 2;

        // Skip parameter bytes (digits, semicolons, spaces)
        while (pos < bytes.Length && IsParameterByte(bytes[pos]))
        {
          pos++;
        }

        // Check for command byte (letter)
        if (pos < bytes.Length && IsCommandByte(bytes[pos]))
        {
          pos++;
          sequences.Add((start, pos - start));
          i = pos - 1; // Skip ahead (loop will increment)
        }
        else
        {
          // Incomplete or malformed sequence
          // Record what we found anyway
          sequences.Add((start, pos - start));
          i = pos - 1;
        }
      }
    }

    return sequences;
  }

  /// <summary>
  /// Counts the number of ANSI escape sequences in a byte sequence.
  /// </summary>
  /// <param name="bytes">The byte sequence to analyse.</param>
  /// <returns>
  /// The number of ANSI CSI sequences detected.
  /// </returns>
  /// <remarks>
  /// This is a convenience method that counts sequences without allocating a list.
  /// </remarks>
  public static int CountEscapeSequences(ReadOnlySpan<byte> bytes)
  {
    int count = 0;

    for (int i = 0; i < bytes.Length - 1; i++)
    {
      if (bytes[i] == EscapeByte && bytes[i + 1] == (byte)'[')
      {
        count++;

        // Skip past this sequence to avoid double-counting
        int pos = i + 2;
        while (pos < bytes.Length && IsParameterByte(bytes[pos]))
        {
          pos++;
        }

        if (pos < bytes.Length && IsCommandByte(bytes[pos]))
        {
          pos++;
        }

        i = pos - 1;
      }
    }

    return count;
  }

  /// <summary>
  /// Determines whether a byte is an ANSI CSI parameter byte.
  /// </summary>
  /// <param name="b">The byte to check.</param>
  /// <returns>
  /// <c>true</c> if the byte is a digit, semicolon, or space; otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// Parameter bytes are '0'-'9', ';', or space characters that appear between
  /// the CSI introducer and the final command byte.
  /// </remarks>
  private static bool IsParameterByte(byte b)
  {
    return (b >= (byte)'0' && b <= (byte)'9') || b == (byte)';' || b == (byte)' ';
  }

  /// <summary>
  /// Determines whether a byte is an ANSI CSI command byte.
  /// </summary>
  /// <param name="b">The byte to check.</param>
  /// <returns>
  /// <c>true</c> if the byte is a letter (A-Z or a-z); otherwise, <c>false</c>.
  /// </returns>
  /// <remarks>
  /// Command bytes are typically uppercase letters (e.g., 'H' for cursor position,
  /// 'm' for SGR graphics mode), but lowercase letters are also valid in some sequences.
  /// </remarks>
  private static bool IsCommandByte(byte b)
  {
    return (b >= (byte)'A' && b <= (byte)'Z') || (b >= (byte)'a' && b <= (byte)'z');
  }
}
