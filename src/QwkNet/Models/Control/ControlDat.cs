using System;
using System.Collections.Generic;

namespace QwkNet.Models.Control;

/// <summary>
/// Represents the CONTROL.DAT file from a QWK packet.
/// </summary>
/// <remarks>
/// <para>
/// CONTROL.DAT is a simple ASCII file containing BBS information, user details,
/// and conference listings. According to the QWK specification by Patrick Y. Lee
/// and Jeffery Foy, it follows a specific line-by-line format.
/// </para>
/// <para>
/// This model preserves all original lines via <see cref="RawLines"/> to maintain
/// byte fidelity and support round-trip operations, even for non-standard or
/// extended door-specific fields.
/// </para>
/// </remarks>
public sealed record ControlDat
{
  /// <summary>
  /// Gets the BBS name.
  /// </summary>
  /// <value>Line 1 of CONTROL.DAT.</value>
  public string BbsName { get; init; }

  /// <summary>
  /// Gets the BBS city and state.
  /// </summary>
  /// <value>Line 2 of CONTROL.DAT.</value>
  public string BbsCity { get; init; }

  /// <summary>
  /// Gets the BBS phone number.
  /// </summary>
  /// <value>Line 3 of CONTROL.DAT.</value>
  public string BbsPhone { get; init; }

  /// <summary>
  /// Gets the BBS sysop name.
  /// </summary>
  /// <value>Line 4 of CONTROL.DAT.</value>
  public string Sysop { get; init; }

  /// <summary>
  /// Gets the mail door registration number.
  /// </summary>
  /// <value>
  /// The registration number portion of line 5, before the comma.
  /// May be "00000" for unregistered doors.
  /// </value>
  public string RegistrationNumber { get; init; }

  /// <summary>
  /// Gets the BBS identifier.
  /// </summary>
  /// <value>
  /// The BBS ID portion of line 5, after the comma. This is a 1-8 character
  /// identifier used for naming REP files and identifying the BBS.
  /// </value>
  public string BbsId { get; init; }

  /// <summary>
  /// Gets the mail packet creation date and time.
  /// </summary>
  /// <value>
  /// Line 6 of CONTROL.DAT, parsed as DateTimeOffset. Returns <see cref="DateTimeOffset.MinValue"/>
  /// if parsing failed in salvage mode.
  /// </value>
  public DateTimeOffset CreatedAt { get; init; }

  /// <summary>
  /// Gets the user name (uppercase).
  /// </summary>
  /// <value>Line 7 of CONTROL.DAT.</value>
  public string UserName { get; init; }

  /// <summary>
  /// Gets the Qmail menu file name, if specified.
  /// </summary>
  /// <value>
  /// Line 8 of CONTROL.DAT. Empty string if not specified or if using a mail
  /// door other than Qmail.
  /// </value>
  public string QmailMenuFile { get; init; }

  /// <summary>
  /// Gets the NetMail conference number.
  /// </summary>
  /// <value>
  /// Line 9 of CONTROL.DAT. Typically 0 if not used, or the conference number
  /// designated for FidoNet NetMail.
  /// </value>
  public ushort NetMailConference { get; init; }

  /// <summary>
  /// Gets the total number of messages in the packet.
  /// </summary>
  /// <value>Line 10 of CONTROL.DAT.</value>
  public int TotalMessages { get; init; }

  /// <summary>
  /// Gets the total number of conferences minus one.
  /// </summary>
  /// <value>
  /// Line 11 of CONTROL.DAT. The actual count of conferences is this value plus one.
  /// </value>
  public int ConferenceCountMinusOne { get; init; }

  /// <summary>
  /// Gets the list of conferences defined in this packet.
  /// </summary>
  /// <value>
  /// Read-only list of conferences extracted from lines 12 onwards. Each conference
  /// occupies two lines: number followed by name.
  /// </value>
  public IReadOnlyList<ConferenceInfo> Conferences { get; init; }

  /// <summary>
  /// Gets the welcome screen file name, if specified.
  /// </summary>
  /// <value>
  /// The filename of the welcome/logon screen, or <see langword="null"/> if not present.
  /// </value>
  public string? WelcomeFile { get; init; }

