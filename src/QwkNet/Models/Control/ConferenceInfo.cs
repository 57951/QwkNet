using System;

namespace QwkNet.Models.Control;

/// <summary>
/// Represents a conference (message area) definition from CONTROL.DAT.
/// </summary>
/// <remarks>
/// <para>
/// According to the QWK specification, conference numbers and names are listed
/// sequentially in CONTROL.DAT after line 11. Each conference occupies two lines:
/// the conference number followed by the conference name (13 characters or less
/// in classic QWK, extended to 255 characters in QWKE).
/// </para>
/// <para>
/// Conference 0 is explicitly permitted by the specification and may be used
/// for NetMail or as the main board area.
/// </para>
/// </remarks>
public sealed record ConferenceInfo
{
  /// <summary>
  /// Gets the conference number.
  /// </summary>
  /// <value>
  /// The conference number as specified in CONTROL.DAT. Conference 0 is valid
  /// per the QWK specification.
  /// </value>
  public ushort Number { get; init; }

  /// <summary>
  /// Gets the conference name.
  /// </summary>
  /// <value>
  /// The conference name, typically 13 characters or less in classic QWK format,
  /// though QWKE permits up to 255 characters.
  /// </value>
  public string Name { get; init; }

  /// <summary>
  /// Initialises a new instance of the <see cref="ConferenceInfo"/> record.
  /// </summary>
  /// <param name="number">The conference number (0-65535).</param>
  /// <param name="name">The conference name.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="name"/> is <see langword="null"/>.
  /// </exception>
  public ConferenceInfo(ushort number, string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    Number = number;
    Name = name;
  }

  /// <summary>
  /// Returns a string representation of this conference.
  /// </summary>
  /// <returns>
  /// A string in the format "Conference {Number}: {Name}".
  /// </returns>
  public override string ToString()
  {
    return $"Conference {Number}: {Name}";
  }
}