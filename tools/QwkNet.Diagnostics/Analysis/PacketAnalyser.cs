using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using QwkNet;
using QwkNet.Archive;
using QwkNet.Models.Control;
using QwkNet.Models.Messages;
using QwkNet.Models.Qwke;
using QwkNet.Parsing.Qwke;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Analysis;

/// <summary>
/// Analyses QWK/REP packets and produces diagnostic reports.
/// </summary>
internal sealed class PacketAnalyser
{
  public AnalysisResult Analyse(string filePath, ValidationMode mode, bool includeBenchmark, bool includeMemory, bool performRoundtrip, bool includeInventory)
  {
    AnalysisResult result = new AnalysisResult
    {
      FilePath = filePath,
      FileName = Path.GetFileName(filePath),
      FileSize = new FileInfo(filePath).Length,
      ValidationMode = mode,
      AnalysisTimestamp = DateTimeOffset.UtcNow
    };

    Stopwatch stopwatch = Stopwatch.StartNew();
    long memoryBefore = 0;
    long memoryAfter = 0;

    if (includeMemory)
    {
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();
      memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
    }

    try
    {
      using QwkPacket packet = QwkPacket.Open(filePath, mode);
      
      stopwatch.Stop();

      if (includeMemory)
      {
        memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        result.MemoryUsedBytes = memoryAfter - memoryBefore;
      }

      if (includeBenchmark)
      {
        result.ParseTimeMs = stopwatch.ElapsedMilliseconds;
      }

      // Perform archive inventory if requested
      if (includeInventory)
      {
        result.ArchiveInventory = PerformArchiveInventory(filePath);
      }

      // Extract packet metadata
      result.ParseSuccess = true;
      result.BbsName = packet.Control.BbsName;
      result.BbsCity = packet.Control.BbsCity;
      result.BbsPhone = packet.Control.BbsPhone;
      result.BbsSysop = packet.Control.Sysop;
      result.PacketId = packet.Control.BbsId;
      result.PacketDate = packet.Control.CreatedAt;
      result.MessageCount = packet.Messages.Count;
      result.ConferenceCount = packet.Conferences.Count;

      // Extract conference information
      result.Conferences = new List<ConferenceAnalysis>();
      foreach (ConferenceInfo conf in packet.Conferences)
      {
        ConferenceAnalysis confAnalysis = new ConferenceAnalysis
        {
          Number = conf.Number,
          Name = conf.Name,
          MessageCount = packet.Messages.GetByConference(conf.Number).Count
        };
        result.Conferences.Add(confAnalysis);
      }

      // Extract message statistics
      result.PrivateMessageCount = packet.Messages.GetPrivateMessages().Count;
      result.UnreadMessageCount = packet.Messages.GetUnreadMessages().Count;
      result.ReadMessageCount = packet.Messages.Count - result.UnreadMessageCount;

      // Extract validation information
      result.ValidationReport = packet.ValidationReport;
      result.HasValidationErrors = packet.ValidationReport.HasErrors;
      result.HasValidationWarnings = packet.ValidationReport.HasWarnings;
      result.ValidationErrorCount = packet.ValidationReport.Errors.Count;
      result.ValidationWarningCount = packet.ValidationReport.Warnings.Count;

      // Extract optional files
      result.OptionalFiles = new List<string>();
      if (packet.OptionalFiles.HasFile("WELCOME"))
      {
        result.OptionalFiles.Add("WELCOME");
      }
      if (packet.OptionalFiles.HasFile("NEWS"))
      {
        result.OptionalFiles.Add("NEWS");
      }
      if (packet.OptionalFiles.HasFile("GOODBYE"))
      {
        result.OptionalFiles.Add("GOODBYE");
      }

      // Extract DOOR.ID if present
      if (packet.DoorId != null)
      {
        result.DoorId = packet.DoorId.DoorName;
        result.DoorVersion = packet.DoorId.Version;
        result.DoorSystem = packet.DoorId.SystemType;
      }

      // CP437 encoding analysis
      result.Cp437Analysis = AnalyseCp437Content(packet);

      // QWKE extension analysis
      result.QwkeAnalysis = AnalyseQwkeExtensions(packet);

      // Sample first message for encoding analysis
      if (packet.Messages.Count > 0)
      {
        Message firstMessage = packet.Messages[0];
        result.SampleMessageFrom = firstMessage.From;
        result.SampleMessageTo = firstMessage.To;
        result.SampleMessageSubject = firstMessage.Subject;
        result.SampleMessageBodyPreview = GetBodyPreview(firstMessage.Body);
      }

      // Perform round-trip validation if requested
      if (performRoundtrip)
      {
        result.RoundtripValidation = PerformRoundtripValidation(packet, filePath, mode);
      }
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      result.ParseSuccess = false;
      result.ParseError = ex.Message;
      result.ParseErrorType = ex.GetType().Name;
      result.ParseErrorStackTrace = ex.StackTrace;

      if (includeMemory)
      {
        memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        result.MemoryUsedBytes = memoryAfter - memoryBefore;
      }

      if (includeBenchmark)
      {
        result.ParseTimeMs = stopwatch.ElapsedMilliseconds;
      }
    }

    return result;
  }

