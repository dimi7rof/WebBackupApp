using Microsoft.AspNetCore.SignalR;

namespace WebBackUp.Hubs;

public interface IBackupProgress
{
    Task ReceiveProgress(string message);
}

internal class ProgressHub : Hub<IBackupProgress> { }
