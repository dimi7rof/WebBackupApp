using MediaDevices;
using System.Diagnostics;
using System.Runtime.Versioning;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

[SupportedOSPlatform("Windows")]
internal static class Phone
{
    internal static string Execute(PathData pathData, Func<string, Task> progressCallback, ILogger<Program> logger)
    {
        var devices = MediaDevice.GetDevices().ToArray();
        if (devices.Length == 0)
        {
            logger.LogAndSendMessage("No devices found.", progressCallback);
            return "No devices found.";
        }

        logger.LogAndSendMessage($"Found {devices.Length} device(s): {string.Join(", ", devices.Select(x => x.FriendlyName))}", progressCallback);
        var device = devices.First();
        logger.LogAndSendMessage($"Connected to the smartphone: {device.FriendlyName}", progressCallback);

        var sw = Stopwatch.StartNew();
        device.Connect();

        var rootDirectory = device.GetRootDirectory();
        if (rootDirectory == null)
        {
            logger.LogAndSendMessage("Unable to get root directory!", progressCallback);
            return "Unable to get root directory!";
        }

        var internalStorage = rootDirectory.EnumerateDirectories().First();
        var dcim = internalStorage.EnumerateDirectories().First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}DCIM"));

        var count = 0;
        var lists = pathData.SourcePaths.Select((x, i) => (x, pathData.DestinationPaths[i]));
        foreach (var (source, destination) in lists)
        {
            if (!Directory.Exists(destination))
            {
                logger.LogAndSendMessage($"Creating folder: {destination}", progressCallback);
                //Task.Delay(2000).Wait();
                Directory.CreateDirectory(destination);
            }

            var existingFiles = Directory.GetFiles(destination)
                .ToDictionary(x => x.Split(Path.DirectorySeparatorChar).Last(), x => x);

            var currentDir = dcim.EnumerateDirectories().FirstOrDefault(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}{source}"))
                ?? internalStorage.EnumerateDirectories()
                    .First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}Pictures")).EnumerateDirectories()
                    .FirstOrDefault(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}{source}"))
                ?? internalStorage.EnumerateDirectories()
                    .First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}Pictures")).EnumerateDirectories()
                    .First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}Gallery")).EnumerateDirectories()
                    .First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}owner")).EnumerateDirectories()
                    .FirstOrDefault(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}{source}"));

            if (currentDir == null)
            {
                logger.LogAndSendMessage($"Directory {source} does not exist!", progressCallback);
                continue;
            }

            var missingFiles = currentDir.EnumerateFiles()
                .Where(x => !existingFiles.ContainsKey(x.Name) && !x.Name.StartsWith('.')).ToList();
            if (missingFiles.Count == 0)
            {
                var currentDirName = currentDir.FullName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Skip(1);
                logger.LogAndSendMessage($"No new files found in {string.Join(Path.DirectorySeparatorChar, currentDirName)}", progressCallback);
                continue;
            }

            logger.LogAndSendMessage($"Found {missingFiles.Count} new files for {destination}...", progressCallback);

            foreach (var file in missingFiles)
            {
                var sw1 = Stopwatch.StartNew();
                var filePath = Path.Combine(destination, file.Name);

                using var stream = file.OpenRead();
                using var fileStream = File.Create(filePath);
                stream.CopyTo(fileStream);

                sw1.Stop();
                progressCallback($"{filePath} => Time:{sw1.ElapsedMilliseconds} ms").Wait();
                logger.LogInformation($"{filePath} => Time:{sw1.ElapsedMilliseconds} ms");
            }
            count += missingFiles.Count;
        }
        sw.Stop();

        var msg = $"Download of {count} files complete for {sw.Elapsed}.";
        logger.LogAndSendMessage(msg, progressCallback);

        return count == 0 ? string.Empty : msg;
    }

    private static void LogAndSendMessage(this ILogger<Program> logger, string msg,  Func<string, Task> progressCallback)
    {
        logger.LogInformation(msg);
        progressCallback($"{msg}").Wait();
    }
}

