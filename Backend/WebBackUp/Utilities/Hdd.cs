using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using WebBackUp.Hubs;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal static class Hdd
{
    internal static async Task Execute([FromBody]UserData userData, IHubContext<ProgressHub> hubContext)
    {
        await hubContext.Clients.All.SendAsync("ReceiveProgress", "Begin backup...");

        var paths = userData.HDD.Paths;
        var lists = paths.SourcePaths.Select((source, i) => (source, Destination: paths.DestinationPaths[i]));
        var realPathList = lists
            .Where(x => !string.IsNullOrEmpty(x.source) && !string.IsNullOrEmpty(x.Destination))
            .Select(x => (x.source, $"{userData.HDD.DeviceLetter}{x.Destination[1..]}"))
            .ToList();

        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Backup of {realPathList.Count} directories started: {DateTime.Now}");

        var sw = Stopwatch.StartNew();
        foreach (var (source, destination) in realPathList)
        {
            await CopyDirectoriesAndFiles(source, destination, hubContext);
        }

        sw.Stop();
        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Backup completed in {sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s.");
    }

    private static async Task CopyDirectoriesAndFiles(string source, string destination, IHubContext<ProgressHub> hubContext)
    {
        if (!Directory.Exists(source))
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Source directory '{source}' not found!");
            return;
        }
        var sourceDirs = Directory.GetDirectories(source);

        if (!Directory.Exists(destination))
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Destination directory '{destination}' not found!");
            return;
        }
        var destinationDirs = Directory.GetDirectories(destination);

        var missingDirs = sourceDirs.Where(x => !destinationDirs.Select(d => d.Split(Path.DirectorySeparatorChar).Last())
                .Contains(x.Split(Path.DirectorySeparatorChar).Last()))
            .Select(x => Path.Combine(destination, x.Split(Path.DirectorySeparatorChar).Last()))
            .Select(Directory.CreateDirectory)
            .ToList();

        var dirs = sourceDirs
            .Select(sourceDir => (sourceDir,
                Destination: destinationDirs.First(d => d.Split(Path.DirectorySeparatorChar).Last() == sourceDir.Split(Path.DirectorySeparatorChar).Last())))
            .ToList();

        foreach (var (sourceDir, destinationDir) in dirs)
        {
            var sourceFiles = Directory.GetFiles(sourceDir);
            var destinationFiles = Directory.GetFiles(destinationDir);

            var missingFiles = sourceFiles.Where(x => !destinationFiles.Select(d => d.Split(Path.DirectorySeparatorChar).Last())
                .Contains(x.Split(Path.DirectorySeparatorChar).Last())).ToList();

            if (missingFiles.Count == 0)
            {
                return;
            }

            var file = missingFiles.Count == 1 ? "file" : "files";
            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Found {missingFiles.Count} new {file}");

            foreach (var sourceFilePath in missingFiles)
            {
                var fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationDir, fileName);
                try
                {
                    var sw = Stopwatch.StartNew();
                    File.Copy(sourceFilePath, destinationFilePath);

                    sw.Stop();
                    var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                    await hubContext.Clients.All.SendAsync("ReceiveProgress", $"{time}s - {destinationFilePath}");
                }
                catch (Exception ex)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Cannot copy file: {destinationFilePath}, {ex.Message}");
                }
            }

            await CopyDirectoriesAndFiles(sourceDir, destinationDir, hubContext);
        }
    }
}