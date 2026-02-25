using System.Globalization;
using System.Text.Json;
using System.Windows.Input;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Data.WebAccess;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebAccessObject? _webAccessObject;
    private readonly IArmyDataAccessor? _armyDataAccessor;
    private readonly IMetadataAccessor? _metadataAccessor;
    private int _count;
    private string _metadataStatus = "Metadata file not downloaded yet.";
    private string _armyStatus = "Army file not downloaded yet.";
    private string _updateStatus = "No update run yet.";
    private string _factionIdInput = "1";

    public MainViewModel(
        IWebAccessObject? webAccessObject = null,
        IArmyDataAccessor? armyDataAccessor = null,
        IMetadataAccessor? metadataAccessor = null)
    {
        _webAccessObject = webAccessObject;
        _armyDataAccessor = armyDataAccessor;
        _metadataAccessor = metadataAccessor;
        IncrementCounterCommand = new Command(OnIncrementCounter);
        DownloadMetadataCommand = new Command(async () => await DownloadMetadataAsync());
        DownloadArmyDataCommand = new Command(async () => await DownloadArmyDataAsync());
        UpdateAllDataCommand = new Command(async () => await UpdateAllDataAsync());
    }

    public string Greeting => "Hello, World!";

    public string Subtitle => "Welcome to\n.NET Multi-platform App UI";

    public string CounterText => _count == 1 ? "Clicked 1 time" : $"Clicked {_count} times";

    public string MetadataStatus
    {
        get => _metadataStatus;
        private set
        {
            if (_metadataStatus == value)
            {
                return;
            }

            _metadataStatus = value;
            OnPropertyChanged();
        }
    }

    public string ArmyStatus
    {
        get => _armyStatus;
        private set
        {
            if (_armyStatus == value)
            {
                return;
            }

            _armyStatus = value;
            OnPropertyChanged();
        }
    }

    public string FactionIdInput
    {
        get => _factionIdInput;
        set
        {
            if (_factionIdInput == value)
            {
                return;
            }

            _factionIdInput = value;
            OnPropertyChanged();
        }
    }

    public string UpdateStatus
    {
        get => _updateStatus;
        private set
        {
            if (_updateStatus == value)
            {
                return;
            }

            _updateStatus = value;
            OnPropertyChanged();
        }
    }

    public ICommand IncrementCounterCommand { get; }

    public ICommand DownloadMetadataCommand { get; }

    public ICommand DownloadArmyDataCommand { get; }

    public ICommand UpdateAllDataCommand { get; }

    private void OnIncrementCounter()
    {
        _count++;
        OnPropertyChanged(nameof(CounterText));
    }

    private async Task DownloadMetadataAsync()
    {
        if (_webAccessObject is null)
        {
            MetadataStatus = "Web API service is not available.";
            return;
        }

        try
        {
            MetadataStatus = "Downloading metadata...";

            var metadataJson = await _webAccessObject.GetMetaDataAsync();
            var filePath = Path.Combine(FileSystem.Current.AppDataDirectory, "metadata.json");

            await File.WriteAllTextAsync(filePath, metadataJson);

            MetadataStatus = $"Saved metadata to: {filePath}";
        }
        catch (Exception ex)
        {
            MetadataStatus = $"Download failed: {ex.Message}";
        }
    }

    private async Task UpdateAllDataAsync()
    {
        if (_webAccessObject is null || _metadataAccessor is null || _armyDataAccessor is null)
        {
            UpdateStatus = "Required services are not available.";
            return;
        }

        try
        {
            UpdateStatus = "Downloading metadata...";

            var metadataJson = await _webAccessObject.GetMetaDataAsync();
            var metadataPath = Path.Combine(FileSystem.Current.AppDataDirectory, "metadata.json");
            await File.WriteAllTextAsync(metadataPath, metadataJson);
            await _metadataAccessor.ImportFromJsonAsync(metadataJson);

            var metadataDocument = JsonSerializer.Deserialize<MetadataDocument>(metadataJson, JsonOptions);
            if (metadataDocument is null || metadataDocument.Factions.Count == 0)
            {
                UpdateStatus = "Metadata download succeeded but no factions were found.";
                return;
            }

            var factionIds = metadataDocument.Factions
                .Select(f => f.Id)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var updatedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            foreach (var factionId in factionIds)
            {
                try
                {
                    UpdateStatus = $"Checking faction {factionId}...";

                    var latestArmyJson = await _webAccessObject.GetArmyDataAsync(factionId);
                    var latestArmy = JsonSerializer.Deserialize<ArmyDocument>(latestArmyJson, JsonOptions);
                    var latestVersion = latestArmy?.Version;

                    if (string.IsNullOrWhiteSpace(latestVersion))
                    {
                        skippedCount++;
                        continue;
                    }

                    var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(factionId);
                    var storedVersion = snapshot?.Version;

                    if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    await _armyDataAccessor.ImportFactionArmyFromJsonAsync(factionId, latestArmyJson);
                    updatedCount++;
                }
                catch
                {
                    errorCount++;
                }
            }

            UpdateStatus = $"Update complete. Updated: {updatedCount}, Unchanged: {skippedCount}, Errors: {errorCount}.";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update failed: {ex.Message}";
        }
    }

    private static int CompareVersions(string left, string right)
    {
        var leftParts = left.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var rightParts = right.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var maxLength = Math.Max(leftParts.Length, rightParts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var leftPart = i < leftParts.Length && int.TryParse(leftParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : 0;
            var rightPart = i < rightParts.Length && int.TryParse(rightParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : 0;

            if (leftPart > rightPart)
            {
                return 1;
            }

            if (leftPart < rightPart)
            {
                return -1;
            }
        }

        return 0;
    }

    private async Task DownloadArmyDataAsync()
    {
        if (_webAccessObject is null)
        {
            ArmyStatus = "Web API service is not available.";
            return;
        }

        if (!int.TryParse(FactionIdInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var factionId) || factionId <= 0)
        {
            ArmyStatus = "Enter a valid positive faction ID.";
            return;
        }

        try
        {
            ArmyStatus = $"Downloading army data for faction {factionId}...";

            var armyJson = await _webAccessObject.GetArmyDataAsync(factionId);
            var filePath = Path.Combine(FileSystem.Current.AppDataDirectory, $"army-{factionId}.json");

            await File.WriteAllTextAsync(filePath, armyJson);

            if (_armyDataAccessor is not null)
            {
                await _armyDataAccessor.ImportFactionArmyFromJsonAsync(factionId, armyJson);
                ArmyStatus = $"Saved and imported faction {factionId} to DB. File: {filePath}";
            }
            else
            {
                ArmyStatus = $"Saved army data to: {filePath}";
            }
        }
        catch (Exception ex)
        {
            ArmyStatus = $"Download failed: {ex.Message}";
        }
    }
}
