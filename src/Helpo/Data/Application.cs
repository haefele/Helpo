namespace Helpo.Data;

public class Application : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public List<Version> Versions { get; set; } = new();
}