  private Cp437AnalysisResult AnalyseCp437Content(QwkPacket packet)
  {
    Cp437AnalysisResult analysis = new Cp437AnalysisResult();

    // Check BBS name for box-drawing characters (0xB0-0xDF range)
    analysis.BoxDrawingInBbsName = ContainsBoxDrawing(packet.Control.BbsName);

    // Check conference names
    foreach (ConferenceInfo conf in packet.Conferences)
    {
      if (ContainsBoxDrawing(conf.Name))
      {
        analysis.BoxDrawingInConferenceNames = true;
        break;
      }
    }

    // Check messages for CP437 content
    foreach (Message msg in packet.Messages)
    {
      // Check headers
      if (ContainsInternationalChars(msg.From) || ContainsInternationalChars(msg.To) || ContainsInternationalChars(msg.Subject))
      {
        analysis.InternationalCharsDetected = true;
      }

      // Check body for box-drawing and ANSI
      string bodyText = string.Join("\n", msg.Body.Lines);
      
      if (ContainsBoxDrawing(bodyText))
      {
        analysis.BoxDrawingInMessageBodies = true;
      }

      if (ContainsAnsiEscapes(bodyText))
      {
        analysis.AnsiEscapeSequencesDetected = true;
      }

      // Check for 0xE3 line terminators in raw text
      if (msg.Body.RawText.Contains('\u03C0')) // Ï€ (U+03C0) encodes to 0xE3 in CP437
      {
        analysis.LineTerminator0xE3Count++;
      }
    }

    return analysis;
  }

  private QwkeAnalysisResult AnalyseQwkeExtensions(QwkPacket packet)
  {
    QwkeAnalysisResult analysis = new QwkeAnalysisResult();

    // Check for QWKE files (TOREADER.EXT, TODOOR.EXT)
    analysis.HasToReaderExt = packet.OptionalFiles.HasFile("TOREADER.EXT");
    analysis.HasToDoorExt = packet.OptionalFiles.HasFile("TODOOR.EXT");

    // Analyse messages for QWKE long headers
    foreach (Message msg in packet.Messages)
    {
      if (msg.Kludges.Count > 0)
      {
        QwkeLongHeaders longHeaders = QwkeLongHeaderParser.Parse(msg.Kludges);
        
        if (longHeaders.HasLongHeaders)
        {
          analysis.MessagesWithLongHeaders++;
          
          if (longHeaders.ExtendedTo != null)
          {
            analysis.HasExtendedToHeaders = true;
          }
          
          if (longHeaders.ExtendedFrom != null)
          {
            analysis.HasExtendedFromHeaders = true;
          }
          
          if (longHeaders.ExtendedSubject != null)
          {
            analysis.HasExtendedSubjectHeaders = true;
          }
        }
      }
    }

    return analysis;
  }

