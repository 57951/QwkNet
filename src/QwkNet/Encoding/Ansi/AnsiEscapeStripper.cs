using System;
using System.Collections.Generic;
using System.Text;

namespace QwkNet.Encoding.Ansi;

/// <summary>
/// Provides static methods for stripping ANSI escape sequences from text.
/// </summary>
/// <remarks>
/// <para>
/// ANSI escape sequence removal is an optional, policy-driven operation. This class
/// provides opt-in stripping for scenarios where plain text is required (e.g., searching,
/// indexing, or displaying in non-ANSI contexts).
/// </para>
/// <para>
/// Default library behaviour is to preserve all bytes, including ANSI sequences.
/// Stripping should only be performed when explicitly requested by the application.
/// </para>
/// </remarks>
public static class AnsiEscapeStripper
{
  /// <summary>
  /// Strips ANSI escape sequences from a byte sequence.
  /// </summary>
  /// <param name="bytes">The byte sequence containing ANSI codes.</param>
  /// <returns>
  /// A new byte array with ANSI escape sequences removed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method removes CSI sequences (ESC '[' ... [command]) from the byte stream.
  /// Other escape sequences (e.g., OSC, other ESC-prefixed codes) are not removed
  /// unless they happen to match the CSI pattern.
  /// </para>
  /// <para>
  /// This operation is lossy and irreversible. Use it only when plain text is required
  /// and ANSI formatting can be safely discarded.
  /// </para>
  /// </remarks>
  public static byte[] StripAnsiEscapes(ReadOnlySpan<byte> bytes)
  {
    if (bytes.Length == 0)
    {
      return Array.Empty<byte>();
    }

    // Find all escape sequences
    IEnumerable<(int start, int length)> sequences = AnsiEscapeDetector.FindEscapeSequences(bytes);

    // Build a list of ranges to keep
    List<(int start, int length)> keepRanges = new List<(int start, int length)>();
    int lastEnd = 0;

    foreach ((int start, int length) item in sequences)
    {
      // Add the range before this escape sequence
      if (item.start > lastEnd)
      {
        keepRanges.Add((lastEnd, item.start - lastEnd));
      }

      lastEnd = item.start + item.length;
    }

    // Add the final range after the last escape sequence
    if (lastEnd < bytes.Length)
    {
      keepRanges.Add((lastEnd, bytes.Length - lastEnd));
    }

    // Calculate total output size
    int totalLength = 0;
    foreach ((int start, int length) item in keepRanges)
    {
      totalLength += item.length;
    }

    // Allocate output array and copy ranges
    byte[] result = new byte[totalLength];
    int destPos = 0;

    foreach ((int start, int length) item in keepRanges)
    {
      bytes.Slice(item.start, item.length).CopyTo(result.AsSpan(destPos));
      destPos += item.length;
    }

    return result;
  }

  /// <summary>
  /// Strips ANSI escape sequences from a string.
  /// </summary>
  /// <param name="text">The text containing ANSI codes.</param>
  /// <returns>
  /// A new string with ANSI escape sequences removed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This overload operates on decoded text rather than raw bytes. It's less accurate
  /// than the byte-based version if encoding conversions have altered the escape sequences.
  /// </para>
  /// <para>
  /// This operation is lossy and irreversible. Use it only when plain text is required.
  /// </para>
  /// </remarks>
  public static string StripAnsiEscapes(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    StringBuilder result = new StringBuilder(text.Length);
    int i = 0;

    while (i < text.Length)
    {
      // Check for ESC '['
      if (i < text.Length - 1 && text[i] == (char)0x1B && text[i + 1] == '[')
      {
        // Skip the ESC '['
        i += 2;

        // Skip parameter bytes
        while (i < text.Length && IsParameterChar(text[i]))
        {
          i++;
        }

        // Skip command byte
        if (i < text.Length && IsCommandChar(text[i]))
        {
          i++;
        }

        // Continue without appending anything (sequence is stripped)
      }
      else
      {
        // Regular character - keep it
        result.Append(text[i]);
        i++;
      }
    }

    return result.ToString();
  }

  /// <summary>
  /// Strips ANSI escape sequences from text and returns lines without formatting.
  /// </summary>
  /// <param name="text">The text containing ANSI codes.</param>
  /// <returns>
  /// A new string with ANSI escape sequences removed and lines preserved.
  /// </returns>
  /// <remarks>
  /// This is a convenience method that strips ANSI codes whilst preserving line structure.
  /// It's useful for displaying BBS content in plain text contexts.
  /// </remarks>
  public static string StripAnsiEscapesPreserveLines(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return text;
    }

    // Split on common line endings
    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

    // Strip ANSI from each line
    for (int i = 0; i < lines.Length; i++)
    {
      lines[i] = StripAnsiEscapes(lines[i]);
    }

    // Rejoin with platform-native line endings
    return string.Join(Environment.NewLine, lines);
  }

  /// <summary>
  /// Determines whether a character is an ANSI CSI parameter character.
  /// </summary>
  private static bool IsParameterChar(char c)
  {
    return (c >= '0' && c <= '9') || c == ';' || c == ' ';
  }

  /// <summary>
  /// Determines whether a character is an ANSI CSI command character.
  /// </summary>
  private static bool IsCommandChar(char c)
  {
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
  }
}