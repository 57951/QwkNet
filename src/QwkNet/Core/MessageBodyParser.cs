using System;
using System.Collections.Generic;
using System.Text;
using QwkNet.Encoding;

namespace QwkNet.Core;

/// <summary>
/// Parses QWK message body chunks into text lines.
/// </summary>
/// <remarks>
/// <para>
/// QWK message bodies follow these rules:
/// </para>
/// <list type="bullet">
/// <item>Lines are terminated with 0xE3 (Ï€) marker</item>
/// <item>Last line may or may not have terminator</item>
/// <item>Null bytes (0x00) are treated as spaces</item>
/// <item>Trailing spaces on each line should be preserved</item>
/// <item>Each 128-byte block contains packed text</item>
/// </list>
/// <para>
/// QWKE extensions may use different line termination schemes.
/// </para>
/// </remarks>
public static class MessageBodyParser
{
  /// <summary>
  /// The standard QWK line terminator byte (0xE3, Ï€ character).
  /// </summary>
  public const byte LineTerminator = 0xE3;

  /// <summary>
  /// Parses message body blocks into individual text lines.
  /// </summary>
  /// <param name="blocks">The message body blocks (each 128 bytes).</param>
  /// <returns>A list of text lines.</returns>
  /// <exception cref="ArgumentNullException">Thrown when blocks is null.</exception>
  public static List<string> ParseLines(byte[][] blocks)
  {
    if (blocks == null)
    {
      throw new ArgumentNullException(nameof(blocks));
    }

    if (blocks.Length == 0)
    {
      return new List<string>();
    }

    // Concatenate all blocks into single buffer
    int totalLength = blocks.Length * BinaryRecordReader.RecordSize;
    byte[] buffer = new byte[totalLength];
    
    for (int i = 0; i < blocks.Length; i++)
    {
      Array.Copy(blocks[i], 0, buffer, i * BinaryRecordReader.RecordSize, BinaryRecordReader.RecordSize);
    }

    return ParseLinesFromBuffer(buffer);
  }

  /// <summary>
  /// Parses message body from a contiguous byte buffer.
  /// </summary>
  /// <param name="buffer">The message body bytes.</param>
  /// <returns>A list of text lines.</returns>
  public static List<string> ParseLinesFromBuffer(ReadOnlySpan<byte> buffer)
  {
    List<string> lines = new List<string>();
    List<byte> currentLine = new List<byte>();

    for (int i = 0; i < buffer.Length; i++)
    {
      byte b = buffer[i];

      if (b == LineTerminator)
      {
        // End of line
        string line = BytesToString(currentLine);
        // Add line even if empty (explicit terminator means intentional line)
        lines.Add(line);
        currentLine.Clear();
      }
      else if (b == 0x00)
      {
        // Null bytes are treated as spaces in QWK
        currentLine.Add((byte)' ');
      }
      else
      {
        currentLine.Add(b);
      }
    }

    // Add final line if it has content (some packets omit final terminator)
    if (currentLine.Count > 0)
    {
      string finalLine = BytesToString(currentLine);
      // Only add if line has actual content after trimming
      if (!string.IsNullOrEmpty(finalLine))
      {
        lines.Add(finalLine);
      }
    }

    return lines;
  }