  private bool ContainsBoxDrawing(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return false;
    }

    // Box-drawing characters in CP437 are in range 0xB0-0xDF
    // These map to Unicode: â–‘â–’â–“â”‚â”¤â•¡â•¢â•–â••â•£â•‘â•—â•â•œâ•›â”â””â”´â”¬â”œâ”€â”¼â•žâ•Ÿâ•šâ•”â•©â•¦â• â•â•¬â•§â•¨â•¤â•¥â•™â•˜â•’â•“â•«â•ªâ”˜â”Œâ–ˆâ–„â–Œâ–â–€
    foreach (char c in text)
    {
      if (c >= '\u2591' && c <= '\u2593') return true; // â–‘â–’â–“
      if (c >= '\u2500' && c <= '\u257F') return true; // Box Drawing range
    }

    return false;
  }

  private bool ContainsInternationalChars(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return false;
    }

    // Check for common international characters (accented letters)
    // CP437 codes 0x80-0x9F contain international characters
    foreach (char c in text)
    {
      if (c >= '\u00C0' && c <= '\u00FF') return true; // Latin-1 Supplement
      if (c == '\u00E9' || c == '\u00E8' || c == '\u00EA' || c == '\u00EB') return true;
      if (c == '\u00E1' || c == '\u00E0' || c == '\u00E2' || c == '\u00E4' || c == '\u00E5') return true;
      if (c == '\u00F1' || c == '\u00FC' || c == '\u00F6' || c == '\u00E7') return true;
    }

    return false;
  }

  private bool ContainsAnsiEscapes(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return false;
    }

    // ANSI escape sequences start with ESC (0x1B) followed by [
    return text.Contains("\x1B[");
  }

  private string GetBodyPreview(MessageBody body)
  {
    if (body.Lines.Count == 0)
    {
      return "(empty)";
    }

    string firstLine = body.Lines[0];
    if (firstLine.Length > 100)
    {
      return firstLine.Substring(0, 100) + "...";
    }

    return firstLine;
  }

  private ArchiveInventory PerformArchiveInventory(string filePath)
  {
    ArchiveInventory inventory = new ArchiveInventory();

    try
    {
      using IArchiveReader reader = ArchiveFactory.OpenArchive(filePath);
      IReadOnlyList<string> fileList = reader.ListFiles();

      foreach (string fileName in fileList)
      {
        using Stream fileStream = reader.OpenFile(fileName);
        using MemoryStream ms = new MemoryStream();
        fileStream.CopyTo(ms);
        
        ArchiveFileEntry entry = new ArchiveFileEntry
        {
          FileName = fileName,
          UncompressedSize = (int)ms.Length,
          IsRequired = IsRequiredFile(fileName),
          FileType = DetermineFileType(fileName)
        };
        inventory.Files.Add(entry);
      }

      inventory.TotalFiles = inventory.Files.Count;
      inventory.TotalUncompressedSize = inventory.Files.Sum(f => (long)f.UncompressedSize);
    }
    catch (Exception ex)
    {
      inventory.InventoryError = $"Failed to enumerate archive: {ex.Message}";
    }

    return inventory;
  }

  private RoundtripValidation PerformRoundtripValidation(QwkPacket originalPacket, string originalPath, ValidationMode mode)
  {
    RoundtripValidation validation = new RoundtripValidation();

    // Note: QwkPacket is read-only by design and doesn't support writing.
    // Round-trip validation would require RepPacket functionality which is
    // for reply packets, not QWK packets.
    // 
    // For now, we document this limitation and mark as not supported.
    
    validation.Success = false;
    validation.Error = "Round-trip validation is not supported for QWK packets. QwkPacket is read-only by design. " +
                      "Round-trip validation requires packet writing capabilities which are only available for REP packets.";
    validation.ErrorType = "NotSupportedException";

    return validation;
  }

  private bool ComparePacketMetadata(QwkPacket original, QwkPacket rewritten)
  {
    return original.Control.BbsName == rewritten.Control.BbsName &&
           original.Control.BbsCity == rewritten.Control.BbsCity &&
           original.Control.BbsPhone == rewritten.Control.BbsPhone &&
           original.Control.Sysop == rewritten.Control.Sysop &&
           original.Control.BbsId == rewritten.Control.BbsId &&
           original.Control.CreatedAt == rewritten.Control.CreatedAt;
  }

  private bool CompareAllMessages(QwkPacket original, QwkPacket rewritten)
  {
    if (original.Messages.Count != rewritten.Messages.Count)
    {
      return false;
    }

    for (int i = 0; i < original.Messages.Count; i++)
    {
      Message originalMsg = original.Messages[i];
      Message rewrittenMsg = rewritten.Messages[i];

      if (originalMsg.From != rewrittenMsg.From ||
          originalMsg.To != rewrittenMsg.To ||
          originalMsg.Subject != rewrittenMsg.Subject ||
          originalMsg.ConferenceNumber != rewrittenMsg.ConferenceNumber ||
          originalMsg.MessageNumber != rewrittenMsg.MessageNumber ||
          originalMsg.ReferenceNumber != rewrittenMsg.ReferenceNumber ||
          originalMsg.Status != rewrittenMsg.Status ||
          originalMsg.DateTime != rewrittenMsg.DateTime)
      {
        return false;
      }

      // Compare body lines
      if (originalMsg.Body.Lines.Count != rewrittenMsg.Body.Lines.Count)
      {
        return false;
      }

      for (int j = 0; j < originalMsg.Body.Lines.Count; j++)
      {
        if (originalMsg.Body.Lines[j] != rewrittenMsg.Body.Lines[j])
        {
          return false;
        }
      }
    }

    return true;
  }

  private bool IsRequiredFile(string fileName)
  {
    string upperName = fileName.ToUpperInvariant();
    return upperName == "CONTROL.DAT" || 
           upperName == "MESSAGES.DAT" ||
           upperName.EndsWith(".NDX");
  }

  private string DetermineFileType(string fileName)
  {
    string upperName = fileName.ToUpperInvariant();

    if (upperName == "CONTROL.DAT") return "Control File";
    if (upperName == "MESSAGES.DAT") return "Message Data";
    if (upperName.EndsWith(".NDX")) return "Index File";
    if (upperName == "DOOR.ID") return "Door Identification";
    if (upperName == "WELCOME") return "Welcome Screen";
    if (upperName == "NEWS") return "News File";
    if (upperName == "GOODBYE") return "Goodbye Screen";
    if (upperName == "TOREADER.EXT") return "QWKE ToReader Extensions";
    if (upperName == "TODOOR.EXT") return "QWKE ToDoor Extensions";

    return "Optional File";
  }
}