  /// <summary>
  /// Gets the news file name, if specified.
  /// </summary>
  /// <value>
  /// The filename of the BBS news file, or <see langword="null"/> if not present.
  /// </value>
  public string? NewsFile { get; init; }

  /// <summary>
  /// Gets the goodbye screen file name, if specified.
  /// </summary>
  /// <value>
  /// The filename of the logoff screen, or <see langword="null"/> if not present.
  /// </value>
  public string? GoodbyeFile { get; init; }

  /// <summary>
  /// Gets all raw lines from CONTROL.DAT for round-trip fidelity.
  /// </summary>
  /// <value>
  /// Complete, unmodified list of all lines from CONTROL.DAT. This preserves
  /// door-specific extensions and non-standard fields.
  /// </value>
  public IReadOnlyList<string> RawLines { get; init; }

  /// <summary>
  /// Initialises a new instance of the <see cref="ControlDat"/> record.
  /// </summary>
  /// <param name="bbsName">The BBS name.</param>
  /// <param name="bbsCity">The BBS city and state.</param>
  /// <param name="bbsPhone">The BBS phone number.</param>
  /// <param name="sysop">The sysop name.</param>
  /// <param name="registrationNumber">The mail door registration number.</param>
  /// <param name="bbsId">The BBS identifier.</param>
  /// <param name="createdAt">The packet creation date/time.</param>
  /// <param name="userName">The user name.</param>
  /// <param name="qmailMenuFile">The Qmail menu file name.</param>
  /// <param name="netMailConference">The NetMail conference number.</param>
  /// <param name="totalMessages">The total message count.</param>
  /// <param name="conferenceCountMinusOne">The conference count minus one.</param>
  /// <param name="conferences">The list of conferences.</param>
  /// <param name="welcomeFile">The welcome file name.</param>
  /// <param name="newsFile">The news file name.</param>
  /// <param name="goodbyeFile">The goodbye file name.</param>
  /// <param name="rawLines">All raw lines from CONTROL.DAT.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when any required string parameter is <see langword="null"/>.
  /// </exception>
  public ControlDat(
    string bbsName,
    string bbsCity,
    string bbsPhone,
    string sysop,
    string registrationNumber,
    string bbsId,
    DateTimeOffset createdAt,
    string userName,
    string qmailMenuFile,
    ushort netMailConference,
    int totalMessages,
    int conferenceCountMinusOne,
    IReadOnlyList<ConferenceInfo> conferences,
    string? welcomeFile,
    string? newsFile,
    string? goodbyeFile,
    IReadOnlyList<string> rawLines)
  {
    BbsName = bbsName ?? throw new ArgumentNullException(nameof(bbsName));
    BbsCity = bbsCity ?? throw new ArgumentNullException(nameof(bbsCity));
    BbsPhone = bbsPhone ?? throw new ArgumentNullException(nameof(bbsPhone));
    Sysop = sysop ?? throw new ArgumentNullException(nameof(sysop));
    RegistrationNumber = registrationNumber ?? throw new ArgumentNullException(nameof(registrationNumber));
    BbsId = bbsId ?? throw new ArgumentNullException(nameof(bbsId));
    CreatedAt = createdAt;
    UserName = userName ?? throw new ArgumentNullException(nameof(userName));
    QmailMenuFile = qmailMenuFile ?? throw new ArgumentNullException(nameof(qmailMenuFile));
    NetMailConference = netMailConference;
    TotalMessages = totalMessages;
    ConferenceCountMinusOne = conferenceCountMinusOne;
    Conferences = conferences ?? throw new ArgumentNullException(nameof(conferences));
    WelcomeFile = welcomeFile;
    NewsFile = newsFile;
    GoodbyeFile = goodbyeFile;
    RawLines = rawLines ?? throw new ArgumentNullException(nameof(rawLines));
  }

  /// <summary>
  /// Returns a string representation of this CONTROL.DAT file.
  /// </summary>
  /// <returns>
  /// A string containing the BBS name and ID.
  /// </returns>
  public override string ToString()
  {
    return $"{BbsName} ({BbsId})";
  }
}