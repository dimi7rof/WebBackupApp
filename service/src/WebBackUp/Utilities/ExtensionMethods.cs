using System.Diagnostics;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal static class ExtensionMethods
{
    internal static string GetElapsedTime(this Stopwatch sw)
        => $"{sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s";

    internal static PathData FilterPaths(this PathData paths)
        => new()
        {
            SourcePaths = paths.SourcePaths.Where(x => !string.IsNullOrEmpty(x)).ToList(),
            DestinationPaths = paths.DestinationPaths.Where(x => !string.IsNullOrEmpty(x)).ToList(),
        };
}
