// QWK.NET - QwkPacket Usage Examples
// QWK Packet Reading

using System;
using System.IO;
using System.Linq;
using QwkNet;
using QwkNet.Models.Messages;
using QwkNet.Models.Control;

namespace QwkNet.Examples;

/// <summary>
/// Demonstrates how to read and process QWK packets using QWK.NET.
/// </summary>
public static class QwkPacketExamples
{
  /// <summary>
  /// Example 1: Basic QWK packet reading.
  /// </summary>
  public static void BasicQwkReading()
  {
    // Open a QWK packet from file
    using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

    // Display basic packet information
    Console.WriteLine($"BBS: {packet.Control.BbsName}");
    Console.WriteLine($"BBS ID: {packet.Control.BbsId}");
    Console.WriteLine($"Messages: {packet.Messages.Count}");
    Console.WriteLine();

    // Enumerate all messages
    Console.WriteLine("Messages:");
    foreach (Message message in packet.Messages)
    {
      Console.WriteLine($"  {message.From} → {message.To}: {message.Subject}");
    }
  }

  /// <summary>
  /// Example 2: Reading messages by conference.
  /// </summary>
  public static void ReadMessagesByConference()
  {
    using QwkPacket packet = QwkPacket.Open("MYBBS.QWK");

    Console.WriteLine("Messages by Conference:");
    Console.WriteLine();

    // List all conferences with message counts
    foreach (ConferenceInfo conf in packet.Conferences)
    {
      IReadOnlyList<Message> messages = packet.Messages.GetByConference(conf.Number);
      Console.WriteLine($"Conference {conf.Number}: {conf.Name}");
      Console.WriteLine($"  Messages: {messages.Count}");
      Console.WriteLine();
    }

    // Read messages from a specific conference (e.g., General discussion)
    ConferenceInfo? generalConf = packet.Conferences.FindByNumber(1);
    if (generalConf != null)
    {
      IReadOnlyList<Message> generalMessages = packet.Messages.GetByConference(1);
      Console.WriteLine($"Messages in '{generalConf.Name}':");
      
      foreach (Message message in generalMessages)
      {
        Console.WriteLine($"  [{message.MessageNumber}] {message.Subject}");
        Console.WriteLine($"      From: {message.From}");
        Console.WriteLine($"      Date: {message.DateTime:yyyy-MM-dd HH:mm}");
        Console.WriteLine();
      }
    }
  }

  /// <summary>
  /// Example 3: Reading private and unread messages.
  /// </summary>
  public static void ReadPrivateAndUnreadMessages()
  {
    using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

    // Get all private messages
    IReadOnlyList<Message> privateMessages = packet.Messages.GetPrivateMessages();
    Console.WriteLine($"You have {privateMessages.Count} private message(s):");
    Console.WriteLine();

    foreach (Message message in privateMessages)
    {
      Console.WriteLine($"From: {message.From.Trim()}");
      Console.WriteLine($"Subject: {message.Subject.Trim()}");
      Console.WriteLine($"Date: {message.DateTime:yyyy-MM-dd HH:mm}");
      Console.WriteLine();
    }

    // Get all unread messages
    IReadOnlyList<Message> unreadMessages = packet.Messages.GetUnreadMessages();
    Console.WriteLine($"You have {unreadMessages.Count} unread message(s)");
    Console.WriteLine();

    // Combine filters using LINQ for more specific queries
    IReadOnlyList<Message> unreadPrivate = packet.Messages
      .Where(m => m.IsPrivate && !m.IsRead)
      .ToList();

    Console.WriteLine($"Unread private messages: {unreadPrivate.Count}");

    // Find messages from a specific person
    IReadOnlyList<Message> fromSysop = packet.Messages
      .Where(m => m.From.Trim().Equals("SYSOP", StringComparison.OrdinalIgnoreCase))
      .ToList();

    Console.WriteLine($"Messages from Sysop: {fromSysop.Count}");

    // Find messages to you that need replies
    string userName = packet.Control.UserName.Trim();
    IReadOnlyList<Message> needsReply = packet.Messages
      .Where(m => m.To.Trim().Equals(userName, StringComparison.OrdinalIgnoreCase) && !m.IsRead)
      .ToList();

    Console.WriteLine($"Messages requiring your attention: {needsReply.Count}");
  }

