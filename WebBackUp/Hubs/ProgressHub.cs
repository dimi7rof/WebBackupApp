using Microsoft.AspNetCore.SignalR;

namespace WebBackUp.Hubs;

internal class ProgressHub : Hub
{
    internal async Task SendProgress(string message)
    {
        await Clients.All.SendAsync("ReceiveProgress", message);
    }
}
