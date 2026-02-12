namespace FactorySnap.Shared.Contracts;

public class OpcNodeDto
{
    public int Id { get; init; }
    public string NodeId { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int SortOrder { get; init; }
}

public class OpcConfigDto
{
    public string Url { get; set; } = string.Empty;
    public List<OpcNodeDto> Nodes { get; init; } = [];
}