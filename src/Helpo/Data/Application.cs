namespace Helpo.Data;

public class Application : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public List<string> Versions { get; set; } = new();
}