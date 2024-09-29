namespace WebBackUp.Endpoints;

internal static class MapEndpoints
{
    internal static void Map(this WebApplication app)
    {
        app.MapGet("/loaduserdata/{id}", Endpoints.LoadUserData);
        app.MapPost("/saveuserdata/{id}", Endpoints.SaveUserData);

        app.MapPost("/execute/Phone", Utilities.Phone.Execute);
        app.MapPost("/execute/HDD", Utilities.Hdd.Execute);
        app.MapPost("/execute/SdCard", Utilities.SdCard.Execute);

    }
}
