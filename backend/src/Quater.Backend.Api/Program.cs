using Quater.Backend.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog to read from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();
