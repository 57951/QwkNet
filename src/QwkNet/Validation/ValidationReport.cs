using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace QwkNet.Validation;

/// <summary>
/// Represents the validation report for a QWK packet.
/// </summary>
/// <remarks>
/// This report contains all validation issues encountered during packet parsing,
/// categorised by severity level.
/// </remarks>
public sealed class ValidationReport
{
  /// <summary>
  /// Gets all validation issues.
  /// </summary>
  public IReadOnlyList<ValidationIssue> AllIssues { get; }

  /// <summary>
  /// Gets all error-level issues.
  /// </summary>
  public IReadOnlyList<ValidationIssue> Errors { get; }

  /// <summary>
  /// Gets all warning-level issues.
  /// </summary>
  public IReadOnlyList<ValidationIssue> Warnings { get; }

  /// <summary>
  /// Gets all informational issues.
  /// </summary>
  public IReadOnlyList<ValidationIssue> Infos { get; }

  /// <summary>
  /// Gets whether the packet passed validation (no errors or warnings).
  /// </summary>
  public bool IsValid => Errors.Count == 0 && Warnings.Count == 0;

  /// <summary>
  /// Gets whether the packet has any errors.
  /// </summary>
  public bool HasErrors => Errors.Count > 0;

  /// <summary>
  /// Gets whether the packet has any warnings.
  /// </summary>
  public bool HasWarnings => Warnings.Count > 0;

  /// <summary>
  /// Gets whether the packet has any informational messages.
  /// </summary>
  public bool HasInfos => Infos.Count > 0;

  /// <summary>
  /// Initialises a new instance of the <see cref="ValidationReport"/> class.
  /// </summary>
  /// <param name="issues">The list of validation issues.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="issues"/> is null.</exception>
  public ValidationReport(IReadOnlyList<ValidationIssue> issues)
  {
    if (issues == null)
    {
      throw new ArgumentNullException(nameof(issues));
    }

    AllIssues = issues;
    Errors = issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
    Warnings = issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
    Infos = issues.Where(i => i.Severity == ValidationSeverity.Info).ToList();
  }

  /// <summary>
  /// Creates a validation report from a validation context.
  /// </summary>
  /// <param name="context">The validation context.</param>
  /// <returns>A new validation report.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
  public static ValidationReport FromContext(ValidationContext context)
  {
    if (context == null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    return new ValidationReport(context.Issues);
  }

  /// <summary>
  /// Formats the validation report as human-readable text.
  /// </summary>
  /// <returns>A formatted string containing all validation issues.</returns>
  public string ToHumanReadableString()
  {
    StringBuilder sb = new StringBuilder();

    sb.AppendLine("=== Validation Report ===");
    sb.AppendLine();

    // Summary
    sb.AppendLine("Summary:");
    sb.AppendLine($"  Total Issues: {AllIssues.Count}");
    sb.AppendLine($"  Errors:       {Errors.Count}");
    sb.AppendLine($"  Warnings:     {Warnings.Count}");
    sb.AppendLine($"  Informational: {Infos.Count}");
    sb.AppendLine($"  Valid:        {(IsValid ? "Yes" : "No")}");
    sb.AppendLine();

    // Errors
    if (Errors.Count > 0)
    {
      sb.AppendLine("Errors:");
      foreach (ValidationIssue error in Errors)
      {
        sb.AppendLine($"  • {error}");
      }
      sb.AppendLine();
    }

    // Warnings
    if (Warnings.Count > 0)
    {
      sb.AppendLine("Warnings:");
      foreach (ValidationIssue warning in Warnings)
      {
        sb.AppendLine($"  • {warning}");
      }
      sb.AppendLine();
    }

    // Informational (only if present and not too many)
    if (Infos.Count > 0 && Infos.Count <= 20)
    {
      sb.AppendLine("Informational:");
      foreach (ValidationIssue info in Infos)
      {
        sb.AppendLine($"  • {info}");
      }
      sb.AppendLine();
    }
    else if (Infos.Count > 20)
    {
      sb.AppendLine($"Informational: {Infos.Count} messages (not shown)");
      sb.AppendLine();
    }

    return sb.ToString().TrimEnd();
  }

  /// <summary>
  /// Serialises the validation report to JSON format.
  /// </summary>
  /// <param name="indented">Whether to format the JSON with indentation.</param>
  /// <returns>A JSON string representing this validation report.</returns>
  public string ToJson(bool indented = false)
  {
    JsonSerializerOptions options = new JsonSerializerOptions
    {
      WriteIndented = indented,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    ValidationReportDto dto = new ValidationReportDto
    {
      IsValid = IsValid,
      TotalIssues = AllIssues.Count,
      ErrorCount = Errors.Count,
      WarningCount = Warnings.Count,
      InfoCount = Infos.Count,
      Errors = Errors.Select(e => new ValidationIssueDto
      {
        Severity = e.Severity.ToString(),
        Message = e.Message,
        Location = e.Location
      }).ToList(),
      Warnings = Warnings.Select(w => new ValidationIssueDto
      {
        Severity = w.Severity.ToString(),
        Message = w.Message,
        Location = w.Location
      }).ToList(),
      Infos = Infos.Select(i => new ValidationIssueDto
      {
        Severity = i.Severity.ToString(),
        Message = i.Message,
        Location = i.Location
      }).ToList()
    };

    return JsonSerializer.Serialize(dto, options);
  }

  /// <summary>
  /// Returns a string representation of this validation report.
  /// </summary>
  /// <returns>A summary string showing issue counts.</returns>
  public override string ToString()
  {
    return $"ValidationReport: {AllIssues.Count} issue(s) ({Errors.Count} error(s), {Warnings.Count} warning(s), {Infos.Count} info)";
  }

  // Internal DTO classes for JSON serialisation
  private sealed class ValidationReportDto
  {
    public bool IsValid { get; set; }
    public int TotalIssues { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public List<ValidationIssueDto> Errors { get; set; } = new List<ValidationIssueDto>();
    public List<ValidationIssueDto> Warnings { get; set; } = new List<ValidationIssueDto>();
    public List<ValidationIssueDto> Infos { get; set; } = new List<ValidationIssueDto>();
  }

  private sealed class ValidationIssueDto
  {
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Location { get; set; }
  }
}