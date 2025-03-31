using System.Text;
using LiteDB;

namespace FollowinBot;

public class NewsEntity
{
    public required string Id { get; init; }
    [BsonIgnore] public required string? Title { get; init; }
    [BsonIgnore] public required string? Text { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Title);
        sb.AppendLine();
        sb.AppendLine(Text);
        return sb.ToString();
    }
}