  /// <summary>
  /// Example 4: Reading message bodies.
  /// </summary>
  public static void ReadMessageBodies()
  {
    using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

    if (packet.Messages.Count == 0)
    {
      Console.WriteLine("No messages in packet.");
      return;
    }

    Message message = packet.Messages[0];

    Console.WriteLine($"Message from {message.From} to {message.To}");
    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine($"Date: {message.DateTime:yyyy-MM-dd HH:mm}");
    Console.WriteLine();

    // Method 1: Access as individual lines
    Console.WriteLine("--- Message Body (Line by Line) ---");
    foreach (string line in message.Body.Lines)
    {
      Console.WriteLine(line);
    }
    Console.WriteLine();

    // Method 2: Access as full text with standard newlines
    Console.WriteLine("--- Message Body (Full Text) ---");
    string fullText = message.Body.GetDecodedText();
    Console.WriteLine(fullText);
    Console.WriteLine();

    // Method 3: Access raw QWK format (for preservation/archival)
    // RawText contains byte 0xE3 (π) terminators instead of newlines
    Console.WriteLine("--- Message Body (Raw QWK Format) ---");
    string rawText = message.Body.RawText;
    Console.WriteLine($"Raw text length: {rawText.Length} characters");
    Console.WriteLine($"Contains QWK terminators (0xE3/π): {rawText.Contains('\u03C0')}");
    Console.WriteLine();

    // Example: Search message content
    Console.WriteLine("--- Searching Message Content ---");
    bool containsKeyword = message.Body.Lines
      .Any(line => line.Contains("BBS", StringComparison.OrdinalIgnoreCase));
    
    if (containsKeyword)
    {
      Console.WriteLine("This message mentions 'BBS'");
    }

    // Example: Count message length
    int totalCharacters = message.Body.Lines.Sum(line => line.Length);
    Console.WriteLine($"Message body contains {message.Body.Lines.Count} line(s) and {totalCharacters} character(s)");
  }

  /// <summary>
  /// Example 5: Reading optional files (WELCOME, NEWS, GOODBYE).
  /// </summary>
  public static void ReadOptionalFiles()
  {
    using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");

    Console.WriteLine($"BBS: {packet.Control.BbsName}");
    Console.WriteLine();

    // Display welcome screen if present
    string? welcome = packet.OptionalFiles.GetWelcomeText();
    if (welcome != null)
    {
      Console.WriteLine("=== WELCOME ===");
      Console.WriteLine(welcome);
      Console.WriteLine();
    }
    else
    {
      Console.WriteLine("No WELCOME file present.");
      Console.WriteLine();
    }

    // Display news if present
    string? news = packet.OptionalFiles.GetNewsText();
    if (news != null)
    {
      Console.WriteLine("=== NEWS ===");
      Console.WriteLine(news);
      Console.WriteLine();
    }
    else
    {
      Console.WriteLine("No NEWS file present.");
      Console.WriteLine();
    }

    // Display goodbye screen if present
    string? goodbye = packet.OptionalFiles.GetGoodbyeText();
    if (goodbye != null)
    {
      Console.WriteLine("=== GOODBYE ===");
      Console.WriteLine(goodbye);
      Console.WriteLine();
    }
    else
    {
      Console.WriteLine("No GOODBYE file present.");
      Console.WriteLine();
    }

    // Check for specific optional files
    if (packet.OptionalFiles.HasFile("BULLETIN.TXT"))
    {
      Console.WriteLine("BBS bulletin found!");
      using Stream? stream = packet.OptionalFiles.OpenFile("BULLETIN.TXT");
      if (stream != null)
      {
        using StreamReader reader = new StreamReader(stream);
        string bulletin = reader.ReadToEnd();
        Console.WriteLine(bulletin);
      }
    }

    // List all files in the packet (if needed for debugging)
    // Note: This would require access to the archive, which is internal.
    // In practice, you'd check for known optional file names.
  }

