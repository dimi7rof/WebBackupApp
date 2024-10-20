using System.Diagnostics;

namespace WebBackUp.Utilities;

internal static class ExtensionMethods
{
    internal static string GetElapsedTime(this Stopwatch sw)
        => $"{sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s";
}
