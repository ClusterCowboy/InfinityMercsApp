namespace InfinityMercsApp.Views.Season;

public partial class DowntimePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;

    public DowntimePage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
        {
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
        }
    }

    private async void OnBackToBaseClicked(object sender, EventArgs e)
    {
        // Pop the entire play → experience → downtime stack back to SeasonPage.
        await Shell.Current.GoToAsync("../../../..");
    }
}
