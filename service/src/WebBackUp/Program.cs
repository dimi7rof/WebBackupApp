using System.Runtime.Versioning;
using WebBackUp.Endpoints;
using WebBackUp.Hubs;
using WebBackUp.Services;

[SupportedOSPlatform("Windows")]
internal class Program
{
    private static async Task Main(string[] args)
    {
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
        builder.Services.AddTransient<ISmartPhoneService, SmartPhoneService>();
        builder.Services.AddTransient<IHardDiskService, HardDiskService>();
        builder.Services.AddTransient<ISdCardService, SdCardService>();

        var app = builder.Build();

        app.UseCors("AllowAngularApp");

        app.Map();

        app.MapHub<ProgressHub>("/progressHub");

        await app.RunAsync();
    }
}