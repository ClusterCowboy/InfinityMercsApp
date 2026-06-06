namespace InfinityMercsApp.Views.Season;

public partial class GameModePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;

    public GameModePage()
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

    private async void OnEndGameClicked(object sender, EventArgs e)
    {
        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        await Shell.Current.GoToAsync($"{nameof(ExperiencePage)}?companyFilePath={encodedPath}");
    }
}
