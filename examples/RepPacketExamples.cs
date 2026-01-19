// QWK.NET - RepPacket Usage Examples
// Milestone 7: REP Packet Creation

using System;
using QwkNet;
using QwkNet.Models.Messages;
using QwkNet.Models.Control;

namespace QwkNet.Examples;

/// <summary>
/// Demonstrates how to create REP (reply) packets using QWK.NET.
/// </summary>
public static class RepPacketExamples
{
  /// <summary>
  /// Example 1: Create a simple REP packet with one message.
  /// </summary>
  public static void CreateSimpleRepPacket()
  {
    // Create a new REP packet
    using RepPacket rep = RepPacket.Create("MYBBS");

    // Build a message using MessageBuilder
    MessageBuilder builder = new MessageBuilder();
    builder.SetFrom("Alice")
           .SetTo("Sysop")
           .SetSubject("RE: Welcome Message")
           .SetConferenceNumber(1)
           .SetBodyText("Thanks for the warm welcome! Looking forward to participating.")
           .SetDateTime(DateTime.Now);

    Message reply = builder.Build();

    // Add the message to the REP packet
    rep.AddMessage(reply);

    // Save to file
    rep.SaveToFile("MYBBS.REP");

    Console.WriteLine("Created MYBBS.REP with 1 message");
  }

  /// <summary>
  /// Example 2: Create a REP packet from an existing QWK packet.
  /// </summary>
  public static void CreateRepFromQwk()
  {
    // Open the original QWK packet
    using QwkPacket qwk = QwkPacket.Open("MYBBS.QWK");

    // Create a REP packet using the QWK's control data
    using RepPacket rep = RepPacket.Create(qwk.Control);

    // Find messages that need replies
    foreach (Message message in qwk.Messages)
    {
      // Example: Reply to private messages addressed to us
      if (message.IsPrivate && message.To.Trim().Equals("ALICE", StringComparison.OrdinalIgnoreCase))
      {
        MessageBuilder replyBuilder = new MessageBuilder();
        replyBuilder.SetFrom("Alice")
                    .SetTo(message.From)
                    .SetSubject($"RE: {message.Subject}")
                    .SetConferenceNumber(message.ConferenceNumber)
                    .SetReferenceNumber(message.MessageNumber)
                    .SetBodyText($"Thanks for your message!\n\nOriginal message:\n> {message.Body.GetDecodedText()}")
                    .SetDateTime(DateTime.Now)
                    .SetStatus(MessageStatus.Private);

        Message reply = replyBuilder.Build();
        rep.AddMessage(reply);
      }
    }

    // Save the REP packet
    rep.SaveToFile($"{qwk.Control.BbsId}.REP");

    Console.WriteLine($"Created {qwk.Control.BbsId}.REP with {rep.Messages.Count} replies");
  }

  /// <summary>
  /// Example 3: Create a REP packet with messages in multiple conferences.
  /// </summary>
  public static void CreateMultiConferenceRep()
  {
    using RepPacket rep = RepPacket.Create("MYBBS");

    // Add a message to conference 1 (General)
    MessageBuilder builder1 = new MessageBuilder();
    builder1.SetFrom("Alice")
            .SetTo("All")
            .SetSubject("Introduction")
            .SetConferenceNumber(1)
            .SetBodyText("Hello everyone! I'm new here.")
            .SetDateTime(DateTime.Now);
    rep.AddMessage(builder1.Build());

    // Add a message to conference 5 (Programming)
    MessageBuilder builder2 = new MessageBuilder();
    builder2.SetFrom("Alice")
            .SetTo("All")
            .SetSubject("C# Question")
            .SetConferenceNumber(5)
            .SetBodyText("What's the best way to handle async/await in .NET?")
            .SetDateTime(DateTime.Now);
    rep.AddMessage(builder2.Build());

    // Add another message to conference 1
    MessageBuilder builder3 = new MessageBuilder();
    builder3.SetFrom("Alice")
            .SetTo("Sysop")
            .SetSubject("Thank You")
            .SetConferenceNumber(1)
            .SetBodyText("Thanks for running this great BBS!")
            .SetDateTime(DateTime.Now)
            .SetStatus(MessageStatus.Private);
    rep.AddMessage(builder3.Build());

    // Save the packet
    rep.SaveToFile("MYBBS.REP");

    Console.WriteLine($"Created MYBBS.REP with {rep.Messages.Count} messages across multiple conferences");
    Console.WriteLine("The packet will contain:");
    Console.WriteLine("  - CONTROL.DAT");
    Console.WriteLine("  - MESSAGES.DAT");
    Console.WriteLine("  - 1.NDX (conference 1 index)");
    Console.WriteLine("  - 5.NDX (conference 5 index)");
  }