  /// <summary>
  /// Converts message body lines back to QWK-formatted blocks.
  /// </summary>
  /// <param name="lines">The text lines to encode.</param>
  /// <param name="terminateLastLine">Whether to add terminator to the last line.</param>
  /// <returns>An array of 128-byte blocks.</returns>
  /// <exception cref="ArgumentNullException">Thrown when lines is null.</exception>
  public static byte[][] EncodeLines(
    IEnumerable<string> lines,
    bool terminateLastLine = true)
  {
    if (lines == null)
    {
      throw new ArgumentNullException(nameof(lines));
    }

    List<byte> buffer = new List<byte>();

    string[] lineArray = lines is string[] arr ? arr : new List<string>(lines).ToArray();

    for (int i = 0; i < lineArray.Length; i++)
    {
      string line = lineArray[i];
      bool isLastLine = i == lineArray.Length - 1;

      // Convert string to bytes
      byte[] lineBytes = StringToBytes(line);
      buffer.AddRange(lineBytes);

      // Add line terminator
      if (!isLastLine || terminateLastLine)
      {
        buffer.Add(LineTerminator);
      }
    }

    // Pad to 128-byte boundary
    int remainder = buffer.Count % BinaryRecordReader.RecordSize;
    if (remainder != 0)
    {
      int paddingNeeded = BinaryRecordReader.RecordSize - remainder;
      for (int i = 0; i < paddingNeeded; i++)
      {
        buffer.Add((byte)' ');
      }
    }

    // Split into 128-byte blocks
    int blockCount = buffer.Count / BinaryRecordReader.RecordSize;
    byte[][] blocks = new byte[blockCount][];

    for (int i = 0; i < blockCount; i++)
    {
      blocks[i] = new byte[BinaryRecordReader.RecordSize];
      buffer.CopyTo(
        i * BinaryRecordReader.RecordSize,
        blocks[i],
        0,
        BinaryRecordReader.RecordSize);
    }

    return blocks;
  }

  /// <summary>
  /// Detects whether a message body uses QWKE-style line endings.
  /// </summary>
  /// <param name="buffer">The message body bytes.</param>
  /// <returns>True if QWKE line endings detected, false for classic QWK.</returns>
  /// <remarks>
  /// QWKE messages may use CR (0x0D) or CRLF (0x0D 0x0A) line endings
  /// instead of 0xE3 terminators.
  /// </remarks>
  public static bool IsQwkeFormat(ReadOnlySpan<byte> buffer)
  {
    int crCount = 0;
    int e3Count = 0;

    for (int i = 0; i < Math.Min(buffer.Length, 512); i++)
    {
      if (buffer[i] == 0x0D)
      {
        crCount++;
      }
      else if (buffer[i] == LineTerminator)
      {
        e3Count++;
      }
    }

    // If we see CRs but no E3 terminators, likely QWKE
    return crCount > 0 && e3Count == 0;
  }

  /// <summary>
  /// Parses QWKE-style message body with CR/CRLF line endings.
  /// </summary>
  /// <param name="buffer">The message body bytes.</param>
  /// <returns>A list of text lines.</returns>
  public static List<string> ParseQwkeLines(ReadOnlySpan<byte> buffer)
  {
    List<string> lines = new List<string>();
    List<byte> currentLine = new List<byte>();

    for (int i = 0; i < buffer.Length; i++)
    {
      byte b = buffer[i];

      if (b == 0x0D)
      {
        // CR - end of line
        lines.Add(BytesToString(currentLine));
        currentLine.Clear();

        // Skip following LF if present
        if (i + 1 < buffer.Length && buffer[i + 1] == 0x0A)
        {
          i++;
        }
      }
      else if (b == 0x00)
      {
        // Treat nulls as spaces
        currentLine.Add((byte)' ');
      }
      else if (b == 0x0A)
      {
        // Standalone LF (unusual but handle it)
        lines.Add(BytesToString(currentLine));
        currentLine.Clear();
      }
      else
      {
        currentLine.Add(b);
      }
    }

    // Add final line if present
    if (currentLine.Count > 0)
    {
      lines.Add(BytesToString(currentLine));
    }

    return lines;
  }

  private static string BytesToString(List<byte> bytes)
  {
    if (bytes.Count == 0)
    {
      return string.Empty;
    }

    // Use CP437 for proper high-ASCII character mapping (DOS/BBS standard)
    string decoded = Cp437Encoding.Decode(bytes.ToArray());
    return decoded.TrimEnd(' ');
  }

  private static byte[] StringToBytes(string text)
  {
    // Use CP437 encoding for proper DOS/BBS character mapping
    return Cp437Encoding.Encode(text);
  }
}