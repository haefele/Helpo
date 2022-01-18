namespace Helpo.Data;

public class Article : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public List<ArticleContent> Contents { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class ArticleContent
{
    public Version ValidFromVersion { get; set; } = default!;
    public string Content { get; set; } = string.Empty;
}