/// <summary>
/// Results from packet analysis.
/// </summary>
internal sealed class AnalysisResult
{
  public string FilePath { get; set; } = string.Empty;
  public string FileName { get; set; } = string.Empty;
  public long FileSize { get; set; }
  public ValidationMode ValidationMode { get; set; }
  public DateTimeOffset AnalysisTimestamp { get; set; }

  public bool ParseSuccess { get; set; }
  public string? ParseError { get; set; }
  public string? ParseErrorType { get; set; }
  public string? ParseErrorStackTrace { get; set; }
  public long? ParseTimeMs { get; set; }
  public long? MemoryUsedBytes { get; set; }

  public string? BbsName { get; set; }
  public string? BbsCity { get; set; }
  public string? BbsPhone { get; set; }
  public string? BbsSysop { get; set; }
  public string? PacketId { get; set; }
  public DateTimeOffset? PacketDate { get; set; }
  public string? DoorId { get; set; }
  public string? DoorVersion { get; set; }
  public string? DoorSystem { get; set; }

  public int MessageCount { get; set; }
  public int ConferenceCount { get; set; }
  public int PrivateMessageCount { get; set; }
  public int UnreadMessageCount { get; set; }
  public int ReadMessageCount { get; set; }

  public List<ConferenceAnalysis> Conferences { get; set; } = new List<ConferenceAnalysis>();
  public List<string> OptionalFiles { get; set; } = new List<string>();

