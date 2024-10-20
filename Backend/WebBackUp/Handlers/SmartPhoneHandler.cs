using Microsoft.AspNetCore.Mvc;
using WebBackUp.Models;
using WebBackUp.Services;

namespace WebBackUp.Handlers;

public static class SmartPhoneHandler
{
    public static async Task TransferFiles(ISmartPhoneService smartPhoneService, [FromBody] UserData userData)
    {
        await smartPhoneService.Execute(userData);
    }
}
