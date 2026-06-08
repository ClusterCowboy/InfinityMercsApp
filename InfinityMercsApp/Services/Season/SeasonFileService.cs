using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Services.Season;

internal static class SeasonFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static async Task<string> CreateSeasonFileAsync(string companyName, string companyIdentifier, string companyFilePath)
    {
        var now = DateTimeOffset.UtcNow;
        var seasonsDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords", "Seasons");
        Directory.CreateDirectory(seasonsDir);

        var timestamp = now.ToString("yyyyMMddHHmmss");
        var safeName = companyName.Replace(' ', '_');
        var fileName = $"{safeName}-{timestamp}.json";
        var filePath = Path.Combine(seasonsDir, fileName);

        var seasonFile = new SeasonFile
        {
            CompanyName = companyName,
            CompanyIdentifier = companyIdentifier,
            CompanyFilePath = companyFilePath,
            CreatedDate = now.ToString("yyyy-MM-dd"),
            InitialPurchases = new SeasonMarketplace(),
            Rounds = []
        };

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(seasonFile, WriteOptions));
        return filePath;
    }

    internal static async Task<SeasonFile?> LoadSeasonFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<SeasonFile>(json, ReadOptions);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonFileService failed to load season file '{filePath}': {ex.Message}");
            return null;
        }
    }

    internal static int ResolveCurrentRound(SeasonFile? seasonFile)
    {
        var rounds = seasonFile?.Rounds ?? [];
        if (rounds.Count == 0)
        {
            return 0;
        }

        var highestRoundIndex = rounds.Max(round => Math.Max(0, round.RoundIndex));
        return highestRoundIndex > 0 ? highestRoundIndex : rounds.Count;
    }
}
