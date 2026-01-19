using System.Collections.Generic;
using QwkNet.Diagnostics.Analysis;

namespace QwkNet.Diagnostics.Output;

/// <summary>
/// Formats analysis results for output.
/// </summary>
internal interface IOutputFormatter
{
  /// <summary>
  /// Formats a single analysis result.
  /// </summary>
  string Format(AnalysisResult result);

  /// <summary>
  /// Formats multiple analysis results (batch mode).
  /// </summary>
  string FormatBatch(IReadOnlyList<AnalysisResult> results);
}