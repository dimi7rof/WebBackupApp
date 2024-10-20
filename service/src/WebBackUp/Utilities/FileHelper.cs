namespace WebBackUp.Utilities;

internal static class FileHelper
{
    internal static string GetFilePath(string setId)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json", $"{setId}.json");
}
