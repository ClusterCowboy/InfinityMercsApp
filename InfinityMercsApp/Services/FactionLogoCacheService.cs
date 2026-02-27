using InfinityMercsApp.Data.Database;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services;

public class FactionLogoCacheService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public FactionLogoCacheService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _cacheDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache");
    }

    public async Task<LogoCacheResult> CacheAllAsync(IEnumerable<FactionDto> factions, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_cacheDirectory);
        var result = new LogoCacheResult();

        foreach (var faction in factions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (faction.Id <= 0 || string.IsNullOrWhiteSpace(faction.Logo))
            {
                result.MissingLogoUrl++;
                continue;
            }

            if (!Uri.TryCreate(faction.Logo, UriKind.Absolute, out var logoUri))
            {
                result.InvalidLogoUrl++;
                continue;
            }

            try
            {
                await using var logoStream = await _httpClient.GetStreamAsync(logoUri, cancellationToken);
                var localPath = GetCachedLogoPath(faction.Id);
                await using var fileStream = File.Create(localPath);
                await logoStream.CopyToAsync(fileStream, cancellationToken);
                result.Downloaded++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Faction logo cache failed for {faction.Id} ({faction.Logo}): {ex.Message}");
                result.Failed++;
            }
        }

        return result;
    }

    public string GetCachedLogoPath(int factionId)
    {
        return Path.Combine(_cacheDirectory, $"{factionId}.svg");
    }

    public string? TryGetCachedLogoPath(int factionId)
    {
        var path = GetCachedLogoPath(factionId);
        return File.Exists(path) ? path : null;
    }
}

public class LogoCacheResult
{
    public int TotalFactions { get; set; }

    public int Downloaded { get; set; }

    public int Failed { get; set; }

    public int MissingLogoUrl { get; set; }

    public int InvalidLogoUrl { get; set; }
}
