using MediaDevices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Runtime.Versioning;
using WebBackUp.Hubs;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

[SupportedOSPlatform("Windows")]
internal static class Phone
{
    internal static async Task Execute([FromBody] UserData userData, IHubContext<ProgressHub> hubContext)
    {
        
        var devices = MediaDevice.GetDevices().ToArray();
        if (devices.Length == 0)
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", "Device not found!");
            return;
        }

        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Found {devices.Length} device(s): {string.Join(", ", devices.Select(x => x.FriendlyName))}");

        MediaDevice? device = null;
        if (devices.Length == 1)
        {
            device = devices.First();
        }
        else
        {
            device = devices.Where(d => d.FriendlyName.Contains("samsung", StringComparison.CurrentCultureIgnoreCase)
                || d.FriendlyName.Contains("xiaomi", StringComparison.CurrentCultureIgnoreCase)).First();
        }

        var sw = Stopwatch.StartNew();
        try
        {
            device.Connect();
        }
        catch (Exception ex)
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Unable to get connect to device: {device.FriendlyName}! {ex.Message}");
            return;
        }

        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Connected to the smartphone: {device.FriendlyName}");

        var rootDirectory = device.GetRootDirectory();
        if (rootDirectory == null)
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", "Unable to get root directory!");
            return;
        }

        var internalStorage = rootDirectory.EnumerateDirectories().First();
        var dcim = internalStorage.EnumerateDirectories().First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}DCIM"));

        var count = 0;
        var pathData = userData.Phone.Paths;
        var lists = pathData.SourcePaths
            .Select((source, i) => (source, Destination: pathData.DestinationPaths[i]))
            .Where(x => !string.IsNullOrEmpty(x.source) && !string.IsNullOrEmpty(x.Destination))
            .ToList();

        foreach (var (source, destination) in lists)
        {
            if (!Directory.Exists(destination))
            {
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Creating folder: {destination}");
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
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Directory {source} does not exist!");
                continue;
            }

            var missingFiles = currentDir.EnumerateFiles()
                .Where(x => !existingFiles.ContainsKey(x.Name) && !x.Name.StartsWith('.')).ToList();
            if (missingFiles.Count == 0)
            {
                var currentDirName = currentDir.FullName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Skip(1);
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"No new files found in {string.Join(Path.DirectorySeparatorChar, currentDirName)}");
                continue;
            }

            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Found {missingFiles.Count} new files for {destination}...");

            foreach (var file in missingFiles)
            {
                var sw1 = Stopwatch.StartNew();
                var filePath = Path.Combine(destination, file.Name);

                using var stream = file.OpenRead();
                using var fileStream = File.Create(filePath);
                stream.CopyTo(fileStream);

                sw1.Stop();
                var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"{time}s - {filePath}");
            }
            count += missingFiles.Count;
        }
        sw.Stop();

        var msg = $"Download of {count} files complete in {sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s.";
        await hubContext.Clients.All.SendAsync("ReceiveProgress", msg);

        device.Disconnect();
        return;
    }
}

