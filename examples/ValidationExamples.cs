using System;
using System.IO;
using QwkNet;
using QwkNet.Validation;

namespace QwkNet.Examples;

/// <summary>
/// Demonstrates the comprehensive validation capabilities added in Milestone 8.
/// </summary>
public static class ValidationExamples
{
  /// <summary>
  /// Example 1: Basic packet validation with human-readable output.
  /// </summary>
  public static void BasicValidation()
  {
    Console.WriteLine("=== Example 1: Basic Validation ===\n");

    // Open packet with default lenient validation
    QwkPacket packet = QwkPacket.Open("mypacket.qwk");
    ValidationReport report = packet.ValidationReport;

    // Check if packet is valid
    if (report.IsValid)
    {
      Console.WriteLine("[VALID] Packet is valid!");
    }
    else
    {
      Console.WriteLine("[INVALID] Packet has issues:\n");
      Console.WriteLine(report.ToHumanReadableString());
    }

    packet.Dispose();
  }

  /// <summary>
  /// Example 2: Strict validation with exception handling.
  /// </summary>
  public static void StrictValidation()
  {
    Console.WriteLine("\n=== Example 2: Strict Validation ===\n");

    try
    {
      // Strict mode throws on first error
      QwkPacket packet = QwkPacket.Open("mypacket.qwk", ValidationMode.Strict);
      Console.WriteLine("[VALID] Packet passed strict validation");
      packet.Dispose();
    }
    catch (QwkFormatException ex)
    {
      Console.WriteLine($"[INVALID] Strict validation failed: {ex.Message}");
    }
  }

  /// <summary>
  /// Example 3: Salvage mode for recovering damaged packets.
  /// </summary>
  public static void SalvageMode()
  {
    Console.WriteLine("\n=== Example 3: Salvage Mode ===\n");

    // Salvage mode attempts to recover as much data as possible
    QwkPacket packet = QwkPacket.Open("damaged-packet.qwk", ValidationMode.Salvage);
    ValidationReport report = packet.ValidationReport;

    Console.WriteLine($"Recovered {packet.Messages.Count} message(s)");
    Console.WriteLine($"Errors: {report.Errors.Count}");
    Console.WriteLine($"Warnings: {report.Warnings.Count}");

    if (report.HasErrors || report.HasWarnings)
    {
      Console.WriteLine("\nIssues encountered during salvage:");
      foreach (ValidationIssue error in report.Errors)
      {
        Console.WriteLine($"  [ERROR] {error.Message}");
      }
      foreach (ValidationIssue warning in report.Warnings)
      {
        Console.WriteLine($"  [WARN]  {warning.Message}");
      }
    }

    packet.Dispose();
  }

  /// <summary>
  /// Example 4: JSON export for automated processing.
  /// </summary>
  public static void JsonExport()
  {
    Console.WriteLine("\n=== Example 4: JSON Export ===\n");

    QwkPacket packet = QwkPacket.Open("mypacket.qwk", ValidationMode.Lenient);
    ValidationReport report = packet.ValidationReport;

    // Export to JSON for automated tools
    string json = report.ToJson(indented: true);
    File.WriteAllText("validation-report.json", json);

    Console.WriteLine("Validation report exported to validation-report.json");
    Console.WriteLine("\nJSON Preview:");
    Console.WriteLine(json.Substring(0, Math.Min(200, json.Length)) + "...");

    packet.Dispose();
  }

