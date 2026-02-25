using System.Globalization;
using System.Windows.Input;
using InfinityMercsApp.Data.WebAccess;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IWebAccessObject? _webAccessObject;
    private int _count;
    private string _metadataStatus = "Metadata file not downloaded yet.";
    private string _armyStatus = "Army file not downloaded yet.";
    private string _factionIdInput = "1";

    public MainViewModel(IWebAccessObject? webAccessObject = null)
    {
        _webAccessObject = webAccessObject;
        IncrementCounterCommand = new Command(OnIncrementCounter);
        DownloadMetadataCommand = new Command(async () => await DownloadMetadataAsync());
        DownloadArmyDataCommand = new Command(async () => await DownloadArmyDataAsync());
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

    public ICommand IncrementCounterCommand { get; }

    public ICommand DownloadMetadataCommand { get; }

    public ICommand DownloadArmyDataCommand { get; }

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

            ArmyStatus = $"Saved army data to: {filePath}";
        }
        catch (Exception ex)
        {
            ArmyStatus = $"Download failed: {ex.Message}";
        }
    }
}
