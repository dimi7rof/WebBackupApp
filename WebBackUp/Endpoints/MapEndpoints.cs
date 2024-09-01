using System.Text.Json;

namespace WebBackUp.Endpoints;

internal static class Endpoints
{
    internal static void MapEndpoints(this WebApplication app)
    {
        app.MapPost("/execute/{setId}", Backup.Execute);

        app.MapPost("/save/{setId}", (PathData pathData, string setId) =>
        {
            var filePath = GetFilePath(setId);
            var jsonData = JsonSerializer.Serialize(pathData);
            File.WriteAllText(filePath, jsonData);

            var setName = GetSetName(setId);
            return Results.Json(new { Message = $"Input values saved successfully for {setName}." });
        });

        app.MapGet("/load/{setId}", (string setId) =>
        {
            var filePath = GetFilePath(setId);
            if (!File.Exists(filePath))
            {
                return Results.Json(new { SourcePaths = new List<string>(), DestinationPaths = new List<string>() });
            }

            var jsonData = File.ReadAllText(filePath);
            var pathData = JsonSerializer.Deserialize<PathData>(jsonData);

            return Results.Json(new
            {
                SourcePaths = pathData?.SourcePaths ?? [],
                DestinationPaths = pathData?.DestinationPaths ?? []
            });
        });
    }

    private static string GetFilePath(string setId) => $"{setId}_paths.json";

    private static string GetSetName(string setId) => setId switch
    {
        "set1" => "Phone",
        "set2" => "HDD",
        "set3" => "SD Card",
        "set4" => "Phone 2",
        "set5" => "Phone 3",
        "set6" => "Phone 4",
        "set7" => "HDD 2",
        "set8" => "HDD 3",
        "set9" => "HDD 4",
        _ => "Unknown"
    };
}
