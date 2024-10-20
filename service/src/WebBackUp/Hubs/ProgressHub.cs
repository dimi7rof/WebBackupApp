using Microsoft.AspNetCore.SignalR;

namespace WebBackUp.Hubs;

public interface IBackupProgress
{
    Task ReceiveProgress(string message);
}

public class ProgressHub : Hub<IBackupProgress> { }
