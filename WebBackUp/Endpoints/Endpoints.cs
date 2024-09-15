using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using WebBackUp.Hubs;
using WebBackUp.Models;
using WebBackUp.Utilities;

namespace WebBackUp.Endpoints;

internal static class Endpoints
{
    internal static async Task<IResult> Execute(PathData pathData, string setId,
        IHubContext<ProgressHub> hubContext, [FromServices] ILogger<Program> logger)
    {
        Func<PathData, Func<string, Task>, ILogger<Program>, string> executor = setId switch
        {
            "set1" or "set4" or "set5" or "set6" => Phone.Execute,
            "set3" => SdCard.Execute,
            _ => Hdd.Execute
        };

        await hubContext.Clients.All.SendAsync("ReceiveProgress", "");

        var result = executor(pathData, async (message) =>
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", message);
        }, logger);

        await hubContext.Clients.All.SendAsync("ReceiveProgress", result);

        return Results.Ok();
    }

    internal static IResult Save(PathData pathData, string setId)
    {
        var jsonData = JsonSerializer.Serialize(new PathData()
        {
            SourcePaths = pathData.SourcePaths.Where(s => !string.IsNullOrEmpty(s)).ToList(),
            DestinationPaths = pathData.DestinationPaths.Where(s => !string.IsNullOrEmpty(s)).ToList()
        });

        File.WriteAllText(GetFilePath(setId), jsonData);

        return Results.Json(new { Message = $"Input values saved successfully for {GetSetName(setId)}." });
    }

    internal static IResult Load(string setId)
    {
        var filePath = GetFilePath(setId);
        if (!File.Exists(filePath))
        {
            return Results.Json(new { SourcePaths = new List<string>(), DestinationPaths = new List<string>() });
        }

        var pathData = JsonSerializer.Deserialize<PathData>(File.ReadAllText(filePath));

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
