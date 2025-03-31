using LiteDB;

namespace FollowinBot;

public class LiteContext
{
    public LiteDatabase Database { get; }
    public ILiteCollection<NewsEntity> News { get; }
    
    public LiteContext()
    {
        Database = new LiteDatabase("data.db");
        News = Database.GetCollection<NewsEntity>("news");
    }
}