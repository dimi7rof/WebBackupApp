using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebBackUp.Models;

namespace WebBackUp.Endpoints;

internal static class Endpoints
{
    internal static IResult SaveUserData([FromBody] UserData userData, [FromRoute] string id)
    {
        var jsonData = JsonSerializer.Serialize(userData);

        File.WriteAllText(GetFilePath(id), jsonData);

        return Results.Json(new { Message = $"Input values saved successfully." });
    }

    internal static IResult LoadUserData(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return Results.Ok(new UserData());
        }

        Console.WriteLine($"UserData loaded {DateTime.Now}");
        var userData = JsonSerializer.Deserialize<UserData>(File.ReadAllText(filePath));
        var result = Results.Ok(userData);
        return result;
    }

    private static string GetFilePath(string setId) => Path.Combine("json", $"{setId}.json");
}


