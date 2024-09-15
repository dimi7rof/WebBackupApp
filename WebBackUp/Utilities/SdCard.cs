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

        foreach (var (source, destination) in realPathList)
        {
            deletedFiles += DeleteFilesAndFolders(source, destination, progressCallback);
            totalFiles += CopyMissingItems(source, destination, progressCallback);
        }

        return $"Transfer of {totalFiles} files completed.";
    }

    private static int CopyMissingItems(string sourcePath, string destinationPath, Func<string, Task> progressCallback)
    {
        var fileCount = 0;

        var missingFolders = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceDir => !Directory.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceDir))))
            .Select(x => Directory.CreateDirectory(Path.Combine(destinationPath, GetRelativePath(sourcePath, x))));

        var missingFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceFile => !File.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFile))));

        progressCallback($"Start transfering {missingFiles.Count()} files...").Wait();

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
                    progressCallback($"Warning: Low sample rate: {mp3.SampleRate}").Wait();
                }
                if (frame.BitRate < 128_000)
                {
                    progressCallback($"Warning: Low bitrate: {frame.BitRate}").Wait();
                }
                msg = $"{frame.BitRate / 1_000}kbps, {mp3.SampleRate}Hz, {mp3.Encoding}, {mp3.Channels} channels";
                var dir = string.Join(Path.DirectorySeparatorChar, destinationFile.Split(Path.DirectorySeparatorChar).SkipLast(1));
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(file, destinationFile);

                if (sw.ElapsedMilliseconds % 2000 == 0)
                {
                    progressCallback($"Copying {destinationFile}").Wait();
                }
            }
            catch (Exception ex)
            {
                progressCallback($"Cannot copy file: {file}, {ex.Message}").Wait();
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine("D:\\Music\\SkodaError", fileName));
                File.Delete(file);
            }

            sw.Stop();
            progressCallback($"{sw.ElapsedMilliseconds.ToString().PadLeft(5, '0')} - {msg} - {destinationFile}").Wait();
            fileCount++;
        }

        return fileCount;
    }

    private static int DeleteFilesAndFolders(string sourcePath, string destinationPath, Func<string, Task> progressCallback)
    {
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

                if (sw.ElapsedMilliseconds % 10000 == 0)
                {
                    progressCallback($"Deleting {file}...").Wait();
                }

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
}
