using WebBackUp.Endpoints;
using WebBackUp.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Map();
app.MapHub<ProgressHub>("/progressHub");

await app.RunAsync();
