namespace InfinityMercsApp.Services;

public interface IWikiDescriptionService
{
    Task<IReadOnlyList<WikiContentBlock>> FetchContentAsync(
        string url,
        string? section = null,
        IReadOnlyList<string>? boxClasses = null,
        CancellationToken cancellationToken = default);
}
