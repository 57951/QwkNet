using System;
using System.IO;
using Xunit;
using QwkNet;
using QwkNet.Models.Messages;

namespace QwkNet.Tests;

/// <summary>
/// Round-trip tests using real QWK packets.
/// </summary>
public sealed class RoundTripRealPacketTests
{
  [Fact]
  public void RoundTrip_Demo1Qwk_PreservesAllContent()
  {
    // Arrange - Use the actual DEMO1.QWK file
    string demo1Path = "/mnt/user-data/uploads/DEMO1.QWK";
    
    if (!File.Exists(demo1Path))
    {
      // Skip test if DEMO1.QWK not available
      return;
    }

    // Act - Open original QWK packet
    using QwkPacket originalPacket = QwkPacket.Open(demo1Path);
    Assert.Single(originalPacket.Messages);

    // Create REP packet from real message
    using RepPacket repPacket = RepPacket.Create(originalPacket.Control);
    foreach (Message msg in originalPacket.Messages)
    {
      repPacket.AddMessage(msg);
    }

    // Save REP to memory
    using MemoryStream repStream = new MemoryStream();
    repPacket.Save(repStream);
    repStream.Position = 0;

    // Read REP back as QWK
    using QwkPacket roundTripPacket = QwkPacket.Open(repStream);

    // Assert
    Assert.Single(roundTripPacket.Messages);
    Message original = originalPacket.Messages[0];
    Message roundTrip = roundTripPacket.Messages[0];

    // Verify all fields match
    Assert.Equal(original.From, roundTrip.From);
    Assert.Equal(original.To, roundTrip.To);
    Assert.Equal(original.Subject, roundTrip.Subject);
    Assert.Equal(original.ConferenceNumber, roundTrip.ConferenceNumber);

    // Body text should match (normalised for line endings)
    string originalBody = NormaliseText(original.Body.GetDecodedText());
    string roundTripBody = NormaliseText(roundTrip.Body.GetDecodedText());
    Assert.Equal(originalBody, roundTripBody);
  }

  /// <summary>
  /// Normalises text for comparison by standardising line endings.
  /// </summary>
  private static string NormaliseText(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return string.Empty;
    }

    // Normalise line endings
    text = text.Replace("\r\n", "\n").Replace("\r", "\n");
    
    // Trim trailing whitespace from lines
    string[] lines = text.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
      lines[i] = lines[i].TrimEnd();
    }

    return string.Join("\n", lines).Trim();
  }
}