using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NAudio.Wave;
using System.Diagnostics;
using WebBackUp.Hubs;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal static class SdCard
{
    internal static async Task Execute([FromBody] UserData userData, IHubContext<ProgressHub> hubContext)
    {
        var totalFiles = 0;
        var deletedFiles = 0;

        var pathData = userData.SD.Paths;
        var lists = pathData.SourcePaths
            .Select((source, i) => (source, Destination: pathData.DestinationPaths[i]));

        var realPathList = lists
            .Skip(1)
            .Select(x => (x.source, $"{lists.First().Destination}{x.Destination[1..]}"))
            .ToList();

        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Transfer started: {DateTime.Now}");


        var sw = Stopwatch.StartNew();
        foreach (var (source, destination) in realPathList)
        {
            deletedFiles += await DeleteFilesAndFolders(source, destination, hubContext);
            totalFiles += await CopyMissingItems(source, destination, hubContext);
        }

        sw.Stop();
        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Transfer of {totalFiles} files completed in {sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s.");
        return;
    }

    private static async Task<int> CopyMissingItems(string sourcePath, string destinationPath, IHubContext<ProgressHub> hubContext)
    {
        if (!Directory.Exists(sourcePath))
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Source directory '{sourcePath}' not found!");
            return 0;
        }

        if (!Directory.Exists(destinationPath))
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Destination directory '{destinationPath}' not found!");
            return 0;
        }

        var fileCount = 0;

        var missingFolders = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceDir => !Directory.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceDir))))
            .Select(x => Directory.CreateDirectory(Path.Combine(destinationPath, GetRelativePath(sourcePath, x))));

        var missingFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceFile => !File.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFile))));

        var fileCountString = missingFiles.Count() == 1 ? "file" : "files";
        await hubContext.Clients.All.SendAsync("ReceiveProgress", $"{missingFiles.Count()} new {fileCountString} found.");

        foreach (var file in missingFiles)
        {
            var sw = Stopwatch.StartNew();
            var destinationFile = Path.Combine(destinationPath, GetRelativePath(sourcePath, file));
            var msg = string.Empty;

            try
            {
                using var mp3Reader = new Mp3FileReader(file);
                var frame = mp3Reader.ReadNextFrame();
                var mp3 = mp3Reader.Mp3WaveFormat;
                if (mp3.SampleRate < 44100)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveProgress", $"[Warning] Low sample rate: {mp3.SampleRate}: '{destinationFile}'");
                }

                var bitRate = mp3.AverageBytesPerSecond * 8 / 1000; 
                if (bitRate < 128)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveProgress", $"[Warning] Low bitrate: {bitRate}: '{destinationFile}'");
                }

                msg = $"{bitRate}kbps, {mp3.SampleRate}Hz, {mp3.Encoding}, {mp3.Channels} channels";

                var dir = string.Join(Path.DirectorySeparatorChar, destinationFile.Split(Path.DirectorySeparatorChar).SkipLast(1));
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(file, destinationFile);

                sw.Stop();
                var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"{time}s - {msg} - {destinationFile}");
            }
            catch (Exception ex)
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine("D:\\Music\\SkodaError", fileName));
                File.Delete(file);

                sw.Stop();
                var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Failed: {time} - {msg} - {destinationFile},\n\t{ex.Message}");
            }
            
            fileCount++;
        }

        return fileCount;
    }

    private static async Task<int> DeleteFilesAndFolders(string sourcePath, string destinationPath, IHubContext<ProgressHub> hubContext)
    {
        if (!Directory.Exists(sourcePath) || !Directory.Exists(destinationPath))
        {
            return 0;
        }

        var dirsToDelete = Directory.GetDirectories(destinationPath, "*", SearchOption.AllDirectories)
            .Where(destinationDir => !Directory.Exists(Path.Combine(sourcePath, GetRelativePath(destinationPath, destinationDir))));

        var filesToDelete = Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories)
            .Where(destinationFile => !File.Exists(Path.Combine(sourcePath, GetRelativePath(destinationPath, destinationFile))));

        await DeleteFolders(dirsToDelete, hubContext);

        var fileCount = await DeleteFiles(filesToDelete, hubContext);

        return fileCount;
    }

    private static async Task<int> DeleteFiles(IEnumerable<string> filesToDelete, IHubContext<ProgressHub> hubContext)
    {
        var fileCount = 0;
        foreach (var file in filesToDelete)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                File.Delete(file);

                sw.Stop();
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"{sw.ElapsedMilliseconds} - {file} - Deleted");
                fileCount++;
            }
            catch (Exception ex)
            {
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Cannot delete file: {file}, {ex.Message}");
            }
        }

        return fileCount;
    }

    private static async Task DeleteFolders(IEnumerable<string> dirsToDelete, IHubContext<ProgressHub> hubContext)
    {
        foreach (var folder in dirsToDelete)
        {
            try
            {
                Directory.Delete(folder, false);
            }
            catch (Exception ex)
            {
                await hubContext.Clients.All.SendAsync("ReceiveProgress", $"Cannot delete folder {folder}, {ex.Message}");
            }
        }
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }
}
