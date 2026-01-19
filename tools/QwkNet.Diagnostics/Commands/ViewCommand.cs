using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QwkNet;
using QwkNet.Diagnostics.Formatting;
using QwkNet.Models.Messages;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Commands;

/// <summary>
/// Extracts and displays QWK messages in human-readable formats.
/// </summary>
internal static class ViewCommand
{
  public static int Execute(string[] args)
  {
    if (args.Length < 2)
    {
      Console.Error.WriteLine("Error: Missing packet file path.");
      Console.Error.WriteLine("Usage: QwkNet.Diagnostics view <packet.qwk> [options]");
      return 1;
    }

    string filePath = args[1];

    if (!File.Exists(filePath))
    {
      Console.Error.WriteLine($"Error: File not found: {filePath}");
      return 1;
    }

    // Parse options
    ViewOptions options = ParseOptions(args);

    if (options.HasError)
    {
      Console.Error.WriteLine($"Error: {options.ErrorMessage}");
      return 1;
    }

    // Validate message selection
    if (!options.MessageNumber.HasValue &&
        options.MessageNumbers.Count == 0 &&
        !options.RangeStart.HasValue &&
        !options.ConferenceNumber.HasValue &&
        !options.ViewAll)
    {
      Console.Error.WriteLine("Error: No messages specified. Use --message, --messages, --range, --conference, or --all");
      return 1;
    }

    try
    {
      // Open packet
      using QwkPacket packet = QwkPacket.Open(filePath, ValidationMode.Lenient);

      // Collect messages to display
      List<MessageView> messagesToDisplay = CollectMessages(packet, options);

      if (messagesToDisplay.Count == 0)
      {
        Console.Error.WriteLine("Error: No messages found matching the specified criteria.");
        return 1;
      }

      // Format and output messages
      IMessageFormatter formatter = CreateFormatter(options);
      string output = formatter.Format(messagesToDisplay, packet);

      // Write output
      if (!string.IsNullOrEmpty(options.OutputPath))
      {
        File.WriteAllText(options.OutputPath, output);
        Console.WriteLine($"Output written to: {options.OutputPath}");
      }
      else
      {
        Console.WriteLine(output);
      }

      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error: {ex.Message}");
      return 1;
    }
  }

  private static ViewOptions ParseOptions(string[] args)
  {
    ViewOptions options = new ViewOptions();

    for (int i = 2; i < args.Length; i++)
    {
      string arg = args[i].ToLowerInvariant();

      switch (arg)
      {
        case "--message":
          if (i + 1 >= args.Length)
          {
            options.ErrorMessage = "--message requires a value";
            return options;
          }
          if (!int.TryParse(args[++i], out int msgNum) || msgNum < 1)
          {
            options.ErrorMessage = $"Invalid message number: {args[i]}";
            return options;
          }
          options.MessageNumber = msgNum;
          break;

        case "--messages":
          if (i + 1 >= args.Length)
          {
            options.ErrorMessage = "--messages requires a value";
            return options;
          }
          string[] numbers = args[++i].Split(',');
          foreach (string numStr in numbers)
          {
            if (!int.TryParse(numStr.Trim(), out int num) || num < 1)
            {
              options.ErrorMessage = $"Invalid message number: {numStr}";
              return options;
            }
            options.MessageNumbers.Add(num);
          }
          break;

        case "--range":
          if (i + 1 >= args.Length)
          {
            options.ErrorMessage = "--range requires a value (e.g., 1-10)";
            return options;
          }
          string[] range = args[++i].Split('-');
          if (range.Length != 2 ||
              !int.TryParse(range[0].Trim(), out int start) ||
              !int.TryParse(range[1].Trim(), out int end) ||
              start < 1 || end < start)
          {
            options.ErrorMessage = $"Invalid range format: {args[i]}. Use format: N1-N2";
            return options;
          }
          options.RangeStart = start;
          options.RangeEnd = end;
          break;

        case "--conference":
          if (i + 1 >= args.Length)
          {
            options.ErrorMessage = "--conference requires a value";
            return options;
          }
          if (!ushort.TryParse(args[++i], out ushort confNum))
          {
            options.ErrorMessage = $"Invalid conference number: {args[i]}";
            return options;
          }
          options.ConferenceNumber = confNum;
          break;

        case "--all":
          options.ViewAll = true;
          break;

        case "--output":
          if (i + 1 >= args.Length)
          {
            options.ErrorMessage = "--output requires a file path";
            return options;
          }
          options.OutputPath = args[++i];
          break;

        case "--format":
          if (i + 1 >= args.Length)
          {
            options.ErrorMessage = "--format requires a value (text|json|markdown)";
            return options;
          }
          string formatStr = args[++i].ToLowerInvariant();
          options.Format = formatStr switch
          {
            "text" or "txt" => ViewFormat.Text,
            "json" => ViewFormat.Json,
            "markdown" or "md" => ViewFormat.Markdown,
            _ => ViewFormat.Text
          };
          if (formatStr != "text" && formatStr != "txt" && formatStr != "json" &&
              formatStr != "markdown" && formatStr != "md")
          {
            options.ErrorMessage = $"Invalid format: {formatStr}. Use text, json, or markdown.";
            return options;
          }
          break;

        case "--show-raw":
          options.ShowRaw = true;
          break;

        case "--show-kludges":
          options.ShowKludges = true;
          break;

        case "--show-cp437":
          options.ShowCp437 = true;
          break;

        default:
          options.ErrorMessage = $"Unknown option: {args[i]}";
          return options;
      }
    }

    return options;
  }

