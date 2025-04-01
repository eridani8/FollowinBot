using System.Text;
using LiteDB;

namespace FollowinBot;

public class NewsEntity
{
    [BsonId] public required string Id { get; init; }
    [BsonIgnore] public required string Title { get; init; }
    public DateTime EndCache { get; set; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Title);
        sb.AppendLine(Id);
        return sb.ToString();
    }
}