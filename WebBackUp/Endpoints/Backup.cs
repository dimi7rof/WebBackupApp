using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using WebBackUp.Hubs;
using WebBackUp.Models;
using WebBackUp.Utilities;

namespace WebBackUp.Endpoints;

internal static class Backup
{
    internal static async Task<IResult> Execute(PathData pathData, string setId, IHubContext<ProgressHub> hubContext)
    {
        Func<PathData, Func<string, Task>, string> executor = setId switch
        {
            "set1" or "set4" or "set5" or "set6" => Phone.Execute,
            "set2" => Hdd.Execute,
            "set3" => SdCard.Execute,
            _ => Hdd.Execute
        };

        await hubContext.Clients.All.SendAsync("ReceiveProgress", "\n");

        var result = executor(pathData, async (message) =>
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", message);
        });

        await hubContext.Clients.All.SendAsync("ReceiveProgress", result);

        return Results.Ok();
    }

    internal static IResult Save(PathData pathData, string setId)
    {
        var filePath = GetFilePath(setId);
        var jsonData = JsonSerializer.Serialize(pathData);
        File.WriteAllText(filePath, jsonData);

        var setName = GetSetName(setId);
        return Results.Json(new { Message = $"Input values saved successfully for {setName}." });
    }

    internal static IResult Load(string setId)
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
    }

    private static string GetFilePath(string setId) => Path.Combine("json", $"{setId}_paths.json");

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
