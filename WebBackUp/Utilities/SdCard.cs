using NAudio.Wave;
using System.Diagnostics;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal class SdCard
{
    internal static string Execute(PathData pathData, Func<string, Task> progressCallback)
    {
        var lists = pathData.SourcePaths.Select((source, i) => (source, Destination: pathData.DestinationPaths[i]));
        var (_, letter) = lists.First();
        var realPathList = lists.Skip(1).Select(x => (x.source, $"{letter}{x.Destination[1..]}")).ToList();
        var totalFiles = 0;
        var deletedFiles = 0;
        foreach (var (source, destination) in realPathList)
        {
            deletedFiles += DeleteFiles(source, destination, progressCallback);
            totalFiles += CopyMissingItems(source, destination, progressCallback);
        }

        return $"Backup of {totalFiles} files completed.\n";
    }

    private static int DeleteFiles(string sourcePath, string destinationPath, Func<string, Task> progressCallback)
    {
        var fileCount = 0;
        var log = new List<string>();

        var missingFolders = Directory.GetDirectories(destinationPath, "*", SearchOption.AllDirectories)
            .Where(destinationDir => !Directory.Exists(Path.Combine(sourcePath, GetRelativePath(destinationPath, destinationDir))));

        var missingFiles = Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories)
            .Where(destinationFile => !File.Exists(Path.Combine(sourcePath, GetRelativePath(destinationPath, destinationFile))));

        foreach (var folder in missingFolders)
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

        foreach (var file in missingFiles)
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

    static int CopyMissingItems(string sourcePath, string destinationPath, Func<string, Task> progressCallback)
    {
        var fileCount = 0;

        var missingFolders = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceDir => !Directory.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceDir))));

        var missingFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
            .Where(sourceFile => !File.Exists(Path.Combine(destinationPath, GetRelativePath(sourcePath, sourceFile))));

        foreach (var folder in missingFolders)
        {
            string destinationFolder = Path.Combine(destinationPath, GetRelativePath(sourcePath, folder));
            Directory.CreateDirectory(destinationFolder);
        }

        foreach (var file in missingFiles)
        {
            var sw = new Stopwatch();
            sw.Start();
            string destinationFile = Path.Combine(destinationPath, GetRelativePath(sourcePath, file));

            try
            {
                var msg = string.Empty;
                using (var mp3Reader = new Mp3FileReader(file))
                {
                    var sampleRate = mp3Reader.Mp3WaveFormat.SampleRate;
                    if (sampleRate < 32000)
                    {
                        progressCallback($"Warning: Low sample rate: {sampleRate}\n").Wait();
                    }
                    else
                    {
                        msg = sampleRate.ToString();
                    }
                }
                File.Copy(file, destinationFile);

                if (sw.ElapsedMilliseconds % 2000 == 0)
                {
                    progressCallback($"Copying {destinationFile}\n").Wait();
                }

                sw.Stop();
                progressCallback($"{msg} - {sw.ElapsedMilliseconds} - {destinationFile}\n").Wait();
                fileCount++;
            }
            catch (Exception ex)
            {
                progressCallback($"Cannot copy file: {file}, {ex.Message}\n").Wait();
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine("D:\\Music\\SkodaError", fileName));
                File.Delete(file);
            }
        }

        return fileCount;
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }
}
