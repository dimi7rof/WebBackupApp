using System.Diagnostics;

namespace WebBackUp;

internal class Hdd
{
    internal static string Execute(PathData pathData, Func<string, Task> progressCallback)
    {
        var lists = pathData.SourcePaths.Select((source, i) => (source, Destination: pathData.DestinationPaths[i]));
        var (_, letter) = lists.First();
        var realPathList = lists.Skip(1).Select(x => (x.source, $"{letter}{x.Destination[1..]}")).ToList();
        var totalFiles = 0;
        foreach (var (source, destination) in realPathList)
        {
            totalFiles += CopyDirectoriesAndFiles(source, destination, progressCallback);
        }

        return $"Backup of {totalFiles} files completed.";
    }

    private static int CopyDirectoriesAndFiles(string source, string destination, Func<string, Task> progressCallback)
    {

        var sourceDirs = Directory.GetDirectories(source);
        if (sourceDirs.Length == 0)
        {
            return 0;
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

        var files = 0;
        foreach (var (sourceDir, destinationDir) in dirs)
        {
            var sourceFiles = Directory.GetFiles(sourceDir);
            var destinationFiles = Directory.GetFiles(destinationDir);

            var missingFiles = sourceFiles.Where(x => !destinationFiles.Select(d => d.Split(Path.DirectorySeparatorChar).Last())
                .Contains(x.Split(Path.DirectorySeparatorChar).Last())).ToList();

            if (missingFiles.Count == 0)
            {
                return 0;
            }

            progressCallback($"Found {missingFiles.Count} files new files\n").Wait();

            foreach (var sourceFilePath in missingFiles)
            {
                var fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.GetRelativePath(destinationDir, sourceFilePath);
                try
                {
                    var sw = Stopwatch.StartNew();
                    File.Copy(sourceFilePath, destinationFilePath);

                    if (sw.ElapsedMilliseconds % 10000 == 0)
                    {
                        progressCallback($"Copying {destinationFilePath}...\n").Wait();
                    }

                    sw.Stop();
                    progressCallback($"{sw.ElapsedMilliseconds} - {destinationFilePath}\n").Wait();
                    files++;
                }
                catch (Exception ex)
                {
                    progressCallback($"Cannot copy file: {destinationFilePath}, {ex.Message}\n").Wait();
                }
            }

            files += CopyDirectoriesAndFiles(sourceDir, destinationDir, progressCallback);
        }

        return files;
    }
}
