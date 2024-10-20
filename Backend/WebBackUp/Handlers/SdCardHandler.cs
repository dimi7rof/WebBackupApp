using Microsoft.AspNetCore.Mvc;
using WebBackUp.Models;
using WebBackUp.Services;

namespace WebBackUp.Handlers;

public class SdCardHandler
{
    public static async Task TransferFiles(ISdCardService sdCardService, [FromBody] UserData userData)
    {
        await sdCardService.Execute(userData);
    }
}
