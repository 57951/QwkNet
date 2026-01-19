using System;

namespace QwkNet.Archive;

/// <summary>
/// Represents an archive format identifier using a string-based discriminator.
/// </summary>
/// <remarks>
/// <para>
/// This type replaces enumeration-based format identification to support third-party
/// archive extensions without requiring modifications to the core library.
/// </para>
/// <para>
/// Format identifiers are case-insensitive and normalised to lowercase invariant
/// culture for consistent comparison. This ensures that "ZIP", "Zip", and "zip"
/// are all treated as equivalent identifiers.
/// </para>
/// <para>
/// Extension developers should use short, descriptive identifiers (e.g., "tar",
/// "rar", "7z") that clearly indicate the archive format they represent.
/// </para>
/// <para>
/// <b>Why not enums?</b> Enumerations are closed types that cannot be extended
/// by third-party libraries without modifying the core assembly. String-backed
/// identifiers preserve core library isolation whilst allowing unlimited format
/// extensions.
/// </para>
/// </remarks>
public readonly struct ArchiveFormatId : IEquatable<ArchiveFormatId>
{
  private readonly string _value;

  /// <summary>
  /// Gets the ZIP archive format identifier.
  /// </summary>
  /// <value>
  /// The built-in identifier for PKZIP-compatible archives.
  /// </value>
  public static ArchiveFormatId Zip => new ArchiveFormatId("zip");

  /// <summary>
  /// Initialises a new instance of the <see cref="ArchiveFormatId"/> struct.
  /// </summary>
  /// <param name="value">
  /// The format identifier string. Must not be <see langword="null"/>, empty, or whitespace.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="value"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="value"/> is empty or contains only whitespace.
  /// </exception>
  private ArchiveFormatId(string value)
  {
    if (value == null)
    {
      throw new ArgumentNullException(nameof(value));
    }

    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException(
        "Format identifier must not be empty or whitespace.",
        nameof(value));
    }

    _value = value.ToLowerInvariant();
  }

  /// <summary>
  /// Creates an archive format identifier from a string value.
  /// </summary>
  /// <param name="value">
  /// The format identifier string. Must not be <see langword="null"/>, empty, or whitespace.
  /// </param>
  /// <returns>
  /// An <see cref="ArchiveFormatId"/> representing the specified format.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="value"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="value"/> is empty or contains only whitespace.
  /// </exception>
  /// <remarks>
  /// The identifier is normalised to lowercase invariant culture for consistent
  /// comparison. Extension developers should use short, descriptive identifiers
  /// that clearly indicate their archive format (e.g., "tar", "rar", "7z").
  /// </remarks>
  public static ArchiveFormatId From(string value)
  {
    return new ArchiveFormatId(value);
  }

  /// <summary>
  /// Returns the string representation of this format identifier.
  /// </summary>
  /// <returns>
  /// The normalised lowercase format identifier string.
  /// </returns>
  public override string ToString()
  {
    return _value ?? string.Empty;
  }

  /// <summary>
  /// Determines whether this instance is equal to another <see cref="ArchiveFormatId"/>.
  /// </summary>
  /// <param name="other">The other format identifier to compare.</param>
  /// <returns>
  /// <see langword="true"/> if the identifiers are equal; otherwise, <see langword="false"/>.
  /// </returns>
  public bool Equals(ArchiveFormatId other)
  {
    return string.Equals(_value, other._value, StringComparison.Ordinal);
  }

  /// <summary>
  /// Determines whether this instance is equal to another object.
  /// </summary>
  /// <param name="obj">The object to compare.</param>
  /// <returns>
  /// <see langword="true"/> if <paramref name="obj"/> is an <see cref="ArchiveFormatId"/>
  /// with the same value; otherwise, <see langword="false"/>.
  /// </returns>
  public override bool Equals(object? obj)
  {
    return obj is ArchiveFormatId other && Equals(other);
  }

  /// <summary>
  /// Returns the hash code for this instance.
  /// </summary>
  /// <returns>
  /// A 32-bit signed integer hash code.
  /// </returns>
  public override int GetHashCode()
  {
    return _value?.GetHashCode() ?? 0;
  }

  /// <summary>
  /// Determines whether two format identifiers are equal.
  /// </summary>
  /// <param name="left">The first format identifier.</param>
  /// <param name="right">The second format identifier.</param>
  /// <returns>
  /// <see langword="true"/> if the identifiers are equal; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator ==(ArchiveFormatId left, ArchiveFormatId right)
  {
    return left.Equals(right);
  }

  /// <summary>
  /// Determines whether two format identifiers are not equal.
  /// </summary>
  /// <param name="left">The first format identifier.</param>
  /// <param name="right">The second format identifier.</param>
  /// <returns>
  /// <see langword="true"/> if the identifiers are not equal; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator !=(ArchiveFormatId left, ArchiveFormatId right)
  {
    return !left.Equals(right);
  }
}