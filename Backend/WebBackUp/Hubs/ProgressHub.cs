using Microsoft.AspNetCore.SignalR;

namespace WebBackUp.Hubs;

internal class ProgressHub() : Hub
{
    internal async Task SendProgressUpdate(string message)
    {
        await Clients.All.SendAsync("ReceiveProgress", message);
    }
}
