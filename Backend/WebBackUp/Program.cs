using WebBackUp.Endpoints;
using WebBackUp.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder => builder.WithOrigins("http://localhost:4200")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
});


builder.Services.AddLogging();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors("AllowAngularApp");

app.UseDefaultFiles();
app.UseStaticFiles();

app.Map();

app.MapHub<ProgressHub>("/progressHub");

await app.RunAsync();
