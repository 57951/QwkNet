using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using QwkNet.Validation;

namespace QwkNet.Tests.Validation;

public sealed class ValidationReportTests
{
  [Fact]
  public void Constructor_NullIssues_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new ValidationReport(null!));
  }

  [Fact]
  public void Constructor_EmptyIssues_CreatesValidReport()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>();

    // Act
    ValidationReport report = new ValidationReport(issues);

    // Assert
    Assert.Empty(report.AllIssues);
    Assert.Empty(report.Errors);
    Assert.Empty(report.Warnings);
    Assert.Empty(report.Infos);
    Assert.True(report.IsValid);
    Assert.False(report.HasErrors);
    Assert.False(report.HasWarnings);
    Assert.False(report.HasInfos);
  }

  [Fact]
  public void Constructor_WithErrors_SetsProperties()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Error 1", "Location 1"),
      new ValidationIssue(ValidationSeverity.Warning, "Warning 1", "Location 2"),
      new ValidationIssue(ValidationSeverity.Info, "Info 1", "Location 3")
    };

    // Act
    ValidationReport report = new ValidationReport(issues);

    // Assert
    Assert.Equal(3, report.AllIssues.Count);
    Assert.Single(report.Errors);
    Assert.Single(report.Warnings);
    Assert.Single(report.Infos);
    Assert.False(report.IsValid);
    Assert.True(report.HasErrors);
    Assert.True(report.HasWarnings);
    Assert.True(report.HasInfos);
  }

  [Fact]
  public void IsValid_WithWarningsOnly_ReturnsFalse()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Warning, "Warning 1", "Location 1")
    };

    // Act
    ValidationReport report = new ValidationReport(issues);

    // Assert
    Assert.False(report.IsValid);
    Assert.False(report.HasErrors);
    Assert.True(report.HasWarnings);
  }

  [Fact]
  public void IsValid_WithInfoOnly_ReturnsTrue()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Info, "Info 1", "Location 1")
    };

    // Act
    ValidationReport report = new ValidationReport(issues);

    // Assert
    Assert.True(report.IsValid);
    Assert.False(report.HasErrors);
    Assert.False(report.HasWarnings);
    Assert.True(report.HasInfos);
  }

  [Fact]
  public void FromContext_NullContext_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => ValidationReport.FromContext(null!));
  }

  [Fact]
  public void FromContext_ValidContext_CreatesReport()
  {
    // Arrange
    ValidationContext context = new ValidationContext(ValidationMode.Lenient);
    context.AddError("Error 1");
    context.AddWarning("Warning 1");
    context.AddInfo("Info 1");

    // Act
    ValidationReport report = ValidationReport.FromContext(context);

    // Assert
    Assert.Equal(3, report.AllIssues.Count);
    Assert.Single(report.Errors);
    Assert.Single(report.Warnings);
    Assert.Single(report.Infos);
  }

  [Fact]
  public void ToHumanReadableString_EmptyReport_ReturnsFormattedString()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>();
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToHumanReadableString();

    // Assert
    Assert.Contains("Validation Report", output);
    Assert.Contains("Total Issues: 0", output);
    Assert.Contains("Valid:        Yes", output);
  }

  [Fact]
  public void ToHumanReadableString_WithErrors_IncludesErrors()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Test error", "Location 1")
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToHumanReadableString();

    // Assert
    Assert.Contains("Errors:", output);
    Assert.Contains("Test error", output);
    Assert.Contains("Location 1", output);
    Assert.Contains("Valid:        No", output);
  }

  [Fact]
  public void ToHumanReadableString_WithWarnings_IncludesWarnings()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Warning, "Test warning", "Location 2")
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToHumanReadableString();

    // Assert
    Assert.Contains("Warnings:", output);
    Assert.Contains("Test warning", output);
    Assert.Contains("Location 2", output);
  }

  [Fact]
  public void ToHumanReadableString_WithInfos_IncludesInfos()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Info, "Test info", "Location 3")
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToHumanReadableString();

    // Assert
    Assert.Contains("Informational:", output);
    Assert.Contains("Test info", output);
  }

  [Fact]
  public void ToHumanReadableString_ManyInfos_LimitsOutput()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>();
    for (int i = 0; i < 25; i++)
    {
      issues.Add(new ValidationIssue(ValidationSeverity.Info, $"Info {i}", null));
    }
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToHumanReadableString();

    // Assert
    Assert.Contains("25 messages (not shown)", output);
  }

  [Fact]
  public void ToJson_EmptyReport_ReturnsValidJson()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>();
    ValidationReport report = new ValidationReport(issues);

    // Act
    string json = report.ToJson();

    // Assert
    Assert.NotEmpty(json);
    JsonDocument doc = JsonDocument.Parse(json);
    JsonElement root = doc.RootElement;
    Assert.True(root.GetProperty("isValid").GetBoolean());
    Assert.Equal(0, root.GetProperty("totalIssues").GetInt32());
  }

  [Fact]
  public void ToJson_WithIssues_ReturnsValidJson()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Test error", "Location 1"),
      new ValidationIssue(ValidationSeverity.Warning, "Test warning", "Location 2"),
      new ValidationIssue(ValidationSeverity.Info, "Test info", null)
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string json = report.ToJson();

    // Assert
    JsonDocument doc = JsonDocument.Parse(json);
    JsonElement root = doc.RootElement;
    Assert.False(root.GetProperty("isValid").GetBoolean());
    Assert.Equal(3, root.GetProperty("totalIssues").GetInt32());
    Assert.Equal(1, root.GetProperty("errorCount").GetInt32());
    Assert.Equal(1, root.GetProperty("warningCount").GetInt32());
    Assert.Equal(1, root.GetProperty("infoCount").GetInt32());

    // Check errors array
    JsonElement errors = root.GetProperty("errors");
    Assert.Equal(1, errors.GetArrayLength());
    JsonElement firstError = errors[0];
    Assert.Equal("Error", firstError.GetProperty("severity").GetString());
    Assert.Equal("Test error", firstError.GetProperty("message").GetString());
    Assert.Equal("Location 1", firstError.GetProperty("location").GetString());
  }

  [Fact]
  public void ToJson_IndentedFalse_ReturnsCompactJson()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Test", "Location")
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string json = report.ToJson(indented: false);

    // Assert
    Assert.DoesNotContain("\n", json);
    Assert.DoesNotContain("  ", json);
  }

  [Fact]
  public void ToJson_IndentedTrue_ReturnsFormattedJson()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Test", "Location")
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string json = report.ToJson(indented: true);

    // Assert
    Assert.Contains("\n", json);
    Assert.Contains("  ", json);
  }

  [Fact]
  public void ToString_EmptyReport_ReturnsSummary()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>();
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToString();

    // Assert
    Assert.Contains("ValidationReport", output);
    Assert.Contains("0 issue(s)", output);
    Assert.Contains("0 error(s)", output);
    Assert.Contains("0 warning(s)", output);
  }

  [Fact]
  public void ToString_WithIssues_ReturnsSummary()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Error 1", null),
      new ValidationIssue(ValidationSeverity.Warning, "Warning 1", null),
      new ValidationIssue(ValidationSeverity.Warning, "Warning 2", null),
      new ValidationIssue(ValidationSeverity.Info, "Info 1", null)
    };
    ValidationReport report = new ValidationReport(issues);

    // Act
    string output = report.ToString();

    // Assert
    Assert.Contains("4 issue(s)", output);
    Assert.Contains("1 error(s)", output);
    Assert.Contains("2 warning(s)", output);
  }

  [Fact]
  public void HasInfos_WithInfos_ReturnsTrue()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Info, "Info 1", null)
    };
    ValidationReport report = new ValidationReport(issues);

    // Act & Assert
    Assert.True(report.HasInfos);
  }

  [Fact]
  public void HasInfos_WithoutInfos_ReturnsFalse()
  {
    // Arrange
    List<ValidationIssue> issues = new List<ValidationIssue>
    {
      new ValidationIssue(ValidationSeverity.Error, "Error 1", null)
    };
    ValidationReport report = new ValidationReport(issues);

    // Act & Assert
    Assert.False(report.HasInfos);
  }
}