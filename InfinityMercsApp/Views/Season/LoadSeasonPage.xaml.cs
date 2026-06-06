using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace InfinityMercsApp.Views.Season;

public partial class LoadSeasonPage : ContentPage
{
    private const string SeasonsDirectoryName = "Seasons";
    private const string RecordsDirectoryName = "MercenaryRecords";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ObservableCollection<LoadSeasonRecordItem> SeasonRecords { get; } = [];
    public bool HasSeasonRecords => SeasonRecords.Count > 0;
    public bool ShowEmptyState => !HasSeasonRecords;

    public LoadSeasonPage()
    {
        InitializeComponent();
        BindingContext = this;
        SeasonRecords.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSeasonRecords));
            OnPropertyChanged(nameof(ShowEmptyState));
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSeasonRecordsAsync();
    }

    private async Task LoadSeasonRecordsAsync()
    {
        SeasonRecords.Clear();

        var seasonsDir = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            RecordsDirectoryName,
            SeasonsDirectoryName);

        if (!Directory.Exists(seasonsDir))
        {
            return;
        }

        var companiesDir = Path.Combine(FileSystem.Current.AppDataDirectory, RecordsDirectoryName);

        var items = new List<LoadSeasonRecordItem>();
        foreach (var filePath in Directory.EnumerateFiles(seasonsDir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var payload = JsonSerializer.Deserialize<SeasonFileHeader>(json, JsonOptions);
                if (payload is null)
                {
                    continue;
                }

                var companyName = string.IsNullOrWhiteSpace(payload.CompanyName)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : payload.CompanyName;

                var companyFilePath = payload.CompanyFilePath;
                if (string.IsNullOrWhiteSpace(companyFilePath) && !string.IsNullOrWhiteSpace(payload.CompanyIdentifier))
                {
                    companyFilePath = await TryFindCompanyFileByIdentifierAsync(companiesDir, payload.CompanyIdentifier);
                }

                var createdUtc = DateTimeOffset.TryParse(
                    payload.CreatedDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed)
                    ? parsed
                    : new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero);

                items.Add(new LoadSeasonRecordItem
                {
                    SeasonFilePath = filePath,
                    CompanyFilePath = companyFilePath ?? string.Empty,
                    CompanyName = companyName,
                    SortDateUtc = createdUtc,
                    DisplayName = companyName,
                    Subtitle = $"Started {createdUtc:yyyy-MM-dd}"
                });
            }
            catch
            {
                // Skip malformed season files.
            }
        }

        foreach (var item in items.OrderByDescending(x => x.SortDateUtc))
        {
            SeasonRecords.Add(item);
        }
    }

    private static async Task<string?> TryFindCompanyFileByIdentifierAsync(string companiesDir, string companyIdentifier)
    {
        if (!Directory.Exists(companiesDir))
        {
            return null;
        }

        foreach (var filePath in Directory.EnumerateFiles(companiesDir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var payload = JsonSerializer.Deserialize<CompanyFileHeader>(json, JsonOptions);
                if (string.Equals(payload?.CompanyIdentifier, companyIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    return filePath;
                }
            }
            catch
            {
                // Skip unreadable files.
            }
        }

        return null;
    }

    private async void OnLoadSeasonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LoadSeasonRecordItem item)
        {
            return;
        }

        if (!File.Exists(item.CompanyFilePath))
        {
            await DisplayAlert("Company Not Found",
                $"The company file for '{item.CompanyName}' could not be found. It may have been deleted.",
                "OK");
            return;
        }

        var encodedCompanyPath = Uri.EscapeDataString(item.CompanyFilePath);
        var encodedSeasonPath = Uri.EscapeDataString(item.SeasonFilePath);
        await Shell.Current.GoToAsync(
            $"//{nameof(SeasonPage)}?companyFilePath={encodedCompanyPath}&seasonFilePath={encodedSeasonPath}");
    }

    private sealed class SeasonFileHeader
    {
        public string CompanyName { get; init; } = string.Empty;
        public string CompanyIdentifier { get; init; } = string.Empty;
        public string CompanyFilePath { get; init; } = string.Empty;
        public string CreatedDate { get; init; } = string.Empty;
    }

    private sealed class CompanyFileHeader
    {
        public string CompanyIdentifier { get; init; } = string.Empty;
    }
}

public sealed class LoadSeasonRecordItem
{
    public string SeasonFilePath { get; init; } = string.Empty;
    public string CompanyFilePath { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public DateTimeOffset SortDateUtc { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
}
