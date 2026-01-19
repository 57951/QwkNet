using System;

namespace QwkNet.Models.Qwke;

/// <summary>
/// Represents a command from the TOREADER.EXT file in a QWKE packet.
/// </summary>
/// <remarks>
/// <para>
/// TOREADER.EXT contains commands sent from the BBS to the offline reader,
/// such as area selection preferences, keyword filtering rules, and twit lists.
/// </para>
/// <para>
/// This class preserves the raw command text verbatim for unknown or custom commands,
/// maintaining byte fidelity for archival purposes.
/// </para>
/// </remarks>
public sealed class ToReaderCommand
{
  /// <summary>
  /// Gets the command type (e.g., "AREA", "KEYWORD", "TWIT", "RESET").
  /// </summary>
  /// <value>
  /// The command identifier, typically uppercase.
  /// </value>
  public string CommandType { get; }

  /// <summary>
  /// Gets the command parameters.
  /// </summary>
  /// <value>
  /// The remainder of the command line after the command type.
  /// May be empty for commands without parameters.
  /// </value>
  public string Parameters { get; }

  /// <summary>
  /// Gets the raw command line as it appeared in TOREADER.EXT.
  /// </summary>
  /// <value>
  /// The original line including command type, separator, and parameters.
  /// </value>
  public string RawLine { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="ToReaderCommand"/> class.
  /// </summary>
  /// <param name="commandType">The command type.</param>
  /// <param name="parameters">The command parameters.</param>
  /// <param name="rawLine">The raw command line.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="commandType"/> or <paramref name="rawLine"/> is <c>null</c>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="commandType"/> is empty or whitespace-only.
  /// </exception>
  public ToReaderCommand(string commandType, string parameters, string rawLine)
  {
    if (commandType == null)
    {
      throw new ArgumentNullException(nameof(commandType));
    }

    if (string.IsNullOrWhiteSpace(commandType))
    {
      throw new ArgumentException("Command type cannot be empty or whitespace.", nameof(commandType));
    }

    if (rawLine == null)
    {
      throw new ArgumentNullException(nameof(rawLine));
    }

    CommandType = commandType;
    Parameters = parameters ?? string.Empty;
    RawLine = rawLine;
  }

  /// <summary>
  /// Returns a string representation of this command.
  /// </summary>
  /// <returns>
  /// A string in the format "CommandType Parameters" or just "CommandType" if no parameters.
  /// </returns>
  public override string ToString()
  {
    if (string.IsNullOrEmpty(Parameters))
    {
      return CommandType;
    }

    return $"{CommandType} {Parameters}";
  }
}
