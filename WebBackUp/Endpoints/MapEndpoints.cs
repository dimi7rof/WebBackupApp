using WebBackUp.Utilities;

namespace WebBackUp.Endpoints;

internal static class MapEndpoints
{
    internal static void Map(this WebApplication app)
    {
        app.MapPost("/execute/{setId}", Endpoints.Execute);

        app.MapPost("/save/{setId}", Endpoints.Save);

        app.MapGet("/load/{setId}", Endpoints.Load);
    }
}
