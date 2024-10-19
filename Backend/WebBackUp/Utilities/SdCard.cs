using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NAudio.Wave;
using System.Diagnostics;
using WebBackUp.Hubs;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal static class SdCard
{
    internal static async Task Execute([FromBody] UserData userData, IHubContext<ProgressHub, IBackupProgress> hubContext)
    {
        var totalFiles = 0;
        var deletedFiles = 0;

        var pathData = userData.SD.Paths;
        var lists = pathData.SourcePaths
            .Select((source, i) => (source, Destination: pathData.DestinationPaths[i]));

        var realPathList = lists
            .Where(x => !string.IsNullOrEmpty(x.source) && !string.IsNullOrEmpty(x.Destination))
            .Select(x => (x.source, $"{userData.SD.DeviceLetter}{x.Destination[1..]}"))
            .ToList();

        await hubContext.Clients.All.ReceiveProgress($"Transfer started: {DateTime.Now}");

        var sw = Stopwatch.StartNew();
        foreach (var (source, destination) in realPathList)
        {
            if (userData.SD.Sync)
            {
                deletedFiles += await DeleteFilesAndFolders(source, destination, hubContext);
            }

            totalFiles += await CopyMissingItems(source, destination, hubContext);
        }

        sw.Stop();
        await hubContext.Clients.All.ReceiveProgress(
            $"Transfer of {totalFiles} files completed in {sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s.");
        return;
    }

    private static async Task<int> CopyMissingItems(string sourcePath, string destinationPath,
        IHubContext<ProgressHub, IBackupProgress> hubContext)
    {
        if (!Directory.Exists(sourcePath))
        {
            await hubContext.Clients.All.ReceiveProgress($"Source directory '{sourcePath}' not found!");
            return 0;
        }

        if (!Directory.Exists(destinationPath))
        {
            await hubContext.Clients.All.ReceiveProgress($"Destination directory '{destinationPath}' not found!");
            return 0;
        }

        var fileCount = 0;

        var missingFolders = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceDir => !Directory.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceDir))))
            .Select(x => Directory.CreateDirectory(Path.Combine(destinationPath, GetRelativePath(sourcePath, x))));

        var missingFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceFile => !File.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFile))));

        var fileCountString = missingFiles.Count() == 1 ? "file" : "files";
        await hubContext.Clients.All.ReceiveProgress($"{missingFiles.Count()} new {fileCountString} found.");

        foreach (var sourceFilePath in missingFiles)
        {
            var sw = Stopwatch.StartNew();
            var destinationFilePath = Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFilePath));
            var msg = string.Empty;

            try
            {
                using var mp3Reader = new Mp3FileReader(sourceFilePath);
                var frame = mp3Reader.ReadNextFrame();
                var mp3 = mp3Reader.Mp3WaveFormat;
                if (mp3.SampleRate < 44100)
                {
                    await hubContext.Clients.All.ReceiveProgress($"[Warning] Low sample rate: {mp3.SampleRate}: '{destinationFilePath}'");
                }

                var bitRate = mp3.AverageBytesPerSecond * 8 / 1000;
                if (bitRate < 128)
                {
                    await hubContext.Clients.All.ReceiveProgress($"[Warning] Low bitrate: {bitRate}: '{destinationFilePath}'");
                }

                msg = $"{bitRate}kbps | {mp3.SampleRate}Hz | {mp3.Encoding} | {mp3.Channels} channels";

                var destinationDir = string.Join(Path.DirectorySeparatorChar,
                    destinationFilePath.Split(Path.DirectorySeparatorChar).SkipLast(1));
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                File.Copy(sourceFilePath, destinationFilePath);

                sw.Stop();
                var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                await hubContext.Clients.All.ReceiveProgress($"{time}s - {msg} - {destinationFilePath}");
            }
            catch (Exception ex)
            {
                var fileName = Path.GetFileName(sourceFilePath);
                File.Copy(sourceFilePath, Path.Combine("D:\\Music\\SkodaError", fileName));
                File.Delete(sourceFilePath);

                sw.Stop();
                var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                await hubContext.Clients.All.ReceiveProgress($"Failed: {time} - {msg} - {destinationFilePath},\n\t{ex.Message}");
            }

            fileCount++;
        }

        return fileCount;
    }

    private static async Task<int> DeleteFilesAndFolders(string sourcePath,
        string destinationPath, IHubContext<ProgressHub, IBackupProgress> hubContext)
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

    private static async Task<int> DeleteFiles(IEnumerable<string> filesToDelete,
        IHubContext<ProgressHub, IBackupProgress> hubContext)
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
                await hubContext.Clients.All.ReceiveProgress($"{sw.ElapsedMilliseconds} - {Path.GetFileName(file)} - Deleted");
                fileCount++;
            }
            catch (Exception ex)
            {
                await hubContext.Clients.All.ReceiveProgress($"Cannot delete file: {file}, {ex.Message}");
            }
        }

        return fileCount;
    }

    private static async Task DeleteFolders(IEnumerable<string> dirsToDelete,
        IHubContext<ProgressHub, IBackupProgress> hubContext)
    {
        foreach (var folder in dirsToDelete)
        {
            try
            {
                Directory.Delete(folder, false);
            }
            catch (Exception ex)
            {
                await hubContext.Clients.All.ReceiveProgress($"Cannot delete folder {folder}, {ex.Message}");
            }
        }
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }
}
