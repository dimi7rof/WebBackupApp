using Microsoft.AspNetCore.SignalR;

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
            _ => Hdd.Execute // Default case
        };

        await hubContext.Clients.All.SendAsync("ReceiveProgress", "\n");

        var result = executor(pathData, async (message) =>
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", message);
        });

        await hubContext.Clients.All.SendAsync("ReceiveProgress", result);

        return Results.Ok();
    }
}