  public ValidationReport? ValidationReport { get; set; }
  public bool HasValidationErrors { get; set; }
  public bool HasValidationWarnings { get; set; }
  public int ValidationErrorCount { get; set; }
  public int ValidationWarningCount { get; set; }

  public string? SampleMessageFrom { get; set; }
  public string? SampleMessageTo { get; set; }
  public string? SampleMessageSubject { get; set; }
  public string? SampleMessageBodyPreview { get; set; }

  public Cp437AnalysisResult? Cp437Analysis { get; set; }
  public QwkeAnalysisResult? QwkeAnalysis { get; set; }
  public ArchiveInventory? ArchiveInventory { get; set; }
  public RoundtripValidation? RoundtripValidation { get; set; }
}

/// <summary>
/// CP437 encoding analysis data.
/// </summary>
internal sealed class Cp437AnalysisResult
{
  public bool BoxDrawingInBbsName { get; set; }
  public bool BoxDrawingInConferenceNames { get; set; }
  public bool BoxDrawingInMessageBodies { get; set; }
  public bool InternationalCharsDetected { get; set; }
  public bool AnsiEscapeSequencesDetected { get; set; }
  public int LineTerminator0xE3Count { get; set; }
}

/// <summary>
/// QWKE extension analysis data.
/// </summary>
internal sealed class QwkeAnalysisResult
{
  public bool HasToReaderExt { get; set; }
  public bool HasToDoorExt { get; set; }
  public int MessagesWithLongHeaders { get; set; }
  public bool HasExtendedToHeaders { get; set; }
  public bool HasExtendedFromHeaders { get; set; }
  public bool HasExtendedSubjectHeaders { get; set; }
}

/// <summary>
/// Conference analysis data.
/// </summary>
internal sealed class ConferenceAnalysis
{
  public int Number { get; set; }
  public string Name { get; set; } = string.Empty;
  public int MessageCount { get; set; }
}

/// <summary>
/// Archive file inventory data.
/// </summary>
internal sealed class ArchiveInventory
{
  public List<ArchiveFileEntry> Files { get; set; } = new List<ArchiveFileEntry>();
  public int TotalFiles { get; set; }
  public long TotalUncompressedSize { get; set; }
  public string? InventoryError { get; set; }
}

/// <summary>
/// Individual file entry in archive inventory.
/// </summary>
internal sealed class ArchiveFileEntry
{
  public string FileName { get; set; } = string.Empty;
  public int UncompressedSize { get; set; }
  public bool IsRequired { get; set; }
  public string FileType { get; set; } = string.Empty;
}

/// <summary>
/// Round-trip validation results (read → write → read → compare).
/// </summary>
internal sealed class RoundtripValidation
{
  public bool Success { get; set; }
  public long OriginalSize { get; set; }
  public long RewrittenSize { get; set; }
  public bool SizeMatches { get; set; }
  public bool MetadataMatches { get; set; }
  public bool MessageCountMatches { get; set; }
  public bool AllMessagesMatch { get; set; }
  public bool BytePerfectMatch { get; set; }
  public long? WriteTimeMs { get; set; }
  public long? ReadTimeMs { get; set; }
  public List<ByteDifference> ByteLevelDifferences { get; set; } = new List<ByteDifference>();
  public string? Error { get; set; }
  public string? ErrorType { get; set; }
}

/// <summary>
/// Byte-level difference between original and rewritten packet.
/// </summary>
internal sealed class ByteDifference
{
  public int Offset { get; set; }
  public byte OriginalByte { get; set; }
  public byte RewrittenByte { get; set; }
  public string Context { get; set; } = string.Empty;
}