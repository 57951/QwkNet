using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QwkNet.Diagnostics.Analysis;

namespace QwkNet.Diagnostics.Output;

/// <summary>
/// Formats analysis results as JSON.
/// </summary>
internal sealed class JsonOutputFormatter : IOutputFormatter
{
  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter() }
  };

  public string Format(AnalysisResult result)
  {
    JsonDocument doc = CreateJsonDocument(result);
    return JsonSerializer.Serialize(doc, JsonOptions);
  }

  public string FormatBatch(IReadOnlyList<AnalysisResult> results)
  {
    List<JsonDocument> documents = new List<JsonDocument>();
    foreach (AnalysisResult result in results)
    {
      documents.Add(CreateJsonDocument(result));
    }

    return JsonSerializer.Serialize(documents, JsonOptions);
  }

  private JsonDocument CreateJsonDocument(AnalysisResult result)
  {
    StringBuilder json = new StringBuilder();
    json.Append("{");

    // File info
    json.Append($"\"filePath\":\"{EscapeJson(result.FilePath)}\",");
    json.Append($"\"fileName\":\"{EscapeJson(result.FileName)}\",");
    json.Append($"\"fileSize\":{result.FileSize},");
    json.Append($"\"analysisTimestamp\":\"{result.AnalysisTimestamp:O}\",");
    json.Append($"\"validationMode\":\"{result.ValidationMode}\",");

    // Parse status
    json.Append($"\"parseSuccess\":{(result.ParseSuccess ? "true" : "false")},");

    if (!result.ParseSuccess)
    {
      json.Append($"\"parseError\":\"{EscapeJson(result.ParseError)}\",");
      json.Append($"\"parseErrorType\":\"{EscapeJson(result.ParseErrorType)}\",");
    }

    if (result.ParseTimeMs.HasValue)
    {
      json.Append($"\"parseTimeMs\":{result.ParseTimeMs.Value},");
    }

    if (result.MemoryUsedBytes.HasValue)
    {
      json.Append($"\"memoryUsedBytes\":{result.MemoryUsedBytes.Value},");
    }

    if (result.ParseSuccess)
    {
      // BBS info
      json.Append($"\"bbsName\":\"{EscapeJson(result.BbsName)}\",");
      json.Append($"\"bbsCity\":\"{EscapeJson(result.BbsCity)}\",");
      json.Append($"\"bbsPhone\":\"{EscapeJson(result.BbsPhone)}\",");
      json.Append($"\"bbsSysop\":\"{EscapeJson(result.BbsSysop)}\",");
      json.Append($"\"packetId\":\"{EscapeJson(result.PacketId)}\",");

      if (result.PacketDate.HasValue)
      {
        json.Append($"\"packetDate\":\"{result.PacketDate.Value:O}\",");
      }

      if (!string.IsNullOrEmpty(result.DoorId))
      {
        json.Append($"\"doorId\":\"{EscapeJson(result.DoorId)}\",");
      }

      // Message stats
      json.Append($"\"messageCount\":{result.MessageCount},");
      json.Append($"\"conferenceCount\":{result.ConferenceCount},");
      json.Append($"\"privateMessageCount\":{result.PrivateMessageCount},");
      json.Append($"\"unreadMessageCount\":{result.UnreadMessageCount},");
      json.Append($"\"readMessageCount\":{result.ReadMessageCount},");

      // Conferences
      json.Append("\"conferences\":[");
      for (int i = 0; i < result.Conferences.Count; i++)
      {
        ConferenceAnalysis conf = result.Conferences[i];
        json.Append("{");
        json.Append($"\"number\":{conf.Number},");
        json.Append($"\"name\":\"{EscapeJson(conf.Name)}\",");
        json.Append($"\"messageCount\":{conf.MessageCount}");
        json.Append("}");
        if (i < result.Conferences.Count - 1)
        {
          json.Append(",");
        }
      }
      json.Append("],");

      // Optional files
      json.Append("\"optionalFiles\":[");
      for (int i = 0; i < result.OptionalFiles.Count; i++)
      {
        json.Append($"\"{EscapeJson(result.OptionalFiles[i])}\"");
        if (i < result.OptionalFiles.Count - 1)
        {
          json.Append(",");
        }
      }
      json.Append("],");

      // Validation
      json.Append($"\"hasValidationErrors\":{(result.HasValidationErrors ? "true" : "false")},");
      json.Append($"\"hasValidationWarnings\":{(result.HasValidationWarnings ? "true" : "false")},");
      json.Append($"\"validationErrorCount\":{result.ValidationErrorCount},");
      json.Append($"\"validationWarningCount\":{result.ValidationWarningCount}");
    }

    // Remove trailing comma if present
    if (json[json.Length - 1] == ',')
    {
      json.Length--;
    }

    json.Append("}");

    return JsonDocument.Parse(json.ToString());
  }

  private string EscapeJson(string? value)
  {
    if (value == null)
    {
      return "";
    }

    return value
      .Replace("\\", "\\\\")
      .Replace("\"", "\\\"")
      .Replace("\n", "\\n")
      .Replace("\r", "\\r")
      .Replace("\t", "\\t");
  }
}

/// <summary>
/// Formats batch analysis results as JSON array.
/// </summary>
internal sealed class JsonBatchOutputFormatter : IOutputFormatter
{
  private readonly JsonOutputFormatter _inner = new JsonOutputFormatter();

  public string Format(AnalysisResult result)
  {
    return _inner.Format(result);
  }

  public string FormatBatch(IReadOnlyList<AnalysisResult> results)
  {
    return _inner.FormatBatch(results);
  }
}