using LiteDB;

namespace FollowinBot;

public class Context
{
    public ILiteCollection<NewsEntity> News { get; }
    
    public Context()
    {
        var db = new LiteDatabase("data.db");
        News = db.GetCollection<NewsEntity>("news");
    }
}

public class NewsEntity
{
    public required string Title { get; set; }
    public required string Text { get; set; }
}