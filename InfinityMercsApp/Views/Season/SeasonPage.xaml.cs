using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Perks;

namespace InfinityMercsApp.Views.Season;

public partial class SeasonPage : ContentPage, IQueryAttributable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string _companyFilePath = string.Empty;

    public ObservableCollection<SeasonUnitItem> TeamUnits { get; } = [];

    public SeasonPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
        {
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCompanyAsync();
    }

    private async Task LoadCompanyAsync()
    {
        if (string.IsNullOrWhiteSpace(_companyFilePath) || !File.Exists(_companyFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_companyFilePath);
            var data = JsonSerializer.Deserialize<SeasonCompanyData>(json, JsonOptions);
            if (data is null)
            {
                return;
            }

            CompanyNameLabel.Text = string.IsNullOrWhiteSpace(data.CompanyName) ? "Season" : data.CompanyName;
            CompanyTypeLabel.Text = data.CompanyType;

            TeamUnits.Clear();
            foreach (var entry in data.Entries.Where(e => !e.IsPeripheralUnit))
            {
                var displayName = string.IsNullOrWhiteSpace(entry.CustomName) ? entry.Name : entry.CustomName;
                if (entry.IsLieutenant)
                {
                    displayName = $"[Lt.] {displayName}";
                }

                TeamUnits.Add(new SeasonUnitItem
                {
                    DisplayName = displayName,
                    Cost = entry.Cost,
                    ExperiencePoints = entry.ExperiencePoints,
                    ExperienceRankName = CompanyUnitExperienceRanks.GetRankName(entry.ExperiencePoints)
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonPage failed to load company: {ex.Message}");
        }
    }

    private void OnTeamTabClicked(object sender, EventArgs e) => SetActiveTab(0);
    private void OnInventoryTabClicked(object sender, EventArgs e) => SetActiveTab(1);
    private void OnMarketplaceTabClicked(object sender, EventArgs e) => SetActiveTab(2);

    private void SetActiveTab(int index)
    {
        TeamTabContent.IsVisible = index == 0;
        InventoryTabContent.IsVisible = index == 1;
        MarketplaceTabContent.IsVisible = index == 2;

        TeamTabButton.TextColor = index == 0 ? Color.FromArgb("#22C55E") : Color.FromArgb("#6B7280");
        InventoryTabButton.TextColor = index == 1 ? Color.FromArgb("#22C55E") : Color.FromArgb("#6B7280");
        MarketplaceTabButton.TextColor = index == 2 ? Color.FromArgb("#22C55E") : Color.FromArgb("#6B7280");

        TeamTabButton.FontAttributes = index == 0 ? FontAttributes.Bold : FontAttributes.None;
        InventoryTabButton.FontAttributes = index == 1 ? FontAttributes.Bold : FontAttributes.None;
        MarketplaceTabButton.FontAttributes = index == 2 ? FontAttributes.Bold : FontAttributes.None;
    }

    private async void OnPlayRoundClicked(object sender, EventArgs e)
    {
        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        await Shell.Current.GoToAsync($"{nameof(PlayModePage)}?companyFilePath={encodedPath}");
    }

    private sealed class SeasonCompanyData
    {
        public string CompanyName { get; init; } = string.Empty;
        public string CompanyType { get; init; } = string.Empty;
        public List<SeasonCompanyEntry> Entries { get; init; } = [];
    }

    private sealed class SeasonCompanyEntry
    {
        public string Name { get; init; } = string.Empty;
        public string CustomName { get; init; } = string.Empty;
        public bool IsPeripheralUnit { get; init; }
        public bool IsLieutenant { get; init; }
        public int Cost { get; init; }
        public int ExperiencePoints { get; init; }
    }
}

public sealed class SeasonUnitItem
{
    public string DisplayName { get; init; } = string.Empty;
    public int Cost { get; init; }
    public int ExperiencePoints { get; init; }
    public string ExperienceRankName { get; init; } = string.Empty;
}
