using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QwkNet.Diagnostics.Analysis;
using QwkNet.Validation;

namespace QwkNet.Diagnostics.Output;

/// <summary>
/// Formats analysis results as Markdown.
/// </summary>
internal sealed class MarkdownOutputFormatter : IOutputFormatter
{
  public string Format(AnalysisResult result)
  {
    StringBuilder md = new StringBuilder();

    md.AppendLine("# QWK Packet Analysis Report");
    md.AppendLine();

    // File Information
    md.AppendLine("## File Information");
    md.AppendLine();
    md.AppendLine($"- **Path:** `{result.FilePath}`");
    md.AppendLine($"- **File Name:** `{result.FileName}`");
    md.AppendLine($"- **File Size:** {FormatFileSize(result.FileSize)}");
    md.AppendLine($"- **Analysis Time:** {result.AnalysisTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
    md.AppendLine($"- **Validation Mode:** {result.ValidationMode}");
    md.AppendLine();

    if (!result.ParseSuccess)
    {
      md.AppendLine("## Parse Status: ❌ FAILED");
      md.AppendLine();
      md.AppendLine($"- **Error Type:** `{result.ParseErrorType}`");
      md.AppendLine($"- **Error Message:** {result.ParseError}");
      
      if (result.ParseTimeMs.HasValue)
      {
        md.AppendLine($"- **Parse Time:** {result.ParseTimeMs.Value} ms");
      }
      
      return md.ToString();
    }

    md.AppendLine("## Parse Status: ✅ SUCCESS");
    md.AppendLine();

    // Performance Metrics
    if (result.ParseTimeMs.HasValue || result.MemoryUsedBytes.HasValue)
    {
      md.AppendLine("## Performance");
      md.AppendLine();
      if (result.ParseTimeMs.HasValue)
      {
        md.AppendLine($"- **Parse Time:** {result.ParseTimeMs.Value} ms");
      }
      if (result.MemoryUsedBytes.HasValue)
      {
        md.AppendLine($"- **Memory Used:** {FormatFileSize(result.MemoryUsedBytes.Value)}");
      }
      md.AppendLine();
    }

    // BBS Information
    md.AppendLine("## BBS Information");
    md.AppendLine();
    md.AppendLine($"- **BBS Name:** {result.BbsName ?? "(not set)"}");
    md.AppendLine($"- **City:** {result.BbsCity ?? "(not set)"}");
    md.AppendLine($"- **Phone:** {result.BbsPhone ?? "(not set)"}");
    md.AppendLine($"- **Sysop:** {result.BbsSysop ?? "(not set)"}");
    md.AppendLine($"- **Packet ID:** {result.PacketId ?? "(not set)"}");
    
    if (result.PacketDate.HasValue)
    {
      md.AppendLine($"- **Packet Date:** {result.PacketDate.Value:yyyy-MM-dd HH:mm:ss}");
    }
    
    if (!string.IsNullOrEmpty(result.DoorId))
    {
      md.AppendLine($"- **Door ID:** {result.DoorId}");
    }
    md.AppendLine();

    // Message Statistics
    md.AppendLine("## Message Statistics");
    md.AppendLine();
    md.AppendLine($"- **Total Messages:** {result.MessageCount}");
    md.AppendLine($"- **Private Messages:** {result.PrivateMessageCount}");
    md.AppendLine($"- **Unread Messages:** {result.UnreadMessageCount}");
    md.AppendLine($"- **Read Messages:** {result.ReadMessageCount}");
    md.AppendLine($"- **Conferences:** {result.ConferenceCount}");
    md.AppendLine();

    // Conference Breakdown
    if (result.Conferences.Count > 0)
    {
      md.AppendLine("## Conferences");
      md.AppendLine();
      md.AppendLine("| Number | Name | Messages |");
      md.AppendLine("|--------|------|----------|");
      foreach (ConferenceAnalysis conf in result.Conferences)
      {
        md.AppendLine($"| {conf.Number} | {conf.Name} | {conf.MessageCount} |");
      }
      md.AppendLine();
    }

    // Optional Files
    if (result.OptionalFiles.Count > 0)
    {
      md.AppendLine("## Optional Files");
      md.AppendLine();
      foreach (string file in result.OptionalFiles)
      {
        md.AppendLine($"- `{file}`");
      }
      md.AppendLine();
    }

    // Validation Results
    if (result.HasValidationErrors || result.HasValidationWarnings)
    {
      md.AppendLine("## Validation Issues");
      md.AppendLine();
      
      if (result.HasValidationErrors)
      {
        md.AppendLine($"### Errors ({result.ValidationErrorCount})");
        md.AppendLine();
        
        if (result.ValidationReport != null)
        {
          foreach (ValidationIssue error in result.ValidationReport.Errors)
          {
            md.AppendLine($"- ❌ {error.Message}");
          }
        }
        md.AppendLine();
      }
      
      if (result.HasValidationWarnings)
      {
        md.AppendLine($"### Warnings ({result.ValidationWarningCount})");
        md.AppendLine();
        
        if (result.ValidationReport != null)
        {
          foreach (ValidationIssue warning in result.ValidationReport.Warnings)
          {
            md.AppendLine($"- ⚠️ {warning.Message}");
          }
        }
        md.AppendLine();
      }
    }

    // Sample Message
    if (!string.IsNullOrEmpty(result.SampleMessageFrom))
    {
      md.AppendLine("## Sample Message (First Message)");
      md.AppendLine();
      md.AppendLine($"- **From:** {result.SampleMessageFrom}");
      md.AppendLine($"- **To:** {result.SampleMessageTo}");
      md.AppendLine($"- **Subject:** {result.SampleMessageSubject}");
      md.AppendLine($"- **Body Preview:** {result.SampleMessageBodyPreview}");
      md.AppendLine();
    }

    return md.ToString();
  }

  public string FormatBatch(IReadOnlyList<AnalysisResult> results)
  {
    // Delegate to batch formatter
    MarkdownBatchOutputFormatter batchFormatter = new MarkdownBatchOutputFormatter(summaryOnly: false);
    return batchFormatter.FormatBatch(results);
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
}

/// <summary>
/// Formats batch analysis results as Markdown.
/// </summary>
internal sealed class MarkdownBatchOutputFormatter : IOutputFormatter
{
  private readonly bool _summaryOnly;

  public MarkdownBatchOutputFormatter(bool summaryOnly)
  {
    _summaryOnly = summaryOnly;
  }

  public string Format(AnalysisResult result)
  {
    MarkdownOutputFormatter inner = new MarkdownOutputFormatter();
    return inner.Format(result);
  }

  public string FormatBatch(IReadOnlyList<AnalysisResult> results)
  {
    StringBuilder md = new StringBuilder();

    md.AppendLine("# QWK Packet Batch Analysis Report");
    md.AppendLine();
    md.AppendLine($"**Analysis Date:** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    md.AppendLine($"**Total Packets:** {results.Count}");
    md.AppendLine();

    // Summary Statistics
    int successCount = results.Count(r => r.ParseSuccess);
    int failedCount = results.Count - successCount;
    int totalMessages = results.Where(r => r.ParseSuccess).Sum(r => r.MessageCount);
    int totalErrors = results.Where(r => r.ParseSuccess).Sum(r => r.ValidationErrorCount);
    int totalWarnings = results.Where(r => r.ParseSuccess).Sum(r => r.ValidationWarningCount);

    md.AppendLine("## Summary");
    md.AppendLine();
    md.AppendLine($"- **Successful Parses:** {successCount}");
    md.AppendLine($"- **Failed Parses:** {failedCount}");
    md.AppendLine($"- **Total Messages:** {totalMessages}");
    md.AppendLine($"- **Validation Errors:** {totalErrors}");
    md.AppendLine($"- **Validation Warnings:** {totalWarnings}");
    md.AppendLine();

    // Results Table
    md.AppendLine("## Results");
    md.AppendLine();
    md.AppendLine("| File | Status | Messages | Conferences | Errors | Warnings |");
    md.AppendLine("|------|--------|----------|-------------|--------|----------|");

    foreach (AnalysisResult result in results)
    {
      string status = result.ParseSuccess ? "✅" : "❌";
      string messages = result.ParseSuccess ? result.MessageCount.ToString() : "-";
      string conferences = result.ParseSuccess ? result.ConferenceCount.ToString() : "-";
      string errors = result.ParseSuccess ? result.ValidationErrorCount.ToString() : "-";
      string warnings = result.ParseSuccess ? result.ValidationWarningCount.ToString() : "-";

      md.AppendLine($"| `{result.FileName}` | {status} | {messages} | {conferences} | {errors} | {warnings} |");
    }
    md.AppendLine();

    // Failed Packets Detail
    List<AnalysisResult> failedResults = results.Where(r => !r.ParseSuccess).ToList();
    if (failedResults.Count > 0)
    {
      md.AppendLine("## Failed Packets");
      md.AppendLine();
      foreach (AnalysisResult result in failedResults)
      {
        md.AppendLine($"### `{result.FileName}`");
        md.AppendLine();
        md.AppendLine($"- **Error Type:** `{result.ParseErrorType}`");
        md.AppendLine($"- **Error Message:** {result.ParseError}");
        md.AppendLine();
      }
    }

    // Individual Packet Details (if not summary-only)
    if (!_summaryOnly)
    {
      md.AppendLine("## Individual Packet Details");
      md.AppendLine();

      foreach (AnalysisResult result in results.Where(r => r.ParseSuccess))
      {
        md.AppendLine($"### `{result.FileName}`");
        md.AppendLine();
        md.AppendLine($"- **BBS Name:** {result.BbsName}");
        md.AppendLine($"- **Messages:** {result.MessageCount}");
        md.AppendLine($"- **Conferences:** {result.ConferenceCount}");
        
        if (result.HasValidationErrors || result.HasValidationWarnings)
        {
          md.AppendLine($"- **Validation:** {result.ValidationErrorCount} error(s), {result.ValidationWarningCount} warning(s)");
        }
        
        md.AppendLine();
      }
    }

    return md.ToString();
  }
}