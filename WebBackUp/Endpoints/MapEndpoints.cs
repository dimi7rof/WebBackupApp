using System.Text.Json;

namespace WebBackUp.Endpoints;

internal static class Endpoints
{
    internal static void MapEndpoints(this WebApplication app)
    {
        app.MapPost("/execute/{setId}", Backup.Execute);

        app.MapPost("/save/{setId}", Backup.Save);

        app.MapGet("/load/{setId}", Backup.Load);
    }
}
