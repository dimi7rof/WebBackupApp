using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebBackUp.Models;

namespace WebBackUp.Handlers;

public static class SaveDataHandler
{
    internal static IResult SaveUserData([FromBody] UserData userData, [FromRoute] string id)
    {
        var filteredData = new UserData()
        {
            SD = new SdData()
            {
                Sync = userData.SD.Sync,
                DeviceLetter = userData.SD.DeviceLetter,
                Paths = userData.SD.Paths.FilterPaths()
            },
            HDD = new HddData()
            {
                DeviceLetter = userData.HDD.DeviceLetter,
                Paths = userData.HDD.Paths.FilterPaths()
            },
            Phone = new PhoneData()
            {
                Paths = userData.Phone.Paths.FilterPaths()
            }
        };

        var jsonData = JsonSerializer.Serialize(filteredData);

        File.WriteAllText(GetFilePath(id), jsonData);

        return Results.Json(new { Message = $"Input values saved successfully." });
    }

    private static string GetFilePath(string setId)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json", $"{setId}.json");

    private static PathData FilterPaths(this PathData paths)
        => new()
        {
            SourcePaths = paths.SourcePaths.RemoveEmptyPaths(),
            DestinationPaths = paths.DestinationPaths.RemoveEmptyPaths(),
        };

    private static List<string> RemoveEmptyPaths(this List<string> paths)
        => paths.Where(x => !string.IsNullOrEmpty(x)).ToList();
}
