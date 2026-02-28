using InfinityMercsApp.Data.Database;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services;

public class FactionLogoCacheService
{
    public const int DebugFactionId = 1199;

    private const string PackagedCacheRoot = "SVGCache";
    private readonly string _localCacheDirectory;
    private readonly string _localUnitCacheDirectory;

    public FactionLogoCacheService()
    {
        _localCacheDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache");
        _localUnitCacheDirectory = Path.Combine(_localCacheDirectory, "units");
    }

    public async Task<LogoCacheResult> CacheAllAsync(IEnumerable<FactionDto> factions, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var faction in factions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (faction.Id <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureFactionLogoAvailableAsync(faction.Id, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.CopiedFromPackage)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public async Task<LogoCacheResult> CacheFactionLogosFromRecordsAsync(
        IEnumerable<FactionRecord> factions,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var faction in factions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (faction.Id <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureFactionLogoAvailableAsync(faction.Id, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.CopiedFromPackage)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public async Task<LogoCacheResult> CacheUnitLogosAsync(
        int factionId,
        IEnumerable<ArmyResumeDto> units,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localUnitCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (unit.Id <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureUnitLogoAvailableAsync(factionId, unit.Id, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.CopiedFromPackage)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public async Task<LogoCacheResult> CacheUnitLogosFromRecordsAsync(
        int factionId,
        IEnumerable<ArmyResumeRecord> units,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localUnitCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (unit.UnitId <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureUnitLogoAvailableAsync(factionId, unit.UnitId, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.CopiedFromPackage)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public string GetCachedLogoPath(int factionId)
    {
        return Path.Combine(_localCacheDirectory, $"{factionId}.svg");
    }

    public string? TryGetCachedLogoPath(int factionId)
    {
        var path = GetCachedLogoPath(factionId);
        return File.Exists(path) ? path : null;
    }

    public string GetCachedUnitLogoPath(int factionId, int unitId)
    {
        return Path.Combine(_localUnitCacheDirectory, $"{factionId}-{unitId}.svg");
    }

    public string? TryGetCachedUnitLogoPath(int factionId, int unitId)
    {
        var path = GetCachedUnitLogoPath(factionId, unitId);
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

    private async Task<EnsureResult> EnsureFactionLogoAvailableAsync(int factionId, CancellationToken cancellationToken)
    {
        var localPath = GetCachedLogoPath(factionId);
        if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
        {
            return EnsureResult.Reused;
        }

        var packagedPath = $"{PackagedCacheRoot}/factions/{factionId}.svg";
        var copied = await TryCopyPackagedAssetAsync(packagedPath, localPath, cancellationToken);
        return copied ? EnsureResult.CopiedFromPackage : EnsureResult.MissingFromPackage;
    }

    private async Task<EnsureResult> EnsureUnitLogoAvailableAsync(int factionId, int unitId, CancellationToken cancellationToken)
    {
        var localPath = GetCachedUnitLogoPath(factionId, unitId);
        if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
        {
            return EnsureResult.Reused;
        }

        var packagedPath = $"{PackagedCacheRoot}/units/{factionId}-{unitId}.svg";
        var copied = await TryCopyPackagedAssetAsync(packagedPath, localPath, cancellationToken);
        return copied ? EnsureResult.CopiedFromPackage : EnsureResult.MissingFromPackage;
    }

    private static async Task<bool> TryCopyPackagedAssetAsync(string packagedPath, string localPath, CancellationToken cancellationToken)
    {
        var candidates = BuildCandidatePackagedPaths(packagedPath).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            try
            {
                var localDirectory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                await using var packageStream = await FileSystem.Current.OpenAppPackageFileAsync(candidate);
                await using var fileStream = File.Create(localPath);
                await packageStream.CopyToAsync(fileStream, cancellationToken);
                return true;
            }
            catch
            {
                // Try next candidate path.
            }
        }

        Console.Error.WriteLine($"SVG package copy failed for '{packagedPath}'.");
        return false;
    }

    private static IEnumerable<string> BuildCandidatePackagedPaths(string packagedPath)
    {
        var normalized = packagedPath.Replace('\\', '/').TrimStart('/');
        var lower = normalized.ToLowerInvariant();
        var backslash = normalized.Replace('/', '\\');
        var backslashLower = lower.Replace('/', '\\');

        yield return normalized;
        yield return lower;
        yield return backslash;
        yield return backslashLower;
    }

    private enum EnsureResult
    {
        Reused,
        CopiedFromPackage,
        MissingFromPackage
    }
}

public class LogoCacheResult
{
    public int TotalFactions { get; set; }

    public int Downloaded { get; set; }

    public int Failed { get; set; }

    public int MissingLogoUrl { get; set; }

    public int InvalidLogoUrl { get; set; }

    public int CachedReuse { get; set; }
}

public class LogoCacheDebugInfo
{
    public int FactionId { get; set; }

    public string? ExpectedLogoUrl { get; set; }

    public string LocalPath { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public long SizeBytes { get; set; }
}
