using System.Globalization;
using System.Windows.Input;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IInfinityArmyAPI? _infinityArmyApi;
    private readonly IFactionProvider? _factionProvider;
    private readonly IArmyImportProvider? _armyImportProvider;
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IImportService? _importService;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private int _count;
    private string _metadataStatus = "Metadata file not downloaded yet.";
    private string _armyStatus = "Army file not downloaded yet.";
    private string _updateStatus = "No update run yet.";
    private bool _isUpdateInProgress;
    private string _updateProgressMessage = string.Empty;
    private string _factionIdInput = "1";
    private bool _showUnitsInInches = true;

    public MainViewModel(
        IInfinityArmyAPI? infinityArmyApi = null,
        IFactionProvider? factionProvider = null,
        IArmyImportProvider? armyImportProvider = null,
        IMetadataProvider? metadataProvider = null,
        IImportService? importService = null,
        FactionLogoCacheService? factionLogoCacheService = null,
        IAppSettingsProvider? appSettingsProvider = null)
    {
        _infinityArmyApi = infinityArmyApi;
        _factionProvider = factionProvider;
        _armyImportProvider = armyImportProvider;
        _metadataProvider = metadataProvider;
        _importService = importService;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;
        IncrementCounterCommand = new Command(OnIncrementCounter);
        DownloadMetadataCommand = new Command(async () => await DownloadMetadataAsync());
        DownloadArmyDataCommand = new Command(async () => await DownloadArmyDataAsync());
        UpdateAllDataCommand = new Command(async () => await UpdateAllDataAsync());
        _ = LoadGlobalSettingsAsync();
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

    public bool IsUpdateInProgress
    {
        get => _isUpdateInProgress;
        private set
        {
            if (_isUpdateInProgress == value)
            {
                return;
            }

            _isUpdateInProgress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanForceUpdate));
        }
    }

    public bool CanForceUpdate => !IsUpdateInProgress;

    public string UpdateProgressMessage
    {
        get => _updateProgressMessage;
        private set
        {
            if (_updateProgressMessage == value)
            {
                return;
            }

            _updateProgressMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand IncrementCounterCommand { get; }

    public ICommand DownloadMetadataCommand { get; }

    public ICommand DownloadArmyDataCommand { get; }

    public ICommand UpdateAllDataCommand { get; }

    public bool ShowUnitsInInches
    {
        get => _showUnitsInInches;
        set
        {
            if (_showUnitsInInches == value)
            {
                return;
            }

            _showUnitsInInches = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsInCentimeters));
            _ = SaveDisplayUnitsSettingAsync(value);
        }
    }

    public bool ShowUnitsInCentimeters
    {
        get => !_showUnitsInInches;
        set
        {
            var targetInches = !value;
            if (_showUnitsInInches == targetInches)
            {
                return;
            }

            _showUnitsInInches = targetInches;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsInInches));
            _ = SaveDisplayUnitsSettingAsync(targetInches);
        }
    }

    private void OnIncrementCounter()
    {
        _count++;
        OnPropertyChanged(nameof(CounterText));
    }

    private async Task DownloadMetadataAsync()
    {
        if (_infinityArmyApi is null)
        {
            MetadataStatus = "Web API service is not available.";
            return;
        }

        try
        {
            MetadataStatus = "Downloading metadata...";

            var metadataDocument = await _infinityArmyApi.GetMetaDataAsync();
            if (metadataDocument is not null && _metadataProvider is not null)
            {
                _metadataProvider.Import(metadataDocument);
            }

            if (metadataDocument is null || metadataDocument.Factions.Count == 0)
            {
                MetadataStatus = "Metadata imported to DB. No factions found.";
                return;
            }

            if (_factionLogoCacheService is not null)
            {
                await _factionLogoCacheService.CacheAllAsync(metadataDocument.Factions);
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
                    MetadataStatus = $"Fetching faction data for {factionId}...";
                    var latestArmy = await _infinityArmyApi.GetArmyDataAsync(factionId);
                    var latestVersion = latestArmy?.Version;

                    if (_factionLogoCacheService is not null && latestArmy?.Resume is not null)
                    {
                        await _factionLogoCacheService.CacheUnitLogosAsync(factionId, latestArmy.Resume);
                    }

                    if (_factionProvider is not null && _armyImportProvider is not null && latestArmy is not null)
                    {
                        if (string.IsNullOrWhiteSpace(latestVersion))
                        {
                            skippedCount++;
                            continue;
                        }

                        var snapshot = _factionProvider.GetFactionSnapshot(factionId);
                        var storedVersion = snapshot?.Version;

                        if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                        {
                            skippedCount++;
                            continue;
                        }

                        await _armyImportProvider.ImportAsync(factionId, latestArmy);
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"DownloadMetadataAsync faction {factionId} failed: {ex.Message}");
                    errorCount++;
                }
            }

            MetadataStatus = $"Metadata imported. Updated: {updatedCount}, Unchanged: {skippedCount}, Errors: {errorCount}.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DownloadMetadataAsync failed: {ex.Message}");
            MetadataStatus = $"Download failed: {ex.Message}";
        }
    }

    private async Task UpdateAllDataAsync()
    {
        if (IsUpdateInProgress)
        {
            return;
        }

        if (_importService is not null)
        {
            try
            {
                IsUpdateInProgress = true;
                UpdateStatus = "Running online update...";
                UpdateProgressMessage = "Starting update...";

                await foreach (var result in _importService.ImportAllDataAsync())
                {
                    UpdateProgressMessage = result.Message;
                    UpdateStatus = result.Message;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UpdateAllDataAsync failed: {ex.Message}");
                UpdateStatus = $"Update failed: {ex.Message}";
            }
            finally
            {
                IsUpdateInProgress = false;
                UpdateProgressMessage = string.Empty;
            }

            return;
        }

        if (_infinityArmyApi is null || _metadataProvider is null || _factionProvider is null || _armyImportProvider is null)
        {
            UpdateStatus = "Required services are not available.";
            return;
        }

        try
        {
            IsUpdateInProgress = true;
            UpdateProgressMessage = "Updating database: downloading metadata...";
            UpdateStatus = "Downloading metadata...";

            var metadataDocument = await _infinityArmyApi.GetMetaDataAsync();
            if (metadataDocument is null)
            {
                UpdateStatus = "Metadata download succeeded but parsing failed.";
                return;
            }

            UpdateProgressMessage = "Updating database: importing metadata...";
            _metadataProvider.Import(metadataDocument);

            if (metadataDocument.Factions.Count == 0)
            {
                UpdateStatus = "Metadata download succeeded but no factions were found.";
                return;
            }

            if (_factionLogoCacheService is not null)
            {
                UpdateProgressMessage = "Updating SVGs: caching faction logos...";
                var logoCacheResult = await _factionLogoCacheService.CacheAllAsync(metadataDocument.Factions);
                var debugFaction = metadataDocument.Factions.FirstOrDefault(x => x.Id == FactionLogoCacheService.DebugFactionId);
                var debugInfo = _factionLogoCacheService.GetDebugInfo(FactionLogoCacheService.DebugFactionId, debugFaction?.Logo);
                Console.Error.WriteLine(
                    $"[SVG DEBUG] Faction {debugInfo.FactionId}: exists={debugInfo.Exists}, bytes={debugInfo.SizeBytes}, path={debugInfo.LocalPath}, url={debugInfo.ExpectedLogoUrl ?? "<null>"}");

                UpdateStatus =
                    $"SVG cache complete. Downloaded: {logoCacheResult.Downloaded}, Reused from cache: {logoCacheResult.CachedReuse}, Failed: {logoCacheResult.Failed}, Missing URL: {logoCacheResult.MissingLogoUrl}, Invalid URL: {logoCacheResult.InvalidLogoUrl}. " +
                    $"Hayabusa(1199): Exists={debugInfo.Exists}, Bytes={debugInfo.SizeBytes}.";
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
                    UpdateProgressMessage = $"Updating factions: checking {factionId}...";
                    UpdateStatus = $"Checking faction {factionId}...";

                    var latestArmy = await _infinityArmyApi.GetArmyDataAsync(factionId);
                    var latestVersion = latestArmy?.Version;

                    if (_factionLogoCacheService is not null && latestArmy?.Resume is not null)
                    {
                        await _factionLogoCacheService.CacheUnitLogosAsync(factionId, latestArmy.Resume);
                    }

                    if (string.IsNullOrWhiteSpace(latestVersion))
                    {
                        skippedCount++;
                        continue;
                    }

                    if (latestArmy is null)
                    {
                        skippedCount++;
                        continue;
                    }

                    var snapshot = _factionProvider.GetFactionSnapshot(factionId);
                    var storedVersion = snapshot?.Version;

                    if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    await _armyImportProvider.ImportAsync(factionId, latestArmy);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"UpdateAllDataAsync faction {factionId} failed: {ex.Message}");
                    errorCount++;
                }
            }

            UpdateStatus = $"Update complete. Updated: {updatedCount}, Unchanged: {skippedCount}, Errors: {errorCount}.";
            UpdateProgressMessage = "Finalizing update...";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateAllDataAsync failed: {ex.Message}");
            UpdateStatus = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdateInProgress = false;
            UpdateProgressMessage = string.Empty;
        }
    }

    private Task LoadGlobalSettingsAsync()
    {
        if (_appSettingsProvider is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var showInches = _appSettingsProvider.GetShowUnitsInInches();
            _showUnitsInInches = showInches;
            OnPropertyChanged(nameof(ShowUnitsInInches));
            OnPropertyChanged(nameof(ShowUnitsInCentimeters));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadGlobalSettingsAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private Task SaveDisplayUnitsSettingAsync(bool showInches)
    {
        if (_appSettingsProvider is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            _appSettingsProvider.SetShowUnitsInInches(showInches);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SaveDisplayUnitsSettingAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
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
        if (_infinityArmyApi is null)
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

            var latestArmy = await _infinityArmyApi.GetArmyDataAsync(factionId);
            var latestVersion = latestArmy?.Version;

            if (_factionProvider is not null && _armyImportProvider is not null && latestArmy is not null)
            {
                if (string.IsNullOrWhiteSpace(latestVersion))
                {
                    ArmyStatus = "Downloaded army data has no version; skipped.";
                    return;
                }

                var snapshot = _factionProvider.GetFactionSnapshot(factionId);
                var storedVersion = snapshot?.Version;
                if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                {
                    ArmyStatus = $"No update needed for faction {factionId}. Stored: {storedVersion}, Incoming: {latestVersion}.";
                    return;
                }

                await _armyImportProvider.ImportAsync(factionId, latestArmy);
                ArmyStatus = $"Faction {factionId} updated to version {latestVersion}.";
            }
            else
            {
                ArmyStatus = "Army data downloaded, but DB accessor is unavailable.";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DownloadArmyDataAsync failed: {ex.Message}");
            ArmyStatus = $"Download failed: {ex.Message}";
        }
    }
}

