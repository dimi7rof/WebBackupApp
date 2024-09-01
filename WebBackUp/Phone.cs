using MediaDevices;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace WebBackUp;

[SupportedOSPlatform("Windows")]
public static class Phone
{
    public static string Execute(PathData pathData, Func<string, Task> progressCallback)
    {
        var devices = MediaDevice.GetDevices().ToArray();
        progressCallback($"Found {devices.Length} device(s): {string.Join(", ", devices.Select(x => x.FriendlyName))}\n").Wait();

        while (devices.Length == 0)
        {
            progressCallback("No devices found.\n").Wait();
            Task.Delay(2000).Wait();
            devices = MediaDevice.GetDevices().ToArray();
        }

        var device = devices.First(); // Assuming the first device is the desired smartphone

        progressCallback($"Connected to the smartphone: {device.FriendlyName}\n").Wait();

        var sw = Stopwatch.StartNew();
        device.Connect();
        var rootDirectory = device.GetRootDirectory();

        if (rootDirectory == null)
        {
            return "Unable to get root directory";
        }
        var internalStorage = rootDirectory.EnumerateDirectories().First();
        var dcim = internalStorage.EnumerateDirectories().First(x => x.FullName.Contains("\\DCIM"));

        var count = 0;
        var lists = pathData.SourcePaths.Select((x, i) => (x, pathData.DestinationPaths[i]));
        foreach (var (source, destination) in lists)
        {
            var existingFiles = Directory.GetFiles(destination)
                .ToDictionary(x => x.Split(Path.DirectorySeparatorChar).Last(), x => x);

            var currentDir = dcim.EnumerateDirectories().FirstOrDefault(x => x.FullName.Contains($"\\{source}"))
                ?? internalStorage.EnumerateDirectories()
                    .First(x => x.FullName.Contains("\\Pictures")).EnumerateDirectories()
                    .First(x => x.FullName.Contains("\\Gallery")).EnumerateDirectories()
                    .First(x => x.FullName.Contains("\\owner")).EnumerateDirectories()
                    .FirstOrDefault(x => x.FullName.Contains($"\\{source}"));

            if (currentDir == null)
            {
                progressCallback($"Directory {source} does not exist!\n").Wait();
                continue;
            }

            var missingFiles = currentDir.EnumerateFiles()
                .Where(x => !existingFiles.ContainsKey(x.Name) && !x.Name.StartsWith('.')).ToList();
            if (missingFiles.Count == 0)
            {
                var currentDirName = currentDir.FullName.Split('\\', StringSplitOptions.RemoveEmptyEntries).Skip(1);
                progressCallback($"No new files found in {string.Join('\\', currentDirName)}\n").Wait();
                continue;
            }
            progressCallback($"Found {missingFiles.Count} new files for {destination}...\n").Wait();

            foreach (var file in missingFiles)
            {
                var sw1 = Stopwatch.StartNew();
                var filePath = Path.Combine(destination, file.Name);
                progressCallback($"{filePath}").Wait();
                using var stream = file.OpenRead();
                using var fileStream = File.Create(filePath);
                stream.CopyTo(fileStream);
                sw1.Stop();
                progressCallback($" => Time:{sw1.ElapsedMilliseconds} ms\n").Wait();
            }
            count += missingFiles.Count;
        }
        sw.Stop();

        return count == 0 ? string.Empty : $"Download of {count} files complete for {sw.Elapsed}.\n";
    }
}

