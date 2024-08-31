using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using WebBackUp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSignalR();

var app = builder.Build();

// Function to generate file path based on set identifier
string GetFilePath(string setId) => $"{setId}_paths.json";

// Helper to get the human-readable name of the set
string GetSetName(string setId) => setId switch
{
    "set1" => "Phone",
    "set2" => "HDD",
    "set3" => "SD Card",
    _ => "Unknown"
};

// Configure SignalR
app.MapHub<ProgressHub>("/progressHub");

app.UseDefaultFiles();
app.UseStaticFiles();

// API endpoint for executing logic
app.MapPost("/execute/{setId}", async (PathData pathData, string setId, IHubContext<ProgressHub> hubContext) =>
{
    if (setId == "set1")
    {
        _ = Task.Run(async () =>
        {
            await hubContext.Clients.All.SendAsync("ReceiveProgress", "Downloading photos...\n");

            var result = Phone.Execute(pathData, async (message) =>
            {
                await hubContext.Clients.All.SendAsync("ReceiveProgress", message);
            });

            await hubContext.Clients.All.SendAsync("ReceiveProgress", result);
        });
    }
    return Results.Ok();
});


// API endpoint for saving inputs to a file
app.MapPost("/save/{setId}", (PathData pathData, string setId) =>
{
    var filePath = GetFilePath(setId);
    var jsonData = JsonSerializer.Serialize(pathData);
    File.WriteAllText(filePath, jsonData);

    var setName = GetSetName(setId);
    return Results.Json(new { Message = $"Input values saved successfully for {setName}." });
});

// API endpoint for loading inputs from a file
app.MapGet("/load/{setId}", (string setId) =>
{
    var filePath = GetFilePath(setId);
    if (!File.Exists(filePath))
    {
        return Results.Json(new { SourcePaths = new List<string>(), DestinationPaths = new List<string>() });
    }

    var jsonData = File.ReadAllText(filePath);
    var pathData = JsonSerializer.Deserialize<PathData>(jsonData);

    return Results.Json(new
    {
        SourcePaths = pathData?.SourcePaths ?? new List<string>(),
        DestinationPaths = pathData?.DestinationPaths ?? new List<string>()
    });
});

app.Run();
