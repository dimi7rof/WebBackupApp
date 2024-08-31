using Microsoft.AspNetCore.SignalR;

namespace WebBackUp;

public class ProgressHub : Hub
{
    public async Task SendProgress(string message)
    {
        await Clients.All.SendAsync("ReceiveProgress", message);
    }
}
