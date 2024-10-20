using System.Text.Json;
using WebBackUp.Models;

namespace WebBackUp.Handlers;

public static class LoadDataHadler
{
    internal static IResult LoadUserData(string id)
    {
        Console.WriteLine($"Loading UserData for id:'{id}' {DateTime.Now}");
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return Results.Ok(new UserData());
        }

        var userData = JsonSerializer.Deserialize<UserData>(File.ReadAllText(filePath));
        var result = Results.Ok(userData);
        return result;
    }

    private static string GetFilePath(string setId)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json", $"{setId}.json");
}
