using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FollowinBot;

public class BotService(ITelegramBotClient client, BotData botData, LiteContext context, FollowinParser parser) : IHostedService
{
    private Task? _task;
    private CancellationTokenSource? _cts;
    private HashSet<string> _skip = [];
    private readonly HashSet<NewsEntity> _cache = [];
    private readonly Random _random = new();

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
                if (update is null) continue;

                foreach (var entity in update.Reverse())
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    try
                    {
                        var hasSimilar = _cache.ToHashSet().Any(cached => IsSimilar(entity.Title, cached.Title, 0.7));
                        if (hasSimilar) continue;
                        if (_skip.Add(entity.Id) && _cache.Add(entity))
                        {
                            await client.SendMessage(botData.ChannelId, entity.ToString(),
                                linkPreviewOptions: new LinkPreviewOptions() { IsDisabled = true });
                            context.News.Insert(entity);
                            Log.ForContext<BotService>().Information("Добавлено в телеграм: {url}", $"{FollowinParser.BaseSiteUrl}/en/feed/{entity.Id}");
                        }
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
                    await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 4)), _cts.Token);
                }

                var now = DateTime.Now;
                foreach (var cache in _cache.ToHashSet())
                {
                    if (now >= cache.EndCache)
                    {
                        _cache.Remove(cache);
                    }
                }
            }
        }
    }
    
    private bool IsSimilar(string str1, string str2, double threshold)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2)) return false;

        // Улучшенная разбивка слов, учитывая знаки препинания
        var words1 = str1.Split([' ', ',', '.', '!', '?', ':', ';', '-', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 1) // Игнорируем однобуквенные слова
            .ToHashSet();
        
        var words2 = str2.Split([' ', ',', '.', '!', '?', ':', ';', '-', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 1)
            .ToHashSet();
        
        if (words1.Count == 0 || words2.Count == 0) return false;
        
        int commonWords = words1.Intersect(words2).Count();
        double similarity = (double)commonWords / Math.Max(words1.Count, words2.Count);
        
        return similarity >= threshold;
    }
}