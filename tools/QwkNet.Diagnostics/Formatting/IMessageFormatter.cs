using System.Collections.Generic;
using QwkNet;
using QwkNet.Diagnostics.Commands;

namespace QwkNet.Diagnostics.Formatting;

/// <summary>
/// Formats message collections for output.
/// </summary>
internal interface IMessageFormatter
{
  /// <summary>
  /// Formats a collection of messages for display.
  /// </summary>
  /// <param name="messages">The messages to format.</param>
  /// <param name="packet">The packet containing the messages.</param>
  /// <returns>The formatted output string.</returns>
  string Format(List<MessageView> messages, QwkPacket packet);
}