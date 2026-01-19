using System;

namespace QwkNet;

/// <summary>
/// The exception that is thrown when a QWK packet violates format specifications.
/// </summary>
/// <remarks>
/// This exception is thrown primarily in strict validation mode when the parser
/// encounters data that cannot be reconciled with the QWK specification.
/// </remarks>
public sealed class QwkFormatException : Exception
{
  /// <summary>
  /// Gets the location context where the error occurred, if available.
  /// </summary>
  /// <value>
  /// A string describing the location (e.g., "CONTROL.DAT line 6", "Message header at offset 0x1A0"),
  /// or <see langword="null"/> if no location information is available.
  /// </value>
  public string? Location { get; }

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkFormatException"/> class.
  /// </summary>
  public QwkFormatException()
    : base("QWK format error")
  {
  }

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkFormatException"/> class with a specified error message.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  public QwkFormatException(string message)
    : base(message)
  {
  }

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkFormatException"/> class with a specified error message
  /// and location context.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="location">The location context where the error occurred.</param>
  public QwkFormatException(string message, string? location)
    : base(location != null ? $"{message} (at {location})" : message)
  {
    Location = location;
  }

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkFormatException"/> class with a specified error message
  /// and a reference to the inner exception that is the cause of this exception.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">The exception that is the cause of the current exception.</param>
  public QwkFormatException(string message, Exception innerException)
    : base(message, innerException)
  {
  }

  /// <summary>
  /// Initialises a new instance of the <see cref="QwkFormatException"/> class with a specified error message,
  /// location context, and a reference to the inner exception.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="location">The location context where the error occurred.</param>
  /// <param name="innerException">The exception that is the cause of the current exception.</param>
  public QwkFormatException(string message, string? location, Exception innerException)
    : base(location != null ? $"{message} (at {location})" : message, innerException)
  {
    Location = location;
  }
}