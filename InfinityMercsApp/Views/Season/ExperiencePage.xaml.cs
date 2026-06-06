namespace InfinityMercsApp.Views.Season;

public partial class ExperiencePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;

    public ExperiencePage()
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

    private async void OnConfirmExperienceClicked(object sender, EventArgs e)
    {
        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        await Shell.Current.GoToAsync($"{nameof(DowntimePage)}?companyFilePath={encodedPath}");
    }
}
