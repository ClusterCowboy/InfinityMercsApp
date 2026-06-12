using System.Globalization;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Views.Season;

public partial class MissionOutcomePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private bool? _victory;
    private int? _pointsScored;

    public MissionOutcomePage()
    {
        InitializeComponent();

        for (var i = 0; i <= 10; i++)
            PointsPicker.Items.Add(i.ToString(CultureInfo.InvariantCulture));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
        if (query.TryGetValue("seasonFilePath", out var seasonRaw))
            _seasonFilePath = Uri.UnescapeDataString(seasonRaw?.ToString() ?? string.Empty);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RebuildBreakdown();
    }

    private void OnVictoryClicked(object sender, EventArgs e)
    {
        _victory = true;
        VictoryButton.BackgroundColor = Color.FromArgb("#166534");
        VictoryButton.TextColor = Colors.White;
        DefeatButton.BackgroundColor = Color.FromArgb("#374151");
        DefeatButton.TextColor = Color.FromArgb("#DC2626");
        MissionOutcomePageData.Victory = true;
        RebuildBreakdown();
        UpdateContinueState();
    }

    private void OnDefeatClicked(object sender, EventArgs e)
    {
        _victory = false;
        DefeatButton.BackgroundColor = Color.FromArgb("#7F1D1D");
        DefeatButton.TextColor = Colors.White;
        VictoryButton.BackgroundColor = Color.FromArgb("#374151");
        VictoryButton.TextColor = Color.FromArgb("#22C55E");
        MissionOutcomePageData.Victory = false;
        RebuildBreakdown();
        UpdateContinueState();
    }

    private void OnPointsChanged(object sender, EventArgs e)
    {
        if (PointsPicker.SelectedIndex >= 0)
        {
            _pointsScored = PointsPicker.SelectedIndex;
            MissionOutcomePageData.PointsScored = _pointsScored.Value;
        }
        RebuildBreakdown();
        UpdateContinueState();
    }

    private void UpdateContinueState()
    {
        var ready = _victory.HasValue && _pointsScored.HasValue;
        ContinueButton.IsEnabled = ready;
        ContinueButton.Opacity = ready ? 1.0 : 0.45;
    }

    private void RebuildBreakdown()
    {
        BreakdownStack.Children.Clear();

        var roundNumber = MissionOutcomePageData.CurrentRound;
        var points = _pointsScored ?? 0;
        var missionCredits = Math.Min(10 * roundNumber, 40);
        var objectivesSecured = 4 * points;
        var victoryBonus = _victory == true ? 10 : 0;
        var sum = missionCredits + objectivesSecured + victoryBonus;

        BreakdownStack.Children.Add(MakeRow("Round Number", roundNumber.ToString(CultureInfo.InvariantCulture)));
        BreakdownStack.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#374151"),
            Margin = new Thickness(0, 4, 0, 4)
        });
        BreakdownStack.Children.Add(MakeRow("Mission Credits", missionCredits.ToString(CultureInfo.InvariantCulture)));
        BreakdownStack.Children.Add(MakeRow("Objectives Secured", objectivesSecured.ToString(CultureInfo.InvariantCulture)));
        if (_victory == true)
            BreakdownStack.Children.Add(MakeRow("Victory", "10"));

        BreakdownStack.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#374151"),
            Margin = new Thickness(0, 4, 0, 4)
        });
        BreakdownStack.Children.Add(MakeRow("SUM", sum.ToString(CultureInfo.InvariantCulture), boldRight: true));
        BreakdownStack.Children.Add(MakeRow("SWC", "0.5", boldRight: true, rightColor: Color.FromArgb("#F59E0B")));

        MissionOutcomePageData.MissionCredits = missionCredits;
        MissionOutcomePageData.ObjectivesSecured = objectivesSecured;
        MissionOutcomePageData.VictoryBonus = victoryBonus;
        MissionOutcomePageData.CreditsTotal = sum;
    }

    private static Grid MakeRow(string label, string value, bool boldRight = false, Color? rightColor = null)
    {
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new Label
        {
            Text = label,
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center
        };
        var right = new Label
        {
            Text = value,
            TextColor = rightColor ?? Colors.White,
            FontSize = 14,
            FontAttributes = boldRight ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalTextAlignment = TextAlignment.Center
        };

        Grid.SetColumn(right, 1);
        row.Children.Add(left);
        row.Children.Add(right);
        return row;
    }

    private async void OnGoToExperienceClicked(object sender, EventArgs e)
    {
        await SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.MissionResults.Won = _victory == true;
            round.MissionResults.OpScored = _pointsScored ?? 0;
        });

        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        var encodedSeasonPath = Uri.EscapeDataString(_seasonFilePath);
        await Shell.Current.GoToAsync($"{nameof(ExperiencePage)}?companyFilePath={encodedPath}&seasonFilePath={encodedSeasonPath}");
    }
}

public static class MissionOutcomePageData
{
    public static int CurrentRound { get; set; } = 1;
    public static bool? Victory { get; set; }
    public static int? PointsScored { get; set; }
    public static int MissionCredits { get; set; }
    public static int ObjectivesSecured { get; set; }
    public static int VictoryBonus { get; set; }
    public static int CreditsTotal { get; set; }
    public static double SwcGain { get; } = 0.5;
}
