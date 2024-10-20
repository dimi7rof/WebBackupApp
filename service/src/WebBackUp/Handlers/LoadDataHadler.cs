using System.Text.Json;
using WebBackUp.Models;
using static WebBackUp.Utilities.FileHelper;

namespace WebBackUp.Handlers;

public static class LoadDataHadler
{
    internal static IResult LoadUserData(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return Results.Ok(new UserData());
        }

        var userData = JsonSerializer.Deserialize<UserData>(File.ReadAllText(filePath));

        return Results.Ok(userData);
    }
}
