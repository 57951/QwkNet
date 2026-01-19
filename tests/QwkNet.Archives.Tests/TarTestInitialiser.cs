using System.Linq;
using System.Runtime.CompilerServices;
using QwkNet.Archive;
using QwkNet.Archives.Tar;

namespace QwkNet.Archives.Tests;

/// <summary>
/// Module initializer that registers the TAR extension before any tests run.
/// </summary>
/// <remarks>
/// This class uses the ModuleInitializer attribute to ensure TAR extension
/// registration happens before xUnit discovers or runs any tests.
/// </remarks>
internal static class TarTestInitializer
{
  /// <summary>
  /// Initializes the TAR extension for all tests.
  /// </summary>
  /// <remarks>
  /// This method is called automatically by the .NET runtime before any other
  /// code in this assembly executes. It ensures the TAR extension is registered
  /// with ArchiveFactory before xUnit starts running tests.
  /// </remarks>
  [ModuleInitializer]
  public static void Initialize()
  {
    // Unregister first in case already registered (e.g., from previous test run)
    ArchiveFactory.UnregisterExtension(ArchiveFormatId.From("tar"));
    
    // Register TAR extension globally for all tests
    TarArchiveExtension extension = new TarArchiveExtension();
    ArchiveFactory.RegisterExtension(extension);
    
    // Verify registration succeeded
    System.Collections.Generic.IReadOnlyList<ArchiveFormatId> registered = 
      ArchiveFactory.ListRegisteredExtensions();
    
    if (!registered.Any(x => x == ArchiveFormatId.From("tar")))
    {
      throw new System.InvalidOperationException(
        "TAR extension failed to register in module initializer!");
    }
  }
}