using NAudio.Wave;
using System.Diagnostics;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal class SdCard
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

        return $"Backup of {totalFiles} files completed.\n";
    }

    private static int CopyMissingItems(string sourcePath, string destinationPath, Func<string, Task> progressCallback)
    {
        var fileCount = 0;

        var missingFolders = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceDir => !Directory.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceDir))))
            .Select(x => Directory.CreateDirectory(Path.Combine(destinationPath, GetRelativePath(sourcePath, x))));

        var missingFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceFile => !File.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFile))));

        foreach (var file in missingFiles)
        {
            var sw = Stopwatch.StartNew();
            var msg = string.Empty;
            string destinationFile = Path.Combine(destinationPath, GetRelativePath(sourcePath, file));

            try
            {
                
                using var mp3Reader = new Mp3FileReader(file);
                var mp3 = mp3Reader.Mp3WaveFormat;
                if (mp3.SampleRate < 32000)
                {
                    progressCallback($"Warning: Low sample rate: {mp3.SampleRate}\n").Wait();
                }
                else
                {
                    msg = $"SampleRate:{mp3.SampleRate}, Encoding:{mp3.Encoding}, Channels:{mp3.Channels}";
                }

                File.Copy(file, destinationFile);

                if (sw.ElapsedMilliseconds % 2000 == 0)
                {
                    progressCallback($"Copying {destinationFile}\n").Wait();
                }
            }
            catch (Exception ex)
            {
                progressCallback($"Cannot copy file: {file}, {ex.Message}\n").Wait();
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine("D:\\Music\\SkodaError", fileName));
                File.Delete(file);
            }

            sw.Stop();
            progressCallback($"{destinationFile} - {sw.ElapsedMilliseconds} - {msg}\n").Wait();
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
                    progressCallback($"Deleting {file}...\n").Wait();
                }

                sw.Stop();
                progressCallback($"{sw.ElapsedMilliseconds} - {file} - Deleted\n");
                fileCount++;
            }
            catch (Exception ex)
            {
                progressCallback($"Cannot delete file: {file}, {ex.Message}\n").Wait();
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
                progressCallback($"Cannot delete folder {folder}, {ex.Message}\n").Wait();
            }
        }
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }
}
