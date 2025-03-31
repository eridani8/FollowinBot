using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

const string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
var logsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

if (!Directory.Exists(logsPath))
{
    Directory.CreateDirectory(logsPath);
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(LogEventLevel.Information, outputTemplate)
    .WriteTo.File($"{logsPath}/.log", rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate,
        restrictedToMinimumLevel: LogEventLevel.Error)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder();
    
    builder.Services.AddSerilog();
    
    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "The application cannot be loaded");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
finally
{
    await Log.CloseAndFlushAsync();
}