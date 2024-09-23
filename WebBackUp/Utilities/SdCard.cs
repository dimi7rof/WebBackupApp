using NAudio.Wave;
using System.Diagnostics;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal static class SdCard
{
    internal static string Execute(PathData pathData, Func<string, Task> progressCallback, ILogger<Program> logger)
    {
        var totalFiles = 0;
        var deletedFiles = 0;

        var lists = pathData.SourcePaths
            .Select((source, i) => (source, Destination: pathData.DestinationPaths[i]));

        var realPathList = lists
            .Skip(1)
            .Select(x => (x.source, $"{lists.First().Destination}{x.Destination[1..]}"))
            .ToList();

        logger.LogAndSendMessage($"Transfer started: {DateTime.Now}", progressCallback);


        var sw = Stopwatch.StartNew();
        foreach (var (source, destination) in realPathList)
        {
            deletedFiles += DeleteFilesAndFolders(source, destination, progressCallback, logger);
            totalFiles += CopyMissingItems(source, destination, progressCallback, logger);
        }

        sw.Stop();
        return $"Transfer of {totalFiles} files completed in {sw.Elapsed.Hours}h:{sw.Elapsed.Minutes}m:{sw.Elapsed.Seconds}s.";
    }

    private static int CopyMissingItems(string sourcePath, string destinationPath, Func<string, Task> progressCallback, ILogger<Program> logger)
    {
        if (!Directory.Exists(sourcePath))
        {
            logger.LogAndSendMessage($"Source directory '{sourcePath}' not found!", progressCallback);
            return 0;
        }

        if (!Directory.Exists(destinationPath))
        {
            logger.LogAndSendMessage($"Destination directory '{destinationPath}' not found!", progressCallback);
            return 0;
        }

        var fileCount = 0;

        var missingFolders = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceDir => !Directory.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceDir))))
            .Select(x => Directory.CreateDirectory(Path.Combine(destinationPath, GetRelativePath(sourcePath, x))));

        var missingFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceFile => !File.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFile))));

        var fileCountString = missingFiles.Count() == 1 ? "file" : "files";
        progressCallback($"{missingFiles.Count()} new {fileCountString} found.").Wait();

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
                    progressCallback($"[Warning] Low sample rate: {mp3.SampleRate}: '{destinationFile}'").Wait();
                }

                var bitRate = mp3.AverageBytesPerSecond * 8 / 1000; 
                if (bitRate < 128)
                {
                    progressCallback($"[Warning] Low bitrate: {bitRate}: '{destinationFile}'").Wait();
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
                progressCallback($"{time}s - {msg} - {destinationFile}").Wait();
            }
            catch (Exception ex)
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine("D:\\Music\\SkodaError", fileName));
                File.Delete(file);

                sw.Stop();
                var time = double.Parse(sw.ElapsedMilliseconds.ToString()) / 1000;
                progressCallback($"Failed: {time} - {msg} - {destinationFile},\n\t{ex.Message}").Wait();
            }
            
            fileCount++;
        }

        return fileCount;
    }

    private static int DeleteFilesAndFolders(string sourcePath, string destinationPath, Func<string, Task> progressCallback, ILogger<Program> logger)
    {
        if (!Directory.Exists(sourcePath) || !Directory.Exists(destinationPath))
        {
            return 0;
        }

        var dirsToDelete = Directory.GetDirectories(destinationPath, "*", SearchOption.AllDirectories)
            .Where(destinationDir => !Directory.Exists(Path.Combine(sourcePath, GetRelativePath(destinationPath, destinationDir))));

        var filesToDelete = Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories)
            .Where(destinationFile => !File.Exists(Path.Combine(sourcePath, GetRelativePath(destinationPath, destinationFile))));

        DeleteFolders(dirsToDelete, progressCallback);

        var fileCount = DeleteFiles(filesToDelete, progressCallback);

        return fileCount;
    }

    private static int DeleteFiles(IEnumerable<string> filesToDelete, Func<string, Task> progressCallback)
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
                progressCallback($"{sw.ElapsedMilliseconds} - {file} - Deleted");
                fileCount++;
            }
            catch (Exception ex)
            {
                progressCallback($"Cannot delete file: {file}, {ex.Message}").Wait();
            }
        }

        return fileCount;
    }

    private static void DeleteFolders(IEnumerable<string> dirsToDelete, Func<string, Task> progressCallback)
    {
        foreach (var folder in dirsToDelete)
        {
            try
            {
                Directory.Delete(folder, false);
            }
            catch (Exception ex)
            {
                progressCallback($"Cannot delete folder {folder}, {ex.Message}").Wait();
            }
        }
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }


    private static void LogAndSendMessage(this ILogger<Program> logger, string msg, Func<string, Task> progressCallback)
    {
        logger.LogInformation(msg);
        progressCallback($"{msg}").Wait();
    }

}
