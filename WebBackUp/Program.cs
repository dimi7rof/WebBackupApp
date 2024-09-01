using WebBackUp;
using WebBackUp.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapEndpoints();
app.MapHub<ProgressHub>("/progressHub");

await app.RunAsync();
