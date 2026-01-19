using System;
using System.Collections.Generic;

namespace QwkNet.Validation;

/// <summary>
/// Tracks validation issues encountered during parsing operations.
/// </summary>
/// <remarks>
/// This class provides a thread-safe way to collect validation issues
/// during parsing. It respects the configured <see cref="Mode"/> to
/// determine whether to throw exceptions or accumulate warnings.
/// </remarks>
public sealed class ValidationContext
{
  private readonly List<ValidationIssue> _issues;
  private readonly object _lock = new object();

  /// <summary>
  /// Gets the validation mode for this context.
  /// </summary>
  public ValidationMode Mode { get; }

  /// <summary>
  /// Gets the read-only list of all validation issues collected.
  /// </summary>
  public IReadOnlyList<ValidationIssue> Issues
  {
    get
    {
      lock (_lock)
      {
        return _issues.ToArray();
      }
    }
  }

  /// <summary>
  /// Gets a value indicating whether any errors have been recorded.
  /// </summary>
  public bool HasErrors
  {
    get
    {
      lock (_lock)
      {
        foreach (ValidationIssue issue in _issues)
        {
          if (issue.Severity == ValidationSeverity.Error)
          {
            return true;
          }
        }
        return false;
      }
    }
  }

  /// <summary>
  /// Gets a value indicating whether any warnings have been recorded.
  /// </summary>
  public bool HasWarnings
  {
    get
    {
      lock (_lock)
      {
        foreach (ValidationIssue issue in _issues)
        {
          if (issue.Severity == ValidationSeverity.Warning)
          {
            return true;
          }
        }
        return false;
      }
    }
  }

  /// <summary>
  /// Initialises a new instance of the <see cref="ValidationContext"/> class.
  /// </summary>
  /// <param name="mode">The validation mode.</param>
  public ValidationContext(ValidationMode mode)
  {
    Mode = mode;
    _issues = new List<ValidationIssue>();
  }

  /// <summary>
  /// Adds an informational message to the validation context.
  /// </summary>
  /// <param name="message">The message text.</param>
  /// <param name="location">Optional location context.</param>
  public void AddInfo(string message, string? location = null)
  {
    ValidationIssue issue = new ValidationIssue(ValidationSeverity.Info, message, location);
    lock (_lock)
    {
      _issues.Add(issue);
    }
  }

  /// <summary>
  /// Adds a warning to the validation context.
  /// </summary>
  /// <param name="message">The message text.</param>
  /// <param name="location">Optional location context.</param>
  public void AddWarning(string message, string? location = null)
  {
    ValidationIssue issue = new ValidationIssue(ValidationSeverity.Warning, message, location);
    lock (_lock)
    {
      _issues.Add(issue);
    }
  }

  /// <summary>
  /// Adds an error to the validation context and throws an exception if in strict mode.
  /// </summary>
  /// <param name="message">The message text.</param>
  /// <param name="location">Optional location context.</param>
  /// <exception cref="QwkFormatException">
  /// Thrown when <see cref="Mode"/> is <see cref="ValidationMode.Strict"/>.
  /// </exception>
  public void AddError(string message, string? location = null)
  {
    ValidationIssue issue = new ValidationIssue(ValidationSeverity.Error, message, location);
    lock (_lock)
    {
      _issues.Add(issue);
    }

    if (Mode == ValidationMode.Strict)
    {
      throw new QwkFormatException(message, location);
    }
  }

  /// <summary>
  /// Clears all recorded validation issues.
  /// </summary>
  public void Clear()
  {
    lock (_lock)
    {
      _issues.Clear();
    }
  }
}