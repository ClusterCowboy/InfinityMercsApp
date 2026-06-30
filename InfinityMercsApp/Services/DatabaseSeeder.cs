using System.IO.Compression;

namespace InfinityMercsApp.Services;

/// <summary>
/// Seeds the working database from a bundled, gzip-compressed snapshot on first launch.
/// Without this the first launch has to download every faction from the network before the
/// app is usable; with it the app starts fully populated (and works offline), and the daily
/// background sync only fetches deltas afterwards.
/// </summary>
internal static class DatabaseSeeder
{
    // Bundled MauiAsset (Resources/Raw/infinitymercs.db3.gz -> logical name below).
    private const string SeedAssetPath = "infinitymercs.db3.gz";

    /// <summary>
    /// Ensures <paramref name="targetDbPath"/> exists, decompressing the bundled seed into it on
    /// first run. No-ops when a database is already present (seeded earlier, or built up by sync).
    /// Best-effort: any failure falls back to the network import path rather than crashing startup.
    /// </summary>
    public static void EnsureSeeded(string targetDbPath)
    {
        try
        {
            if (File.Exists(targetDbPath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetDbPath)!);

            using var compressed = FileSystem.Current.OpenAppPackageFileAsync(SeedAssetPath)
                .GetAwaiter().GetResult();
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);

            // Decompress to a sidecar file first, then move into place, so a crash or kill
            // mid-decompress can't leave a half-written file that looks "already seeded".
            var stagingPath = targetDbPath + ".seeding";
            using (var output = File.Create(stagingPath))
            {
                gzip.CopyTo(output);
            }

            File.Move(stagingPath, targetDbPath, overwrite: true);
        }
        catch (Exception ex)
        {
            // Non-fatal: leave the DB absent so the normal network import seeds it instead.
            Console.Error.WriteLine($"DatabaseSeeder.EnsureSeeded failed: {ex.Message}");
            TryCleanupStaging(targetDbPath);
        }
    }

    private static void TryCleanupStaging(string targetDbPath)
    {
        try
        {
            var stagingPath = targetDbPath + ".seeding";
            if (File.Exists(stagingPath))
            {
                File.Delete(stagingPath);
            }
        }
        catch
        {
            // ignore
        }
    }
}
