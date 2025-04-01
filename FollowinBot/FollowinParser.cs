using Flurl.Http;
using ParserExtension;
using Serilog;

namespace FollowinBot;

public class FollowinParser(CookieJar cookie)
{
    public const string BaseSiteUrl = "https://followin.io";
    public const string SiteUrl = $"{BaseSiteUrl}/en/news";

    private static readonly Dictionary<string, string> Headers = new()
    {
        {
            "user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36"
        },
        { "sec-ch-ua", """ "Chromium";v="134", "Not:A-Brand";v="24", "Google Chrome";v="134" """ },
        { "sec-ch-ua-mobile", "?0" },
        { "sec-ch-ua-platform", """ "Windows" """ },
        { "sec-fetch-dest", "document" },
        { "sec-fetch-mode", "navigate" },
        { "sec-fetch-site", "same-origin" },
        { "sec-fetch-user", "?1" },
        { "upgrade-insecure-requests", "1" }
    };

    public async Task<IEnumerable<NewsEntity>?> GetNews(HashSet<string> skip)
    {
        var parse = await SiteUrl
            .WithCookies(cookie)
            .WithHeaders(Headers)
            .GetStringAsync()
            .GetParse();
        if (parse is null) return null;
        
        Log.ForContext<BotService>().Information("Обновление новостной ленты...");
        var now = DateTime.Now;
        var news = new List<NewsEntity>();
        var newsItems =
            parse.GetXPaths(
                "//div[@class='infinite-scroll-component__outerdiv']/div/div/div");
        foreach (var item in newsItems)
        {
            try
            {
                var url = BaseSiteUrl + parse.GetAttributeValue($"{item}//a[@target='_self']");
                if (string.IsNullOrEmpty(url) || url.EndsWith(BaseSiteUrl)) continue;
                if (skip.Contains(url)) continue;
                var title = parse.GetInnerText($"{item}//a[@target='_self']");
                if (string.IsNullOrEmpty(title)) continue;
                news.Add(new NewsEntity 
                { 
                    Id = url, 
                    Title = title,
                    EndCache = now.AddMinutes(30)
                });
            }
            catch (Exception e)
            {
                Log.ForContext<FollowinParser>().Error(e, "foreach error paring");
            }
        }

        return news;
    }
}