using WebBackUp.Handlers;

namespace WebBackUp.Endpoints;

internal static class MapEndpoints
{
    internal static void Map(this WebApplication app)
    {
        app.MapGet("/loaduserdata/{id}", LoadDataHadler.LoadUserData);
        app.MapPost("/saveuserdata/{id}", SaveDataHandler.SaveUserData);

        app.MapPost("/execute/Phone", SmartPhoneHandler.TransferFiles);
        app.MapPost("/execute/HDD", HardDiskHandler.TransferFiles);
        app.MapPost("/execute/SdCard", SdCardHandler.TransferFiles);

    }
}
