using System;
using System.Collections.Generic;
using System.Text;
using QwkNet.Diagnostics.Analysis;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Output;

/// <summary>
/// Formats analysis results as human-readable text.
/// </summary>
internal sealed class TextOutputFormatter : IOutputFormatter
{
  private readonly bool _verbose;

  public TextOutputFormatter(bool verbose)
  {
    _verbose = verbose;
  }

  public string Format(AnalysisResult result)
  {
    StringBuilder sb = new StringBuilder();

    sb.AppendLine("═══════════════════════════════════════════════════════════════");
    sb.AppendLine("QWK PACKET ANALYSIS");
    sb.AppendLine("═══════════════════════════════════════════════════════════════");
    sb.AppendLine();

    // File Information
    sb.AppendLine("FILE INFORMATION:");
    sb.AppendLine($"  Path:          {result.FilePath}");
    sb.AppendLine($"  File Name:     {result.FileName}");
    sb.AppendLine($"  File Size:     {FormatFileSize(result.FileSize)}");
    sb.AppendLine($"  Analysis Time: {result.AnalysisTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
    sb.AppendLine($"  Mode:          {result.ValidationMode}");
    sb.AppendLine();

    if (!result.ParseSuccess)
    {
      sb.AppendLine("PARSE STATUS: ✗ FAILED");
      sb.AppendLine($"  Error Type:    {result.ParseErrorType}");
      sb.AppendLine($"  Error Message: {result.ParseError}");
      
      if (_verbose && !string.IsNullOrEmpty(result.ParseErrorStackTrace))
      {
        sb.AppendLine();
        sb.AppendLine("  Stack Trace:");
        string[] stackLines = result.ParseErrorStackTrace.Split('\n');
        foreach (string line in stackLines)
        {
          if (!string.IsNullOrWhiteSpace(line))
          {
            sb.AppendLine($"    {line.Trim()}");
          }
        }
      }
      
      if (result.ParseTimeMs.HasValue)
      {
        sb.AppendLine($"  Parse Time:    {result.ParseTimeMs.Value} ms");
      }
      
      return sb.ToString();
    }

    sb.AppendLine("PARSE STATUS: ✓ SUCCESS");
    sb.AppendLine();

    // Performance Metrics
    if (result.ParseTimeMs.HasValue || result.MemoryUsedBytes.HasValue)
    {
      sb.AppendLine("PERFORMANCE:");
      if (result.ParseTimeMs.HasValue)
      {
        sb.AppendLine($"  Parse Time:    {result.ParseTimeMs.Value} ms");
      }
      if (result.MemoryUsedBytes.HasValue)
      {
        sb.AppendLine($"  Memory Used:   {FormatFileSize(result.MemoryUsedBytes.Value)}");
      }
      sb.AppendLine();
    }

    // BBS Information
    sb.AppendLine("BBS INFORMATION:");
    sb.AppendLine($"  BBS Name:      {result.BbsName ?? "(not set)"}");
    sb.AppendLine($"  City:          {result.BbsCity ?? "(not set)"}");
    sb.AppendLine($"  Phone:         {result.BbsPhone ?? "(not set)"}");
    sb.AppendLine($"  Sysop:         {result.BbsSysop ?? "(not set)"}");
    sb.AppendLine($"  Packet ID:     {result.PacketId ?? "(not set)"}");
    
    if (result.PacketDate.HasValue)
    {
      sb.AppendLine($"  Packet Date:   {result.PacketDate.Value:yyyy-MM-dd HH:mm:ss}");
    }
    
    if (!string.IsNullOrEmpty(result.DoorId))
    {
      sb.AppendLine($"  Door ID:       {result.DoorId} {result.DoorVersion}");
      if (!string.IsNullOrEmpty(result.DoorSystem))
      {
        sb.AppendLine($"  Door System:   {result.DoorSystem}");
      }
    }
    sb.AppendLine();

    // Message Statistics
    sb.AppendLine("MESSAGE STATISTICS:");
    sb.AppendLine($"  Total Messages:   {result.MessageCount}");
    sb.AppendLine($"  Private Messages: {result.PrivateMessageCount}");
    sb.AppendLine($"  Unread Messages:  {result.UnreadMessageCount}");
    sb.AppendLine($"  Read Messages:    {result.ReadMessageCount}");
    sb.AppendLine($"  Conferences:      {result.ConferenceCount}");
    sb.AppendLine();

    // CP437 Encoding Analysis
    if (_verbose && result.Cp437Analysis != null)
    {
      sb.AppendLine("CP437 ENCODING ANALYSIS:");
      sb.AppendLine($"  Box-drawing in BBS name:       {YesNo(result.Cp437Analysis.BoxDrawingInBbsName)}");
      sb.AppendLine($"  Box-drawing in conferences:    {YesNo(result.Cp437Analysis.BoxDrawingInConferenceNames)}");
      sb.AppendLine($"  Box-drawing in message bodies: {YesNo(result.Cp437Analysis.BoxDrawingInMessageBodies)}");
      sb.AppendLine($"  International characters:      {YesNo(result.Cp437Analysis.InternationalCharsDetected)}");
      sb.AppendLine($"  ANSI escape sequences:         {YesNo(result.Cp437Analysis.AnsiEscapeSequencesDetected)}");
      sb.AppendLine($"  Line terminator (0xE3) count:  {result.Cp437Analysis.LineTerminator0xE3Count}");
      sb.AppendLine();
    }

    // QWKE Extensions Analysis
    if (_verbose && result.QwkeAnalysis != null)
    {
      bool hasQwkeFeatures = result.QwkeAnalysis.HasToReaderExt ||
                              result.QwkeAnalysis.HasToDoorExt ||
                              result.QwkeAnalysis.MessagesWithLongHeaders > 0;

      if (hasQwkeFeatures)
      {
        sb.AppendLine("QWKE EXTENSIONS:");
        sb.AppendLine($"  TOREADER.EXT present:          {YesNo(result.QwkeAnalysis.HasToReaderExt)}");
        sb.AppendLine($"  TODOOR.EXT present:            {YesNo(result.QwkeAnalysis.HasToDoorExt)}");
        sb.AppendLine($"  Messages with long headers:    {result.QwkeAnalysis.MessagesWithLongHeaders}");
        
        if (result.QwkeAnalysis.MessagesWithLongHeaders > 0)
        {
          sb.AppendLine($"    Extended To: headers:        {YesNo(result.QwkeAnalysis.HasExtendedToHeaders)}");
          sb.AppendLine($"    Extended From: headers:      {YesNo(result.QwkeAnalysis.HasExtendedFromHeaders)}");
          sb.AppendLine($"    Extended Subject: headers:   {YesNo(result.QwkeAnalysis.HasExtendedSubjectHeaders)}");
        }
        sb.AppendLine();
      }
    }

    // Conference Breakdown
    if (result.Conferences.Count > 0 && _verbose)
    {
      sb.AppendLine("CONFERENCES:");
      foreach (ConferenceAnalysis conf in result.Conferences)
      {
        sb.AppendLine($"  {conf.Number,3}: {conf.Name,-30} ({conf.MessageCount} message(s))");
      }
      sb.AppendLine();
    }

    // Optional Files
    if (result.OptionalFiles.Count > 0)
    {
      sb.AppendLine("OPTIONAL FILES:");
      foreach (string file in result.OptionalFiles)
      {
        sb.AppendLine($"  - {file}");
      }
      sb.AppendLine();
    }

    // Archive Inventory
    if (result.ArchiveInventory != null)
    {
      sb.AppendLine("ARCHIVE INVENTORY:");
      
      if (!string.IsNullOrEmpty(result.ArchiveInventory.InventoryError))
      {
        sb.AppendLine($"  ERROR: {result.ArchiveInventory.InventoryError}");
      }
      else
      {
        sb.AppendLine($"  Total Files:   {result.ArchiveInventory.TotalFiles}");
        sb.AppendLine($"  Total Size:    {FormatFileSize(result.ArchiveInventory.TotalUncompressedSize)}");
        sb.AppendLine();
        sb.AppendLine("  Files:");
        
        foreach (ArchiveFileEntry file in result.ArchiveInventory.Files)
        {
          string marker = file.IsRequired ? "*" : " ";
          sb.AppendLine($"    {marker} {file.FileName,-20} {FormatFileSize(file.UncompressedSize),10}  ({file.FileType})");
        }
        sb.AppendLine();
        sb.AppendLine("  (* = Required file)");
      }
      sb.AppendLine();
    }

    // Round-trip Validation
    if (result.RoundtripValidation != null)
    {
      RoundtripValidation rt = result.RoundtripValidation;
      
      sb.AppendLine("ROUND-TRIP VALIDATION:");
      
      if (!string.IsNullOrEmpty(rt.Error))
      {
        sb.AppendLine($"  Status:        ✗ FAILED");
        sb.AppendLine($"  Error Type:    {rt.ErrorType}");
        sb.AppendLine($"  Error Message: {rt.Error}");
      }
      else
      {
        sb.AppendLine($"  Status:        {(rt.Success ? "✓ SUCCESS" : "✗ FAILED")}");
        sb.AppendLine();
        sb.AppendLine("  File Sizes:");
        sb.AppendLine($"    Original:    {FormatFileSize(rt.OriginalSize)}");
        sb.AppendLine($"    Rewritten:   {FormatFileSize(rt.RewrittenSize)}");
        sb.AppendLine($"    Match:       {YesNo(rt.SizeMatches)}");
        sb.AppendLine();
        sb.AppendLine("  Structural Comparison:");
        sb.AppendLine($"    Metadata:    {YesNo(rt.MetadataMatches)}");
        sb.AppendLine($"    Msg Count:   {YesNo(rt.MessageCountMatches)}");
        sb.AppendLine($"    All Messages:{YesNo(rt.AllMessagesMatch)}");
        sb.AppendLine();
        sb.AppendLine("  Byte-Level Comparison:");
        sb.AppendLine($"    Perfect:     {YesNo(rt.BytePerfectMatch)}");
        
        if (rt.ByteLevelDifferences.Count > 0)
        {
          sb.AppendLine($"    Differences: {rt.ByteLevelDifferences.Count}");
          
          if (_verbose)
          {
            sb.AppendLine();
            sb.AppendLine("  First Byte Differences:");
            int displayCount = Math.Min(20, rt.ByteLevelDifferences.Count);
            
            for (int i = 0; i < displayCount; i++)
            {
              ByteDifference diff = rt.ByteLevelDifferences[i];
              sb.AppendLine($"    Offset 0x{diff.Offset:X8}: 0x{diff.OriginalByte:X2} → 0x{diff.RewrittenByte:X2}");
              sb.AppendLine($"      Context: {diff.Context}");
            }
            
            if (rt.ByteLevelDifferences.Count > displayCount)
            {
              sb.AppendLine($"    ... and {rt.ByteLevelDifferences.Count - displayCount} more");
            }
          }
        }
        
        if (rt.WriteTimeMs.HasValue || rt.ReadTimeMs.HasValue)
        {
          sb.AppendLine();
          sb.AppendLine("  Performance:");
          if (rt.WriteTimeMs.HasValue)
          {
            sb.AppendLine($"    Write Time:  {rt.WriteTimeMs.Value} ms");
          }
          if (rt.ReadTimeMs.HasValue)
          {
            sb.AppendLine($"    Read Time:   {rt.ReadTimeMs.Value} ms");
          }
        }
      }
      sb.AppendLine();
    }

    // Validation Results
    if (result.HasValidationErrors || result.HasValidationWarnings)
    {
      sb.AppendLine("VALIDATION ISSUES:");
      
      if (result.HasValidationErrors)
      {
        sb.AppendLine($"  Errors:   {result.ValidationErrorCount}");
        
        if (_verbose && result.ValidationReport != null)
        {
          foreach (ValidationIssue error in result.ValidationReport.Errors)
          {
            sb.AppendLine($"    ✗ {error.Message}");
          }
        }
      }
      
      if (result.HasValidationWarnings)
      {
        sb.AppendLine($"  Warnings: {result.ValidationWarningCount}");
        
        if (_verbose && result.ValidationReport != null)
        {
          foreach (ValidationIssue warning in result.ValidationReport.Warnings)
          {
            sb.AppendLine($"    ⚠ {warning.Message}");
          }
        }
      }
      sb.AppendLine();
    }

    // Sample Message
    if (_verbose && !string.IsNullOrEmpty(result.SampleMessageFrom))
    {
      sb.AppendLine("SAMPLE MESSAGE (First Message):");
      sb.AppendLine($"  From:    {result.SampleMessageFrom}");
      sb.AppendLine($"  To:      {result.SampleMessageTo}");
      sb.AppendLine($"  Subject: {result.SampleMessageSubject}");
      sb.AppendLine($"  Body:    {result.SampleMessageBodyPreview}");
      sb.AppendLine();
    }

    sb.AppendLine("═══════════════════════════════════════════════════════════════");

    return sb.ToString();
  }

  public string FormatBatch(IReadOnlyList<AnalysisResult> results)
  {
    // Not used in text mode (batch uses markdown)
    throw new NotSupportedException("Batch text output not supported. Use markdown or json.");
  }

  private string FormatFileSize(long bytes)
  {
    string[] sizes = { "B", "KB", "MB", "GB" };
    double size = bytes;
    int order = 0;

    while (size >= 1024 && order < sizes.Length - 1)
    {
      order++;
      size /= 1024;
    }

    return $"{size:0.##} {sizes[order]}";
  }

  private string YesNo(bool value)
  {
    return value ? "Yes" : "No";
  }
}