﻿using System.Diagnostics;
using WebBackUp.Models;

namespace WebBackUp.Utilities;

internal static class Hdd
{
    internal static string Execute(PathData pathData, Func<string, Task> progressCallback, ILogger<Program> logger)
    {
        var lists = pathData.SourcePaths.Select((source, i) => (source, Destination: pathData.DestinationPaths[i]));
        var (_, letter) = lists.First();
        var realPathList = lists.Skip(1).Select(x => (x.source, $"{letter}{x.Destination[1..]}")).ToList();
        logger.LogAndSendMessage($"Backup of {realPathList.Count} directories started: {DateTime.Now}", progressCallback);

        var sw = Stopwatch.StartNew();
        foreach (var (source, destination) in realPathList)
        {
            CopyDirectoriesAndFiles(source, destination, progressCallback, logger);
        }

        sw.Stop();
        logger.LogAndSendMessage($"Backup completed in {sw.Elapsed}.", progressCallback);
        return string.Empty;
    }

    private static void CopyDirectoriesAndFiles(string source, string destination, Func<string, Task> progressCallback, ILogger<Program> logger)
    {
        if (!Directory.Exists(source))
        {
            logger.LogAndSendMessage($"Source directory '{source}' not found!", progressCallback);
            return;
        }
        var sourceDirs = Directory.GetDirectories(source);

        if (!Directory.Exists(destination))
        {
            logger.LogAndSendMessage($"Destination directory '{destination}' not found!", progressCallback);
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
            logger.LogAndSendMessage($"Found {missingFiles.Count} new {file}", progressCallback);

            foreach (var sourceFilePath in missingFiles)
            {
                var fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(destinationDir, fileName);
                try
                {
                    var sw = Stopwatch.StartNew();
                    File.Copy(sourceFilePath, destinationFilePath);

                    if (sw.ElapsedMilliseconds % 10000 == 0)
                    {
                        logger.LogAndSendMessage($"Copying {destinationFilePath}...", progressCallback);
                    }

                    sw.Stop();
                    logger.LogAndSendMessage($"{sw.ElapsedMilliseconds} - {destinationFilePath}", progressCallback);
                }
                catch (Exception ex)
                {
                    logger.LogAndSendMessage($"Cannot copy file: {destinationFilePath}, {ex.Message}", progressCallback);
                }
            }

            CopyDirectoriesAndFiles(sourceDir, destinationDir, progressCallback, logger);
        }
    }

    private static void LogAndSendMessage(this ILogger<Program> logger, string msg, Func<string, Task> progressCallback)
    {
        logger.LogInformation(msg);
        progressCallback($"{msg}").Wait();
    }
}