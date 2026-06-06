namespace InfinityMercsApp.Services;

public interface IWikiDescriptionService
{
    Task<IReadOnlyList<WikiContentBlock>> FetchContentAsync(
        string url,
        string? section = null,
        CancellationToken cancellationToken = default);
}