  private static List<MessageView> CollectMessages(QwkPacket packet, ViewOptions options)
  {
    List<MessageView> messages = new List<MessageView>();

    if (options.ViewAll)
    {
      // Add all messages
      for (int i = 0; i < packet.Messages.Count; i++)
      {
        messages.Add(new MessageView(packet.Messages[i], i + 1, packet.Messages.Count));
      }
    }
    else if (options.MessageNumber.HasValue)
    {
      // Single message (1-based index)
      int index = options.MessageNumber.Value - 1;
      if (index >= 0 && index < packet.Messages.Count)
      {
        messages.Add(new MessageView(packet.Messages[index], options.MessageNumber.Value, packet.Messages.Count));
      }
    }
    else if (options.MessageNumbers.Count > 0)
    {
      // Multiple specific messages
      foreach (int msgNum in options.MessageNumbers)
      {
        int index = msgNum - 1;
        if (index >= 0 && index < packet.Messages.Count)
        {
          messages.Add(new MessageView(packet.Messages[index], msgNum, packet.Messages.Count));
        }
      }
    }
    else if (options.RangeStart.HasValue && options.RangeEnd.HasValue)
    {
      // Range of messages
      int start = options.RangeStart.Value - 1;
      int end = Math.Min(options.RangeEnd.Value - 1, packet.Messages.Count - 1);
      for (int i = start; i <= end; i++)
      {
        if (i >= 0 && i < packet.Messages.Count)
        {
          messages.Add(new MessageView(packet.Messages[i], i + 1, packet.Messages.Count));
        }
      }
    }
    else if (options.ConferenceNumber.HasValue)
    {
      // All messages in a conference
      List<Message> confMessages = packet.Messages.GetByConference(options.ConferenceNumber.Value).ToList();
      
      // Find the 1-based message numbers for these messages
      for (int i = 0; i < packet.Messages.Count; i++)
      {
        if (packet.Messages[i].ConferenceNumber == options.ConferenceNumber.Value)
        {
          messages.Add(new MessageView(packet.Messages[i], i + 1, packet.Messages.Count));
        }
      }
    }

    return messages;
  }

  private static IMessageFormatter CreateFormatter(ViewOptions options)
  {
    return options.Format switch
    {
      ViewFormat.Json => new JsonMessageFormatter(options.ShowKludges, options.ShowCp437),
      ViewFormat.Markdown => new MarkdownMessageFormatter(options.ShowKludges, options.ShowCp437),
      ViewFormat.Text => new TextMessageFormatter(options.ShowRaw, options.ShowKludges, options.ShowCp437),
      _ => new TextMessageFormatter(options.ShowRaw, options.ShowKludges, options.ShowCp437)
    };
  }
}

/// <summary>
/// Options for the view command.
/// </summary>
internal sealed class ViewOptions
{
  public int? MessageNumber { get; set; }
  public List<int> MessageNumbers { get; } = new List<int>();
  public int? RangeStart { get; set; }
  public int? RangeEnd { get; set; }
  public ushort? ConferenceNumber { get; set; }
  public bool ViewAll { get; set; }
  public string? OutputPath { get; set; }
  public ViewFormat Format { get; set; } = ViewFormat.Text;
  public bool ShowRaw { get; set; }
  public bool ShowKludges { get; set; }
  public bool ShowCp437 { get; set; }
  public string? ErrorMessage { get; set; }
  public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}

/// <summary>
/// Output format for message display.
/// </summary>
internal enum ViewFormat
{
  Text,
  Json,
  Markdown
}

/// <summary>
/// Represents a message with its display context.
/// </summary>
internal sealed class MessageView
{
  public Message Message { get; }
  public int DisplayNumber { get; }
  public int TotalMessages { get; }

  public MessageView(Message message, int displayNumber, int totalMessages)
  {
    Message = message ?? throw new ArgumentNullException(nameof(message));
    DisplayNumber = displayNumber;
    TotalMessages = totalMessages;
  }
}