using System;
using System.Collections.Generic;
using System.IO;
using QwkNet.Archive;

namespace QwkNet;

/// <summary>
/// Represents a collection of optional files in a QWK packet.
/// </summary>
/// <remarks>
/// Optional files (WELCOME, NEWS, GOODBYE) are lazy-loaded on first access for memory efficiency.
/// </remarks>
public sealed class OptionalFileCollection
{
  private readonly IArchiveReader _archive;
  private readonly Dictionary<string, string?> _cache = new Dictionary<string, string?>();
  private readonly object _lock = new object();

  /// <summary>
  /// Initialises a new instance of the <see cref="OptionalFileCollection"/> class.
  /// </summary>
  /// <param name="archive">The archive reader.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="archive"/> is null.</exception>
  public OptionalFileCollection(IArchiveReader archive)
  {
    _archive = archive ?? throw new ArgumentNullException(nameof(archive));
  }

  /// <summary>
  /// Gets the WELCOME file text, or null if not present.
  /// </summary>
  /// <returns>The WELCOME file contents.</returns>
  public string? GetWelcomeText()
  {
    return GetFileText("WELCOME");
  }

  /// <summary>
  /// Gets the NEWS file text, or null if not present.
  /// </summary>
  /// <returns>The NEWS file contents.</returns>
  public string? GetNewsText()
  {
    return GetFileText("NEWS");
  }

  /// <summary>
  /// Gets the GOODBYE file text, or null if not present.
  /// </summary>
  /// <returns>The GOODBYE file contents.</returns>
  public string? GetGoodbyeText()
  {
    return GetFileText("GOODBYE");
  }

  /// <summary>
  /// Checks whether a file exists in the archive.
  /// </summary>
  /// <param name="name">The file name (case-insensitive).</param>
  /// <returns>True if the file exists; otherwise, false.</returns>
  public bool HasFile(string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    return _archive.FileExists(name);
  }

  /// <summary>
  /// Opens a raw stream for a file in the archive.
  /// </summary>
  /// <param name="name">The file name (case-insensitive).</param>
  /// <returns>A stream for reading the file, or null if not found.</returns>
  public Stream? OpenFile(string name)
  {
    if (name == null)
    {
      throw new ArgumentNullException(nameof(name));
    }

    try
    {
      return _archive.OpenFile(name);
    }
    catch (FileNotFoundException)
    {
      return null;
    }
  }

  private string? GetFileText(string name)
  {
    lock (_lock)
    {
      if (_cache.TryGetValue(name, out string? cachedValue))
      {
        return cachedValue;
      }

      string? text = null;

      if (_archive.FileExists(name))
      {
        try
        {
          using Stream stream = _archive.OpenFile(name);
          using StreamReader reader = new StreamReader(stream, System.Text.Encoding.ASCII);
          text = reader.ReadToEnd();
        }
        catch
        {
          // If we can't read the file, return null
          text = null;
        }
      }

      _cache[name] = text;
      return text;
    }
  }
}