using Flurl.Http;
using ParserExtension;
using Serilog;

namespace FollowinBot;

public class FollowinParser(CookieJar cookie)
{
    public const string SiteUrl = "https://followin.io/en/news";

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
        
        Log.ForContext<BotService>().Information("Получение новостей...");
        
        var news = new List<NewsEntity>();
        var xPaths =
            parse.GetXPaths(
                "//div[@class='infinite-scroll-component__outerdiv']/div/div/div");
        foreach (var root in xPaths)
        {
            try
            {
                var aXpath = $"{root}//a[@target='_self']";
                var url = parse.GetAttributeValue(aXpath);
                if (string.IsNullOrEmpty(url)) continue;
                var id = url.Split("/").Last();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (skip.Contains(id)) continue;

                var title = parse.GetInnerText(aXpath);
                var text = parse.GetInnerText(
                    $"{root}//div[contains(@class, 'text-14') and contains(@class, 'cursor-pointer')]");

                news.Add(new NewsEntity()
                {
                    Id = id,
                    Title = title,
                    Text = text,
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