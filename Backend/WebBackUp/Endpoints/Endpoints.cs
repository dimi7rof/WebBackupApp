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
            Console.WriteLine($"UserData loaded {DateTime.Now}");
            return Results.Ok(new UserData()
            {
                Phone = new PhoneData()
                {
                    Paths = new PathData()
                    {
                        SourcePaths =
                                [
                                    "Camera",
                                    "Teo0",
                                    "Teo1",
                                    "TeOther",
                                    "TeoVideo",
                                    "Photoshop Express"
                                ],
                        DestinationPaths =
                                [
                                    "D:\\Android\\Mi14\\Camera",
                                    "D:\\Android\\Mi14\\Teo0",
                                    "D:\\Android\\Mi14\\Teo1",
                                    "D:\\Android\\Mi14\\TeOther",
                                    "D:\\Android\\Mi14\\TeoVideo",
                                    "D:\\Android\\Common\\Photoshop_Express"
                                ]
                    }
                },
                HDD = new HddData()
                {
                    Paths = new PathData()
                    {
                        SourcePaths =
                                [
                                    "D:\\Android\\Mi14",
                                    "D:\\Pictures\\Snimki\\Razhodki"
                                ],
                        DestinationPaths =
                                [
                                    "F:\\Snimki\\Mi14",
                                    "F:\\Snimki\\Razhodki"
                                ]
                    }
                },
                SD = new SdData()
                {
                    Paths = new PathData()
                    {
                        SourcePaths =
                                [
                                    "D:\\Music\\Skoda"
                                ],
                        DestinationPaths =
                                [
                                    "F:\\Skoda"
                                ]
                    }
                }
            });
        }

        var userData = JsonSerializer.Deserialize<UserData>(File.ReadAllText(filePath));
        var result = Results.Ok(userData);
        return result;
    }

    private static string GetFilePath(string setId) => Path.Combine("json", $"{setId}.json");
}