  /// <summary>
  /// Example 5: Custom validation workflow with specific checks.
  /// </summary>
  public static void CustomValidation()
  {
    Console.WriteLine("\n=== Example 5: Custom Validation Workflow ===\n");

    QwkPacket packet = QwkPacket.Open("mypacket.qwk", ValidationMode.Lenient);

    // Create a custom validation context for additional checks
    ValidationContext customContext = new ValidationContext(ValidationMode.Lenient);

    // Perform specific validation checks
    Console.WriteLine("Running custom validation checks...");

    // Validate message headers
    foreach (Message message in packet.Messages)
    {
      PacketValidator.ValidateMessageHeader(message, customContext);
    }

    // Validate conference numbers
    PacketValidator.ValidateConferenceNumbers(
      packet.Messages,
      packet.Control.Conferences,
      customContext);

    // Generate custom report
    ValidationReport customReport = ValidationReport.FromContext(customContext);

    Console.WriteLine($"\nCustom validation results:");
    Console.WriteLine($"  Total issues: {customReport.AllIssues.Count}");
    Console.WriteLine($"  Errors: {customReport.Errors.Count}");
    Console.WriteLine($"  Warnings: {customReport.Warnings.Count}");
    Console.WriteLine($"  Informational: {customReport.Infos.Count}");

    if (!customReport.IsValid)
    {
      Console.WriteLine("\nDetailed issues:");
      Console.WriteLine(customReport.ToHumanReadableString());
    }

    packet.Dispose();
  }

  /// <summary>
  /// Example 6: Validation report filtering and analysis.
  /// </summary>
  public static void ValidationAnalysis()
  {
    Console.WriteLine("\n=== Example 6: Validation Analysis ===\n");

    QwkPacket packet = QwkPacket.Open("mypacket.qwk", ValidationMode.Lenient);
    ValidationReport report = packet.ValidationReport;

    // Analyse errors by location
    Console.WriteLine("Errors by location:");
    foreach (ValidationIssue error in report.Errors)
    {
      string location = error.Location ?? "Unknown";
      Console.WriteLine($"  {location}: {error.Message}");
    }

    // Analyse warnings by location
    if (report.Warnings.Count > 0)
    {
      Console.WriteLine("\nWarnings by location:");
      foreach (ValidationIssue warning in report.Warnings)
      {
        string location = warning.Location ?? "Unknown";
        Console.WriteLine($"  {location}: {warning.Message}");
      }
    }

    // Count issues by severity
    Console.WriteLine("\nIssues by severity:");
    Console.WriteLine($"  Critical (Errors):    {report.Errors.Count}");
    Console.WriteLine($"  Important (Warnings): {report.Warnings.Count}");
    Console.WriteLine($"  Optional (Info):      {report.Infos.Count}");

    packet.Dispose();
  }

  /// <summary>
  /// Example 7: Batch validation of multiple packets.
  /// </summary>
  public static void BatchValidation()
  {
    Console.WriteLine("\n=== Example 7: Batch Validation ===\n");

    string[] packetFiles = Directory.GetFiles("packets", "*.qwk");
    int validCount = 0;
    int invalidCount = 0;

    foreach (string packetFile in packetFiles)
    {
      try
      {
        QwkPacket packet = QwkPacket.Open(packetFile, ValidationMode.Lenient);
        ValidationReport report = packet.ValidationReport;

        if (report.IsValid)
        {
          validCount++;
          Console.WriteLine($"[VALID] {Path.GetFileName(packetFile)}");
        }
        else
        {
          invalidCount++;
          Console.WriteLine($"[INVALID] {Path.GetFileName(packetFile)} ({report.Errors.Count} errors, {report.Warnings.Count} warnings)");

          // Export detailed report for invalid packets
          string reportFile = Path.ChangeExtension(packetFile, ".json");
          File.WriteAllText(reportFile, report.ToJson(indented: true));
        }

        packet.Dispose();
      }
      catch (Exception ex)
      {
        invalidCount++;
        Console.WriteLine($"[ERROR] {Path.GetFileName(packetFile)} (exception: {ex.Message})");
      }
    }

    Console.WriteLine($"\nSummary:");
    Console.WriteLine($"  Valid packets:   {validCount}");
    Console.WriteLine($"  Invalid packets: {invalidCount}");
    Console.WriteLine($"  Total processed: {packetFiles.Length}");
  }

  /// <summary>
  /// Runs all validation examples.
  /// </summary>
  public static void RunAll()
  {
    try
    {
      BasicValidation();
      StrictValidation();
      SalvageMode();
      JsonExport();
      CustomValidation();
      ValidationAnalysis();
      BatchValidation();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nExample execution failed: {ex.Message}");
    }
  }
}