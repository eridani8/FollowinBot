using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FollowinBot;

public class BotService(ITelegramBotClient client, LiteContext context, FollowinParser parser) : IHostedService
{
    private Task? _task;
    private CancellationTokenSource? _cts;
    private HashSet<string> _skip = [];
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _skip = context.News.FindAll().Select(n => n.Id).ToHashSet();
        var me = await client.GetMe(cancellationToken);
        Log.Information("Bot @{username} started", me.Username);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _task = Worker();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_cts != null)
            {
                await _cts.CancelAsync();
            }
            if (_task != null)
            {
                await Task.WhenAny(_task, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            context.Database.Commit();
        }
        finally
        {
            Log.ForContext<BotService>().Information("Bot service stopped");
            _cts?.Dispose();
            _task?.Dispose();
        }
    }

    private async Task Worker()
    {
        if (_cts?.Token == null)
        {
            Log.ForContext<BotService>().Error("Bot service can not be started");
            return;
        }
        await Task.Delay(3000, _cts.Token);
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var update = await parser.GetNews(_skip);
                if (update is null)
                {
                    continue;
                }
                foreach (var entity in update)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    try
                    {
                        await client.SendMessage(new ChatId(-1002615890333), entity.ToString());
                        _skip.Add(entity.Id);
                        context.News.Insert(entity);
                    }
                    catch (Exception e)
                    {
                        Log.ForContext<FollowinParser>().Error(e, "foreach error sending");
                    }
                }
            }
            catch (Exception e)
            {
                Log.ForContext<BotService>().Error(e, "Exception while executing bot service");
            }
            finally
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cts.Token);
                }
            }
        }
    }
}