using MediaDevices;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Runtime.Versioning;
using WebBackUp.Hubs;
using WebBackUp.Models;
using WebBackUp.Utilities;

namespace WebBackUp.Services;

public interface ISmartPhoneService
{
    Task Execute(UserData userData);
}

[SupportedOSPlatform("Windows")]
public class SmartPhoneService(IHubContext<ProgressHub, IBackupProgress> hubContext) : ISmartPhoneService
{
    public async Task Execute(UserData userData)
    {

        var devices = MediaDevice.GetDevices().ToArray();
        if (devices.Length == 0)
        {
            await hubContext.Clients.All.ReceiveProgress("Device not found!");
            return;
        }

        await hubContext.Clients.All.ReceiveProgress(
            $"Found {devices.Length} device(s): {string.Join(", ", devices.Select(x => x.FriendlyName))}");

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
            await hubContext.Clients.All.ReceiveProgress(
                $"Unable to connect to device: {device.FriendlyName}! {ex.Message}");
            return;
        }

        await hubContext.Clients.All.ReceiveProgress(
            $"Connected to the smartphone: {device.FriendlyName}");

        var rootDirectory = device.GetRootDirectory();
        if (rootDirectory == null)
        {
            await hubContext.Clients.All.ReceiveProgress("Unable to get root directory!");
            return;
        }

        var internalStorage = rootDirectory.EnumerateDirectories().First();
        var dcim = internalStorage.EnumerateDirectories()
            .First(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}DCIM"));

        var count = 0;
        var pathData = userData.Phone.Paths;
        var lists = pathData.SourcePaths
            .Select((source, i) => (source, Destination: pathData.DestinationPaths[i]))
            .Where(x => !string.IsNullOrEmpty(x.source) && !string.IsNullOrEmpty(x.Destination))
            .ToList();

        foreach (var (source, destination) in lists)
        {
            if ($"{destination[1]}{destination[2]}" == ":\\")
            {
                count += await CopyToPc(source, destination, internalStorage, dcim);
            }
            else
            {
                await CopyToPhone(device, source, destination, internalStorage);
            }

        }
        sw.Stop();

        var msg = $"Download of {count} files complete in {sw.GetElapsedTime()}.";
        await hubContext.Clients.All.ReceiveProgress(msg);

        device.Disconnect();
        return;
    }

    private async Task CopyToPhone(MediaDevice device, string source, string destination,
        MediaDirectoryInfo internalStorage)
    {
        var sw = Stopwatch.StartNew();
        var download = @"Internal shared storage\Download"; ;
        if (!device.DirectoryExists(download))
        {
            internalStorage.CreateSubdirectory("Download");
            await hubContext.Clients.All.ReceiveProgress($"Directory {download} created successfully.");
        }

        var destinationPath = $"{download}\\{destination}";

        var downloadDir = internalStorage.EnumerateDirectories()
            .First(x => x.FullName.Contains("Download"));
        if (!device.DirectoryExists(destinationPath))
        {
            downloadDir.CreateSubdirectory(destination);
            await hubContext.Clients.All.ReceiveProgress($"Directory {destinationPath} created successfully.");
        }

        var count = 0;
        var destinationDir = downloadDir.EnumerateDirectories().First(x => x.FullName.Contains(destination));
        var sourceFiles = Directory.GetFiles(source);
        foreach (var file in sourceFiles)
        {
            var sw1 = Stopwatch.StartNew();

            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destinationPath, fileName);
            if (device.FileExists(destFile))
            {
                continue;
            }

            try
            {
                device.UploadFile(file, destFile);
            }
            catch (Exception ex)
            {
                await hubContext.Clients.All.ReceiveProgress(ex.Message);
            }

            sw1.Stop();
            var time = double.Parse(sw1.ElapsedMilliseconds.ToString()) / 1000;
            await hubContext.Clients.All.ReceiveProgress($"{time}s - {destFile}");
            count++;
        }

        var msg = $"Uploaded {count} files to {destination} in {sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s.";
        await hubContext.Clients.All.ReceiveProgress(msg);
    }

    private async Task<int> CopyToPc(string source, string destination, MediaDirectoryInfo internalStorage,
        MediaDirectoryInfo dcim)
    {
        if (!Directory.Exists(destination))
        {
            await hubContext.Clients.All.ReceiveProgress($"Creating folder: {destination}");
            Directory.CreateDirectory(destination);
        }

        var existingFiles = Directory.GetFiles(destination)
            .ToDictionary(x => x.Split(Path.DirectorySeparatorChar).Last(), x => x);

        var currentDir = dcim.EnumerateDirectories()
            .FirstOrDefault(x => x.FullName.Contains($"{Path.DirectorySeparatorChar}{source}"))
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
            await hubContext.Clients.All.ReceiveProgress($"Directory {source} does not exist!");
            return 0;
        }

        var missingFiles = currentDir.EnumerateFiles()
            .Where(x => !existingFiles.ContainsKey(x.Name) && !x.Name.StartsWith('.')).ToList();

        if (missingFiles.Count == 0)
        {
            var currentDirName = currentDir.FullName
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1);
            await hubContext.Clients.All.ReceiveProgress($"No new files found in {string.Join(Path.DirectorySeparatorChar, currentDirName)}");
            return 0;
        }

        await hubContext.Clients.All.ReceiveProgress($"Found {missingFiles.Count} new files for {destination}...");

        await CopyFiles(missingFiles, destination);
        return missingFiles.Count;
    }

    private async Task CopyFiles(List<MediaFileInfo> missingFiles, string destination)
    {
        foreach (var file in missingFiles)
        {
            var sw1 = Stopwatch.StartNew();
            var filePath = Path.Combine(destination, file.Name);

            using var stream = file.OpenRead();
            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);

            sw1.Stop();
            var time = double.Parse(sw1.ElapsedMilliseconds.ToString()) / 1000;
            await hubContext.Clients.All.ReceiveProgress($"{time}s - {filePath}");
        }
    }
}