  /// <summary>
  /// Example 6: Reading control data and BBS information.
  /// </summary>
  public static void ReadControlData()
  {
    using QwkPacket packet = QwkPacket.Open("DEMO1.QWK");
    ControlDat control = packet.Control;

    Console.WriteLine("=== BBS INFORMATION ===");
    Console.WriteLine();
    Console.WriteLine($"BBS Name:        {control.BbsName}");
    Console.WriteLine($"BBS ID:          {control.BbsId}");
    Console.WriteLine($"Location:        {control.City}");
    Console.WriteLine($"Phone:           {control.Phone}");
    Console.WriteLine($"Sysop Name:      {control.SysopName}");
    Console.WriteLine($"User Name:       {control.UserName}");
    Console.WriteLine($"User ID:         {control.UserId}");
    Console.WriteLine($"Packet Created:  {control.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine();

    Console.WriteLine($"Total Messages:  {packet.Messages.Count}");
    Console.WriteLine($"Total Conferences: {control.Conferences.Count}");
    Console.WriteLine();

    // Display conference details
    Console.WriteLine("=== CONFERENCES ===");
    Console.WriteLine();
    foreach (ConferenceInfo conf in control.Conferences)
    {
      int messageCount = packet.Messages.GetByConference(conf.Number).Count;
      Console.WriteLine($"  {conf.Number,3}: {conf.Name,-40} ({messageCount} message(s))");
    }
    Console.WriteLine();

    // Display DOOR.ID information if present
    if (packet.DoorId != null)
    {
      Console.WriteLine("=== DOOR SOFTWARE ===");
      Console.WriteLine();
      Console.WriteLine($"Door Name:    {packet.DoorId.DoorName}");
      Console.WriteLine($"Version:      {packet.DoorId.Version}");
      
      if (packet.DoorId.Capabilities.Count > 0)
      {
        Console.WriteLine($"Capabilities: {string.Join(", ", packet.DoorId.Capabilities)}");
      }
      Console.WriteLine();
    }
    else
    {
      Console.WriteLine("No DOOR.ID information present.");
      Console.WriteLine();
    }

    // Display optional file availability
    Console.WriteLine("=== OPTIONAL FILES ===");
    Console.WriteLine();
    Console.WriteLine($"WELCOME file:  {(control.WelcomeFile != null ? "Present" : "Not present")}");
    Console.WriteLine($"NEWS file:     {(control.NewsFile != null ? "Present" : "Not present")}");
    Console.WriteLine($"GOODBYE file:  {(control.GoodbyeFile != null ? "Present" : "Not present")}");
  }

  /// <summary>
  /// Example 7: Stream-based and memory-based reading.
  /// </summary>
  public static void StreamAndMemoryReading()
  {
    Console.WriteLine("=== Example 7: Stream and Memory-Based Reading ===");
    Console.WriteLine();

    // Example 7a: Read from a file stream
    Console.WriteLine("Method 1: Reading from FileStream");
    using (FileStream fileStream = new FileStream("DEMO1.QWK", FileMode.Open, FileAccess.Read, FileShare.Read))
    {
      using QwkPacket packet = QwkPacket.Open(fileStream);
      Console.WriteLine($"  Loaded {packet.Messages.Count} message(s) from {packet.Control.BbsName}");
    }
    Console.WriteLine();

    // Example 7b: Read from memory (e.g., after downloading via HTTP/FTP)
    Console.WriteLine("Method 2: Reading from memory buffer");
    
    // Simulate downloading packet data (in real scenarios, this might come from HTTP/FTP)
    byte[] packetData = File.ReadAllBytes("MYBBS.QWK");
    ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(packetData);
    
    using (QwkPacket packet = QwkPacket.Open(memory))
    {
      Console.WriteLine($"  Loaded {packet.Messages.Count} message(s) from memory buffer");
      Console.WriteLine($"  Buffer size: {packetData.Length:N0} bytes");
    }
    Console.WriteLine();

    // Example 7c: Simulated HTTP download scenario
    Console.WriteLine("Method 3: Simulated HTTP download scenario");
    SimulateHttpDownload("MYBBS.QWK");
    Console.WriteLine();

    // Example 7d: Process packet from memory without saving to disk
    Console.WriteLine("Method 4: In-memory processing (no disk I/O)");
    byte[] downloadedData = File.ReadAllBytes("MYBBS.QWK"); // In real scenario: from network
    ProcessPacketFromMemory(downloadedData);
  }

  /// <summary>
  /// Simulates downloading a QWK packet via HTTP and processing it.
  /// </summary>
  private static void SimulateHttpDownload(string localFilePath)
  {
    // In a real scenario, this would be:
    // using HttpClient client = new HttpClient();
    // using Stream httpStream = await client.GetStreamAsync("https://bbs.example.com/mail.qwk");
    
    // For this example, we simulate by reading from a local file
    using FileStream fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
    using QwkPacket packet = QwkPacket.Open(fileStream);
    
    Console.WriteLine($"  Downloaded and processed: {packet.Control.BbsName}");
    Console.WriteLine($"  Messages: {packet.Messages.Count}");
    Console.WriteLine($"  Unread: {packet.Messages.GetUnreadMessages().Count}");
  }

  /// <summary>
  /// Processes a QWK packet entirely in memory without disk I/O.
  /// </summary>
  private static void ProcessPacketFromMemory(byte[] packetData)
  {
    ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(packetData);
    using QwkPacket packet = QwkPacket.Open(memory);
    
    Console.WriteLine($"  BBS: {packet.Control.BbsName}");
    Console.WriteLine($"  Processing {packet.Messages.Count} message(s) in memory");
    
    // Example: Extract unread message subjects
    IReadOnlyList<Message> unread = packet.Messages.GetUnreadMessages();
    if (unread.Count > 0)
    {
      Console.WriteLine($"  Unread message subjects:");
      foreach (Message message in unread.Take(5)) // Show first 5
      {
        Console.WriteLine($"    - {message.Subject.Trim()}");
      }
      
      if (unread.Count > 5)
      {
        Console.WriteLine($"    ... and {unread.Count - 5} more");
      }
    }
  }

  /// <summary>
  /// Runs all QWK packet reading examples.
  /// </summary>
  public static void RunAll()
  {
    try
    {
      Console.WriteLine("=== QWK Packet Reading Examples ===");
      Console.WriteLine();

      Console.WriteLine("Example 1: Basic QWK Reading");
      Console.WriteLine("─────────────────────────────");
      BasicQwkReading();
      Console.WriteLine();

      Console.WriteLine("Example 2: Reading Messages by Conference");
      Console.WriteLine("──────────────────────────────────────────");
      ReadMessagesByConference();
      Console.WriteLine();

      Console.WriteLine("Example 3: Reading Private and Unread Messages");
      Console.WriteLine("───────────────────────────────────────────────");
      ReadPrivateAndUnreadMessages();
      Console.WriteLine();

      Console.WriteLine("Example 4: Reading Message Bodies");
      Console.WriteLine("──────────────────────────────────");
      ReadMessageBodies();
      Console.WriteLine();

      Console.WriteLine("Example 5: Reading Optional Files");
      Console.WriteLine("──────────────────────────────────");
      ReadOptionalFiles();
      Console.WriteLine();

      Console.WriteLine("Example 6: Reading Control Data");
      Console.WriteLine("────────────────────────────────");
      ReadControlData();
      Console.WriteLine();

      Console.WriteLine("Example 7: Stream and Memory-Based Reading");
      Console.WriteLine("───────────────────────────────────────────");
      StreamAndMemoryReading();
      Console.WriteLine();

      Console.WriteLine("All examples completed successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Example execution failed: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
  }
}