using SkiaSharp;
using Svg.Skia;

namespace InfinityMercsApp.Services;

internal static class CompanyUnitLogoWorkflowService
{
    /// <summary>
    /// Resolves and decodes a unit logo picture, then returns it for the caller to assign/invalidate UI.
    /// </summary>
    public static async Task<SKPicture?> LoadSelectedUnitLogoAsync(
        string unitName,
        int unitId,
        int sourceFactionId,
        Func<Task<Stream?>> openBestStreamAsync)
    {
        try
        {
            Stream? stream = await openBestStreamAsync();
            if (stream is null)
            {
                return null;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                var picture = svg.Load(stream);
                if (picture is null)
                {
                    Console.Error.WriteLine($"ArmyFactionSelectionPage selected logo parse failed: unit='{unitName}', id={unitId}, faction={sourceFactionId}.");
                    return null;
                }

                var bounds = picture.CullRect;
                Console.WriteLine($"ArmyFactionSelectionPage selected logo loaded: unit='{unitName}', bounds=({bounds.Left},{bounds.Top},{bounds.Right},{bounds.Bottom}).");
                return picture;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitLogoAsync failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Picks the best available logo stream from cached then packaged candidates.
    /// </summary>
    public static async Task<Stream?> OpenBestUnitLogoStreamAsync(
        string unitName,
        int unitId,
        int sourceFactionId,
        IEnumerable<string?> cachedPathCandidates,
        IEnumerable<string?> packagedPathCandidates)
    {
        Console.WriteLine($"ArmyFactionSelectionPage logo resolve start: unit='{unitName}', id={unitId}, faction={sourceFactionId}.");
        foreach (var cachedPath in cachedPathCandidates)
        {
            if (string.IsNullOrWhiteSpace(cachedPath))
            {
                continue;
            }

            var exists = File.Exists(cachedPath);
            Console.WriteLine($"ArmyFactionSelectionPage logo cached candidate: '{cachedPath}', exists={exists}.");
            if (exists)
            {
                Console.WriteLine($"ArmyFactionSelectionPage logo using cached: '{cachedPath}'.");
                return File.OpenRead(cachedPath);
            }
        }

        foreach (var packagedPath in packagedPathCandidates)
        {
            if (string.IsNullOrWhiteSpace(packagedPath))
            {
                continue;
            }

            try
            {
                var stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
                Console.WriteLine($"ArmyFactionSelectionPage logo using packaged: '{packagedPath}'.");
                return stream;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage logo packaged candidate failed: '{packagedPath}': {ex.Message}");
            }
        }

        Console.Error.WriteLine($"ArmyFactionSelectionPage logo resolve failed: unit='{unitName}', id={unitId}, faction={sourceFactionId}.");
        return null;
    }
}

