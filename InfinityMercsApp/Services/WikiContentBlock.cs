namespace InfinityMercsApp.Services;

public enum WikiBlockType { SectionHeader, Paragraph, BulletItem }

public sealed class WikiContentBlock
{
    public WikiBlockType Type { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool Bold { get; init; }
    public int IndentLevel { get; init; }
}
