using InfinityMercsApp.Data.Database;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services;

public class FactionLogoCacheService
{
    public const int DebugFactionId = 1199;
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
                if (faction.Id == DebugFactionId)
                {
                    Console.Error.WriteLine($"[SVG DEBUG] {DebugFactionId} skipped: missing logo URL.");
                }
                result.MissingLogoUrl++;
                continue;
            }

            if (!Uri.TryCreate(faction.Logo, UriKind.Absolute, out var logoUri))
            {
                if (faction.Id == DebugFactionId)
                {
                    Console.Error.WriteLine($"[SVG DEBUG] {DebugFactionId} skipped: invalid logo URL '{faction.Logo}'.");
                }
                result.InvalidLogoUrl++;
                continue;
            }

            try
            {
                if (faction.Id == DebugFactionId)
                {
                    Console.Error.WriteLine($"[SVG DEBUG] {DebugFactionId} download start: {logoUri}");
                }

                await using var logoStream = await _httpClient.GetStreamAsync(logoUri, cancellationToken);
                var localPath = GetCachedLogoPath(faction.Id);
                await using var fileStream = File.Create(localPath);
                await logoStream.CopyToAsync(fileStream, cancellationToken);
                result.Downloaded++;

                if (faction.Id == DebugFactionId)
                {
                    var fileInfo = new FileInfo(localPath);
                    Console.Error.WriteLine($"[SVG DEBUG] {DebugFactionId} download success: {localPath} ({fileInfo.Length} bytes)");
                }
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

    public LogoCacheDebugInfo GetDebugInfo(int factionId, string? expectedLogoUrl = null)
    {
        var localPath = GetCachedLogoPath(factionId);
        var exists = File.Exists(localPath);
        var bytes = exists ? new FileInfo(localPath).Length : 0;

        return new LogoCacheDebugInfo
        {
            FactionId = factionId,
            ExpectedLogoUrl = expectedLogoUrl,
            LocalPath = localPath,
            Exists = exists,
            SizeBytes = bytes
        };
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

public class LogoCacheDebugInfo
{
    public int FactionId { get; set; }

    public string? ExpectedLogoUrl { get; set; }

    public string LocalPath { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public long SizeBytes { get; set; }
}
