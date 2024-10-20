using Microsoft.AspNetCore.Mvc;
using WebBackUp.Models;
using WebBackUp.Services;

namespace WebBackUp.Handlers;

public class HardDiskHandler
{
    public static async Task TransferFiles(IHardDiskService hardDiskService, [FromBody] UserData userData)
    {
        await hardDiskService.Execute(userData);
    }
}
