using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QwkNet.Encoding;
using QwkNet.Models.Qwke;

namespace QwkNet.Parsing.Qwke;

/// <summary>
/// Parses TODOOR.EXT files from QWKE reply packets.
/// </summary>
/// <remarks>
/// <para>
/// TODOOR.EXT contains commands sent from the offline reader to the BBS door.
/// Each line is a command in the format "COMMANDTYPE parameters".
/// </para>
/// <para>
/// This parser preserves unknown commands verbatim, maintaining byte fidelity
/// for custom or proprietary extensions.
/// </para>
/// </remarks>
public static class ToDoorExtParser
{
  /// <summary>
  /// Parses a TODOOR.EXT file from a stream.
  /// </summary>
  /// <param name="stream">The stream containing TODOOR.EXT data.</param>
  /// <returns>
  /// A read-only list of <see cref="ToDoorCommand"/> instances, one per line.
  /// Empty lines and whitespace-only lines are skipped.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="stream"/> is <c>null</c>.
  /// </exception>
  public static IReadOnlyList<ToDoorCommand> Parse(Stream stream)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    List<ToDoorCommand> commands = new List<ToDoorCommand>();

    using StreamReader reader = new StreamReader(stream, Cp437Encoding.GetEncoding(), detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
    
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
      // Skip empty or whitespace-only lines
      if (string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      ToDoorCommand command = ParseCommand(line);
      commands.Add(command);
    }

    return commands;
  }

  /// <summary>
  /// Parses a TODOOR.EXT file from a byte array.
  /// </summary>
  /// <param name="data">The byte array containing TODOOR.EXT data.</param>
  /// <returns>
  /// A read-only list of <see cref="ToDoorCommand"/> instances, one per line.
  /// Empty lines and whitespace-only lines are skipped.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="data"/> is <c>null</c>.
  /// </exception>
  public static IReadOnlyList<ToDoorCommand> Parse(byte[] data)
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    using MemoryStream stream = new MemoryStream(data, writable: false);
    return Parse(stream);
  }

  /// <summary>
  /// Parses a single command line into a <see cref="ToDoorCommand"/>.
  /// </summary>
  /// <param name="line">The command line to parse.</param>
  /// <returns>
  /// A new <see cref="ToDoorCommand"/> instance.
  /// </returns>
  /// <remarks>
  /// The command type is extracted as the first whitespace-delimited token.
  /// The remainder of the line is treated as parameters.
  /// If the line contains no whitespace, the entire line is the command type
  /// and parameters are empty.
  /// </remarks>
  private static ToDoorCommand ParseCommand(string line)
  {
    // Trim the line but preserve original for RawLine
    string trimmedLine = line.Trim();

    // Find first whitespace to split command type from parameters
    int spaceIndex = trimmedLine.IndexOfAny(new[] { ' ', '\t' });

    string commandType;
    string parameters;

    if (spaceIndex == -1)
    {
      // No whitespace - entire line is command type
      commandType = trimmedLine;
      parameters = string.Empty;
    }
    else
    {
      // Split at first whitespace
      commandType = trimmedLine.Substring(0, spaceIndex);
      parameters = trimmedLine.Substring(spaceIndex + 1).Trim();
    }

    return new ToDoorCommand(commandType, parameters, trimmedLine);
  }
}