  /// <summary>
  /// Example 4: Create a REP packet with a long message.
  /// </summary>
  public static void CreateRepWithLongMessage()
  {
    using RepPacket rep = RepPacket.Create("MYBBS");

    // Build a long message (multiple 128-byte blocks)
    string longMessage = @"This is a longer message that will span multiple 128-byte blocks.

When creating REP packets, QWK.NET automatically handles the block count
calculation and padding. Each block is exactly 128 bytes, and the library
ensures that:

1. The block count in the message header is correct
2. The message body is properly padded with spaces
3. QWK line terminators (0xE3) are preserved
4. All records align to 128-byte boundaries

This makes it easy to create valid REP packets that work with all BBS
software, without having to worry about the low-level format details.

The library handles messages of any length, from short one-liners to
extensive multi-paragraph replies like this one.";

    MessageBuilder builder = new MessageBuilder();
    builder.SetFrom("Alice")
           .SetTo("All")
           .SetSubject("About REP Packet Format")
           .SetConferenceNumber(1)
           .SetBodyText(longMessage)
           .SetDateTime(DateTime.Now);

    Message message = builder.Build();
    rep.AddMessage(message);

    // Save to file
    rep.SaveToFile("MYBBS.REP");

    Console.WriteLine($"Created MYBBS.REP with a long message ({longMessage.Length} bytes)");
    Console.WriteLine($"Message will span multiple 128-byte blocks in MESSAGES.DAT");
  }

  /// <summary>
  /// Example 5: Batch reply creation from multiple sources.
  /// </summary>
  public static void BatchReplyCreation()
  {
    using RepPacket rep = RepPacket.Create("MYBBS");

    // Simulate reading messages from different sources
    string[] replyTexts = new string[]
    {
      "Great point about assembly language!",
      "I agree with your analysis.",
      "Has anyone tried the new version?",
      "Thanks for sharing this information.",
      "Looking forward to more discussions."
    };

    DateTime now = DateTime.Now;

    for (int i = 0; i < replyTexts.Length; i++)
    {
      MessageBuilder builder = new MessageBuilder();
      builder.SetFrom("Alice")
             .SetTo("All")
             .SetSubject($"Reply #{i + 1}")
             .SetConferenceNumber((ushort)(1 + (i % 3))) // Spread across conferences 1-3
             .SetBodyText(replyTexts[i])
             .SetDateTime(now.AddMinutes(i)); // Stagger timestamps

      rep.AddMessage(builder.Build());
    }

    // Save the batch
    rep.SaveToFile("MYBBS.REP");

    Console.WriteLine($"Created MYBBS.REP with {rep.Messages.Count} batch replies");
  }

  /// <summary>
  /// Example 6: Using stream-based output for network transmission.
  /// </summary>
  public static void StreamBasedRepCreation()
  {
    using RepPacket rep = RepPacket.Create("MYBBS");

    // Add messages
    MessageBuilder builder = new MessageBuilder();
    builder.SetFrom("Alice")
           .SetTo("Sysop")
           .SetSubject("Test Message")
           .SetConferenceNumber(1)
           .SetBodyText("This REP will be sent directly to a stream.")
           .SetDateTime(DateTime.Now);

    rep.AddMessage(builder.Build());

    // Save to a MemoryStream for network transmission or further processing
    using System.IO.MemoryStream stream = new System.IO.MemoryStream();
    rep.Save(stream);

    // At this point, stream.ToArray() contains the complete REP packet
    Console.WriteLine($"Created REP packet in memory: {stream.Length} bytes");

    // Could now upload via HTTP, FTP, etc.
    // UploadToServer(stream.ToArray());
  }

  /// <summary>
  /// Example 7: Error handling when creating REP packets.
  /// </summary>
  public static void ErrorHandlingExample()
  {
    try
    {
      // Attempt to create with invalid BBS ID
      using RepPacket rep = RepPacket.Create("TOOLONGID"); // > 8 characters
      // This will throw ArgumentException
    }
    catch (ArgumentException ex)
    {
      Console.WriteLine($"Invalid BBS ID: {ex.Message}");
    }

    try
    {
      using RepPacket rep = RepPacket.Create("MYBBS");

      // Try to add a null message
      rep.AddMessage(null!);
      // This will throw ArgumentNullException
    }
    catch (ArgumentNullException ex)
    {
      Console.WriteLine($"Null message: {ex.Message}");
    }

    try
    {
      using RepPacket rep = RepPacket.Create("MYBBS");
      MessageBuilder builder = new MessageBuilder();

      // Try to build without required fields
      Message message = builder.Build();
      // This will throw InvalidOperationException
    }
    catch (InvalidOperationException ex)
    {
      Console.WriteLine($"Incomplete message: {ex.Message}");
    }

    Console.WriteLine("Error handling examples completed");
  }
}
