using WebBackUp.Endpoints;
using WebBackUp.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapEndpoints();
app.MapHub<ProgressHub>("/progressHub");

await app.RunAsync();
