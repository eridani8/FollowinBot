using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using DotNetEnv;
using Flurl.Http;
using FollowinBot;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

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
    .WriteTo.File($"{logsPath}/.log", rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate)
    .CreateLogger();

try
{
    #region env

    Env.Load();
    
    var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
    var channelIdStr = Environment.GetEnvironmentVariable("CHANNEL_ID");

    if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(channelIdStr) || !long.TryParse(channelIdStr, out var channelId))
    {
        throw new Exception("Need to install environment variables in .env");
    }
    
    #endregion
    
    var builder = Host.CreateApplicationBuilder();
    
    builder.Services.AddSerilog();
    builder.Services.AddSingleton<BotData>(_ => new BotData()
    {
        ChannelId = channelId,
    });
    builder.Services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ => new TelegramBotClient(botToken));
    builder.Services.AddSingleton<LiteContext>();
    builder.Services.AddSingleton<CookieJar>();
    builder.Services.AddSingleton<FollowinParser>();
    builder.Services.AddHostedService<BotService>();
    
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